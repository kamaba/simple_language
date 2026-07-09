//****************************************************************************
//  File:      SObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using System.Runtime.InteropServices;

namespace SimpleLanguage.VM
{
    // VMObjectHeader is defined in VMRuntime/VMObjectHeader.cs (packed 64-bit)
    public struct VMBooleanObject
    {
        public VMObjectHeader head;
        public byte value;
    }
    public struct VMByteObject
    {
        public VMObjectHeader head;
        public byte value;
    }
    public struct VMSByteObject
    {
        public VMObjectHeader head;
        public sbyte value;
    }
    public struct VMInt16Object
    {
        public VMObjectHeader head;
        public short value;
    }
    public struct VMUInt16Object
    {
        public VMObjectHeader head;
        public ushort value;
    }
    public struct VMInt32Object
    {
        public VMObjectHeader head;
        public int value;
    }
    public struct VMUInt32Object
    {
        public VMObjectHeader head;
        public uint value;
    }
    public struct VMInt64Object
    {
        public VMObjectHeader head;
        public long value;
    }
    public struct VMUInt64Object
    {
        public VMObjectHeader head;
        public ulong value;
    }
    public struct VMFloatObject
    {
        public VMObjectHeader head;
        public float value;
    }
    public struct VMDoubleObject
    {
        public VMObjectHeader head;
        public double value;
    }
    public struct VMStringObject
    {
        public VMObjectHeader head;
        public char[] value;
    }
    public struct VMClassObject
    {
        public VMObjectHeader head;        
    }
    public struct VMArrayObject
    {
        public VMObjectHeader head;
        public VMObjectHeader[] objectArray;
    }
}
