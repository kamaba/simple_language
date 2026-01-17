//****************************************************************************
//  File:      SValue.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using SimpleLanguage.VM.Runtime;
using System.Collections.Generic;

namespace SimpleLanguage.VM
{
    public partial struct SValue
    {
        public void ComputeSValue(SValue sval, bool isUnsignCompute )
        {
#pragma warning disable CS0219 // 变量已被赋值，但从未使用过它的值
            bool isNumber = false;
#pragma warning restore CS0219 // 变量已被赋值，但从未使用过它的值
#pragma warning disable CS0219 // 变量已被赋值，但从未使用过它的值
            bool isUnsign = false;
#pragma warning restore CS0219 // 变量已被赋值，但从未使用过它的值
            switch (eType)
            {
                case EVMType.Int32:
                case EVMType.UInt32:
                    {
                        
                    }
                    break;
                case EVMType.String:
                    {
                        switch (sval.eType)
                        {
                            case EVMType.Byte:
                                {
                                    stringValue += sval.int8Value.ToString();
                                }
                                break;
                            case EVMType.SByte:
                                {
                                    stringValue += sval.sint8Value.ToString();
                                }
                                break;
                            //case EVMType.Char:
                            //    {
                            //        stringValue += sval.charValue.ToString();
                            //    }
                            //    break;
                            case EVMType.Int16:
                                {
                                    stringValue += sval.int16Value.ToString();
                                }
                                break;
                            case EVMType.UInt16:
                                {
                                    stringValue += sval.uint16Value.ToString();
                                }
                                break;
                            case EVMType.Int32:
                                {
                                    stringValue += sval.int32Value.ToString();
                                }
                                break;
                            case EVMType.UInt32:
                                {
                                    stringValue += sval.uint32Value.ToString();
                                }
                                break;
                            case EVMType.Int64:
                                {
                                    stringValue += sval.int64Value.ToString();
                                }
                                break;
                            case EVMType.UInt64:
                                {
                                    stringValue += sval.uint64Value.ToString();
                                }
                                break;
                            case EVMType.String:
                                {
                                    stringValue += sval.stringValue;
                                }
                                break;
                        }
                    }
                    break;
            }
            switch (sval.eType)
            {
                case EVMType.Int32: int32Value += sval.int32Value; break;
                case EVMType.String:
                    {
                        SetStringValue(int32Value.ToString() + sval.stringValue);
                    }
                    break;
            }
        }       
        public void SetInt8Compare(byte a, byte b, int compareSign, bool isOrEqual)
        {
            if (compareSign == 0)
            {
                SetBoolValue(a == b);

            }
            else if (compareSign == 1)
            {
                SetBoolValue(a != b);
            }
            else if (compareSign == 2)
            {
                if (isOrEqual)
                {
                    SetBoolValue(a >= b);
                }
                else
                {
                    SetBoolValue(a > b);
                }
            }
            else if (compareSign == 3)
            {
                if (isOrEqual)
                {
                    SetBoolValue(a <= b);
                }
                else
                {
                    SetBoolValue(a < b);
                }
            }
        }
        public void SetInt16Compare(short a, short b, int compareSign, bool isOrEqual)
        {
            if (compareSign == 0)
            {
                SetBoolValue(a == b);

            }
            else if (compareSign == 1)
            {
                SetBoolValue(a != b);
            }
            else if (compareSign == 2)
            {
                if (isOrEqual)
                {
                    SetBoolValue(a >= b);
                }
                else
                {
                    SetBoolValue(a > b);
                }
            }
            else if (compareSign == 3)
            {
                if (isOrEqual)
                {
                    SetBoolValue(a <= b);
                }
                else
                {
                    SetBoolValue(a < b);
                }
            }
        }
        public void SetInt32Compare(int a, int b, int compareSign, bool isOrEqual)
        {
            if (compareSign == 0)
            {
                SetBoolValue(a == b);

            }
            else if (compareSign == 1)
            {
                SetBoolValue(a != b);
            }
            else if (compareSign == 2)
            {
                if (isOrEqual)
                {
                    SetBoolValue(a >= b);
                }
                else
                {
                    SetBoolValue(a > b);
                }
            }
            else if (compareSign == 3)
            {
                if (isOrEqual)
                {
                    SetBoolValue(a <= b);
                }
                else
                {
                    SetBoolValue(a < b);
                }
            }
        }
        public void SetInt64Compare(long a, long b, int compareSign, bool isOrEqual)
        {
            if (compareSign == 0)
            {
                SetBoolValue(a == b);

            }
            else if (compareSign == 1)
            {
                SetBoolValue(a != b);
            }
            else if (compareSign == 2)
            {
                if (isOrEqual)
                {
                    SetBoolValue(a >= b);
                }
                else
                {
                    SetBoolValue(a > b);
                }
            }
            else if (compareSign == 3)
            {
                if (isOrEqual)
                {
                    SetBoolValue(a <= b);
                }
                else
                {
                    SetBoolValue(a < b);
                }
            }
        }
        // compareSign 0:== 1:!= 
        public static void CompareEuqalSValue1AndValue2( ref SValue sval1, ref SValue sval2, bool isEqual, out bool methodCall )
        {
            methodCall = false;

            if (sval1.isNull)
            {
                if (isEqual)
                {
                    sval1.SetBoolValue(sval2.isNull ? true : false);
                }
                else
                {
                    sval1.SetBoolValue(sval2.isNull ? false : true);
                }
                return;
            }
            if (sval2.isNull)
            {
                if (isEqual)
                {
                    sval1.SetBoolValue(sval1.isNull ? true : false);
                }
                else
                {
                    sval1.SetBoolValue(sval1.isNull ? false : true);
                }
                return;
            }

            // numeric comparison path will use explicit promotion rules
            // handle simple cases first
            switch (sval1.eType)
            {
                //String 只允许对字符形式比较 
                case EVMType.String:
                    {
                        switch (sval2.eType)
                        {
                            case EVMType.String:
                                {
                                    if (isEqual)
                                        sval1.SetBoolValue(sval1.stringValue == sval2.stringValue);
                                    else
                                        sval1.SetBoolValue(sval1.stringValue != sval2.stringValue);
                                }
                                break;
                            default:
                                {
                                    sval1.SetBoolValue(false); break;
                                }
                        }
                        return;
                    }
                //只允许对boolean 只允许对字符形式比较 
                case EVMType.Boolean:
                    {
                        switch (sval2.eType)
                        {
                            case EVMType.Boolean:
                                {
                                    if (isEqual)
                                    {
                                        sval1.SetBoolValue(sval1.int8Value == sval2.int8Value );
                                    }
                                    else
                                    {
                                        sval1.SetBoolValue(sval1.int8Value != sval2.int8Value);
                                    }
                                }
                                break;
                            default:
                                {
                                    sval1.SetBoolValue(false); break;
                                }                                
                        }
                        return;
                    }
                case EVMType.Byte:
                case EVMType.SByte:
                case EVMType.Int16:
                case EVMType.UInt16:
                case EVMType.Int32:
                case EVMType.UInt32:
                case EVMType.Int64:
                case EVMType.UInt64:
                case EVMType.Float32:
                case EVMType.Float64:
                    // numeric handled below via promotion
                    break;
                case EVMType.Array:
                    {
                        if (sval2.eType == EVMType.Array )
                        {
                            if( sval1.sobject == sval2.sobject )
                            {
                                sval1.SetBoolValue(true);
                            }
                            else
                            {
                                sval1.SetBoolValue(false);
                            }
                        }
                        else
                        {
                            sval1.SetBoolValue(false);
                        }
                    }
                    break;
                case EVMType.Object:
                    {
                        if( sval2.eType == EVMType.Object )
                        {
                            sval1.SetBoolValue( sval1.sobject == sval2.sobject );
                            return;
                        }
                    }
                    break;
                case EVMType.Class:
                    {
                        ClassObject co = (sval1.sobject as ClassObject);
                        RuntimeType rt = co.value.runtimeType;
                        IRMetaClass irc = co.value.irMetaClass;                        
                        if (irc == null)
                        {
                            Log.AddVM(EError.None, "IRC是调用虚函数为空!!");
                            return;
                        }
                        IRMethod cfc = irc.GetIROperatorMethodIndexByMethod(isEqual ? "_eq_" : "_ne_", out int index);
                        if (cfc != null)
                        {
                            List<RuntimeType> irmtList = new List<RuntimeType>();
                            InnerCLRRuntimeVM.RunIRMethod(irmtList, cfc, false );
                            methodCall = true;
                        }
                        else
                        {
                            if (sval2.eType == EVMType.Class)
                            {
                                if (sval1.sobject == sval2.sobject)
                                {
                                    sval1.SetBoolValue(true);
                                }
                                else
                                {
                                    sval1.SetBoolValue(false);
                                }
                            }
                            else
                            {
                                sval1.SetBoolValue(!isEqual);
                                //Log.AddVM(EError.None, " VM Compare SVAlue 比较的低码还没有完善!!");
                            }

                        }
                        return;
                    }
            }

            // if both are numeric types -> numeric equality with promotion
            if (IsNumericType(sval1.eType) && IsNumericType(sval2.eType))
            {
                // float promotion
                bool leftFloat = (sval1.eType == EVMType.Float32 || sval1.eType == EVMType.Float64);
                bool rightFloat = (sval2.eType == EVMType.Float32 || sval2.eType == EVMType.Float64);
                if (leftFloat || rightFloat)
                {
                    double a = (sval1.eType == EVMType.Float64) ? sval1.doubleValue : (sval1.eType == EVMType.Float32 ? sval1.floatValue : sval1.ConvertToDoubleFromIntTypes());
                    double b = (sval2.eType == EVMType.Float64) ? sval2.doubleValue : (sval2.eType == EVMType.Float32 ? sval2.floatValue : sval2.ConvertToDoubleFromIntTypes());
                    if (isEqual) sval1.SetBoolValue(a == b); else sval1.SetBoolValue(a != b);
                    return;
                }

                // unsigned promotion if either is unsigned
                bool useUnsigned = sval1.IsUnsignedType(sval1.eType) || sval2.IsUnsignedType(sval2.eType);
                if (useUnsigned)
                {
                    ulong a = sval1.ConvertToULong();
                    ulong b = sval2.ConvertToULong();
                    if (isEqual) sval1.SetBoolValue(a == b); else sval1.SetBoolValue(a != b);
                    return;
                }

                // signed integer comparison
                long la = sval1.ConvertToLong();
                long lb = sval2.ConvertToLong();
                if (isEqual) sval1.SetBoolValue(la == lb); else sval1.SetBoolValue(la != lb);
                return;
            }

            switch (sval2.eType)
            {
                case EVMType.String:
                    {
                        Log.AddVM(EError.None, " VM Compare SVAlue string 比较的低码还没有完善!!");
                        //return;
                    }
                    break;
                case EVMType.Boolean:
                    {
                        Log.AddVM(EError.None, " VM Compare SVAlue boolean 比较的低码还没有完善!!");
                        //return;
                    }
                    break;
                // numeric types handled earlier
                case EVMType.Array: { 
                    }
                    break;
                case EVMType.Class:
                    {
                        ClassObject co = (sval2.sobject as ClassObject);
                        RuntimeType rt = co.value.runtimeType;
                        IRMetaClass irc = co.value.irMetaClass;                        
                        if (irc == null)
                        {
                            Log.AddVM(EError.None, "IRC是调用虚函数为空!!");
                            return;
                        }
                        IRMethod cfc = irc.GetIROperatorMethodIndexByMethod(isEqual ? "_eq_" : "_ne_", out int index);
                        if (cfc != null)
                        {
                            List<RuntimeType> irmtList = new List<RuntimeType>();
                            InnerCLRRuntimeVM.RunIRMethod(irmtList, cfc);
                            methodCall = true;
                        }
                        else
                        {
                            if (sval1.eType == EVMType.Class)
                            {
                                if (sval1.sobject == sval2.sobject)
                                {
                                    sval1.SetBoolValue(true);
                                }
                                else
                                {
                                    sval1.SetBoolValue(false);
                                }
                            }
                            else
                            {
                                sval1.SetBoolValue(!isEqual);
                                //Log.AddVM(EError.None, " VM Compare SVAlue 比较的低码还没有完善!!");
                            }

                        }
                        return;
                    }
            }

            // fallback: already handled objects/classes earlier; default false
            sval1.SetBoolValue(false);
            Log.AddVM(EError.None, "VM Compare SVAlue 比较的低码还没有完善!!");
        }


