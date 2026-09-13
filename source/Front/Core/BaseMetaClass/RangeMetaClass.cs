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
            var mt = new MetaTemplate(this, "T", CoreMetaClassManager.numMetaClass, ECovariance.None );
            mt.SetIndex(0);
            m_MetaTemplateList.Add( mt );
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new RangeMetaClass();
            return mc;
        }
    }
}
