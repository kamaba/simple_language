//****************************************************************************
//  File:      FileMetaMemberMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile.Parse;

namespace SimpleLanguage.Compile.CoreFileMeta
{
    public partial class FileMetaParamterDefine : FileMetaBase
    {
        public FileMetaClassDefine classDefineRef => m_ClassDefineRef;
        public FileMetaBaseTerm express => m_Express;

        private Token m_AssignToken = null;
        private FileMetaClassDefine m_ClassDefineRef = null;
        private FileMetaBaseTerm m_Express;
        public FileMetaParamterDefine(FileMeta fileMeta, List<Node> list)
        {
            m_FileMeta = fileMeta;

            ParseBuildMetaParamter( list );
        }
        public bool ParseBuildMetaParamter(List<Node> nodeList)
        {
            if (nodeList == null) return false;

            var listDefieNode = new List<Node>();
            var valueNodeList = new List<Node>();
            if (!FileMetatUtil.SplitNodeList(nodeList, listDefieNode, valueNodeList, ref m_AssignToken))
            {
                Console.WriteLine("Error 解析NodeList出现错误~~~");
                return false;
            }
            m_Express = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, valueNodeList, FileMetaTermExpress.EExpressType.ParamVariable);

            Node nameNode = null;
            Node typeNode = null;
            if (!GetNameAndTypeNode(listDefieNode, ref nameNode, ref typeNode))
            {
                Console.WriteLine("Error 没有找到该定义名称 必须使用例: X = 10; 的格式");
                return false;
            }
            if (nameNode == null)
            {
                Console.WriteLine("Error 没有找到该定义名称 必须使用例: X = 10; 的格式");
                return false;
            }
            m_Token = nameNode?.token;

            if( typeNode != null )
                m_ClassDefineRef = new FileMetaClassDefine(m_FileMeta, typeNode.linkTokenList );             

