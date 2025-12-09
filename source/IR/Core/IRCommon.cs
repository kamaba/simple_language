//****************************************************************************
//  File:      IRMetaType.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/9/5 12:00:00
//  Description: Meta class's ir attribute
//****************************************************************************

using SimpleLanguage.VM;

namespace SimpleLanguage.IR
{
    public enum EArrayType
    {
        None,
        Boolean,
        Byte,
        SByte,
        Int16,
        Int32,
        Int64,
        UInt16,
        UInt32,
        UInt64,
        Single,
        Double,
        String,
        Array,
        Any,
        Class,
    }
    public class IRNewArray
    {
        public EArrayType eArrayType;
        public IRMetaType irMetaType;
        public int length;
    }
}
