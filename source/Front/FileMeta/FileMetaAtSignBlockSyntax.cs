//****************************************************************************
//  File:      FileMetaAtSignBlockSyntax.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/9/29 12:00:00
//  Description: @<tag>(...){...} 不透明内联块语法（统一 @ 识别：代码段内 → AtSignLabel 语义）
//****************************************************************************
using System;
using System.Text;

namespace SimpleLanguage.Compile
{
    public class FileMetaAtSignBlockSyntax : FileMetaSyntax
    {
        // Lexer raw 捕获的 @<tag>(...){...} 整块 token：
        //   主 token        : '@' 位置（lexeme = 块原始文本、extend = 标签名）
        //   children[0][0]  : '(' 位置 token（lexeme = 参数表原文 paramsText）
        //   children[1][0]  : '{' 位置 token（lexeme = 块体原文 bodyText）
        // 语义解析（通道扫描/插件转调/块登记/就地脱糖）在 MetaCore 层 AtSignLabelBlockDispatch 完成

        public Token atSignBlockToken => m_Token;
        public string label => m_Token?.extend?.ToString();
        public string paramsText => GetChildTokenLexeme(0);
        public string bodyText => GetChildTokenLexeme(1);

        /// <summary> '(' 所在 1-based 行 </summary>
        public int parenLine => GetChildToken(0)?.sourceBeginLine ?? m_Token.sourceBeginLine;
        /// <summary> '{' 所在 1-based 行（= 通道扫描/插件报错公式的 lbraceLine 基准） </summary>
        public int lbraceLine => GetChildToken(1)?.sourceBeginLine ?? m_Token.sourceBeginLine;

        public FileMetaAtSignBlockSyntax(FileMeta fm, Token _atSignBlockToken)
        {
            m_FileMeta = fm;
            m_Token = _atSignBlockToken;
        }

        private Token GetChildToken(int index)
        {
            var cl = m_Token?.childrenTokensList;
            if (cl == null || index >= cl.Count || cl[index].Count == 0)
                return null;
            return cl[index][0];
        }
        private string GetChildTokenLexeme(int index)
        {
            var t = GetChildToken(index);
            return t?.lexeme?.ToString();
        }

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append(m_Token?.lexeme?.ToString());
            sb.Append(Environment.NewLine);
            return sb.ToString();
        }
    }
}
