using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public partial class MetaTemplate : MetaBase
    {
        public List<MetaClass> constraintMetaClassList => m_ConstraintMetaClassList;

        private FileMetaTemplateDefine m_FileMetaTemplateDefine = null;
        private MetaClass m_OwnerClass = null;
        private List<MetaClass> m_ConstraintMetaClassList = new List<MetaClass>();
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
                    var mc = ClassManager.instance.GetMetaDefineTypeByInputTemplateAndFileMeta(m_OwnerClass, m_FileMetaTemplateDefine.inClassNameTokenList[i]);
                    m_ConstraintMetaClassList.Add(mc.metaClass);
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
}
