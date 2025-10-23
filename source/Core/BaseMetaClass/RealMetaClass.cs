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

namespace SimpleLanguage.Core
{
    public class Float32MetaClass : MetaClass
    {
        public Float32MetaClass() : base(DefaultObject.Float32.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_ClassDefineType = EClassDefineType.InnerDefine;
            MetaConstExpressNode mcen = new MetaConstExpressNode(EType.Float32, 0.0f);
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
            m_Type = EType.Float32;

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
            MetaClass mc = new Float32MetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
    public class Float64MetaClass : MetaClass
    {
        public Float64MetaClass() : base(DefaultObject.Float64.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_ClassDefineType = EClassDefineType.InnerDefine;
            MetaConstExpressNode mcen = new MetaConstExpressNode(EType.Float64, 0.0f);
            SetDefaultExpressNode(mcen);
            m_Type = EType.Float64;
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
            MetaClass mc = new Float64MetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule );
            return mc;
        }
    }
}
