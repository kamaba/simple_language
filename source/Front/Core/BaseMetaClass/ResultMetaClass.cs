//****************************************************************************
//  File:      PtrMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/8/19 12:00:00
//  Description: 
//****************************************************************************

using System;

namespace SimpleLanguage.Core
{
    public class ResultMetaClass : MetaClass
    {
        public ResultMetaClass() : base(DefaultObject.Result.ToString())
        {
            m_Type = EType.Class;
            m_InnderDefine = true;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new ResultMetaClass();
            return mc;
        }
    }
    public class ResultTMetaClass : MetaClass
    {
        public ResultTMetaClass() : base(DefaultObject.ResultT.ToString())
        {
            m_Type = EType.Class;
            m_InnderDefine = true;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);


            var mt = new MetaTemplate(this, "T", CoreMetaClassManager.objectMetaClass, ECovariance.None);
            mt.SetIndex(0);
            m_MetaTemplateList.Add(mt);
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new ResultTMetaClass();
            return mc;
        }
    }
}
