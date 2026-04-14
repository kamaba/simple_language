//****************************************************************************
//  File:      NumObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/01/18 12:00:00
//  Description: Base numeric object for VM numeric types
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using System;

namespace SimpleLanguage.VM
{
    public class NumObject : SObject
    {
        public NumObject() : base(EVMType.Num)
        {
        }
        public NumObject(EVMType etype) : base(etype)
        {
        }
        // Convenient setters; payload lives in SObject.m_Numeric / SetValueByType.
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
            SetValueByType(EVMType.Byte, v);
        }

        public virtual void SetValue(sbyte v)
        {
            SetValueByType(EVMType.SByte, v);
        }


        public virtual double ToDouble()
        {
            if (value == null) return 0.0;
            switch (eType)
            {
                case EVMType.Float64: return (double)value;
                case EVMType.Float32: return Convert.ToDouble((float)value);
                case EVMType.Int64: return Convert.ToDouble((long)value);
                case EVMType.UInt64: return Convert.ToDouble((ulong)value);
                case EVMType.Int32: return Convert.ToDouble((int)value);
                case EVMType.UInt32: return Convert.ToDouble((uint)value);
                case EVMType.Int16: return Convert.ToDouble((short)value);
                case EVMType.UInt16: return Convert.ToDouble((ushort)value);
                case EVMType.Byte: return Convert.ToDouble((byte)value);
                case EVMType.SByte: return Convert.ToDouble((sbyte)value);
                default: return Convert.ToDouble(value);
            }
        }

        public virtual long ToInt64()
        {
            if (value == null) return 0;
            switch (eType)
            {
                case EVMType.Float64: return Convert.ToInt64((double)value);
                case EVMType.Float32: return Convert.ToInt64((float)value);
                case EVMType.Int64: return (long)value;
                case EVMType.UInt64: return Convert.ToInt64((ulong)value);
                case EVMType.Int32: return Convert.ToInt64((int)value);
                case EVMType.UInt32: return Convert.ToInt64((uint)value);
                case EVMType.Int16: return Convert.ToInt64((short)value);
                case EVMType.UInt16: return Convert.ToInt64((ushort)value);
                case EVMType.Byte: return Convert.ToInt64((byte)value);
                case EVMType.SByte: return Convert.ToInt64((sbyte)value);
                default: return Convert.ToInt64(value);
            }
        }

        public override string ToFormatString()
        {
            return value != null ? value.ToString() : "";
        }

        // perform arithmetic operation with another NumObject
        // sign: 0:+ 1:- 2:* 3:/ 4:%
        public virtual void Operate(int sign, NumObject other, bool isUnsign)
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
    }
}
