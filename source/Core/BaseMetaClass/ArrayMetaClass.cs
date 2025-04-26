//****************************************************************************
//  File:      ArrayMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Core.MetaObjects;
using SimpleLanguage.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.SelfMeta
{
    public class IEnumerableMetaClass : MetaClass
    {
        public IEnumerableMetaClass() : base(DefaultObject.Array.ToString())
        {
            m_Type = EType.Array;
            m_IsInnerDefineCompile = true;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            //m_MetaTemplateList.Add(new TemplateMetaClass("T"));
        }
    }
    public class ArrayIteratorMetaClass : MetaClass
    {
        public ArrayIteratorMetaClass() : base(DefaultObject.Class.ToString())
        {
            m_Type = EType.Class;
            m_IsInnerDefineCompile = true;
        }
        public override void ParseInnerVariable()
        {
            MetaMemberVariable index = new MetaMemberVariable(this, "index", CoreMetaClassManager.int32MetaClass);
            AddMetaMemberVariable(index);

            //MetaMemberVariable tvalue = new MetaMemberVariable(this, "value", CoreMetaClassManager.templateMetaClass);
            //AddMetaMemberVariable(tvalue);
        }
        public static MetaClass CreateMetaClass()
        {
            ArrayIteratorMetaClass mc = new ArrayIteratorMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
    public class ArrayMetaClass : MetaClass
    {
        public ArrayMetaClass():base( DefaultObject.Array.ToString() )
        {
            m_Type = EType.Array;
            m_IsInnerDefineCompile = true;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_MetaTemplateList.Add( new MetaTemplate(this, "T") );
        }
        public override void ParseInnerVariable()
        {
            MetaMemberVariable m_Count = new MetaMemberVariable(this, "m_Count", CoreMetaClassManager.uint32MetaClass);
            AddMetaMemberVariable(m_Count);

            MetaMemberVariable m_Bound1 = new MetaMemberVariable(this, "m_Bound1", CoreMetaClassManager.uint16MetaClass);
            AddMetaMemberVariable(m_Bound1);
            MetaMemberVariable m_Bound2 = new MetaMemberVariable(this, "m_Bound2", CoreMetaClassManager.uint16MetaClass);
            AddMetaMemberVariable(m_Bound2);

            MetaMemberVariable m_Index = new MetaMemberVariable(this, "m_Index", CoreMetaClassManager.int32MetaClass);
            AddMetaMemberVariable(m_Index);

            MetaMemberVariable m_Value = new MetaMemberVariable(this, "m_Value", new MetaTemplate(this, "T") );
            AddMetaMemberVariable(m_Value);
        }
        public override void ParseInnerFunction()
        {
            AddCoreFunction();
        }
        public void AddCoreFunction()
        {
            //MetaMemberFunction __Init__ = new MetaMemberFunction(this, "__Init__");
            //MetaExpressNode men = new MetaConstExpressNode(EType.Int32, 0);
            //__Init__.AddMetaDefineParam(new MetaDefineParam("_count", this, null, CoreMetaClassManager.int32MetaClass, men));
            //__Init__.AddMetaDefineParam(new MetaDefineParam("bound1", this, null, CoreMetaClassManager.int32MetaClass, men));
            //AddInnerMetaMemberFunction(__Init__);

            //////Array.AddMetaDefineParam(new MetaDefineParam("bound2", this, null, CoreMetaClassManager.int16MetaClass, men));
            //////Array.AddMetaDefineParam(new MetaDefineParam("bound3", this, null, CoreMetaClassManager.int16MetaClass, men));
            //////Array.AddMetaDefineParam(new MetaDefineParam("bound4", this, null, CoreMetaClassManager.int16MetaClass, men));

            //MetaMemberFunction AddT = new MetaMemberFunction( this , "Add");
            //AddT.AddMetaDefineParam(new MetaDefineParam("T", this, null, new MetaTemplate(this, "T") ));
            //AddInnerMetaMemberFunction(AddT);

            //MetaMemberFunction RemoveIndex = new MetaMemberFunction(this, "RemoveIndex");
            //RemoveIndex.AddMetaDefineParam(new MetaDefineParam("index", this, null, CoreMetaClassManager.int32MetaClass, null));
            ////RemoveIndex.SetMetaDefineType(new MetaDefineType(CoreMetaClassManager.int32MetaClass));
            //AddInnerMetaMemberFunction(RemoveIndex);

            //MetaMemberFunction Remove = new MetaMemberFunction(this, "Remove");
            ////Remove.AddMetaDefineParam(new MetaDefineParam("T", this, null, CoreMetaClassManager.templateMetaClass, null));
            ////Remove.SetMetaDefineType(new MetaType(CoreMetaClassManager.objectMetaClass));
            ////AddInnerMetaMemberFunction(Remove);

            //MetaMemberFunction SetLength = new MetaMemberFunction(this, "SetLength");
            //SetLength.AddMetaDefineParam(new MetaDefineParam("length", this, null, CoreMetaClassManager.int32MetaClass, null));
            //SetLength.SetMetaDefineType(new MetaType(CoreMetaClassManager.voidMetaClass));
            //AddInnerMetaMemberFunction(SetLength);

            //MetaMemberFunction Count = new MetaMemberFunction(this, "Count");
            //Count.SetMetaDefineType(new MetaType(CoreMetaClassManager.int32MetaClass));
            //AddInnerMetaMemberFunction(Count);

            //MetaMemberFunction IsIn = new MetaMemberFunction(this, "IsIn");
            ////IsIn.AddMetaDefineParam(new MetaDefineParam("T", this, null, CoreMetaClassManager.templateMetaClass, null));
            ////IsIn.SetMetaDefineType(new MetaType(CoreMetaClassManager.voidMetaClass));
            ////AddInnerMetaMemberFunction(IsIn);
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new ArrayMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
        public static int GetMetaClassCount( MetaArrayObject amo )
        {
            return amo.count();
        }
    }
}
