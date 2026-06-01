using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Core;

namespace SimpleLanguage.Core
{
    public class BooleanMetaClass : MetaClass
    {
        public BooleanMetaClass(): base(DefaultObject.Boolean.ToString())
        {
            m_Type = EType.Boolean;
            m_InnderDefine = true;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new BooleanMetaClass();
            MetaConstExpressNode mcen = new MetaConstExpressNode( EType.Boolean, false);
            mc.SetDefaultExpressNode(mcen);
            return mc;
        }
    }
}
