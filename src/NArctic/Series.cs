using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Utilities;
using NumCIL;
using NumCIL.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using MongoDB.Driver;
using NumCIL.Boolean;
using MongoDB.Bson;
using System.Text;

namespace NArctic
{
    public static class NumCILMixin
    {
        public static long Count<T>(this NdArray<T> array, int axis = 0)
        {
            return array.Shape.Dimensions[axis].Length;
        }

        public static long Dimension(this NumCIL.Double.NdArray array, int axis = 0)
        {
            return array.Shape.Dimensions[axis].Length;
        }

        public static NdArray<T> Fill<T>(this NdArray<T> array, Func<long, T> f, int axis = 0, long[] idx = null)
        {
            idx = idx ?? new long[array.Shape.Dimensions.Length];
            for (idx[axis] = 0; idx[axis] < array.Count(axis); idx[axis]++)
            {
                array.Value[idx] = f(idx[axis]);
            }
            return array;
        }

        public static NdArray<T> Fill<T>(this NdArray<T> array, Func<long, long, T> f, int axis1 = 0, int axis2 = 1, long[] idx = null)
        {
            idx = idx ?? new long[array.Shape.Dimensions.Length];
            for (idx[axis1] = 0; idx[axis1] < array.Count(axis1); idx[axis1]++)
                for (idx[axis2] = 0; idx[axis2] < array.Count(axis2); idx[axis2]++)
                {
                    array.Value[idx] = f(idx[axis1], idx[axis2]);
                }
            return array;
        }
        public static NumCIL.Double.NdArray CumSum(this NumCIL.Double.NdArray array, double start = 0.0)
        {
            NumCIL.Double.NdArray rtn = array.Copy();
            for (int i = 0; i < rtn.Dimension(); i++)
            {
                start += rtn.Value[i];
                rtn.Value[i] = start;
            }
            return rtn;
        }
    }

    public static class SeriesMixin
    {
        public static Series<double> ToSeries(this NumCIL.Double.NdArray array)
        {
            return new Series<double>(array);
        }
        public static Series<double> Apply(this Series<double> s, Func<NumCIL.Double.NdArray, NumCIL.Double.NdArray> f)
        {
            return new Series<double>(f(s.Values));
        }

        public static TimeSpan Mul(this TimeSpan ts, double x)
        {
            return new TimeSpan((long)(ts.Ticks * x));
        }
    }

    public abstract class Series : IEnumerable
    {
        public DType DType { get; set; }

        public string Name
        {
            get { return DType.Name; }
            set { DType.Name = value; }
        }

        public abstract int Count { get; set; }

        public abstract object At(int index);
        public abstract void Set(int index, object value);

        public virtual void Append(Series other)
        {
            throw new NotSupportedException();
        }

        public abstract Series this[Range range]
        {
            get;
        }

        public virtual BaseSeries<T> As<T>()
        {
            var typed = this as BaseSeries<T>;
            if (typed == null)
                throw new InvalidCastException();
            return typed;
        }

        public virtual Series<T, Q> As<T, Q>()
        {
            var typed = this as Series<T, Q>;
            if (typed == null)
                throw new InvalidCastException();
            return typed;
        }

        public DateTimeSeries AsDateTime()
        {
            return this as DateTimeSeries;
        }

        public abstract void ToBuffer(byte[] buf, DType buftype, int iheight, int icol);

        public static Series FromBufferByType(byte[] buf, DType buftype, int iheight, int icol)
        {
            if (buftype.Fields[icol].Type == typeof(double))
            {
                return Series<double>.FromBuffer(buf, buftype, iheight, icol);
            }
            else if (buftype.Fields[icol].Type == typeof(long))
            {
                return Series<long>.FromBuffer(buf, buftype, iheight, icol);
            }
            else if (buftype.Fields[icol].Type == typeof(int))
            {
                return Series<int>.FromBuffer(buf, buftype, iheight, icol);
            }
            else if (buftype.Fields[icol].Type == typeof(string))
            {
                return StringSeries.FromBuffer(buf, buftype, iheight, icol);
            }
            else if (buftype.Fields[icol].Type == typeof(DateTime))
            {
                return DateTimeSeries.FromBuffer(buf, buftype, iheight, icol, DateTime64.ToDateTime, DateTime64.ToDateTime64);
            }
            else
                throw new InvalidOperationException("Failed decode {0} type".Args(buftype.Fields[icol].Type));
        }

        public abstract Series Clone();

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this.At(i);
        }

        public static implicit operator Series(double[] data)
        {
            return new Series<double>(data);
        }

        public static implicit operator Series(long[] data)
        {
            return new Series<long>(data);
        }