        //0> 1:>= 2:< 3:<= 
        public static void CompareSValue1AndValue2(ref SValue sval1, ref SValue sval2, int compareSign)
        {
            // logical operators (used by VM OpCode And/Or)
            if (compareSign == 4)
            {
                // logical AND
                bool a = IsTruthy(ref sval1);
                bool b = IsTruthy(ref sval2);
                sval1.SetBoolValue(a && b);
                return;
            }
            if (compareSign == 6)
            {
                // logical OR
                bool a = IsTruthy(ref sval1);
                bool b = IsTruthy(ref sval2);
                sval1.SetBoolValue(a || b);
                return;
            }

            // numeric comparisons
            if (IsNumericType(sval1.eType) && IsNumericType(sval2.eType))
            {
                bool leftFloat = (sval1.eType == EVMType.Float32 || sval1.eType == EVMType.Float64);
                bool rightFloat = (sval2.eType == EVMType.Float32 || sval2.eType == EVMType.Float64);
                if (leftFloat || rightFloat)
                {
                    double a = (sval1.eType == EVMType.Float64) ? sval1.doubleValue : (sval1.eType == EVMType.Float32 ? sval1.floatValue : sval1.ConvertToDoubleFromIntTypes());
                    double b = (sval2.eType == EVMType.Float64) ? sval2.doubleValue : (sval2.eType == EVMType.Float32 ? sval2.floatValue : sval2.ConvertToDoubleFromIntTypes());
                    switch (compareSign)
                    {
                        case 0: sval1.SetBoolValue(a > b); break;
                        case 1: sval1.SetBoolValue(a >= b); break;
                        case 2: sval1.SetBoolValue(a < b); break;
                        case 3: sval1.SetBoolValue(a <= b); break;
                    }
                    return;
                }

                bool useUnsigned = sval1.IsUnsignedType(sval1.eType) || sval2.IsUnsignedType(sval2.eType);
                if (useUnsigned)
                {
                    ulong a = sval1.ConvertToULong();
                    ulong b = sval2.ConvertToULong();
                    switch (compareSign)
                    {
                        case 0: sval1.SetBoolValue(a > b); break;
                        case 1: sval1.SetBoolValue(a >= b); break;
                        case 2: sval1.SetBoolValue(a < b); break;
                        case 3: sval1.SetBoolValue(a <= b); break;
                    }
                    return;
                }

                long la = sval1.ConvertToLong();
                long lb = sval2.ConvertToLong();
                switch (compareSign)
                {
                    case 0: sval1.SetBoolValue(la > lb); break;
                    case 1: sval1.SetBoolValue(la >= lb); break;
                    case 2: sval1.SetBoolValue(la < lb); break;
                    case 3: sval1.SetBoolValue(la <= lb); break;
                }
                return;
            }
        }

