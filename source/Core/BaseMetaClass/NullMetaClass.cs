//****************************************************************************
//  File:      NullMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.SelfMeta
{
    public class NullMetaClass : MetaClass
    {
        public NullMetaClass():base(DefaultObject.Null.ToString())
        {
            m_ClassDefineType = EClassDefineType.InnerDefine;
        }        
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new NullMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.selfModule);
            return mc;
        }
    }
}
