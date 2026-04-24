//****************************************************************************
//  File:      ArrayObjectElementStore.cs
//  Description: 数组元素紧凑存储：byte[ length * 单位长度 ]，标量可空为 1 字节标志 + 原始宽度；
//               引用 / String 在 byte[] 中存 4 字节 Hash，真实对象/字符串在并行槽位。读取以 byte 解析为准。
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using System.Buffers.Binary;

namespace SimpleLanguage.VM
{
    /// <summary>
    /// 与 <see cref="MemberDataLayout"/> 标量宽度一致；可空为 1 字节标志（0=null）+ 原样；bool 为单字节 0/1/2；引用为 4 字节 Hash + 旁路。
    /// </summary>
    internal sealed class ArrayObjectElementStore
    {
        public int Length { get; }
        public int UnitLength { get; }
        public EVMType ElementEvmType { get; }
        public RuntimeType ElementRuntimeType { get; }

        private readonly byte[] _data;
        private readonly SObject?[]? _objectRefs;
        private readonly string?[]? _stringRefs;

        public ArrayObjectElementStore(RuntimeType elementType, int length)
        {
            ElementRuntimeType = elementType;
            ElementEvmType = elementType.eType;
            Length = length;
            if (length < 0)
            {
                UnitLength = 0;
                _data = System.Array.Empty<byte>();
                return;
            }

            if (IsRefKind(ElementEvmType, out var refKindString))
            {
                UnitLength = 4;
                _data = new byte[checked(length * 4)];
                if (refKindString)
                {
                    _stringRefs = new string[length];
                }
                else
                {
                    _objectRefs = new SObject[length];
                    if (ElementEvmType == EVMType.Object)
                    {
                        for (int i = 0; i < length; i++)
                        {
                            var s = new SObject(EVMType.Object);
                            _objectRefs[i] = s;
                            WriteInt32At(i, s.GetHashCode());
                        }
                    }
                    else
                    {
                        for (int i = 0; i < length; i++)
                            WriteInt32At(i, 0);
                    }
                }
            }
            else
            {
                UnitLength = GetScalarUnitLength(ElementEvmType);
                _data = length == 0 ? System.Array.Empty<byte>() : new byte[checked(length * UnitLength)];
            }
        }

        private void WriteInt32At(int index, int value)
        {
            int o = index * 4;
            if ((uint)index >= (uint)Length) return;
            BinaryPrimitives.WriteInt32LittleEndian(_data.AsSpan(o, 4), value);
        }

        private static int GetScalarUnitLength(EVMType t)
        {
            return t switch
            {
                EVMType.Boolean => 1,
                EVMType.UInt8 or EVMType.Int8 => 2,
                EVMType.Int16 or EVMType.UInt16 => 3,
                EVMType.Int32 or EVMType.UInt32 or EVMType.Float32 => 5,
                EVMType.Int64 or EVMType.UInt64 or EVMType.Float64 => 9,
                _ => 4,
            };
        }

        public static bool IsRefKind(EVMType t, out bool isString) =>
            IsRefKindStatic(t, out isString);

        private static bool IsRefKindStatic(EVMType t, out bool isString)
        {
            isString = t == EVMType.String;
            return t is EVMType.String or EVMType.Object or EVMType.Type or EVMType.Class or EVMType.Array;
        }

