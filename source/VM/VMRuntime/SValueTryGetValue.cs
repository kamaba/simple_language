//****************************************************************************
//  File:      SValue.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Logging;
using SimpleLanguage.VM.Runtime;
using System.Diagnostics;
using System.Globalization;
namespace SimpleLanguage.VM
{
    public partial struct SValue
    {
        public static bool TryGetInt32FromSValue(in SValue source, out int value)
        {
            value = 0;
            if (source.isNull)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "source is Null");
                return false;
            }

            switch (source.eType)
            {
                case EVMType.Int8:
                    {
                        value = source.int8Value;
                    }
                    break;
                case EVMType.UInt8:
                    {
                        value = source.uint8Value;
                    }
                    break;
                case EVMType.Int16:
                    {
                        value = source.int16Value;
                    }
                    break;
                case EVMType.UInt16:
                    {
                        value = source.uint16Value;
                    }
                    break;
                case EVMType.Int32:
                    {
                        value = source.int32Value;
                    }
                    break;
                case EVMType.UInt32:
                    {
                        value = source.int16Value;
                    }
                    break;
                case EVMType.Int64:
                    {
                        if (source.int64Value > (long)int.MaxValue)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "parse ");
                        }
                        value = (int)source.int64Value;
                    }
                    break;
                case EVMType.UInt64:
                    {
                        if (source.uint64Value > (ulong)int.MaxValue)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "parse ");
                        }
                        value = (int)source.uint64Value;
                    }
                    break;
                default:
                    {
                        Log.AddRuntimeLog(LID.ShowMessageAssert, "parse ");
                    }
                    break;
            }
            return true;
        }
        public static bool TryGetInt16FromSValue(in SValue source, out short value)
        {
            value = 0;
            if (source.isNull)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "source is Null");
                return false;
            }

            switch (source.eType)
            {
                case EVMType.Int8:
                case EVMType.UInt8:
                case EVMType.Int16:
                    {
                        value = source.int16Value;
                    }
                    break;
                case EVMType.Int32:
                case EVMType.UInt16:
                case EVMType.UInt32:
                case EVMType.Int64:
                case EVMType.UInt64:
                    {
                        if (source.uint64Value > (ulong)int.MaxValue)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "parse ");
                        }
                        value = (short)source.uint64Value;
                    }
                    break;
                default:
                    {
                        Log.AddRuntimeLog(LID.ShowMessageAssert, "parse ");
                    }
                    break;
            }
            return true;
        }
    }
}
