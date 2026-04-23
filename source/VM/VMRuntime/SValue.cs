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
        public EVMType eType;
        private NumericUnion nv;
        public string? stringValue;
        public SObject? sobject;
        public bool isNull;

        public byte int8Value { get => nv.i8; set => nv.i8 = value; }
        public sbyte sint8Value { get => nv.si8; set => nv.si8 = value; }
        //public char charValue;
        public short int16Value { get => nv.i16; set => nv.i16 = value; }
        public ushort uint16Value { get => nv.ui16; set => nv.ui16 = value; }
        public int int32Value { get => nv.i32; set => nv.i32 = value; }
        public uint uint32Value { get => nv.u32; set => nv.u32 = value; }
        public long int64Value { get => nv.i64; set => nv.i64 = value; }
        public ulong uint64Value { get => nv.u64; set => nv.u64 = value; }
        public float floatValue { get => nv.f; set => nv.f = value; }
        public double doubleValue { get => nv.d; set => nv.d = value; }
        public object? ToClrObject(Type targetType)
        {
            if (isNull) return null;

            // NativeBridge/BridgeObject 鍙傛暟钀藉湴锛歏M 渚у皢 BridgeObject 瀹炰緥瑙ｆ瀽鎴愮洰鏍?CLR 绫诲瀷銆?
            // BridgeObject 鍦?Front 閲岄€氳繃 _init_(string type) 浣滀负鈥滃弬鏁版弿杩扮鈥濅紶鍏ワ紝
            // 鍦?VM 杩愯鏃堕€氬父浼氳鍐欏叆鏌愪釜鍙鎴愬憳鍙橀噺锛堝父瑙佷负 `type`锛夛紝鍥犳杩欓噷鍋氬绉拌浆鎹€?
            if (sobject is ClassObject co && IsBridgeObjectRuntime(co.runtimeClass))
            {
                if (TryExtractBridgeObjectPayload(co, out var payloadObj))
                {
                    if (payloadObj == null) return null;
                    if (targetType == typeof(object)) return payloadObj;
                    if (targetType == typeof(string))
                        return payloadObj is string s ? s : payloadObj.ToString();

                    if (targetType.IsInstanceOfType(payloadObj)) return payloadObj;

                    // BridgeObject 閲岀殑鍊煎父甯告槸瀛楃涓插舰寮忥紙渚嬪 BridgeObject(123) 浼氳惤涓?"123"锛?
                    if (payloadObj is string payloadStr)
                    {
                        if (targetType == typeof(int) && int.TryParse(payloadStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                            return i;
                        if (targetType == typeof(long) && long.TryParse(payloadStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                            return l;
                        if (targetType == typeof(float) && float.TryParse(payloadStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var f))
                            return f;
                        if (targetType == typeof(double) && double.TryParse(payloadStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d))
                            return d;
                        if (targetType == typeof(bool))
                        {
                            if (bool.TryParse(payloadStr, out var b)) return b;
                            if (int.TryParse(payloadStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bi)) return bi != 0;
                        }
                    }

                    // 鏈€鍚庡厹搴曪細灏濊瘯绯荤粺杞崲锛堝閮ㄥ垎鏁板€?鏋氫妇鍙兘鏈夋晥锛?
                    if (targetType.IsEnum)
                    {
                        try { return Enum.ToObject(targetType, payloadObj); } catch { /* ignore */ }
                    }
                    try { return Convert.ChangeType(payloadObj, targetType); } catch { /* ignore */ }
                }
            }

            if (targetType == typeof(object)) return GetValueObject();
            if (targetType == typeof(string)) return eType == EVMType.String ? stringValue : GetValueObject()?.ToString();
            if (targetType == typeof(bool)) return eType == EVMType.Boolean ? (int8Value == 1) : Convert.ToBoolean(GetValueObject());
            if (targetType == typeof(int)) return eType == EVMType.Int32 ? int32Value : Convert.ToInt32(GetValueObject());
            if (targetType == typeof(long)) return eType == EVMType.Int64 ? int64Value : Convert.ToInt64(GetValueObject());
            if (targetType == typeof(float)) return eType == EVMType.Float32 ? floatValue : Convert.ToSingle(GetValueObject());
            if (targetType == typeof(double)) return eType == EVMType.Float64 ? doubleValue : Convert.ToDouble(GetValueObject());
            return Convert.ChangeType(GetValueObject(), targetType);
        }

        private static bool IsBridgeObjectRuntime(RuntimeClass? runtimeClass)
        {
            if (runtimeClass == null) return false;
            var n = runtimeClass.name ?? string.Empty;
            return n.EndsWith("BridgeObject", StringComparison.Ordinal) || n.Contains(".BridgeObject", StringComparison.Ordinal);
        }

        private static bool TryExtractBridgeObjectPayload(ClassObject co, out object? payloadObj)
        {
            payloadObj = null;
            var rc = co.runtimeClass;
            if (rc == null) return false;

            var vars = rc.nonStaticIRMetaVariableList;
            if (vars == null || vars.Count == 0) return false;

            // _init_ 鍙傛暟鍚嶆槸 `type`锛屽洜姝や紭鍏堣杩欎釜鎴愬憳鍙橀噺锛涘鏋滀笉瀛樺湪锛屽垯閫€鍖栦负璇诲彇绗竴涓垚鍛樺彉閲忋€?
            int index = -1;
            for (int i = 0; i < vars.Count; i++)
            {
                var vn = vars[i]?.name ?? string.Empty;
                if (string.Equals(vn, "type", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(vn, "_type", StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
            if (index < 0) index = 0;

            var sv = default(SValue);
            co.GetMemberVariableSValue(index, ref sv);
            payloadObj = sv.GetValueObject();
            return true;
        }

        public static SValue FromClrObject(object? o)
        {
            var v = default(SValue);
            if (o == null)
            {
                v.SetNull();
                return v;
            }

            switch (o)
            {
                case bool b: v.SetBoolValue(b); break;
                case byte b8: v.SetInt8Value(b8); break;
                case sbyte sb8: v.SetSInt8Value(sb8); break;
                case short i16: v.SetInt16Value(i16); break;
                case ushort u16: v.SetUInt16Value(u16); break;
                case int i32: v.SetInt32Value(i32); break;
                case uint u32: v.SetUInt32Value(u32); break;
                case long i64: v.SetInt64Value(i64); break;
                case ulong u64: v.SetUInt64Value(u64); break;
                case float f: v.SetFloatValue(f); break;
                case double d: v.SetDoubleValue(d); break;
                case string s: v.SetStringValue(s); break;
                default: v.SetNull(); break;
            }
            return v;
        }
        public void SetNullValueType()
        {
            eType = EVMType.Null;
            SetNull();
        }
        public void SetNull()
        {
            isNull = true;
            int8Value = 0;
            sint8Value = 0;
            int16Value = 0;
            uint16Value = 0;
            int32Value = 0;
            uint32Value = 0;
            int64Value = 0;
            uint64Value = 0;
            floatValue = 0;
            doubleValue = 0;
            stringValue = null;
            sobject = null;
        }
        public void SetBoolValue( bool val )
        {
            isNull = false;
            eType = EVMType.Boolean;
            int8Value = val ? (byte)1 : (byte)0;
        }
        public void SetInt8Value(byte val)
        {
            eType = EVMType.UInt8;
            int8Value = val;
            isNull = false;
        }
        public void SetSInt8Value(sbyte val)
        {
            eType = EVMType.Int8;
            sint8Value = val;
            isNull = false;
        }
        //public void SetCharValue(char val)
        //{
        //    eType = EVMType.Char;
        //    charValue = val;
        //}
        public void SetInt16Value(Int16 val)
        {
            eType = EVMType.Int16;
            int16Value = val;
            isNull = false;
        }
        public void SetUInt16Value(UInt16 val)
        {
            eType = EVMType.UInt16;
            uint16Value = val;
            isNull = false;
        }
        public void SetInt32Value(Int32 val)
        {
            eType = EVMType.Int32;
            int32Value = val;
            isNull = false;
        }
        public void SetUInt32Value(UInt32 val)
        {
            eType = EVMType.UInt32;
            uint32Value = val;
            isNull = false;
        }
        public void SetInt64Value(Int64 val)
        {
            eType = EVMType.Int64;
            int64Value = val;
            isNull = false;
        }
        public void SetUInt64Value(UInt64 val)
        {
            eType = EVMType.UInt64;
            uint64Value = val;
        }
        // helper conversions used by ComputeSVAlue
        bool IsUnsignedType(EVMType t)
        {
            return t == EVMType.UInt16 || t == EVMType.UInt32 || t == EVMType.UInt64;
        }
        double ConvertToDoubleFromIntTypes()
        {
            switch (eType)
            {
                case EVMType.UInt8: return int8Value;
                case EVMType.Int8: return sint8Value;
                case EVMType.Int16: return int16Value;
                case EVMType.UInt16: return uint16Value;
                case EVMType.Int32: return int32Value;
                case EVMType.UInt32: return uint32Value;
                case EVMType.Int64: return int64Value;
                case EVMType.UInt64: return (double)uint64Value;
            }
            return 0.0;
        }
        ulong ConvertToULong()
        {
            switch (eType)
            {
                case EVMType.UInt8: return int8Value;
                case EVMType.Int8: return (byte)sint8Value;
                case EVMType.Int16: return (ushort)int16Value;
                case EVMType.UInt16: return uint16Value;
                case EVMType.Int32: return (uint)int32Value;
                case EVMType.UInt32: return uint32Value;
                case EVMType.Int64: return (ulong)int64Value;
                case EVMType.UInt64: return uint64Value;
                default: return 0;
            }
        }
        long ConvertToLong()
        {
            switch (eType)
            {
                case EVMType.UInt8: return int8Value;
                case EVMType.Int8: return sint8Value;
                case EVMType.Int16: return int16Value;
                case EVMType.UInt16: return uint16Value;
                case EVMType.Int32: return int32Value;
                case EVMType.UInt32: return uint32Value;
                case EVMType.Int64: return int64Value;
                case EVMType.UInt64: return (long)uint64Value;
                default: return 0;
            }
        }
        void AssignULongToType(ulong v)
        {
            switch (eType)
            {
                case EVMType.UInt64: uint64Value = v; break;
                case EVMType.Int64: int64Value = (long)v; break;
                case EVMType.UInt32: uint32Value = (uint)v; break;
                case EVMType.Int32: int32Value = (int)v; break;
                case EVMType.UInt16: uint16Value = (ushort)v; break;
                case EVMType.Int16: int16Value = (short)v; break;
                case EVMType.UInt8: int8Value = (byte)v; break;
                case EVMType.Int8: sint8Value = (sbyte)v; break;
                default: break;
            }
        }
        void AssignLongToType(long v)
        {
            switch (eType)
            {
                case EVMType.Int64: int64Value = v; break;
                case EVMType.UInt64: uint64Value = (ulong)v; break;
                case EVMType.Int32: int32Value = (int)v; break;
                case EVMType.UInt32: uint32Value = (uint)v; break;
                case EVMType.Int16: int16Value = (short)v; break;
                case EVMType.UInt16: uint16Value = (ushort)v; break;
                case EVMType.UInt8: int8Value = (byte)v; break;
                case EVMType.Int8: sint8Value = (sbyte)v; break;
                default: break;
            }
        }
        public void SetFloatValue(Single val)
        {
            eType = EVMType.Float32;
            floatValue = val;
            isNull = false;
        }
        public void SetDoubleValue(Double val)
        {
            eType = EVMType.Float64;
            doubleValue = val;
            isNull = false;
        }
        public void SetStringValue(string val)
        {
            eType = EVMType.String;
            stringValue = val;
            isNull = false;


            Log.AddRuntimeLog(LID.ShowMessageInfo, "SValue.SetStringValue" + val );
        }
        public void SetValue(SObject val)
        {
            eType = val.eType;
            SetTypeValue(eType, val.value );
        }
        public void SetTypeValue( EVMType etype, object tobj )
        {
            switch (eType)
            {
                case EVMType.UInt8:
                    {
                        int8Value = (byte)(tobj);
                    }
                    break;
                case EVMType.Boolean:
                    {
                        int8Value = int.Parse(tobj.ToString()) == 1 ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.Int8:
                    {
                        sint8Value = (sbyte)(tobj);
                    }
                    break;
                //case EVMType.Char:
                //    {
                //        charValue = (char)(tobj);
                //    }
                //    break;
                case EVMType.Int16:
                    {
                        int16Value = (short)(tobj);
                    }
                    break;
                case EVMType.UInt16:
                    {
                        uint16Value = (ushort)(tobj);
                    }
                    break;
                case EVMType.Int32:
                    {
                        int32Value = (int)(tobj);
                    }
                    break;
                case EVMType.UInt32:
                    {
                        uint32Value = (uint)(tobj);
                    }
                    break;
                case EVMType.Int64:
                    {
                        int64Value = (long)(tobj);
                    }
                    break;
                case EVMType.UInt64:
                    {
                        uint64Value = (ulong)(tobj);
                    }
                    break;
                case EVMType.Float32:
                    {
                        floatValue = (float)(tobj);
                    }
                    break;
                case EVMType.Float64:
                    {
                        doubleValue = (double)(tobj);
                    }
                    break;
                case EVMType.String:
                    {
                        stringValue = tobj as String;
                        isNull = stringValue == null;
                    }
                    break;
                case EVMType.Array:
                    {
                        sobject = tobj as ArrayObject;
                        isNull = sobject == null;
                    }
                    break;
                case EVMType.Type:
                    {
                        sobject = tobj as TypeObject;
                        isNull = sobject == null;
                    }
                    break;
                case EVMType.Object:
                    {
                        sobject = tobj as SObject;
                        isNull = sobject == null;
                    }
                    break;
                case EVMType.Class:
                    {
                        sobject = tobj as ClassObject;
                        isNull = sobject == null;
                    }
                    break;
                default:
                    {
                        Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "SetTypeValue", eType.ToString());
                    }
                    break;
            }
        }
        public void SetSObject(SObject val)
        {
            if( val == null )
            {
                isNull = true;
                return;
            }
            isNull = val == null;
            if (isNull)
            {
                return;
            }
            isNull = false;
            switch (val)
            {
                case VoidObject voidobj:
                    {

                    }
                    break;
                case BoolObject boolobj:
                    {
                        eType = EVMType.Boolean;
                        int8Value = boolobj.value ? (byte)1 : (byte)0;
                    }
                    break;
                case UInt8Object int8obj:
                    {
                        eType = EVMType.UInt8;
                        int8Value = (byte)int8obj.value;
                    }
                    break;
                case Int8Object sint8obj:
                    {
                        eType = EVMType.Int8;
                        sint8Value = sint8obj.value;
                    }
                    break;
                //case CharObject charObj:
                //    {
                //        eType = EVMType.Char;
                //        charValue = charObj.value;
                //    }
                //    break;
                case Int16Object int16obj:
                    {
                        eType = EVMType.Int16;
                        int16Value = int16obj.value;
                    }
                    break;
                case UInt16Object uint16obj:
                    {
                        eType = EVMType.UInt16;
                        uint16Value = uint16obj.value;
                    }
                    break;
                case Int32Object int32obj:
                    {
                        eType = EVMType.Int32;
                        int32Value = int32obj.value;
                    }
                    break;
                case UInt32Object uint32obj:
                    {
                        eType = EVMType.UInt32;
                        uint32Value = uint32obj.value;
                    }
                    break;
                case Int64Object int64obj:
                    {
                        eType = EVMType.Int64;
                        int64Value = int64obj.value;
                    }
                    break;
                case UInt64Object uint64obj:
                    {
                        eType = EVMType.UInt64;
                        uint64Value = uint64obj.value;
                    }
                    break;
                case Float32Object floatobj:
                    {
                        eType = EVMType.Float32;
                        floatValue = floatobj.value;
                    }
                    break;
                case Float64Object doubleobj:
                    {
                        eType = EVMType.Float64;
                        doubleValue = doubleobj.value;
                    }
                    break;
                case StringObject stringobj:
                    {
                        eType = EVMType.String;
                        stringValue = stringobj.value;
                    }
                    break;
                case ArrayObject arrayobj:
                    {
                        eType = EVMType.Array;
                        sobject = arrayobj;
                    }
                    break;
                case TemplateObject templateobj:
                    {
                        if (templateobj == null )
                        {
                            this.SetNull();
                            return;
                        }
                        eType = templateobj.eType;
                        SetTypeValue(eType, templateobj.value );
                    }
                    break;
                default:
                    {
                        if (val.eType == EVMType.Object)
                        {
                            eType = EVMType.Object;
                            sobject = val;
                        }
                        else if (val.eType == EVMType.Array
                            || val.eType == EVMType.Class )
                        {
                            eType = EVMType.Class;
                            sobject = val;
                        }
                        else if( val.eType == EVMType.Type )
                        {
                            eType = EVMType.Type;
                            sobject = val;
                        }
                        else
                        {
                            SetSObject(val.value as SObject);
                        }
                    }
                    break;
            }
        }
        public SObject GetSObject()
        {        
            if (isNull)
            {
                return null;
            }
            SObject sobj = null;
            switch (eType)
            {
                //case EVMType.RawBoolean:
                case EVMType.Boolean:
                    {
                        sobj = new BoolObject(this.int8Value == 1);
                    }
                    break;
                //case EVMType.RawByte:
                case EVMType.UInt8:
                    {
                        sobj = new UInt8Object(this.int8Value );
                    }
                    break;
                //case EVMType.RawSByte:
                case EVMType.Int8:
                    {
                        sobj = new Int8Object(this.sint8Value);
                    }
                    break;
                //case EVMType.RawInt16:
                case EVMType.Int16:
                    {
                        sobj = new Int16Object(this.int16Value);
                    }
                    break;
                //case EVMType.RawUInt16:
                case EVMType.UInt16:
                    {
                        sobj = new UInt16Object(this.uint16Value);
                    }
                    break;
                //case EVMType.RawInt32:
                case EVMType.Int32:
                    {
                        sobj = new Int32Object(this.int32Value);
                    }
                    break;
                //case EVMType.RawUInt32:
                case EVMType.UInt32:
                    {
                        sobj = new UInt32Object(this.uint32Value);
                    }
                    break;
                //case EVMType.RawInt64:
                case EVMType.Int64:
                    {
                        sobj = new Int64Object(this.int64Value);
                    }
                    break;
                //case EVMType.RawUInt64:
                case EVMType.UInt64:
                    {
                        sobj = new UInt64Object(this.uint64Value );
                    }
                    break;
                //case EVMType.RawFloat32:
                case EVMType.Float32:
                    {
                        sobj = new Float32Object(this.floatValue);
                    }
                    break;
                //case EVMType.RawFloat64:
                case EVMType.Float64:
                    {
                        sobj = new Float64Object(this.doubleValue);
                    }
                    break;
                //case EVMType.RawString:
                case EVMType.String:
                    {
                        sobj = new StringObject(this.stringValue);
                    }
                    break;
                default:
                    {
                        sobj = this.sobject;
                        Debug.Assert(sobj != null);
                    }
                    break;
            }

            return sobj;
        }
        /// <summary>
        /// 赋值/槽位写入前：在标量/布尔之间做与 <see cref="ConvertByEType"/> 一致的阶兼容（如 Int32 → Int8），
        /// 行为接近 Dart 中 num 的宽松窄化/拓宽（由 CLR <see cref="Convert"/> 与截断完成）。
        /// </summary>
        public void TryCoerceScalarForAssignment(EVMType targetEvm)
        {
            if (isNull) return;
            if (eType == targetEvm) return;
            if (!IsScalarOrNumSlotEvm(targetEvm)) return;
            if (!IsScalarOrNumSlotEvm(eType) && eType != EVMType.Boolean) return;
            try
            {
                ConvertByEType(targetEvm);
            }
            catch
            {
                // 保持原值，由后续分支决定失败表现
            }
        }

        private static bool IsScalarOrNumSlotEvm(EVMType t)
        {
            return t is >= EVMType.Boolean and <= EVMType.Num;
        }

        public void ConvertByEType(EVMType neType )
        {
            object cur = GetValueObject();
            
            switch (neType)
            {
                case EVMType.Boolean:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (byte)(Convert.ToInt32(cur, CultureInfo.InvariantCulture) != 0 ? 1 : 0);
                    }
                    break;
                case EVMType.UInt8:
                    {
                        eType = EVMType.UInt8;
                        int8Value = Convert.ToByte(cur);
                    }
                    break;
                case EVMType.Int8:
                    {
                        eType = EVMType.Int8;
                        sint8Value = Convert.ToSByte(cur);
                    }
                    break;
                case EVMType.Int16:
                    {
                        eType = EVMType.Int16;
                        int16Value = Convert.ToInt16(cur);
                    }
                    break;
                case EVMType.UInt16:
                    {
                        eType = EVMType.UInt16;
                        uint16Value = Convert.ToUInt16(cur);
                    }
                    break;
                case EVMType.Int32:
                    {
                        eType = EVMType.Int32;
                        int32Value = Convert.ToInt32(cur);
                    }
                    break;
                case EVMType.UInt32:
                    {
                        eType = EVMType.UInt32;
                        uint32Value = Convert.ToUInt32(cur);
                    }
                    break;
                case EVMType.Int64:
                    {
                        eType = EVMType.Int64;
                        int64Value = Convert.ToInt64(cur);
                    }
                    break;
                case EVMType.UInt64:
                    {
                        eType = EVMType.UInt64;
                        uint64Value = Convert.ToUInt64(cur);
                    }
                    break;
                case EVMType.Float32:
                    {
                        eType = EVMType.Float32;
                        floatValue = Convert.ToSingle(cur);
                    }
                    break;
                case EVMType.Float64:
                    {
                        eType = EVMType.Float64;
                        doubleValue = Convert.ToDouble(cur);
                    }
                    break;
                case EVMType.Num:
                    {
                        eType = EVMType.Num;
                        doubleValue = Convert.ToDouble(cur);
                    }
                    break;
                case EVMType.String:
                    {
                        eType = EVMType.String;
                        stringValue = cur.ToString();
                    }
                    break;
                default:
                    {
                        Debug.Write("Error 寮傚父绫诲瀷鍦–onvertByEType涓");
                    }
                    break;
            }
            isNull = false;
        }
        public Object GetValueObject()
        {
            switch (this.eType)
            {
                case EVMType.Boolean:
                    {
                        return int8Value == 1;
                    }
                case EVMType.UInt8:
                    {
                        return int8Value;
                    }
                case EVMType.Int8:
                    {
                        return sint8Value;
                    }
                //case EVMType.Char:
                //    {
                //        return charValue;
                //    }
                case EVMType.Int16:
                    {
                        return int16Value;
                    }
                case EVMType.UInt16:
                    {
                        return uint16Value;
                    }
                case EVMType.Int32:
                    {
                        return int32Value;
                    }
                case EVMType.UInt32:
                    {
                        return uint32Value;
                    }
                case EVMType.Int64:
                    {
                        return int64Value;
                    }
                case EVMType.UInt64:
                //case EVMType.RawUInt64:
                    {
                        return uint64Value;
                    }
                case EVMType.Float32:
                    {
                        return floatValue;
                    }
                case EVMType.Float64:
                    {
                        return doubleValue;
                    }
                case EVMType.Num:
                    {
                        return doubleValue;
                    }
                case EVMType.String:
                    {
                        return stringValue;
                    }
                default:return sobject;
            }
        }
        public SObject CreateSObject()
        {
            switch( eType )
            {
                case EVMType.UInt8:
                    {
                        return new UInt8Object(int8Value);
                    }
                case EVMType.Boolean:
                    {
                        return new BoolObject(int8Value == 1);
                    }
                case EVMType.Int8:
                    {
                        return new Int8Object(sint8Value);
                    }
                //case EVMType.Char:
                //    {
                //        return new CharObject(charValue);
                //    }
                case EVMType.Int16:
                    {
                        return new Int16Object(int16Value);
                    }
                case EVMType.UInt16:
                    {
                        return new UInt16Object(uint16Value);
                    }
                case EVMType.Int32:
                    {
                        return new Int32Object(int32Value);
                    }
                case EVMType.UInt32:
                    {
                        return new UInt32Object(uint32Value);
                    }
                case EVMType.Int64:
                    {
                        return new Int64Object(int64Value);
                    }
                case EVMType.UInt64:
                    {
                        return new UInt64Object(uint64Value);
                    }
                case EVMType.String:
                    {
                        return new StringObject(stringValue);
                    }
                default:
                    {
                        return sobject;
                    }
            }
            //return sobject;
        }       
    }
}
