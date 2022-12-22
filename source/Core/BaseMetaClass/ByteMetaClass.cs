//****************************************************************************
//  File:      ByteMetaClass.cs
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
    public class ByteMetaClass : MetaClass
    {
        public ByteMetaClass() : base(DefaultObject.Byte.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Byte;
            //System.Type type = typeof(System.Char);
            //MetaConstExpressNode mcen = new MetaConstExpressNode(EType.Char, '0');
            //mc.SetDefaultExpressNode(mcen);

            //MetaMemberVariable mmv = new MetaMemberVariable( mc, "value");
            ////mmv.metaType = new MetaType() { defineType = EType.Char };

            //mc.AddMetaMemberVariable(mmv);
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new ByteMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
    public class SByteMetaClass : MetaClass
    {
        public SByteMetaClass() : base(DefaultObject.SByte.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.SByte;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new SByteMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
}
