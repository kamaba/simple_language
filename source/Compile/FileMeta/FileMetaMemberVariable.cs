//****************************************************************************
//  File:      FileMetaMemberVariable.cs
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
    public partial class FileMetaMemberVariable : FileMetaBase
    {
        public FileMetaClassDefine classDefineRef => m_ClassDefineRef;
        public Token permissionToken => m_PermissionToken;
        public Token staticToken => m_StaticToken;
        public Token nameToken => m_Token;
        public FileMetaBaseTerm express =>m_Express;

        private MetaMemberVariable m_MetaMemberVariable = null;

        private FileMetaClassDefine m_ClassDefineRef;
        private Token m_AssignToken = null;
        private Token m_PermissionToken = null;
        private Token m_StaticToken = null;        
        private FileMetaBaseTerm m_Express;

        private List<Node> m_NodeList = null;
        public FileMetaMemberVariable( FileMeta fm, List<Node> list)
        {
            m_FileMeta = fm;

            m_NodeList = list;

            ParseBuildMetaVariable();
        }

        public bool ParseBuildMetaVariable()
        {
            var bedoreNodeList = new List<Node>();
            var afterNodeList = new List<Node>();

            if (!FileMetatUtil.SplitNodeList(m_NodeList, bedoreNodeList, afterNodeList, ref m_AssignToken))
            {
                Console.WriteLine("Error 解析NodeList出现错误~~~");
                return false;
            }
            if (bedoreNodeList.Count < 1)
            {
                Console.WriteLine("Error listDefieNode 不能为空~");
                return false;
            }

            Node beforeNode = new Node( null );
            beforeNode.childList = bedoreNodeList;
            beforeNode.parseIndex = 0;
            StructParse.HandleLinkNode(beforeNode);
            var defineNodeList = beforeNode.childList;


            Node nameNode = null;
            Node typeNode = null;
            if (!GetNameAndTypeToken(defineNodeList, ref typeNode, ref nameNode, ref m_PermissionToken, ref m_StaticToken))
            {
                Console.WriteLine("Error 没有找到该定义名称 必须使用例: X = 10; 的格式");
                return false;
            }

            if (nameNode == null)
            {
                Console.WriteLine("Error 没有找到该定义名称 必须使用例: X = 10; 的格式");
                return false;
            }
            if (nameNode.extendLinkNodeList.Count > 0)
            {
                Console.WriteLine("Error 没有找到该定义名称 必须使用例: X = 10; 的格式");
                return false;
            }
            m_Token = nameNode.token;

            if (typeNode != null)
                m_ClassDefineRef = new FileMetaClassDefine(m_FileMeta, typeNode);                        

            m_Express = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, afterNodeList, FileMetaTermExpress.EExpressType.MemberVariable );

            return true;
        }
        public bool GetNameAndTypeToken(List<Node> defineNodeList, ref Node typeNode, ref Node nameNode, ref Token permissionToken, ref Token staticToken)
        {
            bool isError = false;

            List<Node> nodeList = new List<Node>();
            for (int i = 0; i < defineNodeList.Count; i++)
            {
                var cnode = defineNodeList[i];

                if (cnode.nodeType == ENodeType.IdentifierLink)
                {
                    nodeList.Add(cnode);
                }
                else
                {
                    var token = cnode.token;
                    if (token.type == ETokenType.Public
                        || token.type == ETokenType.Projected
                        || token.type == ETokenType.Internal
                        || token.type == ETokenType.Private)
                    {
                        if (permissionToken != null)
                        {
                            isError = true;
                            Console.WriteLine("Error 多重定义名称的权限定义!!");
                        }
                        permissionToken = token;
                    }
                    else if (token.type == ETokenType.Static)
                    {
                        if (staticToken != null)
                        {
                            isError = true;
                            Console.WriteLine("Error 多重定义名称的静态定义!!");
                        }
                        staticToken = token;
                    }
                    else if( token.type == ETokenType.Type )
                    {
                        nodeList.Add(cnode);
                    }
                    else
                    {
                        Console.WriteLine("Error 解析变量中，不允许的类型存在!!");
                    }
                }
            }

            if (nodeList.Count == 1)
            {
                nameNode = nodeList[0];
            }
            else if (nodeList.Count == 2)
            {
                typeNode = nodeList[0];
                nameNode = nodeList[1];
            }

            return !isError;
        }
        public void SetMetaMemberVariable( MetaMemberVariable mmv )
        {
            m_MetaMemberVariable = mmv;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            EPermission permis = CompilerUtil.GetPerMissionByString(m_PermissionToken?.lexeme.ToString());
            if( permis == EPermission.Null )
            {
                sb.Append("_public");
            }
            else
            {
                sb.Append(permis.ToFormatString());
            }
            if(m_StaticToken!= null )
            {
                sb.Append(" " + m_StaticToken.lexeme.ToString());
            }
            if (m_ClassDefineRef != null)
                sb.Append( " " + m_ClassDefineRef.ToFormatString());
            sb.Append(" " + name );
            if(m_AssignToken!= null )
            {
                sb.Append( " " + m_AssignToken.lexeme.ToString());
                sb.Append( " " + m_Express?.ToFormatString());
            }
            sb.Append(";");

            return sb.ToString();
        }
    }
}