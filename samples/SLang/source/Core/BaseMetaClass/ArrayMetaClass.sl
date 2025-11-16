//****************************************************************************
//  File:      ArrayMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class IEnumerableMetaClass : MetaClass
    {
        public IEnumerableMetaClass() : base(DefaultObject.Array.ToString())
        {
            m_Type = EType.Array;
            m_ClassDefineType = EClassDefineType.InnerDefine;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            //m_MetaTemplateList.Add(new TemplateMetaClass("T"));
        }
    }
    public class ArrayIteratorMetaClass : MetaClass
    {
        public ArrayIteratorMetaClass() : base(DefaultObject.Array.ToString())
        {
            m_Type = EType.Class;
            m_ClassDefineType = EClassDefineType.InnerDefine;
        }
        public static MetaClass CreateMetaClass()
        {
            ArrayIteratorMetaClass mc = new ArrayIteratorMetaClass();
            return mc;
        }
    }
    public class ArrayMetaClass : MetaClass
    {
        public ArrayMetaClass():base(DefaultObject.Array.ToString() )
        {
            m_Type = EType.Array;
            m_ClassDefineType = EClassDefineType.InnerDefine;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new ArrayMetaClass();
            return mc;
        }
    }
}
