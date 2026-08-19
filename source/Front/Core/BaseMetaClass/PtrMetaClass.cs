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
    public class PtrMetaClass : MetaClass
    {
        public PtrMetaClass() : base( DefaultObject.Ptr.ToString())
        {
            m_Type = EType.Class;
            m_InnderDefine = true;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);


            var mt = new MetaTemplate(this, "T", CoreMetaClassManager.objectMetaClass, ECovariance.None);
            m_MetaTemplateList.Add(mt);
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new PtrMetaClass();
            return mc;
        }
    }
}
