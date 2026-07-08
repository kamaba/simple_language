//****************************************************************************
//  File:      StringObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.VM
{
    class VoidObject : SObject
    {
        public VoidObject()
        {
            m_RuntimeType = RuntimeTypeManager.voidRuntimeType;
        }
        public override string ToFormatString()
        {
            return "void";
        }
    }
}
