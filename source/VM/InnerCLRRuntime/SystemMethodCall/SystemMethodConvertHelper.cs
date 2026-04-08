using System;
using System.Globalization;

namespace SimpleLanguage.VM.Runtime
{
    /// <summary>Shared convert/unbox logic for <see cref="ESystemMethodCall"/> conversion builtins.</summary>
    internal static class SystemMethodConvertHelper
    {
        /// <summary>Pops one stack operand and converts it to the target primitive/string per <see cref="ESystemMethodCall"/>.</summary>
        public static SValue ConvertValue(ref SValue arg, ESystemMethodCall kind)
        {
            if (arg.isNull)
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }
            object raw = UnwrapStackValueForSystemConvert(ref arg);
            try
            {
                object conv = kind switch
                {
                    ESystemMethodCall.SystemConvertInt8 => Convert.ToByte(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertSInt8 => Convert.ToSByte(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertInt16 => Convert.ToInt16(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertUInt16 => Convert.ToUInt16(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertInt32 => Convert.ToInt32(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertUInt32 => Convert.ToUInt32(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertInt64 => Convert.ToInt64(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertUInt64 => Convert.ToUInt64(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertFloat32 => Convert.ToSingle(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertFloat64 => Convert.ToDouble(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertString => raw?.ToString() ?? string.Empty,
                    _ => raw,
                };
                return SValue.FromClrObject(conv);
            }
            catch
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }
        }

        private static object UnwrapStackValueForSystemConvert(ref SValue v)
        {
            if (v.isNull) return 0;
            switch (v.eType)
            {
                case EVMType.Boolean: return v.int8Value != 0;
                case EVMType.Byte: return v.int8Value;
                case EVMType.SByte: return v.sint8Value;
                case EVMType.Int16: return v.int16Value;
                case EVMType.UInt16: return v.uint16Value;
                case EVMType.Int32: return v.int32Value;
                case EVMType.UInt32: return v.uint32Value;
                case EVMType.Int64: return v.int64Value;
                case EVMType.UInt64: return v.uint64Value;
                case EVMType.Float32: return v.floatValue;
                case EVMType.Float64: return v.doubleValue;
                case EVMType.Num: return v.doubleValue;
                case EVMType.String: return v.stringValue ?? string.Empty;
                default: break;
            }
            if (v.sobject != null)
            {
                switch (v.sobject)
                {
                    case BoolObject o: return o.value;
                    case Int8Object o: return o.value;
                    case SInt8Object o: return o.value;
                    case Int16Object o: return o.value;
                    case UInt16Object o: return o.value;
                    case Int32Object o: return o.value;
                    case UInt32Object o: return o.value;
                    case Int64Object o: return o.value;
                    case UInt64Object o: return o.value;
                    case Float32Object o: return o.value;
                    case Float64Object o: return o.value;
                    case StringObject o: return o.value ?? string.Empty;
                    case NumObject o: return o.ToDouble();
                }
                return v.sobject.value ?? v.sobject.ToString() ?? string.Empty;
            }
            return v.GetValueObject() ?? string.Empty;
        }
    }
}
