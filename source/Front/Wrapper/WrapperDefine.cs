//****************************************************************************
//  File:      WrapperDefine.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/21 12:00:00
//  Description: Wrapper payload type definition
//****************************************************************************

namespace SimpleLanguage.Wrapper
{
    public enum EWrapperPayloadType : byte
    {
        None = 0,
        Int32 = 1,
        Int64 = 2,
        Float32 = 3,
        Float64 = 4,
        String = 5,
        Boolean = 6,
        Byte = 7,
        SByte = 8,
        Int16 = 9,
        UInt16 = 10,
        UInt32 = 11,
        UInt64 = 12
    }

    public enum EWrapperMetaType : byte
    {
        None = 0,
        MetaClass,
        MetaMethod,
        MetaVariable,
        MetaType,
        MetaNamespace,
    }
}
