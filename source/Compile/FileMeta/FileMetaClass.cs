//****************************************************************************
//  File:      FileMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Compile.Parse;
using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SimpleLanguage.Compile.CoreFileMeta
{
    public partial class FileMetaClass : FileMetaBase
    {
        public bool innerClass { get; set; } = false;
        public bool isConst { get { return m_ConstToken != null; } }
        public bool isEnum { get { return m_EnumToken != null; } }
        //public bool isData { get { return m_DataToken != null; } }
        public bool isPartial => m_PartialToken != null;
        public MetaClass metaClass => m_MetaClass;
        public FileMetaClassDefine extendClass => m_ExtendClass;
        private List<FileMetaClassDefine> interfaceClassList => m_InterfaceClassList;
        public FileMetaNamespace topLevelFileMetaNamespace => m_TopLevelFileMetaNamespace;
        public FileMetaClass topLevelFileMetaClass => m_TopLevelFileMetaClass;
        public List<FileMetaTemplateDefine> templateParamList => m_TemplateParamList;
        public NamespaceStatementBlock namespaceBlock => m_NamespaceBlock;
        public List<FileMetaMemberVariable> memberVariableList => m_MemberVariableList;
        public List<FileMetaMemberFunction> memberFunctionList => m_MemberFunctionList;

        #region Token
        protected Token m_PermissionToken = null;
        protected Token m_PartialToken = null;
        protected Token m_ClassToken = null;
        protected Token m_EnumToken = null;
        //protected Token m_DataToken = null;
        protected Token m_ConstToken = null;
        #endregion
        private MetaClass m_MetaClass = null;
        private FileMetaNamespace m_TopLevelFileMetaNamespace = null;
        private FileMetaClass m_TopLevelFileMetaClass = null;
        private FileMetaClassDefine m_ExtendClass = null;
        private List<FileMetaClassDefine> m_InterfaceClassList = new List<FileMetaClassDefine>();
        private List<FileMetaClass> m_ChildrenClassList = new List<FileMetaClass>();
        private List<FileMetaTemplateDefine> m_TemplateParamList = new List<FileMetaTemplateDefine>();


        private List<FileMetaMemberVariable> m_MemberVariableList = new List<FileMetaMemberVariable>();
        private List<FileMetaMemberFunction> m_MemberFunctionList = new List<FileMetaMemberFunction>();

        private NamespaceStatementBlock m_NamespaceBlock = null;
        private List<Node> m_NodeList = new List<Node>();

        private StringBuilder stringBuilder = new StringBuilder();
        public FileMetaClass( FileMeta fm, List<Node> listNode)
        {
            m_FileMeta = fm;
            m_NodeList = listNode;
            m_Id = ++s_IdCount;
            Parse();
        }
        private bool Parse()
        {
            if (m_NodeList.Count == 0)
            {
                Console.WriteLine("Error 错误 !!!");
                return false;
            }

            Token permissionToken = null;
            Token extendToken = null;
            Token interfaceToken = null;
            Token commaToken = null;
            bool isError = false;
            List<Token> classNameTokenList = new List<Token>();
            List<Token> inheritNameTokenList = new List<Token>();
            List<Token> interfaceNameTokenList = new List<Token>();
            List<List<Token>> interfaceTokenList = new List<List<Token>>();
            List<Token> list = new List<Token>();

            Node angleNode = null;
            int addCount = 0;
            while (addCount < m_NodeList.Count)
            {
                var cnode = m_NodeList[addCount++];

                if (cnode.nodeType == ENodeType.IdentifierLink)
                {
                    if (interfaceToken != null)
                    {
                        if (interfaceNameTokenList.Count > 0)
                        {
                            Console.WriteLine("Error 字符两次赋值 90 ");
                        }
                        interfaceNameTokenList = cnode.linkTokenList;
                    }
                    else if (extendToken != null && interfaceToken == null)
                    {
                        if (inheritNameTokenList.Count > 0)
                        {
                            Console.WriteLine("Error 字符两次赋值 99");
                        }
                        inheritNameTokenList = cnode.linkTokenList;
                    }
                    else
                    {
                        if (classNameTokenList.Count > 0)
                        {
                            Console.WriteLine("Error 字符两次赋值 107");
                            for( int i = 0; i < classNameTokenList.Count; i++ )
                            {
                                Console.WriteLine(classNameTokenList[i].lexeme.ToString());
                            }
                        }
                        classNameTokenList = cnode.linkTokenList;
                    }
                    if (cnode.angleNode != null)
                    {
                        if (inheritNameTokenList.Count == 0)
                        {
                            List<List<Node>> angleNodeListList = new List<List<Node>>();
                            List<Node> angleNodeList = new List<Node>();
                            for (int i = 0; i < cnode.angleNode.childList.Count; i++)
                            {
                                var caNode = cnode.angleNode.childList[i];
                                if (caNode.nodeType == ENodeType.Comma)
                                {
                                    if (angleNodeListList.Count < 0)
                                    {
                                        Console.WriteLine("Error 解析<,> 不允许第一位出现逗号!!");
                                    }
                                    else
                                    {
                                        angleNodeListList.Add(angleNodeList);
                                    }
                                    continue;
                                }
                                angleNodeList.Add(caNode);
                            }
                            if (angleNodeListList.Count < 0)
                            {
                                Console.WriteLine("Error 解析<,> 不允许第一位出现逗号!!");
                            }
                            else
                            {
                                angleNodeListList.Add(angleNodeList);
                            }
                            for (int i = 0; i < angleNodeListList.Count; i++)
                            {
                                var anll = angleNodeList[i];
                                FileMetaTemplateDefine fmtd = new FileMetaTemplateDefine(m_FileMeta, anll);
                                m_TemplateParamList.Add(fmtd);
                            }
                        }
                        else
                        {
                            angleNode = cnode.angleNode;
                        }
                    }
                }
                else
                {
                    var token = cnode.token;
                    if (token.type == ETokenType.Public
                        || token.type == ETokenType.Private
                        || token.type == ETokenType.Projected
                        || token.type == ETokenType.Internal)
                    {
                        if (permissionToken == null)
                        {
                            permissionToken = token;
                        }
                        else
                        {
                            isError = true;
                            Console.WriteLine("Error 解析过了一次权限!!");
                        }
                    }
                    else if (token.type == ETokenType.Const)
                    {
                        if(m_ConstToken == null )
                        {
                            m_ConstToken = token;
                        }
                        else
                        {
                            isError = true;
                            Console.WriteLine("Error 解析过了一次Const!!");
                        }
                    }
                    else if( token.type == ETokenType.Partial )
                    {
                        if(m_PartialToken != null )
                        {
                            isError = true;
                            Console.WriteLine("Error 解析过了一次Class!!");
                        }
                        m_PartialToken = token;
                    }
                    else if (token.type == ETokenType.Class)
                    {
                        if (m_EnumToken != null)
                        {
                            isError = true;
                            Console.WriteLine("Error 解析过了一次Enum!!");
                        }
                        //if( m_DataToken != null )
                        //{
                        //    isError = true;
                        //    Console.WriteLine("Error 解析过了一次data!!");
                        //}
                        if (m_ClassToken != null)
                        {
                            isError = true;
                            Console.WriteLine("Error 解析过了一次Class!!");
                        }
                        m_ClassToken = token;
                    }
                    
                    else if (token.type == ETokenType.Enum)
                    {
                        if (m_EnumToken != null)
                        {
                            isError = true;
                            Console.WriteLine("Error 解析过了一次Enum!!");
                        }
                        //if (m_DataToken != null)
                        //{
                        //    isError = true;
                        //    Console.WriteLine("Error 解析过了一次data!!");
                        //}
                        if (m_ClassToken != null)
                        {
                            isError = true;
                            Console.WriteLine("Error 解析过了一次Class!!");
                        }
                        m_EnumToken = token;
                    }
                    //else if (token.type == ETokenType.Data)
                    //{
                    //    if(m_EnumToken != null)
                    //    {
                    //        isError = true;
                    //        Console.WriteLine("Error 解析过了一次Enum!!");
                    //    }
                    //    //if (m_DataToken != null)
                    //    //{
                    //    //    isError = true;
                    //    //    Console.WriteLine("Error 解析过了一次data!!");
                    //    //}
                    //    if (m_ClassToken != null)
                    //    {
                    //        isError = true;
                    //        Console.WriteLine("Error 解析过了一次Class!!");
                    //    }
                    //    //m_DataToken = token;
                    //}
                    else if (token.type == ETokenType.Extend )
                    {
                        if (extendToken != null)
                        {
                            isError = true;
                            Console.WriteLine("Error 解析过了一次Extend!!");
                        }
                        extendToken = token;
                    }
                    else if (token.type == ETokenType.Interface)
                    {
                        if (interfaceToken != null)
                        {
                            isError = true;
                            Console.WriteLine("Error 解析类时，已发现用过interface标记，不可重复使用该标记");
                        }
                        interfaceToken = token;
                    }
                    else if (token.type == ETokenType.Comma)
                    {
                        commaToken = token;
                        interfaceTokenList.Add(interfaceNameTokenList);
                        interfaceNameTokenList = new List<Token>();
                    }
                    else
                    {
                        isError = true;
                        Console.WriteLine("Error 有其它未知类型在class中");
                        break;
                    }
                }
            }

            if(m_EnumToken != null )
            {
                if(interfaceToken != null || interfaceNameTokenList.Count > 0 )
                {
                    Console.WriteLine("Error Enum方式，不支持接口方式");
                    return false;
                }
                if (permissionToken != null)
                {
                    Console.WriteLine("Error Enum方式，不支持权限的使用!!");
                    return false;
                }
                if (m_PartialToken != null)
                {
                    Console.WriteLine("Error Enum方式，不支持partial的使用!!");
                    return false;
                }

            }
            //else if (m_DataToken != null)
            //{
            //    if (interfaceToken != null || interfaceNameTokenList.Count > 0)
            //    {
            //        Console.WriteLine("Error Data方式，不支持接口方式");
            //        return false;
            //    }
            //    if (permissionToken != null)
            //    {
            //        Console.WriteLine("Error Data方式，不支持权限的使用!!");
            //        return false;
            //    }
            //    if (m_PartialToken != null)
            //    {
            //        Console.WriteLine("Error Data方式，不支持partial的使用!!");
            //        return false;
            //    }

            //}
            else
            {
                if (interfaceNameTokenList.Count > 0)
                {
                    interfaceTokenList.Add(interfaceNameTokenList);
                }

                if (classNameTokenList.Count == 0)
                {
                    Console.WriteLine("Error 解析类型名称错误!!");
                }
                SetParentClassNameToken(inheritNameTokenList, angleNode);
                for (int i = 0; i < interfaceTokenList.Count; i++)
                {
                    var incn = interfaceTokenList[i];
                    AddInterfaceClassNameToken(incn);
                }
            }
            m_Token = classNameTokenList[classNameTokenList.Count - 1];
            if (classNameTokenList.Count > 1)
            {
                m_NamespaceBlock = NamespaceStatementBlock.CreateStateBlock(classNameTokenList.GetRange(0, classNameTokenList.Count - 2));
            }
            SetPermissionToken(permissionToken);

            return true;
        }
        public MetaNamespace GetLasatMetaNamespace()
        {
            if ( topLevelFileMetaNamespace != null
                && topLevelFileMetaNamespace.namespaceStatementBlock != null 
                && topLevelFileMetaNamespace.namespaceStatementBlock.lastMetaNamespace != null)
            {
                return topLevelFileMetaNamespace.namespaceStatementBlock.lastMetaNamespace;
            }
            return null;
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
        public void SetMetaNamespace( FileMetaNamespace mn )
        {
            m_TopLevelFileMetaNamespace = mn;
        }
        public void SetPartialToken( Token partialToken )
        {
            m_PartialToken = partialToken;
        }
        public void SetPermissionToken(Token permissionToken)
        {
            m_PermissionToken = permissionToken;
        }
        public void SetParentClassNameToken(List<Token> tokenList, Node angleNode)
        {
            if( tokenList != null && tokenList.Count > 0 )
            {
                FileMetaClassDefine fmcd = new FileMetaClassDefine(m_FileMeta, tokenList, angleNode);
                SetExtendClass(fmcd);
            }
        }
        public void AddInterfaceClassNameToken(List<Token> tokenList)
        {
            AddInterfaceClass(new FileMetaClassDefine(m_FileMeta, tokenList));
        }
        public void SetMetaClass( MetaClass mc )
        {
            m_MetaClass = mc;
        }
        public MetaBase GetChildrenMetaBaseByName( string name )
        {
            return  m_MetaClass.GetChildrenMetaBaseByName(name);
        }
        public void AddFileMetaClass( FileMetaClass fmc )
        {
            fmc.m_Deep = this.deep + 1;
            fmc.SetFileMetaClass(this);
            m_ChildrenClassList.Add(fmc);
        }
        public void SetExtendClass(FileMetaClassDefine fileMetaClassVariable )
        {
            m_ExtendClass = fileMetaClassVariable;
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
        public List<MetaClass> GetInterfaceMetaClass()
        {
            List<MetaClass> metaClassList = new List<MetaClass>();
            if( m_InterfaceClassList != null )
            {
                for( int i = 0; i < m_InterfaceClassList.Count; i++ )
                {
                    MetaClass getmc = ClassManager.instance.GetMetaClassByRef(metaClass, m_InterfaceClassList[i] );
                    if (getmc == null)
                    {
                        m_InterfaceClassList[i].AddError2( 0);
                        break;
                    }
                    metaClassList.Add(getmc);
                }
            }

            return metaClassList;
        }
        public MetaClass GetExtendMetaClass()
        {
            if( m_ExtendClass != null )
            {
                MetaClass getmc = ClassManager.instance.GetMetaClassByRef( metaClass, m_ExtendClass );
                if (getmc == null)
                {
                    //Console.WriteLine(" CheckExtendAndInterface 在判断继承的时候，发没的:" + m_ExtendClass.allName + "  类"
                    //    + "位置行: " + m_ExtendClass.token.sourceBeginLine.ToString() );


                    m_ExtendClass.AddError2(0);
                }
                return getmc;
            }
            return null;
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
        public override string ToFormatString()
        {
            stringBuilder.Clear();
            for (int i = 0; i < deep; i++)
                stringBuilder.Append(Global.tabChar);

            //if( m_DataToken != null )
            //{
            //    if (m_ConstToken != null)
            //    {
            //        stringBuilder.Append(m_ConstToken.lexeme.ToString() + " ");
            //    }
            //    stringBuilder.Append(m_DataToken.lexeme.ToString() + " ");
            //    stringBuilder.Append(name);

            //    stringBuilder.Append(Environment.NewLine);
            //    for (int i = 0; i < deep; i++)
            //        stringBuilder.Append(Global.tabChar);
            //    stringBuilder.Append("{" + Environment.NewLine);


            //    foreach (var v in m_MemberDataList)
            //    {
            //        stringBuilder.Append(v.ToFormatString() + Environment.NewLine);
            //    }

            //    for (int i = 0; i < deep; i++)
            //        stringBuilder.Append(Global.tabChar);
            //    stringBuilder.Append("}");
            //}
            //else 
            if( m_EnumToken != null )
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

                if (m_TemplateParamList.Count > 0)
                {
                    stringBuilder.Append("<");
                    for (int i = 0; i < m_TemplateParamList.Count; i++)
                    {
                        stringBuilder.Append(m_TemplateParamList[i].ToFormatString());
                        if (i < m_TemplateParamList.Count - 1)
                        {
                            stringBuilder.Append(",");
                        }
                    }
                    stringBuilder.Append(">");
                }

                if (extendClass != null)
                {
                    stringBuilder.Append(" :: " + extendClass.ToFormatString());
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
