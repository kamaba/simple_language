using System;
using System.Globalization;

namespace SimpleLanguage.VM.Runtime
{
    /// <summary>Shared convert/unbox logic for <see cref="ESystemMethodCall"/> conversion builtins.</summary>
    internal static class SystemMethodConvertHelper
    {
        /// <summary>
        /// <see cref="ESystemMethodCall.SystemConvertInt8"/> with a second stack argument (see <see cref="ConvertInt8"/>).
        /// </summary>
        internal static int ReadInt32ArgLoose(ref SValue v)
        {
            if (v.isNull)
                return int.MinValue;
            switch (v.eType)
            {
                case EVMType.Boolean:
                    return v.uint8Value != 0 ? 1 : 0;
                case EVMType.UInt8:
                    return v.uint8Value;
                case EVMType.Int8:
                    return v.int8Value;
                case EVMType.Int16:
                    return v.int16Value;
                case EVMType.UInt16:
                    return v.uint16Value;
                case EVMType.Int32:
                    return v.int32Value;
                case EVMType.UInt32:
                    return unchecked((int)v.uint32Value);
                case EVMType.Int64:
                    return unchecked((int)v.int64Value);
                case EVMType.UInt64:
                    return unchecked((int)v.uint64Value);
                case EVMType.Float32:
                    return unchecked((int)BitConverter.SingleToInt32Bits(v.float32Value));
                case EVMType.Float64:
                case EVMType.Num:
                    return unchecked((int)BitConverter.DoubleToInt64Bits(v.float64Value));
                case EVMType.String:
                    if (int.TryParse(v.stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var si))
                        return si;
                    return int.MinValue;
            }

            if (v.sobject != null)
            {
                try
                {
                    return Convert.ToInt32(v.sobject.value ?? v.sobject.ToString(), CultureInfo.InvariantCulture);
                }
                catch
                {
                    return int.MinValue;
                }
            }

            try
            {
                return Convert.ToInt32(v.GetValueObject(), CultureInfo.InvariantCulture);
            }
            catch
            {
                return int.MinValue;
            }
        }

        /// <summary>
        /// <paramref name="index"/> == -1: legacy <c>Convert.ToByte</c> (full byte, supports string).<br/>
        /// <paramref name="index"/> &gt;= 0: take 4 bits from the unsigned bit pattern of the value, counting from the least significant bit (<c>index</c> is the low bit of the window). Requires <c>index + 4 &lt;=</c> storage width of the numeric type.
        /// </summary>
        public static SValue ConvertInt8(ref SValue arg, int index)
        {
            if (arg.isNull)
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }

            if (index == int.MinValue)
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }

