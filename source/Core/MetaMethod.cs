
using SimpleLanguage.IR;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
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
                return allName;
            }
        }

        public string irMethodName
        {
            get
            {
                return allName;
            }
        }

        public IRMethod irMethod => m_IRMethod;
        public MetaVariable thisMetaVariable => m_ThisMetaVariable;
        public MetaVariable returnMetaVariable => m_ReturnMetaVariable;
        public bool fixedParam { get; set; } = true;
        public EMethodCallType methodCallType => m_MethodCallType;
        public MetaDefineParamCollection metaMemberParamCollection => m_MetaMemberParamCollection;
        public MetaBlockStatements metaBlockStatements => m_MetaBlockStatements;

        protected MetaBlockStatements m_MetaBlockStatements = null;
        protected MetaVariable m_ThisMetaVariable = null;
        protected MetaVariable m_ReturnMetaVariable = null;
        protected MetaDefineParamCollection m_MetaMemberParamCollection = null;
        protected MetaDefineTemplateCollection m_MetaMemberTemplateCollection = new MetaDefineTemplateCollection();
        protected EMethodCallType m_MethodCallType = EMethodCallType.Local;
        protected bool m_IsMustNeedReturnStatements = false;
        protected IRMethod m_IRMethod = null;
        private List<LabelData> m_LabelDataList = new List<LabelData>();

        public MetaFunction(MetaClass mc)
        {
            m_IRMethod = new IRMethod(this);
            m_MetaMemberParamCollection = new MetaDefineParamCollection(false, true);
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            SetOwnerMetaClass(mc);           
        }

        public void AddMetaStatements(MetaStatements state)
        {
            m_MetaBlockStatements.AddFrontStatements(state);
        }
        public List<MetaVariable> GetCalcMetaVariableList(bool isIncludeArgument = false)
        {
            List<MetaVariable> metaVarList = new List<MetaVariable>();
            m_MetaBlockStatements?.GetCalcMetaVariableList(metaVarList, isIncludeArgument);
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
        public virtual bool IsEqualMetaTemplateCollectionAndMetaParamCollection( MetaInputTemplateCollection mitc, MetaParamCollectionBase mpc )
        {
            if (m_MetaMemberParamCollection.IsEqualMetaTemplateAndParamCollection(mitc, mpc) )
            {
                return true;
            }
            return false;
        }
        public bool TranslateIR()
        {
            m_IRMethod = IRManager.instance.TranslateIRByFunction(this);

            return m_IRMethod != null;
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
