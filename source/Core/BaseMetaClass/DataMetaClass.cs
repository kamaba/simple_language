//****************************************************************************
//  File:      IntMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

using System;

namespace SimpleLanguage.Core
{
    public class DataMetaClass : MetaClass
    {
        public DataMetaClass() : base( DefaultObject.Data.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Class;
            m_InnderDefine = true;
        }
        public static MetaClass CreateMetaClass()
        {
            DataMetaClass mc = new DataMetaClass();
            return mc;
        }
    }
}