            if (index < -1)
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }

            if (index == -1)
                return ConvertInt8Legacy(ref arg);

            if (arg.eType == EVMType.String || (arg.sobject is StringObject))
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }

            if (!TryGetUnsignedBitPattern(ref arg, out ulong bits, out int bitWidth) || index + 4 > bitWidth)
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }

            byte nibble = (byte)((bits >> index) & 0xFUL);
            var outv = default(SValue);
            outv.SetUInt8Value(nibble);
            return outv;
        }

        private static SValue ConvertInt8Legacy(ref SValue arg)
        {
            object raw = UnwrapStackValueForSystemConvert(ref arg);
            try
            {
                byte conv = Convert.ToByte(raw, CultureInfo.InvariantCulture);
                return SValue.FromClrObject(conv);
            }
            catch
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }
        }

        /// <summary>Same rules as <see cref="ConvertInt8"/> but result is <c>sbyte</c> and legacy path uses <c>Convert.ToSByte</c>.</summary>
        public static SValue ConvertSInt8(ref SValue arg, int index)
        {
            if (arg.isNull)
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }

            if (index == int.MinValue)
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }

            if (index < -1)
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }

            if (index == -1)
                return ConvertSInt8Legacy(ref arg);

            if (arg.eType == EVMType.String || (arg.sobject is StringObject))
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }

            if (!TryGetUnsignedBitPattern(ref arg, out ulong bits, out int bitWidth) || index + 4 > bitWidth)
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }

            sbyte nib = unchecked((sbyte)(byte)((bits >> index) & 0xFUL));
            var outv = default(SValue);
            outv.SetInt8Value(nib);
            return outv;
        }

        private static SValue ConvertSInt8Legacy(ref SValue arg)
        {
            object raw = UnwrapStackValueForSystemConvert(ref arg);
            try
            {
                sbyte conv = Convert.ToSByte(raw, CultureInfo.InvariantCulture);
                return SValue.FromClrObject(conv);
            }
            catch
            {
                var z = default(SValue);
                z.SetNull();
                return z;
            }
        }

        private static bool TryGetUnsignedBitPattern(ref SValue v, out ulong bits, out int bitWidth)
        {
            bits = 0;
            bitWidth = 0;
            switch (v.eType)
            {
                case EVMType.Boolean:
                    bits = v.uint8Value != 0 ? 1UL : 0UL;
                    bitWidth = 8;
                    return true;
                case EVMType.UInt8:
                    bits = v.uint8Value;
                    bitWidth = 8;
                    return true;
                case EVMType.Int8:
                    bits = unchecked((ulong)(byte)(uint)(int)v.int8Value);
                    bitWidth = 8;
                    return true;
                case EVMType.Int16:
                    bits = unchecked((ulong)(ushort)(uint)(int)v.int16Value);
                    bitWidth = 16;
                    return true;
                case EVMType.UInt16:
                    bits = v.uint16Value;
                    bitWidth = 16;
                    return true;
                case EVMType.Int32:
                    bits = unchecked((ulong)(uint)v.int32Value);
                    bitWidth = 32;
                    return true;
                case EVMType.UInt32:
                    bits = v.uint32Value;
                    bitWidth = 32;
                    return true;
                case EVMType.Int64:
                    bits = unchecked((ulong)v.int64Value);
                    bitWidth = 64;
                    return true;
                case EVMType.UInt64:
                    bits = v.uint64Value;
                    bitWidth = 64;
                    return true;
                case EVMType.Float32:
                    bits = unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(v.float32Value));
                    bitWidth = 32;
                    return true;
                case EVMType.Float64:
                case EVMType.Num:
                    bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(v.float64Value));
                    bitWidth = 64;
                    return true;
            }

            if (v.sobject != null)
            {
                switch (v.sobject)
                {
                    case BoolObject o:
                        bits = o.value ? 1UL : 0UL;
                        bitWidth = 8;
                        return true;
                    case UInt8Object o:
                        bits = o.value;
                        bitWidth = 8;
                        return true;
                    case Int8Object o:
                        bits = unchecked((ulong)(byte)(uint)(int)o.value);
                        bitWidth = 8;
                        return true;
                    case Int16Object o:
                        bits = unchecked((ulong)(ushort)(uint)(int)o.value);
                        bitWidth = 16;
                        return true;
                    case UInt16Object o:
                        bits = o.value;
                        bitWidth = 16;
                        return true;
                    case Int32Object o:
                        bits = unchecked((ulong)(uint)o.value);
                        bitWidth = 32;
                        return true;
                    case UInt32Object o:
                        bits = o.value;
                        bitWidth = 32;
                        return true;
                    case Int64Object o:
                        bits = unchecked((ulong)o.value);
                        bitWidth = 64;
                        return true;
                    case UInt64Object o:
                        bits = o.value;
                        bitWidth = 64;
                        return true;
                    case Float32Object o:
                        bits = unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(o.value));
                        bitWidth = 32;
                        return true;
                    case Float64Object o:
                        bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(o.value));
                        bitWidth = 64;
                        return true;
                    case NumObject o:
                        bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(o.ToDouble()));
                        bitWidth = 64;
                        return true;
                }
            }

            return false;
        }

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
                    ESystemMethodCall.SystemConvertBool => Convert.ToBoolean(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertInt8 => Convert.ToByte(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertUInt8 => Convert.ToByte(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertSInt8 => Convert.ToSByte(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertInt16 => Convert.ToInt16(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertUInt16 => Convert.ToUInt16(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertInt32 => Convert.ToInt32(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertUInt32 => Convert.ToUInt32(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertInt64 => Convert.ToInt64(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertUInt64 => Convert.ToUInt64(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertFloat32 => Convert.ToSingle(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertFloat64 => Convert.ToDouble(raw, CultureInfo.InvariantCulture),
                    ESystemMethodCall.SystemConvertString => ConvertStringWithDataSupport(ref arg, raw),
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
                case EVMType.Boolean: return v.uint8Value != 0;
                case EVMType.UInt8: return v.uint8Value;
                case EVMType.Int8: return v.int8Value;
                case EVMType.Int16: return v.int16Value;
                case EVMType.UInt16: return v.uint16Value;
                case EVMType.Int32: return v.int32Value;
                case EVMType.UInt32: return v.uint32Value;
                case EVMType.Int64: return v.int64Value;
                case EVMType.UInt64: return v.uint64Value;
                case EVMType.Float32: return v.float32Value;
                case EVMType.Float64: return v.float64Value;
                case EVMType.Num: return v.float64Value;
                case EVMType.String: return v.stringValue ?? string.Empty;
                default: break;
            }
            if (v.sobject != null)
            {
                switch (v.sobject)
                {
                    case BoolObject o: return o.value;
                    case UInt8Object o: return o.value;
                    case Int8Object o: return o.value;
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

        private static object ConvertStringWithDataSupport(ref SValue arg, object raw)
        {
            if (DataSystemMethodCall.TryBuildDataString(ref arg, out var dataText))
            {
                return dataText;
            }

            return raw?.ToString() ?? string.Empty;
        }
    }
}
