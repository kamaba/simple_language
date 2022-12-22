//****************************************************************************
//  File:      FileMetaGlobalSyntax.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile.Parse;
using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile.CoreFileMeta
{
    public partial class FileMetaImportSyntax : FileMetaBase
    {
        public Token m_AsToken;
        public Token m_AsNameToken;
        List<Token> m_ImportNameListToken = new List<Token>();
        private List<Node> m_NodeList = new List<Node>();
        private NamespaceStatementBlock m_NamespaceStatement;
        private NamespaceStatementBlock m_AsNameStatement;

        public NamespaceStatementBlock namespaceStatement => m_NamespaceStatement;
        public NamespaceStatementBlock asNameStatement => m_AsNameStatement;

        public MetaNamespace lastMetaNamespace
        {
            get
            {
                if (m_NamespaceStatement != null)
                {
                    return m_NamespaceStatement.lastMetaNamespace;
                }
                return null;
            }
        }
        public FileMetaImportSyntax(List<Node> _nodeList)
        {
            m_NodeList = _nodeList;

            ParseImportSyntax();
        }
        private bool ParseImportSyntax()
        {
            if (m_NodeList.Count < 2)
            {
                Console.WriteLine("Error import必须有2个节点!!");
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
            MetaBase mb = ModuleManager.instance.selfModule;
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
                        mb = mb.GetChildrenMetaBaseByName(name);
                        if (!(mb is MetaNamespace))
                        {
                            Console.WriteLine("解析Import语句发生错误，没有找到对应的命名空间路径: " + m_NamespaceStatement.tokenList[i].lexeme.ToString()
                                    + "Token: " + m_NamespaceStatement.tokenList[i].sourceBeginLine.ToString());
                            break;
                        }
                        else
                        {
                            m_NamespaceStatement.AddMetaNamespace(mb as MetaNamespace);
                        }
                    }
                }
            }

            if( isCSharp )
            {
                MetaBase curmb = ModuleManager.instance.csharpModule;
                for ( int i = 0; i < tokenList.Count; i++ )
                {
                    string name = tokenList[i].lexeme.ToString();
                    MetaNamespace mn = new MetaNamespace(name);
                    mn.SetRefFromType(RefFromType.CSharp);
                    curmb.AddMetaBase(name, mn);
                    curmb = mn;
                    m_NamespaceStatement.AddMetaNamespace(mn as MetaNamespace);
                }
            }
            else
            {
                if (m_NamespaceStatement.metaNamespaceList.Count != m_NamespaceStatement.tokenList.Count)
                {
                    Console.WriteLine("解析Import语句发生错误，没有找到: " + m_NamespaceStatement.namespaceString + "    Token: " + m_Token.sourceBeginChar.ToString());
                }
            }

        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append( m_Token.lexeme.ToString() + " " + m_NamespaceStatement.ToFormatString() );
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
