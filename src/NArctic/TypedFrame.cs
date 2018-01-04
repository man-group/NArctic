using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NArctic;
using Vexe.Runtime.Extensions;
using Utilities;
using System.Collections;

namespace NArctic
{
    public class RingFrame<T>:Ring where T :new()
    {
        public TypedFrame<T> TypedFrame;
        public DataFrame DataFrame {
            get { return TypedFrame.DataFrame;  }
        }

        public RingFrame(TypedFrame<T> tf) : base(()=>tf.DataFrame.Rows.Count)
        {
            this.TypedFrame = tf;
        }
        public int Enqueue(T value)
        {
            var r = base.Enqueue();
            if (r < 0)
                return -1;
            TypedFrame[r] = value;
            return r;
        }

        public bool Add(T value)
        {
            return Enqueue(value) >= 0;
        }

        public new T Dequeue()
        {
            int r = base.Dequeue();
            if (r < 0)
                throw new InvalidOperationException("Empty queue");
            return TypedFrame[r];
        }
    }

    public class TypedFrame<T>:IList<T> where T :new()
    {
        public static List<PropertyInfo> GetSettableProps(Type t)
        {
            return t
                  .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                  .Where(p => GetPropertySetter(p, t) != null)
                  .ToList();
        }

        public static List<FieldInfo> GetSettableFields(Type t)
        {
            return t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToList();
        }

        public static MethodInfo GetPropertySetter(PropertyInfo propertyInfo, Type type)
        {
            if (propertyInfo.DeclaringType == type) return propertyInfo.GetSetMethod(true);
#if COREFX
            return propertyInfo.DeclaringType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single(x => x.Name == propertyInfo.Name
                        && x.PropertyType == propertyInfo.PropertyType
                        && IsParameterMatch(x.GetIndexParameters(), propertyInfo.GetIndexParameters())
                        ).GetSetMethod(true);
#else
            return propertyInfo.DeclaringType.GetProperty(
                   propertyInfo.Name,
                   BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                   Type.DefaultBinder,
                   propertyInfo.PropertyType,
                   propertyInfo.GetIndexParameters().Select(p => p.ParameterType).ToArray(),
                   null).GetSetMethod(true);
#endif
        }

        public Dictionary<string, PropertyInfo> Props;
        public Action<T, int>[] Writers;
        public Action<T, int>[] Readers;
        public string[] Keys;
        public DataFrame DataFrame;
        public string Index;


        public TypedFrame(long count=0, string index=null, string[] keys=null)
        {
            Init(typeof(T),keys);
            Index = index;
            DataFrame = CreateDataFrame(count);
        }

        public TypedFrame(DataFrame df, string[] keys = null)
        {
            Init(typeof(T), keys);
            DataFrame = df;
        }

        private void Init(Type type, string[] keys)
        {
            Props = GetSettableProps(type).ToDictionary(x => x.Name);
            Keys = keys ?? Props.Keys.ToArray();
            Readers = new Action<T,int>[Keys.Length];
            Writers = new Action<T, int>[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
            {
                var pi = Props[Keys[i]];
                if (pi.PropertyType == typeof(double))
                    InitRW<double>(pi, i);
                else if (pi.PropertyType == typeof(long))
                    InitRW<long>(pi, i);
                else if (pi.PropertyType == typeof(int))
                    InitRW<int>(pi, i);
                else if (pi.PropertyType == typeof(DateTime))
                    InitRW<DateTime>(pi, i);

                else throw new InvalidOperationException("unsupported type {0}".Args(pi.PropertyType));
            }
        }

        private void InitRW<Q>(PropertyInfo pi, int i)
        {
            Writers[i] = (T obj, int row) => DataFrame[i].As<Q>()[row] = pi.DelegateForGet<T, Q>()(obj);
            Readers[i] = (T obj, int row) => pi.DelegateForSet<T, Q>()(ref obj, DataFrame[i].As<Q>()[row]);
        }


        public DataFrame CreateDataFrame(long count)
        {
            var types = Keys.Select(k => Props[k].PropertyType).ToArray();
            var df = new DataFrame(count, types, Keys, index:Index);
            if(this.Index!=null)
                df.Index = df.Columns[this.Index];
            DataFrame = df;
            return df;
        }

        public T Get(DataFrame df, int row)
        {
            T obj = new T();
            for(int i=0;i<Keys.Length;i++)
            {
                var pi = Props[Keys[i]];
                if (pi.PropertyType == typeof(double))
                    SetProperty(ref obj, pi,  df[i].As<double>()[row]);
                else if (pi.PropertyType == typeof(long))
                    SetProperty(ref obj, pi, df[i].As<long>()[row]);
                else if (pi.PropertyType == typeof(int))
                    SetProperty(ref obj, pi, df[i].As<int>()[row]);
                else if (pi.PropertyType == typeof(DateTime))
                    SetProperty(ref obj, pi, df[i].As<DateTime>()[row]);

                else throw new InvalidOperationException("unsupported type {0}".Args(pi.PropertyType));
            }
            return obj;
        }

        public Q GetProperty<Q>(T obj, PropertyInfo pi)
        {
            //return (Q) pi.GetValue(obj);
            return pi.DelegateForGet<T, Q>()(obj);
        }

        public void SetProperty<Q>(ref T obj, PropertyInfo pi, Q value)
        {
            //pi.SetValue(obj, value);
            pi.DelegateForSet<T, Q>()(ref obj, value);
        }

        public void Set(DataFrame df, int row, T obj)
        {
            for (int i = 0; i < Keys.Length; i++)
            {
                var pi = Props[Keys[i]];
                if (pi.PropertyType == typeof(double))
                    df[i].As<double>()[row] = GetProperty<double>(obj, pi);
                else if (pi.PropertyType == typeof(long))
                    df[i].As<long>()[row] = GetProperty<long>(obj, pi);
                else if (pi.PropertyType == typeof(int))
                    df[i].As<int>()[row] = GetProperty<int>(obj, pi);
                else if (pi.PropertyType == typeof(DateTime))
                    df[i].As<DateTime>()[row] = GetProperty<DateTime>(obj, pi);

                else throw new InvalidOperationException("unsupported type {0}".Args(pi.PropertyType));
            }
        }

        public T this[int row]
        {
            get {
                //return Get(DataFrame, row);
                T obj = new T();
                for (int i = 0; i < Keys.Length; i++)
                    Readers[i](obj, row);
                return obj;
            }
            set {
                //Set(DataFrame, row, value);
                for (int i = 0; i < Keys.Length; i++)
                    Writers[i](value, row);
            }
        }
        
        public int Count
        {
            get { return DataFrame.FilledCount; }
            set { DataFrame.FilledCount = value; }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public void Add(T t)
        {
            this.Count++;
            this[Count - 1] = t;
        }

        public RingFrame<T> ToRing()
        {
            return new RingFrame<T>(this);
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < this.Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}
