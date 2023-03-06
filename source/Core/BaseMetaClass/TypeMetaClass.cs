//****************************************************************************
//  File:      TypeMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/3/3 12:00:00
//  Description: 
//****************************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.SelfMeta
{
    public class TypeMetaClass : MetaClass
    {
        public TypeMetaClass() : base(DefaultObject.Byte.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Class;
            m_IsInnerDefineCompile = true;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new ByteMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
}
