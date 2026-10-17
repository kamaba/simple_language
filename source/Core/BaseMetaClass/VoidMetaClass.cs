//****************************************************************************
//  File:      VoidMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.Core
{
    public class VoidMetaClass : MetaClass
    {
        public VoidMetaClass():base( DefaultObject.Void.ToString() )
        {
            SetExtendClass(CoreMetaClassManager.objectMetaClass );
            m_InnderDefine = true;
            MetaConstExpressNode mcen = new MetaConstExpressNode(EType.Null, "null");
            SetDefaultExpressNode(mcen);
            m_Type = EType.Void;
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new VoidMetaClass();
            return mc;

        }
    }
}
