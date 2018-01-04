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
    internal static class Reduce
    {
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush_SByte<C>(C op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
			if (Apply.UFunc_Reduce_Inner_Flush_Typed<System.SByte, C>(op, axis, in1, @out))
				return @out;

#if DEBUG
			Console.WriteLine("Generic Reduce method C for SByte, with op = {0}, T = {1}", op.GetType(), typeof(System.SByte));
#endif
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.SByte*)f1.ptr;
                        var vd = (System.SByte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.SByte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.SByte)op.Op(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.SByte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.SByte)op.Op(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.SByte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.SByte)op.Op(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.SByte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_SByte<C>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush_Byte<C>(C op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
			if (Apply.UFunc_Reduce_Inner_Flush_Typed<System.Byte, C>(op, axis, in1, @out))
				return @out;

#if DEBUG
			Console.WriteLine("Generic Reduce method C for Byte, with op = {0}, T = {1}", op.GetType(), typeof(System.Byte));
#endif
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Byte*)f1.ptr;
                        var vd = (System.Byte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Byte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Byte)op.Op(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Byte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Byte)op.Op(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Byte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Byte)op.Op(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Byte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Byte<C>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush_Int16<C>(C op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
			if (Apply.UFunc_Reduce_Inner_Flush_Typed<System.Int16, C>(op, axis, in1, @out))
				return @out;

#if DEBUG
			Console.WriteLine("Generic Reduce method C for Int16, with op = {0}, T = {1}", op.GetType(), typeof(System.Int16));
#endif
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int16*)f1.ptr;
                        var vd = (System.Int16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int16)op.Op(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int16)op.Op(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int16)op.Op(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int16<C>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush_UInt16<C>(C op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
			if (Apply.UFunc_Reduce_Inner_Flush_Typed<System.UInt16, C>(op, axis, in1, @out))
				return @out;

#if DEBUG
			Console.WriteLine("Generic Reduce method C for UInt16, with op = {0}, T = {1}", op.GetType(), typeof(System.UInt16));
#endif
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt16*)f1.ptr;
                        var vd = (System.UInt16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt16)op.Op(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt16)op.Op(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt16)op.Op(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt16<C>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush_Int32<C>(C op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
			if (Apply.UFunc_Reduce_Inner_Flush_Typed<System.Int32, C>(op, axis, in1, @out))
				return @out;

#if DEBUG
			Console.WriteLine("Generic Reduce method C for Int32, with op = {0}, T = {1}", op.GetType(), typeof(System.Int32));
#endif
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int32*)f1.ptr;
                        var vd = (System.Int32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int32)op.Op(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int32)op.Op(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int32)op.Op(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int32<C>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush_UInt32<C>(C op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
			if (Apply.UFunc_Reduce_Inner_Flush_Typed<System.UInt32, C>(op, axis, in1, @out))
				return @out;

#if DEBUG
			Console.WriteLine("Generic Reduce method C for UInt32, with op = {0}, T = {1}", op.GetType(), typeof(System.UInt32));
#endif
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt32*)f1.ptr;
                        var vd = (System.UInt32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt32)op.Op(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt32)op.Op(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt32)op.Op(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt32<C>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush_Int64<C>(C op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
			if (Apply.UFunc_Reduce_Inner_Flush_Typed<System.Int64, C>(op, axis, in1, @out))
				return @out;

#if DEBUG
			Console.WriteLine("Generic Reduce method C for Int64, with op = {0}, T = {1}", op.GetType(), typeof(System.Int64));
#endif
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int64*)f1.ptr;
                        var vd = (System.Int64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int64)op.Op(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int64)op.Op(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int64)op.Op(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int64<C>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush_UInt64<C>(C op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
			if (Apply.UFunc_Reduce_Inner_Flush_Typed<System.UInt64, C>(op, axis, in1, @out))
				return @out;

#if DEBUG
			Console.WriteLine("Generic Reduce method C for UInt64, with op = {0}, T = {1}", op.GetType(), typeof(System.UInt64));
#endif
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt64*)f1.ptr;
                        var vd = (System.UInt64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt64)op.Op(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt64)op.Op(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt64)op.Op(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt64<C>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush_Single<C>(C op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
			if (Apply.UFunc_Reduce_Inner_Flush_Typed<System.Single, C>(op, axis, in1, @out))
				return @out;

#if DEBUG
			Console.WriteLine("Generic Reduce method C for Single, with op = {0}, T = {1}", op.GetType(), typeof(System.Single));
#endif
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Single*)f1.ptr;
                        var vd = (System.Single*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Single value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Single)op.Op(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Single value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Single)op.Op(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Single value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Single)op.Op(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Single> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Single<C>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush_Double<C>(C op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
			if (Apply.UFunc_Reduce_Inner_Flush_Typed<System.Double, C>(op, axis, in1, @out))
				return @out;

#if DEBUG
			Console.WriteLine("Generic Reduce method C for Double, with op = {0}, T = {1}", op.GetType(), typeof(System.Double));
#endif
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Double*)f1.ptr;
                        var vd = (System.Double*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Double value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Double)op.Op(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Double value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Double)op.Op(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Double value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Double)op.Op(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Double> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Double<C>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush_SByte_TypedImpl<C>(NumCIL.Int8.Add op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.SByte*)f1.ptr;
                        var vd = (System.SByte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.SByte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.SByte)(value + d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.SByte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.SByte)(value + d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.SByte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.SByte)(value + d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.SByte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_SByte<NumCIL.Int8.Add>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush_Byte_TypedImpl<C>(NumCIL.UInt8.Add op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Byte*)f1.ptr;
                        var vd = (System.Byte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Byte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Byte)(value + d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Byte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Byte)(value + d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Byte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Byte)(value + d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Byte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Byte<NumCIL.UInt8.Add>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush_Int16_TypedImpl<C>(NumCIL.Int16.Add op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int16*)f1.ptr;
                        var vd = (System.Int16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int16)(value + d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int16)(value + d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int16)(value + d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int16<NumCIL.Int16.Add>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush_UInt16_TypedImpl<C>(NumCIL.UInt16.Add op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt16*)f1.ptr;
                        var vd = (System.UInt16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt16)(value + d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt16)(value + d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt16)(value + d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt16<NumCIL.UInt16.Add>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush_Int32_TypedImpl<C>(NumCIL.Int32.Add op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int32*)f1.ptr;
                        var vd = (System.Int32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int32)(value + d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int32)(value + d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int32)(value + d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int32<NumCIL.Int32.Add>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush_UInt32_TypedImpl<C>(NumCIL.UInt32.Add op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt32*)f1.ptr;
                        var vd = (System.UInt32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt32)(value + d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt32)(value + d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt32)(value + d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt32<NumCIL.UInt32.Add>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush_Int64_TypedImpl<C>(NumCIL.Int64.Add op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int64*)f1.ptr;
                        var vd = (System.Int64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int64)(value + d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int64)(value + d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int64)(value + d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int64<NumCIL.Int64.Add>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush_UInt64_TypedImpl<C>(NumCIL.UInt64.Add op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt64*)f1.ptr;
                        var vd = (System.UInt64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt64)(value + d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt64)(value + d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt64)(value + d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt64<NumCIL.UInt64.Add>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush_Single_TypedImpl<C>(NumCIL.Float.Add op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Single*)f1.ptr;
                        var vd = (System.Single*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Single value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Single)(value + d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Single value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Single)(value + d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Single value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Single)(value + d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Single> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Single<NumCIL.Float.Add>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush_Double_TypedImpl<C>(NumCIL.Double.Add op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Double*)f1.ptr;
                        var vd = (System.Double*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Double value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Double)(value + d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Double value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Double)(value + d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Double value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Double)(value + d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Double> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Double<NumCIL.Double.Add>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush_SByte_TypedImpl<C>(NumCIL.Int8.Sub op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.SByte*)f1.ptr;
                        var vd = (System.SByte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.SByte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.SByte)(value - d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.SByte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.SByte)(value - d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.SByte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.SByte)(value - d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.SByte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_SByte<NumCIL.Int8.Sub>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush_Byte_TypedImpl<C>(NumCIL.UInt8.Sub op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Byte*)f1.ptr;
                        var vd = (System.Byte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Byte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Byte)(value - d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Byte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Byte)(value - d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Byte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Byte)(value - d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Byte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Byte<NumCIL.UInt8.Sub>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush_Int16_TypedImpl<C>(NumCIL.Int16.Sub op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int16*)f1.ptr;
                        var vd = (System.Int16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int16)(value - d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int16)(value - d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int16)(value - d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int16<NumCIL.Int16.Sub>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush_UInt16_TypedImpl<C>(NumCIL.UInt16.Sub op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt16*)f1.ptr;
                        var vd = (System.UInt16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt16)(value - d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt16)(value - d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt16)(value - d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt16<NumCIL.UInt16.Sub>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush_Int32_TypedImpl<C>(NumCIL.Int32.Sub op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int32*)f1.ptr;
                        var vd = (System.Int32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int32)(value - d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int32)(value - d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int32)(value - d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int32<NumCIL.Int32.Sub>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush_UInt32_TypedImpl<C>(NumCIL.UInt32.Sub op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt32*)f1.ptr;
                        var vd = (System.UInt32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt32)(value - d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt32)(value - d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt32)(value - d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt32<NumCIL.UInt32.Sub>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush_Int64_TypedImpl<C>(NumCIL.Int64.Sub op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int64*)f1.ptr;
                        var vd = (System.Int64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int64)(value - d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int64)(value - d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int64)(value - d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int64<NumCIL.Int64.Sub>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush_UInt64_TypedImpl<C>(NumCIL.UInt64.Sub op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt64*)f1.ptr;
                        var vd = (System.UInt64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt64)(value - d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt64)(value - d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt64)(value - d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt64<NumCIL.UInt64.Sub>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush_Single_TypedImpl<C>(NumCIL.Float.Sub op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Single*)f1.ptr;
                        var vd = (System.Single*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Single value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Single)(value - d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Single value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Single)(value - d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Single value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Single)(value - d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Single> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Single<NumCIL.Float.Sub>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush_Double_TypedImpl<C>(NumCIL.Double.Sub op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Double*)f1.ptr;
                        var vd = (System.Double*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Double value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Double)(value - d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Double value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Double)(value - d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Double value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Double)(value - d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Double> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Double<NumCIL.Double.Sub>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush_SByte_TypedImpl<C>(NumCIL.Int8.Mul op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.SByte*)f1.ptr;
                        var vd = (System.SByte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.SByte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.SByte)(value * d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.SByte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.SByte)(value * d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.SByte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.SByte)(value * d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.SByte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_SByte<NumCIL.Int8.Mul>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush_Byte_TypedImpl<C>(NumCIL.UInt8.Mul op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Byte*)f1.ptr;
                        var vd = (System.Byte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Byte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Byte)(value * d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Byte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Byte)(value * d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Byte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Byte)(value * d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Byte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Byte<NumCIL.UInt8.Mul>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush_Int16_TypedImpl<C>(NumCIL.Int16.Mul op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int16*)f1.ptr;
                        var vd = (System.Int16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int16)(value * d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int16)(value * d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int16)(value * d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int16<NumCIL.Int16.Mul>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush_UInt16_TypedImpl<C>(NumCIL.UInt16.Mul op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt16*)f1.ptr;
                        var vd = (System.UInt16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt16)(value * d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt16)(value * d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt16)(value * d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt16<NumCIL.UInt16.Mul>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush_Int32_TypedImpl<C>(NumCIL.Int32.Mul op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int32*)f1.ptr;
                        var vd = (System.Int32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int32)(value * d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int32)(value * d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int32)(value * d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int32<NumCIL.Int32.Mul>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush_UInt32_TypedImpl<C>(NumCIL.UInt32.Mul op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt32*)f1.ptr;
                        var vd = (System.UInt32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt32)(value * d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt32)(value * d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt32)(value * d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt32<NumCIL.UInt32.Mul>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush_Int64_TypedImpl<C>(NumCIL.Int64.Mul op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int64*)f1.ptr;
                        var vd = (System.Int64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int64)(value * d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int64)(value * d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int64)(value * d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int64<NumCIL.Int64.Mul>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush_UInt64_TypedImpl<C>(NumCIL.UInt64.Mul op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt64*)f1.ptr;
                        var vd = (System.UInt64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt64)(value * d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt64)(value * d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt64)(value * d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt64<NumCIL.UInt64.Mul>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush_Single_TypedImpl<C>(NumCIL.Float.Mul op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Single*)f1.ptr;
                        var vd = (System.Single*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Single value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Single)(value * d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Single value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Single)(value * d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Single value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Single)(value * d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Single> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Single<NumCIL.Float.Mul>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush_Double_TypedImpl<C>(NumCIL.Double.Mul op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Double*)f1.ptr;
                        var vd = (System.Double*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Double value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Double)(value * d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Double value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Double)(value * d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Double value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Double)(value * d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Double> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Double<NumCIL.Double.Mul>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush_SByte_TypedImpl<C>(NumCIL.Int8.Div op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.SByte*)f1.ptr;
                        var vd = (System.SByte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.SByte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.SByte)(value / d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.SByte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.SByte)(value / d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.SByte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.SByte)(value / d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.SByte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_SByte<NumCIL.Int8.Div>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush_Byte_TypedImpl<C>(NumCIL.UInt8.Div op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Byte*)f1.ptr;
                        var vd = (System.Byte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Byte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Byte)(value / d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Byte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Byte)(value / d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Byte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Byte)(value / d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Byte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Byte<NumCIL.UInt8.Div>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush_Int16_TypedImpl<C>(NumCIL.Int16.Div op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int16*)f1.ptr;
                        var vd = (System.Int16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int16)(value / d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int16)(value / d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int16)(value / d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int16<NumCIL.Int16.Div>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush_UInt16_TypedImpl<C>(NumCIL.UInt16.Div op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt16*)f1.ptr;
                        var vd = (System.UInt16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt16)(value / d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt16)(value / d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt16)(value / d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt16<NumCIL.UInt16.Div>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush_Int32_TypedImpl<C>(NumCIL.Int32.Div op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int32*)f1.ptr;
                        var vd = (System.Int32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int32)(value / d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int32)(value / d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int32)(value / d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int32<NumCIL.Int32.Div>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush_UInt32_TypedImpl<C>(NumCIL.UInt32.Div op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt32*)f1.ptr;
                        var vd = (System.UInt32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt32)(value / d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt32)(value / d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt32)(value / d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt32<NumCIL.UInt32.Div>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush_Int64_TypedImpl<C>(NumCIL.Int64.Div op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int64*)f1.ptr;
                        var vd = (System.Int64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int64)(value / d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int64)(value / d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int64)(value / d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int64<NumCIL.Int64.Div>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush_UInt64_TypedImpl<C>(NumCIL.UInt64.Div op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt64*)f1.ptr;
                        var vd = (System.UInt64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt64)(value / d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt64)(value / d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt64)(value / d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt64<NumCIL.UInt64.Div>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush_Single_TypedImpl<C>(NumCIL.Float.Div op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Single*)f1.ptr;
                        var vd = (System.Single*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Single value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Single)(value / d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Single value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Single)(value / d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Single value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Single)(value / d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Single> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Single<NumCIL.Float.Div>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush_Double_TypedImpl<C>(NumCIL.Double.Div op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Double*)f1.ptr;
                        var vd = (System.Double*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Double value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Double)(value / d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Double value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Double)(value / d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Double value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Double)(value / d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Double> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Double<NumCIL.Double.Div>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush_SByte_TypedImpl<C>(NumCIL.Int8.Mod op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.SByte*)f1.ptr;
                        var vd = (System.SByte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.SByte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.SByte)(value % d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.SByte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.SByte)(value % d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.SByte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.SByte)(value % d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.SByte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_SByte<NumCIL.Int8.Mod>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush_Byte_TypedImpl<C>(NumCIL.UInt8.Mod op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Byte*)f1.ptr;
                        var vd = (System.Byte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Byte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Byte)(value % d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Byte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Byte)(value % d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Byte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Byte)(value % d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Byte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Byte<NumCIL.UInt8.Mod>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush_Int16_TypedImpl<C>(NumCIL.Int16.Mod op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int16*)f1.ptr;
                        var vd = (System.Int16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int16)(value % d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int16)(value % d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int16)(value % d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int16<NumCIL.Int16.Mod>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush_UInt16_TypedImpl<C>(NumCIL.UInt16.Mod op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt16*)f1.ptr;
                        var vd = (System.UInt16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt16)(value % d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt16)(value % d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt16)(value % d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt16<NumCIL.UInt16.Mod>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush_Int32_TypedImpl<C>(NumCIL.Int32.Mod op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int32*)f1.ptr;
                        var vd = (System.Int32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int32)(value % d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int32)(value % d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int32)(value % d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int32<NumCIL.Int32.Mod>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush_UInt32_TypedImpl<C>(NumCIL.UInt32.Mod op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt32*)f1.ptr;
                        var vd = (System.UInt32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt32)(value % d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt32)(value % d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt32)(value % d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt32<NumCIL.UInt32.Mod>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush_Int64_TypedImpl<C>(NumCIL.Int64.Mod op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int64*)f1.ptr;
                        var vd = (System.Int64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int64)(value % d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int64)(value % d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int64)(value % d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int64<NumCIL.Int64.Mod>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush_UInt64_TypedImpl<C>(NumCIL.UInt64.Mod op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt64*)f1.ptr;
                        var vd = (System.UInt64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt64)(value % d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt64)(value % d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt64)(value % d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt64<NumCIL.UInt64.Mod>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush_Single_TypedImpl<C>(NumCIL.Float.Mod op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Single*)f1.ptr;
                        var vd = (System.Single*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Single value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Single)(value % d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Single value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Single)(value % d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Single value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Single)(value % d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Single> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Single<NumCIL.Float.Mod>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush_Double_TypedImpl<C>(NumCIL.Double.Mod op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Double*)f1.ptr;
                        var vd = (System.Double*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Double value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Double)(value % d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Double value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Double)(value % d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Double value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Double)(value % d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Double> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Double<NumCIL.Double.Mod>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush_SByte_TypedImpl<C>(NumCIL.Int8.Min op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.SByte*)f1.ptr;
                        var vd = (System.SByte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.SByte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.SByte)Math.Min(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.SByte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.SByte)Math.Min(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.SByte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.SByte)Math.Min(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.SByte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_SByte<NumCIL.Int8.Min>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush_Byte_TypedImpl<C>(NumCIL.UInt8.Min op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Byte*)f1.ptr;
                        var vd = (System.Byte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Byte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Byte)Math.Min(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Byte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Byte)Math.Min(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Byte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Byte)Math.Min(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Byte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Byte<NumCIL.UInt8.Min>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush_Int16_TypedImpl<C>(NumCIL.Int16.Min op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int16*)f1.ptr;
                        var vd = (System.Int16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int16)Math.Min(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int16)Math.Min(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int16)Math.Min(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int16<NumCIL.Int16.Min>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush_UInt16_TypedImpl<C>(NumCIL.UInt16.Min op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt16*)f1.ptr;
                        var vd = (System.UInt16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt16)Math.Min(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt16)Math.Min(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt16)Math.Min(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt16<NumCIL.UInt16.Min>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush_Int32_TypedImpl<C>(NumCIL.Int32.Min op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int32*)f1.ptr;
                        var vd = (System.Int32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int32)Math.Min(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int32)Math.Min(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int32)Math.Min(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int32<NumCIL.Int32.Min>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush_UInt32_TypedImpl<C>(NumCIL.UInt32.Min op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt32*)f1.ptr;
                        var vd = (System.UInt32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt32)Math.Min(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt32)Math.Min(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt32)Math.Min(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt32<NumCIL.UInt32.Min>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush_Int64_TypedImpl<C>(NumCIL.Int64.Min op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int64*)f1.ptr;
                        var vd = (System.Int64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int64)Math.Min(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int64)Math.Min(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int64)Math.Min(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int64<NumCIL.Int64.Min>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush_UInt64_TypedImpl<C>(NumCIL.UInt64.Min op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt64*)f1.ptr;
                        var vd = (System.UInt64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt64)Math.Min(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt64)Math.Min(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt64)Math.Min(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt64<NumCIL.UInt64.Min>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush_Single_TypedImpl<C>(NumCIL.Float.Min op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Single*)f1.ptr;
                        var vd = (System.Single*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Single value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Single)Math.Min(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Single value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Single)Math.Min(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Single value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Single)Math.Min(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Single> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Single<NumCIL.Float.Min>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush_Double_TypedImpl<C>(NumCIL.Double.Min op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Double*)f1.ptr;
                        var vd = (System.Double*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Double value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Double)Math.Min(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Double value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Double)Math.Min(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Double value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Double)Math.Min(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Double> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Double<NumCIL.Double.Min>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush_SByte_TypedImpl<C>(NumCIL.Int8.Max op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.SByte*)f1.ptr;
                        var vd = (System.SByte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.SByte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.SByte)Math.Max(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.SByte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.SByte)Math.Max(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.SByte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.SByte)Math.Max(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.SByte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_SByte<NumCIL.Int8.Max>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush_Byte_TypedImpl<C>(NumCIL.UInt8.Max op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Byte*)f1.ptr;
                        var vd = (System.Byte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Byte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Byte)Math.Max(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Byte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Byte)Math.Max(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Byte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Byte)Math.Max(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Byte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Byte<NumCIL.UInt8.Max>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush_Int16_TypedImpl<C>(NumCIL.Int16.Max op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int16*)f1.ptr;
                        var vd = (System.Int16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int16)Math.Max(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int16)Math.Max(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int16)Math.Max(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int16<NumCIL.Int16.Max>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush_UInt16_TypedImpl<C>(NumCIL.UInt16.Max op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt16*)f1.ptr;
                        var vd = (System.UInt16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt16)Math.Max(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt16)Math.Max(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt16)Math.Max(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt16<NumCIL.UInt16.Max>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush_Int32_TypedImpl<C>(NumCIL.Int32.Max op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int32*)f1.ptr;
                        var vd = (System.Int32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int32)Math.Max(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int32)Math.Max(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int32)Math.Max(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int32<NumCIL.Int32.Max>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush_UInt32_TypedImpl<C>(NumCIL.UInt32.Max op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt32*)f1.ptr;
                        var vd = (System.UInt32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt32)Math.Max(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt32)Math.Max(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt32)Math.Max(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt32<NumCIL.UInt32.Max>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush_Int64_TypedImpl<C>(NumCIL.Int64.Max op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int64*)f1.ptr;
                        var vd = (System.Int64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int64)Math.Max(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int64)Math.Max(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int64)Math.Max(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int64<NumCIL.Int64.Max>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush_UInt64_TypedImpl<C>(NumCIL.UInt64.Max op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt64*)f1.ptr;
                        var vd = (System.UInt64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt64)Math.Max(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt64)Math.Max(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt64)Math.Max(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt64<NumCIL.UInt64.Max>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush_Single_TypedImpl<C>(NumCIL.Float.Max op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Single*)f1.ptr;
                        var vd = (System.Single*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Single value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Single)Math.Max(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Single value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Single)Math.Max(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Single value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Single)Math.Max(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Single> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Single<NumCIL.Float.Max>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush_Double_TypedImpl<C>(NumCIL.Double.Max op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Double*)f1.ptr;
                        var vd = (System.Double*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Double value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Double)Math.Max(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Double value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Double)Math.Max(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Double value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Double)Math.Max(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Double> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Double<NumCIL.Double.Max>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush_SByte_TypedImpl<C>(NumCIL.Int8.Pow op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.SByte*)f1.ptr;
                        var vd = (System.SByte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.SByte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.SByte)Math.Pow(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.SByte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.SByte)Math.Pow(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.SByte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.SByte)Math.Pow(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.SByte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_SByte<NumCIL.Int8.Pow>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush_Byte_TypedImpl<C>(NumCIL.UInt8.Pow op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Byte*)f1.ptr;
                        var vd = (System.Byte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Byte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Byte)Math.Pow(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Byte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Byte)Math.Pow(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Byte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Byte)Math.Pow(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Byte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Byte<NumCIL.UInt8.Pow>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush_Int16_TypedImpl<C>(NumCIL.Int16.Pow op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int16*)f1.ptr;
                        var vd = (System.Int16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int16)Math.Pow(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int16)Math.Pow(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int16)Math.Pow(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int16<NumCIL.Int16.Pow>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush_UInt16_TypedImpl<C>(NumCIL.UInt16.Pow op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt16*)f1.ptr;
                        var vd = (System.UInt16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt16)Math.Pow(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt16)Math.Pow(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt16)Math.Pow(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt16<NumCIL.UInt16.Pow>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush_Int32_TypedImpl<C>(NumCIL.Int32.Pow op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int32*)f1.ptr;
                        var vd = (System.Int32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int32)Math.Pow(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int32)Math.Pow(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int32)Math.Pow(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int32<NumCIL.Int32.Pow>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush_UInt32_TypedImpl<C>(NumCIL.UInt32.Pow op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt32*)f1.ptr;
                        var vd = (System.UInt32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt32)Math.Pow(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt32)Math.Pow(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt32)Math.Pow(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt32<NumCIL.UInt32.Pow>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush_Int64_TypedImpl<C>(NumCIL.Int64.Pow op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int64*)f1.ptr;
                        var vd = (System.Int64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int64)Math.Pow(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int64)Math.Pow(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int64)Math.Pow(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int64<NumCIL.Int64.Pow>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush_UInt64_TypedImpl<C>(NumCIL.UInt64.Pow op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt64*)f1.ptr;
                        var vd = (System.UInt64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt64)Math.Pow(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt64)Math.Pow(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt64)Math.Pow(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt64<NumCIL.UInt64.Pow>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush_Single_TypedImpl<C>(NumCIL.Float.Pow op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Single*)f1.ptr;
                        var vd = (System.Single*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Single value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Single)Math.Pow(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Single value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Single)Math.Pow(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Single value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Single)Math.Pow(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Single> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Single<CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Single<NumCIL.Float.Pow>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush_Double_TypedImpl<C>(NumCIL.Double.Pow op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Double*)f1.ptr;
                        var vd = (System.Double*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Double value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Double)Math.Pow(value, d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Double value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Double)Math.Pow(value, d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Double value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Double)Math.Pow(value, d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Double> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Double<CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Double<NumCIL.Double.Pow>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush_SByte_TypedImpl<C>(NumCIL.Int8.And op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.SByte*)f1.ptr;
                        var vd = (System.SByte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.SByte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.SByte)(value & d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.SByte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.SByte)(value & d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.SByte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.SByte)(value & d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.SByte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_SByte<NumCIL.Int8.And>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush_Byte_TypedImpl<C>(NumCIL.UInt8.And op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Byte*)f1.ptr;
                        var vd = (System.Byte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Byte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Byte)(value & d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Byte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Byte)(value & d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Byte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Byte)(value & d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Byte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Byte<NumCIL.UInt8.And>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush_Int16_TypedImpl<C>(NumCIL.Int16.And op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int16*)f1.ptr;
                        var vd = (System.Int16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int16)(value & d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int16)(value & d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int16)(value & d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int16<NumCIL.Int16.And>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush_UInt16_TypedImpl<C>(NumCIL.UInt16.And op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt16*)f1.ptr;
                        var vd = (System.UInt16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt16)(value & d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt16)(value & d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt16)(value & d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt16<NumCIL.UInt16.And>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush_Int32_TypedImpl<C>(NumCIL.Int32.And op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int32*)f1.ptr;
                        var vd = (System.Int32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int32)(value & d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int32)(value & d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int32)(value & d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int32<NumCIL.Int32.And>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush_UInt32_TypedImpl<C>(NumCIL.UInt32.And op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt32*)f1.ptr;
                        var vd = (System.UInt32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt32)(value & d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt32)(value & d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt32)(value & d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt32<NumCIL.UInt32.And>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush_Int64_TypedImpl<C>(NumCIL.Int64.And op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int64*)f1.ptr;
                        var vd = (System.Int64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int64)(value & d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int64)(value & d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int64)(value & d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int64<NumCIL.Int64.And>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush_UInt64_TypedImpl<C>(NumCIL.UInt64.And op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt64*)f1.ptr;
                        var vd = (System.UInt64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt64)(value & d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt64)(value & d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt64)(value & d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt64<NumCIL.UInt64.And>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Boolean> UFunc_Reduce_Inner_Flush_Boolean_TypedImpl<C>(NumCIL.Boolean.And op, long axis, NdArray<System.Boolean> in1, NdArray<System.Boolean> @out)
            where C : struct, IBinaryOp<System.Boolean>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Boolean<CopyOp<System.Boolean>>(new CopyOp<System.Boolean>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Boolean*)f1.ptr;
                        var vd = (System.Boolean*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Boolean value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Boolean)(value & d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Boolean value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Boolean)(value & d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Boolean value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Boolean)(value & d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Boolean> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Boolean<CopyOp<System.Boolean>>(new CopyOp<System.Boolean>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Boolean<NumCIL.Boolean.And>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush_SByte_TypedImpl<C>(NumCIL.Int8.Or op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.SByte*)f1.ptr;
                        var vd = (System.SByte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.SByte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.SByte)(value | d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.SByte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.SByte)(value | d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.SByte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.SByte)(value | d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.SByte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_SByte<NumCIL.Int8.Or>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush_Byte_TypedImpl<C>(NumCIL.UInt8.Or op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Byte*)f1.ptr;
                        var vd = (System.Byte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Byte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Byte)(value | d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Byte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Byte)(value | d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Byte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Byte)(value | d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Byte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Byte<NumCIL.UInt8.Or>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush_Int16_TypedImpl<C>(NumCIL.Int16.Or op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int16*)f1.ptr;
                        var vd = (System.Int16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int16)(value | d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int16)(value | d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int16)(value | d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int16<NumCIL.Int16.Or>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush_UInt16_TypedImpl<C>(NumCIL.UInt16.Or op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt16*)f1.ptr;
                        var vd = (System.UInt16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt16)(value | d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt16)(value | d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt16)(value | d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt16<NumCIL.UInt16.Or>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush_Int32_TypedImpl<C>(NumCIL.Int32.Or op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int32*)f1.ptr;
                        var vd = (System.Int32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int32)(value | d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int32)(value | d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int32)(value | d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int32<NumCIL.Int32.Or>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush_UInt32_TypedImpl<C>(NumCIL.UInt32.Or op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt32*)f1.ptr;
                        var vd = (System.UInt32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt32)(value | d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt32)(value | d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt32)(value | d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt32<NumCIL.UInt32.Or>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush_Int64_TypedImpl<C>(NumCIL.Int64.Or op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int64*)f1.ptr;
                        var vd = (System.Int64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int64)(value | d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int64)(value | d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int64)(value | d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int64<NumCIL.Int64.Or>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush_UInt64_TypedImpl<C>(NumCIL.UInt64.Or op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt64*)f1.ptr;
                        var vd = (System.UInt64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt64)(value | d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt64)(value | d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt64)(value | d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt64<NumCIL.UInt64.Or>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Boolean> UFunc_Reduce_Inner_Flush_Boolean_TypedImpl<C>(NumCIL.Boolean.Or op, long axis, NdArray<System.Boolean> in1, NdArray<System.Boolean> @out)
            where C : struct, IBinaryOp<System.Boolean>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Boolean<CopyOp<System.Boolean>>(new CopyOp<System.Boolean>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Boolean*)f1.ptr;
                        var vd = (System.Boolean*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Boolean value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Boolean)(value | d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Boolean value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Boolean)(value | d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Boolean value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Boolean)(value | d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Boolean> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Boolean<CopyOp<System.Boolean>>(new CopyOp<System.Boolean>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Boolean<NumCIL.Boolean.Or>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush_SByte_TypedImpl<C>(NumCIL.Int8.Xor op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.SByte*)f1.ptr;
                        var vd = (System.SByte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.SByte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.SByte)(value ^ d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.SByte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.SByte)(value ^ d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.SByte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.SByte)(value ^ d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.SByte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_SByte<CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_SByte<NumCIL.Int8.Xor>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush_Byte_TypedImpl<C>(NumCIL.UInt8.Xor op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Byte*)f1.ptr;
                        var vd = (System.Byte*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Byte value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Byte)(value ^ d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Byte value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Byte)(value ^ d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Byte value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Byte)(value ^ d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Byte> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Byte<CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Byte<NumCIL.UInt8.Xor>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush_Int16_TypedImpl<C>(NumCIL.Int16.Xor op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int16*)f1.ptr;
                        var vd = (System.Int16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int16)(value ^ d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int16)(value ^ d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int16)(value ^ d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int16<CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int16<NumCIL.Int16.Xor>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush_UInt16_TypedImpl<C>(NumCIL.UInt16.Xor op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt16*)f1.ptr;
                        var vd = (System.UInt16*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt16 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt16)(value ^ d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt16)(value ^ d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt16 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt16)(value ^ d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt16> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt16<CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt16<NumCIL.UInt16.Xor>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush_Int32_TypedImpl<C>(NumCIL.Int32.Xor op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int32*)f1.ptr;
                        var vd = (System.Int32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int32)(value ^ d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int32)(value ^ d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int32)(value ^ d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int32<CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int32<NumCIL.Int32.Xor>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush_UInt32_TypedImpl<C>(NumCIL.UInt32.Xor op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt32*)f1.ptr;
                        var vd = (System.UInt32*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt32 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt32)(value ^ d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt32)(value ^ d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt32 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt32)(value ^ d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt32> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt32<CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt32<NumCIL.UInt32.Xor>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush_Int64_TypedImpl<C>(NumCIL.Int64.Xor op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Int64*)f1.ptr;
                        var vd = (System.Int64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Int64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Int64)(value ^ d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Int64)(value ^ d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Int64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Int64)(value ^ d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Int64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Int64<CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Int64<NumCIL.Int64.Xor>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush_UInt64_TypedImpl<C>(NumCIL.UInt64.Xor op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.UInt64*)f1.ptr;
                        var vd = (System.UInt64*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.UInt64 value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.UInt64)(value ^ d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.UInt64)(value ^ d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.UInt64 value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.UInt64)(value ^ d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.UInt64> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_UInt64<CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_UInt64<NumCIL.UInt64.Xor>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Unsafe implementation of the reduce operation
        /// </summary>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Boolean> UFunc_Reduce_Inner_Flush_Boolean_TypedImpl<C>(NumCIL.Boolean.Xor op, long axis, NdArray<System.Boolean> in1, NdArray<System.Boolean> @out)
            where C : struct, IBinaryOp<System.Boolean>
        {
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                Apply.UFunc_Op_Inner_Unary_Flush_Boolean<CopyOp<System.Boolean>>(new CopyOp<System.Boolean>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), ref @out);
            }
            else
            {
                unsafe
                {
                    using (var f1 = new Pinner(in1.DataAccessor))
                    using (var f2 = new Pinner(@out.DataAccessor))
                    {
                        var d = (System.Boolean*)f1.ptr;
                        var vd = (System.Boolean*)f2.ptr;

                        //Simple case, reduce 1D array to scalar value
                        if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                        {
                            long stride = in1.Shape.Dimensions[0].Stride;
                            long ix = in1.Shape.Offset;
                            long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                            System.Boolean value = d[ix];

                            for (long i = ix + stride; i < limit; i += stride)
                                value = (System.Boolean)(value ^ d[i]);

                            vd[@out.Shape.Offset] = value;
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 0 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            long outerCount = in1.Shape.Dimensions[0].Length;

                            for (long i = 0; i < in1.Shape.Dimensions[1].Length; i++)
                            {
                                System.Boolean value = d[ix];

                                long nx = ix;
                                for (long j = 1; j < outerCount; j++)
                                {
                                    nx += strideOuter;
                                    value = (System.Boolean)(value ^ d[nx]);
                                }

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideInner;
                            }
                        }
                        //Simple case, reduce 2D array to 1D
                        else if (axis == 1 && in1.Shape.Dimensions.LongLength == 2)
                        {
                            long strideInner = in1.Shape.Dimensions[1].Stride;
                            long strideOuter = in1.Shape.Dimensions[0].Stride;

                            long ix = in1.Shape.Offset;
                            long limitInner = strideInner * in1.Shape.Dimensions[1].Length;

                            long ox = @out.Shape.Offset;
                            long strideRes = @out.Shape.Dimensions[0].Stride;

                            for (long i = 0; i < in1.Shape.Dimensions[0].Length; i++)
                            {
                                System.Boolean value = d[ix];

                                for (long j = strideInner; j < limitInner; j += strideInner)
                                    value = (System.Boolean)(value ^ d[j + ix]);

                                vd[ox] = value;
                                ox += strideRes;

                                ix += strideOuter;
                            }
                        }
                        //General case
                        else
                        {
                            long size = in1.Shape.Dimensions[axis].Length;
                            NdArray<System.Boolean> vl = @out.Subview(Range.NewAxis, axis);

                            //Initially we just copy the value
                            Apply.UFunc_Op_Inner_Unary_Flush_Boolean<CopyOp<System.Boolean>>(new CopyOp<System.Boolean>(), in1.Subview(Range.El(0), axis), ref vl);

                            //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                            for (long j = 1; j < size; j++)
                            {
                                //Select the new dimension
                                //Apply the operation
                                Apply.UFunc_Op_Inner_Binary_Flush_Boolean<NumCIL.Boolean.Xor>(op, vl, in1.Subview(Range.El(j), axis), ref vl);
                            }
                        }
                    }
                }
            }
            return @out;
        }
	}
}
