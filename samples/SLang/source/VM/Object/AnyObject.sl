//****************************************************************************
//  File:      AnyObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/28 12:00:00
//  Description:  Any Object can use any type data!
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Core;

namespace SimpleLanguage.VM
{
    public class AnyObject : SObject
    {
        public object value;

        public AnyObject()
        {
        }
        public void SetValue( EType _eType, System.Object val )
        {
            m_Etype = _eType;
            value = val;
            m_IsVoid = false;
        }
        public override string ToFormatString()
        {
            return value.ToString();
        }
    }
}
