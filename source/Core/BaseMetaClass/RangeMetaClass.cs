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
    public class RangeMetaClass : MetaClass
    {
        public RangeMetaClass():base( DefaultObject.Range.ToString() )
        {
            m_Type = EType.Range;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_IsInnerDefineCompile = true;
            m_MetaTemplateList.Add( new MetaTemplate(this, "T") );
        }
        public override void ParseInnerVariable()
        {
            MetaTemplate mt = m_MetaTemplateList[0];

            MetaMemberVariable m_Start = new MetaMemberVariable(this, "m_Start", mt );
            AddMetaMemberVariable(m_Start);
            MetaMemberVariable m_End = new MetaMemberVariable(this, "m_End", mt );
            AddMetaMemberVariable(m_End);
            MetaMemberVariable m_Step = new MetaMemberVariable(this, "m_Step", mt );
            AddMetaMemberVariable(m_Step);

            MetaMemberVariable index = new MetaMemberVariable(this, "index", CoreMetaClassManager.int32MetaClass);
            AddMetaMemberVariable(index);

            //MetaMemberVariable tvalue = new MetaMemberVariable(this, "value", CoreMetaClassManager.templateMetaClass);
            //AddMetaMemberVariable(tvalue);
        }
        public override void ParseInnerFunction()
        {
            MetaMemberFunction __Init__ = new MetaMemberFunction(this, "__Init__");
            AddMetaMemberFunction(__Init__);
            __Init__.SetMetaDefineType(new MetaType(CoreMetaClassManager.rangeMetaClass));

            MetaTemplate mt = m_MetaTemplateList[0];

            __Init__.AddMetaDefineParam(new MetaDefineParam("start", this, null, mt ));
            __Init__.AddMetaDefineParam(new MetaDefineParam("end", this, null, mt ));
            __Init__.AddMetaDefineParam(new MetaDefineParam("step", this, null, mt ));

            MetaMemberFunction IsIn = new MetaMemberFunction(this, "IsIn");
            IsIn.AddMetaDefineParam(new MetaDefineParam("T", this, null, mt ));
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
