using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using NArctic;
using Utilities;
using System.Runtime.InteropServices;
using System.Text;

namespace NArctic
{
	public static class DateTime64
	{
		public static DateTime UnixEpoch = new DateTime(1970,01,01);

		public static DateTime ToDateTime(long ns)
		{
			return UnixEpoch.AddTicks (ns / 100);
		}

		public static TimeSpan ToTimeSpan(long ns)
		{
			return TimeSpan.FromTicks (ns / 100);
		}

		public static long ToDateTime64(this DateTime dt)
		{
			return dt.Subtract (UnixEpoch).Ticks * 100;
		}

		public static long ToDateTime64(this TimeSpan dt)
		{
			return dt.Ticks * 100;
		}

        public static Func<DateTime, long> TimeGrouping(string freq)
        {
            if (freq == Years)
                return dt => dt.Year - UnixEpoch.Year;
            else if (freq == Months)
                return dt => (dt.Year - UnixEpoch.Year) * 12 + (dt.Month - UnixEpoch.Month);
            else if (freq == Days)
                return dt => (long)(dt - UnixEpoch).TotalDays;
            else if(freq == Seconds)
                return dt => (long)(dt - UnixEpoch).TotalSeconds;
            else if (freq == Milliseconds)
                return dt => (dt - UnixEpoch).Ticks/10000;
            throw new ArgumentException(freq);
        }

        public static string Months = "M";
        public static string Years = "Y";
        public static string Days = "D";
        public static string Hours = "h";
        public static string Minutes = "m";
        public static string Seconds = "s";
        public static string Milliseconds = "f";

        public static TimeSpan TimeFreq(string freq)
        {
            if (freq == Years)
                return TimeSpan.FromDays(366);
            else if (freq == Months)
                return TimeSpan.FromDays(31);
            else if (freq == Days)
                return TimeSpan.FromDays(1);
            else if (freq == Seconds)
                return TimeSpan.FromSeconds(1);
            else if (freq == Milliseconds)
                return TimeSpan.FromTicks(10000);
            throw new ArgumentException(freq);
        }

        public static string ToTimeFrameString(this TimeSpan tf)
        {
            if (tf < TimeSpan.FromSeconds(1))
                return "u{0}".Args(tf.Ticks / TimeSpan.FromMilliseconds(1).Ticks);
            if (tf <  TimeSpan.FromMinutes(1))
                return "s{0}".Args(tf.Ticks / TimeSpan.FromSeconds(1).Ticks);
            if (tf < TimeSpan.FromHours(1))
                return "m{0}".Args(tf.Ticks / TimeSpan.FromMinutes(1).Ticks);
            if (tf < TimeSpan.FromDays(1))
                return "H{0}".Args(tf.Ticks / TimeSpan.FromHours(1).Ticks);
            return "D{0}".Args(tf.Ticks / TimeSpan.FromDays(1).Ticks);
        }

    }

    public enum EndianType { Native, Big, Little } 

	public class DType
	{
		public Type Type;
		public List<DType> Fields = new List<DType>();
		public DType Parent;
		public string Name;
		public int Size;
        public EndianType Endian = EndianType.Native;
        public Encoding EncodingStyle;
		public string Format = null;
		public static Dictionary<Type,string> Formats = new Dictionary<Type,string> ();
		public static string sep = ", ";
	 
		public static DType DateTime64 { get { return new DType("'<M8[ns]'"); } }
		public static DType Long { get { return new DType("'<i8'"); } }
        public static DType Int { get { return new DType("'<i4'"); } } 
        public static DType Double { get { return new DType("'<f8'"); } }
        public static DType Bool { get { return new DType("'?'"); } }

        static DType()
		{
			Formats [typeof(DateTime)] = "{0:yyyy-MM-dd HH:mm:ss.fff}";
			Formats [typeof(double)] = "{0: 0.0000e+0}";
		}

		public DType(Type type=null, DType parent=null)
		{
			Type = type;
			Parent = null;
		}

		public DType(string spec)
		{
			new DTypeParser ().Parse (spec, 0, this);
		}

