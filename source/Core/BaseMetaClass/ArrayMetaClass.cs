//****************************************************************************
//  File:      ArrayMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************
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
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_MetaTemplateList.Add( new MetaTemplate(this, "T") );
        }

        public override void ParseInnerVariable()
        {
            MetaMemberVariable index = new MetaMemberVariable(this, "index", CoreMetaClassManager.int32MetaClass);
            AddMetaMemberVariable(index);

            //MetaMemberVariable tvalue = new MetaMemberVariable(this, "value", CoreMetaClassManager.templateMetaClass);
            //AddMetaMemberVariable(tvalue);
        }
        public override void ParseInnerFunction()
        {
            AddCoreFunction();
        }
        public void AddCoreFunction()
        {
            MetaMemberFunction Array = new MetaMemberFunction(this, "__Init__");
            MetaExpressNode men = new MetaConstExpressNode(EType.Int16, 0);
            Array.AddMetaDefineParam(new MetaDefineParam("bound1", this, null, CoreMetaClassManager.int16MetaClass, men));         
            Array.SetMetaDefineType(new MetaType(CoreMetaClassManager.arrayMetaClass));
            AddMetaMemberFunction(Array);


            //Array.AddMetaDefineParam(new MetaDefineParam("bound2", this, null, CoreMetaClassManager.int16MetaClass, men));
            //Array.AddMetaDefineParam(new MetaDefineParam("bound3", this, null, CoreMetaClassManager.int16MetaClass, men));
            //Array.AddMetaDefineParam(new MetaDefineParam("bound4", this, null, CoreMetaClassManager.int16MetaClass, men));

            MetaMemberFunction AddT = new MetaMemberFunction( this , "Add");
            //AddT.AddMetaDefineParam(new MetaDefineParam("T", this, null, CoreMetaClassManager.templateMetaClass, null));
            //AddT.SetMetaDefineType(new MetaType(CoreMetaClassManager.templateMetaClass));
            //AddMetaMemberFunction(AddT);

            MetaMemberFunction RemoveIndex = new MetaMemberFunction(this, "RemoveIndex");
            RemoveIndex.AddMetaDefineParam(new MetaDefineParam("index", this, null, CoreMetaClassManager.int32MetaClass, null));
            //RemoveIndex.SetMetaDefineType(new MetaDefineType(CoreMetaClassManager.int32MetaClass));
            AddMetaMemberFunction(RemoveIndex);

            MetaMemberFunction Remove = new MetaMemberFunction(this, "Remove");
            //Remove.AddMetaDefineParam(new MetaDefineParam("T", this, null, CoreMetaClassManager.templateMetaClass, null));
            //Remove.SetMetaDefineType(new MetaType(CoreMetaClassManager.objectMetaClass));
            //AddMetaMemberFunction(Remove);

            MetaMemberFunction SetLength = new MetaMemberFunction(this, "SetLength");
            SetLength.AddMetaDefineParam(new MetaDefineParam("length", this, null, CoreMetaClassManager.int32MetaClass, null));
            SetLength.SetMetaDefineType(new MetaType(CoreMetaClassManager.voidMetaClass));
            AddMetaMemberFunction(SetLength);

            MetaMemberFunction Count = new MetaMemberFunction(this, "Count");
            Count.SetMetaDefineType(new MetaType(CoreMetaClassManager.int32MetaClass));
            AddMetaMemberFunction(Count);

            MetaMemberFunction IsIn = new MetaMemberFunction(this, "IsIn");
            //IsIn.AddMetaDefineParam(new MetaDefineParam("T", this, null, CoreMetaClassManager.templateMetaClass, null));
            //IsIn.SetMetaDefineType(new MetaType(CoreMetaClassManager.voidMetaClass));
            //AddMetaMemberFunction(IsIn);
        }
        public void Init_Call( ArrayObject<int> arrObj, int count )
        {

        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new ArrayMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
}
