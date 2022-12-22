//****************************************************************************
//  File:      DoubleObject.cs
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
    class DoubleObject : SObject
    {
        public Double value;
        public DoubleObject() 
        { }
        public void SetValue(Double _val)
        {
            value = _val;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
}
