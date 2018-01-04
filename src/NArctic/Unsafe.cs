using System;
using MongoDB.Driver;
using System.Runtime.InteropServices;

namespace NumCIL
{
	public static class Unsafe
	{
		public static unsafe void ColumnCopy(void *target, int target_col, int target_width, void *source, int source_col, int source_width, int count, int elsize)
		{
			unsafe
			{
				byte* src = (byte*)source;
				byte* dst = (byte*)target;
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

		public static unsafe void CopyColumn(double []dst, IntPtr src, int bytesPerRow, int fieldOffset)
		{
			fixed(double* pdst = &dst[0]) 
			{
				byte *psrc = (byte*)src.ToPointer();
				double* pdata = pdst;
				psrc += fieldOffset;
				for (int irow = 0; irow < dst.Length; irow++) {
					*pdata = *((double*)psrc);
					pdata++;
					psrc += bytesPerRow;
				}
			}
		}

		public static unsafe void CopyColumn(int []dst, IntPtr src, int bytesPerRow, int fieldOffset)
		{
			fixed(int* pdst = &dst[0]) 
			{
				byte *psrc = (byte*)src.ToPointer();
				int* pdata = pdst;
				psrc += fieldOffset;
				for (int irow = 0; irow < dst.Length; irow++) {
					*pdata = *((int*)psrc);
					pdata++;
					psrc += bytesPerRow;
				}
			}
		}

		public static unsafe void CopyColumn(uint []dst, IntPtr src, int bytesPerRow, int fieldOffset)
		{
			fixed(uint* pdst = &dst[0]) 
			{
				byte *psrc = (byte*)src.ToPointer();
				uint* pdata = pdst;
				psrc += fieldOffset;
				for (int irow = 0; irow < dst.Length; irow++) {
					*pdata = *((uint*)psrc);
					pdata++;
					psrc += bytesPerRow;
				}
			}
		}

		public static unsafe void CopyColumn(long []dst, IntPtr src, int bytesPerRow, int fieldOffset)
		{
			fixed(long* pdst = &dst[0]) 
			{
				byte *psrc = (byte*)src.ToPointer();
				long* pdata = pdst;
				psrc += fieldOffset;
				for (int irow = 0; irow < dst.Length; irow++) {
					*pdata = *((long*)psrc);
					pdata++;
					psrc += bytesPerRow;
				}
			}
		}

		public static unsafe void CopyColumn(ulong []dst, IntPtr src, int bytesPerRow, int fieldOffset)
		{
			fixed(ulong* pdst = &dst[0]) 
			{
				byte *psrc = (byte*)src.ToPointer();
				ulong* pdata = pdst;
				psrc += fieldOffset;
				for (int irow = 0; irow < dst.Length; irow++) {
					*pdata = *((ulong*)psrc);
					pdata++;
					psrc += bytesPerRow;
				}
			}
		}

		public static T[] GetColumn<T>(byte[] buf, int height, int bytesPerRow, int fieldOffset) where T:struct
		{
			object data = new T[height];
			var gh = GCHandle.Alloc(buf, GCHandleType.Pinned);
			IntPtr adr = gh.AddrOfPinnedObject();
			if (typeof(T) == typeof(double))
				CopyColumn ((double[])data, adr, bytesPerRow, fieldOffset);
			else if(typeof(T) == typeof(int))
				CopyColumn ((int[])data, adr, bytesPerRow, fieldOffset);
			else if(typeof(T)==typeof(uint))
				CopyColumn ((uint[])data, adr, bytesPerRow, fieldOffset);
			else if(typeof(T)==typeof(long))
				CopyColumn ((long[])data, adr, bytesPerRow, fieldOffset);
			else if(typeof(T)==typeof(ulong))
				CopyColumn ((ulong[])data, adr, bytesPerRow, fieldOffset);
			gh.Free ();
			return (T[])data;
		}
	}
}

