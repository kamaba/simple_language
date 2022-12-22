//****************************************************************************
//  File:      FileMetaNamespace.cs
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
    public partial class FileDefineNamespace : FileMetaBase
    {
        private List<Node> m_NodeList = new List<Node>();
        public new string name
        {
            get
            {
                if( m_NamespaceStateBlock != null )
                {
                    return m_NamespaceStateBlock.namespaceString;
                }
                return "";
            }
        }
        public List<MetaNamespace> metaNamespaceList
        {
            get
            {
                if( m_NamespaceStateBlock != null )
                {
                    return m_NamespaceStateBlock.metaNamespaceList;
                }
                return null;
            }
        }
        public NamespaceStatementBlock namespaceStatementBlock => m_NamespaceStateBlock;
        protected NamespaceStatementBlock m_NamespaceStateBlock { get; set; }   
        protected FileDefineNamespace() { }
        public FileDefineNamespace(List<Node> lists)
        {
            m_NodeList = lists;

            ParseFileDefineNamespace();
        }
        private bool ParseFileDefineNamespace()
        {
            if (m_NodeList.Count != 2)
            {
                Console.WriteLine("Error  ParseFileDefineNamespace 解析自定义空间!!");
                return false;
            }
            m_Token = m_NodeList[0].token;

            if (m_Token == null)
            {
                Console.WriteLine("Error 没有查找namespaceToken");
                return false;
            }
            m_NamespaceStateBlock = NamespaceStatementBlock.CreateStateBlock(m_NodeList[1].linkTokenList);
            return true;
        }        
        public override string ToString()
        {
            return "namespace " + name + ";";
        }
        public override string ToFormatString()
        {
            return m_Token.lexeme.ToString() + " " + m_NamespaceStateBlock.ToFormatString() + ";";
        }
    }

    public partial class FileMetaNamespace : FileDefineNamespace
    {
        private Node m_NamespaceNode = null;
        private Node m_NamespaceNameNode = null;
        private Token m_BraceBeginToken = null;
        private Token m_BraceEndToken = null;

        public FileMetaNamespace topLevelFileMetaNamespace = null;

        private List<FileMetaNamespace> m_MetaNamespaceList = new List<FileMetaNamespace>();
        private List<FileMetaClass> m_ChildrenClassList = new List<FileMetaClass>();

        private static Stack<FileMetaNamespace> s_MetaNamespaceStack = new Stack<FileMetaNamespace>();
        public Stack<FileMetaNamespace> namespaceStack
        {
            get
            {
                s_MetaNamespaceStack.Clear();
                var t = topLevelFileMetaNamespace;
                while (t != null)
                {
                    s_MetaNamespaceStack.Push(t);
                    t = t.topLevelFileMetaNamespace;
                }
                s_MetaNamespaceStack.Push(this);

                return s_MetaNamespaceStack;
            }
        }
        public FileMetaNamespace(Node namespaceNode, Node namespaceNameNode)
        {
            m_NamespaceNode = namespaceNode;
            m_NamespaceNameNode = namespaceNameNode;

            Node blockNode = namespaceNode.blockNode;

            m_Token = m_NamespaceNode.token;
            m_BraceBeginToken = blockNode.token;
            m_BraceEndToken = blockNode.endToken;
            m_NamespaceStateBlock = NamespaceStatementBlock.CreateStateBlock(m_NamespaceNameNode.linkTokenList);

        }        
        public FileMetaNamespace AddFileNamespace( FileMetaNamespace dln )
        {
            dln.topLevelFileMetaNamespace = this;
            m_MetaNamespaceList.Add(dln);
            dln.m_Deep = this.deep + 1;
            return dln;
        }
        public void AddFileMetaClass( FileMetaClass mc )
        {
            mc.SetMetaNamespace(this);
            m_ChildrenClassList.Add(mc);
        }
        public override string ToString()
        {
            return "namespace " + name + "{}";
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            for (int i = 0; i < m_MetaNamespaceList.Count; i++)
            {
                m_MetaNamespaceList[i].SetDeep(_deep + 1);
            }
            for (int i = 0; i < m_ChildrenClassList.Count; i++)
            {
                m_ChildrenClassList[i].SetDeep(_deep + 1);
            }            
        }
        public new string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append( m_Token.lexeme.ToString() + " " + m_NamespaceStateBlock.ToFormatString());
            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append( "{" + Environment.NewLine );
            for (int i = 0; i < m_MetaNamespaceList.Count; i++)
            {
                sb.Append(m_MetaNamespaceList[i].ToFormatString() + Environment.NewLine);
            }
            for (int i = 0; i < m_ChildrenClassList.Count; i++)
            {
                sb.Append(m_ChildrenClassList[i].ToFormatString() + Environment.NewLine);
            }
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
