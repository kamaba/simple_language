//****************************************************************************
//  File:      Int32Object.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.VM.Runtime;

namespace SimpleLanguage.VM
{
    public class BoolObject : SObject
    {
        public new bool value => m_Numeric.u8 != 0;
        public BoolObject(bool flag) : base(EVMType.Boolean)
        {
            m_Numeric.u8 = flag ? (byte)1 : (byte)0;
            m_RuntimeType = RuntimeTypeManager.boolRuntimeType;
        }

        public void SetValue(bool _val)
        {
            m_Numeric.u8 = _val ? (byte)1 : (byte)0;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
    public class UInt8Object : NumObject
    {
        public new byte value => m_Numeric.u8;

        public UInt8Object(byte _val) : base(EVMType.UInt8)
        {
            m_Numeric.u8 = _val;
            m_RuntimeType = RuntimeTypeManager.uint8RuntimeType;
        }
    }
    public class Int8Object : NumObject
    {
        public new sbyte value => m_Numeric.i8;

        public Int8Object(sbyte _val) : base(EVMType.Int8)
        {
            m_Numeric.i8 = _val;
            m_RuntimeType = RuntimeTypeManager.int8RuntimeType;
        }
    }
    class Int16Object : NumObject
    {
        public new short value => m_Numeric.i16;

        public Int16Object(short val) : base(EVMType.Int16)
        {
            m_Numeric.i16 = val;
            m_RuntimeType = RuntimeTypeManager.int16RuntimeType;
        }
    }
    public class UInt16Object : NumObject
    {
        public new ushort value => m_Numeric.u16;

        public UInt16Object(ushort val) : base(EVMType.UInt16)
        {
            m_Numeric.u16 = val;
            m_RuntimeType = RuntimeTypeManager.uint16RuntimeType;
        }
    }
    public class Int32Object : NumObject
    {
        public new int value => m_Numeric.i32;

        public Int32Object(int obj) : base(EVMType.Int32)
        {
            m_Numeric.i32 = obj;
            m_RuntimeType = RuntimeTypeManager.int32RuntimeType;
        }
    }
    public class UInt32Object : NumObject
    {
        public new uint value => m_Numeric.u32;

        public UInt32Object(uint obj) : base(EVMType.UInt32)
        {
            m_Numeric.u32 = obj;
            m_Header.EType = (byte)EVMType.UInt32;
            m_RuntimeType = RuntimeTypeManager.uint32RuntimeType;
        }
    }


    public class Int64Object : NumObject
    {
        public new long value => m_Numeric.i64;

        public Int64Object(long val) : base(EVMType.Int64)
        {
            m_Numeric.i64 = val;
            m_RuntimeType = RuntimeTypeManager.int64RuntimeType;
        }
    }
    public class UInt64Object : NumObject
    {
        public new ulong value => m_Numeric.u64;

        public UInt64Object(ulong val) : base(EVMType.UInt64)
        {
            m_Numeric.u64 = val;
            m_RuntimeType = RuntimeTypeManager.uint64RuntimeType;
        }
    }
}
