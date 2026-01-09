//****************************************************************************
//  File:      FileMetaMemberFunction.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    public class FileMetaParamterDefine : FileMetaBase
    {
        public FileMetaClassDefine classDefineRef => m_ClassDefineRef;
        public FileMetaBaseTerm express => m_Express;
        public Token paramsToken => m_ParamsToken;

        private Token m_AssignToken = null;
        private Token m_ParamsToken = null;
        private FileMetaClassDefine m_ClassDefineRef = null;
        private FileMetaBaseTerm m_Express;

        // Node 版本构造与解析逻辑（legacy，已准备迁移到 Token 管线）
        // public FileMetaParamterDefine(FileMeta fileMeta, List<Node> list) { ... }
        // public bool ParseBuildMetaParamter(List<Node> inputNodeList) { ... }
        // public bool GetNameAndTypeNode(List<Node> listDefieNode, ref Node nameNode, ref Node typeNode, ref Token paramstoken) { ... }

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            if (m_ClassDefineRef != null)
                sb.Append(" " + m_ClassDefineRef.ToFormatString());
            sb.Append(" " + m_Token?.lexeme.ToString());
            if (m_AssignToken != null)
            {
                sb.Append(" " + m_AssignToken.lexeme.ToString());
                sb.Append(" " + m_Express.ToFormatString());
            }
            return sb.ToString();
        }
    }
    public class FileMetaFunction : FileMetaBase
    {
        public FileMetaClassDefine defineMetaClass => m_DefineMetaClass;
        public FileMetaBlockSyntax fileMetaBlockSyntax => m_FileMetaBlockSyntax;
        public List<FileMetaParamterDefine> metaParamtersList => m_MetaParamtersList;
        public List<FileMetaTemplateDefine> metaTemplatesList => m_MetaTemplatesList;

        protected List<FileMetaParamterDefine> m_MetaParamtersList = new List<FileMetaParamterDefine>();
        protected List<FileMetaTemplateDefine> m_MetaTemplatesList = new List<FileMetaTemplateDefine>();
        protected FileMetaClassDefine m_DefineMetaClass = null;
        protected FileMetaBlockSyntax m_FileMetaBlockSyntax = null;
        protected FileMetaFunction() { }

        public void AddMetaParamter(FileMetaParamterDefine fmp)
        {
            m_MetaParamtersList.Add(fmp);
            fmp.SetFileMeta(m_FileMeta);
        }
        public void AddFileMetaSyntax(FileMetaSyntax fms)
        {
            m_FileMetaBlockSyntax?.AddFileMetaSyntax(fms);
            fms.SetFileMeta(m_FileMeta);
        }
        public void AddMetaTemplate( FileMetaTemplateDefine fmtd )
        {
            m_MetaTemplatesList.Add(fmtd);
            fmtd.SetFileMeta(m_FileMeta);
        }
    }
    public class FileMetaMemberFunction : FileMetaFunction
    {
        public Token interfaceToken => m_InterfaceToken;
        public Token staticToken => m_StaticToken;
        public Token overrideToken => m_OverrideToken;
        public Token permissionToken => m_PermissionToken;
        public Token getToken => m_GetToken;
        public Token setToken => m_SetToken;
        public Token finalToken => m_FinalToken;

        private Token m_InterfaceToken = null;
        private Token m_StaticToken = null;
        private Token m_FinalToken = null;
        private Token m_GetToken = null;
        private Token m_SetToken = null;
        private Token m_OverrideToken = null;
        private Token m_PermissionToken = null;
        private Token m_LeftBraceToken = null;
        private Token m_RightBraceToken = null;
        // legacy Node-based block, superseded by token-based ctor
        // private Node m_BlockNode;

        // Node 版本构造方法（legacy，已由 Token 版本取代）
        // public FileMetaMemberFunction(FileMeta fm, Node block, List<Node> nodeList) { ... }

        public FileMetaMemberFunction(FileMeta fm, List<Token> tokens, List<Token> blockTokens)
        {
            m_FileMeta = fm;
            ParseFunctionFromTokens(tokens);
            // 使用 Token 版本的 Block 解析逻辑，不再依赖 Node
            if (blockTokens != null && blockTokens.Count >= 2)
            {
                // blockTokens 应该包含从 '{' 到 对应 '}' 的完整 Token 序列
                m_LeftBraceToken = blockTokens[0];
                m_RightBraceToken = blockTokens[blockTokens.Count - 1];

                // 仅记录块的起止位置，具体语句由 Token 管线在外部构造 FileMetaSyntax 并通过 AddFileMetaSyntax 填充
                m_FileMetaBlockSyntax = new FileMetaBlockSyntax(m_FileMeta, m_LeftBraceToken, m_RightBraceToken);
            }
        }

        private void ParseFunctionFromTokens(List<Token> tokens)
        {
            // 简化版 Token 解析逻辑，模仿 ParseFunction
             for (int i = 0; i < tokens.Count; i++)
             {
                 Token t = tokens[i];
                 if (t.type == ETokenType.Public || t.type == ETokenType.Private || t.type == ETokenType.Projected || t.type == ETokenType.Extern)
                     m_PermissionToken = t;
                 else if (t.type == ETokenType.Override) m_OverrideToken = t;
                 else if (t.type == ETokenType.Static) m_StaticToken = t;
                 else if (t.type == ETokenType.Get) m_GetToken = t;
                 else if (t.type == ETokenType.Set) m_SetToken = t;
                 else if (t.type == ETokenType.Final) m_FinalToken = t;
                 else if (t.type == ETokenType.Identifier)
                 {
                     // 这里需要识别是返回值类型还是函数名
                     // 简单策略：遇到 '(' 前的一个是函数名，再往前是返回值
                     if (i + 1 < tokens.Count && tokens[i+1].type == ETokenType.LeftPar)
                     {
                         m_Token = t; // 函数名
                         // 此时，如果前面还有 Identifier 或 Type，那就是返回值
                         // 为了更准确，应该从后往前找，或者维护更复杂的状态
                     }
                     else
                     {
                         // 可能是返回值类型的一部分
                     }
                 }
                 else if (t.type == ETokenType.Type || t.type == ETokenType.Void)
                 {
                      // 暂时认为是返回值
                 }
             }
             
             // 注意：参数列表和模板参数 parsing 需要识别 () 和 <>
             // 在线性 Token 流中，这部分由 TokenToFileMeta 分割好传进来会更好，或者在这里扫描
             // 假设 TokenToFileMeta 传进来的 tokens 包含了签名部分（直到 { 之前）
        }

        // Node 版本解析函数与参数/模板解析（legacy，已由 Token 管线取代）
        // public bool ParseFunction(List<Node> nodeList) { ... }
        // public void ParseParam(Node parNode) { ... }
        // public void ParseTemplate(Node node) { ... }

        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_FileMetaBlockSyntax?.SetDeep(m_Deep);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);

            EPermission permis = CompilerUtil.GetPerMissionByString( m_PermissionToken?.lexeme.ToString());
            if (permis == EPermission.Null)
            {
                sb.Append("_public");
            }
            else
            {
                sb.Append(permis.ToFormatString());
            }
            if ( m_StaticToken != null)
            {
                sb.Append(" " + m_StaticToken.lexeme.ToString());
            }
            if ( m_InterfaceToken != null)
            {
                sb.Append(" " + m_InterfaceToken.lexeme.ToString());
            }
            if ( m_OverrideToken != null)
            {
                sb.Append(" " + m_OverrideToken.lexeme.ToString());
            }
            if (m_DefineMetaClass != null)
            {
                sb.Append(" " + m_DefineMetaClass.ToFormatString() );
            }
            if(m_MetaTemplatesList.Count > 0 )
            {
                sb.Append("<");
                for( int i = 0; i < m_MetaTemplatesList.Count; i++ )
                {
                    sb.Append(m_MetaTemplatesList[i].ToFormatString());
                    if( i < m_MetaTemplatesList.Count - 1 )
                    sb.Append(",");
                }
                sb.Append(">");
            }
            sb.Append(" " + token?.lexeme.ToString() +"(" );
            for( int i = 0; i < m_MetaParamtersList.Count; i++ )
            {
                sb.Append(m_MetaParamtersList[i].ToFormatString());
                if (i < m_MetaParamtersList.Count - 1)
                    sb.Append(", ");
            }
            sb.Append(" )" + Environment.NewLine);            

            if( m_FileMetaBlockSyntax != null )
            {
                sb.Append(m_FileMetaBlockSyntax.ToFormatString());
            }

            return sb.ToString();
        }
    }
}
