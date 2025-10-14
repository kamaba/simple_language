//****************************************************************************
//  File:      MetaCallLink.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/7/29 12:00:00
//  Description:  this's a common node handles
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.Parse;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaCallLink
    {
        public MetaVisitNode finalCallNode => m_FinalCallNode;
        public List<MetaVisitNode> callNodeList => m_VisitNodeList;
        public AllowUseSettings allowUseSettings { get; private set; }

        private FileMetaCallLink m_FileMetaCallLink;
        private MetaClass m_OwnerMetaClass = null;
        private MetaBlockStatements m_OwnerMetaBlockStatements = null;
        private List<MetaCallNode> m_CallNodeList = new List<MetaCallNode>();

        private MetaVisitNode m_FinalCallNode = null;
        private List<MetaVisitNode> m_VisitNodeList = new List<MetaVisitNode>();
        public MetaCallLink(FileMetaCallLink fmcl, MetaClass metaClass, MetaBlockStatements mbs, MetaType frontDefineMt, MetaVariable mv )
        {
            m_FileMetaCallLink = fmcl;
            m_OwnerMetaClass = metaClass;
            m_OwnerMetaBlockStatements = mbs;
            CreateCallLinkNode(frontDefineMt, mv);
        }
        private void CreateCallLinkNode(MetaType frontDefineMt, MetaVariable mv)
        {
            MetaCallNode frontMetaNode = null;
            if (m_FileMetaCallLink.callNodeList.Count > 0)
            {
                FileMetaCallNode fmcn = m_FileMetaCallLink.callNodeList[0];
                var firstNode = new MetaCallNode(null, fmcn, m_OwnerMetaClass, m_OwnerMetaBlockStatements, frontDefineMt);
                frontMetaNode = firstNode;
                m_CallNodeList.Add(firstNode);
            }
            for (int i = 1; i < m_FileMetaCallLink.callNodeList.Count; i = i + 2)
            {
                var cn1 = m_FileMetaCallLink.callNodeList[i];
                var cn2 = m_FileMetaCallLink.callNodeList[i + 1];
                var fmn = new MetaCallNode(cn1, cn2, m_OwnerMetaClass, m_OwnerMetaBlockStatements, frontDefineMt);
                fmn.SetFrontCallNode(frontMetaNode);
                m_CallNodeList.Add(fmn);
                frontMetaNode = fmn;
            }
            var m_FinalMetaCallNode = frontMetaNode;
            if( m_FinalMetaCallNode == null )
            {
                Log.AddInStructMeta(EError.None, "Error 连接串没有找到合适的节点  360!!!");
            }
            m_FinalMetaCallNode.SetDefineMetaVariable(mv);
        }
        public Token GetToken() { return null; }
        public bool Parse( AllowUseSettings _useConst )
        {
            allowUseSettings = new AllowUseSettings(_useConst);
            allowUseSettings.setterFunction = false;
            allowUseSettings.getterFunction = true;
            bool flag = true;
            for (int i = 0; i < m_CallNodeList.Count; i++)
            {
                if (flag)
                {
                    if( i == m_CallNodeList.Count - 1 )
                    {
                        allowUseSettings.setterFunction = _useConst.setterFunction;
                        allowUseSettings.getterFunction = _useConst.getterFunction;
                    }
                    flag = m_CallNodeList[i].ParseNode(allowUseSettings);

                    if (m_CallNodeList[i].callNodeType == ECallNodeType.NewClass 
                        || m_CallNodeList[i].callNodeType == ECallNodeType.NewData )
                    {
                        if( i < m_CallNodeList.Count - 1 )
                        {
                            flag = false;
                            Log.AddInStructMeta(EError.None, "Parse Statement Error 在使用NewClassName的方式，后边不允许有其它的调用!");
                        }
                    }
                }
            }
            if( flag )
            {
                int i = 0;
                MetaCallNode frontNode = null;
                while (true)
                {
                    if( i >= m_CallNodeList.Count )
                    {
                        break;
                    }
                    MetaCallNode mcn = m_CallNodeList[i++];
                    if( mcn == null )
                    {
                        break;
                    }
                    if (mcn.callNodeType == ECallNodeType.This)
                    {
                        MetaVisitNode mvn = MetaVisitNode.CreateByThis(mcn.metaVariable);
                        m_VisitNodeList.Add(mvn);
                    }
                    else if (mcn.callNodeType == ECallNodeType.Base)
                    {
                        MetaVisitNode mvn = MetaVisitNode.CreateByBase(mcn.metaVariable);
                        m_VisitNodeList.Add(mvn);
                    }
                    else if (mcn.callNodeType == ECallNodeType.FunctionInnerVariableName)
                    {
                        MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable, mcn.metaType);
                        m_VisitNodeList.Add(mvn);
                    }
                    else 
                    if (mcn.callNodeType == ECallNodeType.MemberVariableName)
                    {
                        MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable, mcn.callMetaType );
                        m_VisitNodeList.Add(mvn);
                    }
                    else if (mcn.callNodeType == ECallNodeType.MemberFunctionName )
                    {
                        MetaVariable newmv = null;
                        MetaMethodCall mmc = null;
                        if (frontNode?.callNodeType == ECallNodeType.ConstValue )
                        {
                            MetaVisitNode fvn = m_VisitNodeList[m_VisitNodeList.Count - 1];                            
                            m_VisitNodeList.Remove(fvn);

                            //MetaMemberVariable mmv = frontNode.m_MetaClass.GetMetaMemberVariableByName("value");

                            //MetaBraceAssignStatements mas = new MetaBraceAssignStatements(frontNode.ownerMetaFunctionBlock,fvn.constValueExpress, mmv);
                            //MetaBraceOrBracketStatementsContent mbobs = new MetaBraceOrBracketStatementsContent(frontNode.ownerMetaFunctionBlock, frontNode.m_MetaClass);
                            
                            //mbobs.assignStatementsList.Add(mas);

                            string name = "auto_constvalue_" + fvn.constValueExpress.eType.ToString() + "_" + fvn.constValueExpress.value.ToString();
                            newmv = frontNode.ownerMetaFunctionBlock.GetMetaVariable(name);
                            if (newmv == null)
                            {
                                var mccm = CoreMetaClassManager.GetMetaClassByEType(fvn.constValueExpress.eType);
                                newmv = new MetaVariable(name, MetaVariable.EVariableFrom.LocalStatement,
                                frontNode.ownerMetaFunctionBlock, frontNode.metaType.metaClass, new MetaType(mccm));

                                frontNode.ownerMetaFunctionBlock.AddMetaVariable(newmv);
                            }

                            MetaVisitNode mvn1 = MetaVisitNode.CraeteByNewClass(frontNode.metaType, null, newmv);
                            m_VisitNodeList.Add(mvn1);

                            mmc = new MetaMethodCall( mcn.callMetaType.metaClass, mcn.callMetaType.templateMetaTypeList, mcn.metaFunction, mcn.metaTemplateParamsList, mcn.metaInputParamCollection, newmv, mcn.storeMetaVariable);
                        }
                        else
                        {
                            var retmv = frontNode?.metaVariable;
                            if ( m_VisitNodeList.Count > 0 && retmv != null )
                            {
                                m_VisitNodeList.RemoveAt(m_VisitNodeList.Count - 1);
                            }
                            mmc = new MetaMethodCall(mcn.callMetaType.metaClass, mcn.callMetaType.templateMetaTypeList, mcn.metaFunction, mcn.metaTemplateParamsList, mcn.metaInputParamCollection, retmv, mcn.storeMetaVariable);                           
                        }

                        MetaVisitNode mvn2 = MetaVisitNode.CreateByMethodCall(mmc);
                        m_VisitNodeList.Add(mvn2);
                    }
                    else if( mcn.callNodeType == ECallNodeType.ConstValue )
                    {
                        MetaVisitNode mvn = MetaVisitNode.CreateByConstExpress(mcn.metaExpressValue as MetaConstExpressNode, mcn.metaVariable );
                        m_VisitNodeList.Add(mvn);
                    }
                    else if (mcn.callNodeType == ECallNodeType.NewClass )
                    {
                        if( i == m_CallNodeList.Count )
                        {
                            MetaVisitNode mvn = MetaVisitNode.CraeteByNewClass(mcn.metaType, mcn.metaBraceStatementsContent, null );
                            m_VisitNodeList.Add(mvn);

                            if( mcn.metaFunction != null )
                            {
                                MetaMethodCall mmc = new MetaMethodCall(mcn.metaType.metaClass, null, mcn.metaFunction, null, mcn.metaInputParamCollection, null, mcn.storeMetaVariable );
                                mvn.SetMethodCall(mmc);
                            }
                        }
                        else
                        {
                            Log.AddInStructMeta(EError.None, "Error 使用NewClass方式，后边不允许跟其它变量相关内容!");
                        }
                    }
                    else if( mcn.callNodeType == ECallNodeType.NewTemplate )
                    {
                        MetaVisitNode mvn = MetaVisitNode.CreateByNewTemplate(mcn.metaType, mcn.metaFunction, mcn.storeMetaVariable);
                        MetaClass cmc = mcn.metaType.metaClass;
                        MetaMethodCall mmc = new MetaMethodCall(mcn.metaType.metaClass, null, mcn.metaFunction, null, mcn.metaInputParamCollection, null, mcn.storeMetaVariable);
                        mvn.SetMethodCall(mmc);
                        m_VisitNodeList.Add(mvn);
                    }
                    else if( mcn.callNodeType == ECallNodeType.NewData )
                    {
                        MetaVisitNode mvn = MetaVisitNode.CraeteByNewData( mcn.metaType, mcn.metaBraceStatementsContent);
                        m_VisitNodeList.Add(mvn);
                    }
                    else if (mcn.callNodeType == ECallNodeType.EnumName)
                    {
                        //Debug.Write("Meta Common Parse IteratorVariable----------------------------------------------------");
                    }
                    else if (mcn.callNodeType == ECallNodeType.EnumValueArray)
                    {
                        MetaVisitNode mvn = MetaVisitNode.CreateByEnumDefaultValue(mcn.metaType, mcn.metaVariable);
                        m_VisitNodeList.Add(mvn);
                    }
                    else if (mcn.callNodeType == ECallNodeType.VisitVariable)
                    {
                        MetaVisitNode mvn = MetaVisitNode.CreateByVisitVariable(mcn.metaVariable as MetaVisitVariable);
                        m_VisitNodeList.Add(mvn);
                    }
                    else if (mcn.callNodeType == ECallNodeType.IteratorVariable)
                    {
                        Log.AddInStructMeta(EError.None, "Meta Common Parse IteratorVariable----------------------------------------------------");
                    }
                    else if (mcn.callNodeType == ECallNodeType.DataName)
                    {
                        MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable);
                        m_VisitNodeList.Add(mvn);
                    }
                    else if( mcn.callNodeType == ECallNodeType.EnumDefaultValue )
                    {
                        MetaVisitNode mvn = MetaVisitNode.CreateByEnumDefaultValue(mcn.metaType, mcn.metaVariable);
                        m_VisitNodeList.Add(mvn);
                    }
                    else if (mcn.callNodeType == ECallNodeType.MemberDataName)
                    {
                        MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable);
                        m_VisitNodeList.Add(mvn);
                        //Debug.Write("Meta Common Parse MemberDataName----------------------------------------------------");
                    }
                    frontNode = mcn;
                }
            }
            if( m_VisitNodeList != null && m_VisitNodeList.Count > 0 )
            {
                m_FinalCallNode = m_VisitNodeList[m_VisitNodeList.Count - 1];
            }
            else
            {
                Log.AddInStructMeta( EError.None, "Error 解析执行链出错");
                flag = false;
            }

            return flag;
        }
        public int CalcParseLevel(int level)
        {
            for (int i = 0; i < m_VisitNodeList.Count; i++)
            {
                level = m_VisitNodeList[i].CalcParseLevel(level);
            }
            return level;
        }
        public void CalcReturnType()
        {
            for (int i = 0; i < m_VisitNodeList.Count; i++)
            {
                m_VisitNodeList[i].CalcReturnType();
            }
        }
        public MetaVariable ExecuteGetMetaVariable()
        {
            return m_FinalCallNode?.GetRetMetaVariable();
        }
        public MetaClass ExecuteGetMetaClass()
        {
            return m_FinalCallNode?.GetMetaClass();
        }
        public MetaExpressNode GetMetaExpressNode()
        {
            //if( m_FinalMetaCallNode.callNodeType == ECallNodeType.ConstValue )
            //{
            //    return new MetaConstExpressNode(EType.Int32, m_FinalMetaCallNode.constValue);
            //}
            return null;
        }
        public MetaType GetMetaDeineType()
        {
            MetaType mt = null;
            for (int i = 0; i < m_VisitNodeList.Count; i++)
            {
                mt = m_VisitNodeList[i].GetMetaDefineType();
            }
            return mt;
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.m_VisitNodeList.Count; i++)
            {
                sb.Append(m_VisitNodeList[i].ToFormatString() );
                if( i < this.m_VisitNodeList.Count - 1 )
                    sb.Append("  ->  ");
            }
            return sb.ToString();
        }
        public string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_FileMetaCallLink.ToTokenString());
            return sb.ToString();

        }
    }
}
