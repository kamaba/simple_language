//****************************************************************************
//  File:      ArrayMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class IteratorMetaClass : MetaClass
    {
        public IteratorMetaClass() : base(DefaultObject.Array.ToString())
        {
            m_Type = EType.Class;
            m_ClassDefineType = EClassDefineType.InnerDefine;

            var mt = new MetaTemplate( this, "T", CoreMetaClassManager.objectMetaClass );
            m_MetaTemplateList.Add(mt);
        }
        public static MetaClass CreateMetaClass()
        {
            IteratorMetaClass mc = new IteratorMetaClass();
            return mc;
        }
    }
    public class ArrayMetaClass : MetaClass
    {
        public ArrayMetaClass() : base(DefaultObject.Array.ToString())
        {
            m_Type = EType.Array;
            m_ClassDefineType = EClassDefineType.InnerDefine;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);


            var mt = new MetaTemplate(this, "T", CoreMetaClassManager.objectMetaClass);
            m_MetaTemplateList.Add(mt);
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new ArrayMetaClass();
            return mc;
        }
    }
}
