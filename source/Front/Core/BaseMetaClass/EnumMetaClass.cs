//****************************************************************************
//  File:      EnumMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core
{
    public class EnumMetaClass : MetaClass
    {
        public EnumMetaClass() : base(DefaultObject.Enum.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Enum;
            m_ClassDefineType = EClassDefineType.InnerDefine;
        }
        //public override void ParseInnerFunction()
        //{
        //    //AddCoreFunction();
        //}
        //public void AddCoreFunction()
        //{
        //    MetaMemberFunction values = new MetaMemberFunction(this, "_values_");
        //    values.SetIsGet( true );
        //    values.SetReturnMetaClass(CoreMetaClassManager.arrayMetaClass);
        //    AddMetaMemberFunction(values);
        //}
        public static MetaClass CreateMetaClass()
        {
            EnumMetaClass mc = new EnumMetaClass();
            return mc;
        }
    }

    public class MemberMetaClass : MetaClass
    {
        public MemberMetaClass() : base(DefaultObject.Member.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Member;
            m_ClassDefineType = EClassDefineType.InnerDefine;
            m_NeedInitMemberVariables = false;
        }
        public override void ParseInnerFunction()
        {
            MetaMemberVariable name = new MetaMemberVariable(this, "name");
            name.SetIndex(0);
            AddMetaMemberVariable(name, false);
            name.SetMetaDefineType(new MetaType(CoreMetaClassManager.stringMetaClass));
            name.SetIsDefineMetaType(true);
            var namecexp = new MetaConstExpressNode(EType.String, "");
            name.SetExpress(namecexp);

            MetaMemberVariable index = new MetaMemberVariable(this, "index");
            index.SetIndex(1);
            AddMetaMemberVariable(index, false);
            index.SetMetaDefineType(new MetaType(CoreMetaClassManager.int32MetaClass));
            index.SetIsDefineMetaType(true);
            var indexcexp = new MetaConstExpressNode(EType.Int32, 0);
            index.SetExpress(indexcexp);


            MetaMemberVariable value = new MetaMemberVariable(this, "value");
            value.SetIndex(2);
            AddMetaMemberVariable(value, false);
            value.SetMetaDefineType(new MetaType(CoreMetaClassManager.objectMetaClass));
            value.SetIsDefineMetaType(false);
            var mcen = new MetaConstExpressNode(EType.Null, null);
            value.SetExpress(mcen);
        }
        public static MetaClass CreateMetaClass()
        {
            MemberMetaClass mc = new MemberMetaClass();
            return mc;
        }
    }
}
