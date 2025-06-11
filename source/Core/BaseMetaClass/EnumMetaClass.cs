//****************************************************************************
//  File:      EnumMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core.SelfMeta
{
    public class EnumMetaClass : MetaClass
    {
        public EnumMetaClass() : base(DefaultObject.Enum.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Enum;
            m_ClassDefineType = EClassDefineType.InnerDefine;
        }
        public override void ParseInnerFunction()
        {
            AddCoreFunction();
        }
        public void AddCoreFunction()
        {
            MetaMemberFunction values = new MetaMemberFunction(this, "_values_");
            values.isGet = true;
            values.SetMetaDefineType(new MetaType( CoreMetaClassManager.listMetaClass ) );
            AddMetaMemberFunction(values);
        }
        public static MetaClass CreateMetaClass()
        {
            EnumMetaClass mc = new EnumMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
}
