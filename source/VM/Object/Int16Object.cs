//****************************************************************************
//  File:      Int16Object.cs
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
    class Int16Object : SObject
    {
        public Int16 value;
        public Int16Object(Int16 val)
        {
            value = val;
        }
        public void SetValue( Int16 _val )
        {
            value = _val;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
    public class UInt16Object : SObject 
    {
        public UInt16 value;
        public UInt16Object(UInt16 val)
        {
            value = val;
        }
        public void SetValue(UInt16 _val)
        {
            value = _val;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
}
