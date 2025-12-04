//****************************************************************************
//  File:      SValue.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using System;
using System.Diagnostics;
namespace SimpleLanguage.VM
{
    public partial struct SValue
    {
        public EType eType;
        public byte int8Value;
        public sbyte sint8Value;
        //public char charValue;
        public short int16Value;
        public ushort uint16Value;
        public int int32Value;
        public uint uint32Value;
        public long int64Value;
        public ulong uint64Value;
        public float floatValue;
        public double doubleValue;
        public string stringValue;
        public SObject sobject;
        public ArrayObject arrayValue;
        public bool isNull;
        public void SetNullValue()
        {
            SetNull();
            eType = EType.Null;
        }
        public void SetBoolValue( bool val )
        {
            isNull = false;
            eType = EType.Boolean;
            int8Value = val ? (byte)1 : (byte)0;
        }
        public void SetNull()
        {
            isNull = true;
            if (sobject != null )
            {
                sobject.SetNull();
            }
        }
        public void SetInt8Value(byte val)
        {
            eType = EType.Byte;
            int8Value = val;
            isNull = false;
        }
        public void SetSInt8Value(sbyte val)
        {
            eType = EType.SByte;
            sint8Value = val;
            isNull = false;
        }
        //public void SetCharValue(char val)
        //{
        //    eType = EType.Char;
        //    charValue = val;
        //}
        public void SetInt16Value(Int16 val)
        {
            eType = EType.Int16;
            int16Value = val;
            isNull = false;
        }
        public void SetUInt16Value(UInt16 val)
        {
            eType = EType.UInt16;
            uint16Value = val;
            isNull = false;
        }
        public void SetInt32Value(Int32 val)
        {
            eType = EType.Int32;
            int32Value = val;
            isNull = false;
        }
        public void SetUInt32Value(UInt32 val)
        {
            eType = EType.UInt32;
            uint32Value = val;
            isNull = false;
        }
        public void SetInt64Value(Int64 val)
        {
            eType = EType.Int64;
            int64Value = val;
            isNull = false;
        }
        public void SetUInt64Value(UInt64 val)
        {
            eType = EType.UInt64;
            uint64Value = val;
        }
        public void SetFloatValue(Single val)
        {
            eType = EType.Float32;
            floatValue = val;
            isNull = false;
        }
        public void SetDoubleValue(Double val)
        {
            eType = EType.Float64;
            doubleValue = val;
            isNull = false;
        }
        public void SetStringValue(string val)
        {
            eType = EType.String;
            stringValue = val;
            isNull = false;
        }
        public void SetArrayValue( ArrayObject arrobj )
        {
            eType = EType.Array;
            arrayValue = arrobj;
            isNull = false;
        }
        public void ConvertByEType(EType neType )
        {
            object cur = GetValueObject();
            
            switch (neType)
            {
                case EType.Byte:
                    {
                        eType = EType.Byte;
                        int8Value = Convert.ToByte(cur);
                    }
                    break;
                case EType.SByte:
                    {
                        eType = EType.SByte;
                        sint8Value = Convert.ToSByte(cur);
                    }
                    break;
                case EType.Int16:
                    {
                        eType = EType.Int16;
                        int16Value = Convert.ToInt16(cur);
                    }
                    break;
                case EType.UInt16:
                    {
                        eType = EType.Float64;
                        doubleValue = Convert.ToUInt16(cur);
                    }
                    break;
                case EType.Int32:
                    {
                        eType = EType.Int32;
                        int32Value = Convert.ToInt32(cur);
                    }
                    break;
                case EType.UInt32:
                    {
                        eType = EType.UInt32;
                        uint32Value = Convert.ToUInt32(cur);
                    }
                    break;
                case EType.Int64:
                    {
                        eType = EType.Int64;
                        int64Value = Convert.ToInt64(cur);
                    }
                    break;
                case EType.UInt64:
                    {
                        eType = EType.UInt64;
                        uint64Value = Convert.ToUInt64(cur);
                    }
                    break;
                case EType.Float32:
                    {
                        eType = EType.Float32;
                        floatValue = Convert.ToSingle(cur);
                    }
                    break;
                case EType.Float64:
                    {
                        eType = EType.Float64;
                        doubleValue = Convert.ToDouble(cur);
                    }
                    break;
                case EType.String:
                    {
                        eType = EType.String;
                        stringValue = cur.ToString();
                    }
                    break;
                default:
                    {
                        Debug.Write("Error 异常类型在ConvertByEType中");
                    }
                    break;
            }
            isNull = false;
        }
        public Object GetValueObject()
        {
            switch (this.eType)
            {
                case EType.Byte:
                    {
                        return int8Value;
                    }
                case EType.SByte:
                    {
                        return sint8Value;
                    }
                //case EType.Char:
                //    {
                //        return charValue;
                //    }
                case EType.Int16:
                    {
                        return int16Value;
                    }
                case EType.UInt16:
                    {
                        return uint16Value;
                    }
                case EType.Int32:
                    {
                        return int32Value;
                    }
                case EType.UInt32:
                    {
                        return uint32Value;
                    }
                case EType.Int64:
                    {
                        return int64Value;
                    }
                case EType.UInt64:
                    {
                        return uint64Value;
                    }
                case EType.Float32:
                    {
                        return floatValue;
                    }
                case EType.Float64:
                    {
                        return doubleValue;
                    }
                case EType.String:
                    {
                        return stringValue;
                    }
                case EType.Array:
                    {
                        return arrayValue;
                    }
                case EType.Object:
                    {
                        return (sobject as AnyObject).value;
                    }
                default:return sobject;
            }
        }
        public void SetSObject(SObject val)
        {
            isNull = val.isNull;
            if( isNull )
            {
                return;
            }
            switch( val )
            {
                case VoidObject voidobj:
                    {

                    }
                    break;
                case BoolObject boolobj:
                    {
                        eType = EType.Boolean;
                        int8Value = boolobj.value ? (byte)1 : (byte)0;
                    }
                    break;
                case ByteObject int8obj:
                    {
                        eType = EType.Byte;
                        int8Value = int8obj.value;
                    }
                    break;
                case SByteObject sint8obj:
                    {
                        eType = EType.SByte;
                        sint8Value = sint8obj.value;
                    }
                    break;
                //case CharObject charObj:
                //    {
                //        eType = EType.Char;
                //        charValue = charObj.value;
                //    }
                //    break;
                case Int16Object int16obj:
                    {
                        eType = EType.Int16;
                        int16Value = int16obj.value;
                    }
                    break;
                case UInt16Object uint16obj:
                    {
                        eType = EType.UInt16;
                        uint16Value = uint16obj.value;
                    }
                    break;
                case Int32Object int32obj:
                    {
                        eType = EType.Int32;
                        int32Value = int32obj.value;
                    }
                    break;
                case UInt32Object uint32obj:
                    {
                        eType = EType.UInt32;
                        uint32Value = uint32obj.value;
                    }
                    break;
                case Int64Object int64obj:
                    {
                        eType = EType.Int64;
                        int64Value = int64obj.value;
                    }
                    break;
                case UInt64Object uint64obj:
                    {
                        eType = EType.UInt64;
                        uint64Value = uint64obj.value;
                    }
                    break;
                case FloatObject floatobj:
                    {
                        eType = EType.Float32;
                        floatValue = floatobj.value;
                    }
                    break;
                case DoubleObject doubleobj:
                    {
                        eType = EType.Float64;
                        doubleValue = doubleobj.value;
                    }
                    break;
                case StringObject stringobj:
                    {
                        eType = EType.String;
                        stringValue = stringobj.value;
                    }
                    break;
                case ArrayObject arrayobj:
                    {
                        eType = EType.Array;
                        arrayValue = arrayobj;
                    }
                    break;
                case AnyObject anyobj:
                    {
                        eType = anyobj.eType;
                        object tobj = anyobj.value;
                        switch (eType)
                        {
                            case EType.Byte:
                                {
                                    int8Value = (byte)(tobj);
                                }
                                break;
                            case EType.SByte:
                                {
                                    sint8Value = (sbyte)(tobj);
                                }
                                break;
                            //case EType.Char:
                            //    {
                            //        charValue = (char)(tobj);
                            //    }
                            //    break;
                            case EType.Int16:
                                {
                                    int16Value = (short)(tobj);
                                }
                                break;
                            case EType.UInt16:
                                {
                                    uint16Value = (ushort)(tobj);
                                }
                                break;
                            case EType.Int32:
                                {
                                    int32Value = (int)(tobj);
                                }
                                break;
                            case EType.UInt32:
                                {
                                    uint32Value = (uint)(tobj);
                                }
                                break;
                            case EType.Int64:
                                {
                                    int64Value = (long)(tobj);
                                }
                                break;
                            case EType.UInt64:
                                {
                                    uint64Value = (ulong)(tobj);
                                }
                                break;
                            case EType.Float32:
                                {
                                    floatValue = (float)(tobj);
                                }
                                break;
                            case EType.Float64:
                                {
                                    doubleValue = (double)(tobj);
                                }
                                break;
                            case EType.String:
                                {
                                    stringValue = tobj as String;
                                }
                                break;
                            case EType.Class:
                                {
                                    sobject = anyobj.value as ClassObject;
                                }
                                break;
                            case EType.Object:
                                {
                                    sobject = anyobj.value as SObject;
                                }
                                break;
                        }                        
                    }
                    break;
                case TemplateObject templateobj:
                    {
                        if(templateobj.isNull )
                        {
                            this.SetNull();
                            return;
                        }
                        eType = templateobj.eType;
                        object tobj = templateobj.value;
                        switch (eType)
                        {
                            case EType.Byte:
                                {
                                    int8Value = (byte)(tobj);
                                }
                                break;
                            case EType.Boolean:
                                {
                                    int8Value = int.Parse(tobj.ToString()) == 1 ? (byte)1 : (byte)0;
                                }
                                break;
                            case EType.SByte:
                                {
                                    sint8Value = (sbyte)(tobj);
                                }
                                break;
                            //case EType.Char:
                            //    {
                            //        charValue = (char)(tobj);
                            //    }
                            //    break;
                            case EType.Int16:
                                {
                                    int16Value = (short)(tobj);
                                }
                                break;
                            case EType.UInt16:
                                {
                                    uint16Value = (ushort)(tobj);
                                }
                                break;
                            case EType.Int32:
                                {
                                    int32Value = (int)(tobj);
                                }
                                break;
                            case EType.UInt32:
                                {
                                    uint32Value = (uint)(tobj);
                                }
                                break;
                            case EType.Int64:
                                {
                                    int64Value = (long)(tobj);
                                }
                                break;
                            case EType.UInt64:
                                {
                                    uint64Value = (ulong)(tobj);
                                }
                                break;
                            case EType.Float32:
                                {
                                    floatValue = (float)(tobj);
                                }
                                break;
                            case EType.Float64:
                                {
                                    doubleValue = (double)(tobj);
                                }
                                break;
                            case EType.String:
                                {
                                    stringValue = tobj as String;
                                }
                                break;
                            case EType.Class:
                                {
                                    sobject = (tobj as ClassObject).value;
                                }
                                break;
                        }
                    }
                    break;
                default:
                    {
                        eType = EType.Class;
                        sobject = (val as ClassObject).value;
                    }
                    break;
            }
        }
        public System.Object CreateCSharpObject()
        {
            switch (eType)
            {
                case EType.Byte:
                    {
                        return int8Value;
                    }
                case EType.SByte:
                    {
                        return sint8Value;
                    }
                //case EType.Char:
                //    {
                //        return charValue;
                //    }
                case EType.Int16:
                    {
                        return int16Value;
                    }
                case EType.UInt16:
                    {
                        return uint16Value;
                    }
                case EType.Int32:
                    {
                        return int32Value;
                    }
                case EType.UInt32:
                    {
                        return uint32Value;
                    }
                case EType.Int64:
                    {
                        return int64Value;
                    }
                case EType.UInt64:
                    {
                        return uint64Value;
                    }
                case EType.Float32:
                    {
                        return floatValue;
                    }
                case EType.Float64:
                    {
                        return doubleValue;
                    }
                case EType.String:
                    {
                        return stringValue;
                    }
                case EType.Array:
                    {
                        return arrayValue;
                    }
            }
            return sobject;
        }
        public void CreateSObjectByCSharpObject( System.Object obj )
        {
            switch( obj )
            {
                case Byte b:
                    {
                        eType = EType.Byte;
                        int8Value = (Byte)obj;
                    }
                    break;
                case SByte sb:
                    {
                        eType = EType.SByte;
                        sint8Value = (SByte)obj;
                    }
                    break;
                case Char ch:
                    {
                        eType = EType.String;
                        stringValue = (string)obj;
                    }
                   break;
                case Int16 int16:
                    {
                        eType = EType.Int16;
                        int16Value = (short)obj;
                    }
                    break;
                case UInt16 int16:
                    {
                        eType = EType.UInt16;
                        uint16Value = (ushort)obj;

                    }
                    break;
                case Int32 int32:
                    {
                        eType = EType.Int32;
                        int32Value = (int)obj;
                    }
                    break;
                case UInt32 int32:
                    {
                        eType = EType.UInt32;
                        uint32Value = (uint)obj;
                    }
                    break;
                case Int64 int64:
                    {
                        eType = EType.Int64;
                        int64Value = (long)obj;
                    }
                    break;
                case UInt64 uint64:
                    {
                        eType = EType.UInt64;
                        uint64Value = (ulong)obj;
                    }
                    break;
                case Single f:
                    {
                        eType = EType.Float32;
                        floatValue = (float)obj;
                    }
                    break;
                case Double d:
                    {
                        eType = EType.Float64;
                        doubleValue = (double)obj;
                    }
                    break;
                case String str:
                    {
                        eType = EType.String;
                        stringValue = obj.ToString();
                    }
                    break;
                case AnyObject ao:
                    {
                        switch( ao.eType )
                        {
                            case EType.Byte:
                                {
                                    int8Value = (byte)ao.value;
                                    eType = EType.Byte;
                                }
                                break;
                            case EType.SByte:
                                {
                                    sint8Value = (sbyte)ao.value;
                                    eType = EType.SByte;
                                }
                                break;
                            case EType.Int16:
                                {
                                    int16Value = (Int16)ao.value;
                                    eType = EType.Int16;
                                }
                                break;
                            case EType.UInt16:
                                {
                                    uint16Value = (UInt16)ao.value;
                                    eType = EType.UInt16;
                                }
                                break;
                            case EType.Int32:
                                {
                                    int32Value = (int)ao.value;
                                    eType = EType.Int32;
                                }
                                break;
                            case EType.UInt32:
                                {
                                    uint32Value = (UInt32)ao.value;
                                    eType = EType.UInt32;
                                }
                                break;
                            case EType.Int64:
                                {
                                    int64Value = (Int64)ao.value;
                                    eType = EType.Int64;
                                }
                                break;
                            case EType.UInt64:
                                {
                                    uint64Value = (UInt64)ao.value;
                                    eType = EType.UInt64;
                                }
                                break;
                            case EType.Float32:
                                {
                                    floatValue = (Single)ao.value;
                                    eType = EType.UInt32;
                                }
                                break;
                            case EType.Float64:
                                {
                                    doubleValue = (Double)ao.value;
                                    eType = EType.UInt32;
                                }
                                break;
                            case EType.String:
                                {
                                    stringValue = ao.value.ToString();
                                    eType = EType.String;
                                }
                                break;
                            default:
                                {
                                    sobject = ao;
                                    eType = EType.Object;
                                }
                                break;
                        }
                    }
                    break;
                default:
                    {
                        sobject = obj as ClassObject;
                        eType = EType.Class;
                    }
                    break;
            }
        }
        public SObject CreateSObject()
        {
            switch( eType )
            {
                case EType.Byte:
                    {
                        return new ByteObject(int8Value);
                    }
                case EType.SByte:
                    {
                        return new SByteObject(sint8Value);
                    }
                case EType.Boolean:
                    {
                        return new BoolObject(int8Value == 1);
                    }
                //case EType.Char:
                //    {
                //        return new CharObject(charValue);
                //    }
                case EType.Int16:
                    {
                        return new Int16Object(int16Value);
                    }
                case EType.UInt16:
                    {
                        return new UInt16Object(uint16Value);
                    }
                case EType.Int32:
                    {
                        return new Int32Object(int32Value);
                    }
                case EType.UInt32:
                    {
                        return new UInt32Object(uint32Value);
                    }
                case EType.Int64:
                    {
                        return new Int64Object(int64Value);
                    }
                case EType.UInt64:
                    {
                        return new UInt64Object(uint64Value);
                    }
                case EType.String:
                    {
                        return new StringObject(stringValue);
                    }
                default:
                    {
                        return sobject;
                    }
            }
            //return sobject;
        }       
    }
}
