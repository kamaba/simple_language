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
using SimpleLanguage.Core;

namespace SimpleLanguage.Core.SelfMeta
{
    public class FloatMetaClass : MetaClass
    {
        public FloatMetaClass() : base(DefaultObject.Float.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_ClassDefineType = EClassDefineType.InnerDefine;
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

            //MetaMemberFunction ToInt32 = new MetaMemberFunction(this, "toInt32");
            //ToInt32.isOverrideFunction = true;
            //AddInnerMetaMemberFunction(ToInt32);


            //MetaMemberFunction ToString = new MetaMemberFunction(this, "toString");
            //ToString.SetMetaDefineType(new MetaType(CoreMetaClassManager.stringMetaClass));
            //AddMetaMemberFunction(ToString);
            //ToString.AddCSharpMetaStatements("SimpleLanguage.VM.Int32Object", "FloatToString");
        }
        public MetaClass Cast( MetaTemplate mt)
        {
            if (mt.name == "Int32")
            {

            }
            return null;
        }
        public static string MetaToString(float v)
        {
            return v.ToString();
        }

        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new FloatMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.selfModule);
            return mc;
        }
    }
    public class DoubleMetaClass : MetaClass
    {
        public DoubleMetaClass() : base(DefaultObject.Double.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_ClassDefineType = EClassDefineType.InnerDefine;
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
        public override void ParseInnerFunction()
        {
            AddCoreFunction();
        }
        public void AddCoreFunction()
        {
            //MetaMemberFunction toFloat = new MetaMemberFunction(this, "toFloat");
            //toFloat.SetMetaDefineType(new MetaType(CoreMetaClassManager.floatMetaClass));
            //AddMetaMemberFunction(toFloat);
            ////toFloat.AddCSharpMetaStatements("SimpleLanguage.VM.DoubleObject", "DoubleToFloat");

            //MetaMemberFunction ToString = new MetaMemberFunction(this, "toString");
            //ToString.SetMetaDefineType(new MetaType(CoreMetaClassManager.stringMetaClass));
            //AddMetaMemberFunction(ToString);
            //ToString.AddCSharpMetaStatements("SimpleLanguage.VM.DoubleObject", "DoubleToString");
        }
        public static string MetaToString( double v )
        {
            return v.ToString();
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new DoubleMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.selfModule);
            return mc;
        }
    }
}
