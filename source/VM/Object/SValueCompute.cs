//****************************************************************************
//  File:      SValue.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description:  compute left and right value's method example: +-*/%&|^>><<
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SimpleLanguage.Core;

namespace SimpleLanguage.VM
{
    public partial struct SValue
    {
        // sign 0:+ 1:- 2:* 3:/ 4:% 5:& 6:| 7:^  8:<< 9:>>
        public void ComputeSVAlue( int sign, ref SValue svalue, bool isUnSign )
        {
            switch (this.eType)
            {
                case EType.Int64:
                    {
                        long svalLong = (long)svalue.GetValueObject();
                        if(sign==0)
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
                            int64Value = int64Value ^ svalLong;
                        else if (sign == 8)
                            int64Value = int64Value << (int)svalLong;
                        else if (sign == 9)
                            int64Value = int64Value >> (int)svalLong;
                    }
                    break;
                case EType.UInt64:
                    {
                        if(svalue.eType == EType.UInt64 )
                        {
                            if (sign == 0)
                                uint64Value += svalue.uint64Value;
                            else if (sign == 1)
                                uint64Value -= svalue.uint64Value;
                            else if (sign == 2)
                                uint64Value *= svalue.uint64Value;
                            else if (sign == 3)
                                uint64Value /= svalue.uint64Value;
                            else if (sign == 4)
                                uint64Value %= svalue.uint64Value;
                            else if (sign == 5)
                                uint64Value &= svalue.uint64Value;
                            else if (sign == 6)
                                uint64Value |= svalue.uint64Value;
                            else if (sign == 7)
                                uint64Value = uint64Value ^ svalue.uint64Value;
                            else if (sign == 8)
                                uint64Value = uint64Value << (int)svalue.uint64Value;
                            else if (sign == 9)
                                uint64Value = uint64Value >> (int)svalue.uint64Value;
                        }
                        else
                        {
                            long svalLong = (long)svalue.GetValueObject();
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
                                int64Value = int64Value ^ svalLong;
                            else if (sign == 8)
                                int64Value = int64Value << (int)svalLong;
                            else if (sign == 9)
                                int64Value = int64Value >> (int)svalLong;
                        }
                    }
                    break;
                case EType.Int32:
                    {
                        int svalInt = (int)svalue.GetValueObject();
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
                            int32Value = int32Value ^ svalInt;
                        else if (sign == 8)
                            int32Value = int32Value << (int)svalInt;
                        else if (sign == 9)
                            int32Value = int32Value >> (int)svalInt;
                    }
                    break;
                case EType.UInt32:
                    {
                        uint svalUInt = (uint)svalue.GetValueObject();
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
                            uint32Value = uint32Value ^ svalUInt;
                        else if (sign == 8)
                            uint32Value = uint32Value << (int)svalUInt;
                        else if (sign == 9)
                            uint32Value = uint32Value >> (int)svalUInt;
                    }
                    break;
                case EType.Int16:
                    {
                        short svalShort = (short)svalue.GetValueObject();
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
                case EType.UInt16:
                    {

                        short svalShort = (short)svalue.GetValueObject();
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
                case EType.Char:
                    {

                        char svalChar = (char)svalue.GetValueObject();
                        if (sign == 0)
                            charValue += svalChar;
                        else if (sign == 1)
                            charValue -= svalChar;
                        else if (sign == 2)
                            charValue *= svalChar;
                        else if (sign == 3)
                            charValue /= svalChar;
                        else if (sign == 4)
                            charValue %= svalChar;
                        else if (sign == 5)
                            charValue &= svalChar;
                        else if (sign == 6)
                            charValue |= svalChar;
                        else if (sign == 7)
                            charValue = (char)(charValue ^ svalChar);
                        else if (sign == 8)
                            charValue = (char)(charValue << svalChar);
                        else if (sign == 9)
                            charValue = (char)(charValue >> svalChar);
                    }
                    break;
                case EType.Byte:
                    {

                        byte svalByte = (byte)svalue.GetValueObject();
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
                            int8Value = (byte)(int16Value ^ svalByte);
                        else if (sign == 8)
                            int8Value = (byte)(int16Value << svalByte);
                        else if (sign == 9)
                            int8Value = (byte)(int16Value >> svalByte);
                    }
                    break;
                case EType.SByte:
                    {

                        sbyte svalSbyte = (sbyte)svalue.GetValueObject();
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
                default:
                    {
                        Console.WriteLine("Error -------------");
                    }
                    break;
            }
        }
        public void AddSValue(ref SValue sval, bool isUnsign )
        {
            if (sval.eType == EType.String)
            {
                stringValue = GetValueObject().ToString() + sval.GetValueObject().ToString();
            }
            else if (this.eType == EType.String)
            {
                eType = EType.String;
                stringValue = GetValueObject().ToString() + sval.GetValueObject().ToString();
            }
            else
            {
                ComputeSVAlue(0,ref sval, isUnsign);
            }
        }
        public void MinusSValue(SValue sval, bool isUnsign)
        {
        }
        public void MultiplySValue(SValue sval, bool isUnsign)
        {
        }
        public void DivSValue(SValue sval, bool isUnsign)
        {
        }
        public void ModuloSValue(SValue sval, bool isUnsign)
        {
        }
        public void CombineSValue(SValue sval, bool isUnsign)
        {
        }
        public void InclusiveOrSValue(SValue sval, bool isUnsign)
        {
        }
        public void XORSValue(SValue sval, bool isUnsign)
        {
        }
        public void ShrSValue(SValue sval, bool isUnsign)
        {
        }
        public void ShiSValue(SValue sval, bool isUnsign)
        {
        }
        public void NotSValue()
        {
            switch (eType)
            {
                case EType.Byte:
                    {
                        eType = EType.Boolean;
                       int8Value = (int8Value== 0) ? (byte)1: (byte)0;
                    }
                    break;
                case EType.SByte:
                    {
                        eType = EType.Boolean;
                        int8Value = (sint8Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EType.Boolean:
                    {
                        int8Value = (int8Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EType.Char:
                    {
                        eType = EType.Boolean;
                        int8Value = (charValue == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EType.Int16:
                    {
                        eType = EType.Boolean;
                        int8Value = (int16Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EType.UInt16:
                    {
                        eType = EType.Boolean;
                        int8Value = (uint16Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EType.Int32:
                    {
                        eType = EType.Boolean;
                        int8Value = (int32Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EType.UInt32:
                    {
                        eType = EType.Boolean;
                        int8Value = (uint32Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EType.Int64:
                    {
                        eType = EType.Boolean;
                        int8Value = (uint32Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
                case EType.UInt64:
                    {
                        eType = EType.Boolean;
                        int8Value = (uint64Value == 0) ? (byte)1 : (byte)0;
                    }
                    break;
            }
        }
        public void NegSValue(bool isUnsign)
        {
            switch (eType)
            {
                case EType.Byte:
                    {
                        eType = EType.Int32;
                        int32Value = -int8Value;
                    }
                    break;
                case EType.SByte:
                    {
                        eType = EType.Int32;
                        int32Value = -sint8Value;
                    }
                    break;
                case EType.Char:
                    {
                        eType = EType.Int32;
                        int32Value = -charValue;
                    }
                    break;
                case EType.Int16:
                    {
                        int16Value = (short)(-int16Value);
                    }
                    break;
                case EType.UInt16:
                    {
                        eType = EType.Int32;
                        int32Value = (-uint16Value);
                    }
                    break;
                case EType.Int32:
                    {
                        int32Value = -int32Value;
                    }
                    break;
                case EType.UInt32:
                    {
                        eType = EType.Int64;
                        int64Value = -uint32Value;
                    }
                    break;
                case EType.Int64:
                    {
                        int64Value = -int64Value;
                    }
                    break;
                case EType.UInt64:
                    {
                        Console.WriteLine("Error -value 1");
                    }
                    break;
                default:
                    {
                        Console.WriteLine("Error -value 2");
                    }
                    break;
            }
        }
    }
}
