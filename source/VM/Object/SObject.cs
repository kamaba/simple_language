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
using SimpleLanguage.IR;

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
        public RuntimeType runtimeType => m_RuntimeType;
        public short typeId { get; set; } = 0;
        public int refCount { get; set; } = 0;
        protected RuntimeType m_RuntimeType = null;
        protected SObject()
        {
        }
        public SObject( EType etype )
        {
            this.m_Etype = etype;
        }
        public virtual void SetNull()
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
