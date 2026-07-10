//****************************************************************************
//  File:      ArrayObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime:  2022/11/22 12:00:00
//  Description: 数组元素使用 byte[] 紧凑存储�?
//                 DEBUG 下额外保�?<see cref="m_DebugArray"/> 便于对照，与存储同步写入；读取走 byte 路径�?
//****************************************************************************
using SimpleLanguage.Logging;
using SimpleLanguage.VM.Runtime;
using System.Buffers.Binary;
namespace SimpleLanguage.VM
{
    public class ArrayObject : ClassObject
    {
        public int length => m_Length;

#if DEBUG
        /// <summary>�?DEBUG：与�?<c>Array</c> 相同形状的镜像，供调试对照；生产环境�?null�?/summary>
        public Array? array => m_DebugArray;
        private Array? m_DebugArray;
#endif
        private byte[]? m_Data;
        private int m_UnitLength;
        private RuntimeType eArrayType = null;
        private int m_Length = 0;
        public ArrayObject(RuntimeType rt, int length )
        {
            m_Header.EType = (byte)EVMType.Array;
            m_RuntimeType = rt;
            if (rt?.runtimeTemplateList != null && rt.runtimeTemplateList.Count > 0)
            {
                eArrayType = rt.runtimeTemplateList[0];
            }
            else
            {
                eArrayType = RuntimeTypeManager.objectRuntimeType;
                Log.AddRuntimeLog(LID.ShowMessageWarning,
                    $"ArrayObject ctor missing element runtime type, fallback to Core.Object. runtimeClass={rt?.runtimeClass?.name ?? "null"}");
            }
            m_Length = length;

            var metaVariableList = m_RuntimeType.runtimeClass.nonStaticIRMetaVariableList;
            m_MemberRuntimeObjectArray = new RuntimeObject[metaVariableList.Count];
            for (int i = 0; i < m_MemberRuntimeObjectArray.Length; i++)
            {
                var rt2 = RuntimeVM.GetRuntimeTypeByDefType(metaVariableList[i].runtimeDefType, m_RuntimeType.runtimeClass, rt.runtimeTemplateList, true);

                SObject sobj = null;
                if( RuntimeTypeManager.IsCoreRuntimeType(rt2) )
                {
                    sobj = ObjectManager.CreateObjectByRuntimeType(rt2, false);
                }

                m_MemberRuntimeObjectArray[i] = new RuntimeObject(rt2, metaVariableList[i], sobj );
            }

            BuildMemberDataLayout();
        }
        public override void CreateObject()
        {
            base.CreateObject();

            var lengthSv = default(RuntimeValue);
            lengthSv.SetInt32Value(m_Length);
            SetMemberVariableSValue(0, lengthSv);

            CreateArray();
        }
        void CreateArray()
        {
            if(m_Length < 0 )
            {
                return;
            }
            if (IsRefKind(eArrayType.eType, out var _))
            {
                m_UnitLength = 4;
                m_Data = new byte[checked(m_Length * 4)];
                //if (eArrayType.eType == EVMType.Object)
                //{
                //    for (int i = 0; i < m_Length; i++)
                //    {
                //        var s = new SObject(EVMType.Object);
                //        ObjectManager.RegisterObject(s);
                //        WriteInt32At(i, s.id);
                //    }
                //}
            }
            else
            {
                m_UnitLength = EVMTypeUtils.GetScalarUnitLength(eArrayType.eType);
                m_Data = m_Length == 0 ? System.Array.Empty<byte>() : new byte[checked(m_Length * m_UnitLength)];
            }
#if DEBUG
            m_DebugArray = AllocateDebugArray();
            if (m_DebugArray != null && m_Data != null)
            {
                for (int i = 0; i < m_Length; i++)
                    DebugSyncIndex(i);
            }
#endif
        }

#if DEBUG
        private Array? AllocateDebugArray()
        {
            int length = m_Length;
            if (m_Length < 0) return null;
            return eArrayType.eType switch
            {
                EVMType.Boolean => new bool?[length],
                EVMType.UInt8 => new byte?[length],
                EVMType.Int8 => new sbyte?[length],
                EVMType.Int16 => new short?[length],
                EVMType.UInt16 => new ushort?[length],
                EVMType.Int32 => new int?[length],
                EVMType.UInt32 => new uint?[length],
                EVMType.Int64 => new long?[length],
                EVMType.UInt64 => new ulong?[length],
                EVMType.Float32 => new float?[length],
                EVMType.Float64 => new double?[length],
                EVMType.String => new String?[length],
                EVMType.Array => new ArrayObject[length],
                EVMType.Object => new SObject[length],
                EVMType.Type => new TypeObject[length],
                EVMType.Class => new ClassObject[length],
                _ => null,
            };
        }

        private void DebugSyncIndex(int index)
        {
            if (m_DebugArray == null || m_Data == null) return;
            m_DebugArray.SetValue(GetBoxedValueInternal(index), index);
        }
#endif

