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
        public StringObject(string str)
        {
            value = str;
        }
        public void SetValue(String _val)
        {
            value = _val;
        }
        public override string ToFormatString()
        {
            return value;
        }
    }
}
