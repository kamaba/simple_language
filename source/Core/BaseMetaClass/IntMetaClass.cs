//****************************************************************************
//  File:      IntMetaClass.cs
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
    public class Int16MetaClass : MetaClass
    {
        public Int16MetaClass() : base(DefaultObject.Int16.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Int16;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Int16MetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
    public class UInt16MetaClass : MetaClass
    {
        public UInt16MetaClass() : base(DefaultObject.UInt16.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.UInt16;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new UInt16MetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
    public class Int32MetaClass : MetaClass
    {
        public Int32MetaClass() : base(DefaultObject.Int32.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Int32;
        }

        public override void ParseInnerFunction()
        {
            AddCoreFunction();
        }
        public void AddCoreFunction()
        {
            MetaMemberFunction ToString = new MetaMemberFunction( this, "ToString" );              
            ToString.SetMetaDefineType(new MetaType(CoreMetaClassManager.stringMetaClass));
            AddMetaMemberFunction(ToString);
            ToString.AddCSharpMetaStatements("SimpleLanguage.VM.Int32Object", "Int32ToString" );
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Int32MetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
    public class UInt32MetaClass : MetaClass
    {
        public UInt32MetaClass() : base(DefaultObject.UInt32.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.UInt32;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new UInt32MetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
    public class Int64MetaClass : MetaClass
    {
        public Int64MetaClass() : base(DefaultObject.Int64.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Int64;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Int64MetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
    public class UInt64MetaClass : MetaClass
    {
        public UInt64MetaClass() : base(DefaultObject.UInt64.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.UInt64;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new UInt64MetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
}
