//****************************************************************************
//  File:      ObjectMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class ObjectMetaClass : MetaClass
    {
        public ObjectMetaClass():base(  DefaultObject.Object.ToString())
        {
            m_InnderDefine = true;
        } 
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new ObjectMetaClass();
            return mc;
        }
    }
}
