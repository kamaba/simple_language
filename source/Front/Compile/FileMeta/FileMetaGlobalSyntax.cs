//****************************************************************************
//  File:      FileMetaGlobalSyntax.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.CSharp;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    public sealed class FileMetaImportSyntax : FileMetaBase
    {
        public NamespaceStatementBlock namespaceStatement => m_NamespaceStatement;
        public NamespaceStatementBlock asNameStatement => m_AsNameStatement;



        private Token m_AsToken;
        private Token m_AsNameToken;
        private List<Token> m_ImportNameListToken = new List<Token>();
        private List<Node> m_NodeList = new List<Node>();
        private List<Token> m_TokenList = new List<Token>();
        private NamespaceStatementBlock m_NamespaceStatement = null;
#pragma warning disable CS0649
        private NamespaceStatementBlock m_AsNameStatement = null;
#pragma warning restore CS0649
        public FileMetaImportSyntax(List<Node> _nodeList)
        {
            m_NodeList = _nodeList;
        }
        public FileMetaImportSyntax(List<Token> _tokenList)
        {
            m_TokenList = _tokenList;
        }
        private bool ParseImportSyntax()
        {
            if (m_NodeList.Count < 2)
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, "Error import必须有2个节点!!");
                return false;
            }
            var namespaceNode = m_NodeList[0];
            if (namespaceNode?.token?.type == ETokenType.Import)
            {
                m_Token = namespaceNode.token;
            }
            var namespaceNameNode = m_NodeList[1];

            m_ImportNameListToken = namespaceNameNode.linkTokenList;

            if (m_NodeList.Count == 4)
            {
                m_AsToken = m_NodeList[2].token;
                var asNameNode = m_NodeList[3];
                m_AsNameToken = asNameNode.token;
            }

            m_NamespaceStatement = NamespaceStatementBlock.CreateStateBlock(m_ImportNameListToken);
            return true;
        }

        public void Parse()
        {
            ParseImportSyntax();

            MetaNode mb = ModuleManager.instance.selfModule.metaNode;
            List<Token> tokenList = new List<Token>();
            for (int i = 0; i < m_NamespaceStatement.tokenList.Count; i++)
            {
                string name = m_NamespaceStatement.tokenList[i].lexeme.ToString();
                if (i == 0)
                {
                    var findmodule = ModuleManager.instance.GetMetaModuleByName(name);
                    if (findmodule != null)
                    {
                        mb = findmodule.metaNode;
                    }
                    else
                    {
                        mb = mb.GetChildrenMetaNodeByName(name);
                    }
                    //Log.AddFileMetaLog(LID.ShowExtendMessage, "查找失败" + name );// mb != null, "查找失败" + name);
                }
                else
                {
                    if (mb.name == "CSharp")
                    {
                        tokenList.Add(m_NamespaceStatement.tokenList[i]);
                    }
                    else
                    {
                        var findmb = mb.GetChildrenMetaNodeByName(name);
                        if (findmb == null)
                        {
                            //Debug.Assert(false, $"文件:{m_NamespaceStatement.tokenList[i].path } 没有找到:{mb.allName} 下的:{name}");
                            Log.AddFileMetaLog(LID.ShowExtendMessage, $"文件:{m_NamespaceStatement.tokenList[i].path} 没有找到:{mb.allName} 下的:{name}");
                            break;
                        }
                        mb = findmb;
                        if (!mb.isMetaNamespace)
                        {
                            Log.AddFileMetaLog(LID.ShowExtendMessage, "解析Import语句发生错误，没有找到对应的命名空间路径: " + m_NamespaceStatement.tokenList[i].lexeme.ToString()
                                    + "Token: " + m_NamespaceStatement.tokenList[i].sourceBeginLine.ToString());
                            break;
                        }
                        else
                        {
                            m_FileMeta.AddImportMetaNamespace(mb.metaNamespace);
                        }
                    }
                }
            }

            if (mb.name == "CSharp")
            {
                if (tokenList.Count < 1)
                {
                    Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 在使用import引用CSharp库时，至少需要一个命名空间");
                    return;
                }

                string allname = "";
                for (int i = 0; i < tokenList.Count; i++)
                {
                    string name = tokenList[i].lexeme.ToString();

                    if (string.IsNullOrEmpty(allname))
                    {
                        allname = name;
                    }
                    else
                    {
                        allname = allname + "." + name;
                    }
                    var findname = allname;
                    if (CSharpManager.IsFindMetaCSharpNamespace(findname))
                    {
                        var findmb = mb.GetChildrenMetaNodeByName(name);

                        if (findmb != null)
                        {
                            if (findmb.metaNamespace is MetaNamespaceCSharp mnc)
                            {
                                mb = findmb;
                                m_FileMeta.AddImportMetaNamespace(mnc);
                            }
                            else
                            {
                                Log.AddFileMetaLog(LID.ShowExtendMessage, "解析Import语句发生错误，没有找到对应的命名空间路径: " + allname
                                    + "Token: " + tokenList[i].sourceBeginLine.ToString());
                                break;
                            }
                        }
                        else
                        {
                            MetaNamespaceCSharp mn = new MetaNamespaceCSharp(name);
                            mb = mb.AddMetaNamespace(mn);
                            m_FileMeta.AddImportMetaNamespace(mn);
                        }
                    }
                }
            }
        }

        public override string ToFormatString()
        {
            if (m_Token == null || m_NamespaceStatement == null)
            {
                ParseImportSyntax();
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(this.m_Token?.lexeme.ToString() + " " + m_NamespaceStatement?.ToFormatString());
            if (m_AsToken != null)
            {
                sb.Append(" " + m_AsToken.lexeme.ToString());
                sb.Append(" " + m_AsNameToken.lexeme.ToString());
            }
            sb.Append(";");
            return sb.ToString();
        }
    }

}
