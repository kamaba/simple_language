//****************************************************************************
//  File:      Int32Object.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.VM.Runtime;
using System;

namespace SimpleLanguage.VM
{
    public class BoolObject : SObject
    {
        public new bool value
        {
            get
            {
                return (bool)m_Value;
            }
        }
        public BoolObject(bool flag) : base(EVMType.Boolean)
        {
            m_Value = flag;
            m_RuntimeType = RuntimeTypeManager.boolRuntimeType;
        }

        public void SetValue(bool _val)
        {
            m_Value = _val;
        }
        public override string ToFormatString()
        {
            return m_Value.ToString();
        }
    }
    public class Int8Object : NumObject
    {
        public new Byte value
        {
            get
            {
                return (byte)m_Value;
            }
        }
        public Int8Object(Byte _val) : base(EVMType.Byte)
        {
            m_Value = _val;
            m_RuntimeType = RuntimeTypeManager.byteRuntimeType;
        }
    }
    public class SInt8Object : NumObject
    {
        public new SByte value
        {
            get
            {
                return (SByte)m_Value;
            }
        }
        public SInt8Object(SByte _val) : base(EVMType.SByte)
        {
            m_Value = _val;
            m_RuntimeType = RuntimeTypeManager.sbyteRuntimeType;
        }
    }
    class Int16Object : NumObject
    {
        public new Int16 value
        {
            get
            {
                return (Int16)m_Value;
            }
        }
        public Int16Object(Int16 val) : base(EVMType.Int16)
        {
            m_Value = val;
            m_RuntimeType = RuntimeTypeManager.int16RuntimeType;
        }
    }
    public class UInt16Object : NumObject
    {
        public new UInt16 value
        {
            get
            {
                return (UInt16)m_Value;
            }
        }
        public UInt16Object(UInt16 val) : base(EVMType.UInt16)
        {
            m_Value = val;
            m_RuntimeType = RuntimeTypeManager.uint16RuntimeType;
        }
    }
    public class Int32Object : NumObject
    {
        public new Int32 value
        {
            get
            {
                return (Int32)m_Value;
            }
        }
        public Int32Object( int obj ) : base(EVMType.Int32)
        {
            m_Value = obj;
            m_RuntimeType = RuntimeTypeManager.int32RuntimeType;
        }
    }
    public class UInt32Object : NumObject
    {
        public new UInt32 value
        {
            get
            {
                return (UInt32)m_Value;
            }
        }
        public UInt32Object(UInt32 obj) : base(EVMType.UInt32)
        {
            m_Value = obj;
            m_Type = EVMType.UInt32;
            m_RuntimeType = RuntimeTypeManager.uint32RuntimeType;
        }
    }


    public class Int64Object : NumObject
    {
        public new Int64 value
        {
            get
            {
                return (Int64)m_Value;
            }
        }
        public Int64Object(Int64 val) : base(EVMType.Int64)
        {
            m_Value = val;
            m_RuntimeType = RuntimeTypeManager.int64RuntimeType;
        }
    }
    public class UInt64Object : NumObject
    {
        public new UInt64 value
        {
            get
            {
                return (UInt64)m_Value;
            }
        }
        public UInt64Object(UInt64 val) : base(EVMType.UInt64)
        {
            m_Value = val;
            m_RuntimeType = RuntimeTypeManager.uint64RuntimeType;
        }
    }
}
