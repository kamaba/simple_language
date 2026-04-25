//****************************************************************************
//  File:      StringObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.Runtime;

namespace SimpleLanguage.VM
{
    public class StringObject : SObject
    {
        public new string? value => m_Reference as string;

        public StringObject(string str) : base(EVMType.String)
        {
            m_Reference = str;
            m_RuntimeType = RuntimeTypeManager.stringRuntimeType;
        }
        public void SetValue(string _val)
        {
            m_Reference = _val;
        }
        public override string ToFormatString()
        {
            return value ?? "";
        }
    }
}
