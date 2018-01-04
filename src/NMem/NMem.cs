using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NMem
{
    public static class NMem
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>()
        {
#if IL
            sizeof !!T
#endif
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read<T>(IntPtr p, ref T obj)
        {
#if IL
            ldarg p
            ldarg obj
            cpobj !!T
#endif
        }
         
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(IntPtr p, ref T obj)
        {
#if IL
           ldarg obj
           ldarg p
           cpobj !!T
#endif
        }
    }
}
