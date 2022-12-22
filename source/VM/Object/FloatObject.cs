//****************************************************************************
//  File:      FloatObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Core;

namespace SimpleLanguage.VM
{
    public class FloatObject : SObject
    {
        public Single value;
        FloatObject() 
        { }
        public void SetValue(Single _val)
        {
            value = _val;
        }
        public Int32 ToInt()
        {
            return Int32.Parse( value.ToString() );
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
}
