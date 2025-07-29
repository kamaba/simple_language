//****************************************************************************
//  File:      MethodMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************

using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public enum EMethodCallType
    {
        Local,
        CSharp,
        CPlus,
    }

    public class LabelData
    {
        public string label;
        public MetaStatements frontStatements;
        public MetaStatements nextStatements;
    }
    public class MetaFunction : MetaVariable
    {
        public virtual string functionAllName {
            get
            {
                if (string.IsNullOrEmpty(m_FunctionAllName))
                {
                    StringBuilder sb = new StringBuilder();
                    if (m_OwnerMetaClass != null)
                    {
                        sb.Append(m_OwnerMetaClass.allClassName);
                        sb.Append(".");
                    }
                    sb.Append(name);
                    if (m_MetaMemberTemplateCollection.metaTemplateList.Count > 0)
                    {
                        sb.Append("<");
                        for (int i = 0; i < m_MetaMemberTemplateCollection.metaTemplateList.Count; i++)
                        {
                            var mtl = m_MetaMemberTemplateCollection.metaTemplateList[i];
                            sb.Append(mtl.name);
                            if (i < m_MetaMemberTemplateCollection.metaTemplateList.Count - 1)
                            {
                                sb.Append(",");
                            }
                        }
                        sb.Append(">");
                    }
                    if (m_MetaMemberParamCollection?.maxParamCount > 0)
                    {
                        sb.Append("_");
                        sb.Append(m_MetaMemberParamCollection.maxParamCount.ToString());
                        sb.Append("_");
                        sb.Append(m_MetaMemberParamCollection.ToParamTypeName());
                        sb.Append("_");
                        sb.Append(GetHashCode().ToString());
                    }
                    m_FunctionAllName = sb.ToString();
                }
                return m_FunctionAllName;
            }
        }
        public MetaVariable thisMetaVariable => m_ThisMetaVariable;
        public MetaVariable returnMetaVariable => m_ReturnMetaVariable;
        public EMethodCallType methodCallType => m_MethodCallType;
        public MetaDefineParamCollection metaMemberParamCollection => m_MetaMemberParamCollection;
        public MetaBlockStatements metaBlockStatements => m_MetaBlockStatements;
        public MetaDefineTemplateCollection metaMemberTemplateCollection => m_MetaMemberTemplateCollection;

        protected MetaBlockStatements m_MetaBlockStatements = null;
        protected MetaVariable m_ThisMetaVariable = null;
        protected MetaVariable m_ReturnMetaVariable = null;
        protected MetaDefineParamCollection m_MetaMemberParamCollection = null;
        protected MetaDefineTemplateCollection m_MetaMemberTemplateCollection = new MetaDefineTemplateCollection();
        protected EMethodCallType m_MethodCallType = EMethodCallType.Local;
        protected bool m_IsMustNeedReturnStatements = false;
        private List<LabelData> m_LabelDataList = new List<LabelData>();
        protected string m_FunctionAllName = null;
        public MetaFunction(MetaClass mc)
        {
            m_MetaMemberParamCollection = new MetaDefineParamCollection(false, true);
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            SetOwnerMetaClass(mc);
        }
        public MetaFunction( MetaFunction mf ):base(mf) 
        {
            m_MetaBlockStatements = mf.m_MetaBlockStatements;
            m_ThisMetaVariable = mf.m_ThisMetaVariable;
            m_ReturnMetaVariable = mf.m_ReturnMetaVariable;
            m_MetaMemberParamCollection = new MetaDefineParamCollection( mf.m_MetaMemberParamCollection );
            m_MetaMemberTemplateCollection = new MetaDefineTemplateCollection(mf.m_MetaMemberTemplateCollection);
            m_MethodCallType = mf.m_MethodCallType;
            m_IsMustNeedReturnStatements = mf.m_IsMustNeedReturnStatements;
            m_LabelDataList = mf.m_LabelDataList;
        }
        public override void SetOwnerMetaClass(MetaClass ownerclass)
        {
            base.SetOwnerMetaClass(ownerclass);
            if(m_MetaBlockStatements != null )
            {
                m_MetaBlockStatements.UpdateOwnerMetaClass(ownerclass);
            }
            if (m_ThisMetaVariable != null)
            {
                m_ThisMetaVariable.SetOwnerMetaClass(ownerclass);
            }
            if (m_ReturnMetaVariable != null)
            {
                m_ReturnMetaVariable.SetOwnerMetaClass(ownerclass);
            }
            if(m_MetaMemberParamCollection != null )
            {
                m_MetaMemberParamCollection.SetOwnerMetaClass(ownerclass);
            }
        }
        public void AddMetaStatements(MetaStatements state)
        {
            m_MetaBlockStatements.AddFrontStatements(state);
        }
        public List<MetaVariable> GetCalcMetaVariableList(bool isIncludeArgument = false)
        {
            List<MetaVariable> metaVarList = new List<MetaVariable>();
            if( isIncludeArgument )
            {
                for( int i = 0; i < m_MetaMemberParamCollection.metaDefineParamList.Count; i++ )
                {
                    var mdp = m_MetaMemberParamCollection.metaDefineParamList[i];
                    if( mdp != null )
                    {
                        metaVarList.Add(mdp.metaVariable);
                    }
                }
            }
            m_MetaBlockStatements?.GetCalcMetaVariableList(metaVarList);
            return metaVarList;
        }
        public LabelData GetLabelDataById(string label)
        {
            return m_LabelDataList.Find(a => a.label == label);
        }
        public LabelData AddLabelData(string label, MetaStatements nextState = null)
        {
            var ld = new LabelData() { label = label, nextStatements = nextState };
            m_LabelDataList.Add(ld);
            return ld;
        }
        public void UpdateLabelData(LabelData newld)
        {
            var ld = m_LabelDataList.Find(a => a.label == newld.label);
            if (ld != null)
            {
                ld.frontStatements = newld.frontStatements;
                ld.nextStatements = newld.nextStatements;
            }
        }
        public MetaDefineParam GetMetaDefineParamByName( string name )
        {
            return m_MetaMemberParamCollection.GetMetaDefineParamByName(name);
        }

        public virtual bool IsEqualMetaInputParamCollection(MetaInputParamCollection mpc)
        {
            if (m_MetaMemberParamCollection.IsEqualMetaInputParamCollection(mpc))
            {
                return true;
            }
            return false;
        }
        public virtual bool IsEqualMetaDefineParamCollection(MetaDefineParamCollection mdpc)
        {
            if (m_MetaMemberParamCollection.IsEqualMetaDefineParamCollection(mdpc))
            {
                return true;
            }
            return false;
        }
        public MetaTemplate GetMetaDefineTemplateByName( string name )
        {
            return m_MetaMemberTemplateCollection.GetMetaDefineTemplateByName(name);
        }
        public virtual bool IsEqualMetaTemplateCollectionAndMetaParamCollection( MetaInputTemplateCollection mitc, MetaDefineParamCollection mpc )
        {
            //if (m_MetaMemberParamCollection.IsEqualMetaTemplateAndParamCollection(mitc, mpc) )
            //{
            //    return true;
            //}
            return false;
        }
        public override string ToStatementString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(name);

            sb.Append(m_MetaMemberTemplateCollection?.ToFormatString());
            //sb.Append("( ");
            sb.Append(m_MetaMemberParamCollection.ToFormatString());
            //sb.Append(" )");

            return sb.ToString();
        }
    }
}
