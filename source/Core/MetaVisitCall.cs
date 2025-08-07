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
        public MetaGenTemplateClass callerInstanceClass => m_CallerInstanceClass;
        public MetaFunction function => m_VMCallMetaFunction;
        public MetaMemberFunction metaMemberFunction => m_MetaMemberFunction;
        public MetaInputParamCollection metaInputParamCollection => m_MetaInputParamCollection;

        protected MetaVariable m_LoadMetaVariable = null;
        protected MetaVariable m_StoreMetaVariable = null;
        protected MetaGenTemplateClass m_CallerInstanceClass = null;
        //模板或者是调用时的函数
        protected MetaFunction m_VMCallMetaFunction = null;
        //真实的成员函数
        protected MetaMemberFunction m_MetaMemberFunction = null;
        protected MetaInputParamCollection m_MetaInputParamCollection = null;
        public MetaMethodCall(MetaGenTemplateClass genGlass, MetaMemberFunction _fun, MetaInputParamCollection _param )
        {
            m_CallerInstanceClass = genGlass;
            m_MetaMemberFunction = _fun;
            m_VMCallMetaFunction = _fun.sourceMetaMemberFunction;
            m_MetaInputParamCollection = _param;
            //m_CallerMetaVariable = mv;
            //var tmmf = _fun as MetaMemberFunction;
            //if (tmmf != null)
            //{
            //    m_IsConstruction = tmmf.isConstructInitFunction;
            //    //m_MethodCallStackType = tmmf.isStatic ? EMethodCallStackType.StaticStack : EMethodCallStackType.DynamicStack;
            //}
            //else
            //{
            //    //m_MethodCallStackType = EMethodCallStackType.DynamicStack;
            //}
        }
        public MetaMethodCall( MetaGenTemplateClass genGlass, MetaFunction _fun, MetaInputParamCollection _param, MetaVariable loadMv, MetaVariable storeMv )
        {
            m_CallerInstanceClass = genGlass;
            m_VMCallMetaFunction = _fun;
            m_MetaInputParamCollection = _param;
            m_LoadMetaVariable = loadMv;
            m_StoreMetaVariable = storeMv;
            //var tmmf = _fun as MetaMemberFunction;
            //if (tmmf != null)
            //{
            //    m_IsConstruction = tmmf.isConstructInitFunction;
            //    //m_MethodCallStackType = tmmf.isStatic ? EMethodCallStackType.StaticStack : EMethodCallStackType.DynamicStack;
            //}
            //else
            //{
            //    //m_MethodCallStackType = EMethodCallStackType.DynamicStack;
            //}
        }
        //public MetaMethodCall(MetaGenTemplateClass mc, MetaMemberFunction _fun, MetaInputParamCollection _param = null)
        //{
        //    m_CallerInstanceClass = mc;
        //    m_CallerMetaClass = mc.metaTemplateClass;
        //    m_MetaFunction = _fun;
        //    m_MetaInputParamCollection = _param;
        //    //m_IsConstruction = _fun.isConstructInitFunction;
        //    //m_MethodCallStackType = _fun.isStatic ? EMethodCallStackType.StaticStack : EMethodCallStackType.DynamicStack;
        //}
        public MetaFunction GetRealMetaFunction()
        {
            if( m_MetaMemberFunction != null )
            {
                return m_MetaMemberFunction;
            }
            return m_VMCallMetaFunction;
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
                        MetaDefineParam mdp = mpList[i] as MetaDefineParam;
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

            sb.Append(this.m_VMCallMetaFunction.name);
            sb.Append("( ");
            sb.Append(this.metaInputParamCollection.ToFormatString() );
            sb.Append(" )");

            return sb.ToString();
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
            NewClass,
            NewTemplate,
            NewData,
            Enum,
        }
        public MetaConstExpressNode constValueExpress { get; private set; } = null;
        public EVisitType visitType { get; private set; }
        public MetaVariable variable { get; private set; } = null;
        public MetaVisitVariable visitVariable { get; private set; } = null;
        public MetaMethodCall methodCall { get; private set; } = null;
        public MetaClass callerMetaClass { get; private set; }= null;

        public MetaTemplate callerMetaTemplate { get; private set; } = null;
        public MetaBraceOrBracketStatementsContent metaBraceStatementsContent => m_MetaBraceStatementsContent;

        private MetaBraceOrBracketStatementsContent m_MetaBraceStatementsContent = null;
        protected MetaType m_ReturnMetaType = null;

        public static MetaVisitNode CreateByNewTemplate( MetaTemplate template, MetaVariable mv)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.callerMetaTemplate = template;
            vn.visitType = EVisitType.NewTemplate;
            vn.variable = mv;

            return vn;

        }
        public static MetaVisitNode CraeteByNewClass(MetaClass mc, MetaBraceOrBracketStatementsContent mb, MetaVariable mv = null )
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.callerMetaClass = mc;
            vn.m_MetaBraceStatementsContent = mb;
            vn.visitType = EVisitType.NewClass;
            vn.variable = mv;
            if( mc is MetaGenTemplateClass mgtc )
            {
                vn.m_ReturnMetaType = new MetaType(mgtc);
            }

            return vn;
        }
        public static MetaVisitNode CraeteByNewData(MetaClass mc, MetaBraceOrBracketStatementsContent mb)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.callerMetaClass = mc;
            vn.m_MetaBraceStatementsContent = mb;
            vn.visitType = EVisitType.NewData;

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
        public static MetaVisitNode CreateByEnumDefaultValue( MetaEnum me, MetaVariable _variable )
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
        public static MetaVisitNode CreateByVariable(MetaVariable _variale)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.visitType = EVisitType.Variable;
            vn.variable = _variale;

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
                case EVisitType.NewClass:
                    {
                        return new MetaType(this.callerMetaClass);
                    }
                case EVisitType.NewData:
                    {
                        return new MetaType(this.callerMetaClass);
                    }
                case EVisitType.NewTemplate:
                    {
                        return new MetaType(this.callerMetaClass);
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
                case EVisitType.NewClass:
                    {
                        return variable;
                    }
                case EVisitType.NewTemplate:
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
                case EVisitType.NewClass:
                    {
                        sb.Append(this.callerMetaClass.ToFormatString());
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