        public DType Clone()
        {
            var clone = new DType(this.Type)
            {
                Name = this.Name,
                Size = this.Size,
                Format = this.Format
            };
            foreach (var f in Fields)
            {
                var ff = f.Clone();
                ff.Parent = this;
                clone.Fields.Add(ff);
            }
            return clone;
        }

		public int FieldOffset(int ifield)
		{
			int offset = 0;
			for (int i = 0; i < ifield; i++)
				offset += Fields [i].Size;
			return offset;
		}

		public void FillBufferFromData<T>(byte[] buf, T[] data, int ofs, int iheight, int icol) {
			var bytesPerRow = this.FieldOffset (this.Fields.Count);
			var fieldOffset = this.FieldOffset (icol);
			var dtype = this.Fields[icol];
			int elsize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
			GCHandle ghsrc = GCHandle.Alloc (data, GCHandleType.Pinned);
			GCHandle ghdst = GCHandle.Alloc (buf, GCHandleType.Pinned);
			UnsafeAPI.ColumnCopy(ghdst.AddrOfPinnedObject(), fieldOffset, bytesPerRow, ghsrc.AddrOfPinnedObject()+ofs*UnsafeAPI.SizeOf<T>(), 0, elsize, iheight, elsize);
			ghsrc.Free ();
			ghdst.Free ();
		}

		public unsafe void FillDataFromBuffer<T>(byte[] buf, T[] data, int iheight, int icol) {
			var bytesPerRow = this.FieldOffset (this.Fields.Count);
			var fieldOffset = this.FieldOffset (icol);
			var dtype = this.Fields[icol];
			int elsize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
			GCHandle ghsrc = GCHandle.Alloc (buf, GCHandleType.Pinned);
			GCHandle ghdst = GCHandle.Alloc (data, GCHandleType.Pinned);
			UnsafeAPI.ColumnCopy(ghdst.AddrOfPinnedObject(), 0, elsize, ghsrc.AddrOfPinnedObject(), fieldOffset, bytesPerRow, iheight, elsize);
			ghsrc.Free ();
			ghdst.Free ();
		}

        public void FillDataFromBufferSlow<T>(byte[] buf, T[] data, int iheight, int icol, Func<byte[], int, int, T> byteConverter)
        {
            var bytesPerRow = this.FieldOffset(this.Fields.Count);
            var dtype = this.Fields[icol];
            var fieldOffset = this.FieldOffset(icol);

            for (int i = 0; i < buf.Length; i += bytesPerRow)
            {
                data[(i / bytesPerRow)] = byteConverter(buf, i + fieldOffset, dtype.Size);
            }
        }

        public string ToString(object value)
		{
			var fmt = Format ?? Formats.Get(value.GetType()) ?? "{0}";
			return fmt.Args (value);
		}

        private string ToString(EndianType endian)
        {
            switch(endian)
            {
                case EndianType.Native:
                    return "=";
                case EndianType.Little:
                    return "<";
                case EndianType.Big:
                    return ">";
                default:
                    throw new InvalidOperationException();
            }
            
        }

		public override string ToString ()
		{
			if (Type == null) {
				return "null";
			}else if (Type == typeof(IList<object>)) {
				string str = string.Join(DType.sep, Fields.Select(f=>f.ToString()));
				return "[{0}]".Args(str);
			} else if (Type == typeof(IDictionary<string, object>)) {
				string str = string.Join(DType.sep, Fields.Select(f=>"('{0}','{1}')".Args(f.Name, f)));
				return "[" + str + "]";
			} else {
                if (Type == typeof(double))
                    return "<f8";
                else if (Type == typeof(long))
                    return "<i8";
                else if (Type == typeof(int))
                    return "<i4";
                else if (Type == typeof(DateTime))
                    return "<M8[ns]";
                else if (Type == typeof(bool))
                    return "?";
                else if (Type == typeof(string) && EncodingStyle == Encoding.UTF8)
                    return $"{ToString(Endian)}S{Size}";
                else if (Type == typeof(string) && EncodingStyle == Encoding.Unicode)
                    return $"{ToString(Endian)}U{Size}";
                else
                    throw new InvalidOperationException("unknown numpy dtype '{0}'".Args(Type));
			}
				
		}
	}
}
