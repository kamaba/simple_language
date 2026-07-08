//****************************************************************************
//  File:      RuntimeValue.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Logging;
using SimpleLanguage.VM.Runtime;
using System.Globalization;
using System.Security.AccessControl;


namespace SimpleLanguage.VM
{
    public partial class RuntimeValueMethod
    {
        public static ulong ConvertToULong(RuntimeValue rv)
        {
            switch (rv.eType)
            {
                case EVMType.UInt8: return rv.uint8Value;
                case EVMType.Int8: return (byte)rv.int8Value;
                case EVMType.Int16: return (ushort)rv.int16Value;
                case EVMType.UInt16: return rv.uint16Value;
                case EVMType.Int32: return (uint)rv.int32Value;
                case EVMType.UInt32: return rv.uint32Value;
                case EVMType.Int64: return (ulong)rv.int64Value;
                case EVMType.UInt64: return rv.uint64Value;
                default: return 0;
            }
        }
        public static long ConvertToLong(RuntimeValue rv)
        {
            switch (rv.eType)
            {
                case EVMType.UInt8: return rv.uint8Value;
                case EVMType.Int8: return rv.int8Value;
                case EVMType.Int16: return rv.int16Value;
                case EVMType.UInt16: return rv.uint16Value;
                case EVMType.Int32: return rv.int32Value;
                case EVMType.UInt32: return rv.uint32Value;
                case EVMType.Int64: return rv.int64Value;
                case EVMType.UInt64: return (long)rv.uint64Value;
                default: return 0;
            }
        }
        public static void ConvertByEType(ref RuntimeValue _rv, EVMType neType)
        {
            // Object/Class slots may carry scalar wrappers (Int32Object/UInt64Object...).
            // Normalize first so Convert.* paths can work consistently.
            _rv.TryNormalizeObjectScalarInPlace();

            var oldType = _rv.eType;

            object? cur = _rv.GetValueObject();

            try
            {
                switch (neType)
                {
                    case EVMType.Boolean:
                        _rv.SetBoolValue(Convert.ToBoolean(cur, CultureInfo.InvariantCulture));
                        break;
                    case EVMType.UInt8:
                        _rv.SetUInt8Value(Convert.ToByte(cur, CultureInfo.InvariantCulture));
                        break;
                    case EVMType.Int8:
                        _rv.SetInt8Value(Convert.ToSByte(cur, CultureInfo.InvariantCulture));
                        break;
                    case EVMType.Int16:
                        _rv.SetInt16Value(Convert.ToInt16(cur, CultureInfo.InvariantCulture));
                        break;
                    case EVMType.UInt16:
                        _rv.SetUInt16Value(Convert.ToUInt16(cur, CultureInfo.InvariantCulture));
                        break;
                    case EVMType.Int32:
                        _rv.SetInt32Value(Convert.ToInt32(cur, CultureInfo.InvariantCulture));
                        break;
                    case EVMType.UInt32:
                        _rv.SetUInt32Value(Convert.ToUInt32(cur, CultureInfo.InvariantCulture));
                        break;
                    case EVMType.Int64:
                        _rv.SetInt64Value(Convert.ToInt64(cur, CultureInfo.InvariantCulture));
                        break;
                    case EVMType.UInt64:
                        _rv.SetUInt64Value(Convert.ToUInt64(cur, CultureInfo.InvariantCulture));
                        break;
                    case EVMType.Float32:
                        _rv.SetFloatValue(Convert.ToSingle(cur, CultureInfo.InvariantCulture));
                        break;
                    case EVMType.Float64:
                    case EVMType.Num:
                        _rv.float64Value = (Convert.ToDouble(cur, CultureInfo.InvariantCulture));
                        _rv.eType = neType;
                        _rv.isNull = false;
                        break;
                    case EVMType.String:
                        _rv.SetStringValue(Convert.ToString(cur, CultureInfo.InvariantCulture) ?? string.Empty);
                        break;
                    default:
                        Log.AddRuntimeLog(LID.ShowMessageAssert, "Error 异常类型在ConvertByEType中");
                        return;
                }

                if (IsNarrowingConversion(oldType, neType))
                {
                    Log.AddRuntimeLog(LID.ShowMessageWarning,
                        $"数值降阶转换 {oldType} -> {neType}, value={cur}");
                }
            }
            catch (Exception e)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert,
                    $"数值转换溢出 {oldType} -> {neType}, value={cur} exception: {e}");
            }
        }

        private static bool IsNarrowingConversion(EVMType source, EVMType target)
        {
            if (source == target) return false;
            if (!IsNumericEType(source) || !IsNumericEType(target)) return false;

            int srcBits = GetNumericBits(source);
            int dstBits = GetNumericBits(target);

            if (srcBits > dstBits) return true;

            bool srcFloat = source is EVMType.Float32 or EVMType.Float64 or EVMType.Num;
            bool dstFloat = target is EVMType.Float32 or EVMType.Float64 or EVMType.Num;
            if (srcFloat && !dstFloat) return true;

            bool srcUnsigned = source is EVMType.UInt8 or EVMType.UInt16 or EVMType.UInt32 or EVMType.UInt64;
            bool dstSigned = target is EVMType.Int8 or EVMType.Int16 or EVMType.Int32 or EVMType.Int64;
            if (srcUnsigned && dstSigned && srcBits >= dstBits) return true;

            return false;
        }

        private static bool IsNumericEType(EVMType t)
        {
            return t is EVMType.Boolean
                or EVMType.UInt8 or EVMType.Int8
                or EVMType.Int16 or EVMType.UInt16
                or EVMType.Int32 or EVMType.UInt32
                or EVMType.Int64 or EVMType.UInt64
                or EVMType.Float32 or EVMType.Float64 or EVMType.Num;
        }

        private static int GetNumericBits(EVMType t)
        {
            return t switch
            {
                EVMType.Boolean => 1,
                EVMType.UInt8 or EVMType.Int8 => 8,
                EVMType.Int16 or EVMType.UInt16 => 16,
                EVMType.Int32 or EVMType.UInt32 or EVMType.Float32 => 32,
                EVMType.Int64 or EVMType.UInt64 or EVMType.Float64 or EVMType.Num => 64,
                _ => 0,
            };
        }
    }
}
