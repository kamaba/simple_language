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
        public override object getValue() { return value; }
        public new bool value { get; protected set; } = false;

        public BoolObject(bool flag) : base(EVMType.Boolean)
        {
            value = flag;
        }

        public void SetValue(bool _val)
        {
            value = _val;
            m_IsNull = false;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
    public class Int8Object : NumObject
    {
        public override object getValue() { return value; }
        public  new Byte value;
        public Int8Object(Byte _val) : base(EVMType.Byte)
        {
            value = _val;
        }
        public void SetValue(Byte _val)
        {
            value = _val;
            m_IsNull = false;
        }
        // Conversions
        public SByte ToSByte() { return (sbyte)value; }
        public short ToInt16() { return Convert.ToInt16(value); }
        public ushort ToUInt16() { return Convert.ToUInt16(value); }
        public int ToInt32() { return Convert.ToInt32(value); }
        public uint ToUInt32() { return Convert.ToUInt32(value); }
        public long ToInt64() { return Convert.ToInt64(value); }
        public ulong ToUInt64() { return Convert.ToUInt64(value); }
        public float ToFloat() { return Convert.ToSingle(value); }
        public double ToDouble() { return Convert.ToDouble(value); }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
    public class SInt8Object : NumObject
    {
        public override object getValue() { return value; }
        public  new SByte value;

        public SInt8Object(SByte _val) : base(EVMType.SByte)
        {
            value = _val;
        }
        public void SetValue(SByte _val)
        {
            value = _val;
            m_IsNull = false;
        }
        // Conversions
        public byte ToByte() { return Convert.ToByte(value); }
        public short ToInt16() { return Convert.ToInt16(value); }
        public ushort ToUInt16() { return Convert.ToUInt16(value); }
        public int ToInt32() { return Convert.ToInt32(value); }
        public uint ToUInt32() { return Convert.ToUInt32(value); }
        public long ToInt64() { return Convert.ToInt64(value); }
        public ulong ToUInt64() { return Convert.ToUInt64(value); }
        public float ToFloat() { return Convert.ToSingle(value); }
        public double ToDouble() { return Convert.ToDouble(value); }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
    class Int16Object : NumObject
    {
        public override object getValue() { return value; }
        public new Int16 value;
        public Int16Object(Int16 val) : base(EVMType.Int16)
        {
            value = val;
        }
        public void SetValue(Int16 _val)
        {
            value = _val;
            m_IsNull = false;
        }
        // Conversions
        public byte ToByte() { return Convert.ToByte(value); }
        public sbyte ToSByte() { return Convert.ToSByte(value); }
        public ushort ToUInt16() { return Convert.ToUInt16(value); }
        public int ToInt32() { return Convert.ToInt32(value); }
        public uint ToUInt32() { return Convert.ToUInt32(value); }
        public long ToInt64() { return Convert.ToInt64(value); }
        public ulong ToUInt64() { return Convert.ToUInt64(value); }
        public float ToFloat() { return Convert.ToSingle(value); }
        public double ToDouble() { return Convert.ToDouble(value); }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
    public class UInt16Object : NumObject
    {
        public override object getValue() { return value; }
        public new UInt16 value;
        public UInt16Object(UInt16 val) : base(EVMType.UInt16)
        {
            value = val;
        }
        public void SetValue(UInt16 _val)
        {
            value = _val;
            m_IsNull = false;
        }
        // Conversions
        public byte ToByte() { return Convert.ToByte(value); }
        public sbyte ToSByte() { return Convert.ToSByte(value); }
        public short ToInt16() { return Convert.ToInt16(value); }
        public int ToInt32() { return Convert.ToInt32(value); }
        public uint ToUInt32() { return Convert.ToUInt32(value); }
        public long ToInt64() { return Convert.ToInt64(value); }
        public ulong ToUInt64() { return Convert.ToUInt64(value); }
        public float ToFloat() { return Convert.ToSingle(value); }
        public double ToDouble() { return Convert.ToDouble(value); }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
    public class Int32Object : NumObject
    {
        public override object getValue() { return value; }
        public new int value = 0;
        public Int32Object( int obj ) : base(EVMType.Int32)
        {
            value = obj;
            m_RuntimeType = RuntimeTypeManager.int32RuntimeType;
        }
        public void SetValue(Int32 _val)
        {
            value = (int)_val;
            m_IsNull = false;
        }
        public void AddInt32( Int32Object int32Obj )
        {
            value = value + (int)int32Obj.value;
        }
        public void MinusInt32( Int32Object int32Obj )
        {
            value = (int)value - (int)int32Obj.value;
        }
        public void MultiplyInt32( Int32Object int32Obj )
        {
            value = (int)value * (int)int32Obj.value;
        }
        public void Divide(Int32Object int32Obj)
        {
            value = (int)value / (int)int32Obj.value;
        }
        public void BeDivide( Int32Object int32Obj )
        {
            value = (int)int32Obj.value / (int)value;
        }
        public void Modulo(Int32Object int32Obj)
        {
            value = (int)value % (int)int32Obj.value;
        }
        public Byte ToByte()
        {
            return Convert.ToByte(value);
        }
        public SByte ToSByte()
        {
            return Convert.ToSByte(value);
        }
        public Char ToChar()
        {
            return Convert.ToChar(value);
        }
        public short ToShort()
        {
            return Convert.ToInt16(value);
        }
        public short ToInt16()
        {
            return Convert.ToInt16(value);
        }
        public Int32 ToInt()
        {
            return value;
        }
        public Int32 ToInt32()
        {
            return value;
        }
        public float ToFloat()
        {
            return Convert.ToSingle(value);
        }
        public double ToDouble()
        {
            return Convert.ToDouble(value);
        }
        public uint ToUInt32() { return Convert.ToUInt32(value); }
        public long ToInt64() { return Convert.ToInt64(value); }
        public ulong ToUInt64() { return Convert.ToUInt64(value); }
        public String Int32ToString()
        {
            return value.ToString();
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
    public class UInt32Object : NumObject
    {
        public override object getValue() { return value; }
        public new UInt32 value;
        public UInt32Object(UInt32 obj) : base(EVMType.UInt32)
        {
            value = obj;
            m_Type = EVMType.UInt32;
        }
        public void SetValue(UInt32 _val)
        {
            value = _val;
            m_IsNull = false;
        }
        // Conversions
        public byte ToByte() { return Convert.ToByte(value); }
        public sbyte ToSByte() { return Convert.ToSByte(value); }
        public short ToInt16() { return Convert.ToInt16(value); }
        public ushort ToUInt16() { return Convert.ToUInt16(value); }
        public int ToInt32() { return Convert.ToInt32(value); }
        public long ToInt64() { return Convert.ToInt64(value); }
        public ulong ToUInt64() { return Convert.ToUInt64(value); }
        public float ToFloat() { return Convert.ToSingle(value); }
        public double ToDouble() { return Convert.ToDouble(value); }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }


    public class Int64Object : NumObject
    {
        public override object getValue() { return value; }
        public new Int64 value;
        public Int64Object(Int64 val) : base(EVMType.Int64)
        {
            value = val;
        }
        public void SetValue(Int64 _val)
        {
            value = _val;
            m_IsNull = false;
        }
        // Conversions
        public byte ToByte() { return Convert.ToByte(value); }
        public sbyte ToSByte() { return Convert.ToSByte(value); }
        public short ToInt16() { return Convert.ToInt16(value); }
        public ushort ToUInt16() { return Convert.ToUInt16(value); }
        public int ToInt32() { return Convert.ToInt32(value); }
        public uint ToUInt32() { return Convert.ToUInt32(value); }
        public ulong ToUInt64() { return Convert.ToUInt64(value); }
        public float ToFloat() { return Convert.ToSingle(value); }
        public double ToDouble() { return Convert.ToDouble(value); }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
    public class UInt64Object : NumObject
    {
        public override object getValue() { return value; }
        public new UInt64 value;
        public UInt64Object(UInt64 val) : base(EVMType.UInt64)
        {
            value = val;
        }
        public void SetValue(UInt64 _val)
        {
            value = _val;
            m_IsNull = false;
        }
        // Conversions
        public byte ToByte() { return Convert.ToByte(value); }
        public sbyte ToSByte() { return Convert.ToSByte(value); }
        public short ToInt16() { return Convert.ToInt16(value); }
        public ushort ToUInt16() { return Convert.ToUInt16(value); }
        public int ToInt32() { return Convert.ToInt32(value); }
        public uint ToUInt32() { return Convert.ToUInt32(value); }
        public long ToInt64() { return Convert.ToInt64(value); }
        public float ToFloat() { return Convert.ToSingle(value); }
        public double ToDouble() { return Convert.ToDouble(value); }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
}
