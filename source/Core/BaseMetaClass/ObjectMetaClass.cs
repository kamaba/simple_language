//****************************************************************************
//  File:      ObjectMetaClass.cs
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
    public class ObjectMetaClass : MetaClass
    {
        public ObjectMetaClass():base(DefaultObject.Object.ToString())
        {
            m_ClassDefineType = EClassDefineType.InnerDefine;
        }        
        public MetaClass Cast( MetaTemplate mc )
        {
            return null;
        }
        public virtual int ToInt32()
        {
            return 0;
        }
        public override void ParseInnerFunction()
        {
            AddCoreFunction();
        }
        public void AddCoreFunction()
        {
            MetaMemberFunction Cast = new MetaMemberFunction(this, "Cast");
            //Cast.AddMetaDefineParam(new MetaDefineParam("t", this, null, CoreMetaClassManager.templateMetaClass, null));
            //Cast.SetDefineMetaClass(CoreMetaClassManager.int32MetaClass);
            //AddMetaMemberFunction(Cast);

            //MetaMemberVariable mmvobjectid = new MetaMemberVariable(this, "objectid", CoreMetaClassManager.int32MetaClass);
            //AddMetaMemberVariable(mmvobjectid);

            //MetaMemberVariable mmvname = new MetaMemberVariable(this, "name", CoreMetaClassManager.stringMetaClass);
            //AddMetaMemberVariable(mmvname);

            MetaMemberFunction _init_ = new MetaMemberFunction(this, "_init_");
            AddMetaMemberFunction(_init_);

            MetaMemberFunction GetHashCode = new MetaMemberFunction(this, "hashCode");
            GetHashCode.SetDefineMetaClass(CoreMetaClassManager.int32MetaClass);
            AddInnerMetaMemberFunction(GetHashCode);
            MetaMemberFunction GetType = new MetaMemberFunction(this, "getType");
            GetType.SetDefineMetaClass(this);
            AddInnerMetaMemberFunction(GetType);
            MetaMemberFunction Clone = new MetaMemberFunction(this, "clone");
            Clone.SetDefineMetaClass( this );
            AddInnerMetaMemberFunction(Clone);
            MetaMemberFunction ToString = new MetaMemberFunction(this, "ToString");
            ToString.SetDefineMetaClass(CoreMetaClassManager.stringMetaClass);
            AddInnerMetaMemberFunction(ToString);
            MetaMemberFunction ToShort = new MetaMemberFunction(this, "toShort");
            ToShort.SetDefineMetaClass(CoreMetaClassManager.int16MetaClass);
            AddInnerMetaMemberFunction(ToShort);
            MetaMemberFunction ToInt = new MetaMemberFunction(this, "toInt");
            ToInt.SetDefineMetaClass(CoreMetaClassManager.int32MetaClass);
            AddInnerMetaMemberFunction(ToShort);
            MetaMemberFunction ToLong = new MetaMemberFunction(this, "toLong");
            ToLong.SetDefineMetaClass(CoreMetaClassManager.int64MetaClass);
            AddInnerMetaMemberFunction(ToLong);
            MetaMemberFunction ToFloat = new MetaMemberFunction(this, "toFloat");
            ToFloat.SetDefineMetaClass(CoreMetaClassManager.floatMetaClass);
            AddInnerMetaMemberFunction(ToFloat);
            MetaMemberFunction ToDouble = new MetaMemberFunction(this, "toDouble");
            ToDouble.SetDefineMetaClass(CoreMetaClassManager.doubleMetaClass);
            AddInnerMetaMemberFunction(ToLong);
        }

        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new ObjectMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
}
