//****************************************************************************
//  File:      FileMetaNamespace.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    public partial class FileMetaNamespace : FileMetaBase
    {
        public FileMetaNamespace topLevelFileMetaNamespace => m_TopLevelFileMetaNamespace;
        public NamespaceStatementBlock namespaceStatementBlock => m_NamespaceStatementBlock;

        private NamespaceStatementBlock m_NamespaceStatementBlock = null;
        private Token m_BraceBeginToken = null;
        private Token m_BraceEndToken = null;
        private bool m_IsSearchNamespace = false;

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
        private NamespaceStatementBlock m_NamespaceStateBlock = null;
        private FileMetaNamespace m_TopLevelFileMetaNamespace = null;
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
            this.m_NamespaceStatementBlock = fmn.m_NamespaceStatementBlock;
        }
        public FileMetaNamespace(NamespaceStatementBlock nsBlock )
        {
            m_NamespaceStatementBlock = nsBlock ?? throw new ArgumentNullException(nameof(nsBlock));
        }
        public void SetBraceToken( Token bs, Token es )
        {
            m_BraceBeginToken = bs;
            m_BraceEndToken = es;
            if (bs != null)
            { m_IsSearchNamespace = true; }
        }
        public FileMetaNamespace AddFileNamespace( FileMetaNamespace dln )
        {
            dln.m_TopLevelFileMetaNamespace = this;
            m_MetaNamespaceList.Add(dln);
            dln.m_Deep = this.deep + 1;

            return dln;
        }
        public void AddFileMetaClass( FileMetaClass mc )
        {
            mc.SetMetaNamespace(this);
            m_ChildrenClassList.Add(mc);
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
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("namespace ");
            if (m_NamespaceStateBlock != null)
            {
                sb.AppendLine("{");
                sb.Append(m_NamespaceStateBlock.namespaceString);
                sb.AppendLine("}");
            }
            return sb.ToString();
        }
    }
}
