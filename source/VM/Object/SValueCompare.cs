//****************************************************************************
//  File:      SValue.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
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
        public void ComputeSValue(SValue sval, bool isUnsignCompute )
        {
            bool isNumber = false;
            bool isUnsign = false;
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
                            case EType.Char:
                                {
                                    stringValue += sval.charValue.ToString();
                                }
                                break;
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
        // compareSign 0:== 1:!= 2:>= 3:<= 4: && 5:||
        public void CompareSValue(SValue sval, int compareSign, bool isOrEqual )
        {
            switch (this.eType)
            {
                case EType.Int32:
                    {
                        switch (sval.eType)
                        {
                            case EType.Byte:
                                {
                                    SetInt32Compare(int32Value, sval.int8Value, compareSign, isOrEqual);
                                }
                                break;
                            case EType.SByte:
                                {
                                    SetInt32Compare(int32Value, sval.sint8Value, compareSign, isOrEqual);
                                }
                                break;
                            case EType.Char:
                                {
                                    SetInt32Compare(int32Value, sval.charValue, compareSign, isOrEqual);
                                }
                                break;
                            case EType.Int16:
                                {
                                    SetInt32Compare(int32Value, sval.int16Value, compareSign, isOrEqual);
                                }
                                break;
                            case EType.UInt16:
                                {
                                    SetInt32Compare(int32Value, sval.uint16Value, compareSign, isOrEqual);
                                }
                                break;
                            case EType.Int32:
                                {
                                    SetInt32Compare(int32Value, sval.int32Value, compareSign, isOrEqual);
                                }
                                break;
                            case EType.UInt32:
                                {
                                    SetInt64Compare(int32Value, (long)sval.uint32Value, compareSign, isOrEqual);
                                }
                                break;
                            case EType.Int64:
                                {
                                    SetInt32Compare(int32Value, sval.int32Value, compareSign, isOrEqual);
                                }
                                break;
                            case EType.UInt64:
                                {
                                    SetInt64Compare( (long)int32Value, (long)sval.uint64Value, compareSign, isOrEqual);
                                }
                                break;
                        }
                    }
                    break;
                case EType.Boolean:
                    {
                        switch (sval.eType)
                        {
                            case EType.Boolean:
                                {
                                    if( compareSign == 4 )
                                    {
                                        SetBoolValue( int8Value == sval.int8Value );
                                    }
                                    else if( compareSign == 5 )
                                    {
                                        SetBoolValue(int8Value == 1 || sval.int8Value == 1 );
                                    }
                                }
                                break;
                        }
                    }
                    break;
            }
        }
    }
}
