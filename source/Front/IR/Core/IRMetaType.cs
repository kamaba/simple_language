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
using SimpleLanguage.Logging;

namespace SimpleLanguage.IR
{
    public class IRMetaType
    {
        public IRMetaClass irMetaClass => m_IRMetaClass;
        public IRMetaClass irOwnerMetaClass => m_IROwnerMetaClass;
        public List<IRMetaType> irMetaTypeList => m_IRMetaTypeList;
        public int templateIndex => m_TemplateIndex;

        private IRMetaClass m_IRMetaClass = null;
        private IRMetaClass m_IROwnerMetaClass = null;
        private List<IRMetaType> m_IRMetaTypeList = new List<IRMetaType>();
        private int m_TemplateIndex = -1;

        public IRMetaType(){ }
        public IRMetaType( IRMetaClass _irMetaClass)
        {
            Debug.Assert(_irMetaClass != null, "");
            m_IRMetaClass = _irMetaClass;
        }
        public IRMetaType(IRMetaClass irmc, List<IRMetaType> irlist)
        {
            m_IRMetaClass = irmc;
            m_IRMetaTypeList = irlist;
        }
        public static IRMetaType CreateIRMetaTypeByGenTemplateMetaTypeList( MetaType type, IRMetaClass ownerIRMc)
        {
            IRMetaType irmt = new();
            irmt.m_IROwnerMetaClass = IRManager.instance.GetIRMetaClassById(ownerIRMc.id);

            var gtmc = type.GetTemplateMetaClass();
            if (type.eMetaTypeType == EMetaTypeType.MetaClass)
            {
                irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(gtmc.GetHashCode());
            }
            else if (type.eMetaTypeType == EMetaTypeType.Template)
            {
                irmt.m_TemplateIndex = type.metaTemplate.index;
                var gtmc2 = type.GetTemplateMetaClass();
                if(gtmc2 != null )
                {
                    irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(gtmc2.GetHashCode());
                }
                else
                {
                    irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassByName("Core.Object");
                }
            }
            else
            {
                irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
            }
            var lits = type.GetGenTemplateMetaTypeList();
            for (int i = 0; i < lits.Count; i++)
            {
                irmt.m_IRMetaTypeList.Add(IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(lits[i], irmt.m_IROwnerMetaClass));
            }
            //for (int i = 0; i < type.genTemplateMetaTypeList.Count; i++)
            //{
            //    irmt.m_IRMetaTypeList.Add(IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(type.genTemplateMetaTypeList[i], irmt.m_IROwnerMetaClass));
            //}
            if (irmt.m_IRMetaClass == null )
            {
                Log.AddIRLog(LID.IRParseMetaTypeMetaClassIsNull, "IRMetaClass");
            }
            if(irmt.m_IROwnerMetaClass == null )
            {
                Log.AddIRLog(LID.IRParseMetaTypeMetaClassIsNull, "IROwnerMetaClass");
            }
            return irmt;
        }
        public static IRMetaType CreateIRMetaTypeByDefineTemplateMetaTypeList(MetaType type, IRMetaClass ownerIRMc )
        {
            IRMetaType irmt = new();
            irmt.m_IROwnerMetaClass = IRManager.instance.GetIRMetaClassById(ownerIRMc.id );
            if (type.eMetaTypeType == EMetaTypeType.MetaClass)
            {
                irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
            }
            else if (type.eMetaTypeType == EMetaTypeType.Template)
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
            if (irmt.m_IRMetaClass == null )
            {
                Log.AddIRLog(LID.IRParseMetaTypeMetaClassIsNull, "irMetaClass");
            }
            if (irmt.m_IROwnerMetaClass == null)
            {
                Log.AddIRLog(LID.IRParseMetaTypeMetaClassIsNull, "irOwnerMetaClass");
            }
            return irmt;
        }


        /*
        public IRMetaType( MetaType type )
        {
            //this.m_IsArray = type.isArray;
            if (type.eMetaTypeType == EMetaTypeType.MetaClass)
            {
                m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
                m_IROwnerMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
            }
            else if (type.eMetaTypeType == EMetaTypeType.Template)
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
        */
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(this.m_IRMetaClass?.irName ?? "null");

            if (m_IRMetaTypeList != null && m_IRMetaTypeList.Count > 0)
            {
                sb.Append("<");
                for (int i = 0; i < m_IRMetaTypeList.Count; i++)
                {
                    sb.Append(m_IRMetaTypeList[i]?.ToString());
                    if (i < m_IRMetaTypeList.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(">");
            }

            if (m_TemplateIndex >= 0)
            {
                sb.Append("[T:");
                sb.Append(m_TemplateIndex);
                sb.Append("]");
            }

            return sb.ToString();
        }
    }
}
