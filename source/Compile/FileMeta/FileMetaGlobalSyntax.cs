//****************************************************************************
//  File:      FileMetaGlobalSyntax.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Core;
using SimpleLanguage.CSharp;
using SimpleLanguage.Parse;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    public sealed class FileMetaImportSyntax : FileMetaBase
    {
        public Token m_AsToken;
        public Token m_AsNameToken;
        List<Token> m_ImportNameListToken = new List<Token>();
        private List<Node> m_NodeList = new List<Node>();
        private List<Token> m_TokenList = new List<Token>();
        private NamespaceStatementBlock m_NamespaceStatement = null;
#pragma warning disable CS0649 // 从未对字段“FileMetaImportSyntax.m_AsNameStatement”赋值，字段将一直保持其默认值 null
        private NamespaceStatementBlock m_AsNameStatement = null;
#pragma warning restore CS0649 // 从未对字段“FileMetaImportSyntax.m_AsNameStatement”赋值，字段将一直保持其默认值 null

        public NamespaceStatementBlock namespaceStatement => m_NamespaceStatement;
        public NamespaceStatementBlock asNameStatement => m_AsNameStatement;
        public FileMetaImportSyntax(List<Node> _nodeList)
        {
            m_NodeList = _nodeList;
        }
        public FileMetaImportSyntax( List<Token> _tokenList )
        {
            m_TokenList = _tokenList;
        }
        private bool ParseImportSyntax()
        {
            if (m_NodeList.Count < 2)
            {
                Log.AddInStructFileMeta(EError.None, "Error import必须有2个节点!!");
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
            //m_AsNameStatement = NamespaceStatementBlock.CreateStateBlock(_asNameTokenList);
            return true;
        }

        /// <summary>
        /// 纯 Token 版本的 import 解析逻辑，替代 Node 依赖。
        /// </summary>
        private bool ParseImportSyntaxFromTokens()
        {
            if (m_TokenList == null || m_TokenList.Count < 2)
            {
                Log.AddInStructFileMeta(EError.None, "Error import必须有2个Token!!");
                return false;
            }

            int index = 0;
            // 第一个必须是 import 关键字
            var first = m_TokenList[index++];
            if (first.type != ETokenType.Import)
            {
                Log.AddInStructFileMeta(EError.None, "Error import 语句必须以 import 关键字开始!!");
                return false;
            }
            m_Token = first;

            // 读取 import 路径：Identifier/Type + 可选的 '.' 分隔
            m_ImportNameListToken.Clear();
            while (index < m_TokenList.Count)
            {
                var t = m_TokenList[index];
                if (t.type == ETokenType.Identifier || t.type == ETokenType.Type)
                {
                    m_ImportNameListToken.Add(t);
                    index++;
                    continue;
                }
                if (t.type == ETokenType.Period)
                {
                    index++;
                    continue;
                }
                break;
            }

            // 可选的 "as 别名" 部分
            if (index < m_TokenList.Count && m_TokenList[index].type == ETokenType.As)
            {
                m_AsToken = m_TokenList[index++];
                if (index < m_TokenList.Count && m_TokenList[index].type == ETokenType.Identifier)
                {
                    m_AsNameToken = m_TokenList[index++];
                }
                else
                {
                    Log.AddInStructFileMeta(EError.None, "Error import as 后必须紧跟标识符");
                }
            }

            m_NamespaceStatement = NamespaceStatementBlock.CreateStateBlock(m_ImportNameListToken);
            return true;
        }
        public void Parse()
        {
            // 优先使用 Token 方式解析，其次退回 Node
            if (m_TokenList != null && m_TokenList.Count > 0)
            {
                if (!ParseImportSyntaxFromTokens())
                {
                    return;
                }
            }
            else
            {
                if (!ParseImportSyntax())
                {
                    return;
                }
            }

            if (m_NamespaceStatement == null || m_NamespaceStatement.tokenList == null)
            {
                Log.AddInStructFileMeta(EError.None, "Error Import 解析失败，NamespaceStatement 为空");
                return;
            }

            MetaNode mb = ModuleManager.instance.selfModule.metaNode;
            List<Token> tokenList = new List<Token>();
            bool isCSharp = false;
            for (int i = 0; i < m_NamespaceStatement.tokenList.Count; i++)
            {
                string name = m_NamespaceStatement.tokenList[i].lexeme.ToString();
                if ( i == 0 && name == "CSharp" )
                {
                    isCSharp = true;
                }
                else
                {
                    if( isCSharp )
                    {
                        tokenList.Add(m_NamespaceStatement.tokenList[i]);
                    }
                    else
                    {
                        mb = mb.GetChildrenMetaNodeByName(name);
                        if (!mb.isMetaNamespace )
                        {
                            Log.AddInStructFileMeta(EError.None, "解析Import语句发生错误，没有找到对应的命名空间路径: " + m_NamespaceStatement.tokenList[i].lexeme.ToString()
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

            if( isCSharp )
            {
                if(tokenList.Count < 1 )
                {
                    Log.AddInStructFileMeta(EError.None, "Error 在使用import引用CSharp库时，至少需要一个命名空间");
                    return;
                }

                MetaNode curmb = ModuleManager.instance.csharpModule.metaNode;

                string allname = "";
                for ( int i = 0; i < tokenList.Count; i++ )
                {
                    string name = tokenList[i].lexeme.ToString();

                    if( string.IsNullOrEmpty(allname) )
                    {
                        allname = name;
                    }
                    else
                    {
                        allname = allname + "." + name;
                    }
                    if ( CSharpManager.IsFindMetaCSharpNamespace(allname) )
                    {
                        MetaNamespaceCSharp mn = new MetaNamespaceCSharp(name);
                        curmb = curmb.AddMetaNamespace(mn);
                        m_FileMeta.AddImportMetaNamespace(mn);
                    }
                }
            }

        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append( this.m_Token?.lexeme.ToString() + " " + m_NamespaceStatement?.ToFormatString() );
            if (m_AsToken != null )
            {
                sb.Append( " " + m_AsToken.lexeme.ToString());
                sb.Append(" " + m_AsNameToken.lexeme.ToString());
            }
            sb.Append(";");

            return sb.ToString();
        }
    }
}
