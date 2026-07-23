//****************************************************************************
//  File:      ExceptionMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/07/23 12:00:00
//  Description: Built-in Exception class for try/catch/throw.
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class ExceptionMetaClass : MetaClass
    {
        public ExceptionMetaClass() : base(DefaultObject.Exception.ToString())
        {
            m_InnderDefine = true;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
        }
        public static MetaClass CreateMetaClass()
        {
            return new ExceptionMetaClass();
        }
    }
}
