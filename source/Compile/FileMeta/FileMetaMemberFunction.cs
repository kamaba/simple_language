//****************************************************************************
//  File:      FileMetaMemberFunction.cs
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
    public class FileMetaParamterDefine : FileMetaBase
    {
        public FileMetaClassDefine classDefineRef => m_ClassDefineRef;
        public FileMetaBaseTerm express => m_Express;
        public Token paramsToken => m_ParamsToken;

        private Token m_AssignToken = null;
        private Token m_ParamsToken = null;
        private FileMetaClassDefine m_ClassDefineRef = null;
        private FileMetaBaseTerm m_Express;
        
        // 通过 token 片段构建参数定义：支持 [TypeTokens] Name [= 默认值]
        public FileMetaParamterDefine(FileMeta fm, List<Token> paramTokens)
        {
            if (fm == null || paramTokens == null || paramTokens.Count == 0)
                return;

            m_FileMeta = fm;

            // 去掉首尾空白
            int start = 0;
            int end = paramTokens.Count - 1;
            while (start <= end && (paramTokens[start].type == ETokenType.Space || paramTokens[start].type == ETokenType.LineEnd))
                start++;
            while (end >= start && (paramTokens[end].type == ETokenType.Space || paramTokens[end].type == ETokenType.LineEnd))
                end--;
            if (end < start) return;

            var slice = paramTokens.GetRange(start, end - start + 1);

            // 寻找 '='，用于拆分默认值表达式
            int assignIndex = -1;
            for (int i = 0; i < slice.Count; i++)
            {
                if (slice[i].type == ETokenType.Assign)
                {
                    assignIndex = i;
                    break;
                }
            }

            List<Token> headerTokens;
            List<Token> defaultExprTokens = null;
            if (assignIndex >= 0)
            {
                headerTokens = slice.GetRange(0, assignIndex);
                if (assignIndex + 1 < slice.Count)
                {
                    defaultExprTokens = slice.GetRange(assignIndex + 1, slice.Count - assignIndex - 1);
                }
                m_AssignToken = slice[assignIndex];
            }
            else
            {
                headerTokens = slice;
            }

            // 从 headerTokens 中抽取类型和名称：最后一个 Identifier/Type 作为名称，其前面的作为类型
            List<Token> idOrType = new List<Token>();
            for (int i = 0; i < headerTokens.Count; i++)
            {
                var t = headerTokens[i];
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

            m_Token = nameTok;

            if (typeTokens != null && typeTokens.Count > 0)
            {
                m_ClassDefineRef = new FileMetaClassDefine(m_FileMeta, typeTokens);
            }

            // 默认值表达式：使用统一表达式入口，类型为 ParamVariable
            if (defaultExprTokens != null && defaultExprTokens.Count > 0)
            {
                m_Express = FileMetatUtil.CreateFileMetaExpressFromTokens(
                    m_FileMeta,
                    defaultExprTokens,
                    FileMetaTermExpress.EExpressType.ParamVariable);
            }
        }

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
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
        }
        public void SetFileMetaBlockSyntax( FileMetaBlockSyntax fmbs )
        {
            m_FileMetaBlockSyntax = fmbs;
            m_FileMetaBlockSyntax.SetFileMeta(m_FileMeta);
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

            var param = new FileMetaParamterDefine(m_FileMeta, slice);
            AddMetaParamter(param);
        }
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
