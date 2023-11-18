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
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaVisitVariable : MetaVariable
    {
        /*
         * 访问变量 一般使用 $x $x 必须先定义
         * int a = 20; Array arr = Array<int>( 1,2,3); 
         * int b = arr.$a; 这里的$a就是访问变量，使用arr为localMV, 使用m_VisitMetaVariable 是a 如果是常量，则保存
         * 常量的  arr.$0  m_VisitMV = null; m_AtName = "0";  返回值本身就是一个变量，相当于已经访问过了，在defineType
         * 中，返回模版类中的名称
         */
        public enum EVisitType
        {
            Link,
            AT
        }
        public MetaVariable localMetaVariable => m_LocalMetaVariable;
        public MetaVariable visitMetaVariable => m_VisitMetaVariable;

        MetaVariable m_LocalMetaVariable = null;
        private EVisitType m_VisitType = EVisitType.AT;
        MetaVariable m_VisitMetaVariable = null;
        string m_AtName = "";

        public MetaVisitVariable( MetaVariable lmv, MetaVariable target )
        {
            m_VisitType = EVisitType.Link;
            m_LocalMetaVariable = lmv;
            m_VisitMetaVariable = target;
            m_DefineMetaType = target.metaDefineType;
        }
        public int GetIRMemberIndex()
        {
            var mmv = m_VisitMetaVariable as MetaMemberVariable;
            if (mmv != null ) 
            {
                return mmv.ownerMetaClass.GetLocalMemberVariableIndex(mmv);
            }
            return -1;
        }
        public MetaVisitVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaVariable vmv)
        {
            m_Name = _name;
            m_AtName = _name;
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_LocalMetaVariable = lmv;
            if (lmv.isArray)
            {
                if (vmv == null && string.IsNullOrEmpty(m_AtName))
                {
                    Console.WriteLine("Error VisitMetaVariable访问变量访问位置不能同时为空!!");
                    return;
                }
                m_VisitMetaVariable = vmv;

                var gmit = m_LocalMetaVariable.metaDefineType.GetMetaInputTemplateByIndex();
                if (gmit == null)
                {
                    Console.WriteLine("Error 访问的Array中，没有找到模版 名称!!");
                    return;
                }
                m_DefineMetaType = new MetaType(gmit);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if(m_VisitType == EVisitType.Link)
            {
                sb.Append("[" + m_LocalMetaVariable.metaDefineType.allName+ "]");
                sb.Append(m_VisitMetaVariable.name);
            }
            else
            {
                sb.Append(m_LocalMetaVariable.name);
                if (m_LocalMetaVariable.isArray)
                {
                    sb.Append("[");
                    //sb.Append(m_DefineMetaType.ToFormatString());
                    sb.Append(m_Name);
                    sb.Append("]");
                    //sb.Append(m_Express.ToFormatString());
                }
                else
                {
                    sb.Append(m_VisitMetaVariable.name);
                }
            }

            return sb.ToString();
        }
    }
    public class MetaIteratorVariable : MetaVariable
    {
        int m_Index = 0;
        MetaVariable m_LocalMetaVariable = null;
        MetaType m_OrgMetaDefineType = null;
        MetaVariable m_IndexMetaVariable = null;
        MetaVariable m_ValueMetaVariable = null;

        public MetaIteratorVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaType orgMC )
        {
            m_Name = _name;
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_LocalMetaVariable = lmv;
            m_OrgMetaDefineType = orgMC;
            m_IndexMetaVariable = new MetaVariable("index", mbs, mc, new MetaType(CoreMetaClassManager.int32MetaClass));
            m_ValueMetaVariable = new MetaVariable("value", mbs, mc, new MetaType(orgMC.metaClass));
            if (lmv.isArray)
            {
                var gmit = m_LocalMetaVariable.metaDefineType.GetMetaInputTemplateByIndex();
                if (gmit == null)
                {
                    Console.WriteLine("Error 访问的Array中，没有找到模版 名称!!");
                    return;
                }
                m_DefineMetaType = new MetaType(gmit);
            }
            else
            {
                m_DefineMetaType = lmv.metaDefineType;
            }
        }
        public MetaClass GetIteratorMetaClass()
        {
            return m_OrgMetaDefineType.metaClass;
        }


        public override MetaVariable GetMetaVaraible(string name)
        {
            if( name == "index" )
            {
                return m_IndexMetaVariable;
            }
            else if( name == "value" )
            {
                return m_ValueMetaVariable;
            }
            if (m_MetaVariableDict.ContainsKey(name))
            {
                return m_MetaVariableDict[name];
            }
            return m_OrgMetaDefineType.metaClass.GetMetaMemberVariableByName( name );
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_LocalMetaVariable.name);
            if (m_LocalMetaVariable.isArray)
            {
                sb.Append("[");
                //sb.Append(m_DefineMetaType.ToFormatString());
                sb.Append(m_Name);
                sb.Append("]");
                //sb.Append(m_Express.ToFormatString());
            }
            else
            {

            }

            return sb.ToString();
        }
    }
    public partial class MetaMethodCall
    {
        public MetaVariable callerMetaVariable => m_CallerMetaVariable;
        public MetaClass callerMetaClass => m_CallerMetaClass;
        public MetaFunction function => m_MetaFunction;
        public MetaInputParamCollection metaInputParamCollection => m_MetaInputParamCollection;

        private MetaVariable m_CallerMetaVariable = null;
        private MetaClass m_CallerMetaClass = null;
        private MetaFunction m_MetaFunction = null;
        private MetaInputParamCollection m_MetaInputParamCollection = null;
        public bool isConstruction { get; set; } = false;
        public bool isStaticCall { get; set; } = false;

        public MetaMethodCall(MetaVariable mv, MetaFunction _fun, MetaInputParamCollection _metaInputParamCollection = null)
        {
            m_CallerMetaVariable = mv;
            m_MetaFunction = _fun;
            m_MetaInputParamCollection = _metaInputParamCollection;
            isStaticCall = false;
            ParseCSharp();
        }
        public MetaMethodCall(MetaClass mc, MetaFunction _fun, MetaInputParamCollection _param = null)
        {
            m_CallerMetaClass = mc;
            m_MetaFunction = _fun;
            m_MetaInputParamCollection = _param;
            if (_fun is MetaMemberFunction)
            {
                isConstruction = (_fun as MetaMemberFunction).isConstructInitFunction;
                isStaticCall = false;
            }
            else
            {
                isStaticCall = true;
            }
            ParseCSharp();
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
                Console.WriteLine("Error 验证失败,函数与输入参数不匹配!!");
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
                sb.Append(m_CallerMetaVariable.name);
            }

            if (m_CallerMetaClass != null)
            {
                sb.Append(m_CallerMetaClass.ToDefineTypeString());
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
            Variable,
            VisitVariable,
            IteratorVariable,
            MethodCall,
            NewMethodCall
        }
        public string name;
        public EVisitType visitType;

        public MetaVariable variable = null;
        public MetaVisitVariable visitVariable = null;
        public MetaMethodCall methodCall = null;
        public MetaClass callerMetaClass;
        public MetaBraceOrBracketStatementsContent metaBraceStatementsContent = null;

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
                        return methodCall.GetRetMetaType();
                    }
                    case EVisitType.VisitVariable:
                    {
                        return visitVariable.metaDefineType;
                    }
                    case EVisitType.Variable:
                    {
                        return variable.metaDefineType;
                    }
            }
            return new MetaType(CoreMetaClassManager.objectMetaClass);
        }
        public MetaClass GetMetaClass()
        {
            if (visitVariable != null)
            {
            }
            return null;
        }
        public MetaVariable GetRetMetaVariable()
        {
            if( visitVariable != null )
            {
                return visitVariable.visitMetaVariable;
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
            //    //    //Console.WriteLine("Error 未解析到:" + mv.allName + "位置在:" + mv.ToFormatString());
            //    //    return;
            //    //}
            //}
            //else if (m_CallNodeType == ECallNodeType.DataName )
            //{
            //    MetaData md = m_CurrentMetaBase as MetaData;
            //    return;
            //}
        }
        public string ToCommonString()
        {
            return "";
        }
        public string ToFormatString()
        {
            return "";
        }
    }
}
