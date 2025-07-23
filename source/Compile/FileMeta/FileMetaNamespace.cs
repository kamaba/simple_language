//****************************************************************************
//  File:      FileMetaNamespace.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Compile.Parse;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile.CoreFileMeta
{
    public partial class FileMetaNamespace : FileMetaBase
    {
        public bool isSearchNamespace => m_IsSearchNamespace;
        public Node namespaceNode => m_NamespaceNode;
        public Node namespaceNameNode => m_NamespaceNameNode;

        private Node m_NamespaceNode = null;
        private Node m_NamespaceNameNode = null;
        private Token m_BraceBeginToken = null;
        private Token m_BraceEndToken = null;
        private bool m_IsSearchNamespace = false;

        //private List<Node> m_NodeList = new List<Node>();
        public new string name
        {
            get
            {
                if (m_NamespaceStateBlock != null)
                {
                    return m_NamespaceStateBlock.namespaceString;
                }
                return "";
            }
        }
        //public List<MetaNamespace> metaNamespaceList
        //{
        //    get
        //    {
        //        if (m_NamespaceStateBlock != null)
        //        {
        //            return m_NamespaceStateBlock.metaNamespaceList;
        //        }
        //        return null;
        //    }
        //}
        public NamespaceStatementBlock namespaceStatementBlock => m_NamespaceStateBlock;
        protected NamespaceStatementBlock m_NamespaceStateBlock { get; set; }

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
        public FileMetaNamespace( FileMetaNamespace fmn )
        {
            this.m_NamespaceNode = fmn.m_NamespaceNode;
            this.m_NamespaceNameNode = fmn.m_NamespaceNameNode;
        }

        public FileMetaNamespace(Node namespaceNode, Node namespaceNameNode)
        {
            m_NamespaceNode = namespaceNode;
            m_NamespaceNameNode = namespaceNameNode;


            if (namespaceNode == null)
            {
                Log.AddInStructFileMeta(EError.None, "Error 在解析namespace 中，没有找到namespace设置的名称!!");
                return;
            }

            Node blockNode = namespaceNode.blockNode;

            m_Token = m_NamespaceNode.token;
            m_IsSearchNamespace = true;
            if (blockNode != null )
            {
                m_BraceBeginToken = blockNode.token;
                m_BraceEndToken = blockNode.endToken;
                m_IsSearchNamespace = false;
            }
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
