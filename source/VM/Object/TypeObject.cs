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
    class TypeObject : ClassObject
    {
        RuntimeType m_Rt = null;
        public TypeObject(RuntimeType rm ) : base(RuntimeTypeManager.typeRuntimeType)
        {
            m_Rt = rm;
            m_Type = EVMType.Type;
            CreateObject();
        }
        public override string ToFormatString()
        {
            return "";
        }
    }
}
