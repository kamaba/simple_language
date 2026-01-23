//****************************************************************************
//  File:      StringObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Core;
using SimpleLanguage.VM.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.VM
{
    class StringObject : SObject
    {
        public new string value { get { return (string)m_Value; } }

        public StringObject(string str) : base(EVMType.String )
        {
            m_Value = str;
            m_RuntimeType = RuntimeTypeManager.stringRuntimeType;
        }
        public void SetValue(String _val)
        {
            m_Value = _val;
            m_IsNull = false;
        }
        public static StringObject SetToString( Int32MetaClass mc )
        {
            StringObject s = new StringObject("");

            return s;
        }
        public override string ToFormatString()
        {
            return value;
        }
    }
}
