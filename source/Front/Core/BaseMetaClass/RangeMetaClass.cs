//****************************************************************************
//  File:      ArrayMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class RangeMetaClass : MetaClass
    {
        public RangeMetaClass():base( DefaultObject.Range.ToString() )
        {
            m_Type = EType.Range;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
            m_InnderDefine = true;
            m_MetaTemplateList.Add( new MetaTemplate(this, "T", CoreMetaClassManager.numMetaClass ) );
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new RangeMetaClass();
            return mc;
        }
    }
}
