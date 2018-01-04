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
using System.Runtime.InteropServices;

namespace NumCIL.Unsafe
{
	/// <summary>
	/// Container for all copy methods
	/// </summary>
    public static class Copy
    {
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(System.SByte[] target, IntPtr source, long count) { unsafe { fixed (System.SByte* t = target) { Inner.Memcpy(t, source.ToPointer(), 1, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(IntPtr target, System.SByte[] source, long count) { unsafe { fixed (System.SByte* s = source) { Inner.Memcpy(target.ToPointer(), s, 1, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(System.Byte[] target, IntPtr source, long count) { unsafe { fixed (System.Byte* t = target) { Inner.Memcpy(t, source.ToPointer(), 1, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(IntPtr target, System.Byte[] source, long count) { unsafe { fixed (System.Byte* s = source) { Inner.Memcpy(target.ToPointer(), s, 1, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(System.Int16[] target, IntPtr source, long count) { unsafe { fixed (System.Int16* t = target) { Inner.Memcpy(t, source.ToPointer(), 2, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(IntPtr target, System.Int16[] source, long count) { unsafe { fixed (System.Int16* s = source) { Inner.Memcpy(target.ToPointer(), s, 2, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(System.UInt16[] target, IntPtr source, long count) { unsafe { fixed (System.UInt16* t = target) { Inner.Memcpy(t, source.ToPointer(), 2, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(IntPtr target, System.UInt16[] source, long count) { unsafe { fixed (System.UInt16* s = source) { Inner.Memcpy(target.ToPointer(), s, 2, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(System.Int32[] target, IntPtr source, long count) { unsafe { fixed (System.Int32* t = target) { Inner.Memcpy(t, source.ToPointer(), 4, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(IntPtr target, System.Int32[] source, long count) { unsafe { fixed (System.Int32* s = source) { Inner.Memcpy(target.ToPointer(), s, 4, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(System.UInt32[] target, IntPtr source, long count) { unsafe { fixed (System.UInt32* t = target) { Inner.Memcpy(t, source.ToPointer(), 4, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(IntPtr target, System.UInt32[] source, long count) { unsafe { fixed (System.UInt32* s = source) { Inner.Memcpy(target.ToPointer(), s, 4, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(System.Int64[] target, IntPtr source, long count) { unsafe { fixed (System.Int64* t = target) { Inner.Memcpy(t, source.ToPointer(), 8, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(IntPtr target, System.Int64[] source, long count) { unsafe { fixed (System.Int64* s = source) { Inner.Memcpy(target.ToPointer(), s, 8, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(System.UInt64[] target, IntPtr source, long count) { unsafe { fixed (System.UInt64* t = target) { Inner.Memcpy(t, source.ToPointer(), 8, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(IntPtr target, System.UInt64[] source, long count) { unsafe { fixed (System.UInt64* s = source) { Inner.Memcpy(target.ToPointer(), s, 8, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(System.Single[] target, IntPtr source, long count) { unsafe { fixed (System.Single* t = target) { Inner.Memcpy(t, source.ToPointer(), 4, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(IntPtr target, System.Single[] source, long count) { unsafe { fixed (System.Single* s = source) { Inner.Memcpy(target.ToPointer(), s, 4, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(System.Double[] target, IntPtr source, long count) { unsafe { fixed (System.Double* t = target) { Inner.Memcpy(t, source.ToPointer(), 8, count); } } }
		/// <summary>
		///	Copies data from source to target
		/// </summary>
		/// <param name="target">The data destination</param>
		/// <param name="source">The data source</param>
		/// <param name="count">The number of elements to copy</param>
        public static void Memcpy(IntPtr target, System.Double[] source, long count) { unsafe { fixed (System.Double* s = source) { Inner.Memcpy(target.ToPointer(), s, 8, count); } } }
        private static unsafe class Inner
        {
            public static void Memcpy(void* target, void* source, int elsize, long count)
            {
                unsafe
                {
                    long bytes = elsize * count;
                    if (bytes % 8 == 0)
                    {
                        ulong* a = (ulong*)source;
                        ulong* b = (ulong*)target;
                        long els = bytes / 8;
                        for (long i = 0; i < els; i++)
                            b[i] = a[i];
                    }
                    else if (bytes % 4 == 0)
                    {
                        uint* a = (uint*)source;
                        uint* b = (uint*)target;
                        long els = bytes / 4;
                        for (long i = 0; i < els; i++)
                            b[i] = a[i];
                    }
                    else if (bytes % 2 == 0)
                    {
                        ushort* a = (ushort*)source;
                        ushort* b = (ushort*)target;
                        long els = bytes / 2;
                        for (long i = 0; i < els; i++)
                            b[i] = a[i];
                    }
                    else
                    {
                        byte* a = (byte*)source;
                        byte* b = (byte*)target;
                        long els = bytes;
                        for (long i = 0; i < els; i++)
                            b[i] = a[i];
                    }
                }
            }
        }
    }
}