        public void LoadSValue(int index, ref SValue sval, EVMType arrayEvm)
        {
            if ((uint)index >= (uint)Length) return;
            sval.eType = arrayEvm;
            if (IsRefKindStatic(arrayEvm, out var strKind))
            {
                if (strKind)
                {
                    var s = _stringRefs![index];
                    sval.eType = EVMType.String;
                    if (s == null) { sval.SetNull(); return; }
                    sval.SetStringValue(s);
                    return;
                }
                var o = _objectRefs![index];
                if (o == null) { sval.SetNull(); sval.eType = arrayEvm; return; }
                object? payload = o;
                if (o.eType == EVMType.Object)
                    payload = o.value;
                sval.eType = arrayEvm;
                sval.SetTypeValue(arrayEvm, payload);
                return;
            }
            if (IsScalarNull(index, arrayEvm))
            {
                sval.SetNull();
                sval.eType = arrayEvm;
                return;
            }
            sval.eType = arrayEvm;
            sval.isNull = false;
            int oi = index * UnitLength;
            var spanT = _data.AsSpan(oi);
            if (arrayEvm == EVMType.Boolean)
            {
                sval.eType = EVMType.Boolean;
                sval.uint8Value = (byte)(spanT[0] == 2 ? 1 : 0);
                return;
            }
            // tag [0] == 1, payload 从 [1] 起
            var w = _data.AsSpan(oi + 1);
            switch (arrayEvm)
            {
                case EVMType.UInt8: sval.SetUInt8Value(w[0]); break;
                case EVMType.Int8: sval.SetInt8Value(unchecked((sbyte)w[0])); break;
                case EVMType.Int16: sval.SetInt16Value(BinaryPrimitives.ReadInt16LittleEndian(w)); break;
                case EVMType.UInt16: sval.SetUInt16Value(BinaryPrimitives.ReadUInt16LittleEndian(w)); break;
                case EVMType.Int32: sval.SetInt32Value(BinaryPrimitives.ReadInt32LittleEndian(w)); break;
                case EVMType.UInt32: sval.SetUInt32Value(BinaryPrimitives.ReadUInt32LittleEndian(w)); break;
                case EVMType.Int64: sval.SetInt64Value(BinaryPrimitives.ReadInt64LittleEndian(w)); break;
                case EVMType.UInt64: sval.SetUInt64Value(BinaryPrimitives.ReadUInt64LittleEndian(w)); break;
                case EVMType.Float32: sval.SetFloatValue(BinaryPrimitives.ReadSingleLittleEndian(w)); break;
                case EVMType.Float64: sval.SetDoubleValue(BinaryPrimitives.ReadDoubleLittleEndian(w)); break;
                default: sval.isNull = true; break;
            }
        }

        public bool IsScalarNull(int index, EVMType t)
        {
            int o = index * UnitLength;
            if (t == EVMType.Boolean)
                return _data[o] == 0;
            return _data[o] == 0;
        }

