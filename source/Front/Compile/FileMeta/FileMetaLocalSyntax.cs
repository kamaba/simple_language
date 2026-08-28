//****************************************************************************
//  File:      FileMetaLocalSyntax.cs
// ------------------------------------------------
//  Description:  File-level global/local block syntax
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    public sealed class FileMetaLocalSyntax : FileMetaBase
    {
        public FileMetaBlockSyntax blockSyntax => m_BlockSyntax;
        public List<FileMetaMemberFunction> functionList => m_FunctionList;

        private readonly FileMetaBlockSyntax m_BlockSyntax;
        private readonly List<FileMetaMemberFunction> m_FunctionList = new List<FileMetaMemberFunction>();
        public FileMetaLocalSyntax(FileMeta fm, Token token, Node blockNode) 
        {
            m_FileMeta = fm;
            m_Token = token;
            m_BlockSyntax = new FileMetaBlockSyntax(fm, blockNode.token, blockNode.endToken);
        }
        public void AddInitSyntax(FileMetaSyntax syntax)
        {
            m_BlockSyntax?.AddFileMetaSyntax(syntax);
        }
        public void AddFunction(FileMetaMemberFunction fn)
        {
            m_FunctionList.Add(fn);
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_BlockSyntax?.SetDeep(_deep);
        }

        public override string ToFormatString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < deep; i++) sb.Append(Global.tabChar);
            sb.Append("local");
            if (m_BlockSyntax != null)
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_BlockSyntax.ToFormatString());
            }
            return sb.ToString();
        }
    }
}
