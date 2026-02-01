//****************************************************************************
//  File:      IRUtil.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: IR common function
//****************************************************************************

using SimpleLanguage.Core;
using System.Diagnostics;

namespace SimpleLanguage.IR
{
    public class IRUtil
    {
        public static EIROpCode GetConstIROpCode(EType etype)
        {
            switch (etype)
            {
                case EType.Byte: return EIROpCode.LoadConstByte;
                case EType.SByte: return EIROpCode.LoadConstSByte;
                case EType.Boolean: return EIROpCode.LoadConstBoolean;
                //case EType.Char: return EIROpCode.LoadConstChar;
                case EType.Int16: return EIROpCode.LoadConstInt16;
                case EType.UInt16: return EIROpCode.LoadConstUInt16;
                case EType.Int32: return EIROpCode.LoadConstInt32;
                case EType.UInt32: return EIROpCode.LoadConstUInt32;
                case EType.Int64: return EIROpCode.LoadConstInt64;
                case EType.UInt64: return EIROpCode.LoadConstUInt64;
                case EType.Float32: return EIROpCode.LoadConstFloat32;
                case EType.Float64: return EIROpCode.LoadConstFloat64;
                case EType.String: return EIROpCode.LoadConstString;
                case EType.Null: return EIROpCode.LoadConstNull;
                default:
                    {
                        Debug.Write("Error GetConstIROpCode!!");
                    }
                    break;
            }
            return EIROpCode.Nop;
        }
        public static IRData CreateLeftAndRightIRData(ELeftRightOpSign opSign)
        {
            IRData data = new IRData();
            switch (opSign)
            {
                case ELeftRightOpSign.Add:
                    {
                        data.opCode = EIROpCode.Add;
                    }
                    break;
                case ELeftRightOpSign.Minus:
                    {
                        data.opCode = EIROpCode.Minus;
                    }
                    break;
                case ELeftRightOpSign.Multiply:
                    {
                        data.opCode = EIROpCode.Multiply;
                    }
                    break;
                case ELeftRightOpSign.Divide:
                    {
                        data.opCode = EIROpCode.Divide;
                    }
                    break;
                case ELeftRightOpSign.Modulo:
                    {
                        data.opCode = EIROpCode.Modulo;
                    }
                    break;
                case ELeftRightOpSign.InclusiveOr:
                    {
                        data.opCode = EIROpCode.InclusiveOr;
                    }
                    break;
                case ELeftRightOpSign.Combine:
                    {
                        data.opCode = EIROpCode.Combine;
                    }
                    break;
                case ELeftRightOpSign.XOR:
                    {
                        data.opCode = EIROpCode.XOR;
                    }
                    break;
                case ELeftRightOpSign.Shi:
                    {
                        data.opCode = EIROpCode.Shi;
                    }
                    break;
                case ELeftRightOpSign.Shr:
                    {
                        data.opCode = EIROpCode.Shr;
                    }
                    break;

                case ELeftRightOpSign.Equal:
                    {
                        data.opCode = EIROpCode.Ceq;
                    }
                    break;
                case ELeftRightOpSign.NotEqual:
                    {
                        data.opCode = EIROpCode.Cne;
                    }
                    break;
                case ELeftRightOpSign.Greater:
                    {
                        data.opCode = EIROpCode.Cgt;
                    }
                    break;
                case ELeftRightOpSign.GreaterOrEqual:
                    {
                        data.opCode = EIROpCode.Cge;
                    }
                    break;
                case ELeftRightOpSign.Less:
                    {
                        data.opCode = EIROpCode.Clt;
                    }
                    break;
                case ELeftRightOpSign.LessOrEqual:
                    {
                        data.opCode = EIROpCode.Cle;
                    }
                    break;
                case ELeftRightOpSign.Or:
                    {
                        data.opCode = EIROpCode.Or;
                    }
                    break;
                case ELeftRightOpSign.And:
                    {
                        data.opCode = EIROpCode.And;
                    }
                    break;
                default:
                    {
                        Debug.Write("Error 未支持表达式中的IR代码" + opSign.ToString());
                    }
                    break;
            }
            return data;
        }
        public static int GetTypeSize(EType etype)
        {
            switch (etype)
            {
                case EType.Bit:
                    return 1;
                case EType.Byte:
                case EType.Boolean:
                    return 1;
                //case EType.Char:
                //    return 2;
                case EType.Int16:
                case EType.UInt16:
                    return 2;
                case EType.Int32:
                case EType.UInt32:
                case EType.Class:
                case EType.String:
                case EType.Float32:
                    return 4;
                case EType.Int64:
                case EType.UInt64:
                case EType.Float64:
                    return 8;
                case EType.Int128:
                case EType.UInt128:
                    return 16;
                case EType.Float2:
                    return 8;

            }
            return 1;
        }
    }

}
