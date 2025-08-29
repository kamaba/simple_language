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
        public MetaType callerMetaType => m_CallerMetaType;
        public MetaFunction function => m_VMCallMetaFunction;
        public MetaMemberFunction metaMemberFunction => m_MetaMemberFunction;
        public MetaInputParamCollection metaInputParamCollection => m_MetaInputParamCollection;

        protected MetaVariable m_LoadMetaVariable = null;
        protected MetaVariable m_StoreMetaVariable = null;
        protected MetaType m_CallerMetaType = null;
        //模板或者是调用时的函数
        protected MetaFunction m_VMCallMetaFunction = null;
        //真实的成员函数
        protected MetaMemberFunction m_MetaMemberFunction = null;
        protected MetaInputParamCollection m_MetaInputParamCollection = null;
        
        public MetaMethodCall( MetaType mt, MetaFunction _fun, MetaInputParamCollection _param, MetaVariable loadMv, MetaVariable storeMv )
        {
            m_CallerMetaType = mt;
            if ( _fun is MetaMemberFunction mmf )
            {
                m_VMCallMetaFunction = mmf.sourceMetaMemberFunction != null ? mmf.sourceMetaMemberFunction : mmf;
            }
            else
            {
                m_VMCallMetaFunction = _fun;
            }
            m_MetaInputParamCollection = _param;
            if(m_MetaInputParamCollection == null && _fun != null )
            {
                m_MetaInputParamCollection = new MetaInputParamCollection(m_VMCallMetaFunction.ownerMetaClass, null);
            }
            m_LoadMetaVariable = loadMv;
            m_StoreMetaVariable = storeMv;
        }
        public void SetStoreMetaVariable( MetaVariable mv )
        {
            this.m_StoreMetaVariable = mv;
        }
        public bool CheckMetaFunctionMatchInputParamCollection()
        {
            if (!m_VMCallMetaFunction.IsEqualMetaInputParamCollection(m_MetaInputParamCollection))
            {
                Log.AddInStructMeta(EError.None, "Error 验证失败,函数与输入参数不匹配!!");
                return false;
            }
            return true;
        }
        public MetaType GeMetaDefineType()
        {
            return m_VMCallMetaFunction.metaDefineType;
        }
        public string ToCommonString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_VMCallMetaFunction != null)
            {
                sb.Append(m_VMCallMetaFunction.name + "(");
                int inputCount = m_MetaInputParamCollection?.metaInputParamList.Count ?? 0;
                List<MetaDefineParam> mpList = m_VMCallMetaFunction.metaMemberParamCollection.metaDefineParamList;
                int defineCount = m_VMCallMetaFunction.metaMemberParamCollection.maxParamCount;
                for (int i = 0; i < defineCount; i++)
                {
                    if (i < inputCount)
                    {
                        MetaInputParam mip = m_MetaInputParamCollection.metaInputParamList[i];
                        sb.Append(mip.ToStatementString());
                    }
                    else
                    {
                        MetaDefineParam mdp = mpList[i];
                        if (mdp != null)
                        {
                            sb.Append(mdp.expressNode?.ToFormatString());
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
            sb.Append(this.m_VMCallMetaFunction.name);
            sb.Append("( ");
            sb.Append(this.metaInputParamCollection.ToFormatString() );
            sb.Append(" )");

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
        public MetaType staticMetaType => m_StaticMetaType;
        public MetaBraceOrBracketStatementsContent metaBraceStatementsContent => m_MetaBraceStatementsContent;

        private MetaBraceOrBracketStatementsContent m_MetaBraceStatementsContent = null;
        protected MetaType m_ReturnMetaType = null;
        //protected MetaClass m_CallerMetaClass = null;
        protected MetaType m_StaticMetaType  = null; //该变量，一般是为 T t = new() 这种情况准备的

        public static MetaVisitNode CreateByNewTemplate(MetaType mt, MetaFunction mf, MetaVariable mv)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_StaticMetaType = mt;
            vn.visitType = EVisitType.New;
            vn.variable = mv;
            vn.methodCall = new MetaMethodCall(mt, mf, null, null, mv);
            return vn;
        }
        public static MetaVisitNode CraeteByNewClass(MetaType mt, MetaBraceOrBracketStatementsContent mb, MetaVariable mv = null )
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_StaticMetaType = mt;
            vn.m_MetaBraceStatementsContent = mb;
            vn.visitType = EVisitType.New;
            vn.variable = mv;
            if(mt.metaClass is MetaGenTemplateClass mgtc )
            {
                vn.m_ReturnMetaType = new MetaType(mgtc);
            }

            return vn;
        }
        public static MetaVisitNode CraeteByNewData(MetaType mt, MetaBraceOrBracketStatementsContent mb)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.m_StaticMetaType = mt;
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
            vn.m_StaticMetaType = callerMt;

            return vn;
        }
        public static MetaVisitNode CreateByThis(MetaVariable _variale, MetaType callerMt = null)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.Variable;
            vn.variable = _variale;
            vn.m_StaticMetaType = callerMt;

            return vn;
        }
        public static MetaVisitNode CreateByBase(MetaVariable _variale, MetaType callerMt = null)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.Variable;
            vn.variable = _variale;
            vn.m_StaticMetaType = callerMt;

            return vn;
        }
        public void SetMethodCall( MetaMethodCall _methodCall)
        {
            this.methodCall = _methodCall;
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
                        return m_StaticMetaType;
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
                        sb.Append(this.staticMetaType.ToString());
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
