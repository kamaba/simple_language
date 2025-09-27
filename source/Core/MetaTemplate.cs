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
        public int index => m_Index;
        public bool isInFunction => m_IsInFunction;
        public MetaClass extendsMetaClass => m_ExtendsMetaClass;
        public MetaClass ownerClass => m_OwnerClass;

        protected FileMetaTemplateDefine m_FileMetaTemplateDefine = null;
        protected MetaClass m_OwnerClass = null;
        protected MetaClass m_ExtendsMetaClass = null;
        protected bool m_IsInFunction = false;
        protected int m_Index = -1;
        //该属性为了绑定new的 _init_ 的方法 然后在实例化模板类的时候，去检查该类，的相关方法，是否有 private _init_的方法，然后就可以确定
        // T t = new() 这时候， 是否有出错 的现象 例 Level<T>{ test(){ T t = new() } } TC{ private _init_(){} } main(){ Level<TC> tc = new()
        // 这时候要进行报错处理，因为在Level.test()里边，有对T进行new的注册，是_init_方法，但Level<TC> 
        protected List<MetaMethodCall> m_BindConstructFunction = new List<MetaMethodCall>();
        public MetaTemplate( MetaClass mc, FileMetaTemplateDefine fmtd, int index )
        {
            m_Name = fmtd.name;
            m_FileMetaTemplateDefine = fmtd;
            m_OwnerClass = mc;
            this.m_Index = index;
        }
        public MetaTemplate( MetaClass mc, string name )
        {
            m_Name = name;
            m_OwnerClass = mc;
        }
        public void SetIndex( int index )
        {
            this.m_Index = index;
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
        public void AddBindConstructFunction(MetaMethodCall mmc)
        {
            if (!m_BindConstructFunction.Contains(mmc))
            {
                m_BindConstructFunction.Add(mmc);
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

    public sealed class MetaGenTemplate
    {
        public string name => m_MetaTemplate != null ? m_MetaTemplate.name : "";
        public MetaType metaType => m_MetaType;
        public MetaTemplate metaTemplate => m_MetaTemplate;

        private MetaType m_MetaType = null;
        private MetaTemplate m_MetaTemplate = null;
        public MetaGenTemplate(MetaTemplate mt)
        {
            m_MetaTemplate = mt;
        }
        public MetaGenTemplate(MetaTemplate mt, MetaType mtype)
        {
            m_MetaTemplate = mt;
            m_MetaType = mtype;
        }

        public bool EqualWithMetaType( MetaType mt )
        {
            return m_MetaType.metaClass.allClassName == mt.metaClass.allClassName;
        }
        public void SetMetaType( MetaType mt ) 
        {
            m_MetaType = mt;
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
                sb.Append(m_MetaType.metaClass.ToString() );
            }           

            return sb.ToString();
        }
        public string ToFormatString()
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
