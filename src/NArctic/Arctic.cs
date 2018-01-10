using System;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using NumCIL;
using Utilities;
using System.Security.Cryptography;

namespace NArctic
{
    public class Arctic
	{
        public MongoClient Mongo { get; set; }
        public IMongoDatabase Db
        {
            get; set;
        }
		protected IMongoCollection<BsonDocument> _versions;
		protected IMongoCollection<BsonDocument> _segments;
		protected IMongoCollection<BsonDocument> _version_numbers;

        public string Name;

		public Arctic(MongoClient mongo, string lib)
		{
            string[] items = lib.Split(new[] { '.' }, 2);
            var db = items[0];
            this.Mongo = mongo;
            this.Db = mongo.GetDatabase(db);
            var name = items[1];
            Console.WriteLine("Connected mongo lib='{0}' db='{1}'".Args(lib, db));
			this._versions = Db.GetCollection<BsonDocument>(name+".versions");
			this._segments = Db.GetCollection<BsonDocument>(name);
			this._version_numbers = Db.GetCollection<BsonDocument> (name+".version_nums");
            this.Name = db;
		}

        public List<Tuple<DateTime,long>> GetSegmentsIndex(BsonDocument version)
        {
            if (!version.Contains("segment_index"))
                return null;

            var result = new List<Tuple<DateTime, long>>();

            var seg_ind_buf = new ByteBuffer();
            seg_ind_buf.AppendDecompress(version["segment_index"].AsByteArray);
            for(int ofs = 0; ofs < seg_ind_buf.Length; ofs += 16)
            {
                var t = seg_ind_buf.Read<long>(ofs);
                var rec = new Tuple<DateTime, long>(DateTime64.ToDateTime(t), seg_ind_buf.Read<long>(ofs + 8));
                result.Add(rec);
            }
            return result;
        }

        public override string ToString()
        {
            return "Arctic('{0}')".Args(this.Name);
        }

        public async Task<BsonDocument> ReadVersionAsync(string symbol)
		{
#if LOG
            Log.Debug("reading {symbol} version", symbol);
#endif
            IAsyncCursorSource<BsonDocument> versions = this._versions.AsQueryable ()
				.Where (x => x ["symbol"] == symbol)
				.OrderByDescending (x => x ["version"])
				.Take(1);
			var rtn =  await versions.FirstOrDefaultAsync<BsonDocument>();
#if LOG
            Log.Debug("read {0} version: {1}".Args (symbol, rtn));
#endif
            return rtn;
		}

        public DataFrame Read(string symbol, DateRange daterange=null, BsonDocument version=null)
        {
            return this.ReadAsync(symbol, daterange, version).Result;
        }

        
        public async Task<DataFrame> ReadAsync(string symbol, DateRange range=null, BsonDocument version=null)
		{
			version = version ?? await ReadVersionAsync(symbol);

            if (version == null)
				return null;
#if LOG
            Log.Debug("version: {0}".Args(version));
#endif

            var buf = new ByteBuffer ();
			
            var index = GetSegmentsIndex(version);
            var id = version["_id"];
            var parent = version.GetValue("base_version_id", null);
            if (parent == null)
                parent = id;

            var filter = BF.Eq("symbol", symbol) & BF.Eq("parent", parent);
            int start_segment = 0;
            int end_segment = -1;

            if (index!=null && range != null)
            {
                foreach (var t in index)
                {
                    if (range.StartDate != default(DateTime) && range.StartDate > t.Item1)  // t.Item1 is inclusive end date of segment
                        start_segment = (int)t.Item2 + 1; // should start from next segment
                    if (range.StopDate != default(DateTime) && range.StopDate <= t.Item1 && end_segment==-1)
                        end_segment = (int)t.Item2;      // should stop at this segment
                }
                if (start_segment != 0)
                    filter = filter & BF.Gte("segment", start_segment);
                if (end_segment != -1)
                    filter = filter & BF.Lte("segment", end_segment);
            }
            if (end_segment == -1)
                end_segment = version["up_to"].AsInt32-1;

            var segments = await this._segments.FindAsync (filter);
			int segcount = 0;

            while (await segments.MoveNextAsync ()) {
				foreach (var segment in segments.Current) {
#if DEBUG
					//Log.Debug ("read segment: {0}".Args(segment));
#endif
					var chunk  = segment["data"].AsByteArray;
					if (segment ["compressed"].AsBoolean)
						buf.AppendDecompress (chunk);
					else
						buf.Append (chunk);
					segcount++;
				}
			}
            var metadataVal = version.GetValue("metadata", new BsonDocument());
            var metadata = !metadataVal.IsBsonNull ? metadataVal.AsBsonDocument : null;
            if (segcount == 0)
            {
                //var df1 = new DataFrame();
                //df1.Metadata = metadata;
                //return df1;
                //throw new InvalidOperationException("No segments found for {0}".Args(version));
            }
			
			var nrows = end_segment-start_segment+1;
			var dtype = version ["dtype"].AsString;
			var buftype = new DType (dtype);
			var bytes = buf.GetBytes ();
#if LOG
			Log.Debug ("converting to dataframe up_to={0} dtype={1} len={2}".Args (nrows, dtype, bytes.Length));
#endif
			var df = DataFrame.FromBuffer(buf.GetBytes(), buftype, nrows);
            var meta = version["dtype_metadata"].AsBsonDocument;
            var index_name = meta.GetValue("index", new BsonArray()).AsBsonArray[0];
            if (index_name != null && df.Columns.Contains(index_name.AsString)) {
                df.Index = df.Columns[index_name.AsString];
            }
            df.Metadata = metadata;
            df.Name = symbol;
            df.FilledCount = df.Rows.Count;
            // TODO: Filter first/last segment
            return df;
		}

