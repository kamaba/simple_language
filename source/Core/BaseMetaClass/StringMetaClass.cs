//****************************************************************************
//  File:      StringMetaClass.cs
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
    public class StringMetaClass : MetaClass
    {
        public StringMetaClass():base( DefaultObject.String.ToString())
        {
            System.Type type = typeof(System.String);
            
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.String;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new StringMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
}
