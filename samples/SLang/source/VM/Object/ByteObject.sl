//****************************************************************************
//  File:      Int8Object.cs
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
    public class Int8Object : SObject
    {
        public Byte value;

        public Int8Object(Byte _val) : base(EType.Byte)
        {
            value = _val;
        }
        public void SetValue(Byte _val)
        {
            value = _val;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
    public class SInt8Object : SObject
    {
        public SByte value;

        public SInt8Object(SByte _val) : base(EType.SByte)
        {
            value = _val;
        }
        public void SetValue(SByte _val)
        {
            value = _val;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
}
