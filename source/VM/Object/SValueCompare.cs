//****************************************************************************
//  File:      SValue.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Parse;
using SimpleLanguage.VM.Runtime;
using System;
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
                case EType.Int32:
                case EType.UInt32:
                    {
                        
                    }
                    break;
                case EType.String:
                    {
                        switch (sval.eType)
                        {
                            case EType.Byte:
                                {
                                    stringValue += sval.int8Value.ToString();
                                }
                                break;
                            case EType.SByte:
                                {
                                    stringValue += sval.sint8Value.ToString();
                                }
                                break;
                            //case EType.Char:
                            //    {
                            //        stringValue += sval.charValue.ToString();
                            //    }
                            //    break;
                            case EType.Int16:
                                {
                                    stringValue += sval.int16Value.ToString();
                                }
                                break;
                            case EType.UInt16:
                                {
                                    stringValue += sval.uint16Value.ToString();
                                }
                                break;
                            case EType.Int32:
                                {
                                    stringValue += sval.int32Value.ToString();
                                }
                                break;
                            case EType.UInt32:
                                {
                                    stringValue += sval.uint32Value.ToString();
                                }
                                break;
                            case EType.Int64:
                                {
                                    stringValue += sval.int64Value.ToString();
                                }
                                break;
                            case EType.UInt64:
                                {
                                    stringValue += sval.uint64Value.ToString();
                                }
                                break;
                            case EType.String:
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
                case EType.Int32: int32Value += sval.int32Value; break;
                case EType.String:
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
        public static void CompareEuqalSValue1AndValue2( ref SValue sval1, ref SValue sval2, bool isEqual )
        {
            if (sval1.isNull)
            {
                if(isEqual )
                {
                    sval1.SetBoolValue(sval2.isNull ? true : false );
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
            dynamic valnum1 = null;
            dynamic valnum2 = null;
            switch (sval1.eType)
            {
                //String 只允许对字符形式比较 
                case EType.String:
                    {
                        switch (sval2.eType)
                        {
                            case EType.String:
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
                case EType.Boolean:
                    {
                        switch (sval2.eType)
                        {
                            case EType.Boolean:
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
                case EType.Byte:
                case EType.SByte:
                case EType.Int16:
                case EType.UInt16:
                case EType.Int32:
                case EType.UInt32:
                case EType.Int64:
                case EType.UInt64:
                case EType.Float32:
                case EType.Float64:
                    valnum1 = sval1.GetValueObject() as dynamic;
                    break;
                case EType.Array:
                    {
                        if (sval2.eType == EType.Array )
                        {
                            if( sval1.arrayValue == sval2.arrayValue )
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
                case EType.Class:
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
                            InnerCLRRuntimeVM.RunIRMethod(irmtList, cfc);
                        }
                        else
                        {
                            if (sval2.eType == EType.Class)
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
                    }
                    break;
                default:
                    {
                        Log.AddVM(EError.None, " VM Compare SVAlue 比较的低码还没有完善!!");
                        return;
                    }
            }

            switch (sval2.eType)
            {
                case EType.String:
                    {
                        Log.AddVM(EError.None, " VM Compare SVAlue 比较的低码还没有完善!!");
                        //return;
                    }
                    break;
                case EType.Boolean:
                    {
                        Log.AddVM(EError.None, " VM Compare SVAlue 比较的低码还没有完善!!");
                        //return;
                    }
                    break;
                case EType.Byte:
                case EType.SByte:
                case EType.Int16:
                case EType.UInt16:
                case EType.Int32:
                case EType.UInt32:
                case EType.Int64:
                case EType.UInt64:
                case EType.Float32:
                case EType.Float64:
                    valnum2 = sval2.GetValueObject() as dynamic;
                    break;
                case EType.Array: { 
                    }
                    break;
                case EType.Class:
                    {
                        Log.AddVM(EError.None, " VM Compare SVAlue 比较的低码还没有完善!!");
                        //return;
                    }
                    break;
                default:
                    {
                        Log.AddVM(EError.None, " VM Compare SVAlue 比较的低码还没有完善!!");
                    }
                    break;
            }

            if(valnum2 != null && valnum1 != null )
            {
                if( isEqual )
                {
                    sval2.SetBoolValue( valnum1 == valnum2 );
                }
                else
                {
                    sval2.SetBoolValue(valnum1 != valnum2);
                }
            }
            else
            {
                sval2.SetBoolValue(false);
                Log.AddVM(EError.None, " VM Compare SVAlue 比较的低码还没有完善!!");
            }
        }


        //0> 1:>= 2:< 3:<= 
        public static void CompareSValue1AndValue2(ref SValue sval1, ref SValue sval2, int compareSign)
        {
            dynamic valnum1 = null;
            dynamic valnum2 = null;
            switch (sval1.eType)
            {
                case EType.Byte:
                case EType.SByte:
                case EType.Int16:
                case EType.UInt16:
                case EType.Int32:
                case EType.UInt32:
                case EType.Int64:
                case EType.UInt64:
                case EType.Float32:
                case EType.Float64:
                    valnum1 = sval1.GetValueObject() as dynamic;
                    break;
            }

            switch (sval2.eType)
            {
                case EType.Byte:
                case EType.SByte:
                case EType.Int16:
                case EType.UInt16:
                case EType.Int32:
                case EType.UInt32:
                case EType.Int64:
                case EType.UInt64:
                case EType.Float32:
                case EType.Float64:
                    valnum2 = sval2.GetValueObject() as dynamic;
                    break;
            }

            if (valnum2 != null && valnum1 != null)
            {
                switch(compareSign )
                {
                    case 0:
                        sval1.SetBoolValue(valnum1 > valnum2);
                        break;
                    case 1:
                        sval1.SetBoolValue(valnum1 >= valnum2);
                        break;
                    case 2:
                        sval1.SetBoolValue(valnum1 < valnum2);
                        break;
                    case 3:
                        sval1.SetBoolValue(valnum1 <= valnum2);
                        break;
                }
            }
            else
            {
                if(sval1.eType == EType.Object )
                {
                    ClassObject co = (sval1.sobject as ClassObject);
                    if (co != null)
                    {
                        var method = co.runtimeType.irClass.GetIROperatorMethodIndexByMethod(">", out int index );
                        if( method != null )
                        {

                        }
                    }
                    else
                    {

                    }
                }
                else
                {
                    sval2.SetBoolValue(false);
                }
                Log.AddVM(EError.None, " VM Compare SVAlue 比较的低码还没有完善!!");
            }
        }
    }
}
