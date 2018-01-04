using System;
using System.Collections;
using System.Collections.Generic;
using NumCIL;

namespace NArctic
{
	namespace Long {
		using NdArray = NumCIL.Int64.NdArray;
		using Generate = NumCIL.Int64.Generate;

		public class Series : Series<long>
		{
			public NdArray Values {get;set;}

			public Series(NdArray values)
			{
				DType = DType.Long;
				Values = values;
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
				return this [index];
			}

			public override IEnumerator<long> GetEnumerator() 
			{
				foreach (var x in Values.Value as IEnumerable<long>) {
					yield return x;
				}
			}

			public override long this[int index]
			{
				get {
					return Values.Value[index];
				}
				set { 
					Values.Value [index] = value;
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
				long[] data = (this.Values as Int64.NdArray).DataAccessor.AsArray();
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
				return data.ToLongSeries ();
			}

			public override string ToString ()
			{
				IEnumerable<double> itr = (IEnumerable<double>)this;
				return (Name!=null?Name+"\n":"") + string.Join ("\n",itr.Select(x => "{0}".Args (x)));
			}
		}
	}
}

