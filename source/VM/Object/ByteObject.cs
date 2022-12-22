//****************************************************************************
//  File:      ByteObject.cs
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
    public class ByteObject : SObject
    {
        public Byte value;

        public ByteObject(Byte _val)
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
    public class SByteObject : SObject
    {
        public SByte value;

        public SByteObject(SByte _val)
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
