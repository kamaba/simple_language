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
        public new string value => m_Numeric.str;
        public StringObject(string str) : base(EVMType.String)
        {
            m_RuntimeType = RuntimeTypeManager.stringRuntimeType;
            m_Numeric.str = str;

        }
        public void SetValue(string _val)
        {
            m_Numeric.str = _val;
        }
        public override string ToFormatString()
        {
            return m_Numeric.str;
        }
    }
}