        public static implicit operator Series(DateTime[] data)
        {
            return new DateTimeSeries(new Series<long>(new NdArray<long>(data.Select(DateTime64.ToDateTime64).ToArray())));
        }


        public NumCIL.Double.NdArray AsDouble
        {
            get
            {
                return new NumCIL.Double.NdArray(As<double>().Values);
            }
            set
            {
                As<double>().Values = value;
            }
        }

        public NumCIL.Int64.NdArray AsLong
        {
            get
            {
                return new NumCIL.Int64.NdArray(As<long>().Values);
            }
            set
            {
                As<long>().Values = value;
            }
        }

        public NumCIL.Int64.NdArray AsDateTime64
        {
            get
            {
                return new NumCIL.Int64.NdArray(As<DateTime, long>().Source.Values);
            }
            set
            {
                As<DateTime, long>().Source.Values = value;
            }
        }

        public static Series FromType(Type t, long count, DType dtype = null)
        {
            if (t == typeof(double))
                return new Series<double>(count, dtype);
            else if (t == typeof(long))
                return new Series<long>(count, dtype);
            else if (t == typeof(DateTime))
                return new DateTimeSeries(count, dtype);
            else if (t == typeof(int))
                return new Series<int>(count, dtype);
            else
                throw new ArgumentException("Type {0} not supported".Args(t));

        }

    }

    public abstract class BaseSeries<T> : Series, IList<T>
    {
        public abstract T this[int index] { get; set; }

        public BaseSeries(DType dtype)
        {
            DType = dtype;
            if (DType == null)
            {
                if (typeof(T) == typeof(double))
                    DType = DType.Double;
                else if (typeof(T) == typeof(long))
                    DType = DType.Long;
                else if (typeof(T) == typeof(DateTime))
                    DType = DType.DateTime64;
                else if (typeof(T) == typeof(int))
                    DType = DType.Int;
                else if (typeof(T) == typeof(string))
                    throw new InvalidOperationException("DType.String cannot be implied, needs size");
                else
                    throw new InvalidOperationException("unknown dtype");
            }
        }

        public virtual NdArray<T> Values
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public Tuple<T, T> Range
        {
            get
            {
                return new Tuple<T, T>(this[-1], this[0]);
            }
        }

        public Range RangeOf(T value, int startIndex = 0, int endIndex = -1, Location match = Location.GE)
        {
            var i = this.IndexOf(value, startIndex, endIndex, match);
            if (i < 0) return new NumCIL.Range();
            switch (match)
            {
                case Location.GE: return new NumCIL.Range(i, endIndex < 0 ? endIndex + 1 : endIndex);
                case Location.GT: return new NumCIL.Range(i, endIndex < 0 ? endIndex + 1 : endIndex);
                case Location.LT: return new NumCIL.Range(startIndex, i);
                case Location.LE: return new NumCIL.Range(startIndex, i + 1);
                case Location.EQ: return new NumCIL.Range(i);
            }
            throw new InvalidOperationException();
        }

        public abstract T[] AsArray();

        public abstract BaseSeries<T> Copy();

        #region IList<T>
        public virtual IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this[i];
        }

        public int IndexOf(T item)
        {
            return this.IndexOf(item, match: Location.EQ);
        }

        public virtual int IndexOf(T item, int startIndex = 0, int endIndex = -1, Location match = 0)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }


        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }
        #endregion
    }

    public class Series<T, Q> : BaseSeries<T>
    {
        public BaseSeries<Q> Source;

        static Q Tq(T t)
        {
            return (Q)(object)t;
        }

        static T Qt(Q q)
        {
            return (T)(object)q;
        }

        protected Func<T, Q> setter = Tq;
        protected Func<Q, T> getter = Qt;

        public Series(BaseSeries<Q> source, Func<Q, T> getter, Func<T, Q> setter, DType dtype = null)
            : base(dtype)
        {
            Source = source;
            if (setter != null)
                this.setter = setter;
            if (getter != null)
                this.getter = getter;
        }


        public override int Count
        {
            get
            {
                return Source.Count;
            }
            set
            {
                Source.Count = value;
            }
        }

        public override object At(int index)
        {
            return getter(Source[index]);
        }

        public override void Set(int index, object value)
        {
            Source[index] = setter((T)value);
        }

        public override void Append(Series other)
        {
            Source.Append((other as Series<T, Q>).Source);
        }

        public override Series Clone()
        {
            return Copy();
        }

        public override BaseSeries<T> Copy()
        {
            return new Series<T, Q>(Source.Copy() as Series<Q>, getter, setter, DType);
        }

        public override IEnumerator<T> GetEnumerator()
        {
            foreach (var q in Source)
                yield return getter(q);
        }

        public override int IndexOf(T item, int startIndex = 0, int endIndex = -1, Location match = Location.EQ)
        {
            return Source.IndexOf(setter(item), startIndex, endIndex, match);
        }


        public override T this[int index]
        {
            get
            {
                return getter(Source[index]);
            }
            set
            {
                Source[index] = setter(value);
            }
        }

        public override Series this[Range range]
        {
            get
            {
                return new Series<T, Q>(Source[range] as Series<Q>, getter, setter, DType.Clone());
            }
        }

        public override T[] AsArray()
        {
            return Source.AsArray().Select(getter).ToArray();
        }

        public static Series FromBuffer(byte[] buf, DType buftype, int iheight, int icol, Func<Q, T> getter, Func<T, Q> setter)
        {
            return new Series<T, Q>(Series<Q>.FromBuffer(buf, buftype, iheight, icol) as Series<Q>, getter, setter);
        }

        public override void ToBuffer(byte[] buf, DType buftype, int iheight, int icol)
        {
            Source.ToBuffer(buf, buftype, iheight, icol);
        }

        public override string ToString()
        {
            return "NdArray<{0}>({1}): {2}".Args(typeof(T).Name, Count);
        }

    }

    public class DateTimeSeries : Series<DateTime, long>
    {
        public DateTimeSeries(long count, DType dtype = null)
            : this(new Series<long>(count))
        {
        }

        public DateTimeSeries(IEnumerable<DateTime> values, string name = null) : this(
            new Series<long>(
                values.Select(DateTime64.ToDateTime64).ToArray(), name)
            )
        {
            Name = name;
        }

        //public DateTimeSeries(NdArray<long> array, DType dtype=null)
        //    : this(new Series<long>(array))
        //{
        //}

        public DateTimeSeries(BaseSeries<long> source, DType dtype = null)
            : base(source, getter: DateTime64.ToDateTime, setter: DateTime64.ToDateTime64, dtype: dtype ?? DType.DateTime64)
        {

        }

        public override BaseSeries<DateTime> Copy()
        {
            return new DateTimeSeries(Source.Copy(), DType.Clone());
        }

        public override Series Clone()
        {
            return new DateTimeSeries(Source.Copy(), DType.Clone());
        }

        public override Series this[Range range]
        {
            get
            {
                return new DateTimeSeries(Source[range].As<long>(), this.DType.Clone());
            }
        }

        public new static DateTimeSeries Range(int count, DateTime start, DateTime end)
        {
            var delta = (end - start).ToDateTime64() / count;
            var r = new NdArray<long>(new Shape(count));
            for (int i = 0; i < r.Count(); i++)
                r.Value[i] = start.ToDateTime64() + delta * i;
            return new DateTimeSeries(new Series<long>(r));
        }

        public new static Series FromBuffer(byte[] buf, DType buftype, int iheight, int icol, Func<long, DateTime> getter, Func<DateTime, long> setter)
        {
            return new DateTimeSeries(Series<long>.FromBuffer(buf, buftype, iheight, icol) as Series<long>);
        }

    }

        public class Series<T> : BaseSeries<T>
    {
        public override NdArray<T> Values { get; set; }

        //public override NumCIL.Double.NdArray AsDouble {
        //	get {
        //		return new NumCIL.Double.NdArray (Values as NdArray<double>);
        //	}
        //}

        public Series(long count, DType dtype = null) : base(dtype)
        {
            Values = new NdArray<T>(new Shape(count));
        }

        public Series(NdArray<T> values, DType dtype = null) : base(dtype)
        {
            Values = values;
        }

        public Series(T[] data, DType dtype = null, string name = null) : this(new NdArray<T>(data), dtype)
        {
            if(!string.IsNullOrEmpty(name))
                Name = name;
        }

        public Series(IEnumerable<T> data, string name = null) : this(new NdArray<T>(data.ToArray()))
        {
            if (!string.IsNullOrEmpty(name))
                Name = name;
        }

        public static implicit operator Series<T>(NdArray<T> values)
        {
            return new Series<T>(values);
        }

        public static implicit operator Series<T>(T[] data)
        {
            return new Series<T>(new NdArray<T>(data));
        }

        public static implicit operator NdArray<T>(Series<T> series)
        {
            return series.Values;
        }

        public override int Count
        {
            get { return (int)Values.Shape.Dimensions[0].Length; }
            set
            {
                // clone underlying ndarray
                var resized = new NdArray<T>(new Shape(value));
                for (int i = 0; i < Math.Min(value, Count); i++)
                    resized.Value[i] = Values.Value[i];
                Values = resized;
            }
        }

        public override void Append(Series other)
        {
            int oldCount = this.Count;
            this.Count += other.Count;
            var other1 = other.As<T>();
            for (int i = 0; i < other.Count; i++)
                this.Values[oldCount + i] = other1.Values[i];
        }

        public override int IndexOf(T item, int startIndex = 0, int endIndex = -1, Location match = Location.EQ)
        {
            return (int)BinarySearch(item, Comparer<T>.Default, startIndex, endIndex, match);
        }

        public long BinarySearch(T item, IComparer<T> comparer, int startIndex = 0, int endIndex = -1, Location match = Location.EQ)
        {
            var first = 0L;
            var last = endIndex < 0 ? Values.Count() + endIndex : endIndex - 1;
            // we treat first<=item<=last
            while (last >= first)
            {
                long mid = (first + last) / 2;
                var vmid = Values.Value[mid];
                var cmp = comparer.Compare(item, vmid);
                if (cmp <= 0)
                    last = mid;
                if (cmp >= 0)
                    first = mid;
                if (last - first <= 1)
                {
                    var cfirst = comparer.Compare(Values.Value[first], item);
                    var clast = comparer.Compare(Values.Value[last], item);
                    switch (match)
                    {
                        case Location.EQ:
                            return cfirst == 0 ? first : (clast == 0 ? last : -1);
                        case Location.GE:
                            return cfirst >= 0 ? first : (clast >= 0 ? last : (last + 1 < Values.Count() ? last + 1 : -1));
                        case Location.LE:
                            return clast <= 0 ? last : (cfirst <= 0 ? first : (first - 1 >= 0 ? first - 1 : -1));
                        case Location.GT:
                            return cfirst > 0 ? first : (clast > 0 ? last : (last + 1 < Values.Count() ? last + 1 : -1));
                        case Location.LT:
                            return clast < 0 ? last : (cfirst < 0 ? first : (first - 1 >= 0 ? first - 1 : -1));
                        default: throw new NotImplementedException();
                    }
                }
            }
            return -1;
        }


        public override NArctic.Series Clone()
        {
            return Copy();
        }

        public override BaseSeries<T> Copy()
        {
            return new Series<T>(this.Values.Clone(), DType.Clone());
        }


        public override IEnumerator<T> GetEnumerator()
        {
            return Values.Value.GetEnumerator();
        }

        public int SliceIndex(int index)
        {
            if (index >= 0)
                return index;
            return (index + Count);
        }

        public override T this[int index]
        {
            get
            {
                return Values.Value[SliceIndex(index)];
            }
            set
            {
                Values.Value[SliceIndex(index)] = value;
            }
        }

        public override Series this[Range range]
        {
            get
            {
                return new Series<T>(range.Initialized ? Values[range] : new NdArray<T>(new Shape(0)), this.DType.Clone());
            }
        }

        public override T[] AsArray()
        {
            return Values.AsArray();
        }


        public override void ToBuffer(byte[] buf, DType buftype, int iheight, int icol)
        {
            T[] data = this.Values.AsArray();
            // hack here
            int ofs = (int)this.Values.Shape.Offset;
            buftype.FillBufferFromData(buf, data, ofs, iheight, icol);
        }

        public static Series FromBuffer(byte[] buf, DType buftype, int iheight, int icol)
        {
            var data = new T[iheight];
            buftype.FillDataFromBuffer(buf, data, iheight, icol);
            return new Series<T>(data, buftype.Fields[icol]);
        }

        public override string ToString()
        {
            return Values.ToString();
        }

        public override object At(int index)
        {
            return this[index];
        }

        public override void Set(int index, object value)
        {
            this[index] = (T)value;
        }
    }

    public class StringSeries : Series<string>
    {
        public StringSeries(long count, DType dtype = null) : base(count, dtype)
        {
        }

        public StringSeries(NdArray<string> values, DType dtype = null) : base(values, dtype)
        {
        }

        public StringSeries(IEnumerable<string> data, string name = null) : base(data, name)
        {
        }

        public StringSeries(string[] data, DType dtype = null, string name = null) : base(data, dtype, name)
        {
        }

        public new static Series FromBuffer(byte[] buf, DType buftype, int iheight, int icol)
        {
            var data = new string[iheight];
            var dtype = buftype.Fields[icol];
            buftype.FillDataFromBufferSlow(buf, data, iheight, icol, byteConverter: (b, i, c) => dtype.EncodingStyle.GetString(b, i, c));
            return new Series<string>(data, dtype);
        }
    }
}

