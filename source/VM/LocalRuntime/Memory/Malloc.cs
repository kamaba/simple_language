using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleLanguage.VM.Runtime.Memory
{
    public class Malloc
    {
        public static unsafe IntPtr  NewClass( int bitLong )
        {
            byte* buff = (byte*)Marshal.AllocHGlobal(bitLong);

            return new IntPtr(buff);
        }
    }
}
