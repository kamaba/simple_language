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

            MetaMemberFunction __Init__ = new MetaMemberFunction(this, "__Init__");
            AddMetaMemberFunction(__Init__);

            MetaMemberFunction GetHashCode = new MetaMemberFunction(this, "GetHashCode");
            GetHashCode.SetDefineMetaClass(CoreMetaClassManager.int32MetaClass);
            AddInnerMetaMemberFunction(GetHashCode);
            MetaMemberFunction GetType = new MetaMemberFunction(this, "GetType");
            GetType.SetDefineMetaClass(this);
            AddInnerMetaMemberFunction(GetType);
            MetaMemberFunction Clone = new MetaMemberFunction(this, "Clone");
            Clone.SetDefineMetaClass( this );
            AddInnerMetaMemberFunction(Clone);
            MetaMemberFunction ToString = new MetaMemberFunction(this, "ToString");
            ToString.SetDefineMetaClass(CoreMetaClassManager.stringMetaClass);
            AddInnerMetaMemberFunction(ToString);
            MetaMemberFunction ToShort = new MetaMemberFunction(this, "ToShort");
            ToShort.SetDefineMetaClass(CoreMetaClassManager.int16MetaClass);
            AddInnerMetaMemberFunction(ToShort);
            MetaMemberFunction ToInt = new MetaMemberFunction(this, "ToInt");
            ToInt.SetDefineMetaClass(CoreMetaClassManager.int32MetaClass);
            AddInnerMetaMemberFunction(ToShort);
            MetaMemberFunction ToLong = new MetaMemberFunction(this, "ToLong");
            ToLong.SetDefineMetaClass(CoreMetaClassManager.int64MetaClass);
            AddInnerMetaMemberFunction(ToLong);
            MetaMemberFunction ToFloat = new MetaMemberFunction(this, "ToFloat");
            ToFloat.SetDefineMetaClass(CoreMetaClassManager.floatMetaClass);
            AddInnerMetaMemberFunction(ToFloat);
            MetaMemberFunction ToDouble = new MetaMemberFunction(this, "ToDouble");
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
