//****************************************************************************
//  File:      MetaVisitCall.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description:  create visit variable or method call!
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaMethodCall
    {
        public MetaVariable callerMetaVariable => m_CallerMetaVariable;
        public MetaClass callerMetaClass => m_CallerMetaClass;
        public MetaFunction function => m_MetaFunction;
        public MetaInputParamCollection metaInputParamCollection => m_MetaInputParamCollection;

        protected MetaVariable m_CallerMetaVariable = null;
        protected MetaClass m_CallerMetaClass = null;
        protected MetaFunction m_MetaFunction = null;
        protected MetaInputParamCollection m_MetaInputParamCollection = null;
        public bool isConstruction { get; set; } = false;
        public bool isStaticCall { get; set; } = false;

        public MetaMethodCall(MetaVariable mv, MetaFunction _fun, MetaInputParamCollection _metaInputParamCollection = null)
        {
            m_CallerMetaVariable = mv;
            m_MetaFunction = _fun;
            m_MetaInputParamCollection = _metaInputParamCollection;
            isStaticCall = false;
        }
        public MetaMethodCall(MetaClass mc, MetaFunction _fun, MetaInputParamCollection _param = null)
        {
            m_CallerMetaClass = mc;
            m_MetaFunction = _fun;
            m_MetaInputParamCollection = _param;
            var tmmf = _fun as MetaMemberFunction;
            if (tmmf != null )
            {
                isConstruction = tmmf.isConstructInitFunction;
                isStaticCall = tmmf.isStatic;
            }
            else
            {
                isStaticCall = false;
            }
        }
        public void SetCallerMetaVariable( MetaVariable metaVariable)
        {
            m_CallerMetaVariable = metaVariable;
        }
        public void Parse()
        {
            //if( param != null )
            //{
            //    param.ParseExpress();
            //}
        }
        public bool CheckMetaFunctionMatchInputParamCollection()
        {
            if (!m_MetaFunction.IsEqualMetaInputParamCollection(m_MetaInputParamCollection))
            {
                Debug.Write("Error 验证失败,函数与输入参数不匹配!!");
                return false;
            }
            return true;
        }
        public MetaType GeMetaDefineType()
        {
            return m_MetaFunction.metaDefineType;
        }
        public MetaClass GetMetaClass()
        {
            if (m_CallerMetaClass != null) { return m_CallerMetaClass; }
            return null;
        }
        public MetaType GetRetMetaType()
        {
            if (m_MetaFunction != null)
            {
                return m_MetaFunction.metaDefineType;
            }
            return null;
        }
        public string ToCommonString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_MetaFunction != null)
            {
                sb.Append(m_MetaFunction.name + "(");
                int inputCount = m_MetaInputParamCollection?.metaParamList.Count ?? 0;
                List<MetaParam> mpList = m_MetaFunction.metaMemberParamCollection.metaParamList;
                int defineCount = m_MetaFunction.metaMemberParamCollection.count;
                for (int i = 0; i < defineCount; i++)
                {
                    if (i < inputCount)
                    {
                        MetaInputParam mip = m_MetaInputParamCollection.metaParamList[i] as MetaInputParam;
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

            if (m_CallerMetaVariable != null)
            {
                sb.Append(m_CallerMetaVariable.name );
            }

            if (m_CallerMetaClass != null)
            {
                sb.Append("[" + m_CallerMetaClass.ToDefineTypeString() + "]");
            }
            sb.Append(".");
            sb.Append(ToCommonString());
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
            NewData,
            Enum,
        }
        public MetaConstExpressNode constValueExpress { get; private set; } = null;
        public EVisitType visitType { get; private set; }
        public MetaVariable variable { get; private set; } = null;
        public MetaVisitVariable visitVariable { get; private set; } = null;
        public MetaMethodCall methodCall { get; private set; } = null;
        public MetaClass callerMetaClass { get; private set; }= null;
        public MetaBraceOrBracketStatementsContent metaBraceStatementsContent { get; private set; } = null;

        public static MetaVisitNode CraeteByNew( MetaClass mc, MetaBraceOrBracketStatementsContent mb)
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.callerMetaClass = mc;
            vn.metaBraceStatementsContent = mb;
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
        public static MetaVisitNode CreateByNew(MetaMethodCall _methodCall, MetaBraceOrBracketStatementsContent mb )
        {
            MetaVisitNode vn = new MetaVisitNode();

            vn.metaBraceStatementsContent = mb;
            vn.visitType = EVisitType.NewClass;
            vn.methodCall = _methodCall;

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
        public MetaType GetMetaDefineType()
        {
            switch(visitType)
            {
                case EVisitType.MethodCall:
                    {
                        return methodCall.callerMetaVariable.metaDefineType;
                    }
                    case EVisitType.VisitVariable:
                    {
                        return visitVariable.metaDefineType;
                    }
                    case EVisitType.Variable:
                    {
                        return variable.metaDefineType;
                    }
                case EVisitType.NewClass:
                    {
                        return new MetaType(this.callerMetaClass);
                    }
                case EVisitType.NewData:
                    {
                        return new MetaType(this.callerMetaClass);
                    }
                default:
                    {
                        Console.Write("Error ---------" + visitType.ToString() );
                    }
                    break;
            }
            return new MetaType(CoreMetaClassManager.objectMetaClass);
        }
        public void SetMetaBraceStatementsContent(MetaBraceOrBracketStatementsContent mbbsc )
        {
            this.metaBraceStatementsContent = mbbsc;
        }
        public MetaClass GetMetaClass()
        {
            var mt = GetMetaDefineType();
            if( mt == null )
            {
                Console.Write("Error");
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
                case EVisitType.Enum:
                    {
                        return variable;
                    }
                default:
                    {
                        Debug.Write("Error MetaVisiCall IsNull!");
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
            //m_MetaInputParamCollection?.CaleReturnType();
            //if (m_CallNodeType == ECallNodeType.VariableName)
            //{
            //    MetaVariable mv = m_CurrentMetaBase as MetaVariable;
            //    //if (mv != null && (!mv.isTemplateClass && mv.metaDefineType.metaClass == null))
            //    //{
            //    //    //Debug.Write("Error 未解析到:" + mv.allName + "位置在:" + mv.ToFormatString());
            //    //    return;
            //    //}
            //}
            //else if (m_CallNodeType == ECallNodeType.DataName )
            //{
            //    MetaData md = m_CurrentMetaBase as MetaData;
            //    return;
            //}
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
                        sb.Append(this.variable.ToFormatString());
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
