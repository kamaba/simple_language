//****************************************************************************
//  File:      RealMetaClass.cs
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
    public class FloatMetaClass : MetaClass
    {
        public FloatMetaClass() : base(DefaultObject.Float.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_IsInnerDefineCompile = true;
            MetaConstExpressNode mcen = new MetaConstExpressNode(EType.Float, 0.0f);
            SetDefaultExpressNode(mcen);


        }
        public override void ParseInnerFunction()
        {
            AddCoreFunction();
        }
        public void AddCoreFunction()
        {
            //MetaMemberFunction Cast = new MetaMemberFunction(this, "Cast");
            //Cast.AddMetaDefineParam(new MetaDefineParam("Template", this, null, CoreMetaClassManager.templateMetaClass, null));
            //AddMetaMemberFunction(Cast);
            m_Type = EType.Float;

            MetaMemberFunction ToInt32 = new MetaMemberFunction(this, "ToInt32");
            ToInt32.isOverrideFunction = true;
            AddMetaMemberFunction(ToInt32);
        }
        public MetaClass Cast( MetaTemplate mt)
        {
            if (mt.name == "Int32")
            {

            }
            return null;
        }

        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new FloatMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
    public class DoubleMetaClass : MetaClass
    {
        public DoubleMetaClass() : base(DefaultObject.Double.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_IsInnerDefineCompile = true;
            MetaConstExpressNode mcen = new MetaConstExpressNode(EType.Double, 0.0f);
            SetDefaultExpressNode(mcen);
            m_Type = EType.Double;
        }
        public MetaClass Cast(MetaTemplate mc)
        {
            if (mc.name == "Int32")
            {

            }
            return null;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new DoubleMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
}
