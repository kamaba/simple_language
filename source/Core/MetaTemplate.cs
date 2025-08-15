//****************************************************************************
//  File:      MetaTemplate.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaTemplate : MetaBase
    {
        public bool isInFunction => m_IsInFunction;
        public MetaClass extendsMetaClass => m_ExtendsMetaClass;
        public MetaClass ownerClass => m_OwnerClass;

        protected FileMetaTemplateDefine m_FileMetaTemplateDefine = null;
        protected MetaClass m_OwnerClass = null;
        protected MetaClass m_ExtendsMetaClass = null;
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
                if( m_FileMetaTemplateDefine.inClassNameTemplateNode != null )
                {
                    m_ExtendsMetaClass = ClassManager.instance.GetMetaClassByInputTemplateAndFileMeta(m_OwnerClass, m_FileMetaTemplateDefine.inClassNameTemplateNode );
                }
                else
                {
                    m_ExtendsMetaClass = CoreMetaClassManager.objectMetaClass;
                }
            }
        }
        public void SetInConstraintMetaClass(MetaClass mc)
        {
            m_ExtendsMetaClass = mc;
        }
        public bool IsInConstraintMetaClass(MetaClass mc)
        {
            return m_ExtendsMetaClass != null;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_Name);
            if (m_ExtendsMetaClass != null )
            {
                sb.Append(" extends ");
                sb.Append(m_ExtendsMetaClass.allClassName );
            }

            return sb.ToString();
        }
    }

    public class TemplateBindMetaType
    {
        public List<MetaType> bindMetaType;
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
            return m_MetaType.metaClass.allClassName == mt.metaClass.allClassName;
        }
        public string ToDefineTypeString()
        {
            StringBuilder sb = new StringBuilder();
            if( m_MetaType.isTemplate )
            {
                sb.Append(m_MetaType.metaTemplate.name);
            }
            else
            {
                sb.Append(m_MetaType.metaClass.ToDefineTypeString());
            }           

            return sb.ToString();
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_MetaType.metaClass.name);
            return sb.ToString();
        }
    }


    public class MetaDefineTemplateCollection
    {
        public List<MetaTemplate> metaTemplateList => m_MetaTemplateList;
        public int count { get { return m_MetaTemplateList.Count; } }

        protected List<MetaTemplate> m_MetaTemplateList = new List<MetaTemplate>();

        public MetaDefineTemplateCollection()
        {

        }
        public MetaDefineTemplateCollection(MetaDefineTemplateCollection mdtc)
        {
            for( int i = 0; i < mdtc.m_MetaTemplateList.Count; i++ )
            {
                m_MetaTemplateList.Add(mdtc.m_MetaTemplateList[i]);
            }
        }
        public MetaTemplate GetMetaDefineTemplateByName(string _name)
        {
            for (int i = 0; i < m_MetaTemplateList.Count; i++)
            {
                if (m_MetaTemplateList[i].name == _name)
                    return m_MetaTemplateList[i];
            }
            return null;
        }
        public bool IsEqualMetaDefineTemplateCollection(MetaDefineTemplateCollection mpc )
        {
            if(mpc == null )
            {
                return m_MetaTemplateList.Count == 0;
            }
            if (m_MetaTemplateList.Count == mpc.m_MetaTemplateList.Count)
            {
                if( m_MetaTemplateList.Count == 0 )
                {
                    return true;
                }

                for (int i = 0; i < m_MetaTemplateList.Count; i++)
                {
                    MetaTemplate a = m_MetaTemplateList[i];
                    MetaTemplate b = mpc.m_MetaTemplateList[i];
                    if (MatchMetaDefineTemplate(a, b))
                        return true;
                }
            }

            return false;
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
        public virtual bool MatchMetaDefineTemplate(MetaTemplate a, MetaTemplate b)
        {
            if (a.name == b.name)
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
