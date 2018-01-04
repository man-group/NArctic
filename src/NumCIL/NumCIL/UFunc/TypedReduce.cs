
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
    /// <summary>
    /// Universal function implementations (elementwise operations)
    /// </summary>
    public partial class UFunc
    {
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<T> UFunc_Reduce_Inner_Flush<T, C>(C op, long axis, NdArray<T> in1, NdArray<T> @out)
            where C : struct, IBinaryOp<T>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<T, C>(op, axis, in1, @out))
                return @out;

			if (UFunc_Reduce_Inner_Flush_Typed<T, C>(op, axis, in1, @out))
				return @out;
#if DEBUG
			Console.WriteLine("Generic Reduce method C for T, with op = {0}, T = {1}", op.GetType(), typeof(T));
#endif
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<T, CopyOp<T>>(new CopyOp<T>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

                    for (long i = ix + stride; i < limit; i += stride)
                        value = (T)op.Op(value, d[i]);

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
                        var value = d[ix];

                        long nx = ix;
                        for (long j = 1; j < outerCount; j++)
                        {
                            nx += strideOuter;
                            value = (T)op.Op(value, d[nx]);
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
                        var value = d[ix];

                        for (long j = strideInner; j < limitInner; j += strideInner)
                            value = (T)op.Op(value, d[j + ix]);

                        vd[ox] = value;
                        ox += strideRes;

                        ix += strideOuter;
                    }
                }                
                //General case
                else
                {
                    long size = in1.Shape.Dimensions[axis].Length;
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<T, CopyOp<T>>(new CopyOp<T>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<T, C>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int8.Add op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.SByte, NumCIL.Int8.Add>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.SByte, NumCIL.Int8.Add>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt8.Add op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Byte, NumCIL.UInt8.Add>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Byte, NumCIL.UInt8.Add>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int16.Add op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int16, NumCIL.Int16.Add>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int16, NumCIL.Int16.Add>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt16.Add op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt16, NumCIL.UInt16.Add>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt16, NumCIL.UInt16.Add>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int32.Add op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int32, NumCIL.Int32.Add>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int32, NumCIL.Int32.Add>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt32.Add op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt32, NumCIL.UInt32.Add>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt32, NumCIL.UInt32.Add>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int64.Add op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int64, NumCIL.Int64.Add>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int64, NumCIL.Int64.Add>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt64.Add op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt64, NumCIL.UInt64.Add>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt64, NumCIL.UInt64.Add>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Float.Add op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Single, NumCIL.Float.Add>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Single, NumCIL.Float.Add>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Double.Add op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Double, NumCIL.Double.Add>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Double, NumCIL.Double.Add>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Numerics.Complex> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Complex128.Add op, long axis, NdArray<System.Numerics.Complex> in1, NdArray<System.Numerics.Complex> @out)
            where C : struct, IBinaryOp<System.Numerics.Complex>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Numerics.Complex, NumCIL.Complex128.Add>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Numerics.Complex, CopyOp<System.Numerics.Complex>>(new CopyOp<System.Numerics.Complex>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

                    for (long i = ix + stride; i < limit; i += stride)
                        value = (System.Numerics.Complex)(value + d[i]);

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
                        var value = d[ix];

                        long nx = ix;
                        for (long j = 1; j < outerCount; j++)
                        {
                            nx += strideOuter;
                            value = (System.Numerics.Complex)(value + d[nx]);
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
                        var value = d[ix];

                        for (long j = strideInner; j < limitInner; j += strideInner)
                            value = (System.Numerics.Complex)(value + d[j + ix]);

                        vd[ox] = value;
                        ox += strideRes;

                        ix += strideOuter;
                    }
                }                
                //General case
                else
                {
                    long size = in1.Shape.Dimensions[axis].Length;
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Numerics.Complex, CopyOp<System.Numerics.Complex>>(new CopyOp<System.Numerics.Complex>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Numerics.Complex, NumCIL.Complex128.Add>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<NumCIL.Complex64.DataType> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Complex64.Add op, long axis, NdArray<NumCIL.Complex64.DataType> in1, NdArray<NumCIL.Complex64.DataType> @out)
            where C : struct, IBinaryOp<NumCIL.Complex64.DataType>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<NumCIL.Complex64.DataType, NumCIL.Complex64.Add>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<NumCIL.Complex64.DataType, CopyOp<NumCIL.Complex64.DataType>>(new CopyOp<NumCIL.Complex64.DataType>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

                    for (long i = ix + stride; i < limit; i += stride)
                        value = (NumCIL.Complex64.DataType)(value + d[i]);

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
                        var value = d[ix];

                        long nx = ix;
                        for (long j = 1; j < outerCount; j++)
                        {
                            nx += strideOuter;
                            value = (NumCIL.Complex64.DataType)(value + d[nx]);
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
                        var value = d[ix];

                        for (long j = strideInner; j < limitInner; j += strideInner)
                            value = (NumCIL.Complex64.DataType)(value + d[j + ix]);

                        vd[ox] = value;
                        ox += strideRes;

                        ix += strideOuter;
                    }
                }                
                //General case
                else
                {
                    long size = in1.Shape.Dimensions[axis].Length;
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<NumCIL.Complex64.DataType, CopyOp<NumCIL.Complex64.DataType>>(new CopyOp<NumCIL.Complex64.DataType>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<NumCIL.Complex64.DataType, NumCIL.Complex64.Add>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int8.Sub op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.SByte, NumCIL.Int8.Sub>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.SByte, NumCIL.Int8.Sub>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt8.Sub op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Byte, NumCIL.UInt8.Sub>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Byte, NumCIL.UInt8.Sub>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int16.Sub op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int16, NumCIL.Int16.Sub>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int16, NumCIL.Int16.Sub>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt16.Sub op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt16, NumCIL.UInt16.Sub>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt16, NumCIL.UInt16.Sub>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int32.Sub op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int32, NumCIL.Int32.Sub>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int32, NumCIL.Int32.Sub>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt32.Sub op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt32, NumCIL.UInt32.Sub>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt32, NumCIL.UInt32.Sub>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int64.Sub op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int64, NumCIL.Int64.Sub>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int64, NumCIL.Int64.Sub>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt64.Sub op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt64, NumCIL.UInt64.Sub>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt64, NumCIL.UInt64.Sub>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Float.Sub op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Single, NumCIL.Float.Sub>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Single, NumCIL.Float.Sub>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Double.Sub op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Double, NumCIL.Double.Sub>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Double, NumCIL.Double.Sub>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Numerics.Complex> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Complex128.Sub op, long axis, NdArray<System.Numerics.Complex> in1, NdArray<System.Numerics.Complex> @out)
            where C : struct, IBinaryOp<System.Numerics.Complex>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Numerics.Complex, NumCIL.Complex128.Sub>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Numerics.Complex, CopyOp<System.Numerics.Complex>>(new CopyOp<System.Numerics.Complex>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

                    for (long i = ix + stride; i < limit; i += stride)
                        value = (System.Numerics.Complex)(value - d[i]);

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
                        var value = d[ix];

                        long nx = ix;
                        for (long j = 1; j < outerCount; j++)
                        {
                            nx += strideOuter;
                            value = (System.Numerics.Complex)(value - d[nx]);
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
                        var value = d[ix];

                        for (long j = strideInner; j < limitInner; j += strideInner)
                            value = (System.Numerics.Complex)(value - d[j + ix]);

                        vd[ox] = value;
                        ox += strideRes;

                        ix += strideOuter;
                    }
                }                
                //General case
                else
                {
                    long size = in1.Shape.Dimensions[axis].Length;
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Numerics.Complex, CopyOp<System.Numerics.Complex>>(new CopyOp<System.Numerics.Complex>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Numerics.Complex, NumCIL.Complex128.Sub>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<NumCIL.Complex64.DataType> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Complex64.Sub op, long axis, NdArray<NumCIL.Complex64.DataType> in1, NdArray<NumCIL.Complex64.DataType> @out)
            where C : struct, IBinaryOp<NumCIL.Complex64.DataType>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<NumCIL.Complex64.DataType, NumCIL.Complex64.Sub>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<NumCIL.Complex64.DataType, CopyOp<NumCIL.Complex64.DataType>>(new CopyOp<NumCIL.Complex64.DataType>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

                    for (long i = ix + stride; i < limit; i += stride)
                        value = (NumCIL.Complex64.DataType)(value - d[i]);

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
                        var value = d[ix];

                        long nx = ix;
                        for (long j = 1; j < outerCount; j++)
                        {
                            nx += strideOuter;
                            value = (NumCIL.Complex64.DataType)(value - d[nx]);
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
                        var value = d[ix];

                        for (long j = strideInner; j < limitInner; j += strideInner)
                            value = (NumCIL.Complex64.DataType)(value - d[j + ix]);

                        vd[ox] = value;
                        ox += strideRes;

                        ix += strideOuter;
                    }
                }                
                //General case
                else
                {
                    long size = in1.Shape.Dimensions[axis].Length;
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<NumCIL.Complex64.DataType, CopyOp<NumCIL.Complex64.DataType>>(new CopyOp<NumCIL.Complex64.DataType>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<NumCIL.Complex64.DataType, NumCIL.Complex64.Sub>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int8.Mul op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.SByte, NumCIL.Int8.Mul>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.SByte, NumCIL.Int8.Mul>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt8.Mul op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Byte, NumCIL.UInt8.Mul>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Byte, NumCIL.UInt8.Mul>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int16.Mul op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int16, NumCIL.Int16.Mul>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int16, NumCIL.Int16.Mul>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt16.Mul op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt16, NumCIL.UInt16.Mul>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt16, NumCIL.UInt16.Mul>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int32.Mul op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int32, NumCIL.Int32.Mul>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int32, NumCIL.Int32.Mul>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt32.Mul op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt32, NumCIL.UInt32.Mul>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt32, NumCIL.UInt32.Mul>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int64.Mul op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int64, NumCIL.Int64.Mul>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int64, NumCIL.Int64.Mul>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt64.Mul op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt64, NumCIL.UInt64.Mul>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt64, NumCIL.UInt64.Mul>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Float.Mul op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Single, NumCIL.Float.Mul>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Single, NumCIL.Float.Mul>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Double.Mul op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Double, NumCIL.Double.Mul>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Double, NumCIL.Double.Mul>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Numerics.Complex> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Complex128.Mul op, long axis, NdArray<System.Numerics.Complex> in1, NdArray<System.Numerics.Complex> @out)
            where C : struct, IBinaryOp<System.Numerics.Complex>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Numerics.Complex, NumCIL.Complex128.Mul>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Numerics.Complex, CopyOp<System.Numerics.Complex>>(new CopyOp<System.Numerics.Complex>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

                    for (long i = ix + stride; i < limit; i += stride)
                        value = (System.Numerics.Complex)(value * d[i]);

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
                        var value = d[ix];

                        long nx = ix;
                        for (long j = 1; j < outerCount; j++)
                        {
                            nx += strideOuter;
                            value = (System.Numerics.Complex)(value * d[nx]);
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
                        var value = d[ix];

                        for (long j = strideInner; j < limitInner; j += strideInner)
                            value = (System.Numerics.Complex)(value * d[j + ix]);

                        vd[ox] = value;
                        ox += strideRes;

                        ix += strideOuter;
                    }
                }                
                //General case
                else
                {
                    long size = in1.Shape.Dimensions[axis].Length;
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Numerics.Complex, CopyOp<System.Numerics.Complex>>(new CopyOp<System.Numerics.Complex>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Numerics.Complex, NumCIL.Complex128.Mul>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<NumCIL.Complex64.DataType> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Complex64.Mul op, long axis, NdArray<NumCIL.Complex64.DataType> in1, NdArray<NumCIL.Complex64.DataType> @out)
            where C : struct, IBinaryOp<NumCIL.Complex64.DataType>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<NumCIL.Complex64.DataType, NumCIL.Complex64.Mul>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<NumCIL.Complex64.DataType, CopyOp<NumCIL.Complex64.DataType>>(new CopyOp<NumCIL.Complex64.DataType>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

                    for (long i = ix + stride; i < limit; i += stride)
                        value = (NumCIL.Complex64.DataType)(value * d[i]);

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
                        var value = d[ix];

                        long nx = ix;
                        for (long j = 1; j < outerCount; j++)
                        {
                            nx += strideOuter;
                            value = (NumCIL.Complex64.DataType)(value * d[nx]);
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
                        var value = d[ix];

                        for (long j = strideInner; j < limitInner; j += strideInner)
                            value = (NumCIL.Complex64.DataType)(value * d[j + ix]);

                        vd[ox] = value;
                        ox += strideRes;

                        ix += strideOuter;
                    }
                }                
                //General case
                else
                {
                    long size = in1.Shape.Dimensions[axis].Length;
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<NumCIL.Complex64.DataType, CopyOp<NumCIL.Complex64.DataType>>(new CopyOp<NumCIL.Complex64.DataType>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<NumCIL.Complex64.DataType, NumCIL.Complex64.Mul>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int8.Div op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.SByte, NumCIL.Int8.Div>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.SByte, NumCIL.Int8.Div>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt8.Div op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Byte, NumCIL.UInt8.Div>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Byte, NumCIL.UInt8.Div>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int16.Div op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int16, NumCIL.Int16.Div>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int16, NumCIL.Int16.Div>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt16.Div op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt16, NumCIL.UInt16.Div>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt16, NumCIL.UInt16.Div>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int32.Div op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int32, NumCIL.Int32.Div>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int32, NumCIL.Int32.Div>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt32.Div op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt32, NumCIL.UInt32.Div>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt32, NumCIL.UInt32.Div>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int64.Div op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int64, NumCIL.Int64.Div>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int64, NumCIL.Int64.Div>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt64.Div op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt64, NumCIL.UInt64.Div>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt64, NumCIL.UInt64.Div>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Float.Div op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Single, NumCIL.Float.Div>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Single, NumCIL.Float.Div>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Double.Div op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Double, NumCIL.Double.Div>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Double, NumCIL.Double.Div>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Numerics.Complex> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Complex128.Div op, long axis, NdArray<System.Numerics.Complex> in1, NdArray<System.Numerics.Complex> @out)
            where C : struct, IBinaryOp<System.Numerics.Complex>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Numerics.Complex, NumCIL.Complex128.Div>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Numerics.Complex, CopyOp<System.Numerics.Complex>>(new CopyOp<System.Numerics.Complex>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

                    for (long i = ix + stride; i < limit; i += stride)
                        value = (System.Numerics.Complex)(value / d[i]);

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
                        var value = d[ix];

                        long nx = ix;
                        for (long j = 1; j < outerCount; j++)
                        {
                            nx += strideOuter;
                            value = (System.Numerics.Complex)(value / d[nx]);
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
                        var value = d[ix];

                        for (long j = strideInner; j < limitInner; j += strideInner)
                            value = (System.Numerics.Complex)(value / d[j + ix]);

                        vd[ox] = value;
                        ox += strideRes;

                        ix += strideOuter;
                    }
                }                
                //General case
                else
                {
                    long size = in1.Shape.Dimensions[axis].Length;
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Numerics.Complex, CopyOp<System.Numerics.Complex>>(new CopyOp<System.Numerics.Complex>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Numerics.Complex, NumCIL.Complex128.Div>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<NumCIL.Complex64.DataType> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Complex64.Div op, long axis, NdArray<NumCIL.Complex64.DataType> in1, NdArray<NumCIL.Complex64.DataType> @out)
            where C : struct, IBinaryOp<NumCIL.Complex64.DataType>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<NumCIL.Complex64.DataType, NumCIL.Complex64.Div>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<NumCIL.Complex64.DataType, CopyOp<NumCIL.Complex64.DataType>>(new CopyOp<NumCIL.Complex64.DataType>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

                    for (long i = ix + stride; i < limit; i += stride)
                        value = (NumCIL.Complex64.DataType)(value / d[i]);

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
                        var value = d[ix];

                        long nx = ix;
                        for (long j = 1; j < outerCount; j++)
                        {
                            nx += strideOuter;
                            value = (NumCIL.Complex64.DataType)(value / d[nx]);
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
                        var value = d[ix];

                        for (long j = strideInner; j < limitInner; j += strideInner)
                            value = (NumCIL.Complex64.DataType)(value / d[j + ix]);

                        vd[ox] = value;
                        ox += strideRes;

                        ix += strideOuter;
                    }
                }                
                //General case
                else
                {
                    long size = in1.Shape.Dimensions[axis].Length;
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<NumCIL.Complex64.DataType, CopyOp<NumCIL.Complex64.DataType>>(new CopyOp<NumCIL.Complex64.DataType>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<NumCIL.Complex64.DataType, NumCIL.Complex64.Div>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int8.Mod op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.SByte, NumCIL.Int8.Mod>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.SByte, NumCIL.Int8.Mod>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt8.Mod op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Byte, NumCIL.UInt8.Mod>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Byte, NumCIL.UInt8.Mod>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int16.Mod op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int16, NumCIL.Int16.Mod>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int16, NumCIL.Int16.Mod>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt16.Mod op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt16, NumCIL.UInt16.Mod>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt16, NumCIL.UInt16.Mod>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int32.Mod op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int32, NumCIL.Int32.Mod>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int32, NumCIL.Int32.Mod>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt32.Mod op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt32, NumCIL.UInt32.Mod>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt32, NumCIL.UInt32.Mod>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int64.Mod op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int64, NumCIL.Int64.Mod>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int64, NumCIL.Int64.Mod>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt64.Mod op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt64, NumCIL.UInt64.Mod>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt64, NumCIL.UInt64.Mod>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Float.Mod op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Single, NumCIL.Float.Mod>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Single, NumCIL.Float.Mod>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Double.Mod op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Double, NumCIL.Double.Mod>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Double, NumCIL.Double.Mod>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int8.Min op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.SByte, NumCIL.Int8.Min>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.SByte, NumCIL.Int8.Min>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt8.Min op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Byte, NumCIL.UInt8.Min>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Byte, NumCIL.UInt8.Min>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int16.Min op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int16, NumCIL.Int16.Min>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int16, NumCIL.Int16.Min>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt16.Min op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt16, NumCIL.UInt16.Min>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt16, NumCIL.UInt16.Min>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int32.Min op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int32, NumCIL.Int32.Min>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int32, NumCIL.Int32.Min>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt32.Min op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt32, NumCIL.UInt32.Min>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt32, NumCIL.UInt32.Min>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int64.Min op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int64, NumCIL.Int64.Min>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int64, NumCIL.Int64.Min>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt64.Min op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt64, NumCIL.UInt64.Min>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt64, NumCIL.UInt64.Min>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Float.Min op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Single, NumCIL.Float.Min>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Single, NumCIL.Float.Min>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Double.Min op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Double, NumCIL.Double.Min>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Double, NumCIL.Double.Min>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int8.Max op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.SByte, NumCIL.Int8.Max>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.SByte, NumCIL.Int8.Max>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt8.Max op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Byte, NumCIL.UInt8.Max>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Byte, NumCIL.UInt8.Max>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int16.Max op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int16, NumCIL.Int16.Max>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int16, NumCIL.Int16.Max>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt16.Max op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt16, NumCIL.UInt16.Max>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt16, NumCIL.UInt16.Max>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int32.Max op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int32, NumCIL.Int32.Max>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int32, NumCIL.Int32.Max>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt32.Max op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt32, NumCIL.UInt32.Max>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt32, NumCIL.UInt32.Max>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int64.Max op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int64, NumCIL.Int64.Max>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int64, NumCIL.Int64.Max>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt64.Max op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt64, NumCIL.UInt64.Max>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt64, NumCIL.UInt64.Max>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Float.Max op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Single, NumCIL.Float.Max>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Single, NumCIL.Float.Max>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Double.Max op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Double, NumCIL.Double.Max>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Double, NumCIL.Double.Max>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int8.Pow op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.SByte, NumCIL.Int8.Pow>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.SByte, NumCIL.Int8.Pow>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt8.Pow op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Byte, NumCIL.UInt8.Pow>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Byte, NumCIL.UInt8.Pow>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int16.Pow op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int16, NumCIL.Int16.Pow>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int16, NumCIL.Int16.Pow>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt16.Pow op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt16, NumCIL.UInt16.Pow>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt16, NumCIL.UInt16.Pow>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int32.Pow op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int32, NumCIL.Int32.Pow>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int32, NumCIL.Int32.Pow>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt32.Pow op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt32, NumCIL.UInt32.Pow>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt32, NumCIL.UInt32.Pow>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int64.Pow op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int64, NumCIL.Int64.Pow>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int64, NumCIL.Int64.Pow>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt64.Pow op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt64, NumCIL.UInt64.Pow>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt64, NumCIL.UInt64.Pow>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Single> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Float.Pow op, long axis, NdArray<System.Single> in1, NdArray<System.Single> @out)
            where C : struct, IBinaryOp<System.Single>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Single, NumCIL.Float.Pow>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Single, CopyOp<System.Single>>(new CopyOp<System.Single>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Single, NumCIL.Float.Pow>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Double> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Double.Pow op, long axis, NdArray<System.Double> in1, NdArray<System.Double> @out)
            where C : struct, IBinaryOp<System.Double>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Double, NumCIL.Double.Pow>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Double, CopyOp<System.Double>>(new CopyOp<System.Double>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Double, NumCIL.Double.Pow>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Numerics.Complex> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Complex128.Pow op, long axis, NdArray<System.Numerics.Complex> in1, NdArray<System.Numerics.Complex> @out)
            where C : struct, IBinaryOp<System.Numerics.Complex>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Numerics.Complex, NumCIL.Complex128.Pow>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Numerics.Complex, CopyOp<System.Numerics.Complex>>(new CopyOp<System.Numerics.Complex>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

                    for (long i = ix + stride; i < limit; i += stride)
                        value = (System.Numerics.Complex)System.Numerics.Complex.Pow(value, d[i]);

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
                        var value = d[ix];

                        long nx = ix;
                        for (long j = 1; j < outerCount; j++)
                        {
                            nx += strideOuter;
                            value = (System.Numerics.Complex)System.Numerics.Complex.Pow(value, d[nx]);
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
                        var value = d[ix];

                        for (long j = strideInner; j < limitInner; j += strideInner)
                            value = (System.Numerics.Complex)System.Numerics.Complex.Pow(value, d[j + ix]);

                        vd[ox] = value;
                        ox += strideRes;

                        ix += strideOuter;
                    }
                }                
                //General case
                else
                {
                    long size = in1.Shape.Dimensions[axis].Length;
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Numerics.Complex, CopyOp<System.Numerics.Complex>>(new CopyOp<System.Numerics.Complex>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Numerics.Complex, NumCIL.Complex128.Pow>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<NumCIL.Complex64.DataType> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Complex64.Pow op, long axis, NdArray<NumCIL.Complex64.DataType> in1, NdArray<NumCIL.Complex64.DataType> @out)
            where C : struct, IBinaryOp<NumCIL.Complex64.DataType>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<NumCIL.Complex64.DataType, NumCIL.Complex64.Pow>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<NumCIL.Complex64.DataType, CopyOp<NumCIL.Complex64.DataType>>(new CopyOp<NumCIL.Complex64.DataType>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

                    for (long i = ix + stride; i < limit; i += stride)
                        value = (NumCIL.Complex64.DataType)NumCIL.Complex64.DataType.Pow(value, d[i]);

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
                        var value = d[ix];

                        long nx = ix;
                        for (long j = 1; j < outerCount; j++)
                        {
                            nx += strideOuter;
                            value = (NumCIL.Complex64.DataType)NumCIL.Complex64.DataType.Pow(value, d[nx]);
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
                        var value = d[ix];

                        for (long j = strideInner; j < limitInner; j += strideInner)
                            value = (NumCIL.Complex64.DataType)NumCIL.Complex64.DataType.Pow(value, d[j + ix]);

                        vd[ox] = value;
                        ox += strideRes;

                        ix += strideOuter;
                    }
                }                
                //General case
                else
                {
                    long size = in1.Shape.Dimensions[axis].Length;
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<NumCIL.Complex64.DataType, CopyOp<NumCIL.Complex64.DataType>>(new CopyOp<NumCIL.Complex64.DataType>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<NumCIL.Complex64.DataType, NumCIL.Complex64.Pow>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int8.And op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.SByte, NumCIL.Int8.And>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.SByte, NumCIL.Int8.And>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt8.And op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Byte, NumCIL.UInt8.And>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Byte, NumCIL.UInt8.And>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int16.And op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int16, NumCIL.Int16.And>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int16, NumCIL.Int16.And>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt16.And op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt16, NumCIL.UInt16.And>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt16, NumCIL.UInt16.And>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int32.And op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int32, NumCIL.Int32.And>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int32, NumCIL.Int32.And>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt32.And op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt32, NumCIL.UInt32.And>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt32, NumCIL.UInt32.And>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int64.And op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int64, NumCIL.Int64.And>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int64, NumCIL.Int64.And>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt64.And op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt64, NumCIL.UInt64.And>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt64, NumCIL.UInt64.And>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Boolean> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Boolean.And op, long axis, NdArray<System.Boolean> in1, NdArray<System.Boolean> @out)
            where C : struct, IBinaryOp<System.Boolean>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Boolean, NumCIL.Boolean.And>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Boolean, CopyOp<System.Boolean>>(new CopyOp<System.Boolean>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Boolean, CopyOp<System.Boolean>>(new CopyOp<System.Boolean>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Boolean, NumCIL.Boolean.And>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int8.Or op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.SByte, NumCIL.Int8.Or>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.SByte, NumCIL.Int8.Or>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt8.Or op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Byte, NumCIL.UInt8.Or>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Byte, NumCIL.UInt8.Or>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int16.Or op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int16, NumCIL.Int16.Or>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int16, NumCIL.Int16.Or>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt16.Or op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt16, NumCIL.UInt16.Or>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt16, NumCIL.UInt16.Or>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int32.Or op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int32, NumCIL.Int32.Or>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int32, NumCIL.Int32.Or>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt32.Or op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt32, NumCIL.UInt32.Or>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt32, NumCIL.UInt32.Or>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int64.Or op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int64, NumCIL.Int64.Or>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int64, NumCIL.Int64.Or>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt64.Or op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt64, NumCIL.UInt64.Or>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt64, NumCIL.UInt64.Or>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Boolean> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Boolean.Or op, long axis, NdArray<System.Boolean> in1, NdArray<System.Boolean> @out)
            where C : struct, IBinaryOp<System.Boolean>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Boolean, NumCIL.Boolean.Or>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Boolean, CopyOp<System.Boolean>>(new CopyOp<System.Boolean>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Boolean, CopyOp<System.Boolean>>(new CopyOp<System.Boolean>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Boolean, NumCIL.Boolean.Or>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.SByte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int8.Xor op, long axis, NdArray<System.SByte> in1, NdArray<System.SByte> @out)
            where C : struct, IBinaryOp<System.SByte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.SByte, NumCIL.Int8.Xor>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.SByte, CopyOp<System.SByte>>(new CopyOp<System.SByte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.SByte, NumCIL.Int8.Xor>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Byte> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt8.Xor op, long axis, NdArray<System.Byte> in1, NdArray<System.Byte> @out)
            where C : struct, IBinaryOp<System.Byte>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Byte, NumCIL.UInt8.Xor>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Byte, CopyOp<System.Byte>>(new CopyOp<System.Byte>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Byte, NumCIL.UInt8.Xor>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int16.Xor op, long axis, NdArray<System.Int16> in1, NdArray<System.Int16> @out)
            where C : struct, IBinaryOp<System.Int16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int16, NumCIL.Int16.Xor>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int16, CopyOp<System.Int16>>(new CopyOp<System.Int16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int16, NumCIL.Int16.Xor>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt16> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt16.Xor op, long axis, NdArray<System.UInt16> in1, NdArray<System.UInt16> @out)
            where C : struct, IBinaryOp<System.UInt16>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt16, NumCIL.UInt16.Xor>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt16, CopyOp<System.UInt16>>(new CopyOp<System.UInt16>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt16, NumCIL.UInt16.Xor>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int32.Xor op, long axis, NdArray<System.Int32> in1, NdArray<System.Int32> @out)
            where C : struct, IBinaryOp<System.Int32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int32, NumCIL.Int32.Xor>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int32, CopyOp<System.Int32>>(new CopyOp<System.Int32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int32, NumCIL.Int32.Xor>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt32> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt32.Xor op, long axis, NdArray<System.UInt32> in1, NdArray<System.UInt32> @out)
            where C : struct, IBinaryOp<System.UInt32>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt32, NumCIL.UInt32.Xor>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt32, CopyOp<System.UInt32>>(new CopyOp<System.UInt32>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt32, NumCIL.UInt32.Xor>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Int64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Int64.Xor op, long axis, NdArray<System.Int64> in1, NdArray<System.Int64> @out)
            where C : struct, IBinaryOp<System.Int64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Int64, NumCIL.Int64.Xor>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Int64, CopyOp<System.Int64>>(new CopyOp<System.Int64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Int64, NumCIL.Int64.Xor>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.UInt64> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.UInt64.Xor op, long axis, NdArray<System.UInt64> in1, NdArray<System.UInt64> @out)
            where C : struct, IBinaryOp<System.UInt64>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.UInt64, NumCIL.UInt64.Xor>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.UInt64, CopyOp<System.UInt64>>(new CopyOp<System.UInt64>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.UInt64, NumCIL.UInt64.Xor>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }
        /// <summary>
        /// Actually executes a reduce operation in CIL by retrieving the data and executing the <see cref="T:NumCIL.IBinaryOp{0}"/> on each element in the given dimension.
        /// This implementation is optimized for use with up to 2 dimensions, but works for any size dimension.
        /// This method is optimized for 64bit processors, using the .Net 4.0 runtime.
        /// </summary>
        /// <typeparam name="T">The type of data to operate on</typeparam>
        /// <typeparam name="C">The type of operation to reduce with</typeparam>
        /// <param name="op">The instance of the operation to reduce with</param>
        /// <param name="in1">The input argument</param>
        /// <param name="axis">The axis to reduce</param>
        /// <param name="out">The output target</param>
        /// <returns>The output target</returns>
        private static NdArray<System.Boolean> UFunc_Reduce_Inner_Flush<T, C>(NumCIL.Boolean.Xor op, long axis, NdArray<System.Boolean> in1, NdArray<System.Boolean> @out)
            where C : struct, IBinaryOp<System.Boolean>
        {
            if (UnsafeAPI.UFunc_Reduce_Inner_Flush_Unsafe<System.Boolean, NumCIL.Boolean.Xor>(op, axis, in1, @out))
                return @out;
            if (axis < 0)
                axis = in1.Shape.Dimensions.LongLength - axis;

            //Basic case, just return a reduced array
            if (in1.Shape.Dimensions[axis].Length == 1 && in1.Shape.Dimensions.LongLength > 1)
            {
                //TODO: If both in and out use the same array, just return a reshaped in
                long j = 0;
                var sizes = in1.Shape.Dimensions.Where(x => j++ != axis).ToArray();
                UFunc_Op_Inner_Unary_Flush<System.Boolean, CopyOp<System.Boolean>>(new CopyOp<System.Boolean>(), in1.Reshape(new Shape(sizes, in1.Shape.Offset)), @out);
            }
            else
            {
                var d = in1.AsArray();
                var vd = @out.AsArray();

                //Simple case, reduce 1D array to scalar value
                if (axis == 0 && in1.Shape.Dimensions.LongLength == 1)
                {
                    long stride = in1.Shape.Dimensions[0].Stride;
                    long ix = in1.Shape.Offset;
                    long limit = (stride * in1.Shape.Dimensions[0].Length) + ix;

                    var value = d[ix];

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
                        var value = d[ix];

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
                        var value = d[ix];

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
                    var vl = @out.Subview(Range.NewAxis, axis);

                    //Initially we just copy the value
                    UFunc_Op_Inner_Unary_Flush<System.Boolean, CopyOp<System.Boolean>>(new CopyOp<System.Boolean>(), in1.Subview(Range.El(0), axis), vl);
                    
                    //If there is more than one element in the dimension to reduce, apply the operation accumulatively
                    for (long j = 1; j < size; j++)
                    {
                        //Select the new dimension
                        //Apply the operation
                        UFunc_Op_Inner_Binary_Flush<System.Boolean, NumCIL.Boolean.Xor>(op, vl, in1.Subview(Range.El(j), axis), vl);
                    }
                }
            }
            return @out;
        }

    }
}

