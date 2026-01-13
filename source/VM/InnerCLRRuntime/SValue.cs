//****************************************************************************
//  File:      SValue.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Logging;
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


            Log.AddVM(EError.None, "SetStringValue" + val );
        }

        public void SetValue(SObject val)
        {
            eType = val.eAnyType;
            SetTypeValue(eType, val.value);
        }
        public void SetTypeValue( EVMType etype, object tobj )
        {
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
                default:
                    {
                        Debug.Assert(false);
                    }
                    break;
            }
        }
        public void SetSObject(SObject val)
        {
            if( val == null )
            {
                isNull = true;
                return;
            }
            isNull = val.isNull;
            if (isNull)
            {
                return;
            }
            isNull = false;
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
                case TemplateObject templateobj:
                    {
                        if (templateobj.isNull)
                        {
                            this.SetNull();
                            return;
                        }
                        eType = templateobj.eType;
                        SetTypeValue(eType, templateobj.value);
                    }
                    break;
                default:
                    {
                        if (val.eType == EVMType.Object)
                        {
                            eType = EVMType.Object;
                            sobject = val;
                        }
                        else if (val.eType == EVMType.Array
                            || val.eType == EVMType.Class )
                        {
                            eType = EVMType.Class;
                            sobject = val;
                        }
                        else
                        {
                            SetSObject(val.value as SObject);
                        }
                    }
                    break;
            }
        }
        public SObject GetSObject()
        {        
            if (isNull)
            {
                return null;
            }
            SObject sobj = null;
            switch (eType)
            {
                //case EVMType.RawBoolean:
                case EVMType.Boolean:
                    {
                        sobj = new BoolObject(this.int8Value == 1);
                    }
                    break;
                //case EVMType.RawByte:
                case EVMType.Byte:
                    {
                        sobj = new Int8Object(this.int8Value );
                    }
                    break;
                //case EVMType.RawSByte:
                case EVMType.SByte:
                    {
                        sobj = new SInt8Object(this.sint8Value);
                    }
                    break;
                //case EVMType.RawInt16:
                case EVMType.Int16:
                    {
                        sobj = new Int16Object(this.int16Value);
                    }
                    break;
                //case EVMType.RawUInt16:
                case EVMType.UInt16:
                    {
                        sobj = new UInt16Object(this.uint16Value);
                    }
                    break;
                //case EVMType.RawInt32:
                case EVMType.Int32:
                    {
                        sobj = new Int32Object(this.int32Value);
                    }
                    break;
                //case EVMType.RawUInt32:
                case EVMType.UInt32:
                    {
                        sobj = new UInt32Object(this.uint32Value);
                    }
                    break;
                //case EVMType.RawInt64:
                case EVMType.Int64:
                    {
                        sobj = new Int64Object(this.int64Value);
                    }
                    break;
                //case EVMType.RawUInt64:
                case EVMType.UInt64:
                    {
                        sobj = new UInt64Object(this.uint64Value );
                    }
                    break;
                //case EVMType.RawFloat32:
                case EVMType.Float32:
                    {
                        sobj = new Float32Object(this.floatValue);
                    }
                    break;
                //case EVMType.RawFloat64:
                case EVMType.Float64:
                    {
                        sobj = new Float64Object(this.doubleValue);
                    }
                    break;
                //case EVMType.RawString:
                case EVMType.String:
                    {
                        sobj = new StringObject(this.stringValue);
                    }
                    break;
                default:
                    {
                        sobj = this.sobject;
                        Debug.Assert(sobj != null);
                    }
                    break;
            }

            return sobj;
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
                //case EVMType.RawByte:
                    {
                        return int8Value;
                    }
                case EVMType.SByte:
                //case EVMType.RawSByte:
                    {
                        return sint8Value;
                    }
                //case EVMType.Char:
                //    {
                //        return charValue;
                //    }
                case EVMType.Int16:
                //case EVMType.RawInt16:
                    {
                        return int16Value;
                    }
                case EVMType.UInt16:
                //case EVMType.RawUInt16:
                    {
                        return uint16Value;
                    }
                case EVMType.Int32:
                //case EVMType.RawInt32:
                    {
                        return int32Value;
                    }
                case EVMType.UInt32:
                //case EVMType.RawUInt32:
                    {
                        return uint32Value;
                    }
                case EVMType.Int64:
                //case EVMType.RawInt64:
                    {
                        return int64Value;
                    }
                case EVMType.UInt64:
                //case EVMType.RawUInt64:
                    {
                        return uint64Value;
                    }
                case EVMType.Float32:
                //case EVMType.RawFloat32:
                    {
                        return floatValue;
                    }
                case EVMType.Float64:
                //case EVMType.RawFloat64:
                    {
                        return doubleValue;
                    }
                case EVMType.String:
                //case EVMType.RawString:
                    {
                        return stringValue;
                    }
                default:return sobject;
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
