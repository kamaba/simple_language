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
using SimpleLanguage.Export.SLIR.Types;
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
            if (_irMetaClass == null)
            {
                _irMetaClass = IRManager.instance.GetIRMetaClassByName("Core.Object");
            }
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

            if (type.eMetaTypeType == EMetaTypeType.MetaClass
                || type.eMetaTypeType == EMetaTypeType.MetaData
                || type.eMetaTypeType == EMetaTypeType.MetaEnum)
            {
                irmt.m_IRMetaClass = IRManager.GetIRMetaClassByMetaType(type);
            }
            else if( type.eMetaTypeType == EMetaTypeType.MetaEnumValue )
            {
                irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(CoreMetaClassManager.memberMetaClass.classId);
            }
            else if (type.eMetaTypeType == EMetaTypeType.Template)
            {
                irmt.m_TemplateIndex = type.metaTemplate.index;
                irmt.m_IRMetaClass = IRManager.GetIRMetaClassByMetaType(type)
                    ?? IRManager.instance.GetIRMetaClassByName("Core.Object");
            }
            else
            {
                irmt.m_IRMetaClass = IRManager.GetIRMetaClassByMetaType(type);
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
            if (type.eMetaTypeType == EMetaTypeType.MetaClass
                || type.eMetaTypeType == EMetaTypeType.MetaData
                || type.eMetaTypeType == EMetaTypeType.MetaEnum)
            {
                irmt.m_IRMetaClass = IRManager.GetIRMetaClassByMetaType(type);
            }
            else if( type.eMetaTypeType == EMetaTypeType.MetaEnumValue )
            {
                irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.enumValue.ownerMetaBase.classId);
            }
            else if (type.eMetaTypeType == EMetaTypeType.Template)
            {
                irmt.m_TemplateIndex = type.metaTemplate.index;
                irmt.m_IRMetaClass = IRManager.GetIRMetaClassByMetaType(type)
                    ?? IRManager.instance.GetIRMetaClassByName("Core.Object");
            }
            else
            {
                irmt.m_IRMetaClass = IRManager.GetIRMetaClassByMetaType(type);
            }

            var dtmtList = type.GetGenTemplateMetaTypeList();
            for (int i = 0; i < dtmtList.Count; i++)
            {
                irmt.m_IRMetaTypeList.Add(IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(dtmtList[i], irmt.m_IROwnerMetaClass));
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
        
        /// <summary>
        /// 从导出的 SLRuntimeDefTypePackage 直接构建 IRMetaType，用于 ref module 导入。
        /// </summary>
        public static IRMetaType CreateFromPackage(SLRuntimeDefTypePackage typeDef, IRMetaClass ownerIRMc)
        {
            if (typeDef == null) return new IRMetaType(IRManager.instance.GetIRMetaClassByName("Core.Object"));
            var irmt = new IRMetaType();
            irmt.m_IROwnerMetaClass = ownerIRMc;
            if (typeDef.isTemplate && typeDef.templateIndex >= 0)
            {
                irmt.m_TemplateIndex = typeDef.templateIndex;
                irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassByName("Core.Object");
            }
            else
            {
                var name = string.IsNullOrEmpty(typeDef.className) ? "Core.Object" : typeDef.className;
                // 导出端 StripModulePrefix 去掉了模块前缀，而 Core 内建类型的 IRMetaClass.irName
                // 仍带 "Core." 前缀（如 "Core.Int32"）。因此先按去前缀名查（非 Core ref module 命中），
                // 再按 "Core."+name 查（Core 内建类型命中），最后回退 Core.Object。
                irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassByName(name)
                    ?? IRManager.instance.GetIRMetaClassByName("Core." + name)
                    ?? IRManager.instance.GetIRMetaClassByName("Core.Object");
            }
            if (typeDef.runtimeDefTypeList != null)
            {
                foreach (var child in typeDef.runtimeDefTypeList)
                {
                    irmt.m_IRMetaTypeList.Add(CreateFromPackage(child, ownerIRMc));
                }
            }
            return irmt;
        }

        /// <summary>
        /// 反向：IRMetaType -> MetaType（ref module 导入时从 IR 层复原 Meta 层类型）。
        /// 需要 IRMetaClass.typeOwner 已通过 LinkMetaOwner 关联（Phase C 之后调用）。
        /// 模板参数还原为 ownerClass 的 MetaTemplate；泛型参数递归复原。
        /// </summary>
        public static MetaType ToMetaType(IRMetaType irmt, MetaClass ownerClass)
        {
            if (irmt == null)
            {
                return new MetaType(CoreMetaClassManager.objectMetaClass);
            }
            if (irmt.templateIndex >= 0)
            {
                if (ownerClass != null && irmt.templateIndex < ownerClass.metaTemplateList.Count)
                {
                    return new MetaType(ownerClass.metaTemplateList[irmt.templateIndex]);
                }
                return new MetaType(CoreMetaClassManager.objectMetaClass);
            }
            var mb = irmt.irMetaClass?.typeOwner;
            if (mb is MetaClass mc)
            {
                if (irmt.irMetaTypeList != null && irmt.irMetaTypeList.Count > 0)
                {
                    var args = new List<MetaType>();
                    for (int i = 0; i < irmt.irMetaTypeList.Count; i++)
                    {
                        args.Add(ToMetaType(irmt.irMetaTypeList[i], ownerClass));
                    }
                    return new MetaType(mc, args);
                }
                return new MetaType(mc);
            }
            if (mb is MetaData md)
            {
                return new MetaType(md);
            }
            if (mb is MetaEnum me)
            {
                return new MetaType(me);
            }
            return new MetaType(CoreMetaClassManager.objectMetaClass);
        }

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
