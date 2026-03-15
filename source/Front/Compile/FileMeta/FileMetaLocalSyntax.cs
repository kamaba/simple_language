//****************************************************************************
//  File:      FileMetaLocalSyntax.cs
// ------------------------------------------------
//  Description:  File-level global/local block syntax
//****************************************************************************

using SimpleLanguage.Logging;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Compile
{
    public sealed class FileMetaGlobalOrLocalSyntax : FileMetaBase
    {
        public bool isLocal => m_IsLocal;
        public FileMetaBlockSyntax blockSyntax => m_BlockSyntax;
        public List<FileMetaMemberFunction> functionList => m_FunctionList;

        private readonly bool m_IsLocal;
        private readonly FileMetaBlockSyntax m_BlockSyntax;
        private readonly List<FileMetaMemberFunction> m_FunctionList = new List<FileMetaMemberFunction>();
        private readonly FileMetaMemberFunction m_InitFileMetaMemberFunction = null;

        public FileMetaGlobalOrLocalSyntax(FileMeta fm, Token token, Node blockNode, bool isLocal)
        {
            m_IsLocal = isLocal;
            m_FileMeta = fm;
            m_Token = token;

            if (blockNode == null || blockNode.nodeType != ENodeType.Brace)
            {
                Log.AddInStructFileMeta(EError.None, "Error " + (m_IsLocal ? "local" : "global") + " ºó±ØÐë¸ú {} ¿é");
                return;
            }

            var left = blockNode.token;
            var right = blockNode.endToken;
            if (left == null || right == null)
            {
                Debug.Assert(false, (m_IsLocal ? "local" : "global") + " block token missing");
                return;
            }

            m_BlockSyntax = new FileMetaBlockSyntax(fm, left, right);
        }

        public void AddInitSyntax(FileMetaSyntax syntax)
        {
            m_BlockSyntax?.AddFileMetaSyntax(syntax);
        }

        public void AddFunction(FileMetaMemberFunction fn)
        {
            if (fn == null) return;
            m_FunctionList.Add(fn);
        }

        public void AddLocalInitSyntax(FileMetaSyntax syntax) => AddInitSyntax(syntax);
        public void AddLocalFunction(FileMetaMemberFunction fn) => AddFunction(fn);
        public void AddGlobalInitSyntax(FileMetaSyntax syntax) => AddInitSyntax(syntax);
        public void AddGlobalFunction(FileMetaMemberFunction fn) => AddFunction(fn);

        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_BlockSyntax?.SetDeep(_deep);
        }

        public override string ToFormatString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < deep; i++) sb.Append(Global.tabChar);
            sb.Append(m_IsLocal ? "local" : "global");
            if (m_BlockSyntax != null)
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_BlockSyntax.ToFormatString());
            }
            return sb.ToString();
        }
    }
}