        public object? GetBoxedValue(int index)
        {
            if ((uint)index >= (uint)Length) return null;
            if (IsRefKindStatic(ElementEvmType, out var strKind))
            {
                if (strKind) return _stringRefs![index];
                return _objectRefs![index];
            }
            if (IsScalarNull(index, ElementEvmType)) return null;
            int o = index * UnitLength;
            return ElementEvmType switch
            {
                EVMType.Boolean => _data[o] == 2,
                EVMType.UInt8 => _data[o + 1],
                EVMType.Int8 => unchecked((sbyte)_data[o + 1]),
                EVMType.Int16 => BinaryPrimitives.ReadInt16LittleEndian(_data.AsSpan(o + 1, 2)),
                EVMType.UInt16 => BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(o + 1, 2)),
                EVMType.Int32 => BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(o + 1, 4)),
                EVMType.UInt32 => BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(o + 1, 4)),
                EVMType.Int64 => BinaryPrimitives.ReadInt64LittleEndian(_data.AsSpan(o + 1, 8)),
                EVMType.UInt64 => BinaryPrimitives.ReadUInt64LittleEndian(_data.AsSpan(o + 1, 8)),
                EVMType.Float32 => BinaryPrimitives.ReadSingleLittleEndian(_data.AsSpan(o + 1, 4)),
                EVMType.Float64 => BinaryPrimitives.ReadDoubleLittleEndian(_data.AsSpan(o + 1, 8)),
                _ => null,
            };
        }

        public SObject? GetSObjectAt(int index) =>
            (uint)index < (uint)Length && _objectRefs != null ? _objectRefs[index] : null;

        public void SetObjectSlotToNull(int index)
        {
            if ((uint)index >= (uint)Length || _objectRefs == null) return;
            // object[] 槽位在构造时已放入 SObject(EVMType.Object) 外壳；只清空负载，不置 _objectRefs 为 null，
            // 否则与 StoreValue 中 anyobj 路径及 Load/写入约定不一致。
            if (ElementEvmType == EVMType.Object)
            {
                var o = _objectRefs[index];
                if (o != null && o.eType == EVMType.Object)
                {
                    o.SetValue(null);
                }
                else
                {
                    var fresh = new SObject(EVMType.Object);
                    ObjectManager.RegisterObject(fresh);
                    _objectRefs[index] = fresh;
                    WriteInt32At(index, fresh.GetHashCode());
                }
                return;
            }
            _objectRefs[index] = null;
            WriteInt32At(index, 0);
        }

        public void StoreFromSValue(int index, SValue svalue, EVMType arrayEvm)
        {
            if ((uint)index >= (uint)Length) return;
            if (IsRefKindStatic(arrayEvm, out var strKind))
            {
                if (svalue.isNull)
                {
                    if (strKind) { _stringRefs![index] = null; WriteInt32At(index, 0); }
                    else
                    {
                        _objectRefs![index] = null;
                        WriteInt32At(index, 0);
                    }
                    return;
                }
                if (strKind)
                {
                    _stringRefs![index] = svalue.stringValue;
                    WriteInt32At(index, svalue.stringValue == null ? 0 : svalue.stringValue.GetHashCode());
                }
                else
                {
                    var so = svalue.sobject;
                    _objectRefs![index] = so;
                    if (so != null) WriteInt32At(index, so.GetHashCode());
                    else WriteInt32At(index, 0);
                }
                return;
            }
            if (svalue.isNull) { WriteNullScalar(index, arrayEvm); return; }
            WriteNonNullScalar(index, svalue, arrayEvm);
        }

        public void WriteNullScalar(int index, EVMType t)
        {
            int o = index * UnitLength;
            if (t == EVMType.Boolean) { if (o < _data.Length) _data[o] = 0; return; }
            for (int i = 0; i < UnitLength && o + i < _data.Length; i++) _data[o + i] = 0;
        }

        public void WriteNonNullScalar(int index, SValue s, EVMType t)
        {
            int o = index * UnitLength;
            if (t == EVMType.Boolean) { _data[o] = (byte)(s.int8Value == 1 ? 2 : 1); return; }
            _data[o] = 1;
            var w = _data.AsSpan(o + 1);
            switch (t)
            {
                case EVMType.UInt8: w[0] = s.uint8Value; break;
                case EVMType.Int8: w[0] = unchecked((byte)s.int8Value); break;
                case EVMType.Int16: BinaryPrimitives.WriteInt16LittleEndian(w, s.int16Value); break;
                case EVMType.UInt16: BinaryPrimitives.WriteUInt16LittleEndian(w, s.uint16Value); break;
                case EVMType.Int32: BinaryPrimitives.WriteInt32LittleEndian(w, s.int32Value); break;
                case EVMType.UInt32: BinaryPrimitives.WriteUInt32LittleEndian(w, s.uint32Value); break;
                case EVMType.Int64: BinaryPrimitives.WriteInt64LittleEndian(w, s.int64Value); break;
                case EVMType.UInt64: BinaryPrimitives.WriteUInt64LittleEndian(w, s.uint64Value); break;
                case EVMType.Float32: BinaryPrimitives.WriteSingleLittleEndian(w, s.float32Value); break;
                case EVMType.Float64: BinaryPrimitives.WriteDoubleLittleEndian(w, s.float64Value); break;
            }
        }

        public bool TryStoreCoercedNumber(int index, SValue svalue, EVMType arrayEvm)
        {
            if (IsRefKindStatic(arrayEvm, out _) || svalue.isNull) return false;
            if (svalue.eType == EVMType.Null) { WriteNullScalar(index, arrayEvm); return true; }
            if (!ArrayObject.TryGetNumericAsDouble(svalue, out var d)) return false;
            var tmp = default(SValue);
            switch (arrayEvm)
            {
                case EVMType.UInt8: tmp.eType = EVMType.UInt8; tmp.uint8Value = (byte)Convert.ToByte(d); break;
                case EVMType.Int8: tmp.eType = EVMType.Int8; tmp.int8Value = (sbyte)Convert.ToSByte(d); break;
                case EVMType.Int16: tmp.eType = EVMType.Int16; tmp.int16Value = (short)Convert.ToInt16(d); break;
                case EVMType.UInt16: tmp.eType = EVMType.UInt16; tmp.uint16Value = (ushort)Convert.ToUInt16(d); break;
                case EVMType.Int32: tmp.eType = EVMType.Int32; tmp.int32Value = (int)Convert.ToInt32(d); break;
                case EVMType.UInt32: tmp.eType = EVMType.UInt32; tmp.uint32Value = (uint)Convert.ToUInt32(d); break;
                case EVMType.Int64: tmp.eType = EVMType.Int64; tmp.int64Value = (long)Convert.ToInt64(d); break;
                case EVMType.UInt64: tmp.eType = EVMType.UInt64; tmp.uint64Value = (ulong)Convert.ToUInt64(d); break;
                case EVMType.Float32: tmp.eType = EVMType.Float32; tmp.float32Value = (float)Convert.ToSingle(d); break;
                case EVMType.Float64: tmp.eType = EVMType.Float64; tmp.float64Value = d; break;
                case EVMType.Boolean: tmp.eType = EVMType.Boolean; tmp.uint8Value = (byte)(d != 0 ? 1 : 0); break;
                default: return false;
            }
            tmp.isNull = false;
            WriteNonNullScalar(index, tmp, arrayEvm);
            return true;
        }
    }
}
