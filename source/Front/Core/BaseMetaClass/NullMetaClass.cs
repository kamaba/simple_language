//****************************************************************************
//  File:      NullMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class NullMetaClass : MetaClass
    {
        public NullMetaClass():base( DefaultObject.Null.ToString())
        {
            m_ClassDefineType = EClassDefineType.InnerDefine;
            m_Type = EType.Null;
        }        
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new NullMetaClass();
            return mc;
        }
    }
}
