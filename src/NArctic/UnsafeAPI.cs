using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NMem;

namespace NArctic
{
	public static class UnsafeAPI
	{
		public static unsafe void ColumnCopy(IntPtr ptarget, int target_col, int target_width, IntPtr psource, int source_col, int source_width, int count, int elsize)
		{
			unsafe
			{
				byte* src = (byte*)psource.ToPointer();
				byte* dst = (byte*)ptarget.ToPointer();
				src += source_col;
				dst += target_col;

				for (int i = 0; i < count; i++) {
					for (int j = 0; j < elsize; j++)
						dst [j] = src [j];
					dst += target_width;
					src += source_width;
				}
			}
		}

        public static unsafe void Copy(IntPtr pdest, IntPtr psrc, ulong len)
        {
            byte* dest = (byte*)(void*)pdest.ToPointer();
            byte* src = (byte*)(void*)psrc.ToPointer();
            for (ulong i = 0; i < len; i++)
                dest[i] = src[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>()
        {
            return NMem.NMem.SizeOf<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(byte[] data, int offset)
        {
            T result = default(T);
            GCHandle h = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr ptr = h.AddrOfPinnedObject();
            NMem.NMem.Write(ptr+offset, ref result);
            h.Free();
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(byte[] data, int offset, T value)
        {
            GCHandle h = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr ptr = h.AddrOfPinnedObject();
            NMem.NMem.Read(ptr+offset, ref value);
            h.Free();
        }

    }
}

