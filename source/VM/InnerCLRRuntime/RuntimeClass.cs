//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Parse;
using System.Collections.Generic;

namespace SimpleLanguage.VM
{
    public class RuntimeClass
    {
        public string runtimeName;
        private List<IRMetaVariable> m_LocalIRMetaVariableList = new List<IRMetaVariable>();
        public RuntimeClass()
        {
        }
        public override string ToString()
        {
            return "";
        }
    }
}
