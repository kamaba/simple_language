//****************************************************************************
//  File:      FileMetaGlobalSyntax.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile.Parse;
using SimpleLanguage.Core;
using SimpleLanguage.CSharp;
using SimpleLanguage.Parse;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile.CoreFileMeta
{
    public sealed class FileMetaImportSyntax : FileMetaBase
    {
        public Token m_AsToken;
        public Token m_AsNameToken;
        List<Token> m_ImportNameListToken = new List<Token>();
        private List<Node> m_NodeList = new List<Node>();
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
        public void Parse()
        {
            ParseImportSyntax();

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
