//****************************************************************************
//  File:      StringObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Core;
using SimpleLanguage.VM.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.VM
{
    class TypeObject : SObject
    {
        public override object getValue() { return value; }

        public TypeObject(RuntimeType rm ) : base(EVMType.Type )
        {
            value = rm;
            m_RuntimeType = RuntimeTypeManager.typeRuntimeType;
        }
        public void SetValue(RuntimeType _val)
        {
            value = _val;
            m_IsNull = false;
        }
        public override string ToFormatString()
        {
            return "";
        }
    }
}