		internal static FilterDefinitionBuilder<BsonDocument> BF {get{ return Builders<BsonDocument>.Filter;} }
		internal static UpdateDefinitionBuilder<BsonDocument> BU {get{ return Builders<BsonDocument>.Update;} }
		internal static UpdateOptions Upsert = new UpdateOptions{ IsUpsert = true };

        public long Delete(string symbol)
        {
            return DeleteAsync(symbol).Result;
        }

		public async Task<long> DeleteAsync(string symbol)
		{
			var rtn = await _segments.DeleteManyAsync (BF.Eq ("symbol", symbol));
			//Log.Information ("Deleted {count} segments for {symbol}", rtn.DeletedCount, symbol);
			rtn = await _versions.DeleteManyAsync (BF.Eq ("symbol", symbol));
			//Log.Information ("Deleted {count} versions for {symbol}", rtn.DeletedCount, symbol);
			return rtn.DeletedCount;
		}

        public IMongoCollection<BsonDocument> Versions { get { return _versions; } }

        public BsonDocument Append(string symbol, DataFrame df, int chunksize = 0)
        {
            return this.AppendAsync(symbol, df, chunksize).Result;
        }

		public async Task<BsonDocument> AppendAsync(string symbol, DataFrame df, int chunksize=0, bool skipAlreadyWrittenDates = true)
		{
            if (df.Index == null)
                throw new ArgumentException("Please specify DataFrame.Index column before saving");

            if(chunksize>0 && df.Rows.Count>chunksize)
            {
                var rng = Range.R(0, chunksize);
                BsonDocument ver = null;
				int chunkscount = 0;
                while (rng.First<df.Rows.Count) {
                    var chunk = df[rng];
                    ver = await AppendAsync(symbol, chunk);
                    rng = Range.R(rng.First + chunksize, rng.Last + chunksize);
					chunkscount++;
                }
                return ver;
            }
            int attemptNo = 0;
            for (;;)
            {
                var previous_version = await ReadVersionAsync(symbol);
                var version = await GetNewVersion(symbol, previous_version);

                /*var previous_version = await (_versions.AsQueryable ()
                    .Where (v => v ["symbol"] == symbol && v ["version"] < version ["version"])
                    .OrderByDescending (v => v ["version"]) as IAsyncCursorSource<BsonDocument>)
                    .FirstOrDefaultAsync ();*/

                var dtype = version.GetValue("dtype", "").ToString();
#if LOG
                Log.Debug("loaded dtype {0}", dtype);
#endif
                if (dtype != "" && df.DType.ToString() != dtype)
                {
                    // dtype changed. need reload old data and repack it.
                    throw new NotImplementedException("old dtype {0}, new dtype {1}: not implemented".Args(dtype, df.DType));
                }

                var sdt = df.DType.ToString();

                version["metadata"] = df.Metadata;

                version["dtype"] = sdt;
#if LOG
                Log.Debug("saved dtype {0}", sdt);
#endif

                version["shape"] = new BsonArray { { -1 } };
                version["dtype_metadata"] = new BsonDocument {
                    { "index", new BsonArray { { df.Index.Name } } } ,
                    { "columns", new BsonArray(df.Columns.Select(c=>c.Name).ToList()) }
                };
                version["type"] = "pandasdf";
                version["segment_count"] = previous_version != null ? previous_version["segment_count"].AsInt32 + 1 : 1;
                version["append_count"] = previous_version != null ? previous_version["append_count"].AsInt32 + 1 : 0;

                var seg_ind_buf = new ByteBuffer();
                int segment_offset = 0;
                bool is_date_time_index = DType.DateTime64.ToString().Equals(df.Index.DType.ToString());

                //version ["base_sha"] = version ["sha"];
                if (previous_version != null)
                {
                    var seg_ind = previous_version["segment_index"].AsByteArray;
                    seg_ind_buf.AppendDecompress(seg_ind);
                    segment_offset = previous_version["up_to"].AsInt32;
                    if (is_date_time_index && skipAlreadyWrittenDates)
                    {
                        long date = seg_ind_buf.Read<long>(seg_ind_buf.Length - 16);
                        DateTime dt = DateTime64.ToDateTime(date);
                        var range = df.Index.AsDateTime().RangeOf(dt, 0, df.FilledCount-1, Location.GT);
                        if (range.Last <= range.First)
                        {
#if LOG
                            Log.Information($"Skipped DataFrame.Append because date {dt} already written for {symbol}");
#endif
                            return null; // Hey all was skipped
                        }
                        else if (range.First != 0)
                        {
#if LOG
                            Log.Information($"Skipped DataFrame.Append initial {range.First} elements date {dt} already written for {symbol}");
#endif
                        }
                        df = df[range];
                    }
                }


                var up_to = segment_offset + df.Rows.Count;
                var buf = new ByteBuffer();

                // add index that is last datetime + 0-based int64 index of last appended record like (segment_count-1)
                if (is_date_time_index && df.Rows.Count > 0)
                {
                    var date = df.Index.AsDateTime().Source[-1];
                    seg_ind_buf.Append<long>(date);
                    seg_ind_buf.Append<long>(up_to - 1);
                }

                var seg_ind_buf2 = new ByteBuffer();
                seg_ind_buf2.AppendCompress(seg_ind_buf.GetBytes());
                version["segment_index"] = seg_ind_buf2.GetBytes();

                version["up_to"] = up_to;
                var bin = df.ToBuffer();
                buf.AppendCompress(bin);

                var sha1 = SHA1.Create();

                var sha = version.GetValue("sha", null);
                if (sha == null)
                {
                    byte[] hashBytes = sha1.ComputeHash(bin);
                    version["sha"] = new BsonBinaryData(hashBytes);
                }

#if false
			var buf2 = new ByteBuffer ();
			buf2.AppendDecompress (buf.GetBytes());
			var bin2 = buf2.GetBytes ();
			if (!bin.SequenceEqual (bin2))
				throw new InvalidOperationException ();
			var df2 = DataFrame.FromBuffer(bin2, df.DType, df.Rows.Count);
#endif
                var segment = new BsonDocument {
                    {"symbol", symbol},
                    {"data", new BsonBinaryData(buf.GetBytes())},
                    {"compressed", true},
                    {"segment", segment_offset + df.Rows.Count - 1},
                    {"parent", new BsonArray{ version["_id"] }},
                };

                var hash = new ByteBuffer();
                hash.Append(Encoding.ASCII.GetBytes(symbol));
                foreach (var key in segment.Names.OrderByDescending(x => x))
                {
                    var value = segment.GetValue(key);
                    if (value is BsonBinaryData)
                        hash.Append(value.AsByteArray);
                    else {
                        var str = value.ToString();
                        hash.Append(Encoding.ASCII.GetBytes(str));
                    }
                }
                segment["sha"] = sha1.ComputeHash(hash.GetBytes());

                //await _versions.InsertOneAsync(version);	
                try {
                    await _versions.ReplaceOneAsync(BF.Eq("symbol", symbol), version, Upsert);
                }catch(MongoWriteException e)
                {
#if LOG
                    Log.Information(e, "Retrying append symbol {symbol}, attempt {attemptNo}", symbol, attemptNo++);
#endif
                    continue;
                }
                await _segments.InsertOneAsync(segment);
                //Log.Information ("inserted new segment {segment} for symbol {symbol}", segment["_id"], symbol);
                //Log.Information ("replaced version {0} for symbol {symbol} sha1 {sha}", version["_id"], symbol, sha);

                // update parents in versions
                //var res = await _segments.UpdateManyAsync (BF.Eq("symbol", symbol), BU.Set ("parent", new BsonArray{ version ["_id"] }));
                //Log.Debug ("updated segments parents {0}".Args(res.MatchedCount));
                return version;
            }
		}

