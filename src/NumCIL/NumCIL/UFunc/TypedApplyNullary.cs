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

namespace NumCIL
{
    public static partial class UFunc
    {
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(C op, NdArray<T> @out)
            where C : struct, INullaryOp<T>
        {
			if (UFunc_Op_Inner_Nullary_Impl_Flush_Typed<T, C>(op, @out))
				return;

#if DEBUG
			Console.WriteLine("Generic Nullary method C for T, with op = {0}, T = {1}", op.GetType(), typeof(T));
#endif

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RandomGeneratorOpInt8 op, NdArray<System.SByte> @out)
            where C : struct, INullaryOp<System.SByte>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RandomGeneratorOpUInt8 op, NdArray<System.Byte> @out)
            where C : struct, INullaryOp<System.Byte>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RandomGeneratorOpInt16 op, NdArray<System.Int16> @out)
            where C : struct, INullaryOp<System.Int16>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RandomGeneratorOpUInt16 op, NdArray<System.UInt16> @out)
            where C : struct, INullaryOp<System.UInt16>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RandomGeneratorOpInt32 op, NdArray<System.Int32> @out)
            where C : struct, INullaryOp<System.Int32>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RandomGeneratorOpUInt32 op, NdArray<System.UInt32> @out)
            where C : struct, INullaryOp<System.UInt32>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RandomGeneratorOpInt64 op, NdArray<System.Int64> @out)
            where C : struct, INullaryOp<System.Int64>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RandomGeneratorOpUInt64 op, NdArray<System.UInt64> @out)
            where C : struct, INullaryOp<System.UInt64>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RandomGeneratorOpFloat op, NdArray<System.Single> @out)
            where C : struct, INullaryOp<System.Single>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RandomGeneratorOpDouble op, NdArray<System.Double> @out)
            where C : struct, INullaryOp<System.Double>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RandomGeneratorOpBoolean op, NdArray<System.Boolean> @out)
            where C : struct, INullaryOp<System.Boolean>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RandomGeneratorOpComplex128 op, NdArray<System.Numerics.Complex> @out)
            where C : struct, INullaryOp<System.Numerics.Complex>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RandomGeneratorOpComplex64 op, NdArray<NumCIL.Complex64.DataType> @out)
            where C : struct, INullaryOp<NumCIL.Complex64.DataType>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RangeGeneratorOp<System.SByte, NumCIL.Generic.NumberConverterInt8> op, NdArray<System.SByte> @out)
            where C : struct, INullaryOp<System.SByte>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RangeGeneratorOp<System.Byte, NumCIL.Generic.NumberConverterUInt8> op, NdArray<System.Byte> @out)
            where C : struct, INullaryOp<System.Byte>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RangeGeneratorOp<System.Int16, NumCIL.Generic.NumberConverterInt16> op, NdArray<System.Int16> @out)
            where C : struct, INullaryOp<System.Int16>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RangeGeneratorOp<System.UInt16, NumCIL.Generic.NumberConverterUInt16> op, NdArray<System.UInt16> @out)
            where C : struct, INullaryOp<System.UInt16>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RangeGeneratorOp<System.Int32, NumCIL.Generic.NumberConverterInt32> op, NdArray<System.Int32> @out)
            where C : struct, INullaryOp<System.Int32>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RangeGeneratorOp<System.UInt32, NumCIL.Generic.NumberConverterUInt32> op, NdArray<System.UInt32> @out)
            where C : struct, INullaryOp<System.UInt32>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RangeGeneratorOp<System.Int64, NumCIL.Generic.NumberConverterInt64> op, NdArray<System.Int64> @out)
            where C : struct, INullaryOp<System.Int64>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RangeGeneratorOp<System.UInt64, NumCIL.Generic.NumberConverterUInt64> op, NdArray<System.UInt64> @out)
            where C : struct, INullaryOp<System.UInt64>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RangeGeneratorOp<System.Single, NumCIL.Generic.NumberConverterFloat> op, NdArray<System.Single> @out)
            where C : struct, INullaryOp<System.Single>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RangeGeneratorOp<System.Double, NumCIL.Generic.NumberConverterDouble> op, NdArray<System.Double> @out)
            where C : struct, INullaryOp<System.Double>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RangeGeneratorOp<System.Boolean, NumCIL.Generic.NumberConverterBoolean> op, NdArray<System.Boolean> @out)
            where C : struct, INullaryOp<System.Boolean>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RangeGeneratorOp<System.Numerics.Complex, NumCIL.Generic.NumberConverterComplex128> op, NdArray<System.Numerics.Complex> @out)
            where C : struct, INullaryOp<System.Numerics.Complex>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
        /// <summary>
        /// Actually executes a nullary operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.INullaryOp{0}"/> or <see cref="T:NumCIL.IUnaryConvOp{0}"/> on each element.
        /// This implementation is optimized for use with up to 4 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to generat</typeparam>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        private static void UFunc_Op_Inner_Nullary_Impl_Flush<T, C>(NumCIL.Generic.RangeGeneratorOp<NumCIL.Complex64.DataType, NumCIL.Generic.NumberConverterComplex64> op, NdArray<NumCIL.Complex64.DataType> @out)
            where C : struct, INullaryOp<NumCIL.Complex64.DataType>
        {

            var d = @out.AsArray();

            if (@out.Shape.Dimensions.Length == 1)
            {
                long totalOps = @out.Shape.Dimensions[0].Length;
                long ix = @out.Shape.Offset;
                long stride = @out.Shape.Dimensions[0].Stride;

                for (long i = 0; i < totalOps; i++)
                {
                    d[ix] = op.Op();
                    ix += stride;
                }
            }
            else if (@out.Shape.Dimensions.Length == 2)
            {
                long opsOuter = @out.Shape.Dimensions[0].Length;
                long opsInner = @out.Shape.Dimensions[1].Length;

                long ix = @out.Shape.Offset;
                long outerStride = @out.Shape.Dimensions[0].Stride;
                long innerStride = @out.Shape.Dimensions[1].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[1].Length;

                for (long i = 0; i < opsOuter; i++)
                {
                    for (long j = 0; j < opsInner; j++)
                    {
                        d[ix] = op.Op();
                        ix += innerStride;
                    }

                    ix += outerStride;
                }
            }
            else
            {
                long n = @out.Shape.Dimensions.LongLength - 3;
                long[] limits = @out.Shape.Dimensions.Where(x => n-- > 0).Select(x => x.Length).ToArray();
                long[] counters = new long[limits.LongLength];

                long totalOps = limits.LongLength == 0 ? 1 : limits.Aggregate<long>((a, b) => a * b);

                long dimIndex0 = 0 + limits.LongLength;
                long dimIndex1 = 1 + limits.LongLength;
                long dimIndex2 = 2 + limits.LongLength;

                long opsOuter = @out.Shape.Dimensions[dimIndex0].Length;
                long opsInner = @out.Shape.Dimensions[dimIndex1].Length;
                long opsInnerInner = @out.Shape.Dimensions[dimIndex2].Length;

                long outerStride = @out.Shape.Dimensions[dimIndex0].Stride;
                long innerStride = @out.Shape.Dimensions[dimIndex1].Stride;
                long innerInnerStride = @out.Shape.Dimensions[dimIndex2].Stride;

                outerStride -= innerStride * @out.Shape.Dimensions[dimIndex1].Length;
                innerStride -= innerInnerStride * @out.Shape.Dimensions[dimIndex2].Length;

                for (long outer = 0; outer < totalOps; outer++)
                {
                    //Get the array offset for the first element in the outer dimension
                    long ix = @out.Shape[counters];

                    for (long i = 0; i < opsOuter; i++)
                    {
                        for (long j = 0; j < opsInner; j++)
                        {
                            for (long k = 0; k < opsInnerInner; k++)
                            {
                                d[ix] = op.Op();
                                ix += innerInnerStride;
                            }

                            ix += innerStride;
                        }

                        ix += outerStride;
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
}