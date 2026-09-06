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
        public IteratorMetaClass() : base("IIterator")
        {
            m_Type = EType.Class;
            m_InnderDefine = true;
            m_IsInterfaceClass = true;

            // 源码声明为 IIterator<out T>，协变
            var mt = new MetaTemplate(this, "T", CoreMetaClassManager.objectMetaClass, ECovariance.Out);
            mt.SetIndex(0);
            m_MetaTemplateList.Add(mt);
        }
        public static MetaClass CreateMetaClass()
        {
            IteratorMetaClass mc = new IteratorMetaClass();
            return mc;
        }
    }
    public class IterableMetaClass : MetaClass
    {
        public IterableMetaClass() : base("IIterable")
        {
            m_Type = EType.Class;
            m_InnderDefine = true;
            m_IsInterfaceClass = true;

            var mt = new MetaTemplate(this, "T", CoreMetaClassManager.objectMetaClass, ECovariance.None);
            mt.SetIndex(0);
            m_MetaTemplateList.Add(mt);
        }
        public static MetaClass CreateMetaClass()
        {
            IterableMetaClass mc = new IterableMetaClass();
            return mc;
        }
    }
    public class ArrayMetaClass : MetaClass
    {
        public ArrayMetaClass() : base(DefaultObject.Array.ToString())
        {
            m_Type = EType.Array;
            m_InnderDefine = true;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);


            var mt = new MetaTemplate(this, "T", CoreMetaClassManager.objectMetaClass, ECovariance.None);
            mt.SetIndex(0);
            m_MetaTemplateList.Add(mt);
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new ArrayMetaClass();
            return mc;
        }
    }
}
