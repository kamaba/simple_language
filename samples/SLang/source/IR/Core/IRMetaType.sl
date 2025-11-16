//****************************************************************************
//  File:      IRMetaType.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/9/5 12:00:00
//  Description: Meta class's ir attribute
//****************************************************************************


using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SimpleLanguage.Core;

namespace SimpleLanguage.IR
{
    public class IRMetaType
    {
        public IRMetaClass irMetaClass => m_IRMetaClass;
        public IRMetaClass irOwnerMetaClass => m_IROwnerMetaClass;
        public List<IRMetaType> irMetaTypeList => m_IRMetaTypeList;
        public int templateIndex => m_TemplateIndex;

        public IRMetaClass m_IRMetaClass = null;
        private IRMetaClass m_IROwnerMetaClass = null;
        private List<IRMetaType> m_IRMetaTypeList = new List<IRMetaType>();

        private int m_TemplateIndex = -1;

        public IRMetaType()
        {

        }
        public IRMetaType( IRMetaClass _irMetaClass)
        {
            m_IRMetaClass = _irMetaClass;
        }
        public IRMetaType( MetaType type )
        {
            if (type.eType == EMetaTypeType.MetaClass)
            {
                m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
                m_IROwnerMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
            }
            else if (type.eType == EMetaTypeType.Template)
            {
                m_TemplateIndex = type.metaTemplate.index;
                m_IROwnerMetaClass = IRManager.instance.GetIRMetaClassById(type.metaTemplate.ownerClass.GetHashCode());
                m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
            }
            else
            {
                m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
                m_IROwnerMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
            }
            if (m_IRMetaClass == null || m_IROwnerMetaClass == null )
            {
                Debug.Assert(false, "这个不可以为空!");
            }
            for (int i = 0; i < type.defineTemplateMetaTypeList.Count; i++)
            {
                m_IRMetaTypeList.Add(IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(type.defineTemplateMetaTypeList[i], m_IROwnerMetaClass));
            }
        }
        public static IRMetaType CreateIRMetaTypeByGenTemplateMetaTypeList( MetaType type, IRMetaClass ownerIRMc)
        {
            IRMetaType irmt = new();
            irmt.m_IROwnerMetaClass = IRManager.instance.GetIRMetaClassById(ownerIRMc.id);
            if (type.eType == EMetaTypeType.MetaClass)
            {
                irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
            }
            else if (type.eType == EMetaTypeType.Template)
            {
                irmt.m_TemplateIndex = type.metaTemplate.index;
                irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
            }
            else
            {
                irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
            }

            for (int i = 0; i < type.genTemplateMetaTypeList.Count; i++)
            {
                irmt.m_IRMetaTypeList.Add(IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(type.genTemplateMetaTypeList[i], irmt.m_IROwnerMetaClass));
            }
            if (irmt.m_IRMetaClass == null || irmt.m_IROwnerMetaClass == null)
            {
                Debug.Assert(false, "这个不可以为空!");
            }
            return irmt;
        }
        public static IRMetaType CreateIRMetaTypeByDefineTemplateMetaTypeList(MetaType type, IRMetaClass ownerIRMc )
        {
            IRMetaType irmt = new();
            irmt.m_IROwnerMetaClass = IRManager.instance.GetIRMetaClassById(ownerIRMc.id );
            if (type.eType == EMetaTypeType.MetaClass)
            {
                irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
            }
            else if (type.eType == EMetaTypeType.Template)
            {
                irmt.m_TemplateIndex = type.metaTemplate.index;
                irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
            }
            else
            {
                irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
            }

            for (int i = 0; i < type.defineTemplateMetaTypeList.Count; i++)
            {
                irmt.m_IRMetaTypeList.Add(IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(type.defineTemplateMetaTypeList[i], irmt.m_IROwnerMetaClass));
            }
            if (irmt.m_IRMetaClass == null || irmt.m_IROwnerMetaClass == null)
            {
                Debug.Assert(false, "这个不可以为空!");
            }
            return irmt;
        }
        public IRMetaType( IRMetaClass irmc, List<IRMetaType> irlist )
        {
            m_IRMetaClass = irmc;
            m_IRMetaTypeList = irlist;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(this.m_IRMetaClass.irName);           

            return sb.ToString();
        }
    }
}
