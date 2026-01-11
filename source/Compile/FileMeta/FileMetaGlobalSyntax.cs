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
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Compile
{
    public sealed class FileMetaImportSyntax : FileMetaBase
    {
        public Token m_AsToken;
        public Token m_AsNameToken;
        List<Token> m_ImportNameListToken = new List<Token>();
        private List<Token> m_TokenList = new List<Token>();
        private NamespaceStatementBlock m_NamespaceStatement = null;
        private NamespaceStatementBlock m_AsNameStatement = null;

        public NamespaceStatementBlock namespaceStatement => m_NamespaceStatement;
        public NamespaceStatementBlock asNameStatement => m_AsNameStatement;
        // Node 版本构造方法（legacy，已由 Token 版本取代）
        // public FileMetaImportSyntax(List<Node> _nodeList) { ... }
        public FileMetaImportSyntax( Token importToken, List<Token> _tokenList )
        {
            m_Token = importToken;
            m_TokenList = _tokenList;
        }

        /// <summary>
        /// 纯 Token 版本的 import 解析逻辑，替代 Node 依赖。
        /// </summary>
        private bool ParseImportSyntaxFromTokens()
        {
            if (m_TokenList == null || m_TokenList.Count < 1 )
            {
                Debug.Assert(false, "");
                Log.AddInStructFileMeta(EError.None, "Error import必须有2个Token!!");
                return false;
            }

            // 读取 import 路径：Identifier/Type + 可选的 '.' 分隔
            m_ImportNameListToken.Clear();
            foreach ( var t in m_TokenList)
            {
                if( t.type == ETokenType.SemiColon )
                {
                    break;
                }

                if (m_AsNameToken == null)
                {
                    if( t.type == ETokenType.As )
                    {
                        m_AsToken = t;
                    }
                    else
                    {
                        m_ImportNameListToken.Add(t);
                    }
                }
                else
                {
                    m_AsNameToken = t;
                }
            }
            m_NamespaceStatement = NamespaceStatementBlock.CreateStateBlock(m_ImportNameListToken);
            return true;
        }
        public void Parse()
        {
            // 仅使用 Token 方式解析，Node 流程已废弃
            if (!ParseImportSyntaxFromTokens())
            {
                return;
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
                        if (mb?.isMetaNamespace == true )
                        {
                            m_FileMeta.AddImportMetaNamespace(mb.metaNamespace);
                        }
                        else
                        {
                            Log.AddInStructFileMeta(EError.None, "解析Import语句发生错误，没有找到对应的命名空间路径: " + m_NamespaceStatement.tokenList[i].lexeme.ToString()
                                    + "Token: " + m_NamespaceStatement.tokenList[i].sourceBeginLine.ToString());
                            break;
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