            return true;
        }
        public bool GetNameAndTypeNode(List<Node> defineNodeList, ref Node nameNode, ref Node typeNode)
        {
            if (defineNodeList.Count == 2)
            {
                typeNode = defineNodeList[0];
                nameNode = defineNodeList[1];
            }
            else if (defineNodeList.Count == 1)
            {
                nameNode = defineNodeList[0];
            }
            else
            {
                return false;
            }
            return true;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            if (m_ClassDefineRef != null)
                sb.Append(" " + m_ClassDefineRef.ToFormatString());
            sb.Append(" " + m_Token.lexeme.ToString());
            if (m_AssignToken != null)
            {
                sb.Append(" " + m_AssignToken.lexeme.ToString());
                sb.Append(" " + m_Express.ToFormatString());
            }
            return sb.ToString();
        }
    }
    public partial class FileMetaFunction : FileMetaBase
    {
        public FileMetaClassDefine returnMetaClass => m_ReturnMetaClass;
        public FileMetaBlockSyntax fileMetaBlockSyntax => m_FileMetaBlockSyntax;
        public List<FileMetaParamterDefine> metaParamtersList => m_MetaParamtersList;
        public List<FileMetaTemplateDefine> metaTemplatesList => m_MetaTemplatesList;

        protected List<FileMetaParamterDefine> m_MetaParamtersList = new List<FileMetaParamterDefine>();
        protected List<FileMetaTemplateDefine> m_MetaTemplatesList = new List<FileMetaTemplateDefine>();
        protected FileMetaClassDefine m_ReturnMetaClass = null;
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
    public partial class FileMetaMemberFunction : FileMetaFunction
    {
        public Token interfaceToken => m_InterfaceToken;
        public Token staticToken => m_StaticToken;
        public Token virtualOverrideToken => m_VirtualOverrideToken;
        public Token permissionToken => m_PermissionToken;
        public Token getToken => m_GetToken;
        public Token setToken => m_SetToken;
        public Token finalToken => m_FinalToken;

        private Token m_InterfaceToken = null;
        private Token m_StaticToken = null;
        private Token m_FinalToken = null;
        private Token m_GetToken = null;
        private Token m_SetToken = null;
        private Token m_VirtualOverrideToken = null;
        private Token m_PermissionToken = null;
        private Token m_LeftBraceToken = null;
        private Token m_RightBraceToken = null;
        private Node m_BlockNode;
        public FileMetaMemberFunction( FileMeta fm, Node block, List<Node> nodeList)
        {
            m_FileMeta = fm;
            m_BlockNode = block;
            ParseFunction(nodeList);
        }
        public bool ParseFunction(List<Node> nodeList)
        {
            Token permissionToken = null;
            Token virtualToken = null;
            int addCount = 0;
            bool isError = false;
            Node returnClassNameNode = null;
            Token interfaceToken = null;
            Token staticToken = null;
            Token getToken = null;
            Token setToken = null;
            Token finalToken = null;
            List<Token> inheritNameTokenList = new List<Token>();
            List<Token> interfaceNameTokenList = new List<Token>();
            List<List<Token>> interfaceTokenList = new List<List<Token>>();
            List<Token> list = new List<Token>();
            Node finalNode = null;
            while (addCount < nodeList.Count)
            {
                var cnode = nodeList[addCount++];

                if (cnode.nodeType == ENodeType.IdentifierLink)
                {
                    if (addCount == nodeList.Count)
                    {
                        finalNode = cnode;
                    }
                    else
                    {
                        returnClassNameNode = cnode;
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
                    else if (token.type == ETokenType.Override)
                    {
                        if (virtualToken != null)
                        {
                            isError = true;
                            Console.WriteLine("Error 解析过了一次Override!!");
                        }
                        virtualToken = token;
                    }
                    else if (token.type == ETokenType.Static)
                    {
                        staticToken = token;
                    }
                    else if (token.type == ETokenType.Get)
                    {
                        if (getToken != null)
                        {
                            isError = true;
                            Console.WriteLine(" Error 解析类型多个get");
                        }
                        getToken = token;
                    }
                    else if (token.type == ETokenType.Set)
                    {
                        if (setToken != null)
                        {
                            isError = true;
                            Console.WriteLine(" Error 解析类型多个set");
                        }
                        setToken = token;
                    }
                    else if (token.type == ETokenType.Type || token.type == ETokenType.Void)
                    {
                        returnClassNameNode = cnode;
                    }
                    else if (token.type == ETokenType.Interface)
                    {
                        if (interfaceToken != null)
                        {
                            isError = true;
                            Console.WriteLine("Error 解析interface已使用过一次，不允许重复使用!!");
                        }
                        interfaceToken = token;
                    }
                    else if( token.type == ETokenType.Final )
                    {
                        if( finalNode != null )
                        {
                            isError = true;
                            Console.WriteLine(" Error 解析类型多个final");
                        }
                        finalToken = token;
                    }
                    else
                    {
                        isError = true;
                        Console.WriteLine("Error 有其它未知类型在class中");
                        break;
                    }
                }
            }
            if(finalNode == null )
            {
                Console.WriteLine("Eror 没有找到合适的函数类型: 位置: " + nodeList[0].token?.ToLexemeAllString());
                return false;
            }

            ParseParam(finalNode.parNode);
            ParseTemplate(finalNode.angleNode);

            m_Token = finalNode.token;
            if ( m_BlockNode != null)
            {
                m_LeftBraceToken = m_BlockNode.token;
                m_RightBraceToken = m_BlockNode.endToken;
                m_FileMetaBlockSyntax = new FileMetaBlockSyntax(m_FileMeta, m_LeftBraceToken, m_RightBraceToken);
            }
            else
            {
                Console.WriteLine("Error 解析位置 Token: " + m_Token?.ToLexemeAllString() );
                return false;
            }
            if( isError )
            {
                Console.WriteLine("ParseFunction 解析函数");
            }
            m_VirtualOverrideToken = virtualToken;            
            m_PermissionToken = permissionToken;
            m_StaticToken = staticToken;
            m_GetToken = getToken;
            m_SetToken = setToken;
            m_FinalToken = finalToken;
            if (returnClassNameNode != null )
            {
                m_ReturnMetaClass = new FileMetaClassDefine(m_FileMeta, returnClassNameNode );
            }
            return true;
        }
        public void ParseParam(Node parNode)
        {
            if (parNode == null) return;

            List<List<Node>> tparamList = new List<List<Node>>();
            List<Node> tempList = new List<Node>();

            for (int i = 0; i < parNode.childList.Count; i++)
            {
                var pnode = parNode.childList[i];
                if (pnode.nodeType == ENodeType.Comma)
                {
                    tparamList.Add(tempList);
                    tempList = new List<Node>();
                }
                else
                {
                    tempList.Add(pnode);
                }
            }
            if (tempList.Count > 0)
            {
                tparamList.Add(tempList);
            }


            HashSet<string> nameSet = new HashSet<string>();
            for (int i = 0; i < tparamList.Count; i++)
            {
                var nodelist = tparamList[i];
                FileMetaParamterDefine cdp = new FileMetaParamterDefine(m_FileMeta, nodelist);
                if (nameSet.Contains(cdp.name))
                {
                    Console.WriteLine("Error 参数名称有重名!!!");
                }
                AddMetaParamter(cdp);
            }
        }
        public void ParseTemplate( Node node )
        {
            if (node == null) return;

            List<Node> tempList = new List<Node>();
            for (int i = 0; i < node.childList.Count; i++)
            {
                var cnode = node.childList[i];
                if (cnode.nodeType == ENodeType.Comma)
                {
                    continue;
                }
                else
                {
                    FileMetaTemplateDefine cdp = new FileMetaTemplateDefine(m_FileMeta, cnode);
                    if (m_MetaTemplatesList.Find( a=> a.name == cdp.name ) != null )
                    {
                        Console.WriteLine("Error 参数名称有重名!!!");
                        continue;
                    }
                    AddMetaTemplate(cdp);
                }
            }
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
            if ( m_VirtualOverrideToken != null)
            {
                sb.Append(" " + m_VirtualOverrideToken.lexeme.ToString());
            }
            if (m_ReturnMetaClass != null)
            {
                sb.Append(" " + m_ReturnMetaClass.ToFormatString() );
            }
            if(m_MetaTemplatesList.Count > 0 )
            {
                sb.Append("<");
                for( int i = 0; i < m_MetaTemplatesList.Count; i++ )
                {
                    sb.Append(m_MetaTemplatesList[i].ToFormatString());
                    if( i < m_MetaTemplatesList.Count - 1 )
                    {
                        sb.Append(",");
                    }                    
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
