using System;
using System.Linq;
using Utilities;
using System.Collections.Generic;

using NumCIL.Generic;
using NumCIL;

namespace NArctic
{
	public abstract class Series<T, Q> : Series
	{
		public NdArray<Q> Values {get;set;}
		public Func<Q,T> Getter;
		public Func<T,Q> Setter;

		public Series(DType dtype, NdArray<Q> values, Func<Q,T> getter, Func<T,Q> setter)
		{
			DType = dtype;
			Values = values;
			Getter = getter;
			Setter = setter;
		}

		public override int Count
		{
			get{ return (int)Values.Shape.Dimensions [0].Length;}
		}

		public override IEnumerator<T> GetEnumerator() 
		{
			foreach (Q x in Values.Value as IEnumerable<Q>) {
				yield return Getter(x);
			}
		}

		public override T this[int index]
		{
			get {
				Q q = Values.Value[index];
				return Getter(q); 
			}
			set { 
				Values.Value[index] = Setter((T)value); 
			}
		}

		/*public override Series this [Range range] {
			get {
				return new Series<T, Q> (DType, Values[range], Getter, Setter);
			}
		}*/

		public override string ToString ()
		{
			IEnumerable<T> itr = (IEnumerable<T>)this;
			return (Name!=null?Name+"\n":"") + string.Join ("\n",itr.Select(x => "{0}".Args (x)));
		}
	}
}

