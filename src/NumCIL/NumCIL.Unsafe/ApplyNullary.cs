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
using System.Runtime.InteropServices;

namespace NumCIL.Unsafe
{
    internal static partial class Apply
    {
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_SByte<C>(C op, NdArray<System.SByte> @out)
            where C : INullaryOp<System.SByte>
        {
			if (UFunc_Op_Inner_Nullary_Flush_Typed<System.SByte, C>(op, @out))
				return;

#if DEBUG
			Console.WriteLine("Generic Nullary method C for SByte, with op = {0}, Ta = {1}", op.GetType(), typeof(System.SByte));
#endif
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.SByte*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Byte<C>(C op, NdArray<System.Byte> @out)
            where C : INullaryOp<System.Byte>
        {
			if (UFunc_Op_Inner_Nullary_Flush_Typed<System.Byte, C>(op, @out))
				return;

#if DEBUG
			Console.WriteLine("Generic Nullary method C for Byte, with op = {0}, Ta = {1}", op.GetType(), typeof(System.Byte));
#endif
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Byte*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Int16<C>(C op, NdArray<System.Int16> @out)
            where C : INullaryOp<System.Int16>
        {
			if (UFunc_Op_Inner_Nullary_Flush_Typed<System.Int16, C>(op, @out))
				return;

#if DEBUG
			Console.WriteLine("Generic Nullary method C for Int16, with op = {0}, Ta = {1}", op.GetType(), typeof(System.Int16));
#endif
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Int16*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_UInt16<C>(C op, NdArray<System.UInt16> @out)
            where C : INullaryOp<System.UInt16>
        {
			if (UFunc_Op_Inner_Nullary_Flush_Typed<System.UInt16, C>(op, @out))
				return;

#if DEBUG
			Console.WriteLine("Generic Nullary method C for UInt16, with op = {0}, Ta = {1}", op.GetType(), typeof(System.UInt16));
#endif
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.UInt16*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Int32<C>(C op, NdArray<System.Int32> @out)
            where C : INullaryOp<System.Int32>
        {
			if (UFunc_Op_Inner_Nullary_Flush_Typed<System.Int32, C>(op, @out))
				return;

#if DEBUG
			Console.WriteLine("Generic Nullary method C for Int32, with op = {0}, Ta = {1}", op.GetType(), typeof(System.Int32));
#endif
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Int32*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_UInt32<C>(C op, NdArray<System.UInt32> @out)
            where C : INullaryOp<System.UInt32>
        {
			if (UFunc_Op_Inner_Nullary_Flush_Typed<System.UInt32, C>(op, @out))
				return;

#if DEBUG
			Console.WriteLine("Generic Nullary method C for UInt32, with op = {0}, Ta = {1}", op.GetType(), typeof(System.UInt32));
#endif
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.UInt32*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Int64<C>(C op, NdArray<System.Int64> @out)
            where C : INullaryOp<System.Int64>
        {
			if (UFunc_Op_Inner_Nullary_Flush_Typed<System.Int64, C>(op, @out))
				return;

#if DEBUG
			Console.WriteLine("Generic Nullary method C for Int64, with op = {0}, Ta = {1}", op.GetType(), typeof(System.Int64));
#endif
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Int64*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_UInt64<C>(C op, NdArray<System.UInt64> @out)
            where C : INullaryOp<System.UInt64>
        {
			if (UFunc_Op_Inner_Nullary_Flush_Typed<System.UInt64, C>(op, @out))
				return;

#if DEBUG
			Console.WriteLine("Generic Nullary method C for UInt64, with op = {0}, Ta = {1}", op.GetType(), typeof(System.UInt64));
#endif
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.UInt64*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Single<C>(C op, NdArray<System.Single> @out)
            where C : INullaryOp<System.Single>
        {
			if (UFunc_Op_Inner_Nullary_Flush_Typed<System.Single, C>(op, @out))
				return;

#if DEBUG
			Console.WriteLine("Generic Nullary method C for Single, with op = {0}, Ta = {1}", op.GetType(), typeof(System.Single));
#endif
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Single*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Double<C>(C op, NdArray<System.Double> @out)
            where C : INullaryOp<System.Double>
        {
			if (UFunc_Op_Inner_Nullary_Flush_Typed<System.Double, C>(op, @out))
				return;

#if DEBUG
			Console.WriteLine("Generic Nullary method C for Double, with op = {0}, Ta = {1}", op.GetType(), typeof(System.Double));
#endif
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Double*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_SByte_TypedImpl<C>(NumCIL.Generic.RandomGeneratorOpInt8 op, NdArray<System.SByte> @out)
            where C : INullaryOp<System.SByte>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.SByte*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Byte_TypedImpl<C>(NumCIL.Generic.RandomGeneratorOpUInt8 op, NdArray<System.Byte> @out)
            where C : INullaryOp<System.Byte>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Byte*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Int16_TypedImpl<C>(NumCIL.Generic.RandomGeneratorOpInt16 op, NdArray<System.Int16> @out)
            where C : INullaryOp<System.Int16>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Int16*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_UInt16_TypedImpl<C>(NumCIL.Generic.RandomGeneratorOpUInt16 op, NdArray<System.UInt16> @out)
            where C : INullaryOp<System.UInt16>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.UInt16*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Int32_TypedImpl<C>(NumCIL.Generic.RandomGeneratorOpInt32 op, NdArray<System.Int32> @out)
            where C : INullaryOp<System.Int32>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Int32*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_UInt32_TypedImpl<C>(NumCIL.Generic.RandomGeneratorOpUInt32 op, NdArray<System.UInt32> @out)
            where C : INullaryOp<System.UInt32>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.UInt32*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Int64_TypedImpl<C>(NumCIL.Generic.RandomGeneratorOpInt64 op, NdArray<System.Int64> @out)
            where C : INullaryOp<System.Int64>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Int64*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_UInt64_TypedImpl<C>(NumCIL.Generic.RandomGeneratorOpUInt64 op, NdArray<System.UInt64> @out)
            where C : INullaryOp<System.UInt64>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.UInt64*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Single_TypedImpl<C>(NumCIL.Generic.RandomGeneratorOpFloat op, NdArray<System.Single> @out)
            where C : INullaryOp<System.Single>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Single*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Double_TypedImpl<C>(NumCIL.Generic.RandomGeneratorOpDouble op, NdArray<System.Double> @out)
            where C : INullaryOp<System.Double>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Double*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_SByte_TypedImpl<C>(NumCIL.Generic.RangeGeneratorOp<System.SByte, NumCIL.Generic.NumberConverterInt8> op, NdArray<System.SByte> @out)
            where C : INullaryOp<System.SByte>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.SByte*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Byte_TypedImpl<C>(NumCIL.Generic.RangeGeneratorOp<System.Byte, NumCIL.Generic.NumberConverterUInt8> op, NdArray<System.Byte> @out)
            where C : INullaryOp<System.Byte>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Byte*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Int16_TypedImpl<C>(NumCIL.Generic.RangeGeneratorOp<System.Int16, NumCIL.Generic.NumberConverterInt16> op, NdArray<System.Int16> @out)
            where C : INullaryOp<System.Int16>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Int16*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_UInt16_TypedImpl<C>(NumCIL.Generic.RangeGeneratorOp<System.UInt16, NumCIL.Generic.NumberConverterUInt16> op, NdArray<System.UInt16> @out)
            where C : INullaryOp<System.UInt16>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.UInt16*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Int32_TypedImpl<C>(NumCIL.Generic.RangeGeneratorOp<System.Int32, NumCIL.Generic.NumberConverterInt32> op, NdArray<System.Int32> @out)
            where C : INullaryOp<System.Int32>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Int32*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_UInt32_TypedImpl<C>(NumCIL.Generic.RangeGeneratorOp<System.UInt32, NumCIL.Generic.NumberConverterUInt32> op, NdArray<System.UInt32> @out)
            where C : INullaryOp<System.UInt32>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.UInt32*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Int64_TypedImpl<C>(NumCIL.Generic.RangeGeneratorOp<System.Int64, NumCIL.Generic.NumberConverterInt64> op, NdArray<System.Int64> @out)
            where C : INullaryOp<System.Int64>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Int64*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_UInt64_TypedImpl<C>(NumCIL.Generic.RangeGeneratorOp<System.UInt64, NumCIL.Generic.NumberConverterUInt64> op, NdArray<System.UInt64> @out)
            where C : INullaryOp<System.UInt64>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.UInt64*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Single_TypedImpl<C>(NumCIL.Generic.RangeGeneratorOp<System.Single, NumCIL.Generic.NumberConverterFloat> op, NdArray<System.Single> @out)
            where C : INullaryOp<System.Single>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Single*)f.ptr;

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
        /// <summary>
        /// Unsafe implementation of applying a floating point nullary operation
        /// </summary>
        /// <typeparam name="C">The type of operation to perform</typeparam>
        /// <param name="op">The operation instance</param>
        /// <param name="out">The output target</param>
        internal static void UFunc_Op_Inner_Nullary_Flush_Double_TypedImpl<C>(NumCIL.Generic.RangeGeneratorOp<System.Double, NumCIL.Generic.NumberConverterDouble> op, NdArray<System.Double> @out)
            where C : INullaryOp<System.Double>
        {
            unsafe
            {
                using (var f = new Pinner(@out.DataAccessor))
                {
                    var d = (System.Double*)f.ptr;

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
	}
}


