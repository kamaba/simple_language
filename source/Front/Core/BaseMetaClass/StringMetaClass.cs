//****************************************************************************
//  File:      StringMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************


namespace SimpleLanguage.Core
{
    public class StringMetaClass : MetaClass
    {
        public StringMetaClass():base( DefaultObject.String.ToString())
        {
            m_ClassDefineType = EClassDefineType.InnerDefine;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.String;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new StringMetaClass();
            return mc;
        }
    }
}
