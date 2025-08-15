//****************************************************************************
//  File:      VoidMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Core;

namespace SimpleLanguage.Core.SelfMeta
{
    public class VoidMetaClass : MetaClass
    {
        public VoidMetaClass():base( DefaultObject.Void.ToString() )
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass );
            m_ClassDefineType = EClassDefineType.InnerDefine;
            MetaConstExpressNode mcen = new MetaConstExpressNode(EType.Null, "null");
            SetDefaultExpressNode(mcen);
            m_Type = EType.Void;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new VoidMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.selfModule);
            return mc;

        }
    }
}
