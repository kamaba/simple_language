//****************************************************************************
//  File:      CharMetaClass.cs
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
    public class CharMetaClass : MetaClass
    {
        public CharMetaClass():base( DefaultObject.Char.ToString() )
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_IsInnerDefineCompile = true;
            m_Type = EType.Char;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new CharMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
}
