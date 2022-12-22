//****************************************************************************
//  File:      Int64Object.cs
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
    public class Int64Object : SObject
    {
        public Int64 value;
        public Int64Object( Int64 val )
        {
            value = val;
        }
        public void SetValue(Int64 _val)
        {
            value = _val;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
    public class UInt64Object : SObject
    {
        public UInt64 value;
        public UInt64Object(UInt64 val)
        {
            value = val;
        }
        public void SetValue(UInt64 _val)
        {
            value = _val;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
}
