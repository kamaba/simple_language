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
        private string m_Value;
        public new string value => m_Value;
        public StringObject(string str) : base(EVMType.String)
        {
            m_RuntimeType = RuntimeTypeManager.stringRuntimeType;
            m_Value = str;

        }
        public void SetValue(string _val)
        {
            m_Value = _val;
        }
        public override string ToFormatString()
        {
            return m_Value;
        }
    }
}
