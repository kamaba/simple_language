//****************************************************************************
//  File:      SObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.Runtime;

namespace SimpleLanguage.VM
{
    public class SObject
    {
        public int id => m_Id;
        public EVMType eType => (EVMType)m_Header.EType;
        public virtual object? value => GetBoxedValue();
        public RuntimeClass runtimeClass => m_RuntimeType?.runtimeClass;
        public RuntimeType runtimeType => m_RuntimeType;
        public short typeId { get; set; } = 0;

        /// <summary>Reference count, stored in the packed header (14 bits, 0–16383).</summary>
        public int refCount
        {
            get => m_Header.RefCount;
            set => m_Header.RefCount = (ushort)value;
        }

        /// <summary>Meta-kind: 0=regular, 1=enum, 2=data, 3=type_object.</summary>
        public byte metaKind
        {
            get => m_Header.MetaKind;
            set => m_Header.MetaKind = value;
        }

        /// <summary>GC tri-color mark: 0=white, 1=gray, 2=black.</summary>
        public byte gcColor
        {
            get => m_Header.GcColor;
            set => m_Header.GcColor = value;
        }

        /// <summary>Packed 64-bit object header (etype, meta_kind, refcount, hash, gc_color).</summary>
        public VMObjectHeader header => m_Header;

        /// <summary>
        /// Dart-style generation: 0 = nursery (new space), 1 = old space.
        /// Updated by <see cref="T:SimpleLanguage.VM.MemoryManagement.SlMemoryManager"/> during SL GC.
        /// </summary>
        public byte SlMemoryGeneration { get; internal set; }

        protected VMObjectHeader m_Header;
        protected int m_Id = 0;
        protected RuntimeType? m_RuntimeType = null;
        /// <summary>标量位型数据（布尔用 <see cref="NumericUnion.i8"/> 0/1，与 <see cref="RuntimeValue"/> 一致）</summary>
        protected NumericUnion m_Numeric;

        /// <summary>
        /// Object type, stored in the packed header's etype field (6 bits).
        /// Routes through m_Header.EType, matching cvm's header.bits.etype.
        /// </summary>
        protected EVMType m_Type
        {
            get => (EVMType)m_Header.EType;
            set => m_Header.EType = (byte)value;
        }

        protected static int idCount = 10000;
        protected SObject()
        {
            m_Id = ++idCount;
            m_Numeric = default;
            m_RuntimeType = RuntimeTypeManager.objectRuntimeType;
            m_Header = VMObjectHeader.Make((byte)EVMType.Class, VMObjectHeader.MetaKindRegular, 0);
        }
        public SObject(EVMType etype)
        {
            m_Id = ++idCount;
            m_Numeric = default;
            m_RuntimeType = RuntimeTypeManager.GetRuntimeTypeByEVMType(etype);
            m_Header = VMObjectHeader.Make((byte)etype, VMObjectHeader.MetaKindRegular, 0);
        }

        protected virtual object? GetBoxedValue()
        {
            switch (m_Type)
            {
                case EVMType.Boolean:
                    return m_Numeric.u8 != 0;
                case EVMType.UInt8:
                    return m_Numeric.u8;
                case EVMType.Int8:
                    return m_Numeric.i8;
                case EVMType.Int16:
                    return m_Numeric.i16;
                case EVMType.UInt16:
                    return m_Numeric.u16;
                case EVMType.Int32:
                    return m_Numeric.i32;
                case EVMType.UInt32:
                    return m_Numeric.u32;
                case EVMType.Int64:
                    return m_Numeric.i64;
                case EVMType.UInt64:
                    return m_Numeric.u64;
                case EVMType.Float32:
                    return m_Numeric.f32;
                case EVMType.Float64:
                case EVMType.Num:
                    return m_Numeric.f64;
                default:
                    return this;
            }
        }

        /// <summary>写入类型与负载（不修�?<see cref="refCount"/>）�?/summary>
        protected void StoreValue(EVMType vmType, object? val)
        {
            m_Type = vmType;
            m_Numeric = default;
            if (val == null)
                return;

            switch (vmType)
            {
                case EVMType.Boolean:
                    if (val is bool bb)
                        m_Numeric.u8 = bb ? (byte)1 : (byte)0;
                    else if (val is byte b8)
                        m_Numeric.u8 = b8;
                    else
                        m_Numeric.u8 = Convert.ToBoolean(val) ? (byte)1 : (byte)0;
                    break;
                case EVMType.UInt8:
                    m_Numeric.u8 = Convert.ToByte(val);
                    break;
                case EVMType.Int8:
                    m_Numeric.i8 = Convert.ToSByte(val);
                    break;
                case EVMType.Int16:
                    m_Numeric.i16 = Convert.ToInt16(val);
                    break;
                case EVMType.UInt16:
                    m_Numeric.u16 = Convert.ToUInt16(val);
                    break;
                case EVMType.Int32:
                    m_Numeric.i32 = Convert.ToInt32(val);
                    break;
                case EVMType.UInt32:
                    m_Numeric.u32 = Convert.ToUInt32(val);
                    break;
                case EVMType.Int64:
                    m_Numeric.i64 = Convert.ToInt64(val);
                    break;
                case EVMType.UInt64:
                    m_Numeric.u64 = Convert.ToUInt64(val);
                    break;
                case EVMType.Float32:
                    m_Numeric.f32 = Convert.ToSingle(val);
                    break;
                case EVMType.Float64:
                case EVMType.Num:
                    m_Numeric.f64 = Convert.ToDouble(val);
                    break;
                case EVMType.String:
                case EVMType.Member:
                case EVMType.Class:
                case EVMType.Array:
                case EVMType.Object:
                case EVMType.Type:
                default:
                    break;
            }
        }

        public virtual void SetValue(object? val)
        {
            if (val == null)
            {
                m_Numeric = default;
                return;
            }
            switch (val)
            {
                case bool b:
                    StoreValue(EVMType.Boolean, b);
                    return;
                case byte b8:
                    StoreValue(EVMType.UInt8, b8);
                    return;
                case sbyte sb:
                    StoreValue(EVMType.Int8, sb);
                    return;
                case short s16:
                    StoreValue(EVMType.Int16, s16);
                    return;
                case ushort u16:
                    StoreValue(EVMType.UInt16, u16);
                    return;
                case int i32:
                    StoreValue(EVMType.Int32, i32);
                    return;
                case uint u32:
                    StoreValue(EVMType.UInt32, u32);
                    return;
                case long i64:
                    StoreValue(EVMType.Int64, i64);
                    return;
                case ulong u64:
                    StoreValue(EVMType.UInt64, u64);
                    return;
                case float f:
                    StoreValue(EVMType.Float32, f);
                    return;
                case double d:
                    StoreValue(EVMType.Float64, d);
                    return;
                case string:
                    StoreValue(EVMType.String, val);
                    return;
                default:
                    break;
            }
        }
        public void SetValueByType(EVMType vmType, object? val)
        {
            StoreValue(vmType, val);
            refCount++;
        }

        public virtual string ToFormatString()
        {
            return $"ID: {m_Id} value:" + this != null ? this.ToString() : m_Numeric.ToString();
        }
    }
}
