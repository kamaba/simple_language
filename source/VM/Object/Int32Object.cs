//****************************************************************************
//  File:      Int32Object.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Core;

namespace SimpleLanguage.VM
{
    public class Int32Object : SObject
    {
        public int value = 0;
        Int32Object(MetaClass mc)
        { }
        public Int32Object( int obj )
        {
            value = obj;
            m_Etype = EType.Int32;
        }
        public void SetValue(Int32 _val)
        {
            value = (int)_val;
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
        public String Int32ToString()
        {
            return value.ToString();
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
    class UInt32Object : SObject
    {
        public UInt32 value;
        public UInt32Object()
        { }
        public UInt32Object(object obj)
        {
            value = (UInt32)obj;
            m_Etype = EType.UInt32;
        }
        public void SetValue(UInt32 _val)
        {
            value = _val;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
}