        // helper: numeric type check
        static bool IsNumericType(EVMType t)
        {
            switch (t)
            {
                case EVMType.Byte:
                case EVMType.SByte:
                case EVMType.Int16:
                case EVMType.UInt16:
                case EVMType.Int32:
                case EVMType.UInt32:
                case EVMType.Int64:
                case EVMType.UInt64:
                case EVMType.Float32:
                case EVMType.Float64:
                    return true;
                default:
                    return false;
            }
        }

        // logical && and || on truthiness
        public static void LogicalAnd(ref SValue left, ref SValue right)
        {
            bool a = IsTruthy(ref left);
            bool b = IsTruthy(ref right);
            left.SetBoolValue(a && b);
        }
        public static void LogicalOr(ref SValue left, ref SValue right)
        {
            bool a = IsTruthy(ref left);
            bool b = IsTruthy(ref right);
            left.SetBoolValue(a || b);
        }
        public static bool IsTruthy(ref SValue v)
        {
            if (v.isNull) return false;
            switch (v.eType)
            {
                case EVMType.Boolean: return v.int8Value != 0;
                case EVMType.String: return !string.IsNullOrEmpty(v.stringValue);
                case EVMType.Float32: return v.floatValue != 0.0f;
                case EVMType.Float64: return v.doubleValue != 0.0;
                case EVMType.Byte: return v.int8Value != 0;
                case EVMType.SByte: return v.sint8Value != 0;
                case EVMType.Int16: return v.int16Value != 0;
                case EVMType.UInt16: return v.uint16Value != 0;
                case EVMType.Int32: return v.int32Value != 0;
                case EVMType.UInt32: return v.uint32Value != 0;
                case EVMType.Int64: return v.int64Value != 0;
                case EVMType.UInt64: return v.uint64Value != 0;
                default: return v.sobject != null;
            }
        }
    }
}
