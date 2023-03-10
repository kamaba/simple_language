
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
        public MetaFunction( MetaFunction mf )
        {
            m_MetaMemberParamCollection = new MetaDefineParamCollection(false, true);
            m_DefineMetaType = new MetaType(mf.metaDefineType);
            SetOwnerMetaClass(mf.ownerMetaClass);
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

    public partial class MetaFunctionCall : MetaBase
    {
        public MetaClass callerMetaClass => m_CallerMetaClass;
        public MetaFunction function => m_MetaFunction;
        public MetaInputParamCollection metaInputParamCollection => m_MetaInputParamCollection;

        private MetaVariable m_CallerMetaVariable = null;
        private MetaClass m_CallerMetaClass = null;
        private MetaFunction m_MetaFunction = null;
        private MetaInputParamCollection m_MetaInputParamCollection = null;
        public bool isConstruction { get; set; } = false;
        public bool isStaticCall { get; set; } = false;

        public MetaFunctionCall( MetaVariable mv, MetaFunction _fun, MetaInputParamCollection _metaInputParamCollection = null )
        {
            m_CallerMetaVariable = mv;
            m_MetaFunction = _fun;
            m_MetaInputParamCollection = _metaInputParamCollection;
            isStaticCall = false;
            ParseCSharp();
        }
        public MetaFunctionCall( MetaClass mc, MetaFunction _fun, MetaInputParamCollection _param = null )
        {
            m_CallerMetaClass = mc;
            m_MetaFunction = _fun;
            m_MetaInputParamCollection = _param;
            if ( _fun is MetaMemberFunction )
            {
                isConstruction = (_fun as MetaMemberFunction).isConstructInitFunction;
            }
            isStaticCall = true;
            ParseCSharp();
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
        public MetaClass GetMetaClass()
        {
            if(m_CallerMetaClass != null) { return m_CallerMetaClass; }
            return null;
        }
        public MetaType GetRetMetaType()
        {
            if(m_MetaFunction != null )
            {
                return m_MetaFunction.metaDefineType;
            }
            return null;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if( m_MetaFunction != null )
            {
                sb.Append(m_MetaFunction.ownerMetaClass.ToDefineTypeString() );
                sb.Append("." + m_MetaFunction.name + "(");
                int inputCount = m_MetaInputParamCollection?.metaParamList.Count ?? 0;
                List<MetaParam> mpList = m_MetaFunction.metaMemberParamCollection.metaParamList;
                int defineCount = m_MetaFunction.metaMemberParamCollection.count;
                for ( int i = 0; i < defineCount; i++ )
                {
                    if( i < inputCount)
                    {
                        sb.Append(m_MetaInputParamCollection.metaParamList[i].ToFormatString());
                    }
                    else
                    {
                        MetaDefineParam mdp = mpList[i] as MetaDefineParam;
                        if( mdp != null )
                        {
                            sb.Append(mdp.expressNode.ToFormatString() );
                        }
                    }
                    if (i < defineCount - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(")");
            }

            return sb.ToString();
        }
    }
}
