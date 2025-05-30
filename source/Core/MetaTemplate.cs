//****************************************************************************
//  File:      MetaTemplate.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaTemplate : MetaBase
    {
        public bool isInFunction => m_IsInFunction;
        public List<MetaClass> constraintMetaClassList => m_ConstraintMetaClassList;
        public MetaClass ownerClass => m_OwnerClass;

        protected FileMetaTemplateDefine m_FileMetaTemplateDefine = null;
        protected MetaClass m_OwnerClass = null;
        protected List<MetaClass> m_ConstraintMetaClassList = new List<MetaClass>();
        protected bool m_IsInFunction = false;
        public MetaTemplate( MetaClass mc, FileMetaTemplateDefine fmtd)
        {
            m_Name = fmtd.name;
            m_FileMetaTemplateDefine = fmtd;
            m_OwnerClass = mc;
        }
        public MetaTemplate( MetaClass mc, string name )
        {
            m_Name = name;
            m_OwnerClass = mc;
        }
        public void ParseInConstraint()
        {
            if (m_FileMetaTemplateDefine != null)
            {
                for (int i = 0; i < m_FileMetaTemplateDefine.inClassNameTokenList.Count; i++)
                {
                    var mc = ClassManager.instance.GetMetaClassByInputTemplateAndFileMeta(m_OwnerClass, m_FileMetaTemplateDefine.inClassNameTokenList[i]);
                    m_ConstraintMetaClassList.Add(mc);
                }
            }
        }
        public void AddInConstraintMetaClass(MetaClass mc)
        {
            m_ConstraintMetaClassList.Add(mc);
        }
        public bool IsInConstraintMetaClass(MetaClass mc)
        {
            if (m_ConstraintMetaClassList.Count == 0) return true;

            // 还需要处理继承关系的类
            return m_ConstraintMetaClassList.Find(a => a == mc) != null;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_Name);
            if (constraintMetaClassList.Count > 0)
            {
                sb.Append(" in ");
                if (constraintMetaClassList.Count == 1)
                {
                    sb.Append(constraintMetaClassList[0].allName);
                }
                else
                {
                    sb.Append("[");
                    for (int i = 0; i < constraintMetaClassList.Count; i++)
                    {
                        sb.Append(constraintMetaClassList[i].allName);
                        if (i < constraintMetaClassList.Count - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sb.Append("]");
                }
            }

            return sb.ToString();
        }
    }

    public class MetaGenTemplate : MetaTemplate
    {
        public MetaType metaType => m_MetaType;

        private MetaType m_MetaType = null;
        public MetaGenTemplate(MetaTemplate mt, MetaType mtype ) : base( mt.ownerClass, mt.name )
        {
            m_MetaType = mtype;
        }

        public bool EqualWithMetaType( MetaType mt )
        {
            return m_MetaType.metaClass.allName == mt.metaClass.allName;
        }
        public string ToDefineTypeString()
        {
            StringBuilder sb = new StringBuilder();
            MetaGenTemplateClass mtc = m_MetaType.metaClass as MetaGenTemplateClass;
            if (mtc != null)
            {
                sb.Append(mtc.ToDefineTypeString());
            }
            else
            {
                sb.Append(m_MetaType.metaClass.name);
            }

            return sb.ToString();
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            MetaGenTemplateClass mtc = m_MetaType.metaClass as MetaGenTemplateClass;
            if (mtc!= null)
            {
                sb.Append(mtc.ToFormatString());
            }
            else
            {
                sb.Append(m_MetaType.metaClass.name);
            }

            return sb.ToString();
        }
    }


    public class MetaDefineTemplateCollection
    {
        public List<MetaTemplate> metaTemplateList => m_MetaTemplateList;
        public int count { get { return m_MetaTemplateList.Count; } }


        protected List<MetaTemplate> m_MetaTemplateList = new List<MetaTemplate>();

        public MetaTemplate GetMetaDefineTemplateByName(string _name)
        {
            for (int i = 0; i < m_MetaTemplateList.Count; i++)
            {
                if (m_MetaTemplateList[i].name == _name)
                    return m_MetaTemplateList[i];
            }
            return null;
        }
        public bool IsEqualMetaInputTemplateCollection(MetaInputTemplateCollection mpc)
        {
            if (mpc == null)
            {
                return m_MetaTemplateList.Count == 0;
            }

            if (m_MetaTemplateList.Count == mpc.metaTemplateParamsList.Count)
            {
                for (int i = 0; i < m_MetaTemplateList.Count; i++)
                {
                    MetaTemplate a = m_MetaTemplateList[i];
                    MetaType b = mpc.metaTemplateParamsList[i];
                    if (MatchMetaInputTemplate(a, b))
                        return true;
                }
            }
            return false;
        }
        public virtual bool MatchMetaInputTemplate(MetaTemplate a, MetaType b)
        {
            if (a.IsInConstraintMetaClass(b.metaClass))
                return true;
            return false;
        }
        public virtual void AddMetaDefineTemplate(MetaTemplate defineTemplate)
        {
            m_MetaTemplateList.Add(defineTemplate);
        }
        public virtual string ToFormatString()
        {
            //    StringBuilder sb = new StringBuilder();
            //    for (int i = 0; i < metaParamList.Count; i++)
            //    {
            //        sb.Append(metaParamList[i].ToTypeName());
            //        if (i < metaParamList.Count - 1)
            //            sb.Append("_");
            //    }
            //    return sb.ToString();
            return "";
        }
    }
}
