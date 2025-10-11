//****************************************************************************
//  File:      IRUtil.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: IR common function
//****************************************************************************

using SimpleLanguage.Core;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRUtil
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
                //case EType.Char:
                //    return 2;
                case EType.Int16:
                case EType.UInt16:
                    return 2;
                case EType.Int32:
                case EType.UInt32:
                case EType.Class:
                case EType.String:
                case EType.Float32:
                    return 4;
                case EType.Int64:
                case EType.UInt64:
                case EType.Float64:
                    return 8;
                case EType.Int128:
                case EType.UInt128:
                    return 16;
                case EType.Float2:
                    return 8;

            }
            return 1;
        }
    }

}
