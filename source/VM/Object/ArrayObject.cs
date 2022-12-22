//****************************************************************************
//  File:      ArrayObject.cs
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
    public class ArrayObject<T> : SObject
    {
        public Byte value;

        public Array m_Array = null;

        public ArrayObject() 
        { }
        public void SetValue( Byte _val )
        {
            value = _val;
        }
        public override string ToFormatString()
        {
            return "";
        }
    }
}
