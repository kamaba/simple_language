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
using System.Text;

namespace SimpleLanguage.Compile.CoreFileMeta
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
                Log.AddInStructFileMeta(EError.None, "Error 错误 !!!");
                return false;
            }

            Token permissionToken = null;
            Token m_ExtendsToken = null;
            Token commaToken = null;
#pragma warning disable CS0219 // 变量已被赋值，但从未使用过它的值
            bool isError = false;
#pragma warning restore CS0219 // 变量已被赋值，但从未使用过它的值
            List<Token> classNameTokenList = new List<Token>();
            List<Token> list = new List<Token>();

            Node angleNode = null;
            Node lastNode = null;
            int addCount = 0;
            while (addCount < m_NodeList.Count)
            {
                var cnode = m_NodeList[addCount++];

                if (cnode.nodeType == ENodeType.IdentifierLink)
                {
                    if ( m_SufInterfaceToken != null || m_ExtendsToken != null )
                    {
                        List<FileMetaClassDefine> fcdList = new List<FileMetaClassDefine>();
                        addCount = ReadClassDefineStruct(addCount -1, m_NodeList, fcdList);
                        if (m_ExtendsToken != null && m_SufInterfaceToken == null)
                        {
                            if(m_FileMetaExtendClass != null )
                            {
                                Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 已有继承类,请勿多重继承!");

                            }
                            if (fcdList.Count == 0 )
                            {
                                Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 继承关键字后边没有相应的内容!");
                            }
                            if (fcdList.Count > 1)
                            {
                                Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 继承只能单继承，不能多继承!!");
                            }
                            m_FileMetaExtendClass = fcdList[0];
                        }
                        else if (m_SufInterfaceToken != null )
                        {
                            if (fcdList.Count == 0)
                            {
                                Log.AddInStructFileMeta(EError.StructFileMetaStart, "接口关键字后边没有相应的内容!");
                            }
                            m_InterfaceClassList.AddRange(fcdList);
                        }
                    }
                    else
                    {
                        if (classNameTokenList.Count > 0)
                        {
                            Log.AddInStructFileMeta(EError.StructClassNameRepeat, "Error 字符两次赋值 107");
                            for (int i = 0; i < classNameTokenList.Count; i++)
                            {
                                Log.AddInStructFileMeta(EError.None, classNameTokenList[i].lexeme.ToString());
                            }
                        }
                        classNameTokenList = cnode.linkTokenList;

                        Node nnode = null;
                        if (addCount < m_NodeList.Count)
                        {
                            nnode = m_NodeList[addCount];
                        }

                        if( nnode?.nodeType == ENodeType.LeftAngle )
                        {
                            int cAddCount = addCount + 1;
                            bool templateInExtends = false;
                            Node templateNode = null;
                            Node templateExtendsNode = null;
                            while (cAddCount < m_NodeList.Count)
                            {
                                var cnode2 = m_NodeList[cAddCount++];
                                if (cnode2.nodeType == ENodeType.RightAngle)
                                {
                                    if(templateNode == null )
                                    {
                                        var ld = Log.AddInStructFileMeta(EError.None, "没有找到模板定义T");
                                        ld.filePath = cnode2.token.path;
                                        ld.sourceBeginLine = cnode2.token.sourceBeginLine;
                                        break;
                                    }
                                    FileMetaTemplateDefine fmtd = new FileMetaTemplateDefine(m_FileMeta, templateNode, templateExtendsNode);
                                    m_TemplateDefineList.Add(fmtd);
                                    break;
                                }
                                else if (cnode2.nodeType == ENodeType.Comma)
                                {
                                    FileMetaTemplateDefine fmtd = new FileMetaTemplateDefine(m_FileMeta, templateNode, templateExtendsNode);
                                    m_TemplateDefineList.Add(fmtd);
                                    templateNode = null;
                                    continue;
                                }
                                else if (cnode2.nodeType == ENodeType.Key && cnode2.token?.type == ETokenType.Colon )
                                {
                                    templateInExtends = true;
                                    continue;
                                }
                                else if (cnode2.nodeType == ENodeType.IdentifierLink)
                                {
                                    if (templateInExtends)
                                    {
                                        templateExtendsNode = cnode2;
                                    }
                                    else
                                    {
                                        templateNode = cnode2;
                                    }
                                }
                                else
                                {
                                    Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 不支持其它格式 在类后续的模板限定中!");
                                }
                            }
                            addCount = cAddCount;
                        }
                    }
                }
                else
                {
                    var token = cnode.token;
                    if (token.type == ETokenType.Public
                        || token.type == ETokenType.Private
                        || token.type == ETokenType.Projected
                        || token.type == ETokenType.Extern )
                    {
                        if (permissionToken == null)
                        {
                            permissionToken = token;
                        }
                        else
                        {
                            isError = true;
                            Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次权限!!");
                        }
                    }
                    else if (token.type == ETokenType.Const)
                    {
                        if (m_ConstToken == null)
                        {
                            m_ConstToken = token;
                        }
                        else
                        {
                            isError = true;
                            Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次Const!!");
                        }
                    }
                    else if (token.type == ETokenType.Partial)
                    {
                        if (m_PartialToken != null)
                        {
                            isError = true;
                            Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次Class!!");
                        }
                        m_PartialToken = token;
                    }
                    else if (token.type == ETokenType.Class)
                    {
                        if (m_EnumToken != null)
                        {
                            isError = true;
                            Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次Enum!!");
                        }
                        if (m_DataToken != null)
                        {
                            isError = true;
                            Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次data!!");
                        }
                        if (m_ClassToken != null)
                        {
                            isError = true;
                            Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次Class!!");
                        }
                        m_ClassToken = token;
                    }
                    else if (token.type == ETokenType.Enum)
                    {
                        if (m_EnumToken != null)
                        {
                            isError = true;
                            Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次Enum!!");
                        }
                        if (m_DataToken != null)
                        {
                            isError = true;
                            Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次data!!");
                        }
                        if (m_ClassToken != null)
                        {
                            isError = true;
                            Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次Class!!");
                        }
                        m_EnumToken = token;
                    }
                    else if (token.type == ETokenType.Data)
                    {
                        if (m_EnumToken != null)
                        {
                            isError = true;
                            Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次Enum!!");
                        }
                        else
                        {
                            if (m_DataToken != null)
                            {
                                isError = true;
                                Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次data!!");
                            }
                            if (m_ClassToken != null)
                            {
                                isError = true;
                                Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次Class!!");
                            }
                            m_DataToken = token;
                        }
                    }
                    else if (token.type == ETokenType.Extends)
                    {
                        if (m_ExtendsToken != null)
                        {
                            isError = true;
                            Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次Extend!!");
                        }
                        m_ExtendsToken = token;
                    }
                    else if (token.type == ETokenType.Interface)
                    {
                        if (m_EnumToken != null)
                        {
                            isError = true;
                            Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次Enum!!");
                        }
                        if (m_DataToken != null)
                        {
                            isError = true;
                            Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析过了一次data!!");
                        }
                        if (classNameTokenList.Count > 0 )    //后置
                        {
                            if (m_PreInterfaceToken != null)
                            {
                                isError = true;
                                Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析类时，已发现用过interface标记，不可重复使用该标记");
                            }
                            if (m_SufInterfaceToken != null)
                            {
                                isError = true;
                                Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析类时，已发现用过interface标记，不可重复使用该标记");
                            }
                            m_SufInterfaceToken = token;

                        }
                        else
                        {
                            if (m_ClassToken != null)
                            {
                                isError = true;
                                Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析interface与class不可以周时出现!!");
                            }
                            if (m_PreInterfaceToken != null)
                            {
                                isError = true;
                                Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析类时，已发现用过interface标记，不可重复使用该标记");
                            }
                            if (m_SufInterfaceToken != null)
                            {
                                isError = true;
                                Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析类时，已发现用过interface标记，不可重复使用该标记");
                            }
                            m_PreInterfaceToken = token;
                        }
                    }
                    else if (token.type == ETokenType.Comma)
                    {
                        commaToken = token;
                    }
                    else
                    {
                        isError = true; 
                        Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 有其它未知类型在class中");
                        break;
                    }
                }
                lastNode = cnode;
            }

            if(m_EnumToken != null )
            {
                if (m_PreInterfaceToken != null)
                {
                    Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error Enum方式，与enum同级，不允许同时出现");
                    return false;
                }
                if (m_SufInterfaceToken != null)
                {
                    Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error Enum方式，不支持接口方式");
                    Log.AddInStructFileMeta(EError.None, "");
                    return false;
                }
                if (permissionToken != null)
                {
                    Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error Enum方式，不支持权限的使用!!");
                    return false;
                }
                if (m_PartialToken != null)
                {
                    Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error Enum方式，不支持partial的使用!!");
                    return false;
                }

            }
            else if (m_DataToken != null)
            {
                if (m_PreInterfaceToken != null)
                {
                    Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error Enum方式，与data同级，不允许同时出现");
                    return false;
                }
                if (m_SufInterfaceToken != null)
                {
                    Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error Enum方式，不支持接口方式");
                    return false;
                }
                if (permissionToken != null)
                {
                    Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error Data方式，不支持权限的使用!!");
                    return false;
                }
                if (m_PartialToken != null)
                {
                    Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error Data方式，不支持partial的使用!!");
                    return false;
                }

            }
            else
            {

                if (classNameTokenList.Count == 0)
                {
                    Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析类型名称错误!!");
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
        public class ParseStructTemp
        {
            public ParseStructTemp parentPSt { get; set; } = null;
            public Node nameNode { get; set; } = null;
            public Node angleNode { get; set; } = null;

            public List<ParseStructTemp> angleContentNodeList = new List<ParseStructTemp>();

            public void AddParseStructTemplate( ParseStructTemp pst )
            {
                angleContentNodeList.Add(pst);
                pst.parentPSt = this;
            }

            public void GenFileInputTemplateNode( Node node, FileMeta fm )
            {
                node.angleNode = angleNode;
                for ( int i = 0; i < angleContentNodeList.Count; i++ )
                {
                    node.angleNode.AddChild( angleContentNodeList[i].nameNode);
                    if(angleContentNodeList[i].angleContentNodeList.Count > 0 )
                    {
                        angleContentNodeList[i].GenFileInputTemplateNode(angleContentNodeList[i].nameNode, fm );
                    }
                }
            }
        }
        public int ReadClassDefineStruct( int cAddCount, List<Node> m_NodeList, List<FileMetaClassDefine> fcdList )
        {
            List<ParseStructTemp> rootPST = new List<ParseStructTemp>();
            ParseStructTemp curPST = null;
            while (cAddCount < m_NodeList.Count)
            {
                var cnode2 = m_NodeList[cAddCount];
                
                if (cnode2.nodeType == ENodeType.RightAngle)
                {
                    if (curPST == null)
                    {
                        cAddCount++;
                        break;
                    }
                    if (curPST.angleNode != null)
                    {
                        curPST.angleNode.endToken = cnode2.token;
                        if (curPST.parentPSt == null)
                        {
                            cAddCount++;
                            break;
                        }
                        else
                        {
                            curPST = curPST.parentPSt;
                        }
                    }
                    else
                    {
                        if (curPST.parentPSt == null)
                        {
                            cAddCount++;
                            break;
                        }
                        else
                        {
                            curPST = curPST.parentPSt;
                        }
                    }
                }
                else if (cnode2.nodeType == ENodeType.Comma)
                {
                    cAddCount++;
                    continue;
                }
                else if (cnode2.nodeType == ENodeType.IdentifierLink
                    || (cnode2.nodeType == ENodeType.Key && cnode2.token.type == ETokenType.Object ))
                {
                    ParseStructTemp newpst = null;
                    if (curPST == null)
                    {
                        newpst = new ParseStructTemp();
                        newpst.nameNode = cnode2;
                        rootPST.Add(newpst);
                    }
                    else
                    {
                        newpst = new ParseStructTemp();
                        newpst.nameNode = cnode2;
                        curPST.AddParseStructTemplate(newpst);
                    }
                    
                    if( cAddCount + 1 < m_NodeList.Count )
                    {
                        var nextNode = m_NodeList[cAddCount + 1];
                        if (nextNode.nodeType == ENodeType.LeftAngle)
                        {
                            cAddCount++;
                            newpst.angleNode = nextNode;
                            curPST = newpst;
                        }
                    }                    
                }
                else if( cnode2.nodeType == ENodeType.Key && cnode2.token.type == ETokenType.Interface )
                {
                    cAddCount++;
                    break;
                }
                else
                {
                    Log.AddInStructFileMeta(EError.None, "Error 不支持其它格式 在类后续的模板限定中!");
                }
                cAddCount++;
            }
            for( int i = 0; i < rootPST.Count; i++ )
            {
                var pst = rootPST[i];
                Node nameNode = pst.nameNode;
                pst.GenFileInputTemplateNode(nameNode, m_FileMeta);
                FileMetaClassDefine fmcd = new FileMetaClassDefine(m_FileMeta, pst.nameNode, null );
                fcdList.Add(fmcd);
            }

            return cAddCount;
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
                        var namespaceNameNode = new Node(fmn.namespaceNameNode.token);
                        List<Node> extendLinkNodeList = new List<Node>();
                        for ( int i = 0; i < fmn.namespaceNameNode.extendLinkNodeList.Count -2; i++ )
                        {
                            extendLinkNodeList.Add(fmn.namespaceNameNode.extendLinkNodeList[i]);
                        }
                        namespaceNameNode.SetLinkNode(extendLinkNodeList);
                        FileMetaNamespace fmnnew = new FileMetaNamespace(fmn.namespaceNode, namespaceNameNode);
                        m_TopLevelFileMetaNamespace = fmnnew;
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
