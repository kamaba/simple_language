//****************************************************************************
//  File:      TemplateMetaClass.cs
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
    public class TemplateMetaClass : MetaClass
    {
        public TemplateMetaClass( string _name ):base( _name )
        {
            m_Name = _name;
            ParseInit();
        }
        private void ParseInit()
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            HandleExtendClassVariable();
            MetaType mdt = new MetaType(this);
            m_DefaultExpressNode = new MetaNewObjectExpressNode(mdt, this, null, null);
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new TemplateMetaClass("Template");
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
}
