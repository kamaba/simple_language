//****************************************************************************
//  File:      StringObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Core;
using SimpleLanguage.Core.SelfMeta;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.VM
{
    class VoidObject : SObject
    {
        public VoidObject()
        {
            m_IsVoid = true;
        }
        public void SetValue(String _val)
        {
        }
        public static StringObject SetToString( Int32MetaClass mc )
        {
            StringObject s = new StringObject("");

            return s;
        }
        public override string ToFormatString()
        {
            return "void";
        }
    }
}
