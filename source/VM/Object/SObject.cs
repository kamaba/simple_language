//****************************************************************************
//  File:      SObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description:
//****************************************************************************

using SimpleLanguage.Logging;
using SimpleLanguage.VM.Runtime;
using System.Text;

namespace SimpleLanguage.VM
{
    public class SObject
    {
        public int hashCode => (m_Header.Hash);
        public EVMType eType => (EVMType)m_Header.EType;
        public bool isNumeric => eType >= EVMType.UInt8 && eType <= EVMType.Num;
        public virtual object? value => GetBoxedValue();
        public RuntimeClass runtimeClass => m_RuntimeType?.runtimeClass;
        public RuntimeType runtimeType { get => m_RuntimeType; set => m_RuntimeType = value; }

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

        /// <summary>GC generation: 0=young (nursery), 1=old. Stored in header.</summary>
        public byte gcGeneration
        {
            get => m_Header.GcGeneration;
            set => m_Header.GcGeneration = value;
        }

        protected VMObjectHeader m_Header;
        protected RuntimeType? m_RuntimeType = null;
        /// <summary>标量位型数据（布尔用 <see cref="NumericUnion.i8"/> 0/1，与 <see cref="RuntimeValue"/> 一致）</summary>
        public NumericUnion m_Numeric;

        protected RuntimeObject[] m_MemberRuntimeObjectArray = null;
        protected byte[] m_MemberData = null;

        /// <summary>实例字段接收部分（静态字段见 <see cref="RuntimeType.memberData"/>）。</summary>
        public byte[]? memberData => m_MemberData;

        protected static int idCount = 10000;
        protected SObject()
        {
            m_Numeric = default;
            m_RuntimeType = RuntimeTypeManager.objectRuntimeType;
            m_Header = VMObjectHeader.Make((byte)EVMType.Class, VMObjectHeader.MetaKindRegular, 0);
            m_Header.Hash = (int)++idCount;
        }
        public SObject(EVMType etype)
        {
            m_Numeric = default;
            m_RuntimeType = RuntimeTypeManager.GetRuntimeTypeByEVMType(etype);
            m_Header = VMObjectHeader.Make((byte)etype, VMObjectHeader.MetaKindRegular, 0);
            m_Header.Hash = (int)++idCount;
        }

        public SObject(RuntimeType irmt)
        {
            m_Numeric = default;
            m_Header = VMObjectHeader.Make((byte)EVMType.Class, VMObjectHeader.MetaKindRegular, 0);
            m_Header.Hash = (int)++idCount;
            m_RuntimeType = irmt;

            var metaVariableList = m_RuntimeType.runtimeClass.nonStaticIRMetaVariableList;
            m_MemberRuntimeObjectArray = new RuntimeObject[metaVariableList.Count];
            for (int i = 0; i < m_MemberRuntimeObjectArray.Length; i++)
            {
                var rt = RuntimeVM.GetRuntimeTypeByDefType(metaVariableList[i].runtimeDefType, m_RuntimeType.runtimeClass, irmt.runtimeTemplateList, true);
                m_MemberRuntimeObjectArray[i] = new RuntimeObject(rt, metaVariableList[i], null);
            }
            BuildMemberDataLayout();
        }

        public virtual void CreateObject() { }

