//****************************************************************************
//  File:      BoolObject.cs
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
    public class BoolObject : SObject
    {
        public bool value { get; set; } = false;

        public BoolObject( bool flag ) :base(  )
        {
            value = flag;
        }

        public void SetValue(bool _val )
        {
            value = _val;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
}
