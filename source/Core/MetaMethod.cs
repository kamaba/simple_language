//****************************************************************************
//  File:      MethodMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System.Collections.Generic;
using System.Text;
using static SimpleLanguage.Core.MetaVariable;

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
    public class MetaFunction : MetaBase
    {
        public MetaType metaDefineType
        {
            get
            {
                if( m_ReturnMetaVariable != null )
                {
                    return m_ReturnMetaVariable.metaDefineType;
                }
                return null;
            }
        }
        public Token pingToken => m_PintTokenList.Count > 0 ? m_PintTokenList[0] : null;
        public virtual bool isStatic => m_IsStatic;
        public virtual bool isParsed => m_IsParsed;
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
                    }
                    m_FunctionAllName = sb.ToString();
                }
                return m_FunctionAllName;
            }
        }
        public virtual string virtualFunctionName => m_VirtualFunctionName;
        public MetaVariable thisMetaVariable => m_ThisMetaVariable;
        public MetaVariable returnMetaVariable => m_ReturnMetaVariable;
        public EMethodCallType methodCallType => m_MethodCallType;
        public MetaClass ownerMetaClass => m_OwnerMetaClass;
        public MetaDefineParamCollection metaMemberParamCollection => m_MetaMemberParamCollection;
        public MetaBlockStatements metaBlockStatements => m_MetaBlockStatements;
        public MetaDefineTemplateCollection metaMemberTemplateCollection => m_MetaMemberTemplateCollection;


        #region 属性
        protected MetaClass m_OwnerMetaClass = null;
        protected MetaBlockStatements m_MetaBlockStatements = null;
        protected MetaVariable m_ThisMetaVariable = null;
        protected MetaVariable m_ReturnMetaVariable = null;
        protected MetaDefineParamCollection m_MetaMemberParamCollection = new MetaDefineParamCollection();
        protected MetaDefineTemplateCollection m_MetaMemberTemplateCollection = new MetaDefineTemplateCollection();
        protected EMethodCallType m_MethodCallType = EMethodCallType.Local;
        private List<LabelData> m_LabelDataList = new List<LabelData>();
        protected bool m_IsStatic = false;
        #endregion

        #region Compile or Debug
        protected bool m_IsParsed = false;
        protected string m_FunctionAllName = null;
        protected string m_VirtualFunctionName = null;
        protected List<Token> m_PintTokenList = new List<Token>();
        #endregion
        public MetaFunction(MetaClass mc)
        {
            m_MetaMemberParamCollection = new MetaDefineParamCollection(false, true);
            SetOwnerMetaClass(mc);
        }
        public MetaFunction( MetaFunction mf ):base(mf)
        {
            m_IsParsed = mf.m_IsParsed;
            m_FunctionAllName = null;
            m_PintTokenList = mf.m_PintTokenList;
            m_VirtualFunctionName = mf.m_VirtualFunctionName;

            m_OwnerMetaClass = mf.m_OwnerMetaClass; 
            m_MetaBlockStatements = mf.m_MetaBlockStatements;
            if( mf.m_ThisMetaVariable != null )
            {
                m_ThisMetaVariable = new MetaVariable(mf.m_ThisMetaVariable);
            }
            if (mf.m_ReturnMetaVariable != null)
            {
                m_ReturnMetaVariable = new MetaVariable(mf.m_ReturnMetaVariable);
            }
            m_MetaMemberParamCollection = new MetaDefineParamCollection( mf.m_MetaMemberParamCollection );
            m_MetaMemberTemplateCollection = new MetaDefineTemplateCollection(mf.m_MetaMemberTemplateCollection);
            m_MethodCallType = mf.m_MethodCallType;
            m_LabelDataList = mf.m_LabelDataList;
            m_IsStatic = mf.m_IsStatic;
        }
        public override void SetDeep(int deep)
        {
            base.SetDeep(deep);
            m_MetaBlockStatements?.SetDeep(deep);
        }
        public virtual void SetOwnerMetaClass(MetaClass ownerclass)
        {
            if( ownerclass == null )
            {
                return;
            }
            if ( ownerclass == m_OwnerMetaClass )
            {
                return;
            }
            m_OwnerMetaClass = ownerclass;
            if (m_MetaBlockStatements != null )
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
        public void AddFrontMetaStatements(MetaStatements state)
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
        public void UpdateFunctionName()
        {
            m_FunctionAllName = "";
            m_FunctionAllName = functionAllName;
        }
        public virtual bool Parse()
        {
            return true;
        }
        public void SetReturnMetaClass( MetaClass metaClass )
        {
            if( m_ReturnMetaVariable != null )
            {
                m_ReturnMetaVariable.metaDefineType.SetMetaClass(metaClass);
            }
        }
        public MetaDefineParam GetMetaDefineParamByName( string name )
        {
            return m_MetaMemberParamCollection.GetMetaDefineParamByName(name);
        }
        public bool IsEqualMetaFunction( MetaFunction mf )
        {
            if( mf == null )
            {
                return false;
            }
            if( this.m_Name != mf.m_Name )
            {
                return false;
            }
            if( !this.m_MetaMemberTemplateCollection.IsEqualMetaDefineTemplateCollection( mf.metaMemberTemplateCollection) )
            {
                return false;
            }
            if( !this.m_MetaMemberParamCollection.IsEqualMetaDefineParamCollection( mf.metaMemberParamCollection ) )
            {
                return false;
            }

            return true;
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
        public virtual string ToStatementString()
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
