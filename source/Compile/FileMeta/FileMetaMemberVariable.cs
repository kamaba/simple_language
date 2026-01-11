//****************************************************************************
//  File:      FileMetaMemberVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************


using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Compile
{
    public sealed class FileMetaMemberVariable : FileMetaBase
    {
        public enum EMemberDataType
        {
            None,
            NameClass,
            //NoNameClass,
            Array,
            KeyValue,
            ConstVariable,
        }

        public FileMetaClassDefine classDefineRef => m_ClassDefineRef;
        public Token permissionToken => m_PermissionToken;
        public Token staticToken => m_StaticToken;
        public Token nameToken => m_Token;
        public FileMetaBaseTerm express => m_Express;
        public Token assignToken => m_AssignToken;
        public List<FileMetaMemberVariable> fileMetaMemberVariable => m_FileMetaMemberVariableList;
        //public FileMetaCallTerm fileMetaCallTermValue => m_FileMetaCallTermValue;
        public EMemberDataType DataType => m_MemberDataType;

        private FileMetaClassDefine m_ClassDefineRef;
        private Token m_AssignToken = null;
        private Token m_PermissionToken = null;
        private Token m_StaticToken = null;        
        private FileMetaBaseTerm m_Express;
        private List<FileMetaMemberVariable> m_FileMetaMemberVariableList = new List<FileMetaMemberVariable>();
        private EMemberDataType m_MemberDataType = EMemberDataType.None;
        //private FileMetaCallTerm m_FileMetaCallTermValue = null;
        public FileMetaMemberVariable(FileMeta fm, List<Token> modifiers, List<Token> typeTokens, Token nameToken, List<Token> exprTokens)
        {
            m_FileMeta = fm;
 
             // 权限/修饰符
             if (modifiers != null)
             {
                 foreach (var t in modifiers)
                 {
                     if (t.type == ETokenType.Extern || t.type == ETokenType.Public || t.type == ETokenType.Private || t.type == ETokenType.Projected)
                     {
                         m_PermissionToken = t;
                     }
                     else if (t.type == ETokenType.Static)
                     {
                         m_StaticToken = t;
                     }
                 }
             }
 
             // 名称 token
             m_Token = nameToken;
 
             // 类型定义，与旧逻辑保持一致：统一通过 FileMetaClassDefine 解析复杂类型
             if (typeTokens != null && typeTokens.Count > 0)
             {
                 m_ClassDefineRef = new FileMetaClassDefine(m_FileMeta, typeTokens);
                // 与旧构造逻辑保持一致：有类型+常量初始化时视为 ConstVariable
                m_MemberDataType = EMemberDataType.ConstVariable;
             }
 
             // 初始化表达式 token 列表，延用原来的 ParseExpress 逻辑
             if (exprTokens != null && exprTokens.Count > 0)
             {
                 // 右侧表达式可能包含前导的 '='，在这里做一次简单的裁剪
                 int start = 0;
                 while (start < exprTokens.Count &&
                        (exprTokens[start].type == ETokenType.Space || exprTokens[start].type == ETokenType.LineEnd))
                 {
                     start++;
                 }
                if (start < exprTokens.Count && exprTokens[start].type == ETokenType.Assign)
                {
                    // 记录 '=' 号，与旧构造中的 m_AssignToken 一致
                    m_AssignToken = exprTokens[start];
                    start++;
                }
 
                 if (start < exprTokens.Count)
                 {
                     var rhs = exprTokens.GetRange(start, exprTokens.Count - start);
                    // 直接使用 Token 版表达式构造，与旧逻辑一致
                    m_Express = FileMetatUtil.CreateFileMetaExpressFromTokens(
                        m_FileMeta,
                        rhs,
                        FileMetaTermExpress.EExpressType.MemberVariable);
                    m_MemberDataType = EMemberDataType.ConstVariable;
                 }
             }
         }
        private static void TrimTokenList(List<Token> list)
        {
            if (list == null || list.Count == 0) return;
            int start = 0;
            int end = list.Count - 1;
            while (start <= end && (list[start].type == ETokenType.Space || list[start].type == ETokenType.LineEnd || list[start].type == ETokenType.SemiColon))
                start++;
            while (end >= start && (list[end].type == ETokenType.Space || list[end].type == ETokenType.LineEnd || list[end].type == ETokenType.SemiColon))
                end--;
            if (start == 0 && end == list.Count - 1) return;
            if (end < start)
            {
                list.Clear();
                return;
            }
            var trimmed = list.GetRange(start, end - start + 1);
            list.Clear();
            list.AddRange(trimmed);
        }
        public void AddFileMemberVariable(FileMetaMemberVariable fmmd)
        {
            m_FileMetaMemberVariableList.Add(fmmd);
        }
        public FileMetaMemberVariable GetFileMetaMemberDataByName(string name)
        {
            FileMetaMemberVariable fmmd = m_FileMetaMemberVariableList.Find(a => a.name == name);

            return fmmd;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;

            for (int i = 0; i < m_FileMetaMemberVariableList.Count; i++)
            {
                m_FileMetaMemberVariableList[i].SetDeep(m_Deep + 1);
            }
        }
        public override string ToString()
        {
            return m_Token.lexeme.ToString();
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            EPermission permis = CompilerUtil.GetPerMissionByString(m_PermissionToken?.lexeme.ToString());
            if( permis == EPermission.Null )
            {
                sb.Append("_public ");
            }
            else
            {
                sb.Append(permis.ToFormatString());
            }
            if(m_StaticToken!= null )
            {
                sb.Append(" " + m_StaticToken.lexeme.ToString());
            }

            if (m_MemberDataType == EMemberDataType.NameClass)
            {
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.AppendLine(name);
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.AppendLine("{");
                for (int i = 0; i < m_FileMetaMemberVariableList.Count; i++)
                {
                    sb.AppendLine(m_FileMetaMemberVariableList[i].ToFormatString());
                }
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.Append("}");

                if (m_ClassDefineRef != null)
                    sb.Append(" " + m_ClassDefineRef.ToFormatString());
                sb.Append(" " + name);
                if (m_AssignToken != null)
                {
                    sb.Append(" " + m_AssignToken.lexeme.ToString());
                    sb.Append(" " + m_Express?.ToFormatString());
                }
                sb.Append(";");
            }
            else if (m_MemberDataType == EMemberDataType.KeyValue)
            {
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.Append(name + " = ");
                sb.Append(m_Express.ToFormatString());
                sb.Append(";");
            }
            else if (m_MemberDataType == EMemberDataType.Array)
            {
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.Append(name + " = [");
                for (int i = 0; i < m_FileMetaMemberVariableList.Count; i++)
                {
                    sb.Append(m_FileMetaMemberVariableList[i].ToFormatString());
                    if (i < m_FileMetaMemberVariableList.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append("]");
                sb.Append(";");
            }
            else if (m_MemberDataType == EMemberDataType.ConstVariable )
            {
                sb.Append(m_ClassDefineRef?.ToFormatString());
                sb.Append( " " + name + " = ");
                sb.Append(m_Express?.ToFormatString());
            }
            else
            {
                sb.Append("没有差别MemberDataType");
            }
            return sb.ToString();
        }
    }
}