        /// <summary>实例成员的 IR 都是非静态字段顺序一致，使用或者 <see cref="RuntimeClass.nonStaticIRMetaVariableList"/> 相同下标。</summary>
        public RuntimeObject? GetMemberRuntimeObject(int memberIndex)
        {
            if (m_MemberRuntimeObjectArray == null) return null;
            if (memberIndex < 0 || memberIndex >= m_MemberRuntimeObjectArray.Length)
                return null;
            return m_MemberRuntimeObjectArray[memberIndex];
        }
        /// <summary>按成员下标从 <see cref="memberData"/> 读取到 <paramref name="RuntimeValue"/>，类型不匹配位为数字指针 Id，否则 RuntimeObject 处理。</summary>
        public bool TryReadMemberDataAsSValue(int memberIndex, ref RuntimeValue RuntimeValue)
        {
            if( m_MemberRuntimeObjectArray == null )return false;

            if (memberIndex < 0 || memberIndex >= m_MemberRuntimeObjectArray.Length)
                return false;
            return m_MemberRuntimeObjectArray[memberIndex].TryReadMemberDataToSValue(ref RuntimeValue);
        }
        public void BuildMemberDataLayout()
        {
            if (m_MemberRuntimeObjectArray == null || m_MemberRuntimeObjectArray.Length == 0)
            {
                m_MemberData = null;
                return;
            }

            int n = m_MemberRuntimeObjectArray.Length;
            int totalBytes = 0;
            for (int i = 0; i < n; i++)
            {
                totalBytes += MemberDataLayout.GetSlotByteLength(m_MemberRuntimeObjectArray[i].runtimeType);
            }

            m_MemberData = totalBytes > 0 ? new byte[totalBytes] : null;
            int offset = 0;
            for (int i = 0; i < n; i++)
            {
                var ro = m_MemberRuntimeObjectArray[i];
                int len = MemberDataLayout.GetSlotByteLength(ro.runtimeType);
                ro.AttachMemberDataSlice(m_MemberData, offset, len, i);
                offset += len;
            }
        }
        public virtual void SetSValue(SObject val)
        {
            val.refCount++;
        }
        /// <summary>从实例成员获取到 <paramref name="RuntimeValue"/>，与 <see cref="m_MemberData"/> 一致，同 <see cref="RuntimeType.GetStaticMemberVariableSValue"/> 静态类）。</summary>
        public void GetMemberVariableSValue(int index, ref RuntimeValue RuntimeValue)
        {
            if (index < 0)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "执行的参数超出范围!! < 0 ");
                return;
            }
            if (m_MemberRuntimeObjectArray == null || index >= m_MemberRuntimeObjectArray.Length)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "执行的参数超出范围!!");
                return;
            }
            m_MemberRuntimeObjectArray[index].SetSValueByRuntimeObjct(ref RuntimeValue);
        }
        /// <summary>实例成员写统一入口，同步到 <see cref="m_MemberData"/>，同 <see cref="RuntimeType.SetStaticMemberVariableSValue"/> 静态类）。</summary>
        public void SetMemberVariableSValue(int index, RuntimeValue RuntimeValue)
        {
            if (m_MemberRuntimeObjectArray == null || index < 0 || index >= m_MemberRuntimeObjectArray.Length)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "执行的参数超出范围!!");
                return;
            }

            int targetIndex = ResolveCompatibleMemberIndex(index, ref RuntimeValue);
            m_MemberRuntimeObjectArray[targetIndex].SetSObjectBySValue(ref RuntimeValue);
        }

        private int ResolveCompatibleMemberIndex(int preferIndex, ref RuntimeValue RuntimeValue)
        {
            if (m_RuntimeType?.runtimeClass?.metaClassKind != 2)
                return preferIndex;

            if (preferIndex < 0 || preferIndex >= m_MemberRuntimeObjectArray.Length)
                return preferIndex;

            var preferRuntimeType = m_MemberRuntimeObjectArray[preferIndex]?.runtimeType;
            if (IsValueCompatibleWithRuntimeType(ref RuntimeValue, preferRuntimeType))
                return preferIndex;

            for (int i = 0; i < m_MemberRuntimeObjectArray.Length; i++)
            {
                if (i == preferIndex)
                    continue;

                var candidateType = m_MemberRuntimeObjectArray[i]?.runtimeType;
                if (IsValueCompatibleWithRuntimeType(ref RuntimeValue, candidateType))
                    return i;
            }

            return preferIndex;
        }

        private static bool IsValueCompatibleWithRuntimeType(ref RuntimeValue RuntimeValue, RuntimeType? expectedType)
        {
            if (expectedType == null)
                return false;

            if (RuntimeValue.isNull)
                return true;

            if (expectedType.runtimeClass?.metaClassKind == 2)
                return RuntimeValue.sobject != null && RuntimeValue.sobject.eType == EVMType.Class;

            if (expectedType.eType == EVMType.Array)
                return RuntimeValue.sobject is ArrayObject || RuntimeValue.eType == EVMType.Array;

            if (expectedType.eType == EVMType.String)
                return RuntimeValue.eType == EVMType.String || RuntimeValue.sobject is StringObject;

            return true;
        }

        /// <summary>
        /// Finds the member index by field name in the non-static member list.
        /// </summary>
        protected static int FindMemberIndex(RuntimeClass? rc, string target)
        {
            if (rc == null || string.IsNullOrEmpty(target)) return -1;
            var list = rc.nonStaticIRMetaVariableList;
            for (int i = 0; i < list.Count; i++)
            {
                var rv = list[i];
                if (rv == null) continue;
                var n = rv.name ?? string.Empty;
                if (string.Equals(n, target, StringComparison.Ordinal)
                    || n.EndsWith(target, StringComparison.Ordinal)
                    || n.Contains(target, StringComparison.Ordinal))
                    return rv.index >= 0 ? rv.index : i;
            }
            return -1;
        }

        protected virtual object? GetBoxedValue()
        {
            switch ((EVMType)m_Header.EType)
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

        /// <summary>写入类型与负载（不修改 <see cref="refCount"/>）。</summary>
        public void StoreValue(EVMType vmType, object? val)
        {
            m_Header.EType = (byte)vmType;
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

        // Convenient setters; payload lives in m_Numeric / SetValueByType.
        public virtual void SetValue(double v)
        {
            SetValueByType(EVMType.Float64, v);
        }

        public virtual void SetValue(float v)
        {
            SetValueByType(EVMType.Float32, v);
        }

        public virtual void SetValue(long v)
        {
            SetValueByType(EVMType.Int64, v);
        }

        public virtual void SetValue(ulong v)
        {
            SetValueByType(EVMType.UInt64, v);
        }

        public virtual void SetValue(int v)
        {
            SetValueByType(EVMType.Int32, v);
        }

        public virtual void SetValue(uint v)
        {
            SetValueByType(EVMType.UInt32, v);
        }

        public virtual void SetValue(short v)
        {
            SetValueByType(EVMType.Int16, v);
        }

        public virtual void SetValue(ushort v)
        {
            SetValueByType(EVMType.UInt16, v);
        }
        public virtual void SetValue(byte v)
        {
            SetValueByType(EVMType.UInt8, v);
        }

        public virtual void SetValue(sbyte v)
        {
            SetValueByType(EVMType.Int8, v);
        }

        public virtual double ToDouble()
        {
            switch ((EVMType)m_Header.EType)
            {
                case EVMType.Float64:
                case EVMType.Num:
                    return m_Numeric.f64;
                case EVMType.Float32:
                    return (double)m_Numeric.f32;
                case EVMType.Int64:
                    return (double)m_Numeric.i64;
                case EVMType.UInt64:
                    return (double)m_Numeric.u64;
                case EVMType.Int32:
                    return (double)m_Numeric.i32;
                case EVMType.UInt32:
                    return (double)m_Numeric.u32;
                case EVMType.Int16:
                    return (double)m_Numeric.i16;
                case EVMType.UInt16:
                    return (double)m_Numeric.u16;
                case EVMType.UInt8:
                    return (double)m_Numeric.u8;
                case EVMType.Int8:
                    return (double)m_Numeric.i8;
                case EVMType.Boolean:
                    return m_Numeric.u8 != 0 ? 1.0 : 0.0;
                default:
                    return 0.0;
            }
        }

        public virtual long ToInt64()
        {
            switch ((EVMType)m_Header.EType)
            {
                case EVMType.Float64:
                case EVMType.Num:
                    return (long)m_Numeric.f64;
                case EVMType.Float32:
                    return (long)m_Numeric.f32;
                case EVMType.Int64:
                    return m_Numeric.i64;
                case EVMType.UInt64:
                    return (long)m_Numeric.u64;
                case EVMType.Int32:
                    return (long)m_Numeric.i32;
                case EVMType.UInt32:
                    return (long)m_Numeric.u32;
                case EVMType.Int16:
                    return (long)m_Numeric.i16;
                case EVMType.UInt16:
                    return (long)m_Numeric.u16;
                case EVMType.UInt8:
                    return (long)m_Numeric.u8;
                case EVMType.Int8:
                    return (long)m_Numeric.i8;
                case EVMType.Boolean:
                    return m_Numeric.u8 != 0 ? 1L : 0L;
                default:
                    return 0;
            }
        }

        // perform arithmetic operation with another SObject
        // sign: 0:+ 1:- 2:* 3:/ 4:%
        public virtual void Operate(int sign, SObject other, bool isUnsign)
        {
            double a = this.ToDouble();
            double b = other != null ? other.ToDouble() : 0.0;
            double r = 0.0;
            switch (sign)
            {
                case 0: r = a + b; break;
                case 1: r = a - b; break;
                case 2: r = a * b; break;
                case 3: r = (b == 0.0) ? 0.0 : a / b; break;
                case 4: r = (b == 0.0) ? 0.0 : a % b; break;
                default:
                    r = a;
                    break;
            }
            SetValue(r);
        }

        public virtual string ToFormatString()
        {
            switch ((EVMType)m_Header.EType)
            {
                case EVMType.Class:
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(m_RuntimeType?.runtimeClass?.ToString() ?? m_RuntimeType?.ToString() ?? "");
                        return sb.ToString();
                    }
                case EVMType.Boolean:
                    return (m_Numeric.u8 != 0).ToString();
                case EVMType.UInt8:
                    return m_Numeric.u8.ToString();
                case EVMType.Int8:
                    return m_Numeric.i8.ToString();
                case EVMType.Int16:
                    return m_Numeric.i16.ToString();
                case EVMType.UInt16:
                    return m_Numeric.u16.ToString();
                case EVMType.Int32:
                    return m_Numeric.i32.ToString();
                case EVMType.UInt32:
                    return m_Numeric.u32.ToString();
                case EVMType.Int64:
                    return m_Numeric.i64.ToString();
                case EVMType.UInt64:
                    return m_Numeric.u64.ToString();
                case EVMType.Float32:
                    return m_Numeric.f32.ToString();
                case EVMType.Float64:
                case EVMType.Num:
                    return m_Numeric.f64.ToString();
                default:
                    return $"ID: {hashCode} value:" + (this != null ? this.ToString() : m_Numeric.ToString());
            }
        }

        public override string ToString()
        {
            if (m_RuntimeType != null)
                return m_RuntimeType.ToString();
            return base.ToString();
        }
    }
}
