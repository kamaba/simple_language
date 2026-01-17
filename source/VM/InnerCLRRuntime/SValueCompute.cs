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
            // unify numeric operations with promotion rules
            bool leftIsFloat = (eType == EVMType.Float32 || eType == EVMType.Float64);
            bool rightIsFloat = (svalue.eType == EVMType.Float32 || svalue.eType == EVMType.Float64);

            // If any side is float -> use double precision
            if (leftIsFloat || rightIsFloat)
            {
                double a = (eType == EVMType.Float64) ? doubleValue : (eType == EVMType.Float32 ? (double)floatValue : ConvertToDoubleFromIntTypes());
                double b = (svalue.eType == EVMType.Float64) ? svalue.doubleValue : (svalue.eType == EVMType.Float32 ? (double)svalue.floatValue : svalue.ConvertToDoubleFromIntTypes());
                double r = 0;
                switch (sign)
                {
                    case 0: r = a + b; break;
                    case 1: r = a - b; break;
                    case 2: r = a * b; break;
                    case 3: r = (b == 0) ? 0 : a / b; break;
                    case 4: r = (b == 0) ? 0 : a % b; break;
                    default:
                        Debug.Write("Error 不支持浮点的位运算");
                        break;
                }
                if (eType == EVMType.Float64)
                    doubleValue = r;
                else
                    floatValue = (float)r;
                return;
            }

            // integer operations - decide signed vs unsigned based on flag or operand types
            bool useUnsigned = isUnSign || IsUnsignedType(eType) || IsUnsignedType(svalue.eType);
            if (useUnsigned)
            {
                ulong a = ConvertToULong();
                ulong b = svalue.ConvertToULong();
                ulong r = 0;
                switch (sign)
                {
                    case 0: r = a + b; break;
                    case 1: r = a - b; break;
                    case 2: r = a * b; break;
                    case 3: r = (b == 0) ? 0UL : a / b; break;
                    case 4: r = (b == 0) ? 0UL : a % b; break;
                    case 5: r = a & b; break;
                    case 6: r = a | b; break;
                    case 7: r = a ^ b; break;
                    case 8: r = a << (int)b; break;
                    case 9: r = a >> (int)b; break;
                }
                // write back according to original left type
                AssignULongToType(r);
                return;
            }

            // signed integer
            long la = ConvertToLong();
            long lb = svalue.ConvertToLong();
            long lr = 0;
            switch (sign)
            {
                case 0: lr = la + lb; break;
                case 1: lr = la - lb; break;
                case 2: lr = la * lb; break;
                case 3: lr = (lb == 0) ? 0L : la / lb; break;
                case 4: lr = (lb == 0) ? 0L : la % lb; break;
                case 5: lr = la & lb; break;
                case 6: lr = la | lb; break;
                case 7: lr = la ^ lb; break;
                case 8: lr = la << (int)lb; break;
                case 9: lr = la >> (int)lb; break;
            }
            AssignLongToType(lr);
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
