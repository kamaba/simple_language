//****************************************************************************
//  File:      ErrorMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/07/24 12:00:00
//  Description: Built-in Error base class. Only enums extending Error can be thrown.
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class ErrorMetaClass : MetaClass
    {
        public ErrorMetaClass() : base(DefaultObject.Error.ToString())
        {
            m_InnderDefine = true;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
        }
        public static MetaClass CreateMetaClass()
        {
            return new ErrorMetaClass();
        }
    }
}
