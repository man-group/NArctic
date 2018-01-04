using System;
using System.Collections;

namespace NArctic.Randoms
{
	public interface IRandomGenerator {
		double GetNext (Random rng = null);
	}

	public abstract class BaseRandomGenerator : IRandomGenerator
	{
		public static Random Random = new Random ((int)DateTime.UtcNow.Ticks);
		public abstract double GetNext (Random rng = null);

	}

	public class Uniform : BaseRandomGenerator 
	{
		public override double GetNext(Random rng = null)
		{
			rng = rng ?? Random;
			return rng.NextDouble();
		}
	}

	public class BoxMullerNormal : BaseRandomGenerator {

		private bool haveNextDeviate = false;
		private double nextDeviate;

		public static Tuple<double, double> Transform(double x, double y)
		{
			// pick a point in the unit disc
			double u = x;
			double t = 2.0 * Math.PI * y;

			double a = Math.Sqrt(-2.0 * Math.Log(u));

			// store one deviate
			return new Tuple<double,double>(a*Math.Sin(t), a* Math.Cos(t));
		}

		public override double GetNext (Random rng = null) {
			rng = rng ?? Random;
			if (haveNextDeviate) {
				haveNextDeviate = false;
				return (nextDeviate);
			} else {
				haveNextDeviate = true;
				var rtn =  Transform (rng.NextDouble (), rng.NextDouble ());
				nextDeviate = rtn.Item2;
				return rtn.Item1;
			}
		}
	}
}

