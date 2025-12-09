//****************************************************************************
//  File:      SValue.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using System;
using System.Diagnostics;
namespace SimpleLanguage.VM
{
    public partial struct SValue
    {
        public EVMType eType;
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
        public bool isNull;
        public void SetNullValueType()
        {
            eType = EVMType.Null;
            SetNull();
        }
        public void SetNull()
        {
            isNull = true;
            int8Value = 0;
            sint8Value = 0;
            int16Value = 0;
            uint16Value = 0;
            int32Value = 0;
            uint32Value = 0;
            int64Value = 0;
            uint64Value = 0;
            floatValue = 0;
            doubleValue = 0;
            stringValue = null;
            sobject = null;
        }
        public void SetBoolValue( bool val )
        {
            isNull = false;
            eType = EVMType.Boolean;
            int8Value = val ? (byte)1 : (byte)0;
        }
        public void SetInt8Value(byte val)
        {
            eType = EVMType.Byte;
            int8Value = val;
            isNull = false;
        }
        public void SetSInt8Value(sbyte val)
        {
            eType = EVMType.SByte;
            sint8Value = val;
            isNull = false;
        }
        //public void SetCharValue(char val)
        //{
        //    eType = EVMType.Char;
        //    charValue = val;
        //}
        public void SetInt16Value(Int16 val)
        {
            eType = EVMType.Int16;
            int16Value = val;
            isNull = false;
        }
        public void SetUInt16Value(UInt16 val)
        {
            eType = EVMType.UInt16;
            uint16Value = val;
            isNull = false;
        }
        public void SetInt32Value(Int32 val)
        {
            eType = EVMType.Int32;
            int32Value = val;
            isNull = false;
        }
        public void SetUInt32Value(UInt32 val)
        {
            eType = EVMType.UInt32;
            uint32Value = val;
            isNull = false;
        }
        public void SetInt64Value(Int64 val)
        {
            eType = EVMType.Int64;
            int64Value = val;
            isNull = false;
        }
        public void SetUInt64Value(UInt64 val)
        {
            eType = EVMType.UInt64;
            uint64Value = val;
        }
        public void SetFloatValue(Single val)
        {
            eType = EVMType.Float32;
            floatValue = val;
            isNull = false;
        }
        public void SetDoubleValue(Double val)
        {
            eType = EVMType.Float64;
            doubleValue = val;
            isNull = false;
        }
        public void SetStringValue(string val)
        {
            eType = EVMType.String;
            stringValue = val;
            isNull = false;
        }
        public void SetSObject(SObject val)
        {
            isNull = val.isNull;
            if (isNull)
            {
                return;
            }
            switch (val)
            {
                case VoidObject voidobj:
                    {

                    }
                    break;
                case BoolObject boolobj:
                    {
                        eType = EVMType.Boolean;
                        int8Value = boolobj.value ? (byte)1 : (byte)0;
                    }
                    break;
                case Int8Object int8obj:
                    {
                        eType = EVMType.Byte;
                        int8Value = int8obj.value;
                    }
                    break;
                case SInt8Object sint8obj:
                    {
                        eType = EVMType.SByte;
                        sint8Value = sint8obj.value;
                    }
                    break;
                //case CharObject charObj:
                //    {
                //        eType = EVMType.Char;
                //        charValue = charObj.value;
                //    }
                //    break;
                case Int16Object int16obj:
                    {
                        eType = EVMType.Int16;
                        int16Value = int16obj.value;
                    }
                    break;
                case UInt16Object uint16obj:
                    {
                        eType = EVMType.UInt16;
                        uint16Value = uint16obj.value;
                    }
                    break;
                case Int32Object int32obj:
                    {
                        eType = EVMType.Int32;
                        int32Value = int32obj.value;
                    }
                    break;
                case UInt32Object uint32obj:
                    {
                        eType = EVMType.UInt32;
                        uint32Value = uint32obj.value;
                    }
                    break;
                case Int64Object int64obj:
                    {
                        eType = EVMType.Int64;
                        int64Value = int64obj.value;
                    }
                    break;
                case UInt64Object uint64obj:
                    {
                        eType = EVMType.UInt64;
                        uint64Value = uint64obj.value;
                    }
                    break;
                case Float32Object floatobj:
                    {
                        eType = EVMType.Float32;
                        floatValue = floatobj.value;
                    }
                    break;
                case Float64Object doubleobj:
                    {
                        eType = EVMType.Float64;
                        doubleValue = doubleobj.value;
                    }
                    break;
                case StringObject stringobj:
                    {
                        eType = EVMType.String;
                        stringValue = stringobj.value;
                    }
                    break;
                case ArrayObject arrayobj:
                    {
                        eType = EVMType.Array;
                        sobject = arrayobj;
                    }
                    break;
                    /*
                case AnyObject anyobj:
                    {
                        eType = anyobj.eType;
                        object tobj = anyobj.value;
                        switch (eType)
                        {
                            case EVMType.Byte:
                                {
                                    int8Value = (byte)(tobj);
                                }
                                break;
                            case EVMType.Boolean:
                                {
                                    int8Value = (byte)(tobj);
                                }
                                break;
                            case EVMType.SByte:
                                {
                                    sint8Value = (sbyte)(tobj);
                                }
                                break;
                            //case EVMType.Char:
                            //    {
                            //        charValue = (char)(tobj);
                            //    }
                            //    break;
                            case EVMType.Int16:
                                {
                                    int16Value = (short)(tobj);
                                }
                                break;
                            case EVMType.UInt16:
                                {
                                    uint16Value = (ushort)(tobj);
                                }
                                break;
                            case EVMType.Int32:
                                {
                                    int32Value = (int)(tobj);
                                }
                                break;
                            case EVMType.UInt32:
                                {
                                    uint32Value = (uint)(tobj);
                                }
                                break;
                            case EVMType.Int64:
                                {
                                    int64Value = (long)(tobj);
                                }
                                break;
                            case EVMType.UInt64:
                                {
                                    uint64Value = (ulong)(tobj);
                                }
                                break;
                            case EVMType.Float32:
                                {
                                    floatValue = (float)(tobj);
                                }
                                break;
                            case EVMType.Float64:
                                {
                                    doubleValue = (double)(tobj);
                                }
                                break;
                            case EVMType.String:
                                {
                                    stringValue = tobj as String;
                                }
                                break;
                            case EVMType.Object:
                                {
                                    sobject = tobj as AnyObject;
                                }
                                break;
                            case EVMType.Class:
                                {
                                    sobject = anyobj.value as ClassObject;
                                }
                                break;
                            default:
                                {
                                    sobject = anyobj;
                                }
                                break;
                        }
                    }
                    break;
                    */
                case TemplateObject templateobj:
                    {
                        if (templateobj.isNull)
                        {
                            this.SetNull();
                            return;
                        }
                        eType = templateobj.eType;
                        object tobj = templateobj.value;
                        switch (eType)
                        {
                            case EVMType.Byte:
                                {
                                    int8Value = (byte)(tobj);
                                }
                                break;
                            case EVMType.Boolean:
                                {
                                    int8Value = int.Parse(tobj.ToString()) == 1 ? (byte)1 : (byte)0;
                                }
                                break;
                            case EVMType.SByte:
                                {
                                    sint8Value = (sbyte)(tobj);
                                }
                                break;
                            //case EVMType.Char:
                            //    {
                            //        charValue = (char)(tobj);
                            //    }
                            //    break;
                            case EVMType.Int16:
                                {
                                    int16Value = (short)(tobj);
                                }
                                break;
                            case EVMType.UInt16:
                                {
                                    uint16Value = (ushort)(tobj);
                                }
                                break;
                            case EVMType.Int32:
                                {
                                    int32Value = (int)(tobj);
                                }
                                break;
                            case EVMType.UInt32:
                                {
                                    uint32Value = (uint)(tobj);
                                }
                                break;
                            case EVMType.Int64:
                                {
                                    int64Value = (long)(tobj);
                                }
                                break;
                            case EVMType.UInt64:
                                {
                                    uint64Value = (ulong)(tobj);
                                }
                                break;
                            case EVMType.Float32:
                                {
                                    floatValue = (float)(tobj);
                                }
                                break;
                            case EVMType.Float64:
                                {
                                    doubleValue = (double)(tobj);
                                }
                                break;
                            case EVMType.String:
                                {
                                    stringValue = tobj as String;
                                }
                                break;
                            case EVMType.Class:
                                {
                                    sobject = (tobj as ClassObject).value;
                                }
                                break;
                        }
                    }
                    break;
                default:
                    {
                        eType = EVMType.Class;
                        sobject = val;
                    }
                    break;
            }
        }
        public void ConvertByEType(EVMType neType )
        {
            object cur = GetValueObject();
            
            switch (neType)
            {
                case EVMType.Boolean:
                    {
                        eType = EVMType.Boolean;
                        int8Value = Convert.ToByte(cur);
                    }
                    break;
                case EVMType.Byte:
                    {
                        eType = EVMType.Byte;
                        int8Value = Convert.ToByte(cur);
                    }
                    break;
                case EVMType.SByte:
                    {
                        eType = EVMType.SByte;
                        sint8Value = Convert.ToSByte(cur);
                    }
                    break;
                case EVMType.Int16:
                    {
                        eType = EVMType.Int16;
                        int16Value = Convert.ToInt16(cur);
                    }
                    break;
                case EVMType.UInt16:
                    {
                        eType = EVMType.Float64;
                        doubleValue = Convert.ToUInt16(cur);
                    }
                    break;
                case EVMType.Int32:
                    {
                        eType = EVMType.Int32;
                        int32Value = Convert.ToInt32(cur);
                    }
                    break;
                case EVMType.UInt32:
                    {
                        eType = EVMType.UInt32;
                        uint32Value = Convert.ToUInt32(cur);
                    }
                    break;
                case EVMType.Int64:
                    {
                        eType = EVMType.Int64;
                        int64Value = Convert.ToInt64(cur);
                    }
                    break;
                case EVMType.UInt64:
                    {
                        eType = EVMType.UInt64;
                        uint64Value = Convert.ToUInt64(cur);
                    }
                    break;
                case EVMType.Float32:
                    {
                        eType = EVMType.Float32;
                        floatValue = Convert.ToSingle(cur);
                    }
                    break;
                case EVMType.Float64:
                    {
                        eType = EVMType.Float64;
                        doubleValue = Convert.ToDouble(cur);
                    }
                    break;
                case EVMType.String:
                    {
                        eType = EVMType.String;
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
                case EVMType.Byte:
                    {
                        return int8Value;
                    }
                case EVMType.SByte:
                    {
                        return sint8Value;
                    }
                //case EVMType.Char:
                //    {
                //        return charValue;
                //    }
                case EVMType.Int16:
                    {
                        return int16Value;
                    }
                case EVMType.UInt16:
                    {
                        return uint16Value;
                    }
                case EVMType.Int32:
                    {
                        return int32Value;
                    }
                case EVMType.UInt32:
                    {
                        return uint32Value;
                    }
                case EVMType.Int64:
                    {
                        return int64Value;
                    }
                case EVMType.UInt64:
                    {
                        return uint64Value;
                    }
                case EVMType.Float32:
                    {
                        return floatValue;
                    }
                case EVMType.Float64:
                    {
                        return doubleValue;
                    }
                case EVMType.String:
                    {
                        return stringValue;
                    }
                case EVMType.Array:
                    {
                        return (sobject as ArrayObject);
                    }
                //case EVMType.Object:
                //    {
                //        return (sobject as AnyObject).value;
                //    }
                default:return sobject;
            }
        }
        public System.Object CreateCSharpObject()
        {
            switch (eType)
            {
                case EVMType.Boolean:
                    {
                        return int8Value == 1;
                    }
                case EVMType.Byte:
                    {
                        return int8Value;
                    }
                case EVMType.SByte:
                    {
                        return sint8Value;
                    }
                //case EVMType.Char:
                //    {
                //        return charValue;
                //    }
                case EVMType.Int16:
                    {
                        return int16Value;
                    }
                case EVMType.UInt16:
                    {
                        return uint16Value;
                    }
                case EVMType.Int32:
                    {
                        return int32Value;
                    }
                case EVMType.UInt32:
                    {
                        return uint32Value;
                    }
                case EVMType.Int64:
                    {
                        return int64Value;
                    }
                case EVMType.UInt64:
                    {
                        return uint64Value;
                    }
                case EVMType.Float32:
                    {
                        return floatValue;
                    }
                case EVMType.Float64:
                    {
                        return doubleValue;
                    }
                case EVMType.String:
                    {
                        return stringValue;
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
                        eType = EVMType.Byte;
                        int8Value = (Byte)obj;
                    }
                    break;
                case SByte sb:
                    {
                        eType = EVMType.SByte;
                        sint8Value = (SByte)obj;
                    }
                    break;
                case Char ch:
                    {
                        eType = EVMType.String;
                        stringValue = (string)obj;
                    }
                   break;
                case Int16 int16:
                    {
                        eType = EVMType.Int16;
                        int16Value = (short)obj;
                    }
                    break;
                case UInt16 int16:
                    {
                        eType = EVMType.UInt16;
                        uint16Value = (ushort)obj;

                    }
                    break;
                case Int32 int32:
                    {
                        eType = EVMType.Int32;
                        int32Value = (int)obj;
                    }
                    break;
                case Int32Object int32Obj:
                    {
                        eType = EVMType.Int32;
                        int32Value = int32Obj.value;
                    }
                    break;
                case UInt32 int32:
                    {
                        eType = EVMType.UInt32;
                        uint32Value = (uint)obj;
                    }
                    break;
                case Int64 int64:
                    {
                        eType = EVMType.Int64;
                        int64Value = (long)obj;
                    }
                    break;
                case UInt64 uint64:
                    {
                        eType = EVMType.UInt64;
                        uint64Value = (ulong)obj;
                    }
                    break;
                case Single f:
                    {
                        eType = EVMType.Float32;
                        floatValue = (float)obj;
                    }
                    break;
                case Double d:
                    {
                        eType = EVMType.Float64;
                        doubleValue = (double)obj;
                    }
                    break;
                case String str:
                    {
                        eType = EVMType.String;
                        stringValue = obj.ToString();
                    }
                    break;
                    /*
                case AnyObject ao:
                    {
                        switch( ao.eType )
                        {
                            case EVMType.Byte:
                                {
                                    int8Value = (byte)ao.value;
                                    eType = EVMType.Byte;
                                }
                                break;
                            case EVMType.SByte:
                                {
                                    sint8Value = (sbyte)ao.value;
                                    eType = EVMType.SByte;
                                }
                                break;
                            case EVMType.Int16:
                                {
                                    int16Value = (Int16)ao.value;
                                    eType = EVMType.Int16;
                                }
                                break;
                            case EVMType.UInt16:
                                {
                                    uint16Value = (UInt16)ao.value;
                                    eType = EVMType.UInt16;
                                }
                                break;
                            case EVMType.Int32:
                                {
                                    int32Value = (int)ao.value;
                                    eType = EVMType.Int32;
                                }
                                break;
                            case EVMType.UInt32:
                                {
                                    uint32Value = (UInt32)ao.value;
                                    eType = EVMType.UInt32;
                                }
                                break;
                            case EVMType.Int64:
                                {
                                    int64Value = (Int64)ao.value;
                                    eType = EVMType.Int64;
                                }
                                break;
                            case EVMType.UInt64:
                                {
                                    uint64Value = (UInt64)ao.value;
                                    eType = EVMType.UInt64;
                                }
                                break;
                            case EVMType.Float32:
                                {
                                    floatValue = (Single)ao.value;
                                    eType = EVMType.UInt32;
                                }
                                break;
                            case EVMType.Float64:
                                {
                                    doubleValue = (Double)ao.value;
                                    eType = EVMType.UInt32;
                                }
                                break;
                            case EVMType.String:
                                {
                                    stringValue = ao.value.ToString();
                                    eType = EVMType.String;
                                }
                                break;
                            default:
                                {
                                    sobject = ao;
                                    isNull = sobject.isNull;
                                    eType = EVMType.Class;
                                }
                                break;
                        }
                    }
                    break;
                    */
                default:
                    {
                        sobject = obj as SObject;
                        eType = EVMType.Class;
                        isNull = sobject.isNull;
                    }
                    break;
            }
        }
        public SObject CreateSObject()
        {
            switch( eType )
            {
                case EVMType.Byte:
                    {
                        return new Int8Object(int8Value);
                    }
                case EVMType.Boolean:
                    {
                        return new BoolObject(int8Value == 1);
                    }
                case EVMType.SByte:
                    {
                        return new SInt8Object(sint8Value);
                    }
                //case EVMType.Char:
                //    {
                //        return new CharObject(charValue);
                //    }
                case EVMType.Int16:
                    {
                        return new Int16Object(int16Value);
                    }
                case EVMType.UInt16:
                    {
                        return new UInt16Object(uint16Value);
                    }
                case EVMType.Int32:
                    {
                        return new Int32Object(int32Value);
                    }
                case EVMType.UInt32:
                    {
                        return new UInt32Object(uint32Value);
                    }
                case EVMType.Int64:
                    {
                        return new Int64Object(int64Value);
                    }
                case EVMType.UInt64:
                    {
                        return new UInt64Object(uint64Value);
                    }
                case EVMType.String:
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
