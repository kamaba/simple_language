//****************************************************************************
//  File:      SObject.cs
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
    public class SObject
    {
        public bool isNull => m_IsNull;
        public bool isVoid => m_IsVoid;
        public EType eType => m_Etype;
        protected EType m_Etype = EType.Class;
        public bool m_IsNull = false;
        public bool m_IsVoid = false;
        protected SObject()
        {

        }
        public void SetNull()
        {
            m_IsNull = true;
        }
        public void SetVoid()
        {
            m_IsVoid = true;
        }
        public virtual string ToFormatString()
        {
            return "";
        }
    }
}
