using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.SelfMeta
{
    public class BooleanMetaClass : MetaClass
    {
        public BooleanMetaClass(): base(DefaultObject.Boolean.ToString())
        {
            m_Type = EType.Boolean;
            m_IsInnerDefineCompile = true;
            SetExtendClass(CoreMetaClassManager.objectMetaClass);
        }
        public static MetaClass CreateMetaClass()
        {
            MetaClass mc = new BooleanMetaClass();
            ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.coreModule );
            MetaConstExpressNode mcen = new MetaConstExpressNode( EType.Boolean, false);
            mc.SetDefaultExpressNode(mcen);
            return mc;
        }
    }
}
