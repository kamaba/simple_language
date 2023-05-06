//****************************************************************************
//  File:      FileMetaSyntax.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile.Parse;
using static SimpleLanguage.Compile.Parse.StructParse;
using System.Xml.Linq;
using System.Runtime.Intrinsics.X86;

namespace SimpleLanguage.Compile.CoreFileMeta
{    
    public partial class FileMetaSyntax : FileMetaBase
    {
        public int parseIndex { get; private set; } = 0;
        public bool isAppendSemiColon { get; set; } = true;
        public List<FileMetaSyntax> fileMetaSyntax => m_FileMetaSyntax;

        public bool IsNotEnd()
        {
            return parseIndex < m_FileMetaSyntax.Count;
        }
        public FileMetaSyntax GetCurrentSyntaxAndMove( int moveIndex = 1 )
        {
            if (parseIndex < m_FileMetaSyntax.Count )
            {
                FileMetaSyntax fms =  m_FileMetaSyntax[parseIndex];
                parseIndex += moveIndex;
                return fms;
            }
            return null;
        }

        protected List<FileMetaSyntax> m_FileMetaSyntax = new List<FileMetaSyntax>();
        protected FileMetaSyntax()
        {

        }
        private List<Node> m_NodeList = new List<Node>();
        public void AddFileMetaSyntax( FileMetaSyntax fms )
        {
            m_FileMetaSyntax.Add(fms);
        }
        public virtual string ToFormatString()
        {
            return "";
        }
    }
    public class FileMetaConditionExpressSyntax : FileMetaSyntax
    {
        // if condition{}  elif condition{}  while condition{}  dowhile condition{}

        public FileMetaBaseTerm conditionExpress => m_ConditionExpress;
        public FileMetaBlockSyntax executeBlockSyntax => m_ExecuteBlockSyntax;



        private FileMetaBaseTerm m_ConditionExpress = null;
        private FileMetaBlockSyntax m_ExecuteBlockSyntax = null;

        public FileMetaConditionExpressSyntax( FileMeta fm, Token _ifToken, FileMetaBaseTerm _condition, FileMetaBlockSyntax _executeBlockSyntax )
        {
            m_FileMeta = fm;
            m_Token = _ifToken;

            m_ConditionExpress = _condition;
            m_ExecuteBlockSyntax = _executeBlockSyntax;
        }

        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_ExecuteBlockSyntax?.SetDeep(_deep);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            if (m_Token != null)
            {
                sb.Append(m_Token.lexeme.ToString() + " ");
            }
            sb.Append(m_ConditionExpress?.ToFormatString());

            sb.Append(Environment.NewLine);
            sb.Append(m_ExecuteBlockSyntax?.ToFormatString());

