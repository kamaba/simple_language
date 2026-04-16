//****************************************************************************
//  File:      FileMetaMemberFunction.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Logging;
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
        public FileMetaParamterDefine(FileMeta fileMeta, List<Node> list)
        {
            m_FileMeta = fileMeta;

            ParseBuildMetaParamter( list );
        }
        public bool ParseBuildMetaParamter(List<Node> inputNodeList )
        {
            if (inputNodeList == null) return false;

            var listDefieNode = new List<Node>();
            var valueNodeList = new List<Node>();
            //Node beforeNode = new Node(null);
            //beforeNode.SetChildList(inputNodeList);
            //beforeNode.parseIndex = 0;
            //var nodeList = StructParse.HandleBeforeNode(beforeNode);
            var nodeList = StructParse.HandleNodeSingleLine(inputNodeList);

            if (!FileMetatUtil.SplitNodeList(nodeList, listDefieNode, valueNodeList, ref m_AssignToken))
            {
                Log.AddFileMetaLog(LID.AutoFileMetaMemberFunctionL47, "Error 解析NodeList出现错误~~~");
                return false;
            }
            if(valueNodeList.Count > 0 )
                m_Express = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, valueNodeList, FileMetaTermExpress.EExpressType.ParamVariable);


            Node nameNode = null;
            Node typeNode = null;
            if (!GetNameAndTypeNode(listDefieNode, ref nameNode, ref typeNode, ref m_ParamsToken ))
            {
                Log.AddFileMetaLog( LID.AutoFileMetaMemberFunctionL58, "Error 没有找到该定义名称 必须使用例: X = 102; 的格式");
                return false;
            }
            if (nameNode == null)
            {
                Log.AddFileMetaLog(LID.AutoFileMetaMemberFunctionL63, "Error 没有找到该定义名称 必须使用例: X = 101; 的格式");
                return false;
            }
            m_Token = nameNode?.token;

            if (typeNode != null)
            {
                Node qNode = null;
                int idx = listDefieNode.IndexOf(typeNode);
                if (idx >= 0 && idx + 1 < listDefieNode.Count)
                {
                    var nextNode = listDefieNode[idx + 1];
                    if (nextNode.nodeType == ENodeType.QuestionMark || (nextNode.token != null && nextNode.token.type == ETokenType.QuestionMark))
                    {
                        qNode = nextNode;
                    }
                }
                m_ClassDefineRef = new FileMetaClassDefine(m_FileMeta, typeNode, qNode);
            }

            return true;
        }
        public bool GetNameAndTypeNode(List<Node> listDefieNode, ref Node nameNode, ref Node typeNode, ref Token paramstoken )
        {
            List<Node> removeNodeList = new List<Node>();
            for (int i = 0; i < listDefieNode.Count - 1; i++)
            {
                var curNode = listDefieNode[i];
                Node nextNode = listDefieNode[i + 1];
                if ( nextNode.nodeType == ENodeType.Bracket)
                {
                    curNode.AddBracketNode( nextNode );
                    removeNodeList.Add(nextNode);
                }
                else if(curNode.nodeType == ENodeType.Key && curNode.token.type == ETokenType.Params )
                {
                    typeNode = curNode;
                    paramstoken = curNode.token;
                    removeNodeList.Add(curNode);
                }

            }
            for( int i = 0; i < removeNodeList.Count; i++)
            {
                listDefieNode.Remove(removeNodeList[i]);
            }

            if (listDefieNode.Count == 2)
            {
                typeNode = listDefieNode[0];
                nameNode = listDefieNode[1];
            }
            else if (listDefieNode.Count == 1)
            {
                nameNode = listDefieNode[0];
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
        public List<FileMetaAttributeSyntax> attributeList => m_AttributeList;

        public Token interfaceToken => m_InterfaceToken;
        public Token staticToken => m_StaticToken;
        public Token overrideToken => m_OverrideToken;
        public Token abstractToken => m_AbstractToken;
        public Token permissionToken => m_PermissionToken;
        public Token getToken => m_GetToken;
        public Token setToken => m_SetToken;
        public Token finalToken => m_FinalToken;

        private Token m_InterfaceToken = null;
        private Token m_StaticToken = null;
        private Token m_AbstractToken = null;
        private Token m_FinalToken = null;
        private Token m_GetToken = null;
        private Token m_SetToken = null;
        private Token m_OverrideToken = null;
        private Token m_PermissionToken = null;
        private Token m_LeftBraceToken = null;
        private Token m_RightBraceToken = null;
        private Node m_BlockNode;

        private readonly List<FileMetaAttributeSyntax> m_AttributeList = new List<FileMetaAttributeSyntax>();

        public void AddAttributes(List<FileMetaAttributeSyntax> list)
        {
            if (list == null || list.Count == 0) return;
            m_AttributeList.AddRange(list);
        }

        public void AddAttribute(FileMetaAttributeSyntax attr)
        {
            if (attr == null) return;
            m_AttributeList.Add(attr);
        }

        public FileMetaMemberFunction( FileMeta fm, Node block, List<Node> nodeList)
        {
            m_FileMeta = fm;
            m_BlockNode = block;
            ParseFunction(nodeList);
        }

        public FileMetaMemberFunction(FileMeta fm, string functionName, Token ownerToken, Token leftBraceToken, Token rightBraceToken)
        {
            m_FileMeta = fm;
            m_Token = new Token(ownerToken?.path ?? fm?.path ?? string.Empty, ETokenType.Identifier, functionName, 0, 0);
            m_LeftBraceToken = leftBraceToken ?? ownerToken;
            m_RightBraceToken = rightBraceToken ?? ownerToken;
            if (m_LeftBraceToken != null && m_RightBraceToken != null)
            {
                m_FileMetaBlockSyntax = new FileMetaBlockSyntax(m_FileMeta, m_LeftBraceToken, m_RightBraceToken);
            }
        }
        public bool ParseFunction(List<Node> nodeList)
        {
            Token permissionToken = null;
            Token overrideToken = null;
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
            Node funNameNode = null;
            //Node node = new Node(null);
            //node.childList.AddRange(nodeList);
            //var nodeList2 = StructParse.HandleBeforeNode(node);
            var nodeList2 = StructParse.HandleNodeSingleLine(nodeList);

            while (addCount < nodeList2.Count)
            {
                var cnode = nodeList2[addCount++];

                if (cnode.nodeType == ENodeType.IdentifierLink)
                {
                    if (cnode.parNode != null)
                    {
                        if(funNameNode != null )
                        {
                            Log.AddFileMetaLog( LID.ShowExtendMessage, token, "Error 已有函数实体，不能同时出现两个函数实体!");
                        }
                        funNameNode = cnode;
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
                        || token.type == ETokenType.Extern )
                    {
                        if (permissionToken == null)
                        {
                            permissionToken = token;
                        }
                        else
                        {
                            isError = true;
                            Log.AddFileMetaLog(LID.FileFunctionDefineConflict, token, $"permission: [{permissionToken.lexeme.ToString()}]");
                        }
                    }
                    else if (token.type == ETokenType.Override)
                    {
                        if (overrideToken != null)
                        {
                            isError = true;
                            Log.AddFileMetaLog(LID.FileFunctionDefineConflict, token, $"override:[{overrideToken.lexeme.ToString()}]");
                        }
                        overrideToken = token;
                    }
                    else if (token.type == ETokenType.Abstract)
                    {
                        if (m_AbstractToken != null)
                        {
                            isError = true;
                            Log.AddFileMetaLog(LID.FileFunctionDefineConflict, token, $"abstract:[{m_AbstractToken.lexeme.ToString()}]");
                        }
                        m_AbstractToken = token;
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
                            Log.AddFileMetaLog(LID.FileFunctionDefineConflict, token, $"get:[{getToken.lexeme.ToString()}]" );
                        }
                        getToken = token;
                    }
                    else if (token.type == ETokenType.Set)
                    {
                        if (setToken != null)
                        {
                            isError = true;
                            Log.AddFileMetaLog(LID.FileFunctionDefineConflict, token, $"set:[{getToken.lexeme.ToString()}]");
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
                            Log.AddFileMetaLog(LID.FileFunctionDefineConflict, token, $"set:[{interfaceToken.lexeme.ToString()}]" );
                        }
                        interfaceToken = token;
                    }
                    else if( token.type == ETokenType.Final )
                    {
                        if(finalToken != null )
                        {
                            isError = true;
                            Log.AddFileMetaLog(LID.FileFunctionDefineConflict, token, $"final:[{finalToken.lexeme.ToString()}]" );
                        }
                        finalToken = token;
                    }
                    else
                    {
                        isError = true;
                        Log.AddFileMetaLog(LID.FileFunctionDefineNotHandle,  token, $"");
                        break;
                    }
                }
            }
            if(funNameNode == null )
            {
                Log.AddFileMetaLog(LID.FileFunctionDefineNotName, nodeList[0].token );
                return false;
            }

            ParseParam(funNameNode.parNode);
            ParseTemplate(funNameNode.angleNode);

            m_Token = funNameNode.token;
            if ( m_BlockNode != null)
            {
                m_LeftBraceToken = m_BlockNode.token;
                m_RightBraceToken = m_BlockNode.endToken;
                m_FileMetaBlockSyntax = new FileMetaBlockSyntax(m_FileMeta, m_LeftBraceToken, m_RightBraceToken);
            }
            if( isError )
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, m_Token, "ParseFileMetaMemberFunction have Error");
            }
            m_OverrideToken = overrideToken;            
            m_PermissionToken = permissionToken;
            m_StaticToken = staticToken;
            m_GetToken = getToken;
            m_SetToken = setToken;
            m_FinalToken = finalToken;
            if (returnClassNameNode != null)
            {
                Node qNode = null;
                if (returnClassNameNode.parent != null)
                {
                    var siblings = returnClassNameNode.parent.childList;
                    int idx = siblings.IndexOf(returnClassNameNode);
                    if (idx >= 0 && idx + 1 < siblings.Count)
                    {
                        var nextNode = siblings[idx + 1];
                        if (nextNode.nodeType == ENodeType.QuestionMark || (nextNode.token != null && nextNode.token.type == ETokenType.QuestionMark))
                        {
                            qNode = nextNode;
                        }
                    }
                }
                m_DefineMetaClass = new FileMetaClassDefine(m_FileMeta, returnClassNameNode, qNode);
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
                    Log.AddFileMetaLog(LID.AutoFileMetaMemberFunctionL435, "Error 参数名称有重名!!!");
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
                        Log.AddFileMetaLog(LID.AutoFileMetaMemberFunctionL457, "Error 参数名称有重名!!!");
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

            EPermission permis = EPermission.Null;
            if( m_PermissionToken != null)
                permis = CompilerUtil.GetPerMissionByType( m_PermissionToken.type );
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
