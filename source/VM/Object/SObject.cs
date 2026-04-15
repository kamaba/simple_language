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
        public EVMType eType => m_Type;
        public virtual object? value => GetBoxedValue();
        public RuntimeClass runtimeClass => m_RuntimeType?.runtimeClass;
        public RuntimeType runtimeType => m_RuntimeType;
        public short typeId { get; set; } = 0;
        public int refCount { get; set; } = 0;
        /// <summary>
        /// Dart-style generation: 0 = nursery (new space), 1 = old space.
        /// Updated by <see cref="T:SimpleLanguage.VM.MemoryManagement.SlMemoryManager"/> during SL GC.
        /// </summary>
        public byte SlMemoryGeneration { get; internal set; }


        protected int m_Id = 0;
        protected EVMType m_Type = EVMType.Class;
        protected RuntimeType? m_RuntimeType = null;
        /// <summary>标量位型数据（布尔用 <see cref="NumericUnion.i8"/> 0/1，与 <see cref="SValue"/> 一致）。</summary>
        protected NumericUnion m_Numeric;
        /// <summary>引用型负载：字符串、类实例、MethodHandle 等。</summary>
        protected object? m_Reference;

        protected static int idCount = 10000;
        protected SObject()
        {
            m_Id = ++idCount;
            m_Numeric = default;
            m_Reference = this;
            m_RuntimeType = RuntimeTypeManager.objectRuntimeType;
        }
        public SObject(EVMType etype)
        {
            m_Id = ++idCount;
            m_Type = etype;
            m_Numeric = default;
            m_Reference = null;
            m_RuntimeType = RuntimeTypeManager.GetRuntimeTypeByEVMType(etype);
        }

        protected virtual object? GetBoxedValue()
        {
            switch (m_Type)
            {
                case EVMType.Boolean:
                    return m_Numeric.i8 != 0;
                case EVMType.UInt8:
                    return m_Numeric.i8;
                case EVMType.Int8:
                    return m_Numeric.si8;
                case EVMType.Int16:
                    return m_Numeric.i16;
                case EVMType.UInt16:
                    return m_Numeric.ui16;
                case EVMType.Int32:
                    return m_Numeric.i32;
                case EVMType.UInt32:
                    return m_Numeric.u32;
                case EVMType.Int64:
                    return m_Numeric.i64;
                case EVMType.UInt64:
                    return m_Numeric.u64;
                case EVMType.Float32:
                    return m_Numeric.f;
                case EVMType.Float64:
                case EVMType.Num:
                    return m_Numeric.d;
                default:
                    return m_Reference;
            }
        }

        /// <summary>写入类型与负载（不修改 <see cref="refCount"/>）。</summary>
        protected void StoreValue(EVMType vmType, object? val)
        {
            m_Type = vmType;
            m_Numeric = default;
            m_Reference = null;
            if (val == null)
                return;

            switch (vmType)
            {
                case EVMType.Boolean:
                    if (val is bool bb)
                        m_Numeric.i8 = bb ? (byte)1 : (byte)0;
                    else if (val is byte b8)
                        m_Numeric.i8 = b8;
                    else
                        m_Numeric.i8 = Convert.ToBoolean(val) ? (byte)1 : (byte)0;
                    break;
                case EVMType.UInt8:
                    m_Numeric.i8 = Convert.ToByte(val);
                    break;
                case EVMType.Int8:
                    m_Numeric.si8 = Convert.ToSByte(val);
                    break;
                case EVMType.Int16:
                    m_Numeric.i16 = Convert.ToInt16(val);
                    break;
                case EVMType.UInt16:
                    m_Numeric.ui16 = Convert.ToUInt16(val);
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
                    m_Numeric.f = Convert.ToSingle(val);
                    break;
                case EVMType.Float64:
                case EVMType.Num:
                    m_Numeric.d = Convert.ToDouble(val);
                    break;
                case EVMType.String:
                case EVMType.Member:
                case EVMType.Class:
                case EVMType.Array:
                case EVMType.Object:
                case EVMType.Type:
                default:
                    m_Reference = val;
                    break;
            }
        }

        public virtual void SetValue(object? val)
        {
            if (val == null)
            {
                m_Numeric = default;
                m_Reference = null;
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
                    m_Reference = val;
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
            return $"ID: {m_Id} value:" + m_Reference != null ? m_Reference.ToString() : m_Numeric.ToString();
        }
    }
}
