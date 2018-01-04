#region Copyright
/*
This file is part of Bohrium and copyright (c) 2012 the Bohrium
team <http://www.bh107.org>.

Bohrium is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as 
published by the Free Software Foundation, either version 3 
of the License, or (at your option) any later version.

Bohrium is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the 
GNU Lesser General Public License along with Bohrium. 

If not, see <http://www.gnu.org/licenses/>.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NumCIL.Generic;

namespace NumCIL.Unsafe
{
    internal static class Aggregate
    {
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.SByte Aggregate_Entry_SByte<C>(C op, NdArray<System.SByte> in1)
            where C : struct, IBinaryOp<System.SByte>
        {
            System.SByte result;

			if (Apply.UFunc_Aggregate_Entry_Typed<System.SByte, C>(op, in1, out result))
				return result;

#if DEBUG
			Console.WriteLine("Generic Aggregate method C for SByte, with op = {0}, T = {1}", op.GetType(), typeof(System.SByte));
#endif
            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.SByte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.SByte)op.Op(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.SByte)op.Op(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.SByte)op.Op(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Byte Aggregate_Entry_Byte<C>(C op, NdArray<System.Byte> in1)
            where C : struct, IBinaryOp<System.Byte>
        {
            System.Byte result;

			if (Apply.UFunc_Aggregate_Entry_Typed<System.Byte, C>(op, in1, out result))
				return result;

#if DEBUG
			Console.WriteLine("Generic Aggregate method C for Byte, with op = {0}, T = {1}", op.GetType(), typeof(System.Byte));
#endif
            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Byte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Byte)op.Op(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Byte)op.Op(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Byte)op.Op(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int16 Aggregate_Entry_Int16<C>(C op, NdArray<System.Int16> in1)
            where C : struct, IBinaryOp<System.Int16>
        {
            System.Int16 result;

			if (Apply.UFunc_Aggregate_Entry_Typed<System.Int16, C>(op, in1, out result))
				return result;

#if DEBUG
			Console.WriteLine("Generic Aggregate method C for Int16, with op = {0}, T = {1}", op.GetType(), typeof(System.Int16));
#endif
            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int16)op.Op(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int16)op.Op(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int16)op.Op(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt16 Aggregate_Entry_UInt16<C>(C op, NdArray<System.UInt16> in1)
            where C : struct, IBinaryOp<System.UInt16>
        {
            System.UInt16 result;

			if (Apply.UFunc_Aggregate_Entry_Typed<System.UInt16, C>(op, in1, out result))
				return result;

#if DEBUG
			Console.WriteLine("Generic Aggregate method C for UInt16, with op = {0}, T = {1}", op.GetType(), typeof(System.UInt16));
#endif
            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt16)op.Op(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt16)op.Op(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt16)op.Op(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int32 Aggregate_Entry_Int32<C>(C op, NdArray<System.Int32> in1)
            where C : struct, IBinaryOp<System.Int32>
        {
            System.Int32 result;

			if (Apply.UFunc_Aggregate_Entry_Typed<System.Int32, C>(op, in1, out result))
				return result;

#if DEBUG
			Console.WriteLine("Generic Aggregate method C for Int32, with op = {0}, T = {1}", op.GetType(), typeof(System.Int32));
#endif
            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int32)op.Op(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int32)op.Op(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int32)op.Op(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt32 Aggregate_Entry_UInt32<C>(C op, NdArray<System.UInt32> in1)
            where C : struct, IBinaryOp<System.UInt32>
        {
            System.UInt32 result;

			if (Apply.UFunc_Aggregate_Entry_Typed<System.UInt32, C>(op, in1, out result))
				return result;

#if DEBUG
			Console.WriteLine("Generic Aggregate method C for UInt32, with op = {0}, T = {1}", op.GetType(), typeof(System.UInt32));
#endif
            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt32)op.Op(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt32)op.Op(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt32)op.Op(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int64 Aggregate_Entry_Int64<C>(C op, NdArray<System.Int64> in1)
            where C : struct, IBinaryOp<System.Int64>
        {
            System.Int64 result;

			if (Apply.UFunc_Aggregate_Entry_Typed<System.Int64, C>(op, in1, out result))
				return result;

#if DEBUG
			Console.WriteLine("Generic Aggregate method C for Int64, with op = {0}, T = {1}", op.GetType(), typeof(System.Int64));
#endif
            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int64)op.Op(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int64)op.Op(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int64)op.Op(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt64 Aggregate_Entry_UInt64<C>(C op, NdArray<System.UInt64> in1)
            where C : struct, IBinaryOp<System.UInt64>
        {
            System.UInt64 result;

			if (Apply.UFunc_Aggregate_Entry_Typed<System.UInt64, C>(op, in1, out result))
				return result;

#if DEBUG
			Console.WriteLine("Generic Aggregate method C for UInt64, with op = {0}, T = {1}", op.GetType(), typeof(System.UInt64));
#endif
            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt64)op.Op(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt64)op.Op(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt64)op.Op(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Single Aggregate_Entry_Single<C>(C op, NdArray<System.Single> in1)
            where C : struct, IBinaryOp<System.Single>
        {
            System.Single result;

			if (Apply.UFunc_Aggregate_Entry_Typed<System.Single, C>(op, in1, out result))
				return result;

#if DEBUG
			Console.WriteLine("Generic Aggregate method C for Single, with op = {0}, T = {1}", op.GetType(), typeof(System.Single));
#endif
            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Single*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Single)op.Op(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Single)op.Op(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Single)op.Op(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Double Aggregate_Entry_Double<C>(C op, NdArray<System.Double> in1)
            where C : struct, IBinaryOp<System.Double>
        {
            System.Double result;

			if (Apply.UFunc_Aggregate_Entry_Typed<System.Double, C>(op, in1, out result))
				return result;

#if DEBUG
			Console.WriteLine("Generic Aggregate method C for Double, with op = {0}, T = {1}", op.GetType(), typeof(System.Double));
#endif
            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Double*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Double)op.Op(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Double)op.Op(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Double)op.Op(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.SByte Aggregate_Entry_SByte_TypedImpl<C>(NumCIL.Int8.Add op, NdArray<System.SByte> in1)
            where C : struct, IBinaryOp<System.SByte>
        {
            System.SByte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.SByte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.SByte)(result + d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.SByte)(result + d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.SByte)(result + d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Byte Aggregate_Entry_Byte_TypedImpl<C>(NumCIL.UInt8.Add op, NdArray<System.Byte> in1)
            where C : struct, IBinaryOp<System.Byte>
        {
            System.Byte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Byte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Byte)(result + d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Byte)(result + d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Byte)(result + d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int16 Aggregate_Entry_Int16_TypedImpl<C>(NumCIL.Int16.Add op, NdArray<System.Int16> in1)
            where C : struct, IBinaryOp<System.Int16>
        {
            System.Int16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int16)(result + d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int16)(result + d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int16)(result + d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt16 Aggregate_Entry_UInt16_TypedImpl<C>(NumCIL.UInt16.Add op, NdArray<System.UInt16> in1)
            where C : struct, IBinaryOp<System.UInt16>
        {
            System.UInt16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt16)(result + d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt16)(result + d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt16)(result + d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int32 Aggregate_Entry_Int32_TypedImpl<C>(NumCIL.Int32.Add op, NdArray<System.Int32> in1)
            where C : struct, IBinaryOp<System.Int32>
        {
            System.Int32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int32)(result + d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int32)(result + d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int32)(result + d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt32 Aggregate_Entry_UInt32_TypedImpl<C>(NumCIL.UInt32.Add op, NdArray<System.UInt32> in1)
            where C : struct, IBinaryOp<System.UInt32>
        {
            System.UInt32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt32)(result + d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt32)(result + d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt32)(result + d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int64 Aggregate_Entry_Int64_TypedImpl<C>(NumCIL.Int64.Add op, NdArray<System.Int64> in1)
            where C : struct, IBinaryOp<System.Int64>
        {
            System.Int64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int64)(result + d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int64)(result + d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int64)(result + d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt64 Aggregate_Entry_UInt64_TypedImpl<C>(NumCIL.UInt64.Add op, NdArray<System.UInt64> in1)
            where C : struct, IBinaryOp<System.UInt64>
        {
            System.UInt64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt64)(result + d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt64)(result + d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt64)(result + d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Single Aggregate_Entry_Single_TypedImpl<C>(NumCIL.Float.Add op, NdArray<System.Single> in1)
            where C : struct, IBinaryOp<System.Single>
        {
            System.Single result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Single*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Single)(result + d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Single)(result + d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Single)(result + d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Double Aggregate_Entry_Double_TypedImpl<C>(NumCIL.Double.Add op, NdArray<System.Double> in1)
            where C : struct, IBinaryOp<System.Double>
        {
            System.Double result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Double*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Double)(result + d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Double)(result + d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Double)(result + d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.SByte Aggregate_Entry_SByte_TypedImpl<C>(NumCIL.Int8.Sub op, NdArray<System.SByte> in1)
            where C : struct, IBinaryOp<System.SByte>
        {
            System.SByte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.SByte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.SByte)(result - d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.SByte)(result - d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.SByte)(result - d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Byte Aggregate_Entry_Byte_TypedImpl<C>(NumCIL.UInt8.Sub op, NdArray<System.Byte> in1)
            where C : struct, IBinaryOp<System.Byte>
        {
            System.Byte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Byte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Byte)(result - d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Byte)(result - d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Byte)(result - d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int16 Aggregate_Entry_Int16_TypedImpl<C>(NumCIL.Int16.Sub op, NdArray<System.Int16> in1)
            where C : struct, IBinaryOp<System.Int16>
        {
            System.Int16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int16)(result - d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int16)(result - d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int16)(result - d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt16 Aggregate_Entry_UInt16_TypedImpl<C>(NumCIL.UInt16.Sub op, NdArray<System.UInt16> in1)
            where C : struct, IBinaryOp<System.UInt16>
        {
            System.UInt16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt16)(result - d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt16)(result - d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt16)(result - d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int32 Aggregate_Entry_Int32_TypedImpl<C>(NumCIL.Int32.Sub op, NdArray<System.Int32> in1)
            where C : struct, IBinaryOp<System.Int32>
        {
            System.Int32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int32)(result - d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int32)(result - d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int32)(result - d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt32 Aggregate_Entry_UInt32_TypedImpl<C>(NumCIL.UInt32.Sub op, NdArray<System.UInt32> in1)
            where C : struct, IBinaryOp<System.UInt32>
        {
            System.UInt32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt32)(result - d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt32)(result - d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt32)(result - d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int64 Aggregate_Entry_Int64_TypedImpl<C>(NumCIL.Int64.Sub op, NdArray<System.Int64> in1)
            where C : struct, IBinaryOp<System.Int64>
        {
            System.Int64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int64)(result - d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int64)(result - d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int64)(result - d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt64 Aggregate_Entry_UInt64_TypedImpl<C>(NumCIL.UInt64.Sub op, NdArray<System.UInt64> in1)
            where C : struct, IBinaryOp<System.UInt64>
        {
            System.UInt64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt64)(result - d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt64)(result - d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt64)(result - d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Single Aggregate_Entry_Single_TypedImpl<C>(NumCIL.Float.Sub op, NdArray<System.Single> in1)
            where C : struct, IBinaryOp<System.Single>
        {
            System.Single result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Single*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Single)(result - d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Single)(result - d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Single)(result - d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Double Aggregate_Entry_Double_TypedImpl<C>(NumCIL.Double.Sub op, NdArray<System.Double> in1)
            where C : struct, IBinaryOp<System.Double>
        {
            System.Double result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Double*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Double)(result - d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Double)(result - d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Double)(result - d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.SByte Aggregate_Entry_SByte_TypedImpl<C>(NumCIL.Int8.Mul op, NdArray<System.SByte> in1)
            where C : struct, IBinaryOp<System.SByte>
        {
            System.SByte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.SByte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.SByte)(result * d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.SByte)(result * d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.SByte)(result * d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Byte Aggregate_Entry_Byte_TypedImpl<C>(NumCIL.UInt8.Mul op, NdArray<System.Byte> in1)
            where C : struct, IBinaryOp<System.Byte>
        {
            System.Byte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Byte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Byte)(result * d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Byte)(result * d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Byte)(result * d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int16 Aggregate_Entry_Int16_TypedImpl<C>(NumCIL.Int16.Mul op, NdArray<System.Int16> in1)
            where C : struct, IBinaryOp<System.Int16>
        {
            System.Int16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int16)(result * d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int16)(result * d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int16)(result * d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt16 Aggregate_Entry_UInt16_TypedImpl<C>(NumCIL.UInt16.Mul op, NdArray<System.UInt16> in1)
            where C : struct, IBinaryOp<System.UInt16>
        {
            System.UInt16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt16)(result * d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt16)(result * d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt16)(result * d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int32 Aggregate_Entry_Int32_TypedImpl<C>(NumCIL.Int32.Mul op, NdArray<System.Int32> in1)
            where C : struct, IBinaryOp<System.Int32>
        {
            System.Int32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int32)(result * d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int32)(result * d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int32)(result * d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt32 Aggregate_Entry_UInt32_TypedImpl<C>(NumCIL.UInt32.Mul op, NdArray<System.UInt32> in1)
            where C : struct, IBinaryOp<System.UInt32>
        {
            System.UInt32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt32)(result * d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt32)(result * d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt32)(result * d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int64 Aggregate_Entry_Int64_TypedImpl<C>(NumCIL.Int64.Mul op, NdArray<System.Int64> in1)
            where C : struct, IBinaryOp<System.Int64>
        {
            System.Int64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int64)(result * d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int64)(result * d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int64)(result * d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt64 Aggregate_Entry_UInt64_TypedImpl<C>(NumCIL.UInt64.Mul op, NdArray<System.UInt64> in1)
            where C : struct, IBinaryOp<System.UInt64>
        {
            System.UInt64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt64)(result * d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt64)(result * d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt64)(result * d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Single Aggregate_Entry_Single_TypedImpl<C>(NumCIL.Float.Mul op, NdArray<System.Single> in1)
            where C : struct, IBinaryOp<System.Single>
        {
            System.Single result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Single*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Single)(result * d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Single)(result * d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Single)(result * d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Double Aggregate_Entry_Double_TypedImpl<C>(NumCIL.Double.Mul op, NdArray<System.Double> in1)
            where C : struct, IBinaryOp<System.Double>
        {
            System.Double result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Double*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Double)(result * d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Double)(result * d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Double)(result * d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.SByte Aggregate_Entry_SByte_TypedImpl<C>(NumCIL.Int8.Div op, NdArray<System.SByte> in1)
            where C : struct, IBinaryOp<System.SByte>
        {
            System.SByte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.SByte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.SByte)(result / d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.SByte)(result / d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.SByte)(result / d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Byte Aggregate_Entry_Byte_TypedImpl<C>(NumCIL.UInt8.Div op, NdArray<System.Byte> in1)
            where C : struct, IBinaryOp<System.Byte>
        {
            System.Byte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Byte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Byte)(result / d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Byte)(result / d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Byte)(result / d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int16 Aggregate_Entry_Int16_TypedImpl<C>(NumCIL.Int16.Div op, NdArray<System.Int16> in1)
            where C : struct, IBinaryOp<System.Int16>
        {
            System.Int16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int16)(result / d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int16)(result / d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int16)(result / d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt16 Aggregate_Entry_UInt16_TypedImpl<C>(NumCIL.UInt16.Div op, NdArray<System.UInt16> in1)
            where C : struct, IBinaryOp<System.UInt16>
        {
            System.UInt16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt16)(result / d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt16)(result / d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt16)(result / d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int32 Aggregate_Entry_Int32_TypedImpl<C>(NumCIL.Int32.Div op, NdArray<System.Int32> in1)
            where C : struct, IBinaryOp<System.Int32>
        {
            System.Int32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int32)(result / d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int32)(result / d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int32)(result / d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt32 Aggregate_Entry_UInt32_TypedImpl<C>(NumCIL.UInt32.Div op, NdArray<System.UInt32> in1)
            where C : struct, IBinaryOp<System.UInt32>
        {
            System.UInt32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt32)(result / d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt32)(result / d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt32)(result / d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int64 Aggregate_Entry_Int64_TypedImpl<C>(NumCIL.Int64.Div op, NdArray<System.Int64> in1)
            where C : struct, IBinaryOp<System.Int64>
        {
            System.Int64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int64)(result / d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int64)(result / d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int64)(result / d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt64 Aggregate_Entry_UInt64_TypedImpl<C>(NumCIL.UInt64.Div op, NdArray<System.UInt64> in1)
            where C : struct, IBinaryOp<System.UInt64>
        {
            System.UInt64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt64)(result / d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt64)(result / d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt64)(result / d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Single Aggregate_Entry_Single_TypedImpl<C>(NumCIL.Float.Div op, NdArray<System.Single> in1)
            where C : struct, IBinaryOp<System.Single>
        {
            System.Single result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Single*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Single)(result / d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Single)(result / d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Single)(result / d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Double Aggregate_Entry_Double_TypedImpl<C>(NumCIL.Double.Div op, NdArray<System.Double> in1)
            where C : struct, IBinaryOp<System.Double>
        {
            System.Double result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Double*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Double)(result / d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Double)(result / d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Double)(result / d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.SByte Aggregate_Entry_SByte_TypedImpl<C>(NumCIL.Int8.Mod op, NdArray<System.SByte> in1)
            where C : struct, IBinaryOp<System.SByte>
        {
            System.SByte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.SByte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.SByte)(result % d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.SByte)(result % d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.SByte)(result % d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Byte Aggregate_Entry_Byte_TypedImpl<C>(NumCIL.UInt8.Mod op, NdArray<System.Byte> in1)
            where C : struct, IBinaryOp<System.Byte>
        {
            System.Byte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Byte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Byte)(result % d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Byte)(result % d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Byte)(result % d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int16 Aggregate_Entry_Int16_TypedImpl<C>(NumCIL.Int16.Mod op, NdArray<System.Int16> in1)
            where C : struct, IBinaryOp<System.Int16>
        {
            System.Int16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int16)(result % d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int16)(result % d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int16)(result % d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt16 Aggregate_Entry_UInt16_TypedImpl<C>(NumCIL.UInt16.Mod op, NdArray<System.UInt16> in1)
            where C : struct, IBinaryOp<System.UInt16>
        {
            System.UInt16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt16)(result % d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt16)(result % d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt16)(result % d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int32 Aggregate_Entry_Int32_TypedImpl<C>(NumCIL.Int32.Mod op, NdArray<System.Int32> in1)
            where C : struct, IBinaryOp<System.Int32>
        {
            System.Int32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int32)(result % d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int32)(result % d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int32)(result % d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt32 Aggregate_Entry_UInt32_TypedImpl<C>(NumCIL.UInt32.Mod op, NdArray<System.UInt32> in1)
            where C : struct, IBinaryOp<System.UInt32>
        {
            System.UInt32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt32)(result % d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt32)(result % d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt32)(result % d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int64 Aggregate_Entry_Int64_TypedImpl<C>(NumCIL.Int64.Mod op, NdArray<System.Int64> in1)
            where C : struct, IBinaryOp<System.Int64>
        {
            System.Int64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int64)(result % d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int64)(result % d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int64)(result % d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt64 Aggregate_Entry_UInt64_TypedImpl<C>(NumCIL.UInt64.Mod op, NdArray<System.UInt64> in1)
            where C : struct, IBinaryOp<System.UInt64>
        {
            System.UInt64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt64)(result % d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt64)(result % d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt64)(result % d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Single Aggregate_Entry_Single_TypedImpl<C>(NumCIL.Float.Mod op, NdArray<System.Single> in1)
            where C : struct, IBinaryOp<System.Single>
        {
            System.Single result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Single*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Single)(result % d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Single)(result % d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Single)(result % d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Double Aggregate_Entry_Double_TypedImpl<C>(NumCIL.Double.Mod op, NdArray<System.Double> in1)
            where C : struct, IBinaryOp<System.Double>
        {
            System.Double result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Double*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Double)(result % d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Double)(result % d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Double)(result % d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.SByte Aggregate_Entry_SByte_TypedImpl<C>(NumCIL.Int8.Min op, NdArray<System.SByte> in1)
            where C : struct, IBinaryOp<System.SByte>
        {
            System.SByte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.SByte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.SByte)Math.Min(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.SByte)Math.Min(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.SByte)Math.Min(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Byte Aggregate_Entry_Byte_TypedImpl<C>(NumCIL.UInt8.Min op, NdArray<System.Byte> in1)
            where C : struct, IBinaryOp<System.Byte>
        {
            System.Byte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Byte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Byte)Math.Min(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Byte)Math.Min(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Byte)Math.Min(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int16 Aggregate_Entry_Int16_TypedImpl<C>(NumCIL.Int16.Min op, NdArray<System.Int16> in1)
            where C : struct, IBinaryOp<System.Int16>
        {
            System.Int16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int16)Math.Min(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int16)Math.Min(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int16)Math.Min(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt16 Aggregate_Entry_UInt16_TypedImpl<C>(NumCIL.UInt16.Min op, NdArray<System.UInt16> in1)
            where C : struct, IBinaryOp<System.UInt16>
        {
            System.UInt16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt16)Math.Min(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt16)Math.Min(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt16)Math.Min(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int32 Aggregate_Entry_Int32_TypedImpl<C>(NumCIL.Int32.Min op, NdArray<System.Int32> in1)
            where C : struct, IBinaryOp<System.Int32>
        {
            System.Int32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int32)Math.Min(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int32)Math.Min(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int32)Math.Min(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt32 Aggregate_Entry_UInt32_TypedImpl<C>(NumCIL.UInt32.Min op, NdArray<System.UInt32> in1)
            where C : struct, IBinaryOp<System.UInt32>
        {
            System.UInt32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt32)Math.Min(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt32)Math.Min(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt32)Math.Min(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int64 Aggregate_Entry_Int64_TypedImpl<C>(NumCIL.Int64.Min op, NdArray<System.Int64> in1)
            where C : struct, IBinaryOp<System.Int64>
        {
            System.Int64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int64)Math.Min(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int64)Math.Min(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int64)Math.Min(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt64 Aggregate_Entry_UInt64_TypedImpl<C>(NumCIL.UInt64.Min op, NdArray<System.UInt64> in1)
            where C : struct, IBinaryOp<System.UInt64>
        {
            System.UInt64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt64)Math.Min(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt64)Math.Min(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt64)Math.Min(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Single Aggregate_Entry_Single_TypedImpl<C>(NumCIL.Float.Min op, NdArray<System.Single> in1)
            where C : struct, IBinaryOp<System.Single>
        {
            System.Single result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Single*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Single)Math.Min(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Single)Math.Min(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Single)Math.Min(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Double Aggregate_Entry_Double_TypedImpl<C>(NumCIL.Double.Min op, NdArray<System.Double> in1)
            where C : struct, IBinaryOp<System.Double>
        {
            System.Double result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Double*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Double)Math.Min(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Double)Math.Min(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Double)Math.Min(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.SByte Aggregate_Entry_SByte_TypedImpl<C>(NumCIL.Int8.Max op, NdArray<System.SByte> in1)
            where C : struct, IBinaryOp<System.SByte>
        {
            System.SByte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.SByte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.SByte)Math.Max(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.SByte)Math.Max(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.SByte)Math.Max(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Byte Aggregate_Entry_Byte_TypedImpl<C>(NumCIL.UInt8.Max op, NdArray<System.Byte> in1)
            where C : struct, IBinaryOp<System.Byte>
        {
            System.Byte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Byte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Byte)Math.Max(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Byte)Math.Max(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Byte)Math.Max(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int16 Aggregate_Entry_Int16_TypedImpl<C>(NumCIL.Int16.Max op, NdArray<System.Int16> in1)
            where C : struct, IBinaryOp<System.Int16>
        {
            System.Int16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int16)Math.Max(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int16)Math.Max(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int16)Math.Max(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt16 Aggregate_Entry_UInt16_TypedImpl<C>(NumCIL.UInt16.Max op, NdArray<System.UInt16> in1)
            where C : struct, IBinaryOp<System.UInt16>
        {
            System.UInt16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt16)Math.Max(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt16)Math.Max(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt16)Math.Max(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int32 Aggregate_Entry_Int32_TypedImpl<C>(NumCIL.Int32.Max op, NdArray<System.Int32> in1)
            where C : struct, IBinaryOp<System.Int32>
        {
            System.Int32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int32)Math.Max(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int32)Math.Max(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int32)Math.Max(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt32 Aggregate_Entry_UInt32_TypedImpl<C>(NumCIL.UInt32.Max op, NdArray<System.UInt32> in1)
            where C : struct, IBinaryOp<System.UInt32>
        {
            System.UInt32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt32)Math.Max(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt32)Math.Max(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt32)Math.Max(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int64 Aggregate_Entry_Int64_TypedImpl<C>(NumCIL.Int64.Max op, NdArray<System.Int64> in1)
            where C : struct, IBinaryOp<System.Int64>
        {
            System.Int64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int64)Math.Max(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int64)Math.Max(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int64)Math.Max(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt64 Aggregate_Entry_UInt64_TypedImpl<C>(NumCIL.UInt64.Max op, NdArray<System.UInt64> in1)
            where C : struct, IBinaryOp<System.UInt64>
        {
            System.UInt64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt64)Math.Max(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt64)Math.Max(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt64)Math.Max(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Single Aggregate_Entry_Single_TypedImpl<C>(NumCIL.Float.Max op, NdArray<System.Single> in1)
            where C : struct, IBinaryOp<System.Single>
        {
            System.Single result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Single*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Single)Math.Max(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Single)Math.Max(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Single)Math.Max(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Double Aggregate_Entry_Double_TypedImpl<C>(NumCIL.Double.Max op, NdArray<System.Double> in1)
            where C : struct, IBinaryOp<System.Double>
        {
            System.Double result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Double*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Double)Math.Max(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Double)Math.Max(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Double)Math.Max(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.SByte Aggregate_Entry_SByte_TypedImpl<C>(NumCIL.Int8.Pow op, NdArray<System.SByte> in1)
            where C : struct, IBinaryOp<System.SByte>
        {
            System.SByte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.SByte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.SByte)Math.Pow(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.SByte)Math.Pow(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.SByte)Math.Pow(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Byte Aggregate_Entry_Byte_TypedImpl<C>(NumCIL.UInt8.Pow op, NdArray<System.Byte> in1)
            where C : struct, IBinaryOp<System.Byte>
        {
            System.Byte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Byte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Byte)Math.Pow(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Byte)Math.Pow(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Byte)Math.Pow(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int16 Aggregate_Entry_Int16_TypedImpl<C>(NumCIL.Int16.Pow op, NdArray<System.Int16> in1)
            where C : struct, IBinaryOp<System.Int16>
        {
            System.Int16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int16)Math.Pow(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int16)Math.Pow(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int16)Math.Pow(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt16 Aggregate_Entry_UInt16_TypedImpl<C>(NumCIL.UInt16.Pow op, NdArray<System.UInt16> in1)
            where C : struct, IBinaryOp<System.UInt16>
        {
            System.UInt16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt16)Math.Pow(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt16)Math.Pow(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt16)Math.Pow(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int32 Aggregate_Entry_Int32_TypedImpl<C>(NumCIL.Int32.Pow op, NdArray<System.Int32> in1)
            where C : struct, IBinaryOp<System.Int32>
        {
            System.Int32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int32)Math.Pow(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int32)Math.Pow(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int32)Math.Pow(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt32 Aggregate_Entry_UInt32_TypedImpl<C>(NumCIL.UInt32.Pow op, NdArray<System.UInt32> in1)
            where C : struct, IBinaryOp<System.UInt32>
        {
            System.UInt32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt32)Math.Pow(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt32)Math.Pow(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt32)Math.Pow(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int64 Aggregate_Entry_Int64_TypedImpl<C>(NumCIL.Int64.Pow op, NdArray<System.Int64> in1)
            where C : struct, IBinaryOp<System.Int64>
        {
            System.Int64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int64)Math.Pow(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int64)Math.Pow(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int64)Math.Pow(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt64 Aggregate_Entry_UInt64_TypedImpl<C>(NumCIL.UInt64.Pow op, NdArray<System.UInt64> in1)
            where C : struct, IBinaryOp<System.UInt64>
        {
            System.UInt64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt64)Math.Pow(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt64)Math.Pow(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt64)Math.Pow(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Single Aggregate_Entry_Single_TypedImpl<C>(NumCIL.Float.Pow op, NdArray<System.Single> in1)
            where C : struct, IBinaryOp<System.Single>
        {
            System.Single result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Single*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Single)Math.Pow(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Single)Math.Pow(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Single)Math.Pow(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Double Aggregate_Entry_Double_TypedImpl<C>(NumCIL.Double.Pow op, NdArray<System.Double> in1)
            where C : struct, IBinaryOp<System.Double>
        {
            System.Double result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Double*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Double)Math.Pow(result, d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Double)Math.Pow(result, d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Double)Math.Pow(result, d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.SByte Aggregate_Entry_SByte_TypedImpl<C>(NumCIL.Int8.And op, NdArray<System.SByte> in1)
            where C : struct, IBinaryOp<System.SByte>
        {
            System.SByte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.SByte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.SByte)(result & d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.SByte)(result & d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.SByte)(result & d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Byte Aggregate_Entry_Byte_TypedImpl<C>(NumCIL.UInt8.And op, NdArray<System.Byte> in1)
            where C : struct, IBinaryOp<System.Byte>
        {
            System.Byte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Byte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Byte)(result & d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Byte)(result & d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Byte)(result & d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int16 Aggregate_Entry_Int16_TypedImpl<C>(NumCIL.Int16.And op, NdArray<System.Int16> in1)
            where C : struct, IBinaryOp<System.Int16>
        {
            System.Int16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int16)(result & d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int16)(result & d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int16)(result & d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt16 Aggregate_Entry_UInt16_TypedImpl<C>(NumCIL.UInt16.And op, NdArray<System.UInt16> in1)
            where C : struct, IBinaryOp<System.UInt16>
        {
            System.UInt16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt16)(result & d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt16)(result & d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt16)(result & d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int32 Aggregate_Entry_Int32_TypedImpl<C>(NumCIL.Int32.And op, NdArray<System.Int32> in1)
            where C : struct, IBinaryOp<System.Int32>
        {
            System.Int32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int32)(result & d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int32)(result & d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int32)(result & d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt32 Aggregate_Entry_UInt32_TypedImpl<C>(NumCIL.UInt32.And op, NdArray<System.UInt32> in1)
            where C : struct, IBinaryOp<System.UInt32>
        {
            System.UInt32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt32)(result & d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt32)(result & d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt32)(result & d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int64 Aggregate_Entry_Int64_TypedImpl<C>(NumCIL.Int64.And op, NdArray<System.Int64> in1)
            where C : struct, IBinaryOp<System.Int64>
        {
            System.Int64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int64)(result & d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int64)(result & d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int64)(result & d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt64 Aggregate_Entry_UInt64_TypedImpl<C>(NumCIL.UInt64.And op, NdArray<System.UInt64> in1)
            where C : struct, IBinaryOp<System.UInt64>
        {
            System.UInt64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt64)(result & d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt64)(result & d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt64)(result & d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Boolean Aggregate_Entry_Boolean_TypedImpl<C>(NumCIL.Boolean.And op, NdArray<System.Boolean> in1)
            where C : struct, IBinaryOp<System.Boolean>
        {
            System.Boolean result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Boolean*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Boolean)(result & d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Boolean)(result & d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Boolean)(result & d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.SByte Aggregate_Entry_SByte_TypedImpl<C>(NumCIL.Int8.Or op, NdArray<System.SByte> in1)
            where C : struct, IBinaryOp<System.SByte>
        {
            System.SByte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.SByte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.SByte)(result | d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.SByte)(result | d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.SByte)(result | d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Byte Aggregate_Entry_Byte_TypedImpl<C>(NumCIL.UInt8.Or op, NdArray<System.Byte> in1)
            where C : struct, IBinaryOp<System.Byte>
        {
            System.Byte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Byte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Byte)(result | d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Byte)(result | d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Byte)(result | d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int16 Aggregate_Entry_Int16_TypedImpl<C>(NumCIL.Int16.Or op, NdArray<System.Int16> in1)
            where C : struct, IBinaryOp<System.Int16>
        {
            System.Int16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int16)(result | d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int16)(result | d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int16)(result | d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt16 Aggregate_Entry_UInt16_TypedImpl<C>(NumCIL.UInt16.Or op, NdArray<System.UInt16> in1)
            where C : struct, IBinaryOp<System.UInt16>
        {
            System.UInt16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt16)(result | d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt16)(result | d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt16)(result | d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int32 Aggregate_Entry_Int32_TypedImpl<C>(NumCIL.Int32.Or op, NdArray<System.Int32> in1)
            where C : struct, IBinaryOp<System.Int32>
        {
            System.Int32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int32)(result | d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int32)(result | d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int32)(result | d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt32 Aggregate_Entry_UInt32_TypedImpl<C>(NumCIL.UInt32.Or op, NdArray<System.UInt32> in1)
            where C : struct, IBinaryOp<System.UInt32>
        {
            System.UInt32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt32)(result | d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt32)(result | d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt32)(result | d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int64 Aggregate_Entry_Int64_TypedImpl<C>(NumCIL.Int64.Or op, NdArray<System.Int64> in1)
            where C : struct, IBinaryOp<System.Int64>
        {
            System.Int64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int64)(result | d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int64)(result | d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int64)(result | d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt64 Aggregate_Entry_UInt64_TypedImpl<C>(NumCIL.UInt64.Or op, NdArray<System.UInt64> in1)
            where C : struct, IBinaryOp<System.UInt64>
        {
            System.UInt64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt64)(result | d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt64)(result | d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt64)(result | d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Boolean Aggregate_Entry_Boolean_TypedImpl<C>(NumCIL.Boolean.Or op, NdArray<System.Boolean> in1)
            where C : struct, IBinaryOp<System.Boolean>
        {
            System.Boolean result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Boolean*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Boolean)(result | d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Boolean)(result | d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Boolean)(result | d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.SByte Aggregate_Entry_SByte_TypedImpl<C>(NumCIL.Int8.Xor op, NdArray<System.SByte> in1)
            where C : struct, IBinaryOp<System.SByte>
        {
            System.SByte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.SByte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.SByte)(result ^ d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.SByte)(result ^ d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.SByte)(result ^ d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Byte Aggregate_Entry_Byte_TypedImpl<C>(NumCIL.UInt8.Xor op, NdArray<System.Byte> in1)
            where C : struct, IBinaryOp<System.Byte>
        {
            System.Byte result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Byte*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Byte)(result ^ d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Byte)(result ^ d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Byte)(result ^ d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int16 Aggregate_Entry_Int16_TypedImpl<C>(NumCIL.Int16.Xor op, NdArray<System.Int16> in1)
            where C : struct, IBinaryOp<System.Int16>
        {
            System.Int16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int16)(result ^ d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int16)(result ^ d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int16)(result ^ d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt16 Aggregate_Entry_UInt16_TypedImpl<C>(NumCIL.UInt16.Xor op, NdArray<System.UInt16> in1)
            where C : struct, IBinaryOp<System.UInt16>
        {
            System.UInt16 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt16*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt16)(result ^ d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt16)(result ^ d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt16)(result ^ d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int32 Aggregate_Entry_Int32_TypedImpl<C>(NumCIL.Int32.Xor op, NdArray<System.Int32> in1)
            where C : struct, IBinaryOp<System.Int32>
        {
            System.Int32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int32)(result ^ d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int32)(result ^ d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int32)(result ^ d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt32 Aggregate_Entry_UInt32_TypedImpl<C>(NumCIL.UInt32.Xor op, NdArray<System.UInt32> in1)
            where C : struct, IBinaryOp<System.UInt32>
        {
            System.UInt32 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt32*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt32)(result ^ d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt32)(result ^ d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt32)(result ^ d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Int64 Aggregate_Entry_Int64_TypedImpl<C>(NumCIL.Int64.Xor op, NdArray<System.Int64> in1)
            where C : struct, IBinaryOp<System.Int64>
        {
            System.Int64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Int64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Int64)(result ^ d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Int64)(result ^ d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Int64)(result ^ d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.UInt64 Aggregate_Entry_UInt64_TypedImpl<C>(NumCIL.UInt64.Xor op, NdArray<System.UInt64> in1)
            where C : struct, IBinaryOp<System.UInt64>
        {
            System.UInt64 result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.UInt64*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.UInt64)(result ^ d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.UInt64)(result ^ d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.UInt64)(result ^ d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the scalar result of applying the binary operation to all elements
        /// </summary>
        /// <typeparam name="C">The operation to perform</typeparam>
        /// <param name="op">The operation to perform</param>
        /// <param name="in1">The array to aggregate</param>
        /// <returns>A scalar value that is the result of aggregating all elements</returns>
        private static System.Boolean Aggregate_Entry_Boolean_TypedImpl<C>(NumCIL.Boolean.Xor op, NdArray<System.Boolean> in1)
            where C : struct, IBinaryOp<System.Boolean>
        {
            System.Boolean result;

            unsafe
            {
                using (var f1 = new Pinner(in1.DataAccessor))
                {
                    var d1 = (System.Boolean*)f1.ptr;

                    if (in1.Shape.Dimensions.Length == 1)
                    {
                        long totalOps = in1.Shape.Dimensions[0].Length;
                        long ix1 = in1.Shape.Offset;
                        long stride1 = in1.Shape.Dimensions[0].Stride;

                        result = d1[ix1];
                        ix1 += stride1;

                        for (long i = 1; i < totalOps; i++)
                        {
                            result = (System.Boolean)(result ^ d1[ix1]);
                            ix1 += stride1;
                        }
                    }
                    else if (in1.Shape.Dimensions.Length == 2)
                    {
                        long opsOuter = in1.Shape.Dimensions[0].Length;
                        long opsInner = in1.Shape.Dimensions[1].Length;

                        long ix1 = in1.Shape.Offset;
                        long outerStride1 = in1.Shape.Dimensions[0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[1].Stride;
                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[1].Length;

                        result = d1[ix1];
                        ix1 += innerStride1;

                        for (long i = 0; i < opsOuter; i++)
                        {
                            for (long j = (i == 0 ? 1 : 0); j < opsInner; j++)
                            {
                                result = (System.Boolean)(result ^ d1[ix1]);
                                ix1 += innerStride1;
                            }

                            ix1 += outerStride1;
                        }
                    }
                    else
                    {
                        long n = in1.Shape.Dimensions.LongLength - 3;
                        long[] limits = in1.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                        long[] counters = new long[limits.LongLength];

                        long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                        //This chunck of variables are used to prevent repeated calculations of offsets
                        long dimIndex0 = 0 + limits.LongLength;
                        long dimIndex1 = 1 + limits.LongLength;
                        long dimIndex2 = 2 + limits.LongLength;

                        long opsOuter = in1.Shape.Dimensions[0 + limits.LongLength].Length;
                        long opsInner = in1.Shape.Dimensions[1 + limits.LongLength].Length;
                        long opsInnerInner = in1.Shape.Dimensions[2 + limits.LongLength].Length;

                        long outerStride1 = in1.Shape.Dimensions[dimIndex0].Stride;
                        long innerStride1 = in1.Shape.Dimensions[dimIndex1].Stride;
                        long innerInnerStride1 = in1.Shape.Dimensions[dimIndex2].Stride;

                        outerStride1 -= innerStride1 * in1.Shape.Dimensions[dimIndex1].Length;
                        innerStride1 -= innerInnerStride1 * in1.Shape.Dimensions[dimIndex2].Length;

                        result = d1[in1.Shape[counters]];
                        bool first = true;

                        for (long outer = 0; outer < totalOps; outer++)
                        {
                            //Get the array offset for the first element in the outer dimension
                            long ix1 = in1.Shape[counters];
                            if (first)
                                ix1 += innerInnerStride1;

                            for (long i = 0; i < opsOuter; i++)
                            {
                                for (long j = 0; j < opsInner; j++)
                                {
                                    for (long k = (first ? 1 : 0); k < opsInnerInner; k++)
                                    {
                                        result = (System.Boolean)(result ^ d1[ix1]);
                                        ix1 += innerInnerStride1;
                                    }
                                    first = false;

                                    ix1 += innerStride1;
                                }

                                ix1 += outerStride1;
                            }

                            if (counters.LongLength > 0)
                            {
                                //Basically a ripple carry adder
                                long p = counters.LongLength - 1;
                                while (++counters[p] == limits[p] && p > 0)
                                {
                                    counters[p] = 0;
                                    p--;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
	}
}