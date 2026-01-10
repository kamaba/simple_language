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

        public FileMetaMemberFunction(
            FileMeta fm,
            List<Token> modifiers,
            List<Token> typeTokens,
            Token nameToken,
            List<Token> paramTokens )
        {
            m_FileMeta = fm;

            // 1. 解析修饰符
            if (modifiers != null)
            {
                foreach (var t in modifiers)
                {
                    if (t.type == ETokenType.Public || t.type == ETokenType.Private || t.type == ETokenType.Projected || t.type == ETokenType.Extern)
                        m_PermissionToken = t;
                    else if (t.type == ETokenType.Override) m_OverrideToken = t;
                    else if (t.type == ETokenType.Static) m_StaticToken = t;
                    else if (t.type == ETokenType.Get) m_GetToken = t;
                    else if (t.type == ETokenType.Set) m_SetToken = t;
                    else if (t.type == ETokenType.Final) m_FinalToken = t;
                    else if (t.type == ETokenType.Interface) m_InterfaceToken = t;
                }
            }

            // 2. 解析返回类型（与成员变量使用同一类 FileMetaClassDefine 类型定义逻辑）
            if (typeTokens != null && typeTokens.Count > 0)
            {
                m_DefineMetaClass = new FileMetaClassDefine(m_FileMeta, typeTokens);
            }

            // 3. 函数名 token
            m_Token = nameToken;

            // 4. 解析参数列表：paramTokens 应包含完整的 "( ... )" token 序列
            if (paramTokens != null && paramTokens.Count > 0)
            {
                ParseParametersFromTokens(paramTokens);
            }

            //// 5. 函数体块 token：与原来一样，只记录 { } 边界，内部语句由 TokenToFileMeta 另行拆分
            //if (blockTokens != null && blockTokens.Count >= 2)
            //{
            //    m_LeftBraceToken = blockTokens[0];
            //    m_RightBraceToken = blockTokens[blockTokens.Count - 1];
            //    m_FileMetaBlockSyntax = new FileMetaBlockSyntax(m_FileMeta, m_LeftBraceToken, m_RightBraceToken);
            //}
        }
        
        /// <summary>
        /// 由 Token 管线在确定完整的函数体 { } token 范围之后调用，用于在成员函数上初始化块语法节点，
        /// 然后再由 TokenToFileMeta.ParseFunctionBodyTokens 把内部语句拆分成 FileMetaSyntax。
        /// </summary>
        public void InitializeBlockFromTokens(List<Token> blockTokens)
        {
            if (blockTokens == null || blockTokens.Count < 2)
                return;

            m_LeftBraceToken = blockTokens[0];
            m_RightBraceToken = blockTokens[blockTokens.Count - 1];
            m_FileMetaBlockSyntax = new FileMetaBlockSyntax(m_FileMeta, m_LeftBraceToken, m_RightBraceToken);
        }

        private void ParseParametersFromTokens(List<Token> paramTokens)
        {
            // 期望格式: '(' [参数1 [, 参数2 ...]] ')'
            if (paramTokens == null || paramTokens.Count < 2)
                return;

            int index = 0;
            if (paramTokens[index].type != ETokenType.LeftPar)
                return;

            index++; // 跳过 '('
            List<Token> currentParam = new List<Token>();
            int parenDepth = 1;

            for (; index < paramTokens.Count; index++)
            {
                var t = paramTokens[index];

                if (t.type == ETokenType.LeftPar)
                {
                    parenDepth++;
                    currentParam.Add(t);
                }
                else if (t.type == ETokenType.RightPar)
                {
                    parenDepth--;
                    if (parenDepth == 0)
                    {
                        // 结束整个参数列表
                        AddParameterIfAny(currentParam);
                        break;
                    }
                    currentParam.Add(t);
                }
                else if (t.type == ETokenType.Comma && parenDepth == 1)
                {
                    // 顶层逗号分隔一个参数
                    AddParameterIfAny(currentParam);
                    currentParam.Clear();
                }
                else
                {
                    currentParam.Add(t);
                }
            }
        }

        private void AddParameterIfAny(List<Token> paramTokens)
        {
            if (paramTokens == null)
                return;

            // 去掉首尾空白
            int start = 0;
            int end = paramTokens.Count - 1;
            while (start <= end && (paramTokens[start].type == ETokenType.Space || paramTokens[start].type == ETokenType.LineEnd))
                start++;
            while (end >= start && (paramTokens[end].type == ETokenType.Space || paramTokens[end].type == ETokenType.LineEnd))
                end--;

            if (end < start)
                return;

            var slice = paramTokens.GetRange(start, end - start + 1);

            // 参数格式与成员变量类似: [TypeTokens] Name [= 表达式]
            // 暂时只支持 "类型 名称" 或 "名称"，使用 Identifier/Type 简单拆分

            List<Token> idOrType = new List<Token>();
            foreach (var t in slice)
            {
                if (t.type == ETokenType.Identifier || t.type == ETokenType.Type)
                {
                    idOrType.Add(t);
                }
            }

            if (idOrType.Count == 0)
                return;

            Token nameTok = idOrType[idOrType.Count - 1];
            List<Token> typeTokens = null;
            if (idOrType.Count > 1)
            {
                typeTokens = idOrType.GetRange(0, idOrType.Count - 1);
            }

            var param = new FileMetaParamterDefine();
            param.SetFileMeta(m_FileMeta);
            typeof(FileMetaBase)
                .GetField("m_Token", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(param, nameTok);

            if (typeTokens != null && typeTokens.Count > 0)
            {
                // 复用 FileMetaClassDefine 来解析参数类型
                var classDef = new FileMetaClassDefine(m_FileMeta, typeTokens);
                // 将解析好的类型绑定到参数
                typeof(FileMetaParamterDefine)
                    .GetField("m_ClassDefineRef", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(param, classDef);
            }

            AddMetaParamter(param);
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
