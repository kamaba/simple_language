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
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace SimpleLanguage.Core.SelfMeta
{
    public class RangeIteratorMetaClass : MetaClass
    {
        public RangeIteratorMetaClass() : base(DefaultObject.Class.ToString())
        {
            m_Type = EType.Class;
        }
        public override void ParseInnerVariable()
        {
            MetaMemberVariable index = new MetaMemberVariable(this, "index", CoreMetaClassManager.int32MetaClass);
            AddMetaMemberVariable(index);

            MetaMemberVariable tvalue = new MetaMemberVariable(this, "value", CoreMetaClassManager.templateMetaClass);
            AddMetaMemberVariable(tvalue);
        }
        public static MetaClass CreateMetaClass()
        {
            ArrayIteratorMetaClass mc = new ArrayIteratorMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
    public class RangeMetaClass : MetaClass
    {
        public RangeMetaClass():base( DefaultObject.Range.ToString() )
        {
            m_Type = EType.Array;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_MetaTemplateList.Add( new MetaTemplate(this, "T") );
        }
        public override void ParseInnerVariable()
        {
            MetaMemberVariable index = new MetaMemberVariable(this, "index", CoreMetaClassManager.int32MetaClass);
            AddMetaMemberVariable(index);

            MetaMemberVariable tvalue = new MetaMemberVariable(this, "value", CoreMetaClassManager.templateMetaClass);
            AddMetaMemberVariable(tvalue);
        }
        public override void ParseInnerFunction()
        {
            AddCoreFunction();
        }
        public void AddCoreFunction()
        {
            MetaMemberFunction Array = new MetaMemberFunction(this, "__Init__");
            AddMetaMemberFunction(Array); 
            Array.SetMetaDefineType(new MetaType(CoreMetaClassManager.rangeMetaClass));

            MetaExpressNode men = new MetaConstExpressNode(EType.Int32, 0);
            Array.AddMetaDefineParam(new MetaDefineParam("start", this, null, CoreMetaClassManager.int32MetaClass, men));         

            MetaExpressNode men2 = new MetaConstExpressNode(EType.Int32, 1);
            Array.AddMetaDefineParam(new MetaDefineParam("end", this, null, CoreMetaClassManager.int32MetaClass, men2 ));

            MetaExpressNode men3 = new MetaConstExpressNode(EType.Int32, 1);
            Array.AddMetaDefineParam(new MetaDefineParam("step", this, null, CoreMetaClassManager.int32MetaClass, men3));


            MetaMemberFunction AddT = new MetaMemberFunction( this , "Add");
            AddT.AddMetaDefineParam(new MetaDefineParam("T", this, null, CoreMetaClassManager.templateMetaClass, null));
            AddT.SetMetaDefineType(new MetaType(CoreMetaClassManager.templateMetaClass));
            AddMetaMemberFunction(AddT);

            MetaMemberFunction RemoveIndex = new MetaMemberFunction(this, "RemoveIndex");
            RemoveIndex.AddMetaDefineParam(new MetaDefineParam("index", this, null, CoreMetaClassManager.int32MetaClass, null));
            //RemoveIndex.SetMetaDefineType(new MetaDefineType(CoreMetaClassManager.int32MetaClass));
            AddMetaMemberFunction(RemoveIndex);

            MetaMemberFunction Remove = new MetaMemberFunction(this, "Remove");
            Remove.AddMetaDefineParam(new MetaDefineParam("T", this, null, CoreMetaClassManager.templateMetaClass, null));
            Remove.SetMetaDefineType(new MetaType(CoreMetaClassManager.objectMetaClass));
            AddMetaMemberFunction(Remove);

            MetaMemberFunction SetLength = new MetaMemberFunction(this, "SetLength");
            SetLength.AddMetaDefineParam(new MetaDefineParam("length", this, null, CoreMetaClassManager.int32MetaClass, null));
            SetLength.SetMetaDefineType(new MetaType(CoreMetaClassManager.voidMetaClass));
            AddMetaMemberFunction(SetLength);

            MetaMemberFunction Count = new MetaMemberFunction(this, "Count");
            Count.SetMetaDefineType(new MetaType(CoreMetaClassManager.int32MetaClass));
            AddMetaMemberFunction(Count);

            MetaMemberFunction IsIn = new MetaMemberFunction(this, "IsIn");
            IsIn.AddMetaDefineParam(new MetaDefineParam("T", this, null, CoreMetaClassManager.templateMetaClass, null));
            IsIn.SetMetaDefineType(new MetaType(CoreMetaClassManager.voidMetaClass));
            AddMetaMemberFunction(IsIn);
        }
        public void Init_Call( ArrayObject<int> arrObj, int count )
        {

        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new RangeMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule);
            return mc;
        }
    }
}
