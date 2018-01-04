using System;
using System.Linq;
using System.Collections.Generic;
using Utilities;
using NumCIL;
using MongoDB.Driver;

namespace NArctic
{
	namespace Double {
		using NdArray = NumCIL.Double.NdArray;
		using T = System.Double;

		public class Series : Series<T>
		{
			public NdArray Values {get;set;}

			public Series(NdArray values)
			{
				DType = DType.Of<T>();
				Values = values;
			}

			public Series(T[] data)
				:this(new NdArray(data))
			{
				
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

			public override IEnumerator<double> GetEnumerator() 
			{
				foreach (var x in Values.Value as IEnumerable<T>) {
					yield return x;
				}
			}

			public override T this[int index]
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
		}
	}
}