using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NumCIL;
using MongoDB.Driver;

namespace NArctic
{
	namespace Time {
		using NdArray = NumCIL.Int64.NdArray;
		using Generate = NumCIL.Int64.Generate;

		public class Series : Series<DateTime>
		{
			public NdArray Values {get;set;}

			public Series(NdArray values)
			{
				DType = DType.DateTime64;
				Values = values;
			}

			public Series(long[] values)
			{
				DType = DType.DateTime64;
				Values =  new NdArray(values);
			}

			public Series(IEnumerable<DateTime> data) :
				this(new NumCIL.Int64.NdArray(data.Select (DateTime64.ToDateTime64).ToArray ()))
			{
				
			}

			public static Series DateTimeRange(DateTime start, DateTime end, long count)
			{
				var np = Generate.Range (new Shape (count)) * (end - start).ToDateTime64 () / count;
				return new Time.Series (np);
			}

			public static implicit operator Series(NdArray values) {
				return new Series (values);
			}

			public static implicit operator NdArray(Series series) {
				return series.Values;
			}

			public override int Count
			{
				get{ return (int)Values.Shape.Dimensions [0].Length;}

			}

			public override NArctic.Series Clone()
			{
				return new Series (this.Values.Clone ());
			}

			public override T As<T>(int index)
			{
				return (object)this [index] as T;
			}

			public override IEnumerator<DateTime> GetEnumerator() 
			{
				foreach (var x in Values.Value) {
					yield return DateTime64.ToDateTime(x);
				}
			}

			public override DateTime this[int index]
			{
				get {
					return DateTime64.ToDateTime(Values.Value [index]);
				}
				set { 
					Values.Value [index] = value.ToDateTime64 ();
				}
			}

			public override NArctic.Series this [Range range] {
				get {
					return new Series(Values[range]);
				}
			}

			public unsafe override void  ToBuffer(byte[] buf, DType buftype, int iheight, int icol)
			{
				var bytesPerRow = buftype.FieldOffset (buftype.Fields.Count);
				var fieldOffset = buftype.FieldOffset (icol);
				var dtype = buftype.Fields[icol];
				long[] data = this.Values.DataAccessor.AsArray();
				int elsize = sizeof(long);
				fixed(void *dst = &buf[0])
				fixed(void *src = &data[0])
				ColumnCopy(dst, fieldOffset, bytesPerRow, src, 0, elsize, iheight, elsize);
			}

			public unsafe static Series FromBuffer(byte[] buf, DType buftype, int iheight, int icol )
			{
				var bytesPerRow = buftype.FieldOffset (buftype.Fields.Count);
				var fieldOffset = buftype.FieldOffset (icol);
				var dtype = buftype.Fields[icol];
				var data = new long[iheight];
				int elsize = sizeof(long);
				fixed(void *src = &buf[0])
				fixed(void *dst = &data[0])
				ColumnCopy(dst, 0, elsize, src, fieldOffset, bytesPerRow, iheight, elsize);
				return new Series (data);
			}


			public override string ToString ()
			{
				return Values.ToString ();
			}
		}
	}
}

