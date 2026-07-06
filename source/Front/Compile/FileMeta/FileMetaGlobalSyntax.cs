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
        public string moduleName => m_ModuleName;
        public string importNamespaceName => m_ImportNamespaceName;
        public string aliasName => m_AliasName;



        private Token m_AsToken;
        private Token m_AsNameToken;
        private List<Token> m_ImportNameListToken = new List<Token>();
        private List<Node> m_NodeList = new List<Node>();
        private List<Token> m_TokenList = new List<Token>();
        private NamespaceStatementBlock m_NamespaceStatement = null;
        private string m_ModuleName = string.Empty;
        private string m_ImportNamespaceName = string.Empty;
        private string m_AliasName = string.Empty;
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

            if (namespaceNameNode?.token?.type == ETokenType.String)
            {
                ParseStringImportTarget(namespaceNameNode.token);
            }
            else
            {
                m_ImportNameListToken = namespaceNameNode.GetLinkTokenList();
                m_ImportNamespaceName = TokensToNamespaceText(m_ImportNameListToken);
            }

            if (m_NodeList.Count == 4)
            {
                m_AsToken = m_NodeList[2].token;
                var asNameNode = m_NodeList[3];
                m_AsNameToken = asNameNode.token;
                m_AliasName = m_AsNameToken?.lexeme?.ToString() ?? string.Empty;
            }

            m_NamespaceStatement = NamespaceStatementBlock.CreateStateBlock(m_ImportNameListToken);
            return true;
        }

        private void ParseStringImportTarget(Token targetToken)
        {
            var text = targetToken?.lexeme?.ToString() ?? string.Empty;
            text = text.Trim();

            int colonIndex = text.IndexOf(':');
            if (colonIndex >= 0)
            {
                m_ModuleName = text.Substring(0, colonIndex).Trim();
                if( colonIndex + 1 < text.Length )
                {

                }
                else
                {
                    m_ImportNamespaceName = text.Substring(colonIndex + 1).Trim();
                }
            }
            else
            {
                m_ModuleName = string.Empty;
                m_ImportNamespaceName = text;
            }

            if (string.IsNullOrWhiteSpace(m_ImportNamespaceName))
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, targetToken, "Error import 字符串中没有命名空间名称");
                return;
            }

            m_ImportNameListToken = BuildNamespaceTokens(targetToken, m_ImportNamespaceName);
        }

        private static List<Token> BuildNamespaceTokens(Token sourceToken, string namespaceName)
        {
            List<Token> tokens = new List<Token>();
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return tokens;
            }

            var parts = namespaceName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0)
                {
                    tokens.Add(new Token(sourceToken.path, ETokenType.Period, ".", sourceToken.sourceBeginLine - 1, sourceToken.sourceBeginChar));
                }
                tokens.Add(new Token(sourceToken.path, ETokenType.Identifier, parts[i], sourceToken.sourceBeginLine - 1, sourceToken.sourceBeginChar));
            }
            return tokens;
        }

        private static string TokensToNamespaceText(List<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tokens.Count; i++)
            {
                var text = tokens[i]?.lexeme?.ToString();
                if (string.IsNullOrEmpty(text)) continue;
                sb.Append(text);
            }
            return sb.ToString();
        }

        public void Parse()
        {
            ParseImportSyntax();

            MetaNode mb = ResolveImportMetaNode();
            if (mb == null)
            {
                return;
            }

            if (mb.isMetaNamespace)
            {
                m_FileMeta.AddImportMetaNamespace(mb.metaNamespace);
                if (!string.IsNullOrWhiteSpace(m_AliasName))
                {
                    m_FileMeta.AddImportAliasMetaNamespace(m_AliasName, mb.metaNamespace);
                }
                return;
            }

            Log.AddFileMetaLog(LID.ShowExtendMessage, "解析Import语句发生错误，没有找到对应的命名空间路径: " + (m_ImportNamespaceName ?? string.Empty));
        }

        private MetaNode ResolveImportMetaNode()
        {
            if (m_NamespaceStatement == null || m_NamespaceStatement.tokenList.Count == 0)
            {
                return null;
            }

            MetaNode mb = null;
            int startIndex = 0;

            if (!string.IsNullOrWhiteSpace(m_ModuleName))
            {
                var module = ModuleManager.instance.GetMetaModuleByName(m_ModuleName);
                if (module == null)
                {
                    Log.AddFileMetaLog(LID.ShowExtendMessage, m_Token, "没有找到 import 模块: " + m_ModuleName);
                    return null;
                }
                mb = module.metaNode;
            }
            else if (m_NodeList.Count > 1 && m_NodeList[1]?.token?.type != ETokenType.String)
            {
                var firstName = m_NamespaceStatement.tokenList[0].lexeme.ToString();
                var module = ModuleManager.instance.GetMetaModuleByName(firstName);
                if (module != null)
                {
                    mb = module.metaNode;
                    startIndex = 1;
                }
                else
                {
                    mb = ModuleManager.instance.selfModule.metaNode;
                }
            }
            else
            {
                mb = ModuleManager.instance.selfModule.metaNode;
            }

            if (mb?.name == "CSharp")
            {
                return ResolveCSharpImportNamespace(mb, startIndex);
            }

            for (int i = startIndex; i < m_NamespaceStatement.tokenList.Count; i++)
            {
                string name = m_NamespaceStatement.tokenList[i].lexeme.ToString();
                var findmb = mb.GetChildrenMetaNodeByName(name);
                if (findmb == null)
                {
                    Log.AddFileMetaLog(LID.ShowExtendMessage, m_NamespaceStatement.tokenList[i], $"文件:{m_NamespaceStatement.tokenList[i].path} 没有找到:{mb.allName} 下的:{name}");
                    return null;
                }
                mb = findmb;
            }

            return mb;
        }

        private MetaNode ResolveCSharpImportNamespace(MetaNode csharpModuleNode, int startIndex)
        {
            MetaNode mb = csharpModuleNode;
            string allname = "";
            for (int i = startIndex; i < m_NamespaceStatement.tokenList.Count; i++)
            {
                string name = m_NamespaceStatement.tokenList[i].lexeme.ToString();

                if (string.IsNullOrEmpty(allname))
                {
                    allname = name;
                }
                else
                {
                    allname = allname + "." + name;
                }

                if (CSharpManager.IsFindMetaCSharpNamespace(allname))
                {
                    var findmb = mb.GetChildrenMetaNodeByName(name);

                    if (findmb != null)
                    {
                        if (findmb.metaNamespace is MetaNamespaceCSharp)
                        {
                            mb = findmb;
                        }
                        else
                        {
                            Log.AddFileMetaLog(LID.ShowExtendMessage, "解析Import语句发生错误，没有找到对应的命名空间路径: " + allname
                                + "Token: " + m_NamespaceStatement.tokenList[i].sourceBeginLine.ToString());
                            return null;
                        }
                    }
                    else
                    {
                        MetaNamespaceCSharp mn = new MetaNamespaceCSharp(name);
                        mb = mb.AddMetaNamespace(mn);
                    }
                }
            }

            if (mb == null)
            {
                return null;
            }

            return mb;
        }

        public override string ToFormatString()
        {
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
