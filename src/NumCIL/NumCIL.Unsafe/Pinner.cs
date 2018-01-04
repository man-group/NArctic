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
using NumCIL.Generic;

namespace NumCIL.Unsafe
{
    /// <summary>
    /// Helper class to extract a pinned pointer from the data
    /// </summary>
    internal unsafe class Pinner : IDisposable
    {
        private GCHandle m_handle;
        public readonly void* ptr = null;

        public Pinner(IDataAccessor<System.SByte> i)
        {
            if (i is IUnmanagedDataAccessor<System.SByte>)
                ptr = ((IUnmanagedDataAccessor<System.SByte>)i).Pointer.ToPointer();
            else
            {
                m_handle = GCHandle.Alloc(i.AsArray(), GCHandleType.Pinned);
                ptr = m_handle.AddrOfPinnedObject().ToPointer();
            }

			if (ptr == null)
				throw new Exception("Null pointer allocation?");
        }
        public Pinner(IDataAccessor<System.Byte> i)
        {
            if (i is IUnmanagedDataAccessor<System.Byte>)
                ptr = ((IUnmanagedDataAccessor<System.Byte>)i).Pointer.ToPointer();
            else
            {
                m_handle = GCHandle.Alloc(i.AsArray(), GCHandleType.Pinned);
                ptr = m_handle.AddrOfPinnedObject().ToPointer();
            }

			if (ptr == null)
				throw new Exception("Null pointer allocation?");
        }
        public Pinner(IDataAccessor<System.Int16> i)
        {
            if (i is IUnmanagedDataAccessor<System.Int16>)
                ptr = ((IUnmanagedDataAccessor<System.Int16>)i).Pointer.ToPointer();
            else
            {
                m_handle = GCHandle.Alloc(i.AsArray(), GCHandleType.Pinned);
                ptr = m_handle.AddrOfPinnedObject().ToPointer();
            }

			if (ptr == null)
				throw new Exception("Null pointer allocation?");
        }
        public Pinner(IDataAccessor<System.UInt16> i)
        {
            if (i is IUnmanagedDataAccessor<System.UInt16>)
                ptr = ((IUnmanagedDataAccessor<System.UInt16>)i).Pointer.ToPointer();
            else
            {
                m_handle = GCHandle.Alloc(i.AsArray(), GCHandleType.Pinned);
                ptr = m_handle.AddrOfPinnedObject().ToPointer();
            }

			if (ptr == null)
				throw new Exception("Null pointer allocation?");
        }
        public Pinner(IDataAccessor<System.Int32> i)
        {
            if (i is IUnmanagedDataAccessor<System.Int32>)
                ptr = ((IUnmanagedDataAccessor<System.Int32>)i).Pointer.ToPointer();
            else
            {
                m_handle = GCHandle.Alloc(i.AsArray(), GCHandleType.Pinned);
                ptr = m_handle.AddrOfPinnedObject().ToPointer();
            }

			if (ptr == null)
				throw new Exception("Null pointer allocation?");
        }
        public Pinner(IDataAccessor<System.UInt32> i)
        {
            if (i is IUnmanagedDataAccessor<System.UInt32>)
                ptr = ((IUnmanagedDataAccessor<System.UInt32>)i).Pointer.ToPointer();
            else
            {
                m_handle = GCHandle.Alloc(i.AsArray(), GCHandleType.Pinned);
                ptr = m_handle.AddrOfPinnedObject().ToPointer();
            }

			if (ptr == null)
				throw new Exception("Null pointer allocation?");
        }
        public Pinner(IDataAccessor<System.Int64> i)
        {
            if (i is IUnmanagedDataAccessor<System.Int64>)
                ptr = ((IUnmanagedDataAccessor<System.Int64>)i).Pointer.ToPointer();
            else
            {
                m_handle = GCHandle.Alloc(i.AsArray(), GCHandleType.Pinned);
                ptr = m_handle.AddrOfPinnedObject().ToPointer();
            }

			if (ptr == null)
				throw new Exception("Null pointer allocation?");
        }
        public Pinner(IDataAccessor<System.UInt64> i)
        {
            if (i is IUnmanagedDataAccessor<System.UInt64>)
                ptr = ((IUnmanagedDataAccessor<System.UInt64>)i).Pointer.ToPointer();
            else
            {
                m_handle = GCHandle.Alloc(i.AsArray(), GCHandleType.Pinned);
                ptr = m_handle.AddrOfPinnedObject().ToPointer();
            }

			if (ptr == null)
				throw new Exception("Null pointer allocation?");
        }
        public Pinner(IDataAccessor<System.Single> i)
        {
            if (i is IUnmanagedDataAccessor<System.Single>)
                ptr = ((IUnmanagedDataAccessor<System.Single>)i).Pointer.ToPointer();
            else
            {
                m_handle = GCHandle.Alloc(i.AsArray(), GCHandleType.Pinned);
                ptr = m_handle.AddrOfPinnedObject().ToPointer();
            }

			if (ptr == null)
				throw new Exception("Null pointer allocation?");
        }
        public Pinner(IDataAccessor<System.Double> i)
        {
            if (i is IUnmanagedDataAccessor<System.Double>)
                ptr = ((IUnmanagedDataAccessor<System.Double>)i).Pointer.ToPointer();
            else
            {
                m_handle = GCHandle.Alloc(i.AsArray(), GCHandleType.Pinned);
                ptr = m_handle.AddrOfPinnedObject().ToPointer();
            }

			if (ptr == null)
				throw new Exception("Null pointer allocation?");
        }
        public Pinner(IDataAccessor<System.Boolean> i)
        {
            if (i is IUnmanagedDataAccessor<System.Boolean>)
                ptr = ((IUnmanagedDataAccessor<System.Boolean>)i).Pointer.ToPointer();
            else
            {
                m_handle = GCHandle.Alloc(i.AsArray(), GCHandleType.Pinned);
                ptr = m_handle.AddrOfPinnedObject().ToPointer();
            }

			if (ptr == null)
				throw new Exception("Null pointer allocation?");
        }

        public void Dispose() { Dispose(true); }

        protected void Dispose(bool disposing)
        {
            if (m_handle.IsAllocated)
                m_handle.Free();

            if (disposing)
                GC.SuppressFinalize(this);
        }

        ~Pinner()
        {
            Dispose(false);
        }

	}
}