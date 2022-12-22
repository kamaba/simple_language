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
        public EType eType;
        public byte int8Value;
        public sbyte sint8Value;
        public char charValue;
        public short int16Value;
        public ushort uint16Value;
        public int int32Value;
        public uint uint32Value;
        public string stringValue;
        public ClassObject sobject;
        public long int64Value;
        public ulong uint64Value;

        public void SetNullValue()
        {
            eType = EType.Null;
        }
        public void SetBoolValue( bool val )
        {
            eType = EType.Boolean;
            int8Value = val ? (byte)1 : (byte)0;
        }
        public void SetInt8Value(byte val)
        {
            eType = EType.Byte;
            int8Value = val;
        }
        public void SetSInt8Value(sbyte val)
        {
            eType = EType.SByte;
            sint8Value = val;
        }
        public void SetCharValue(char val)
        {
            eType = EType.Char;
            charValue = val;
        }
        public void SetInt16Value(Int16 val)
        {
            eType = EType.Int16;
            int16Value = val;
        }
        public void SetUInt16Value(UInt16 val)
        {
            eType = EType.UInt16;
            uint16Value = val;
        }
        public void SetInt32Value(Int32 val)
        {
            eType = EType.Int32;
            int32Value = val;
        }
        public void SetUInt32Value(UInt32 val)
        {
            eType = EType.UInt32;
            uint32Value = val;
        }
        public void SetInt64Value(Int64 val)
        {
            eType = EType.Int64;
            int64Value = val;
        }
        public void SetUInt64Value(UInt64 val)
        {
            eType = EType.UInt64;
            uint64Value = val;
        }
        public void SetStringValue(string val)
        {
            eType = EType.String;
            stringValue = val;
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
                case EType.Char:
                    {
                        return charValue;
                    }
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
                case EType.String:
                    {
                        return stringValue;
                    }
                default:return sobject;
            }
        }
        public void SetSObject(SObject val)
        {
            switch( val )
            {
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
                case CharObject charObj:
                    {
                        eType = EType.Char;
                        charValue = charObj.value;
                    }
                    break;
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
                case StringObject stringobj:
                    {
                        eType = EType.String;
                        stringValue = stringobj.value;
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
                            case EType.Char:
                                {
                                    charValue = (char)(tobj);
                                }
                                break;
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
                            case EType.String:
                                {
                                    stringValue = tobj as String;
                                }
                                break;
                            case EType.Class:
                                {
                                    sobject = tobj as ClassObject;
                                }
                                break;
                        }                        
                    }
                    break;
                default:
                    {
                        eType = EType.Class;
                        sobject = val as ClassObject;
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
                case EType.Char:
                    {
                        return charValue;
                    }
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
                case EType.String:
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
                        eType = EType.Char;
                        charValue = (char)obj;

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
                case String str:
                    {
                        eType = EType.String;
                        stringValue = obj.ToString();
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
                case EType.Char:
                    {
                        return new CharObject(charValue);
                    }
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