		public async Task<BsonDocument> GetNewVersion(string symbol, BsonDocument version=null)
		{
			version = version ?? new BsonDocument ();

			var version_num = await _version_numbers.FindOneAndUpdateAsync (
				                       	BF.Eq ("symbol", symbol),
				                       	BU.Inc ("version", 1),
									   	new FindOneAndUpdateOptions<BsonDocument> { 
											IsUpsert = true, 
											ReturnDocument = ReturnDocument.After 
										}
			                       );
			version ["version"] = version_num.Unwrap(v=>v["version"], 1);
			if(version.GetValue("_id",null)==null)
				version ["_id"] = new BsonObjectId (ObjectId.GenerateNewId ());
			version ["symbol"] = symbol;
			return version;
		}

        public async Task<List<BsonDocument>> ListSymbolsAsync()
        {
            return await (await _versions.FindAsync(x => true)).ToListAsync();
        }

        public async Task<List<BsonDocument>> ListSymbolsAsync(FilterDefinition<BsonDocument> filter)
        {
            var cur = await _versions.FindAsync(filter);
            return await cur.ToListAsync();
        }

        public async Task<List<BsonDocument>> ListSymbolsAsync(System.Linq.Expressions.Expression<Func<BsonDocument, bool>> filter)
        {
            var cur = await _versions.FindAsync(filter);
            return await cur.ToListAsync();
        }

        public IMongoCollection<BsonDocument> Symbols {
            get { return _versions; }
        }

        public List<BsonDocument> ListSymbols(System.Linq.Expressions.Expression<Func<BsonDocument, bool>> filter)
        {
            return ListSymbolsAsync(filter).Result;
        }

        public static FilterDefinitionBuilder<BsonDocument> Filter = Builders<BsonDocument>.Filter;
	}
}

	