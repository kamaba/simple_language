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
            foreach( var v in m_FunctionList )
            {
                v.SetDeep(_deep + 1);
            }
        }

        public override string ToFormatString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < deep; i++) sb.Append(Global.tabChar);
            sb.AppendLine("local");
            if (m_BlockSyntax != null)
            {
                for (int i = 0; i < m_BlockSyntax.deep; i++)
                    sb.Append(Global.tabChar);
                sb.Append(m_BlockSyntax.beginBlock.lexeme.ToString() + Environment.NewLine);
                for (int i = 0; i < m_BlockSyntax.fileMetaSyntax.Count; i++)
                {
                    sb.Append(m_BlockSyntax.fileMetaSyntax[i].ToFormatString());
                    sb.Append(Environment.NewLine);
                }
                foreach (var v in m_FunctionList)
                {
                    sb.AppendLine(v.ToFormatString());
                }
                for (int i = 0; i < m_BlockSyntax.deep; i++)
                    sb.Append(Global.tabChar);
                sb.Append("}");
            }
            return sb.ToString();
        }
    }
}
