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
    internal static class CreateAccessor
    {
        private static IDataAccessor<System.SByte> CreateFromSize_SByte(long size) { return new UnmanagedAccessorSByte(size); }
        private static IDataAccessor<System.SByte> CreateFromData_SByte(System.SByte[] data) { return new UnmanagedAccessorSByte(data); }

		public class UnmanagedAccessorSByte : UnmanagedAccessorBase<System.SByte>
		{
			public UnmanagedAccessorSByte(long size) : base(size) { }
			public UnmanagedAccessorSByte(System.SByte[] data) : base(data) { }

			public override System.SByte this[long index]
			{
				get
				{
					Allocate();
					unsafe 
					{
						return m_dataPtr != IntPtr.Zero ? ((System.SByte*)m_dataPtr.ToPointer())[index] : m_data[index];
					}
				}
				set
				{
					Allocate();
					if (m_dataPtr != IntPtr.Zero)
					{
						unsafe 
						{
							((System.SByte*)m_dataPtr.ToPointer())[index] = value;
						}
					}
					else
					{
						m_data[index] = value;
					}
				}
			}
		}
        private static IDataAccessor<System.Byte> CreateFromSize_Byte(long size) { return new UnmanagedAccessorByte(size); }
        private static IDataAccessor<System.Byte> CreateFromData_Byte(System.Byte[] data) { return new UnmanagedAccessorByte(data); }

		public class UnmanagedAccessorByte : UnmanagedAccessorBase<System.Byte>
		{
			public UnmanagedAccessorByte(long size) : base(size) { }
			public UnmanagedAccessorByte(System.Byte[] data) : base(data) { }

			public override System.Byte this[long index]
			{
				get
				{
					Allocate();
					unsafe 
					{
						return m_dataPtr != IntPtr.Zero ? ((System.Byte*)m_dataPtr.ToPointer())[index] : m_data[index];
					}
				}
				set
				{
					Allocate();
					if (m_dataPtr != IntPtr.Zero)
					{
						unsafe 
						{
							((System.Byte*)m_dataPtr.ToPointer())[index] = value;
						}
					}
					else
					{
						m_data[index] = value;
					}
				}
			}
		}
        private static IDataAccessor<System.Int16> CreateFromSize_Int16(long size) { return new UnmanagedAccessorInt16(size); }
        private static IDataAccessor<System.Int16> CreateFromData_Int16(System.Int16[] data) { return new UnmanagedAccessorInt16(data); }

		public class UnmanagedAccessorInt16 : UnmanagedAccessorBase<System.Int16>
		{
			public UnmanagedAccessorInt16(long size) : base(size) { }
			public UnmanagedAccessorInt16(System.Int16[] data) : base(data) { }

			public override System.Int16 this[long index]
			{
				get
				{
					Allocate();
					unsafe 
					{
						return m_dataPtr != IntPtr.Zero ? ((System.Int16*)m_dataPtr.ToPointer())[index] : m_data[index];
					}
				}
				set
				{
					Allocate();
					if (m_dataPtr != IntPtr.Zero)
					{
						unsafe 
						{
							((System.Int16*)m_dataPtr.ToPointer())[index] = value;
						}
					}
					else
					{
						m_data[index] = value;
					}
				}
			}
		}
        private static IDataAccessor<System.UInt16> CreateFromSize_UInt16(long size) { return new UnmanagedAccessorUInt16(size); }
        private static IDataAccessor<System.UInt16> CreateFromData_UInt16(System.UInt16[] data) { return new UnmanagedAccessorUInt16(data); }

		public class UnmanagedAccessorUInt16 : UnmanagedAccessorBase<System.UInt16>
		{
			public UnmanagedAccessorUInt16(long size) : base(size) { }
			public UnmanagedAccessorUInt16(System.UInt16[] data) : base(data) { }

			public override System.UInt16 this[long index]
			{
				get
				{
					Allocate();
					unsafe 
					{
						return m_dataPtr != IntPtr.Zero ? ((System.UInt16*)m_dataPtr.ToPointer())[index] : m_data[index];
					}
				}
				set
				{
					Allocate();
					if (m_dataPtr != IntPtr.Zero)
					{
						unsafe 
						{
							((System.UInt16*)m_dataPtr.ToPointer())[index] = value;
						}
					}
					else
					{
						m_data[index] = value;
					}
				}
			}
		}
        private static IDataAccessor<System.Int32> CreateFromSize_Int32(long size) { return new UnmanagedAccessorInt32(size); }
        private static IDataAccessor<System.Int32> CreateFromData_Int32(System.Int32[] data) { return new UnmanagedAccessorInt32(data); }

		public class UnmanagedAccessorInt32 : UnmanagedAccessorBase<System.Int32>
		{
			public UnmanagedAccessorInt32(long size) : base(size) { }
			public UnmanagedAccessorInt32(System.Int32[] data) : base(data) { }

			public override System.Int32 this[long index]
			{
				get
				{
					Allocate();
					unsafe 
					{
						return m_dataPtr != IntPtr.Zero ? ((System.Int32*)m_dataPtr.ToPointer())[index] : m_data[index];
					}
				}
				set
				{
					Allocate();
					if (m_dataPtr != IntPtr.Zero)
					{
						unsafe 
						{
							((System.Int32*)m_dataPtr.ToPointer())[index] = value;
						}
					}
					else
					{
						m_data[index] = value;
					}
				}
			}
		}
        private static IDataAccessor<System.UInt32> CreateFromSize_UInt32(long size) { return new UnmanagedAccessorUInt32(size); }
        private static IDataAccessor<System.UInt32> CreateFromData_UInt32(System.UInt32[] data) { return new UnmanagedAccessorUInt32(data); }

		public class UnmanagedAccessorUInt32 : UnmanagedAccessorBase<System.UInt32>
		{
			public UnmanagedAccessorUInt32(long size) : base(size) { }
			public UnmanagedAccessorUInt32(System.UInt32[] data) : base(data) { }

			public override System.UInt32 this[long index]
			{
				get
				{
					Allocate();
					unsafe 
					{
						return m_dataPtr != IntPtr.Zero ? ((System.UInt32*)m_dataPtr.ToPointer())[index] : m_data[index];
					}
				}
				set
				{
					Allocate();
					if (m_dataPtr != IntPtr.Zero)
					{
						unsafe 
						{
							((System.UInt32*)m_dataPtr.ToPointer())[index] = value;
						}
					}
					else
					{
						m_data[index] = value;
					}
				}
			}
		}
        private static IDataAccessor<System.Int64> CreateFromSize_Int64(long size) { return new UnmanagedAccessorInt64(size); }
        private static IDataAccessor<System.Int64> CreateFromData_Int64(System.Int64[] data) { return new UnmanagedAccessorInt64(data); }

		public class UnmanagedAccessorInt64 : UnmanagedAccessorBase<System.Int64>
		{
			public UnmanagedAccessorInt64(long size) : base(size) { }
			public UnmanagedAccessorInt64(System.Int64[] data) : base(data) { }

			public override System.Int64 this[long index]
			{
				get
				{
					Allocate();
					unsafe 
					{
						return m_dataPtr != IntPtr.Zero ? ((System.Int64*)m_dataPtr.ToPointer())[index] : m_data[index];
					}
				}
				set
				{
					Allocate();
					if (m_dataPtr != IntPtr.Zero)
					{
						unsafe 
						{
							((System.Int64*)m_dataPtr.ToPointer())[index] = value;
						}
					}
					else
					{
						m_data[index] = value;
					}
				}
			}
		}
        private static IDataAccessor<System.UInt64> CreateFromSize_UInt64(long size) { return new UnmanagedAccessorUInt64(size); }
        private static IDataAccessor<System.UInt64> CreateFromData_UInt64(System.UInt64[] data) { return new UnmanagedAccessorUInt64(data); }

		public class UnmanagedAccessorUInt64 : UnmanagedAccessorBase<System.UInt64>
		{
			public UnmanagedAccessorUInt64(long size) : base(size) { }
			public UnmanagedAccessorUInt64(System.UInt64[] data) : base(data) { }

			public override System.UInt64 this[long index]
			{
				get
				{
					Allocate();
					unsafe 
					{
						return m_dataPtr != IntPtr.Zero ? ((System.UInt64*)m_dataPtr.ToPointer())[index] : m_data[index];
					}
				}
				set
				{
					Allocate();
					if (m_dataPtr != IntPtr.Zero)
					{
						unsafe 
						{
							((System.UInt64*)m_dataPtr.ToPointer())[index] = value;
						}
					}
					else
					{
						m_data[index] = value;
					}
				}
			}
		}
        private static IDataAccessor<System.Single> CreateFromSize_Single(long size) { return new UnmanagedAccessorSingle(size); }
        private static IDataAccessor<System.Single> CreateFromData_Single(System.Single[] data) { return new UnmanagedAccessorSingle(data); }

		public class UnmanagedAccessorSingle : UnmanagedAccessorBase<System.Single>
		{
			public UnmanagedAccessorSingle(long size) : base(size) { }
			public UnmanagedAccessorSingle(System.Single[] data) : base(data) { }

			public override System.Single this[long index]
			{
				get
				{
					Allocate();
					unsafe 
					{
						return m_dataPtr != IntPtr.Zero ? ((System.Single*)m_dataPtr.ToPointer())[index] : m_data[index];
					}
				}
				set
				{
					Allocate();
					if (m_dataPtr != IntPtr.Zero)
					{
						unsafe 
						{
							((System.Single*)m_dataPtr.ToPointer())[index] = value;
						}
					}
					else
					{
						m_data[index] = value;
					}
				}
			}
		}
        private static IDataAccessor<System.Double> CreateFromSize_Double(long size) { return new UnmanagedAccessorDouble(size); }
        private static IDataAccessor<System.Double> CreateFromData_Double(System.Double[] data) { return new UnmanagedAccessorDouble(data); }

		public class UnmanagedAccessorDouble : UnmanagedAccessorBase<System.Double>
		{
			public UnmanagedAccessorDouble(long size) : base(size) { }
			public UnmanagedAccessorDouble(System.Double[] data) : base(data) { }

			public override System.Double this[long index]
			{
				get
				{
					Allocate();
					unsafe 
					{
						return m_dataPtr != IntPtr.Zero ? ((System.Double*)m_dataPtr.ToPointer())[index] : m_data[index];
					}
				}
				set
				{
					Allocate();
					if (m_dataPtr != IntPtr.Zero)
					{
						unsafe 
						{
							((System.Double*)m_dataPtr.ToPointer())[index] = value;
						}
					}
					else
					{
						m_data[index] = value;
					}
				}
			}
		}
	}
}