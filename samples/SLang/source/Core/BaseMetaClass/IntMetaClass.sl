//****************************************************************************
//  File:      IntMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class Int16MetaClass : MetaClass
    {
        public Int16MetaClass() : base( DefaultObject.Int16.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Int16;
            m_ClassDefineType = EClassDefineType.InnerDefine;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Int16MetaClass();
            return mc;
        }
    }
    public class UInt16MetaClass : MetaClass
    {
        public UInt16MetaClass() : base(DefaultObject.UInt16.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.UInt16;
            m_ClassDefineType = EClassDefineType.InnerDefine;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new UInt16MetaClass();
            return mc;
        }
    }
    public class Int32MetaClass : MetaClass
    {
        public Int32MetaClass() : base( DefaultObject.Int32.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Int32;
            m_ClassDefineType = EClassDefineType.InnerDefine;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Int32MetaClass();
            return mc;
        }
    }
    public class UInt32MetaClass : MetaClass
    {
        public UInt32MetaClass() : base( DefaultObject.UInt32.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.UInt32;
            m_ClassDefineType = EClassDefineType.InnerDefine;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new UInt32MetaClass();
            return mc;
        }
    }
    public class Int64MetaClass : MetaClass
    {
        public Int64MetaClass() : base( DefaultObject.Int64.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.Int64;
            m_ClassDefineType = EClassDefineType.InnerDefine;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Int64MetaClass();
            return mc;
        }
    }
    public class UInt64MetaClass : MetaClass
    {
        public UInt64MetaClass() : base( DefaultObject.UInt64.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_Type = EType.UInt64;
            m_ClassDefineType = EClassDefineType.InnerDefine;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new UInt64MetaClass();
            return mc;
        }
    }
}
