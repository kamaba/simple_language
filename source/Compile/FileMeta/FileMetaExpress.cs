//****************************************************************************
//  File:      FileMetaExpress.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Compile
{
    public class FileMetaBaseTerm : FileMetaBase
    {
        public bool isDirty { get; set; } = false;
        public int priority { get; set; } = int.MaxValue;
        public bool isOnlyOne
        {
            get
            {
                return left == null && right == null;
            }
        }
        public List<FileMetaBaseTerm> fileMetaExpressList => m_FileMetaExpressList;
        public FileMetaBaseTerm left
        {
            get { return m_Left; }
            set
            {
                m_Left = value;
                isDirty = true;
            }
        }
        public FileMetaBaseTerm right
        {
            get { return m_Right; }
            set
            {
                m_Right = value;
                isDirty = true;
            }
        }
        public virtual FileMetaBaseTerm root
        {
            get
            {
                return m_Root;
            }
        }

        protected List<FileMetaBaseTerm> m_FileMetaExpressList = new List<FileMetaBaseTerm>();
        protected FileMetaBaseTerm m_Left = null;
        protected FileMetaBaseTerm m_Right = null;
        protected FileMetaBaseTerm m_Root = null;

        public List<FileMetaBaseTerm> SplitParamList()
        {
            List<FileMetaBaseTerm> ParamFileMetaTermList = new List<FileMetaBaseTerm>();

            List<List<FileMetaBaseTerm>> fmbtListList = new List<List<FileMetaBaseTerm>>();
            List<FileMetaBaseTerm> fmbtList = new List<FileMetaBaseTerm>();

            bool isComma = false;
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                var fmen = m_FileMetaExpressList[i];
                var fmst = fmen as FileMetaSymbolTerm;
                if (fmst != null && fmst.token.type == ETokenType.Comma)
                {
                    if (isComma)
                    {
                        Log.AddInStructFileMeta(EError.None, "Error 多重逗号，导致解析无法解析!!");
                        break;
                    }
                    if (fmbtList.Count == 0)
                    {
                        Log.AddInStructFileMeta(EError.None, "Error 首符号不能为逗号");
                        break;
                    }
                    isComma = true;
                    fmbtListList.Add(fmbtList);
                    fmbtList = new List<FileMetaBaseTerm>();
                }
                else
                {
                    isComma = false;
                    fmbtList.Add(fmen);
                }
            }
            if (fmbtList.Count == 0)
            {
                return ParamFileMetaTermList;
            }
            fmbtListList.Add(fmbtList);

            for (int i = 0; i < fmbtListList.Count; i++)
            {
                var fmbt2 = fmbtListList[i];

                if (fmbt2.Count == 1)
                {
                    ParamFileMetaTermList.Add(fmbt2[0]);
                }
                else
                {
                    //FileMetaTermExpress fmte = new FileMetaTermExpress(fileMeta);
                    //m_ParamFileMetaTermList.Add(fmte);
                    //fmte.AddRangeFileMetaTerm(fmbt2);
                    ParamFileMetaTermList.AddRange(fmbt2);
                }
            }
            return ParamFileMetaTermList;
        }
        public virtual void ClearDirty()
        {
            isDirty = false;
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                var fme = m_FileMetaExpressList[i];
                fme.ClearDirty();
            }
        }
        public virtual void AddFileMetaTerm(FileMetaBaseTerm fmn)
        {
            fmn.SetFileMeta(m_FileMeta);
            m_FileMetaExpressList.Add(fmn);
        }
        public virtual void AddRangeFileMetaTerm(List<FileMetaBaseTerm> fmn)
        {
            for (int i = 0; i < fmn.Count; i++)
            {
                fmn[i].SetFileMeta(m_FileMeta);
            }
            m_FileMetaExpressList.AddRange(fmn);
        }
        public virtual bool BuildAST()
        {
            return true;
        }
        public override string ToFormatString()
        {
            return token.lexeme.ToString();
        }
        public virtual string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_Token != null)
                sb.Append(m_Token.ToLexemeAllString());

            if (left != null)
            {
            }
            if (m_Right != null)
            {
            }
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                var fme = m_FileMetaExpressList[i];
                sb.Append(" " + fme.ToTokenString());
            }
            return sb.ToString();
        }
    }
    public class FileMetaSymbolTerm : FileMetaBaseTerm
    {
        public ETokenType symBolType
        {
            get
            {
                if (m_Token != null)
                {
                    return m_Token.type;
                }
                return ETokenType.None;
            }
        }
        public FileMetaSymbolTerm(FileMeta fm, Token _token)
        {
            m_FileMeta = fm;
            m_Token = _token;
            m_Root = this;
            SetPriory();
        }

        // Token 版本构造方法（实际上 FileMetaSymbolTerm 本身就是 Token 驱动的，这里用 List<Token> 兼容接口）
        public FileMetaSymbolTerm(FileMeta fm, List<Token> tokenList)
        {
            m_FileMeta = fm;
            m_Root = this;

            if (tokenList != null && tokenList.Count > 0)
            {
                m_Token = tokenList[0];
            }

            SetPriory();
        }

        private void SetPriory()
        {
            if (m_Token == null) return;

            switch (m_Token.type)
            {

                case ETokenType.Plus:
                case ETokenType.Minus:
                    priority = SignComputePriority.Level2_LinkOp;
                    break;
                case ETokenType.Multiply:
                case ETokenType.Divide:
                    priority = SignComputePriority.Level2_LinkOp;
                    break;
                case ETokenType.DoublePlus:
                case ETokenType.DoubleMinus:
                    priority = SignComputePriority.Level2_LinkOp;
                    break;
                case ETokenType.Modulo:
                case ETokenType.Not:
                case ETokenType.Negative:
                    priority = SignComputePriority.Level3_Hight_Compute;
                    break;
                //case ETokenType.Shi:
                //case ETokenType.Shr:
                //    priority = SignComputePriority.Level5_BitMoveOp;
                //    break;
                case ETokenType.Less:
                case ETokenType.GreaterOrEqual:
                case ETokenType.Greater:
                case ETokenType.LessOrEqual:
                    priority = SignComputePriority.Level6_Compare;
                    break;
                case ETokenType.Equal:
                case ETokenType.NotEqual:
                    priority = SignComputePriority.Level7_EqualAb;
                    break;
                case ETokenType.Combine:
                    priority = SignComputePriority.Level8_BitAndOp;
                    break;
                case ETokenType.InclusiveOr:
                    priority = SignComputePriority.Level8_BitOrOp;
                    break;
                case ETokenType.XOR:
                    priority = SignComputePriority.Level8_BitXOrOp;
                    break;
                case ETokenType.Or:
                    priority = SignComputePriority.Level9_Or;
                    break;
                case ETokenType.And:
                    priority = SignComputePriority.Level9_And;
                    break;
                case ETokenType.PlusAssign:
                case ETokenType.MinusAssign:
                case ETokenType.MultiplyAssign:
                case ETokenType.DivideAssign:
                case ETokenType.ModuloAssign:
                case ETokenType.InclusiveOrAssign:
                case ETokenType.XORAssign:
                    priority = SignComputePriority.Level3_Hight_Compute;
                    break;
                case ETokenType.Comma:
                    priority = SignComputePriority.Level12_Split;
                    break;
                case ETokenType.Colon:
                    priority = SignComputePriority.Level12_Split;
                    break;
            }
            priority = SignComputePriority.Level2_LinkOp;
        }
        public override bool BuildAST()
        {
            return true;
        }
        public override string ToFormatString()
        {
            return token.lexeme.ToString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(token?.lexeme.ToString());

            return sb.ToString();
        }
    }
    public class FileMetaAsOrIsTerm : FileMetaBaseTerm
    {
        public bool isAsTerm => m_AsOrIsToken?.type == ETokenType.As;
        public FileMetaCallLink variableCallLink => m_VariableCallLink;
        public FileMetaClassDefine defineType => m_DefineType;
        public Token convertIsTypeNameToken => m_ConvertIsTypeNameToken;
        public Token asOrIsToken => m_AsOrIsToken;

        private FileMetaCallLink m_VariableCallLink = null;
        private Token m_AsOrIsToken = null;
        private FileMetaClassDefine m_DefineType = null;
        private Token m_ConvertIsTypeNameToken = null;

        public FileMetaAsOrIsTerm(FileMeta fm, List<Token> tokenList)
        {
            m_FileMeta = fm;
            m_Root = this;

            if (tokenList == null || tokenList.Count < 3)
            {
                Log.AddInStructFileMeta(EError.None, "Error AsOrIsTerm Token列表长度不足，需要至少3个token（变量 as/is 类型）");
                return;
            }

            // 查找 as 或 is 关键字的位置
            int asIsIndex = -1;
            for (int i = 1; i < tokenList.Count; i++)
            {
                if (tokenList[i].type == ETokenType.As || tokenList[i].type == ETokenType.Is)
                {
                    asIsIndex = i;
                    break;
                }
            }

            if (asIsIndex == -1)
            {
                Log.AddInStructFileMeta(EError.None, "Error AsOrIsTerm 未找到 as 或 is 关键字");
                return;
            }

            // 变量部分：从开始到 as/is 前一个位置
            var varTokens = tokenList.GetRange(0, asIsIndex);
            if (varTokens.Count == 0)
            {
                Log.AddInStructFileMeta(EError.None, "Error AsOrIsTerm 变量部分为空");
                return;
            }

            // 使用纯 Token 版本的 FileMetaCallLink
            m_VariableCallLink = new FileMetaCallLink(fm, varTokens);

            // as/is 关键字
            m_AsOrIsToken = tokenList[asIsIndex];

            // 类型部分：从 as/is 后一个位置到结尾
            if (asIsIndex + 1 >= tokenList.Count)
            {
                Log.AddInStructFileMeta(EError.None, "Error AsOrIsTerm 类型部分为空");
                return;
            }

            var typeTokens = tokenList.GetRange(asIsIndex + 1, tokenList.Count - asIsIndex - 1);

            // 处理可能的转换变量名（is 表达式中的第四部分）
            // 格式：var is Class1 newVarName
            if (m_AsOrIsToken?.type == ETokenType.Is && typeTokens.Count >= 2)
            {
                // 假设最后一个 token 是新变量名（如果之前有 as，则该部分为空）
                // 这里需要识别类型和变量名的边界
                // 简单实现：如果是 is，最后一个 token 可能是变量名
                var lastToken = typeTokens[typeTokens.Count - 1];
                if (lastToken.type == ETokenType.Identifier && typeTokens.Count > 1)
                {
                    // 移除最后一个 token，它是转换后的变量名
                    m_ConvertIsTypeNameToken = lastToken;
                    typeTokens = typeTokens.GetRange(0, typeTokens.Count - 1);
                }
            }

            // 为类型创建 FileMetaClassDefine
            // 直接使用 Token 列表构建，不涉及 Node
            m_DefineType = new FileMetaClassDefine(fm, typeTokens);

            // 验证 as 不能有转换变量名
            if (m_AsOrIsToken?.type == ETokenType.As && m_ConvertIsTypeNameToken != null)
            {
                Log.AddInStructFileMeta(EError.None, "Error as 表达式不能有转换变量名");
                m_ConvertIsTypeNameToken = null;
            }
        }

        public override string ToFormatString()
        {
            return m_AsOrIsToken?.ToConstString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_AsOrIsToken?.ToLexemeAllString());

            return sb.ToString();
        }
    }
    public class FileMetaConstValueTerm : FileMetaBaseTerm
    {
        private Token m_PlusOrMinusToken = null;
        public FileMetaConstValueTerm(FileMeta fm, Token _token, Token plusMinusToken = null)
        {
            m_FileMeta = fm;
            m_Token = _token;
            m_PlusOrMinusToken = plusMinusToken;
            m_Root = this;
        }
        public override string ToFormatString()
        {
            return m_Token?.ToConstString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_Token?.ToLexemeAllString());

            return sb.ToString();
        }
    }
    public class FileMetaCallTerm : FileMetaBaseTerm
    {
        public FileMetaCallLink callLink => m_CallLink;

        private FileMetaCallLink m_CallLink = null;

        // Token 版本构造方法（纯 Token 实现，无 Node 构建）
        public FileMetaCallTerm(FileMeta fm, List<Token> tokenList)
        {
            m_FileMeta = fm;
            m_Root = this;

            if (tokenList != null && tokenList.Count > 0)
            {
                // 直接使用 Token 列表版本的 FileMetaCallLink，不创建临时 Node
                m_CallLink = new FileMetaCallLink(fm, tokenList);

                // 设置第一个 token 作为主 token（用于位置信息等）
                m_Token = tokenList[0];
            }
        }

        public override bool BuildAST()
        {
            if (m_CallLink == null)
                return false;

            for (int j = 0; j < m_CallLink.callNodeList.Count; j++)
            {
                var clc = callLink.callNodeList[j];
                if (clc.fileMetaParTerm != null)
                {
                    bool flag = clc.fileMetaParTerm.BuildAST();
                    if (flag == false)
                        return false;
                }
            }
            return true;
        }

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            if (m_CallLink != null)
                sb.Append(m_CallLink.ToFormatString());
            return sb.ToString();
        }

        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            if (callLink != null)
            {
                sb.Append(callLink.ToTokenString());
            }
            return sb.ToString();
        }
    }
    public class FileMetaParTerm : FileMetaBaseTerm
    {
        public Token endToken => m_EndToken;

        private Token m_EndToken = null;

        // Token 版本构造方法
        public FileMetaParTerm(FileMeta fm, List<Token> tokenList, FileMetaTermExpress.EExpressType expressType)
        {
            m_FileMeta = fm;
            m_Root = this;

            if (tokenList == null || tokenList.Count < 2)
            {
                Log.AddInStructFileMeta(EError.None, "Error ParTerm Token列表长度不足");
                return;
            }

            m_Token = tokenList[0];  // '('
            m_EndToken = tokenList[tokenList.Count - 1];  // ')'

            // 将 token 列表中间的部分视为参数或子表达式，交给统一的表达式解析器
            if (tokenList.Count > 2)
            {
                var paramTokens = tokenList.GetRange(1, tokenList.Count - 2);

                // 按顶层逗号拆分参数 token 列表：
                // - 如果存在多个片段 => 视为函数/调用参数列表: ClassInit(a, b + (c.v-20))
                // - 如果只有一个片段且无顶层逗号 => 视为嵌套表达式: (a + c.x / (100.0f - b))
                List<List<Token>> paramListList = new List<List<Token>>();
                List<Token> tempParamList = new List<Token>();

                int parenDepth = 0;
                int angleDepth = 0;
                int bracketDepth = 0;
                int braceDepth = 0;

                for (int i = 0; i < paramTokens.Count; i++)
                {
                    var t = paramTokens[i];

                    // 跟踪嵌套深度，避免在括号/泛型/数组/大括号内部误拆
                    if (t.type == ETokenType.LeftPar) parenDepth++;
                    else if (t.type == ETokenType.RightPar && parenDepth > 0) parenDepth--;
                    else if (t.type == ETokenType.Less) angleDepth++;
                    else if (t.type == ETokenType.Greater && angleDepth > 0) angleDepth--;
                    else if (t.type == ETokenType.LeftBracket) bracketDepth++;
                    else if (t.type == ETokenType.RightBracket && bracketDepth > 0) bracketDepth--;
                    else if (t.type == ETokenType.LeftBrace) braceDepth++;
                    else if (t.type == ETokenType.RightBrace && braceDepth > 0) braceDepth--;

                    // 顶层逗号作为参数分隔符
                    if (t.type == ETokenType.Comma && parenDepth == 0 && angleDepth == 0 && bracketDepth == 0 && braceDepth == 0)
                    {
                        if (tempParamList.Count > 0)
                        {
                            paramListList.Add(new List<Token>(tempParamList));
                            tempParamList.Clear();
                        }
                    }
                    else
                    {
                        tempParamList.Add(t);
                    }
                }

                if (tempParamList.Count > 0)
                {
                    paramListList.Add(tempParamList);
                }

                // 为每个参数调用统一的表达式构造入口
                if (paramListList.Count == 1)
                {
                    // 如果只有一个片段并且没有顶层逗号，则将整个 () 内视为一个整体表达式
                    var innerExpr = FileMetatUtil.CreateFileMetaExpressFromTokens(
                        m_FileMeta,
                        paramListList[0],
                        expressType);

                    if (innerExpr != null)
                    {
                        AddFileMetaTerm(innerExpr);
                    }
                }
                else
                {
                    // 多个片段：每个片段是一个独立的参数表达式
                    foreach (var paramList in paramListList)
                    {
                        var expr = FileMetatUtil.CreateFileMetaExpressFromTokens(
                            m_FileMeta,
                            paramList,
                            expressType);

                        if (expr != null)
                        {
                            AddFileMetaTerm(expr);
                        }
                    }
                }
            }
        }

        public override void ClearDirty()
        {
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                m_FileMetaExpressList[i].ClearDirty();
            }
        }
        public override bool BuildAST()
        {
            if (m_FileMetaExpressList.Count == 1)
            {
                FileMetaBaseTerm fmbt = m_FileMetaExpressList[0];
                if( fmbt == null) return false;
                if (fmbt.BuildAST())
                {
                    isDirty = true;
                    m_Root = fmbt.root;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                bool flag = true;
                for (int i = 0; i < m_FileMetaExpressList.Count; i++)
                {
                    var fme = m_FileMetaExpressList[i];
                    if (!fme.BuildAST())
                    {
                        flag = false;
                    }
                }

                m_Root = this;
                return flag;
            }
        }
        public override string ToFormatString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(m_Token.lexeme.ToString());
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                stringBuilder.Append(m_FileMetaExpressList[i].ToFormatString());
                if (i < m_FileMetaExpressList.Count - 1)
                    stringBuilder.Append(",");
            }
            stringBuilder.Append(m_EndToken.lexeme.ToString());
            return stringBuilder.ToString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Param:(");
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                var fme = m_FileMetaExpressList[i];
                sb.Append(fme.ToTokenString());
                if (i < m_FileMetaExpressList.Count - 1)
                    sb.Append(",");
            }
            sb.Append(")");

            return sb.ToString();
        }
    }
    public class FileMetaBracketTerm : FileMetaBaseTerm
    {
        public Token beginToken => m_BeginBracketToken;
        public Token endToken => m_EndBracketetToken;

        Token m_BeginBracketToken = null;
        Token m_EndBracketetToken = null;

        // = [1][2][var1.index]  Node 版本构造方法（legacy）
        // public FileMetaBracketTerm(FileMeta fm, Node node) { ... }

        // Token 版本构造方法
        public FileMetaBracketTerm(FileMeta fm, List<Token> tokenList)
        {
            m_FileMeta = fm;
            m_Root = this;

            if (tokenList == null || tokenList.Count < 2)
            {
                Log.AddInStructFileMeta(EError.None, "Error BracketTerm Token列表长度不足");
                return;
            }

            m_Token = tokenList[0];  // '['
            m_BeginBracketToken = tokenList[0];
            m_EndBracketetToken = tokenList[tokenList.Count - 1];  // ']'

            // 按逗号拆分数组元素
            if (tokenList.Count > 2)
            {
                var elemTokens = tokenList.GetRange(1, tokenList.Count - 2);
                List<List<Token>> elemListList = new List<List<Token>>();
                List<Token> tempElemList = new List<Token>();

                for (int i = 0; i < elemTokens.Count; i++)
                {
                    if (elemTokens[i].type == ETokenType.Comma)
                    {
                        elemListList.Add(new List<Token>(tempElemList));
                        tempElemList.Clear();
                    }
                    else
                    {
                        tempElemList.Add(elemTokens[i]);
                    }
                }

                if (tempElemList.Count > 0)
                {
                    elemListList.Add(tempElemList);
                }

                // 为每个元素创建表达式
                foreach (var elemList in elemListList)
                {
                    if (elemList.Count == 1)
                    {
                        var t = elemList[0];
                        if (t.type == ETokenType.Number || t.type == ETokenType.String || t.type == ETokenType.Const)
                        {
                            var constTerm = new FileMetaConstValueTerm(m_FileMeta, elemList[0]);
                            AddFileMetaTerm(constTerm);
                        }
                        else if (t.type == ETokenType.Identifier)
                        {
                            var callTerm = new FileMetaCallTerm(m_FileMeta, elemList);
                            AddFileMetaTerm(callTerm);
                        }
                    }
                    else if (elemList.Count > 0)
                    {
                        // 复杂表达式：使用 CallTerm
                        var term = new FileMetaCallTerm(m_FileMeta, elemList);
                        AddFileMetaTerm(term);
                    }
                }
            }
        }

        // = [{a=20;b="aaa";},{a=30;b="ccc";}] Node 版本构造方法（legacy）
        // public FileMetaBracketTerm(FileMeta fm, Node node, int a) { ... }

        // Token 版本构造方法
        public FileMetaBracketTerm(FileMeta fm, List<Token> tokenList, FileMetaTermExpress.EExpressType expressType)
        {
            m_FileMeta = fm;
            m_Root = this;

            if (tokenList == null || tokenList.Count < 2)
            {
                Log.AddInStructFileMeta(EError.None, "Error BracketTerm Token列表长度不足");
                return;
            }

            m_Token = tokenList[0];  // '['
            m_BeginBracketToken = tokenList[0];
            m_EndBracketetToken = tokenList[tokenList.Count - 1];  // ']'

            // 按逗号拆分数组元素
            if (tokenList.Count > 2)
            {
                var elemTokens = tokenList.GetRange(1, tokenList.Count - 2);
                List<List<Token>> elemListList = new List<List<Token>>();
                List<Token> tempElemList = new List<Token>();

                for (int i = 0; i < elemTokens.Count; i++)
                {
                    if (elemTokens[i].type == ETokenType.Comma)
                    {
                        elemListList.Add(new List<Token>(tempElemList));
                        tempElemList.Clear();
                    }
                    else
                    {
                        tempElemList.Add(elemTokens[i]);
                    }
                }

                if (tempElemList.Count > 0)
                {
                    elemListList.Add(tempElemList);
                }

                // 为每个元素创建表达式
                foreach (var elemList in elemListList)
                {
                    if (elemList.Count == 1)
                    {
                        var t = elemList[0];
                        if (t.type == ETokenType.Number || t.type == ETokenType.String || t.type == ETokenType.Const)
                        {
                            var constTerm = new FileMetaConstValueTerm(m_FileMeta, elemList[0]);
                            AddFileMetaTerm(constTerm);
                        }
                        else if (t.type == ETokenType.Identifier)
                        {
                            var callTerm = new FileMetaCallTerm(m_FileMeta, elemList);
                            AddFileMetaTerm(callTerm);
                        }
                    }
                    else if (elemList.Count > 0)
                    {
                        // 复杂表达式：使用 CallTerm
                        var term = new FileMetaCallTerm(m_FileMeta, elemList);
                        AddFileMetaTerm(term);
                    }
                }
            }
        }

        // = [{},{}]
        public override string ToFormatString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("[");
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                stringBuilder.Append(m_FileMetaExpressList[i].ToFormatString());
            }
            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("BeginBraceToken:" + m_BeginBracketToken?.ToLexemeAllString());
            sb.Append("EndBraceToken:" + m_EndBracketetToken?.ToLexemeAllString());

            return sb.ToString();
        }
    }
    public class FileMetaBraceTerm : FileMetaBaseTerm
    {
        private Token m_BraceEndToken = null;
        public FileMetaBraceTerm(FileMeta fm, List<Token> tokenList)
        {
            m_FileMeta = fm;
            m_Root = this;

            if (tokenList == null || tokenList.Count < 2)
            {
                Log.AddInStructFileMeta(EError.None, "Error BraceTerm Token列表长度不足");
                return;
            }

            m_Token = tokenList[0];  // '{'
            m_BraceEndToken = tokenList[tokenList.Count - 1];  // '}'

            // 使用 Token 版本处理大括号内容
            HandleBraceTermFromTokens(tokenList);
        }

        // Token 版本的大括号处理逻辑（纯 Token 实现）
        private void HandleBraceTermFromTokens(List<Token> tokenList)
        {
            // { a = 10, b = 20, c = Class1() }
            // 顶层用逗号分隔每个元素；每个元素内部统一交给表达式处理
            List<List<Token>> elementTokenLists = new List<List<Token>>();
            List<Token> temp = new List<Token>();
            int braceDepth = 0;
            int parenDepth = 0;
            int bracketDepth = 0;

            // 跳过首尾的大括号
            for (int i = 1; i < tokenList.Count - 1; i++)
            {
                var token = tokenList[i];

                if (token.type == ETokenType.LeftBrace) braceDepth++;
                else if (token.type == ETokenType.RightBrace && braceDepth > 0) braceDepth--;
                else if (token.type == ETokenType.LeftPar) parenDepth++;
                else if (token.type == ETokenType.RightPar && parenDepth > 0) parenDepth--;
                else if (token.type == ETokenType.LeftBracket) bracketDepth++;
                else if (token.type == ETokenType.RightBracket && bracketDepth > 0) bracketDepth--;

                // 顶层逗号分隔元素
                if (token.type == ETokenType.Comma && braceDepth == 0 && parenDepth == 0 && bracketDepth == 0)
                {
                    if (temp.Count > 0)
                    {
                        elementTokenLists.Add(new List<Token>(temp));
                        temp.Clear();
                    }
                }
                else
                {
                    temp.Add(token);
                }
            }

            if (temp.Count > 0)
            {
                elementTokenLists.Add(temp);
            }

            // 对每个元素，按是否存在 '=' 或 ':' 拆分键和值，统一走表达式入口
            foreach (var elemTokens in elementTokenLists)
            {
                if (elemTokens == null || elemTokens.Count == 0)
                    continue;

                List<Token> defineTokens = null;
                List<Token> valueTokens = null;
                Token assignToken = null;

                int assignIndex = -1;
                for (int j = 0; j < elemTokens.Count; j++)
                {
                    var t = elemTokens[j];
                    if ((t.type == ETokenType.Assign || t.type == ETokenType.Colon) && assignIndex == -1)
                    {
                        assignIndex = j;
                        assignToken = t;
                        break;
                    }
                }

                if (assignIndex != -1)
                {
                    defineTokens = elemTokens.GetRange(0, assignIndex);
                    if (assignIndex + 1 < elemTokens.Count)
                    {
                        valueTokens = elemTokens.GetRange(assignIndex + 1, elemTokens.Count - assignIndex - 1);
                    }
                }
                else
                {
                    defineTokens = elemTokens;
                }

                // 仅值的元素：直接作为一个表达式
                if (assignToken == null)
                {
                    var expr = FileMetatUtil.CreateFileMetaExpressFromTokens(
                        m_FileMeta,
                        defineTokens,
                        FileMetaTermExpress.EExpressType.Common);
                    if (expr != null)
                    {
                        AddFileMetaTerm(expr);
                    }
                }
                else
                {
                    // key = value 或 key : value
                    var keyExpr = FileMetatUtil.CreateFileMetaExpressFromTokens(
                        m_FileMeta,
                        defineTokens,
                        FileMetaTermExpress.EExpressType.Common);
                    var valExpr = FileMetatUtil.CreateFileMetaExpressFromTokens(
                        m_FileMeta,
                        valueTokens,
                        FileMetaTermExpress.EExpressType.Common);

                    if (keyExpr != null && valExpr != null)
                    {
                        var assignTerm = new FileMetaSymbolTerm(m_FileMeta, assignToken)
                        {
                            left = keyExpr,
                            right = valExpr
                        };
                        AddFileMetaTerm(assignTerm);
                    }
                }
            }
        }

        public override void ClearDirty()
        {
            base.ClearDirty();
        }

        public override string ToFormatString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(m_Token?.lexeme.ToString());
            foreach (var v in m_FileMetaExpressList)
            {
                stringBuilder.Append(v.ToFormatString());
            }
            stringBuilder.Append(m_BraceEndToken?.lexeme.ToString());
            return stringBuilder.ToString();
        }

        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("BeginBraceToken:" + m_Token?.ToLexemeAllString());
            sb.Append("EndBraceToken:" + m_BraceEndToken?.ToLexemeAllString());

            return sb.ToString();
        }
    }    
    /// <summary>
    /// 复杂表达式的包装类，支持 as/is、二元操作等多种表达式形式
    /// </summary>
    public class FileMetaTermExpress : FileMetaBaseTerm
    {
        public enum EExpressType
        {
            Common = 0,              // 普通表达式
            MemberVariable = 1,      // 成员变量初始值
            ParamVariable = 2,       // 参数默认值
        }

        private EExpressType m_ExpressType;
        public FileMetaTermExpress(FileMeta fm, List<Token> tokenList, EExpressType expressType)
        {
            m_FileMeta = fm;
            m_ExpressType = expressType;
            m_Root = this;

            if (tokenList == null || tokenList.Count == 0)
                return;

            // 简化处理：按照 Token 类型直接创建对应的 Term
            for (int i = 0; i < tokenList.Count; i++)
            {
                var t = tokenList[i];

                // 跳过分隔符和空白
                if (t.type == ETokenType.Space || t.type == ETokenType.LineEnd || t.type == ETokenType.SemiColon)
                    continue;

                FileMetaBaseTerm term = null;

                // 常量
                if (t.type == ETokenType.Number || t.type == ETokenType.String || t.type == ETokenType.Const)
                {
                    term = new FileMetaConstValueTerm(m_FileMeta, t);
                    term.priority = SignComputePriority.Level1;
                }
                // 标识符或调用
                else if (t.type == ETokenType.Identifier)
                {
                    // 收集连续的标识符和点号作为一个调用链
                    List<Token> callTokens = new List<Token> { t };
                    int j = i + 1;
                    while (j < tokenList.Count && 
                           (tokenList[j].type == ETokenType.Period || 
                            tokenList[j].type == ETokenType.Identifier))
                    {
                        callTokens.Add(tokenList[j]);
                        j++;
                    }
                    i = j - 1;
                    term = new FileMetaCallTerm(m_FileMeta, callTokens);
                    //term.priority = SignComputePriority_Level1;
                }
                // 操作符符号
                else if (FileMetatUtil.IsSymbol(t))
                {
                    term = new FileMetaSymbolTerm(m_FileMeta, t);
                }

                if (term != null)
                {
                    AddFileMetaTerm(term);
                }
            }
        }

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                sb.Append(m_FileMetaExpressList[i].ToFormatString());
                if (i < m_FileMetaExpressList.Count - 1)
                    sb.Append(" ");
            }
            return sb.ToString();
        }

        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Express(");
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                sb.Append(m_FileMetaExpressList[i].ToTokenString());
                if (i < m_FileMetaExpressList.Count - 1)
                    sb.Append(", ");
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
}
