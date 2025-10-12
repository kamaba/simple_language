//****************************************************************************
//  File:      MetaVisitCall.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description:  create visit variable or method call!
//****************************************************************************

using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaMethodCall
    {
        public MetaVariable loadMetaVariable => m_LoadMetaVariable;
        public MetaVariable storeMetaVariable => m_StoreMetaVariable;
        public MetaClass staticCallerMetaClass => m_StaticCallerMetaClass;
        public List<MetaType> staticMetaClassInputTemplateList => m_StaticMetaClassInputTemplateList;
        public MetaFunction function => m_VMCallMetaFunction;
        public MetaMemberFunction metaMemberFunction => m_MetaMemberFunction;
        public List<MetaExpressNode> metaInputParamList => m_MetaInputParamList;
        public List<MetaType> metaFunctionInputTemplateList => m_MetaFunctionInputTemplateList;


        protected MetaVariable m_LoadMetaVariable = null;
        protected MetaVariable m_StoreMetaVariable = null;
        protected MetaClass m_StaticCallerMetaClass = null;
        protected List<MetaType> m_StaticMetaClassInputTemplateList = new List<MetaType>();
        //模板或者是调用时的函数
        protected MetaFunction m_VMCallMetaFunction = null;
        //真实的成员函数
        protected MetaMemberFunction m_MetaMemberFunction = null;
        protected List<MetaExpressNode> m_MetaInputParamList = new List<MetaExpressNode>();
        protected List<MetaType> m_MetaFunctionInputTemplateList = new List<MetaType>();
        
        public MetaMethodCall( MetaClass staticMc, List<MetaType> staticMmitList,  MetaFunction _fun, List<MetaType> mpipList, MetaInputParamCollection _paramCollection, MetaVariable loadMv, MetaVariable storeMv )
        {
            m_StaticCallerMetaClass = staticMc;
            if( staticMmitList != null )
            {
                this.m_StaticMetaClassInputTemplateList = staticMmitList;
            }
            m_VMCallMetaFunction = _fun;
            //m_MetaInputParamList = _param;
            if( mpipList != null )
            {
                this.m_MetaFunctionInputTemplateList = mpipList;
            }

            List<MetaDefineParam> mpList = new();
            if( m_VMCallMetaFunction?.metaMemberParamCollection != null )
            {
                mpList = m_VMCallMetaFunction.metaMemberParamCollection.metaDefineParamList;
            }
            int defineCount = m_VMCallMetaFunction.metaMemberParamCollection.maxParamCount;
            int inputCount = _paramCollection != null ?_paramCollection.metaInputParamList.Count : 0;
            for (int i = 0; i < defineCount; i++)
            {
                if (i < inputCount)
                {
                    MetaInputParam mip = _paramCollection.metaInputParamList[i];
                    m_MetaInputParamList.Add(mip.express);
                }
                else
                {
                    MetaDefineParam mdp = mpList[i];
                    if (mdp != null)
                    {
                        m_MetaInputParamList.Add(mdp.expressNode);
                    }
                }
            }
            m_LoadMetaVariable = loadMv;
            m_StoreMetaVariable = storeMv;
        }
        public void SetStoreMetaVariable( MetaVariable mv )
        {
            this.m_StoreMetaVariable = mv;
        }
        public MetaType GeMetaDefineType()
        {
            return m_VMCallMetaFunction.metaDefineType;
        }
        public MetaFunction GetTemplateMemberFunction()
        {
            if( m_VMCallMetaFunction is MetaGenTempalteFunction mgtf )
            {
                return mgtf.sourceTemplateFunctionMetaMemberFunction;
            }
            return m_VMCallMetaFunction;
        }
        public string ToCommonString()
        {
            StringBuilder sb = new StringBuilder();
            if (m_VMCallMetaFunction != null)
            {
                sb.Append(m_VMCallMetaFunction.name + "(");
                int inputCount = m_MetaInputParamList.Count;
                for (int i = 0; i < inputCount; i++)
                {
                    sb.Append(m_MetaInputParamList[i].ToFormatString());
                    if (i < inputCount - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(")");
            }
            return sb.ToString();

        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if( this.loadMetaVariable != null )
            {
                sb.Append("[");
                sb.Append(this.m_VMCallMetaFunction.ownerMetaClass.allClassName);
                sb.Append("]");

                sb.Append(this.loadMetaVariable.name);
                sb.Append(".");
            }
            else
            {
                sb.Append(this.m_VMCallMetaFunction.ownerMetaClass.allClassName);
                sb.Append(".");
            }
            if (m_VMCallMetaFunction != null)
            {
                sb.Append(m_VMCallMetaFunction.name + "(");
                int inputCount = m_MetaInputParamList.Count;
                for (int i = 0; i < inputCount; i++)
                {
                    sb.Append(m_MetaInputParamList[i].ToFormatString());
                    if (i < inputCount - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(")");
            }

            return sb.ToString();
        }
        public override string ToString()
        {
            return ToFormatString();
        }
    }

    public class MetaVisitNode
    {
        public enum EVisitType
        {
            ConstValue,
            Variable,
            VisitVariable,
            IteratorVariable,
            MethodCall,
            New,
            Enum,
        }
        public MetaConstExpressNode constValueExpress { get; private set; } = null;
        public EVisitType visitType { get; private set; }
        public MetaVariable variable { get; private set; } = null;
        public MetaVisitVariable visitVariable { get; private set; } = null;
        public MetaMethodCall methodCall { get; private set; } = null;
        //public MetaClass callerMetaClass => m_CallerMetaClass;
        public MetaType callMetaType => m_CallMetaType;
        public MetaBraceOrBracketStatementsContent metaBraceStatementsContent => m_MetaBraceStatementsContent;

        private MetaBraceOrBracketStatementsContent m_MetaBraceStatementsContent = null;
        protected MetaType m_ReturnMetaType = null;
        //protected MetaClass m_CallerMetaClass = null;
        protected MetaType m_CallMetaType = null; //该变量，一般是为 T t = new() 这种情况准备的

        public static MetaVisitNode CreateByNewTemplate(MetaType mt, MetaFunction mf, MetaVariable mv)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            vn.visitType = EVisitType.New;
            vn.variable = mv;
            vn.methodCall = new MetaMethodCall(mt.metaClass, null, mf, null, null, null, mv);
            return vn;
        }
        public static MetaVisitNode CraeteByNewClass(MetaType mt, MetaBraceOrBracketStatementsContent mb, MetaVariable mv = null )
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            vn.m_MetaBraceStatementsContent = mb;
            vn.visitType = EVisitType.New;
            vn.variable = mv;
            if (mt.metaClass is MetaGenTemplateClass mgtc)
            {
                vn.m_ReturnMetaType = new MetaType(mgtc);
            }

            return vn;
        }
        public static MetaVisitNode CraeteByNewData(MetaType mt, MetaBraceOrBracketStatementsContent mb)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_CallMetaType = mt;
            vn.m_MetaBraceStatementsContent = mb;
            vn.visitType = EVisitType.New;

            return vn;
        }
        public static MetaVisitNode CreateByConstExpress(  MetaConstExpressNode constExpress, MetaVariable _variable)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.constValueExpress = constExpress;
            vn.variable = _variable;
            vn.visitType = EVisitType.ConstValue;

            return vn;
        }
        public static MetaVisitNode CreateByEnumDefaultValue( MetaType mt, MetaVariable _variable )
        {
            MetaVisitNode vn = new MetaVisitNode();
            vn.variable = _variable;
            vn.visitType = EVisitType.Variable;

            return vn;
        }
        public static MetaVisitNode CreateByMethodCall( MetaMethodCall _methodCall)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.MethodCall;
            vn.methodCall = _methodCall;

            return vn;
        }
        public static MetaVisitNode CreateByVisitVariable(MetaVisitVariable _variale)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.VisitVariable;
            vn.visitVariable = _variale;

            return vn;
        }
        public static MetaVisitNode CreateByVariable(MetaVariable _variale, MetaType callerMt = null)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.Variable;
            vn.variable = _variale;
            vn.m_CallMetaType = callerMt;

            return vn;
        }
        public static MetaVisitNode CreateByThis(MetaVariable _variale)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.Variable;
            vn.variable = _variale;
            vn.m_CallMetaType = null;

            return vn;
        }
        public static MetaVisitNode CreateByBase(MetaVariable _variale)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.Variable;
            vn.variable = _variale;
            vn.m_CallMetaType = null;

            return vn;
        }
        public void SetMethodCall( MetaMethodCall _methodCall)
        {
            this.methodCall = _methodCall;
        }
        public MetaVariable GetOrgTemplateMetaVariable()
        {
            if (variable == null) { return null; }

            var t = variable;
            while(t != null )
            {
                if( t.sourceMetaVariable == null  )
                {
                    break;
                }
                t = t.sourceMetaVariable;
            }
            return t;
        }
        public MetaType GetMetaDefineType()
        {
            if( m_ReturnMetaType != null )
            {
                return m_ReturnMetaType;
            }
            switch(visitType)
            {
                case EVisitType.MethodCall:
                    {
                        if( methodCall.metaMemberFunction != null )
                        {
                            return methodCall.metaMemberFunction.returnMetaVariable.metaDefineType;
                        }
                        return methodCall.function.returnMetaVariable.metaDefineType;
                    }
                    case EVisitType.VisitVariable:
                    {
                        return visitVariable.metaDefineType;
                    }
                    case EVisitType.Variable:
                    {
                        return this.variable.metaDefineType;
                    }
                case EVisitType.New:
                    {
                        return m_CallMetaType;
                    }
                default:
                    {
                        Log.AddInStructMeta(EError.None, "Error ---------" + visitType.ToString() );
                    }
                    break;
            }
            return new MetaType(CoreMetaClassManager.objectMetaClass);
        }
        public MetaClass GetMetaClass()
        {
            var mt = GetMetaDefineType();
            if( mt == null )
            {
                Log.AddInStructMeta(EError.None, "Error");
                return null;
            }
            return mt.metaClass;
        }
        public MetaVariable GetRetMetaVariable()
        {
            switch( visitType )
            {
                case EVisitType.Variable:
                    {
                        return variable;
                    }
                case EVisitType.MethodCall:
                    {
                        return methodCall.function.returnMetaVariable;
                    }
                case EVisitType.New:
                    {
                        return variable;
                    }
                case EVisitType.Enum:
                    {
                        return variable;
                    }
                default:
                    {
                        Log.AddInStructMeta(EError.None, "Error MetaVisiCall IsNull!");
                    }
                    break;
            }
            return null;
        }

        public int CalcParseLevel(int level)
        {
            //if (m_CurrentMetaBase == null) return level;
            //var mv = m_CurrentMetaBase as MetaMemberVariable;
            //if (mv != null)
            //{
            //    return mv.CalcParseLevelBeCall(level);
            //}
            return level;
        }
        public void CalcReturnType()
        {
            GetMetaDefineType();
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder(); 

            switch( visitType )
            {
                case EVisitType.MethodCall:
                    {
                        sb.Append(this.methodCall.ToFormatString() );
                    }
                    break;
                case EVisitType.VisitVariable:
                    {
                        sb.Append(this.visitVariable.ToFormatString());
                    }
                    break;
                case EVisitType.ConstValue:
                    {
                        sb.Append(this.constValueExpress.value.ToString());
                    }
                    break;
                case EVisitType.Variable:
                    {
                        sb.Append(this.variable.ToFormatString());
                    }
                    break;
                case EVisitType.New:
                    {
                        sb.Append(this.m_CallMetaType.ToString());
                    }
                    break;
                default:
                    {
                        sb.Append("Error MetaVisitCall Default Parse!");
                    }
                    break;
            }

            return sb.ToString();
        }
    }
}
