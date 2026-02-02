//****************************************************************************
//  File:      StringObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.VM
{
    class VoidObject : SObject
    {
        public VoidObject()
        {
            m_RuntimeType = RuntimeTypeManager.voidRuntimeType;
        }
        public void SetValue(String _val)
        {
        }       
        public override string ToFormatString()
        {
            return "void";
        }
    }
}
