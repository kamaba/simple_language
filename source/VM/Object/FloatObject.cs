//****************************************************************************
//  File:      Float32Object.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using System;
namespace SimpleLanguage.VM
{
    public class Float32Object : NumObject
    {
        public override object getValue() { return value; }
        public Single value;
        public Float32Object( Single _val ) : base(EVMType.Float32)
        {
            value = _val;
        }
        public void SetValue(Single _val)
        {
            value = _val;
            m_IsNull = false;
        }
        public Int32 ToInt()
        {
            return Int32.Parse( value.ToString() );
        }
        // Conversions
        public byte ToByte() { return Convert.ToByte(value); }
        public sbyte ToSByte() { return Convert.ToSByte(value); }
        public short ToInt16() { return Convert.ToInt16(value); }
        public ushort ToUInt16() { return Convert.ToUInt16(value); }
        public int ToInt32() { return Convert.ToInt32(value); }
        public uint ToUInt32() { return Convert.ToUInt32(value); }
        public long ToInt64() { return Convert.ToInt64(value); }
        public ulong ToUInt64() { return Convert.ToUInt64(value); }
        public double ToDouble() { return Convert.ToDouble(value); }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
    public class Float64Object : NumObject
    {
        public override object getValue() { return value; }
        public Double value;
        public Float64Object(Double _val) : base(EVMType.Float64)
        {
            value = _val;
        }
        public void SetValue(Double _val)
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
        public ulong ToUInt64() { return Convert.ToUInt64(value); }
        public float ToFloat32() { return Convert.ToSingle(value); }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
}
