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
    public class TypeMetaClass : MetaClass
    {
        public TypeMetaClass() : base( DefaultObject.Type.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Class;
            m_InnderDefine = true;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new TypeMetaClass();
            return mc;
        }
    }
}