        public void LoadValue( int index, ref RuntimeValue sval )
        {
            if (index < 0)
            {
                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "loadvalue index < 0 ", index );
                return;
            }
            if (index >= m_Length )
            {
                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "loadvalue index >= length ", index );
                return;
            }
            if (m_Data == null) return;

            var arrayEvm = eArrayType.eType;
            if (IsRefKind(arrayEvm, out var strKind))
            {
                int id = ReadInt32At(index);
                var obj = ObjectManager.GetObjectById(id);
                if (obj == null)
                {
                    sval.SetNull();
                    sval.eType = arrayEvm;
                    return;
                }

                if (strKind)
                {
                    sval.eType = EVMType.String;
                    if (obj is StringObject so)
                        sval.SetStringValue(so.value);
                    else
                        sval.SetStringValue(obj.value?.ToString() ?? string.Empty);
                    return;
                }
                sval.SetRawSObject(obj);
                sval.eType = eArrayType.eType;
                return;
            }

            sval.eType = arrayEvm;
            sval.isNull = false;
            int oi = index * m_UnitLength;
            var w = m_Data.AsSpan(oi);
            switch (arrayEvm)
            {
                case EVMType.Boolean: sval.SetBoolValue(w[0] != 0); break;
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
                case EVMType.Num: sval.SetDoubleValue(BinaryPrimitives.ReadDoubleLittleEndian(w)); break;
                default: sval.isNull = true; break;
            }
        }

        public object? GetValue( int index )
        {
            if (index < 0)
            {
                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "getvalue index < 0 ", index);
                return null;
            }
            if (index >= m_Length)
            {
                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "getvalue index >= length ", index);
                return null;
            }
            return GetBoxedValueInternal(index);
        }
        public void StoreValue(int index, RuntimeValue RuntimeValue)
        {
            if (m_Data == null) return;

            if (TryStoreCoercedNumber(index, RuntimeValue, eArrayType.eType))
            {
#if DEBUG
                DebugSyncIndex(index);
#endif
                return;
            }

            if (RuntimeValue.eType == EVMType.Null)
            {
                WriteNullScalar(index, eArrayType.eType);
#if DEBUG
                DebugSyncIndex(index);
#endif
                return;
            }

            // 对对象类型存储不再走 anyobj 包装写入，统一按普通对象写入路径处理�?
            StoreFromSValueRaw(index, RuntimeValue, eArrayType.eType);
#if DEBUG
            DebugSyncIndex(index);
#endif
        }

        private static bool IsRefKind(EVMType t, out bool isString)
        {
            isString = t == EVMType.String;
            return t is EVMType.String or EVMType.Object or EVMType.Type or EVMType.Class or EVMType.Array;
        }


        private int ReadInt32At(int index)
        {
            if (m_Data == null || (uint)index >= (uint)m_Length) return 0;
            return BinaryPrimitives.ReadInt32LittleEndian(m_Data.AsSpan(index * 4, 4));
        }

        private void WriteInt32At(int index, int value)
        {
            if (m_Data == null || (uint)index >= (uint)m_Length) return;
            BinaryPrimitives.WriteInt32LittleEndian(m_Data.AsSpan(index * 4, 4), value);
        }

        private object? GetBoxedValueInternal(int index)
        {
            if (m_Data == null || (uint)index >= (uint)m_Length) return null;
            if (IsRefKind(eArrayType.eType, out var strKind))
            {
                int id = ReadInt32At(index);
                var obj = ObjectManager.GetObjectById(id);
                if (obj == null) return null;
                if (strKind)
                    return obj is StringObject so ? so.value : obj.value?.ToString();
                return obj;
            }

            int o = index * m_UnitLength;
            return eArrayType.eType switch
            {
                EVMType.Boolean => m_Data[o] != 0,
                EVMType.UInt8 => m_Data[o],
                EVMType.Int8 => unchecked((sbyte)m_Data[o]),
                EVMType.Int16 => BinaryPrimitives.ReadInt16LittleEndian(m_Data.AsSpan(o, 2)),
                EVMType.UInt16 => BinaryPrimitives.ReadUInt16LittleEndian(m_Data.AsSpan(o, 2)),
                EVMType.Int32 => BinaryPrimitives.ReadInt32LittleEndian(m_Data.AsSpan(o, 4)),
                EVMType.UInt32 => BinaryPrimitives.ReadUInt32LittleEndian(m_Data.AsSpan(o, 4)),
                EVMType.Int64 => BinaryPrimitives.ReadInt64LittleEndian(m_Data.AsSpan(o, 8)),
                EVMType.UInt64 => BinaryPrimitives.ReadUInt64LittleEndian(m_Data.AsSpan(o, 8)),
                EVMType.Float32 => BinaryPrimitives.ReadSingleLittleEndian(m_Data.AsSpan(o, 4)),
                EVMType.Float64 => BinaryPrimitives.ReadDoubleLittleEndian(m_Data.AsSpan(o, 8)),
                EVMType.Num => BinaryPrimitives.ReadDoubleLittleEndian(m_Data.AsSpan(o, 8)),
                _ => null,
            };
        }

        private SObject? GetSObjectAt(int index)
        {
            if (m_Data == null || (uint)index >= (uint)m_Length) return null;
            if (!IsRefKind(eArrayType.eType, out _)) return null;
            return ObjectManager.GetObjectById(ReadInt32At(index));
        }

        private void SetObjectSlotToNull(int index)
        {
            if (m_Data == null || (uint)index >= (uint)m_Length || !IsRefKind(eArrayType.eType, out _)) return;
            if (eArrayType.eType == EVMType.Object)
            {
                var o = GetSObjectAt(index);
                if (o != null && o.eType == EVMType.Object)
                {
                    o.SetValue(null);
                }
                else
                {
                    var fresh = new SObject(EVMType.Object);
                    ObjectManager.RegisterObject(fresh);
                    WriteInt32At(index, fresh.hashCode);
                }
                return;
            }
            WriteInt32At(index, 0);
        }

        private void StoreFromSValueRaw(int index, RuntimeValue RuntimeValue, EVMType arrayEvm)
        {
            if (m_Data == null || (uint)index >= (uint)m_Length) return;
            if (IsRefKind(arrayEvm, out var strKind))
            {
                if (RuntimeValue.isNull)
                {
                    WriteInt32At(index, 0);
                    return;
                }
                if (strKind)
                {
                    var str = RuntimeValue.stringValue;
                    if (str == null)
                    {
                        WriteInt32At(index, 0);
                    }
                    else
                    {
                        var strObj = new StringObject(str);
                        ObjectManager.RegisterObject(strObj);
                        WriteInt32At(index, strObj.hashCode);
                    }
                }
                else
                {
                    // Object / Class / Array / Type 等引用槽：标量须先装箱（�?RuntimeObject.SetSObjectBySValue 一致）�?
                    var refObj = RuntimeValue.GetReferenceSObject(createStringRef: true);
                    if (refObj != null)
                    {
                        ObjectManager.RegisterObject(refObj);
                        WriteInt32At(index, refObj.hashCode);
                    }
                    else
                        WriteInt32At(index, 0);
                }
                return;
            }

            if (RuntimeValue.isNull)
            {
                WriteNullScalar(index, arrayEvm);
                return;
            }

            WriteNonNullScalarRaw(index, RuntimeValue, arrayEvm);
        }

        private void WriteNullScalar(int index, EVMType t)
        {
            if (m_Data == null) return;
            int o = index * m_UnitLength;
            for (int i = 0; i < m_UnitLength && o + i < m_Data.Length; i++) m_Data[o + i] = 0;
        }

        private void WriteNonNullScalarRaw(int index, RuntimeValue s, EVMType t)
        {
            if (m_Data == null) return;
            int o = index * m_UnitLength;
            var w = m_Data.AsSpan(o);
            switch (t)
            {
                case EVMType.Boolean: w[0] = (byte)(s.int8Value == 1 ? 1 : 0); break;
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
                case EVMType.Num: BinaryPrimitives.WriteDoubleLittleEndian(w, s.float64Value); break;
            }
        }

        private bool TryStoreCoercedNumber(int index, RuntimeValue RuntimeValue, EVMType arrayEvm)
        {
            if (IsRefKind(arrayEvm, out _) || RuntimeValue.isNull) return false;
            if (RuntimeValue.eType == EVMType.Null) { WriteNullScalar(index, arrayEvm); return true; }
            if (!TryGetNumericAsDouble(RuntimeValue, out var d)) return false;
            var tmp = default(RuntimeValue);
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
                case EVMType.Num: tmp.eType = EVMType.Num; tmp.float64Value = d; break;
                case EVMType.Boolean: tmp.eType = EVMType.Boolean; tmp.uint8Value = (byte)(d != 0 ? 1 : 0); break;
                default: return false;
            }
            tmp.isNull = false;
            WriteNonNullScalarRaw(index, tmp, arrayEvm);
            return true;
        }
        internal static bool TryGetNumericAsDouble(RuntimeValue RuntimeValue, out double value)
        {
            value = 0;
            switch (RuntimeValue.eType)
            {
                case EVMType.UInt8:
                    value = RuntimeValue.uint8Value;
                    return true;
                case EVMType.Int8:
                    value = RuntimeValue.int8Value;
                    return true;
                case EVMType.Int16:
                    value = RuntimeValue.int16Value;
                    return true;
                case EVMType.UInt16:
                    value = RuntimeValue.uint16Value;
                    return true;
                case EVMType.Int32:
                    value = RuntimeValue.int32Value;
                    return true;
                case EVMType.UInt32:
                    value = RuntimeValue.uint32Value;
                    return true;
                case EVMType.Int64:
                    value = RuntimeValue.int64Value;
                    return true;
                case EVMType.UInt64:
                    value = RuntimeValue.uint64Value;
                    return true;
                case EVMType.Float32:
                    value = RuntimeValue.float32Value;
                    return true;
                case EVMType.Float64:
                    value = RuntimeValue.float64Value;
                    return true;
                default:
                    return false;
            }
        }
        public override string ToFormatString()
        {
            return $"Array ID: { hashCode } ";
        }
    }
}
