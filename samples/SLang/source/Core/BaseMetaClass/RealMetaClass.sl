//****************************************************************************
//  File:      RealMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class Float32MetaClass : MetaClass
    {
        public Float32MetaClass() : base(DefaultObject.Float32.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_ClassDefineType = EClassDefineType.InnerDefine;
            MetaConstExpressNode mcen = new MetaConstExpressNode(EType.Float32, 0.0f);
            SetDefaultExpressNode(mcen);


        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Float32MetaClass();
            return mc;
        }
    }
    public class Float64MetaClass : MetaClass
    {
        public Float64MetaClass() : base(DefaultObject.Float64.ToString())
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_ClassDefineType = EClassDefineType.InnerDefine;
            MetaConstExpressNode mcen = new MetaConstExpressNode(EType.Float64, 0.0f);
            SetDefaultExpressNode(mcen);
            m_Type = EType.Float64;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new Float64MetaClass();
            return mc;
        }
    }
}
