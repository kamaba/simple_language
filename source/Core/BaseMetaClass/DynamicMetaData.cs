//****************************************************************************
//  File:      DynamicMetaData.cs
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
    public class DynamicMetaData : MetaClass
    {
        public DynamicMetaData():base(DefaultObject.Data.ToString())
        {
            m_ClassDefineType = EClassDefineType.InnerDefine;
        }        
        public MetaClass Cast( MetaTemplate mc )
        {
            return null;
        }
        public override void ParseInnerFunction()
        {
            AddCoreFunction();
        }
        public void AddCoreFunction()
        {
            MetaMemberFunction Cast = new MetaMemberFunction(this, "cast");
            //Cast.AddMetaDefineParam(new MetaDefineParam("t", this, null, CoreMetaClassManager.templateMetaClass, null));
            //Cast.SetDefineMetaClass(CoreMetaClassManager.int32MetaClass);
            //AddMetaMemberFunction(Cast);

            //MetaMemberVariable mmvobjectid = new MetaMemberVariable(this, "objectid", CoreMetaClassManager.int32MetaClass);
            //AddMetaMemberVariable(mmvobjectid);

            //MetaMemberVariable mmvname = new MetaMemberVariable(this, "name", CoreMetaClassManager.stringMetaClass);
            //AddMetaMemberVariable(mmvname);

            MetaMemberFunction _init_ = new MetaMemberFunction(this, "_init_");
            AddMetaMemberFunction(_init_);

            MetaMemberFunction GetHashCode = new MetaMemberFunction(this, "GetHashCode");
            GetHashCode.SetReturnMetaClass(CoreMetaClassManager.int32MetaClass);
            AddInnerMetaMemberFunction(GetHashCode);
            MetaMemberFunction GetType = new MetaMemberFunction(this, "GetType");
            GetType.SetReturnMetaClass(this);
            AddInnerMetaMemberFunction(GetType);
            MetaMemberFunction Clone = new MetaMemberFunction(this, "Clone");
            Clone.SetReturnMetaClass( this );
            AddInnerMetaMemberFunction(Clone);
            MetaMemberFunction ToString = new MetaMemberFunction(this, "toString");
            ToString.SetReturnMetaClass(CoreMetaClassManager.stringMetaClass);
            AddInnerMetaMemberFunction(ToString);
            MetaMemberFunction ToShort = new MetaMemberFunction(this, "toShort");
            ToShort.SetReturnMetaClass(CoreMetaClassManager.int16MetaClass);
            AddInnerMetaMemberFunction(ToShort);
            MetaMemberFunction ToInt = new MetaMemberFunction(this, "toInt");
            ToInt.SetReturnMetaClass(CoreMetaClassManager.int32MetaClass);
            AddInnerMetaMemberFunction(ToShort);
            MetaMemberFunction ToLong = new MetaMemberFunction(this, "toLong");
            ToLong.SetReturnMetaClass(CoreMetaClassManager.int64MetaClass);
            AddInnerMetaMemberFunction(ToLong);
            MetaMemberFunction ToFloat = new MetaMemberFunction(this, "toFloat32");
            ToFloat.SetReturnMetaClass(CoreMetaClassManager.float32MetaClass);
            AddInnerMetaMemberFunction(ToFloat);
            MetaMemberFunction ToDouble = new MetaMemberFunction(this, "toFloat64");
            ToDouble.SetReturnMetaClass(CoreMetaClassManager.float64MetaClass);
            AddInnerMetaMemberFunction(ToLong);
        }

        public static MetaClass CreateMetaClass()
        {
            DynamicMetaData mc = new DynamicMetaData();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.selfModule);
            return mc;
        }
    }
}
