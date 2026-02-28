//****************************************************************************
//  File:      FileMetaLocalSyntax.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/2/28 12:00:00
//  Description:  File-level local{} block syntax
//****************************************************************************

using SimpleLanguage.Logging;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Compile
{
    public sealed class FileMetaLocalSyntax : FileMetaBase
    {
        public FileMetaBlockSyntax executeBlockSyntax => m_ExecuteBlockSyntax;
        public FileMetaBlockSyntax initBlockSyntax => m_InitBlockSyntax;
        public List<FileMetaMemberFunction> localFunctionList => m_LocalFunctionList;

        private readonly FileMetaBlockSyntax m_ExecuteBlockSyntax;
        private readonly FileMetaBlockSyntax m_InitBlockSyntax;
        private readonly List<FileMetaMemberFunction> m_LocalFunctionList = new List<FileMetaMemberFunction>();

        public FileMetaLocalSyntax(FileMeta fm, Token localToken, Node blockNode)
        {
            m_FileMeta = fm;
            m_Token = localToken;
            if (blockNode == null || blockNode.nodeType != ENodeType.Brace)
            {
                Log.AddInStructFileMeta(EError.None, "Error local ºó±ØÐë¸ú {} ¿é");
                return;
            }

            var left = blockNode.token;
            var right = blockNode.endToken;
            if (left == null || right == null)
            {
                Debug.Assert(false, "local block token missing");
                return;
            }
            m_ExecuteBlockSyntax = new FileMetaBlockSyntax(fm, left, right);
            // initBlockSyntax will be filled by StructParseFrame.ParseLocalContent
            m_InitBlockSyntax = new FileMetaBlockSyntax(fm, left, right);
        }

        public void AddLocalInitSyntax(FileMetaSyntax syntax)
        {
            m_InitBlockSyntax?.AddFileMetaSyntax(syntax);
        }

        public void AddLocalFunction(FileMetaMemberFunction fn)
        {
            if (fn == null) return;
            m_LocalFunctionList.Add(fn);
        }

        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_ExecuteBlockSyntax?.SetDeep(_deep);
        }

        public override string ToFormatString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < deep; i++) sb.Append(Global.tabChar);
            sb.Append("local");
            if (m_ExecuteBlockSyntax != null)
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_ExecuteBlockSyntax.ToFormatString());
            }
            return sb.ToString();
        }
    }
}