            return sb.ToString();
        }
    }
    public class FileMetaKeyIfSyntax : FileMetaSyntax
    {
        public FileMetaConditionExpressSyntax ifExpressSyntax => m_IfExpressSyntax;
        public List<FileMetaConditionExpressSyntax> elseIfExpressSyntax => m_ElseIfExpressSyntax;
        public FileMetaKeyOnlySyntax elseExpressSyntax => m_ElseExpressSyntax;


        private FileMetaConditionExpressSyntax m_IfExpressSyntax = null;
        private List<FileMetaConditionExpressSyntax> m_ElseIfExpressSyntax = new List<FileMetaConditionExpressSyntax>();
        private FileMetaKeyOnlySyntax m_ElseExpressSyntax = null;


        public static FileMetaKeyIfSyntax ParseIfSyntax( FileMeta fm, SyntaxNodeStruct sns)
        {
            FileMetaKeyIfSyntax ifSyntax = new FileMetaKeyIfSyntax(fm);
            FileMetaBaseTerm conditionExpress = FileMetatUtil.CreateFileMetaExpress(fm, sns.keyContent, FileMetaTermExpress.EExpressType.Common);
            FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(fm, sns.blockNode.token, sns.blockNode.endToken);
            var fms = new FileMetaConditionExpressSyntax(fm, sns.keyNode.token, conditionExpress, executeBlock);

            if (sns.keyNode.token.type == ETokenType.If)
            {
                if (ifSyntax.ifExpressSyntax != null)
                {
                    Console.WriteLine("Error 不能有多个if语句!!");
                }
                ifSyntax.SetFileMetaConditionExpressSyntax(fms);
            }

            for (int i = 0; i < sns.followKeySyntaxStructList.Count; i++)
            {
                var csns = sns.followKeySyntaxStructList[i];
                var cnode = csns.keyNode;
                Token token = cnode.token;
                if (token.type == ETokenType.ElseIf)
                {
                    FileMetaBaseTerm child_conditionExpress = FileMetatUtil.CreateFileMetaExpress(fm, csns.keyContent, FileMetaTermExpress.EExpressType.Common);
                    FileMetaBlockSyntax child_executeBlock = new FileMetaBlockSyntax(fm, csns.blockNode.token, csns.blockNode.endToken);
                    var child_fms = new FileMetaConditionExpressSyntax(fm, token, child_conditionExpress, child_executeBlock);

                    ifSyntax.AddElseIfExpressSyntax(child_fms);
                }
                else if (token.type == ETokenType.Else)
                {
                    FileMetaBlockSyntax executeBlock2 = new FileMetaBlockSyntax(fm, csns.blockNode.token, csns.blockNode.endToken);
                    var fms3 = new FileMetaKeyOnlySyntax(fm, token, executeBlock2);

                    ifSyntax.SetElseExpressSyntax(fms3);
                }
            }
            return ifSyntax;
        }

        public FileMetaKeyIfSyntax(FileMeta fm )
        {
            m_FileMeta = fm;
        }
        public void SetFileMetaConditionExpressSyntax(FileMetaConditionExpressSyntax fmces)
        {
            m_IfExpressSyntax = fmces;
        }
        public void SetElseExpressSyntax(FileMetaKeyOnlySyntax fmces)
        {
            m_ElseExpressSyntax = fmces;
        }
        public void AddElseIfExpressSyntax(FileMetaConditionExpressSyntax fmces )
        {
            m_ElseIfExpressSyntax.Add( fmces );
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_IfExpressSyntax?.SetDeep(m_Deep);
            for (int i = 0; i < m_ElseIfExpressSyntax.Count; i++)
            {
                m_ElseIfExpressSyntax[i].SetDeep(m_Deep);
            }
            m_ElseExpressSyntax?.SetDeep(m_Deep);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if(m_IfExpressSyntax != null )
            {
                sb.Append(m_IfExpressSyntax.ToFormatString());
            }
            for( int i = 0; i < m_ElseIfExpressSyntax.Count; i++ )
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_ElseIfExpressSyntax[i].ToFormatString());
            }
            if(m_ElseExpressSyntax != null )
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_ElseExpressSyntax.ToFormatString());
            }

            return sb.ToString();
        }
    }
    public class FileMetaKeySwitchSyntax : FileMetaSyntax
    {
        public class FileMetaKeyCaseSyntax
        {
            public FileMeta fileMeta => m_FileMeta;
            public bool isContinueNextCastSyntax { get; set; } = false;
            public Token variableToken => m_VariableToken;
            public FileMetaCallLink defineClassCallLink => m_DefineClassToken;
            public FileMetaBlockSyntax executeBlockSyntax => m_ExecuteBlockSyntax;
            public List<FileMetaConstValueTerm> constValueTokenList => m_ConstValueTokenList;

            private FileMeta m_FileMeta = null;
            private Token m_Token = null;
            private FileMetaCallLink m_DefineClassToken = null;
            private Token m_VariableToken = null;

            private List<FileMetaConstValueTerm> m_ConstValueTokenList = new List<FileMetaConstValueTerm>();
            private FileMetaBlockSyntax m_ExecuteBlockSyntax = null;
            public int deep { get; set; } = 0;

            public void aaaa()
            {
                /*
                var fmkcs = new FileMetaKeySwitchSyntax.FileMetaKeyCaseSyntax(m_FileMeta, castnode.token);

                var parlist = castnode.parNode.childList;
                if (parlist.Count == 0)
                {
                    Console.WriteLine("Error Case语句不允许没有检查值!!");
                }
                List<Node> childList = new List<Node>();
                bool isComma = false;
                for (int i = 0; i < parlist.Count; i++)
                {
                    if (parlist[i].token?.type == ETokenType.Comma)
                    {
                        isComma = true;
                        continue;
                    }
                    childList.Add(parlist[i]);
                }
                if (isComma)
                {
                    bool isSame = true;//是否通过,号切后的类型是相同的
                    for (int i = 0; i < childList.Count - 1; i++)
                    {
                        var curNode = childList[i];
                        var nextNode = childList[i + 1];
                        var type = curNode.token.type;
                        if (type != ETokenType.Number && type != ETokenType.String)
                        {
                            Console.WriteLine("Error 逗号分割只允许number,string");
                            break;
                        }
                        if (type != nextNode.token.type)
                        {
                            isSame = false;
                            break;
                        }
                    }
                    if (!isSame)
                    {
                        Console.WriteLine("Error 使用逗号切割开后，类型不相同!!");
                    }
                    for (int i = 0; i < childList.Count; i++)
                    {
                        fmkcs.AddConstValueTokenList(new FileMetaConstValueTerm(m_FileMeta, childList[i].token));
                    }
                }
                else
                {
                    if (parlist.Count == 2)
                    {
                        if (parlist[0].token?.type == ETokenType.Identifier
                            || parlist[1].token?.type == ETokenType.Identifier)
                        {
                            fmkcs.SetDefineClassNode(parlist[0]);
                            fmkcs.SetVariableToken(parlist[1].token);
                        }
                    }
                    else if (parlist.Count == 1)
                    {
                        var ttype = parlist[0].token?.type;
                        if (ttype == ETokenType.Type
                            || ttype == ETokenType.Identifier)
                        {
                            fmkcs.SetDefineClassNode(parlist[0]);
                        }
                        else if (ttype == ETokenType.Number
                            || ttype == ETokenType.String)
                        {
                            fmkcs.AddConstValueTokenList(new FileMetaConstValueTerm(m_FileMeta, parlist[0].token));
                        }
                    }
                }
                FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(m_FileMeta, castnode.blockNode.token, castnode.blockNode.endToken);
                fmkcs.SetExecuteBlockSyntax(executeBlock);
                ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(executeBlock);
                m_CurrentNodeInfoStack.Push(pcnic);
                ParseSyntax(castnode.blockNode);
                m_CurrentNodeInfoStack.Pop();

                if (j != castlist.Count - 1)
                    fmkcs.isContinueNextCastSyntax = true;

                fms.AddFileMetaKeyCaseSyntaxList(fmkcs);
                */
            }
            public FileMetaKeyCaseSyntax(FileMeta fm, Token castToken )
            {
                m_FileMeta = fm;
                m_Token = castToken;
            }
            public void SetDefineClassNode( Node _defineClassNode )
            {
                m_DefineClassToken = new FileMetaCallLink(m_FileMeta, _defineClassNode);
            }
            public void SetVariableToken(Token _variableToken)
            {
                m_VariableToken = _variableToken;
            }
            public void SetExecuteBlockSyntax( FileMetaBlockSyntax ebs )
            {
                m_ExecuteBlockSyntax = ebs;
            }
            public void AddConstValueTokenList( FileMetaConstValueTerm fmcvt )
            {
                m_ConstValueTokenList.Add(fmcvt);
            }
            public void SetDeep(int _deep)
            {
                deep = _deep;
                m_ExecuteBlockSyntax.SetDeep(_deep);
            }
            public string ToFormatString()
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.Append("case ");
                if(m_DefineClassToken != null )
                {
                    if (defineClassCallLink != null)
                    {
                        sb.Append(defineClassCallLink.ToFormatString() + " ");
                    }
                    if (m_VariableToken != null)
                    {
                        sb.Append(m_VariableToken.lexeme?.ToString() + " ");
                    }
                }
                else
                {
                    for( int i = 0; i < m_ConstValueTokenList.Count; i++ )
                    {
                        sb.Append(m_ConstValueTokenList[i].ToFormatString() );
                        if (i < m_ConstValueTokenList.Count - 1)
                            sb.Append(",");
                    }
                }
                
                if(m_ExecuteBlockSyntax != null )
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(m_ExecuteBlockSyntax.ToFormatString());
                }
                if( isContinueNextCastSyntax )
                {
                    sb.Append("next;");
                }


                return sb.ToString();
            }
        }

        public FileMetaCallLink fileMetaVariableRef => m_FileMetaVariableRef;
        public FileMetaBlockSyntax defaultExecuteBlockSyntax => m_DefaultExecuteBlockSyntax;
        public List<FileMetaKeyCaseSyntax> fileMetaKeyCaseSyntaxList => m_FileMetaKeyCaseSyntaxList;
        public FileMetaBlockSyntax executeBlockSyntax => m_DefaultExecuteBlockSyntax;

        private Token m_LeftBraceToken = null;
        private Token m_RightBraceToken = null;
        private FileMetaCallLink m_FileMetaVariableRef = null;
        private FileMetaBlockSyntax m_DefaultExecuteBlockSyntax = null;
        private List<FileMetaKeyCaseSyntax> m_FileMetaKeyCaseSyntaxList = new List<FileMetaKeyCaseSyntax>();



        public FileMetaKeySwitchSyntax(FileMeta fm, Token _switchToken, Token _leftBraceToken,
            Token _rightBraceToken, FileMetaCallLink cl )
        {
            m_FileMeta = fm;
            m_Token = _switchToken;
            m_LeftBraceToken = _leftBraceToken;
            m_RightBraceToken = _rightBraceToken;
            m_FileMetaVariableRef = cl;
        }
        public void AddFileMetaKeyCaseSyntaxList(FileMetaKeyCaseSyntax keyCase )
        {
            m_FileMetaKeyCaseSyntaxList.Add(keyCase);
        }
        public void SetDefaultExecuteBlockSyntax(FileMetaBlockSyntax fmbs)
        {
            m_DefaultExecuteBlockSyntax = fmbs;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            for (int i = 0; i < m_FileMetaKeyCaseSyntaxList.Count; i++)
            {
                m_FileMetaKeyCaseSyntaxList[i].SetDeep(m_Deep + 1);
            }
            m_DefaultExecuteBlockSyntax?.SetDeep(m_Deep  + 1);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("switch ");
            if(m_FileMetaVariableRef != null )
            {
                sb.Append(m_FileMetaVariableRef.ToFormatString());
            }
            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("{");

            for ( int i = 0; i < m_FileMetaKeyCaseSyntaxList.Count; i++ )
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_FileMetaKeyCaseSyntaxList[i].ToFormatString());
            }
            if(m_DefaultExecuteBlockSyntax != null )
            {
                sb.Append(Environment.NewLine);
                for (int i = 0; i < deep + 1; i++)
                    sb.Append(Global.tabChar);
                sb.Append("default");
                sb.Append(Environment.NewLine);
                sb.Append(m_DefaultExecuteBlockSyntax.ToFormatString());
            }

            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("}");

            return sb.ToString();
        }
    }
    public class FileMetaKeyForSyntax : FileMetaSyntax
    {
        public FileMetaBlockSyntax executeBlockSyntax => m_ExecuteBlockSyntax;
        public FileMetaSyntax fileMetaClassDefine => m_FileMetaClassDefine;
        public FileMetaBaseTerm conditionExpress => m_ConditionExpress;
        public FileMetaOpAssignSyntax stepFileMetaOpAssignSyntax => m_StepFileMetaOpAssignSyntax;
        public bool isInFor { get { return m_InToken != null;  } }

        private FileMetaBlockSyntax m_ExecuteBlockSyntax = null;
        private FileMetaSyntax m_FileMetaClassDefine = null;
        private FileMetaBaseTerm m_ConditionExpress  = null;
        private FileMetaOpAssignSyntax m_StepFileMetaOpAssignSyntax = null;
        private Token m_InToken = null;

        public FileMetaKeyForSyntax(FileMeta fm, Token _forToken, FileMetaBlockSyntax fmbs )
        {
            m_FileMeta = fm;
            m_Token = _forToken;
            m_ExecuteBlockSyntax = fmbs;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_ExecuteBlockSyntax?.SetDeep(m_Deep);
        }        
        public void SetFileMetaClassDefine(FileMetaSyntax fmdvs)
        {
            m_FileMetaClassDefine = fmdvs;
            if( fmdvs is FileMetaCallSyntax )
            {
                (fmdvs as FileMetaCallSyntax).isAppendSemiColon = false;
            }
        }
        public void SetInKeyAndArrayVariable(Token inToken, FileMetaBaseTerm cn)
        {
            m_InToken = inToken;
            m_ConditionExpress = cn;
        }
        public void SetConditionExpress(FileMetaBaseTerm fmte)
        {
            m_ConditionExpress = fmte;
        }
        public void SetStepFileMetaOpAssignSyntax(FileMetaOpAssignSyntax fmoas)
        {
            m_StepFileMetaOpAssignSyntax = fmoas;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            
            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("for ");
            if (m_FileMetaClassDefine != null)
            {
                sb.Append(m_FileMetaClassDefine.ToFormatString() );
            }
            if (m_InToken != null)
            {
                sb.Append( " " + m_InToken.lexeme.ToString());
                sb.Append(" " + m_ConditionExpress.ToFormatString());
            }
            else
            {
                if (m_ConditionExpress != null)
                {
                    sb.Append(", " + m_ConditionExpress.ToFormatString() );
                }
                if (m_StepFileMetaOpAssignSyntax != null)
                {
                    sb.Append(", " + m_StepFileMetaOpAssignSyntax.ToFormatString());
                }
            }
            if (m_ExecuteBlockSyntax != null)
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_ExecuteBlockSyntax.ToFormatString());
            }

            return sb.ToString();
        }
    }
    public class FileMetaKeyOnlySyntax : FileMetaSyntax
    {
        public FileMetaBlockSyntax executeBlockSyntax => m_ExecuteBlockSyntax;

        private FileMetaBlockSyntax m_ExecuteBlockSyntax = null;
        public FileMetaKeyOnlySyntax(FileMeta fm, Token _token, FileMetaBlockSyntax _executeBlockSyntax)
        {
            m_FileMeta = fm;
            m_Token = _token;
            m_ExecuteBlockSyntax = _executeBlockSyntax;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            if(m_ExecuteBlockSyntax != null )
            {
                m_ExecuteBlockSyntax.SetDeep(_deep);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append(m_Token?.lexeme.ToString() );
            if (m_ExecuteBlockSyntax != null)
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_ExecuteBlockSyntax?.ToFormatString());
            }
            return sb.ToString();
        }
    }
    public class FileMetaKeyReturnSyntax : FileMetaSyntax
    {
        public FileMetaBaseTerm returnExpress =>m_ReturnExpress;

        private FileMetaBaseTerm m_ReturnExpress = null;

        public static FileMetaKeyReturnSyntax ParseIfSyntax(FileMeta fm, SyntaxNodeStruct akss)
        {           
            var cnode = akss.keyNode;
            FileMetaBaseTerm conditionExpress = FileMetatUtil.CreateFileMetaExpress(fm, akss.keyContent, FileMetaTermExpress.EExpressType.Common);
            var fms = new FileMetaKeyReturnSyntax(fm, cnode.token, conditionExpress);
            return fms;
        }
        public FileMetaKeyReturnSyntax(FileMeta fm, Token _token, FileMetaBaseTerm _express )
        {
            m_FileMeta = fm;
            m_Token = _token;
            m_ReturnExpress = _express;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append(m_Token?.lexeme.ToString() + " ");
            sb.Append(m_ReturnExpress?.ToFormatString());

            return sb.ToString();
        }
    }
    public class FileMetaKeyGotoLabelSyntax: FileMetaSyntax
    {
        public Token labelToken => m_LabelToken;

        private Token m_LabelToken = null;

        public static FileMetaKeyGotoLabelSyntax ParseIfSyntax(FileMeta fm, SyntaxNodeStruct akss)
        {
            var cnode = akss.keyNode;
            Token labelToken = null;
            if (akss.keyContent.Count != 1 )
            {
                Console.WriteLine("Error 解析Goto Label语法，只支持 goto id;的语法!!");
            }
            else
            {
                labelToken = akss.keyContent[0].token;
                if (labelToken.type != ETokenType.Identifier)
                {
                    Console.WriteLine("Error 解析GotoLabel中 后边必须使用普通字符");
                }
            }
            var fms = new FileMetaKeyGotoLabelSyntax( fm, cnode.token, labelToken);

            return fms;
        }
        public FileMetaKeyGotoLabelSyntax(FileMeta fm, Token _token, Token _label )
        {
            m_FileMeta = fm;
            m_Token = _token;
            m_LabelToken = _label;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append(m_Token?.lexeme.ToString() + " ");
            sb.Append(m_LabelToken?.lexeme.ToString() );
            return sb.ToString();
        }
    }
    public class FileMetaCallSyntax : FileMetaSyntax
    {
        public FileMetaCallLink variableRef => m_FileMetaVariableRef;

        private FileMetaCallLink m_FileMetaVariableRef;
        public FileMetaCallSyntax( FileMetaCallLink fmrv )
        {
            m_FileMetaVariableRef = fmrv;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append( m_FileMetaVariableRef?.ToFormatString() );
            if(isAppendSemiColon )
            {
                sb.Append(";");
            }
            return sb.ToString();
        }
    }
    public class FileMetaDefineVariableSyntax : FileMetaSyntax
    {
        public Token nameToken => m_Token;
        public Token staticToken => m_StaticToken;
        public Token assignToken => m_AssignToken;
        public FileMetaClassDefine fileMetaClassDefine => m_FileMetaClassDefine;       
        public FileMetaBaseTerm express => m_FileMetaExpress;

        private FileMetaClassDefine m_FileMetaClassDefine = null;
        private Token m_StaticToken = null;
        private Token m_AssignToken = null;
        private FileMetaBaseTerm m_FileMetaExpress = null;

        public FileMetaDefineVariableSyntax( FileMeta fm, FileMetaClassDefine fmcd, Token nameToken,
            Token _assignToken, Token _staticToken, FileMetaBaseTerm _express )
        {
            m_FileMeta = fm;
            m_Token = nameToken;
            m_StaticToken = _staticToken;
            m_AssignToken = _assignToken;
            m_FileMetaClassDefine = fmcd;
            m_FileMetaExpress = _express;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            if( m_StaticToken != null )
            {
                sb.Append( m_StaticToken.lexeme.ToString() + " ");
            }
            if (m_FileMetaClassDefine != null)
                sb.Append( m_FileMetaClassDefine.ToFormatString() + " ");
            sb.Append( m_Token.lexeme.ToString() + " ");
            if(m_AssignToken!= null )
            {
                sb.Append( m_AssignToken.lexeme.ToString() + " " );
                sb.Append(m_FileMetaExpress.ToFormatString());
                if( isAppendSemiColon )
                {
                   sb.Append(";");
                }
            }
            return sb.ToString();
        }
    }
    public class FileMetaOpAssignSyntax : FileMetaSyntax
    {
        public FileMetaCallLink variableRef => m_VariableRef;
        public FileMetaBaseTerm express => m_Express;
        public Token assignToken { get; protected set;} = null;
        public Token constToken { get; protected set;} = null;
        public Token staticToken { get; protected set; } = null;

        private FileMetaCallLink m_VariableRef = null;
        private FileMetaBaseTerm m_Express = null;
        public FileMetaOpAssignSyntax(FileMetaCallLink fileMetaVariableRef, Token _opAssignToken, FileMetaBaseTerm fme, bool flag = false  )
        {
            m_VariableRef = fileMetaVariableRef;
            assignToken = _opAssignToken;
            m_Express = fme;
            isAppendSemiColon = flag;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_Express?.SetDeep(m_Deep);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append(m_VariableRef.ToFormatString());
            sb.Append(" " + assignToken.lexeme.ToString());
            sb.Append(" " + m_Express?.ToFormatString() );
            if (isAppendSemiColon)
            {
                sb.Append(";");
            }
            return sb.ToString();
        }

    }
    public class FileMetaBlockSyntax : FileMetaSyntax
    {
        public Token beginBlock => m_BeginBlock;
        public Token endBlock => m_EndBlock;

        private Token m_BeginBlock = null;
        private Token m_EndBlock = null;
        public FileMetaBlockSyntax( FileMeta fm, Token _bblock, Token _eblock )
        {
            m_BeginBlock = _bblock;
            m_EndBlock = _eblock;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            for (int i = 0; i < m_FileMetaSyntax.Count; i++)
            {
                m_FileMetaSyntax[i].SetDeep(m_Deep + 1);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append(beginBlock.lexeme.ToString() + Environment.NewLine);
            for( int i = 0; i < m_FileMetaSyntax.Count; i++ )
            {
                sb.Append(m_FileMetaSyntax[i].ToFormatString());                
                sb.Append( Environment.NewLine );
            }
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("}");

            return sb.ToString();
        }
    }    
}
