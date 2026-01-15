//****************************************************************************
//  File:      SValue.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description:  compute left and right value's method example: +-*/%&|^>><<
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleLanguage.VM
{
    public partial struct SValue
    {
        // sign 0:+ 1:- 2:* 3:/ 4:% 5:& 6:| 7:^  8:<< 9:>>
        public void ComputeSVAlue( int sign, ref SValue svalue, bool isUnSign )
        {
            switch (this.eType)
            {
                case EVMType.Int64:
                    {
                        long svalLong = svalue.int64Value;
                        if (sign == 0)
                            int64Value += svalLong;
                        else if (sign == 1)
                            int64Value -= svalLong;
                        else if (sign == 2)
                            int64Value *= svalLong;
                        else if (sign == 3)
                            int64Value /= svalLong;
                        else if (sign == 4)
                            int64Value %= svalLong;
                        else if (sign == 5)
                            int64Value &= svalLong;
                        else if (sign == 6)
                            int64Value |= svalLong;
                        else if (sign == 7)
                            int64Value ^= svalLong;
                        else if (sign == 8)
                            int64Value <<= (int)svalLong;
                        else if (sign == 9)
                            int64Value >>= (int)svalLong;
                    }
                    break;
                case EVMType.UInt64:
                    {
                        ulong svalULong = svalue.eType == EVMType.UInt64
                            ? svalue.uint64Value
                            : (ulong)svalue.int64Value;
                        if (sign == 0)
                            uint64Value += svalULong;
                        else if (sign == 1)
                            uint64Value -= svalULong;
                        else if (sign == 2)
                            uint64Value *= svalULong;
                        else if (sign == 3)
                            uint64Value /= svalULong;
                        else if (sign == 4)
                            uint64Value %= svalULong;
                        else if (sign == 5)
                            uint64Value &= svalULong;
                        else if (sign == 6)
                            uint64Value |= svalULong;
                        else if (sign == 7)
                            uint64Value ^= svalULong;
                        else if (sign == 8)
                            uint64Value <<= (int)svalULong;
                        else if (sign == 9)
                            uint64Value >>= (int)svalULong;
                    }
                    break;
                case EVMType.Int32:
                    {
                        int svalInt = svalue.int32Value;
                        if (sign == 0)
                            int32Value += svalInt;
                        else if (sign == 1)
                            int32Value -= svalInt;
                        else if (sign == 2)
                            int32Value *= svalInt;
                        else if (sign == 3)
                            int32Value /= svalInt;
                        else if (sign == 4)
                            int32Value %= svalInt;
                        else if (sign == 5)
                            int32Value &= svalInt;
                        else if (sign == 6)
                            int32Value |= svalInt;
                        else if (sign == 7)
                            int32Value ^= svalInt;
                        else if (sign == 8)
                            int32Value <<= svalInt;
                        else if (sign == 9)
                            int32Value >>= svalInt;
                    }
                    break;
                case EVMType.UInt32:
                    {
                        uint svalUInt = svalue.uint32Value;
                        if (sign == 0)
                            uint32Value += svalUInt;
                        else if (sign == 1)
                            uint32Value -= svalUInt;
                        else if (sign == 2)
                            uint32Value *= svalUInt;
                        else if (sign == 3)
                            uint32Value /= svalUInt;
                        else if (sign == 4)
                            uint32Value %= svalUInt;
                        else if (sign == 5)
                            uint32Value &= svalUInt;
                        else if (sign == 6)
                            uint32Value |= svalUInt;
                        else if (sign == 7)
                            uint32Value ^= svalUInt;
                        else if (sign == 8)
                            uint32Value <<= (int)svalUInt;
                        else if (sign == 9)
                            uint32Value >>= (int)svalUInt;
                    }
                    break;
                case EVMType.Int16:
                    {
                        short svalShort = svalue.int16Value;
                        if (sign == 0)
                            int16Value += svalShort;
                        else if (sign == 1)
                            int16Value -= svalShort;
                        else if (sign == 2)
                            int16Value *= svalShort;
                        else if (sign == 3)
                            int16Value /= svalShort;
                        else if (sign == 4)
                            int16Value %= svalShort;
                        else if (sign == 5)
                            int16Value &= svalShort;
                        else if (sign == 6)
                            int16Value |= svalShort;
                        else if (sign == 7)
                            int16Value = (short)(int16Value ^ svalShort);
                        else if (sign == 8)
                            int16Value = (short)(int16Value << svalShort);
                        else if (sign == 9)
                            int16Value = (short)(int16Value >> svalShort);
                    }
                    break;
                case EVMType.UInt16:
                    {
                        ushort svalUShort = svalue.uint16Value;
                        if (sign == 0)
                            uint16Value += svalUShort;
                        else if (sign == 1)
                            uint16Value -= svalUShort;
                        else if (sign == 2)
                            uint16Value *= svalUShort;
                        else if (sign == 3)
                            uint16Value /= svalUShort;
                        else if (sign == 4)
                            uint16Value %= svalUShort;
                        else if (sign == 5)
                            uint16Value &= svalUShort;
                        else if (sign == 6)
                            uint16Value |= svalUShort;
                        else if (sign == 7)
                            uint16Value = (ushort)(uint16Value ^ svalUShort);
                        else if (sign == 8)
                            uint16Value = (ushort)(uint16Value << svalUShort);
                        else if (sign == 9)
                            uint16Value = (ushort)(uint16Value >> svalUShort);
                    }
                    break;
                case EVMType.Byte:
                    {
                        byte svalByte = svalue.int8Value;
                        if (sign == 0)
                            int8Value += svalByte;
                        else if (sign == 1)
                            int8Value -= svalByte;
                        else if (sign == 2)
                            int8Value *= svalByte;
                        else if (sign == 3)
                            int8Value /= svalByte;
                        else if (sign == 4)
                            int8Value %= svalByte;
                        else if (sign == 5)
                            int8Value &= svalByte;
                        else if (sign == 6)
                            int8Value |= svalByte;
                        else if (sign == 7)
                            int8Value = (byte)(int8Value ^ svalByte);
                        else if (sign == 8)
                            int8Value = (byte)(int8Value << svalByte);
                        else if (sign == 9)
                            int8Value = (byte)(int8Value >> svalByte);
                    }
                    break;
                case EVMType.SByte:
                    {
                        sbyte svalSbyte = svalue.sint8Value;
                        if (sign == 0)
                            sint8Value += svalSbyte;
                        else if (sign == 1)
                            sint8Value -= svalSbyte;
                        else if (sign == 2)
                            sint8Value *= svalSbyte;
                        else if (sign == 3)
                            sint8Value /= svalSbyte;
                        else if (sign == 4)
                            sint8Value %= svalSbyte;
                        else if (sign == 5)
                            sint8Value &= svalSbyte;
                        else if (sign == 6)
                            sint8Value |= svalSbyte;
                        else if (sign == 7)
                            sint8Value = (sbyte)(sint8Value ^ svalSbyte);
                        else if (sign == 8)
                            sint8Value = (sbyte)(sint8Value << svalSbyte);
                        else if (sign == 9)
                            sint8Value = (sbyte)(sint8Value >> svalSbyte);
                    }
                    break;
                case EVMType.Float32:
                    {
                        float svalFloat = svalue.floatValue;
                        if (sign == 0)
                            floatValue += svalFloat;
                        else if (sign == 1)
                            floatValue -= svalFloat;
                        else if (sign == 2)
                            floatValue *= svalFloat;
                        else if (sign == 3)
                            floatValue /= svalFloat;
                        else if (sign == 4)
                            floatValue %= svalFloat;
                        else
                        {
                            Debug.Write("Error 不支持Float 的这种类型的操作");
                        }
                    }
                    break;
                case EVMType.Float64:
                    {
                        double svalDouble = svalue.doubleValue;
                        if (sign == 0)
                            doubleValue += svalDouble;
                        else if (sign == 1)
                            doubleValue -= svalDouble;
                        else if (sign == 2)
                            doubleValue *= svalDouble;
                        else if (sign == 3)
                            doubleValue /= svalDouble;
                        else if (sign == 4)
                            doubleValue %= svalDouble;
                        else
                        {
                            Debug.Write("Error 不支持Double 的这种类型的操作");
                        }
                    }
                    break;
                default:
                    {
                        Debug.Write("Error 不支持该类型的算术/位运算");
                    }
                    break;
            }
        }

        public void AddSValue(ref SValue sval, bool isUnsign, out bool isMethodCall )
        {
            isMethodCall = false;
            if (sval.eType == EVMType.String)
            {
                string str = "";
                if( this.eType == EVMType.Class )
                {
                    ClassObject co = this.sobject as ClassObject;
                    if (co != null)
                    {
                        var method = co.runtimeType.irClass.GetIRNonStaticMethodIndexByName("toString", out int index);
                        if (method != null)
                        {
                            InnerCLRRuntimeVM.RunIRMethod(null, method, false);
                            var clrvm = InnerCLRRuntimeVM.clrRuntimeStack.Peek();
                            SValue curval = clrvm.GetCurrentIndexValue(clrvm.m_ValueIndex - 1);
                            str = curval.stringValue;
                        }
                    }
                    else
                    {
                        str = sval.sobject.ToString();
                    }
                }
                else
                {
                    str = sval.GetValueObject().ToString();
                }
                stringValue = this.GetValueObject().ToString() + str;
                this.eType = EVMType.String;
            }
            else if (this.eType == EVMType.String)
            {
                string str = "";
                if (sval.eType == EVMType.Class)
                {
                    ClassObject co = sval.sobject as ClassObject;
                    if (co != null)
                    {
                        var method = co.runtimeType.irClass.GetIRNonStaticMethodIndexByName("toString", out int index);
                        if (method != null)
                        {
                            InnerCLRRuntimeVM.RunIRMethod(null, method, false);
                            var clrvm = InnerCLRRuntimeVM.clrRuntimeStack.Peek();
                            SValue curval = clrvm.GetCurrentIndexValue(clrvm.m_ValueIndex - 1);
                            str = curval.stringValue;
                            clrvm.m_ValueIndex--;
                        }
                    }
                    else
                    {
                        str = sval.sobject.ToString();
                    }
                }
                else
                {
                    str = sval.GetValueObject().ToString();
                }
                stringValue = this.GetValueObject().ToString() + str;
            }
            else if( this.eType == EVMType.Array )
            {
                // 处理array1 + array2
            }
            else
            {
                if( this.eType == EVMType.Class )
                {
                    ClassObject co = sval.sobject as ClassObject;
                    if (co != null)
                    {
                        var method = co.runtimeType.irClass.GetIROperatorMethodIndexByMethod("_add_", out int index);
                        if (method != null)
                        {
                            InnerCLRRuntimeVM.RunIRMethod(null, method, false);
                            isMethodCall = true;
                        }
                    }
                }
                else
                {
                    ComputeSVAlue(0, ref sval, isUnsign);
                }
            }
        }
        public void MinusSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(1, ref sval, isUnsign);
        }
        public void MultiplySValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(2, ref sval, isUnsign);
        }
        public void DivSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(3, ref sval, isUnsign);
        }
        public void ModuloSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(4, ref sval, isUnsign);
        }
        public void CombineSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(5, ref sval, isUnsign);
        }
        public void InclusiveOrSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(6, ref sval, isUnsign);
        }
        public void XORSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(7, ref sval, isUnsign);
        }
        public void ShrSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(9, ref sval, isUnsign);
        }
        public void ShiSValue(SValue sval, bool isUnsign)
        {
            ComputeSVAlue(8, ref sval, isUnsign);
        }
        public void NotSValue()
        {
            switch (eType)
            {
                case EVMType.Byte:
                    {
                        eType = EVMType.Boolean;
                       int8Value = (int8Value== 0) ? (byte)1: (byte)0;
                    }
                    break;
                case EVMType.SByte:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (sint8Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.Boolean:
                    {
                        int8Value = (int8Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                //case EVMType.Char:
                //    {
                //        eType = EVMType.Boolean;
                //        int8Value = (charValue == 0) ? (byte)1 : (byte)0;
                //    }
                //    break;
                case EVMType.Int16:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (int16Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.UInt16:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (uint16Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.Int32:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (int32Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.UInt32:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (uint32Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.Int64:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (int64Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EVMType.UInt64:
                    {
                        eType = EVMType.Boolean;
                        int8Value = (uint64Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
            }
        }
        public void NegSValue(bool isUnsign)
        {
            switch (eType)
            {
                case EVMType.Byte:
                    {
                        eType = EVMType.Int32;
                        int32Value = -int8Value;
                    }
                    break;
                case EVMType.SByte:
                    {
                        eType = EVMType.Int32;
                        int32Value = -sint8Value;
                    }
                    break;
                //case EVMType.Char:
                //    {
                //        eType = EVMType.Int32;
                //        int32Value = -charValue;
                //    }
                //    break;
                case EVMType.Int16:
                    {
                        int16Value = (short)(-int16Value);
                    }
                    break;
                case EVMType.UInt16:
                    {
                        eType = EVMType.Int32;
                        int32Value = (-uint16Value);
                    }
                    break;
                case EVMType.Int32:
                    {
                        int32Value = -int32Value;
                    }
                    break;
                case EVMType.UInt32:
                    {
                        eType = EVMType.Int64;
                        int64Value = -uint32Value;
                    }
                    break;
                case EVMType.Int64:
                    {
                        int64Value = -int64Value;
                    }
                    break;
                case EVMType.UInt64:
                    {
                        Debug.Write("Error -value 1");
                    }
                    break;
                default:
                    {
                        Debug.Write("Error -value 2");
                    }
                    break;
            }
        }
    }
}
