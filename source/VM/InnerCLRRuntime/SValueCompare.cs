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

            dynamic valnum1 = null;
            dynamic valnum2 = null;
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
                    valnum1 = sval1.GetValueObject() as dynamic;
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
                    valnum2 = sval2.GetValueObject() as dynamic;
                    break;
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

            if (valnum2 != null && valnum1 != null )
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
                Log.AddVM(EError.None, "else VM Compare SVAlue 比较的低码还没有完善!!");
            }
        }


        //0> 1:>= 2:< 3:<= 
        public static void CompareSValue1AndValue2(ref SValue sval1, ref SValue sval2, int compareSign)
        {
            dynamic valnum1 = null;
            dynamic valnum2 = null;
            switch (sval1.eType)
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
                //case EVMType.RawByte:
                //case EVMType.RawSByte:
                //case EVMType.RawInt16:
                //case EVMType.RawUInt16:
                //case EVMType.RawInt32:
                //case EVMType.RawUInt32:
                //case EVMType.RawInt64:
                //case EVMType.RawUInt64:
                //case EVMType.RawFloat32:
                //case EVMType.RawFloat64:
                    valnum1 = sval1.GetValueObject() as dynamic;
                    break;
            }

            switch (sval2.eType)
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
                //case EVMType.RawByte:
                //case EVMType.RawSByte:
                //case EVMType.RawInt16:
                //case EVMType.RawUInt16:
                //case EVMType.RawInt32:
                //case EVMType.RawUInt32:
                //case EVMType.RawInt64:
                //case EVMType.RawUInt64:
                //case EVMType.RawFloat32:
                //case EVMType.RawFloat64:
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
                if(sval1.eType == EVMType.Class )
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
                Log.AddVM(EError.None, " end VM Compare SVAlue 比较的低码还没有完善!!");
            }
        }
    }
}
