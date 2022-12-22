
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
        protected int m_ParamCount = 0;
        protected int m_TemplateCount = 0;
        protected EMethodCallType m_MethodCallType = EMethodCallType.Local;
        protected bool m_IsMustNeedReturnStatements = false;
        protected IRMethod m_IRMethod = null;
        private List<LabelData> labelDataList = new List<LabelData>();

        public void AddMetaStatements(MetaStatements state )
        {
            m_MetaBlockStatements.AddFrontStatements(state);
        }
        public void SetIRMethod( IRMethod method )
        {
            m_IRMethod = method;
        }
        public List<MetaVariable> GetCalcMetaVariableList( bool isIncludeArgument = false )
        {
            List<MetaVariable> metaVarList = new List<MetaVariable>();
            m_MetaBlockStatements?.GetCalcMetaVariableList(metaVarList, isIncludeArgument);
            return metaVarList;
        }
        public LabelData GetLabelDataById( string label )
        {
            return labelDataList.Find(a => a.label == label);
        }
        public LabelData AddLabelData( string label, MetaStatements nextState = null )
        {
            var ld = new LabelData() { label = label, nextStatements = nextState };
            labelDataList.Add(ld );
            return ld;
        }
        public void UpdateLabelData( LabelData newld )
        {
            var ld = labelDataList.Find(a => a.label == newld.label);
            if( ld != null )
            {
                ld.frontStatements = newld.frontStatements;
                ld.nextStatements = newld.nextStatements;
            }
        }
        public MetaFunction(MetaClass mc)
        {
            m_MetaMemberParamCollection = new MetaDefineParamCollection(false, true);
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            SetOwnerMetaClass(mc);           
        }
        public MetaDefineParam GetMetaDefineParamByName( string name )
        {
            return m_MetaMemberParamCollection.GetMetaDefineParamByName(name);
        }

        public virtual bool IsEqualMetaParamCollection( MetaParamCollectionBase mpc )
        {
            if(m_MetaMemberParamCollection.IsEqualMetaParamCollection(mpc) )
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

        public string ToStatementString()
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

    public partial class MetaFunctionCall : MetaBase
    {
        public MetaFunction function => m_MetaFunction;
        public MetaInputParamCollection metaInputParamCollection => m_MetaInputParamCollection;

        private MetaFunction m_MetaFunction = null;
        private MetaInputParamCollection m_MetaInputParamCollection = null;
        public bool isConstruction { get; set; } = false;

        public MetaFunctionCall( MetaClass mc, MetaFunction _fun, MetaInputParamCollection _param = null )
        {
            m_MetaFunction = _fun;
            m_MetaInputParamCollection = _param;
            if ( _fun is MetaMemberFunction )
            {
                isConstruction = (_fun as MetaMemberFunction).isConstructInitFunction;
            }
            ParseCSharp();
        }
        public void SetMetaInputParamCollection(MetaInputParamCollection mipc )
        {
            m_MetaInputParamCollection = mipc;
        }
        public void Parse()
        {
            //if( param != null )
            //{
            //    param.ParseExpress();
            //}
        }
        public MetaType GeMetaDefineType()
        {
            return m_MetaFunction.metaDefineType;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_MetaFunction?.allName );
            sb.Append(m_MetaInputParamCollection.ToFormatString());

            return sb.ToString();
        }
    }
}
