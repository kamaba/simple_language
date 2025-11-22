//****************************************************************************
//  File:      StringObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Core;

using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.VM
{
    class StringObject : SObject
    {
        public string value;

        public StringObject() : base(EType.String)
        {
            m_RuntimeType = RuntimeTypeManager.stringRuntimeType;
        }

        public StringObject(string str) : base( EType.String )
        {
            value = str;
            m_RuntimeType = RuntimeTypeManager.stringRuntimeType;
        }
        public void SetValue(String _val)
        {
            value = _val;
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
