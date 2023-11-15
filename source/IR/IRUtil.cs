//****************************************************************************
//  File:      IRUtil.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: IR common function
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Reflection;

namespace SimpleLanguage.IR
{
    public partial class IRUtil
    {
        public static int GetTypeSize(EType etype)
        {
            switch (etype)
            {
                case EType.Bit:
                    return 1;
                case EType.Byte:
                case EType.Boolean:
                    return 1;
                case EType.Char:
                    return 2;
                case EType.Int16:
                case EType.UInt16:
                    return 2;
                case EType.Int32:
                case EType.UInt32:
                case EType.Class:
                case EType.String:
                case EType.Float:
                    return 4;
                case EType.Int64:
                case EType.UInt64:
                case EType.Double:
                    return 8;
                case EType.Int128:
                case EType.UInt128:
                case EType.Decimal:
                    return 16;

            }
            return 1;
        }
    }
}
