using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

namespace OpenSlideNET
{
    internal static partial class Interop
    {
        internal static unsafe string StringFromNativeUtf8(IntPtr nativeUtf8)
        {
            List<byte> ar = new List<byte>();
            if (nativeUtf8 == IntPtr.Zero)
                return null;
            //int len = 0;
            //while (*(byte*)(nativeUtf8 + len) != 0) ++len;
            //return Encoding.UTF8.GetString((byte*)nativeUtf8, 0, len);
            for (int i = 0; *(byte*)(nativeUtf8 + i) != 0; ++i)
            {
                ar.Add(*(byte*)(nativeUtf8 + i));
            }
            ar.Add((byte)0);
            return Encoding.UTF8.GetString(ar.ToArray());
        }

        internal ref struct UnsafeUtf8Encoder
        {
            private readonly IntPtr _stackPointer;
            private readonly int _stackSize;
            private GCHandle _handle;

            public unsafe UnsafeUtf8Encoder(byte* stackPointer, int stackSize)
            {
                _stackPointer = (IntPtr)stackPointer;
                _stackSize = stackSize;
                _handle = default;
            }

            public unsafe IntPtr Encode(string value)
            {
                Debug.Assert(value != null);

                if (_handle != default)
                {
                    _handle.Free();
                    _handle = default;
                }

                IntPtr pointer = _stackPointer;
                int count = Encoding.UTF8.GetByteCount(value);
                if (count + 1 > _stackSize)
                {
                    var buffer = new byte[count + 1];
                    _handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    pointer = _handle.AddrOfPinnedObject();
                }
                fixed (char* pValue = value)
                {
                    Encoding.UTF8.GetBytes(pValue, value.Length, (byte*)pointer, count);
                }
                ((byte*)pointer)[count] = 0;
                return pointer;
            }

            public void Dispose()
            {
                if (_handle != default)
                {
                    _handle.Free();
                    _handle = default;
                }
            }

        }
    }
}
