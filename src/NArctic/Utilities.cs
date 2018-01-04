using System;
using System.Collections.Generic;
using MongoDB.Bson;
using System.Text;
using LZ4;
using System.Linq;
using System.Configuration;
using System.Collections;
using System.Runtime.InteropServices;
using NArctic;

namespace Utilities
{
	public static class StringMixin
	{
		public static string Args(this string fmt, params object[] args)
		{
			return string.Format (fmt, args);
		}
		public static string Joined(this string sep, params object[] args)
		{
			return sep.Joined (args.Select (x => "{0}".Args (x)));
		}
		public static string Joined(this string sep, IEnumerable<string> args)
		{
			return string.Join (sep, args);
		}
	}

	public static class MongoMixin
	{
		public static string ToString(this IList<BsonDocument> lst, string sep)
		{
			var sb = new StringBuilder ();
			foreach (var doc in lst) {
				sb.Append (doc.ToString ());
				sb.Append (sep);
			}
			return sb.ToString ();
		}
	}

    public class ByteBuffer
    {
        private byte[] data;
        public int Length;

        public ByteBuffer(int capacity = 32)
        {
            data = new byte[capacity];
        }

        public byte[] GetBytes()
        {
            if (Length == data.Length)
                return data;
            byte[] value = new byte[Length];
            Array.Copy(data, 0, value, 0, Length);
            return value;
        }

        public void EnsureCapacity(int capacity)
		{
			if (data.Length<capacity) {
				byte[] next = new byte[capacity * 2];
				Buffer.BlockCopy (data, 0, next, 0, Length);
				data = next;
			}
		}

		public void Append(byte[] more)
		{
			var len = more.Length;
			EnsureCapacity (Length + len);
			Buffer.BlockCopy (more, 0, data, Length, len);
			Length += len;
		}


        public void Append<T>(T value)
        {
            var s = UnsafeAPI.SizeOf<T>();
            EnsureCapacity(Length + s);
            UnsafeAPI.Write<T>(this.data, Length, value);
            Length += s;
        }

        public T Read<T>(int offset)
        {
            return UnsafeAPI.Read<T>(this.data, offset);
        }

		public ByteBuffer AppendDecompress(byte[] encoded)
		{
			int len = (int)((uint)encoded[0] + ((uint)encoded[1]<<8) + ((uint)encoded[2]<<16)  + ((uint)encoded[3]<<24));
			EnsureCapacity (Length + len);
			LZ4Codec.Decode(encoded, 4, encoded.Length-4, data, Length, len, true);
			Length += len;
			return this;
		}

		public ByteBuffer AppendCompress(byte[] raw)
		{
			int maxlen = LZ4Codec.MaximumOutputLength (raw.Length);
			EnsureCapacity (Length + maxlen + 4);
			int len = LZ4Codec.Encode (raw, 0, raw.Length, data, Length + 4, maxlen);

			data [Length] = (byte)(raw.Length & 0xFF);
			data [Length+1] = (byte)((raw.Length>>8) & 0xFF);
			data [Length+2] = (byte)((raw.Length>>16) & 0xFF);
			data [Length+3] = (byte)((raw.Length>>24) & 0xFF);

			Length += len + 4;
			return this;
		}
	}

	public static class Collections
	{
		public static TValue Get<TKey,TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue def = default(TValue))
		{
			TValue val;
			if (!dict.TryGetValue(key, out val))
				return def;
			return val;
		}

		public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue val)
		{
			TValue v;
			if (dict.TryGetValue(key, out v))
				return v;
			dict.Add(key, val);
			return val;
		}

		public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue :class,new()
		{
			TValue v;
			if (dict.TryGetValue(key, out v))
				return v;
			v = new TValue();
			dict.Add(key, v);
			return v;
		}

        public static List<T> ToList<T>(T value) {
            var l = new List<T>();
            l.Add(value);
            return l;
        }


		public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TKey,TValue> getval)
		{
			TValue val;
			if (dict.TryGetValue(key, out val))
				return val;
			dict.Add(key, val=getval(key));
			return val;
		}

        public static TValue Unwrap<T, TValue>(this T nullable, Func<T,TValue> getter, TValue def=default(TValue)) where T : class
        {
            return nullable == null ? def : getter(nullable);
        }

	}

    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime StopDate {get; set; }

        public DateRange(DateTime start = default(DateTime), DateTime stop = default(DateTime))
        {
            StartDate = start == default(DateTime) ? DateTime.MinValue : start;
            StopDate = stop == default(DateTime) ? DateTime.MinValue : stop;
        }
    }

    public static class Dates
    {
        public static DateRange ToDateRange(this DateTime date, int count)
        {
            return new DateRange(date, date.AddDays(count));
        }
    }

}

