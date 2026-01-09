//****************************************************************************
//  File:      FileMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Compile
{
    public partial class FileMetaClass : FileMetaBase
    {
        public bool innerClass { get; set; } = false;
        public bool isConst { get { return m_ConstToken != null; } }
        public bool isStatic { get { return m_StaticToken != null; } }
        public bool isEnum { get { return m_EnumToken != null; } }
        public bool isData { get { return m_DataToken != null; } }
        public bool isPartial => m_PartialToken != null;
        public Token preInterfaceToken => m_PreInterfaceToken;
        public MetaClass metaClass => m_MetaClass;
        public FileMetaClassDefine fileMetaExtendClass => m_FileMetaExtendClass;
        public List<FileMetaClassDefine> interfaceClassList => m_InterfaceClassList;
        public FileMetaNamespace topLevelFileMetaNamespace => m_TopLevelFileMetaNamespace;
        public FileMetaClass topLevelFileMetaClass => m_TopLevelFileMetaClass;
        public List<FileMetaTemplateDefine> templateDefineList => m_TemplateDefineList;
        public NamespaceStatementBlock namespaceBlock => m_NamespaceBlock;
        public List<FileMetaMemberVariable> memberVariableList => m_MemberVariableList;
        public List<FileMetaMemberFunction> memberFunctionList => m_MemberFunctionList;
        public List<FileMetaMemberData> memberDataList => m_MemberDataList;

        #region Token
        protected Token m_PermissionToken = null;
        protected Token m_PartialToken = null;
        protected Token m_PreInterfaceToken = null;
        protected Token m_SufInterfaceToken = null;
        protected Token m_ClassToken = null;
        protected Token m_EnumToken = null;
        protected Token m_DataToken = null;
        protected Token m_ConstToken = null;
        protected Token m_StaticToken = null;
        #endregion
        private MetaClass m_MetaClass = null;
        private FileMetaNamespace m_TopLevelFileMetaNamespace = null;
        private FileMetaClass m_TopLevelFileMetaClass = null;
        private FileMetaClassDefine m_FileMetaExtendClass = null;
        private List<FileMetaClassDefine> m_InterfaceClassList = new List<FileMetaClassDefine>();
        private List<FileMetaClass> m_ChildrenClassList = new List<FileMetaClass>();
        private List<FileMetaTemplateDefine> m_TemplateDefineList = new List<FileMetaTemplateDefine>();

        private List<FileMetaMemberVariable> m_MemberVariableList = new List<FileMetaMemberVariable>();
        private List<FileMetaMemberFunction> m_MemberFunctionList = new List<FileMetaMemberFunction>();
        private List<FileMetaMemberData> m_MemberDataList = new List<FileMetaMemberData>();
        private NamespaceStatementBlock m_NamespaceBlock = null;

        private StringBuilder stringBuilder = new StringBuilder();  
        // TokenToFileMeta 专用构造函数：各部分 token 已经分拆好，直接根据参数填充状态
        public FileMetaClass(
            FileMeta fm,
            List<Token> modifiers,
            Token classKeyword,
            List<Token> classNameTokens,
            List<Token> typeParameters,
            Token extendsKeyword,
            List<Token> baseClassTokens,
            Token interfaceKeyword,
            List<List<Token>> interfaceTokenLists)
        {
            m_FileMeta = fm;
            m_Id = ++s_IdCount;

            // 1. 直接根据参数设置权限 / 修饰符 / class 标记
            if (modifiers != null)
            {
                m_PermissionToken = modifiers.Find(t => t.type == ETokenType.Public || t.type == ETokenType.Private || t.type == ETokenType.Projected || t.type == ETokenType.Extern);
                m_StaticToken     = modifiers.Find(t => t.type == ETokenType.Static);
                m_ConstToken      = modifiers.Find(t => t.type == ETokenType.Const);
                m_PartialToken    = modifiers.Find(t => t.type == ETokenType.Partial);
            }

            if (classKeyword != null)
            {
                if (classKeyword.type == ETokenType.Class)      m_ClassToken      = classKeyword;
                else if (classKeyword.type == ETokenType.Interface) m_PreInterfaceToken = classKeyword;
                else if (classKeyword.type == ETokenType.Enum)  m_EnumToken       = classKeyword;
                else if (classKeyword.type == ETokenType.Data)  m_DataToken       = classKeyword;
            }

            // 2. 类名（支持多段）→ m_NamespaceBlock + name/m_Token
            if (classNameTokens != null && classNameTokens.Count > 0)
            {
                var nsBlock = NamespaceStatementBlock.CreateStateBlock(classNameTokens);
                if (nsBlock != null)
                {
                    m_NamespaceBlock = nsBlock;
                    var nsList = nsBlock.namespaceList;
                    if (nsList.Count > 0)
                    {
                        // NamespaceStatementBlock 的最后一段即为类名字符串
                        // 但我们仍保留原始 token 供 name/m_Token 使用
                        m_Token = classNameTokens[classNameTokens.Count - 1];
                    }
                }
            }

            // 3. 模板参数
            m_TemplateDefineList.Clear();
            if (typeParameters != null && typeParameters.Count > 0)
            {
                // 直接复用 ParseTemplateDefine 逻辑
                ParseTemplateDefine(typeParameters, 0);
            }

            // 4. 继承类
            m_FileMetaExtendClass = null;
            if (extendsKeyword != null && baseClassTokens != null && baseClassTokens.Count > 0)
            {
                var extendDef = CreateClassDefineFromTokens(baseClassTokens);
                if (extendDef != null)
                {
                    m_FileMetaExtendClass = extendDef;
                }
            }

            // 5. 接口列表
            m_InterfaceClassList.Clear();
            if (interfaceKeyword != null && interfaceTokenLists != null)
            {
                foreach (var it in interfaceTokenLists)
                {
                    if (it == null || it.Count == 0) continue;
                    var ifaceDef = CreateClassDefineFromTokens(it);
                    if (ifaceDef != null)
                    {
                        m_InterfaceClassList.Add(ifaceDef);
                    }
                }
            }

            // 若 name 还没设置，则至少保证 m_Token 不为 null（用于错误定位）
            if (m_Token == null && classNameTokens != null && classNameTokens.Count > 0)
            {
                m_Token = classNameTokens[0];
            }
        }
        // 解析模板参数列表 <T, U:Base>
        private int ParseTemplateDefine(List<Token> tokens, int startIndex)
        {
            bool inConstraint = false;
            List<Token> currentNameTokens = new List<Token>();
            List<Token> currentExtendsTokens = new List<Token>();

            int index = startIndex;
            // 跳过起始的 '<'，避免被当成模板名的一部分
            if (index < tokens.Count && tokens[index].type == ETokenType.Less)
            {
                index++;
            }
             while (index < tokens.Count)
             {
                 var t = tokens[index];
                 if (t.type == ETokenType.Greater)
                 {
                     if (currentNameTokens.Count > 0)
                     {
                         CreateTemplateDefine(currentNameTokens, currentExtendsTokens);
                     }
                     return index + 1;
                 }
                 if (t.type == ETokenType.Comma)
                 {
                     if (currentNameTokens.Count > 0)
                     {
                         CreateTemplateDefine(currentNameTokens, currentExtendsTokens);
                         currentNameTokens = new List<Token>();
                         currentExtendsTokens = new List<Token>();
                         inConstraint = false;
                     }
                     index++;
                     continue;
                 }
                 if (t.type == ETokenType.Colon)
                 {
                     inConstraint = true;
                     index++;
                     continue;
                 }

                if (!inConstraint)
                    currentNameTokens.Add(t);
                else
                    currentExtendsTokens.Add(t);

                index++;
            }
            return index;
        }

        private void CreateTemplateDefine(List<Token> nameTokens, List<Token> extendsTokens)
        {
            if (nameTokens.Count == 0)
                return;

            // nameTokens: 模板参数名及其前缀（例如 T），
            // extendsTokens: 约束类型（例如 Collections.List<Map<int,string>>）。
            // 统一组装为: [ nameTokens ] [ In ] [ extendsTokens ] 传给基于 Token 的构造函数。

            List<Token> allTokens = new List<Token>();
            allTokens.AddRange(nameTokens);

            if (extendsTokens != null && extendsTokens.Count > 0)
            {
                // 在 name 和约束类型之间插入一个虚拟的 in 关键字，以复用 FileMetaTemplateDefine(List<Token>) 的解析逻辑。
                Token first = nameTokens[0];
                Token inToken = new Token(first.path, ETokenType.In, "in", first.sourceBeginLine, first.sourceBeginChar);
                allTokens.Add(inToken);
                allTokens.AddRange(extendsTokens);
            }

            var fmtd = new FileMetaTemplateDefine(m_FileMeta, allTokens);
            m_TemplateDefineList.Add(fmtd);
        }

        // 解析 extends 或 interface 列表
        private int ParseExtendsOrInterface(List<Token> tokens, int startIndex, bool isInterface)
        {
            List<List<Token>> typeTokenGroups = new List<List<Token>>();
            List<Token> current = new List<Token>();
            int depth = 0;

            int index = startIndex;
            while (index < tokens.Count)
            {
                var t = tokens[index];
                if (t.type == ETokenType.Comma && depth == 0)
                {
                    if (current.Count > 0)
                    {
                        typeTokenGroups.Add(current);
                        current = new List<Token>();
                    }
                    index++;
                    continue;
                }
                if (t.type == ETokenType.Less)
                    depth++;
                else if (t.type == ETokenType.Greater && depth > 0)
                    depth--;

                if (t.type == ETokenType.LeftBrace || t.type == ETokenType.LineEnd)
                    break;

                current.Add(t);
                index++;
            }
            if (current.Count > 0)
                typeTokenGroups.Add(current);

            foreach (var group in typeTokenGroups)
            {
                var fmcd = CreateClassDefineFromTokens(group);
                if (fmcd == null)
                    continue;

                if (!isInterface)
                {
                    if (m_FileMetaExtendClass != null)
                    {
                        Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 已有继承类,请勿多重继承!!");
                    }
                    else
                    {
                        m_FileMetaExtendClass = fmcd;
                    }
                }
                else
                {
                    m_InterfaceClassList.Add(fmcd);
                }
            }

            return index;
        }

        private FileMetaClassDefine CreateClassDefineFromTokens(List<Token> tokens)
        {
            // 直接使用基于 Token 的 FileMetaClassDefine 构造函数，
            // 由 FileMetaClassDefine 自己在内部解析泛型参数、数组维度等信息。
            return new FileMetaClassDefine(m_FileMeta, tokens);
         }
        public void AddFileMemberData(FileMetaMemberData fmmd)
        {
            m_MemberDataList.Add(fmmd);
            fmmd.SetFileMeta(m_FileMeta);
        }
        public FileMetaMemberData GetFileMemberData(string name)
        {
            return m_MemberDataList.Find(a => a.name == name);
        }
        public void AddFileMemberVariable(FileMetaMemberVariable fmv )
        {
            m_MemberVariableList.Add(fmv);
            fmv.SetFileMeta(m_FileMeta);
        }
        public void AddFileMemberFunction( FileMetaMemberFunction fmmf )
        {
            m_MemberFunctionList.Add(fmmf);
            fmmf.SetFileMeta(m_FileMeta);
        }

        /// <summary>
        /// 返回最近添加的成员函数，用于在 TokenToFileMeta 中把函数体 token 绑定到对应的 FileMetaMemberFunction 上。
        /// </summary>
        public FileMetaMemberFunction GetLastMemberFunction()
        {
            if (m_MemberFunctionList == null || m_MemberFunctionList.Count == 0)
                return null;
            return m_MemberFunctionList[m_MemberFunctionList.Count - 1];
        }
        public void SetMetaNamespace( FileMetaNamespace mn )
        {
            m_TopLevelFileMetaNamespace = mn;
        }
        public void AddExtendMetaNamespace( FileMetaNamespace fmn )
        {
            if( m_TopLevelFileMetaNamespace != null )
            {

            }
            else
            {
                var list = fmn.namespaceStatementBlock.namespaceList;
                if ( list?.Count < 1 )
                {
                    return;
                }
                if(this.m_NamespaceBlock != null )
                {
                    string lastName = this.m_NamespaceBlock.namespaceList[this.m_NamespaceBlock.namespaceList.Count - 1];

                    if(list[list.Count-1] == lastName )
                    { 
                        // 使用 NamespaceStatementBlock 重新构造顶层命名空间，而不依赖 Node
                        var nsTokens = new List<Token>();
                        foreach (var nsName in fmn.namespaceStatementBlock.namespaceList)
                        {
                            nsTokens.Add(new Token(m_FileMeta.path, ETokenType.Identifier, nsName, 0, 0));
                        }
                        var nsBlock = NamespaceStatementBlock.CreateStateBlock(nsTokens);
                        if (nsBlock != null)
                        {
                            m_TopLevelFileMetaNamespace = new FileMetaNamespace(nsBlock);
                        }
                    }
                }
            }
        }
        public void SetPartialToken( Token partialToken )
        {
            m_PartialToken = partialToken;
        }
        public void SetPermissionToken(Token permissionToken)
        {
            m_PermissionToken = permissionToken;
        }
        //public void SetParentClassNameToken(List<Token> tokenList, Node angleNode)
        //{
        //    if( tokenList != null && tokenList.Count > 0 )
        //    {
        //        FileMetaClassDefine fmcd = new FileMetaClassDefine(m_FileMeta, tokenList, angleNode);
        //        SetExtendClass(fmcd);
        //    }
        //}
        public void SetMetaClass( MetaClass mc )
        {
            m_MetaClass = mc;
        }
        //public MetaBase GetChildrenMetaBaseByName( string name )
        //{
        //    return  m_MetaClass.GetChildrenMetaBaseByName(name);
        //}
        public void AddFileMetaClass( FileMetaClass fmc )
        {
            fmc.m_Deep = this.deep + 1;
            fmc.SetFileMetaClass(this);
            m_ChildrenClassList.Add(fmc);
        }
        public void AddInterfaceClass(FileMetaClassDefine fmcv )
        {
            m_InterfaceClassList.Add(fmcv);
        }
        private void SetFileMetaClass( FileMetaClass fmc )
        {
            m_TopLevelFileMetaClass = fmc;
            innerClass = true;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            foreach (var v in m_ChildrenClassList)
            {
                v.SetDeep(m_Deep + 1);
            }
            foreach (var v in m_MemberVariableList)
            {
                v.SetDeep(m_Deep + 1);
            }            
            foreach (var v in m_MemberFunctionList)
            {
                v.SetDeep(m_Deep + 1);
            }
        }
        public override string ToString()
        {
            return this.name;
        }
        public override string ToFormatString()
        {
            stringBuilder.Clear();
            for (int i = 0; i < deep; i++)
                stringBuilder.Append(Global.tabChar);

            if (m_DataToken != null)
            {
                if (m_ConstToken != null)
                {
                    stringBuilder.Append(m_ConstToken.lexeme.ToString() + " ");
                }
                stringBuilder.Append(m_DataToken.lexeme.ToString() + " ");
                stringBuilder.Append(name);

                stringBuilder.Append(Environment.NewLine);
                for (int i = 0; i < deep; i++)
                    stringBuilder.Append(Global.tabChar);
                stringBuilder.Append("{" + Environment.NewLine);


                foreach (var v in m_MemberDataList)
                {
                    stringBuilder.Append(v.ToFormatString() + Environment.NewLine);
                }

                for (int i = 0; i < deep; i++)
                    stringBuilder.Append(Global.tabChar);
                stringBuilder.Append("}");
            }
            else
            if ( m_EnumToken != null )
            {
                if( m_ConstToken != null )
                {
                    stringBuilder.Append(m_ConstToken.lexeme.ToString() + " ");
                }
                stringBuilder.Append(m_EnumToken.lexeme.ToString() + " ");
                stringBuilder.Append(name);

                stringBuilder.Append(Environment.NewLine);
                for (int i = 0; i < deep; i++)
                    stringBuilder.Append(Global.tabChar);
                stringBuilder.Append("{" + Environment.NewLine);


                foreach (var v in m_MemberVariableList)
                {
                    stringBuilder.Append(v.ToFormatString() + Environment.NewLine);
                }
                if (m_MemberVariableList.Count > 0)
                    stringBuilder.Append(Environment.NewLine);

                for (int i = 0; i < deep; i++)
                    stringBuilder.Append(Global.tabChar);
                stringBuilder.Append("}");
            }
            else
            {

                stringBuilder.Append(m_PermissionToken != null ? m_PermissionToken.lexeme.ToString() : "_public");
                stringBuilder.Append(" ");
                if (m_PartialToken != null)
                    stringBuilder.Append(m_PartialToken.lexeme.ToString() + " ");
                else
                    stringBuilder.Append("_partial ");
                if (m_ClassToken != null)
                {
                    stringBuilder.Append(m_ClassToken.lexeme.ToString());
                    stringBuilder.Append(" ");
                }
                else
                {
                    stringBuilder.Append("_class" + " ");
                }

                if (m_NamespaceBlock != null)
                {
                    stringBuilder.Append(m_NamespaceBlock.ToFormatString());
                    stringBuilder.Append(".");
                }
                stringBuilder.Append(name);

                if (m_TemplateDefineList.Count > 0)
                {
                    stringBuilder.Append("<");
                    for (int i = 0; i < m_TemplateDefineList.Count; i++)
                    {
                        stringBuilder.Append(m_TemplateDefineList[i].ToFormatString());
                        if (i < m_TemplateDefineList.Count - 1)
                        {
                            stringBuilder.Append(",");
                        }
                    }
                    stringBuilder.Append(">");
                }

                if ( m_FileMetaExtendClass != null)
                {
                    stringBuilder.Append(" extends " + m_FileMetaExtendClass.ToFormatString());
                }
                if (interfaceClassList.Count > 0)
                {
                    stringBuilder.Append("  interface");
                }
                for (int i = 0; i < interfaceClassList.Count; i++)
                {
                    stringBuilder.Append(" " + interfaceClassList[i].ToFormatString());
                    if (i < interfaceClassList.Count - 1)
                        stringBuilder.Append(",");
                }
                stringBuilder.Append(Environment.NewLine);
                for (int i = 0; i < deep; i++)
                    stringBuilder.Append(Global.tabChar);
                stringBuilder.Append("{" + Environment.NewLine);
                foreach (var v in m_ChildrenClassList)
                {
                    stringBuilder.Append(v.ToFormatString() + Environment.NewLine);
                }
                if (m_ChildrenClassList.Count > 0)
                    stringBuilder.Append(Environment.NewLine);

                foreach (var v in m_MemberVariableList)
                {
                    stringBuilder.Append(v.ToFormatString() + Environment.NewLine);
                }
                if (m_MemberVariableList.Count > 0)
                    stringBuilder.Append(Environment.NewLine);

                foreach (var v in m_MemberFunctionList)
                {
                    stringBuilder.Append(v.ToFormatString() + Environment.NewLine);
                }
                for (int i = 0; i < deep; i++)
                    stringBuilder.Append(Global.tabChar);
                stringBuilder.Append("}");
            }

            return stringBuilder.ToString();
        }
    }
}
