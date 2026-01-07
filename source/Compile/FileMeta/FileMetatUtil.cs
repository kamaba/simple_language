//****************************************************************************
//  File:      FileMetaUtil.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile.Grammer;
using SimpleLanguage.Parse;
using System.Collections.Generic;

namespace SimpleLanguage.Compile
{
    /// <summary>
    /// 统一的表达式处理工具类
    /// Token → Node 折叠和规范化 → FileMeta 创建的集中入口
    /// </summary>
    public partial class FileMetatUtil
    {
        public static List<string> GetLinkStringMidPeriodList(List<Token> tokenList)
        {
            List<string> stringList = new List<string>();
            for (int i = 0; i < tokenList.Count; i++)
            {
                var token = tokenList[i];
                if (token.lexeme == null)
                {
                    Log.AddInStructFileMeta(EError.None, "检查到Import语句中，token内容lexeme为空!!");
                    return null;
                }
                if (token.type != ETokenType.Period)
                {
                    if (!GrammerUtil.IdentifierCheck(token.lexeme.ToString()))
                    {
                        Log.AddInStructFileMeta(EError.None, "检查到Import语句中，导入名称不合规!!");
                        return null;
                    }
                    stringList.Add(token.lexeme.ToString());
                }
            }
            return stringList;
        }
        public static bool IsSymbol( Token token )
        {
            switch( token.type )
            {
                case ETokenType.Plus:
                case ETokenType.Minus:
                case ETokenType.Multiply:
                case ETokenType.Divide:
                case ETokenType.DoublePlus:     //++
                case ETokenType.DoubleMinus:    //--
                case ETokenType.Modulo:          // %
                case ETokenType.Not:             // !
                case ETokenType.Negative:        // ~
                case ETokenType.Shi:               //  <<
                case ETokenType.Shr:               //  >>
                case ETokenType.Less:            // >
                case ETokenType.GreaterOrEqual:  // >=
                case ETokenType.Greater:         // <
                case ETokenType.LessOrEqual:     // <=
                case ETokenType.Equal:           // ==
                case ETokenType.NotEqual:        // !=
                case ETokenType.Combine:         // &
                case ETokenType.InclusiveOr:     // |
                case ETokenType.XOR:             //  ^
                case ETokenType.Or:              // ||
                case ETokenType.And:             // &&  
                case ETokenType.PlusAssign:             // +=
                case ETokenType.MinusAssign:            // -=
                case ETokenType.MultiplyAssign:         // *=
                case ETokenType.DivideAssign:           // /=
                case ETokenType.ModuloAssign:           // %=
                case ETokenType.InclusiveOrAssign:      // |=
                case ETokenType.XORAssign:              // ^=
                    {
                        return true;
                    }
            }
            return false;
        }
        public static bool SplitNodeList(List<Node> nodeList, List<Node> preNodeList, List<Node> afterNodeList, ref Token assignToken )
        {
            bool isEqual = false;
            for (int i = 0; i < nodeList.Count; i++)
            {
                var n = nodeList[i];
                if (n.token.type == ETokenType.Assign)
                {
                    isEqual = true;
                    assignToken = n.token;
                    continue;
                }
                if (isEqual)
                    afterNodeList.Add(n);
                else
                    preNodeList.Add(n);
            }
            if (isEqual)
            {
                if (afterNodeList.Count == 0)
                {
                    Log.AddInStructFileMeta(EError.None, "解析NodeStructVariable时有=号，但没有值内容 " + assignToken?.ToLexemeAllString() );
                    return false;
                }
            }
            if (preNodeList.Count == 0)
            {
                return false;
            }
            return true;
        }
        public static FileMetaBaseTerm CreateFileOneTerm( FileMeta fm, Node node, FileMetaTermExpress.EExpressType expressType)
        {
            FileMetaBaseTerm fmbt = null;
            if (node.nodeType == ENodeType.IdentifierLink
                || (node.nodeType == ENodeType.Key && 
                        (node.token?.type == ETokenType.This|| node.token?.type == ETokenType.Base ) ) 
                )
            {
                fmbt = new FileMetaCallTerm(fm, node);
                fmbt.priority = SignComputePriority.Level1;
            }
            else if (node.nodeType == ENodeType.ConstValue)
            {
                if( node.extendLinkNodeList.Count > 1 )
                {
                    fmbt = new FileMetaCallTerm(fm, node);
                    fmbt.priority = SignComputePriority.Level1;
                }
                else
                {
                    fmbt = new FileMetaConstValueTerm(fm, node.token);
                    fmbt.priority = SignComputePriority.Level1;
                }
            }
            else if (node.nodeType == ENodeType.Par)
            {
                fmbt = new FileMetaParTerm(fm, node, expressType);
                fmbt.priority = SignComputePriority.Level1;
            }
            else if (node.nodeType == ENodeType.Brace)
            {
                fmbt = new FileMetaBraceTerm(fm, node);
                fmbt.priority = SignComputePriority.Level1;
            }
            else if (node.nodeType == ENodeType.Bracket)
            {
                fmbt = new FileMetaBracketTerm(fm, node);
                fmbt.priority = SignComputePriority.Level1;
            }
            else
            {
                Log.AddInStructFileMeta(EError.None, "Error CreateFileOneTerm 单1表达式，没有找到该类型: " + node.token.type.ToString() + " 位置: " + node.token.ToLexemeAllString());
            }
            return fmbt;
        }

        /// <summary>
        /// 统一的 Token→Node 规范化入口。
        /// 把分散的表达式折叠逻辑集中到这里，然后生成可供 FileMeta 直接使用的 Node 列表。
        /// </summary>
        private static List<Node> NormalizeNodeListForFileMeta(FileMeta fm, List<Node> nodeList)
        {
            if (nodeList == null || nodeList.Count == 0)
                return nodeList;

            // 创建临时容器并规范化（折叠 par/angle/bracket/link）
            Node tempNode = new Node(null);
            tempNode.SetChildList(new List<Node>(nodeList));
            var normalized = StructParse.NormalizeExpression(tempNode);

            // 返回规范化后的节点列表
            return normalized ?? nodeList;
        }

        public static FileMetaBaseTerm CreateFileMetaExpress(FileMeta fm, List<Node> nodeList, FileMetaTermExpress.EExpressType expressType)
        {
            if (nodeList.Count == 0)
                return null;

            FileMetaBaseTerm fmbt = null;
            if ( nodeList.Count == 1 )
            {
                fmbt = CreateFileOneTerm(fm, nodeList[0], expressType );
            }
            else
            {
                bool isAsIsExpress = false;
                for (int i = 0; i < nodeList.Count; i++)
                {
                    if (nodeList[i].token?.type == ETokenType.As
                        || nodeList[i].token?.type == ETokenType.Is )
                    {
                        isAsIsExpress = true;
                        break;
                    }
                }
                if( isAsIsExpress )
                {
                    fmbt = new FileMetaAsOrIsTerm(fm, nodeList);
                }
                else
                {
                    fmbt = new FileMetaTermExpress(fm, nodeList, expressType);
                }
            }
            if( fmbt == null )
            {
                Log.AddInStructFileMeta(EError.None, "Error 生成表达式错误!!");
                return null;
            }
            fmbt.BuildAST();
            return fmbt;
        }
    }

    public static partial class FileMetatUtil
    {
        public static FileMetaBaseTerm CreateFileMetaExpressFromTokens(
            FileMeta fm,
            List<Token> tokens,
            FileMetaTermExpress.EExpressType eType)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return null;
            }

            // 简单去掉首尾空白/换行/分号
            var cleaned = TrimTokenList(tokens);
            if (cleaned.Count == 0)
            {
                return null;
            }

            var parser = new TokenExpressionParser(fm, cleaned, eType);
            return parser.Parse();
        }

        private static List<Token> TrimTokenList(List<Token> list)
        {
            if (list == null || list.Count == 0) return new List<Token>();
            int start = 0;
            int end = list.Count - 1;
            while (start <= end && (list[start].type == ETokenType.Space || list[start].type == ETokenType.LineEnd || list[start].type == ETokenType.SemiColon))
                start++;
            while (end >= start && (list[end].type == ETokenType.Space || list[end].type == ETokenType.LineEnd || list[end].type == ETokenType.SemiColon))
                end--;
            if (end < start) return new List<Token>();
            return list.GetRange(start, end - start + 1);
        }

        private sealed class TokenExpressionParser
        {
            private readonly FileMeta m_FileMeta;
            private readonly List<Token> m_Tokens;
            private readonly FileMetaTermExpress.EExpressType m_ExprType;
            private int m_Index;

            public TokenExpressionParser(FileMeta fm, List<Token> tokens, FileMetaTermExpress.EExpressType eType)
            {
                m_FileMeta = fm;
                m_Tokens = tokens;
                m_ExprType = eType;
                m_Index = 0;
            }

            public FileMetaBaseTerm Parse()
            {
                // 最小骨架: 先按二元表达式解析, 后续再逐步扩展
                return ParseBinaryExpression(0);
            }

            private FileMetaBaseTerm ParsePrimary()
            {
                var t = Current;
                if (t == null) return null;

                // 简单支持常量和标识符
                if (t.type == ETokenType.Const)
                {
                    Advance();
                    return new FileMetaConstValueTerm(m_FileMeta, t);
                }

                if (t.type == ETokenType.Identifier)
                {
                    Advance();
                    // 通过临时Node适配到现有的 FileMetaCallTerm(Node) 构造
                    Node fakeNode = new Node(t) { nodeType = ENodeType.IdentifierLink };
                    return new FileMetaCallTerm(m_FileMeta, fakeNode);
                }

                if (t.type == ETokenType.LeftPar)
                {
                    Advance();
                    var inner = ParseBinaryExpression(0);
                    Expect(ETokenType.RightPar);
                    return inner;
                }

                return null;
            }

            private FileMetaBaseTerm ParseBinaryExpression(int parentPrecedence)
            {
                var left = ParsePrimary();
                while (true)
                {
                    var op = Current;
                    int prec = GetPrecedence(op);
                    if (prec <= parentPrecedence)
                        break;

                    // 当前先忽略实际的二元运算构造，后续可根据需要扩展
                    Advance();
                    var right = ParsePrimary();
                    if (right == null)
                        break;
                    // 简化处理：暂时返回左侧，保留解析结构最小可用
                    left = left ?? right;
                }
                return left;
            }

            private int GetPrecedence(Token op)
            {
                if (op == null) return 0;
                switch (op.type)
                {
                    case ETokenType.Assign: return 1;
                    case ETokenType.Or: return 2;               // ||
                    case ETokenType.And: return 3;              // &&
                    case ETokenType.Equal:
                    case ETokenType.NotEqual: return 4;
                    case ETokenType.Less:
                    case ETokenType.LessOrEqual:
                    case ETokenType.Greater:
                    case ETokenType.GreaterOrEqual: return 5;
                    case ETokenType.Plus:
                    case ETokenType.Minus: return 6;
                    case ETokenType.Multiply:
                    case ETokenType.Divide:
                    case ETokenType.Modulo: return 7;
                    default: return 0;
                }
            }

            private Token Current => m_Index < m_Tokens.Count ? m_Tokens[m_Index] : null;

            private void Advance()
            {
                if (m_Index < m_Tokens.Count) m_Index++;
            }

            private void Expect(ETokenType type)
            {
                if (Current?.type != type)
                {
                    Log.AddInStructFileMeta(EError.UnMatchChar, "Error 表达式缺少符号: " + type);
                }
                else
                {
                    Advance();
                }
            }
        }
    }

    public partial class FileMetatUtil
    {
        /// <summary>
        /// 根据一条语句的 Token 列表创建对应的 FileMetaSyntax。
        /// 该方法只关心“这是一条什么语句”，不负责切分多条语句。
        /// 后续可以在这里逐步扩展 if/while/for/switch 等关键字。
        /// </summary>
        public static FileMetaSyntax CreateFileMetaSyntaxFromTokens(
            FileMeta fm,
            List<Token> statementTokens)
        {
            if (fm == null || statementTokens == null || statementTokens.Count == 0)
                return null;

            // 简单去掉前后空白/换行/分号
            var tokens = TrimTokenList(statementTokens);
            if (tokens.Count == 0)
                return null;

            var first = tokens[0];

            // return 语句：return expr;
            if (first.type == ETokenType.Return)
            {
                var exprTokens = new List<Token>();
                for (int i = 1; i < tokens.Count; i++)
                    exprTokens.Add(tokens[i]);

                var expr = CreateFileMetaExpressFromTokens(
                    fm,
                    exprTokens,
                    FileMetaTermExpress.EExpressType.Common);

                return new FileMetaKeyReturnSyntax(fm, first, expr);
            }

            // if / while / dowhile / for / switch / as-is 语句
            // 当前仅提供最小骨架：识别关键字，并构造对应的语法节点壳，“这种语法”能否通过编译还要看后续完整性检查
            if (first.type == ETokenType.If)
            {
                // if (cond) { } 简化版：条件=整个 tokens[1..]，block 由外部 BlockSyntax 表示
                var condTokens = tokens.GetRange(1, tokens.Count - 1);
                var cond = CreateFileMetaExpressFromTokens(
                    fm,
                    condTokens,
                    FileMetaTermExpress.EExpressType.Common);
                var dummyBlock = new FileMetaBlockSyntax(fm, null, null);
                var ifCond = new FileMetaConditionExpressSyntax(fm, first, cond, dummyBlock);
                var ifSyntax = new FileMetaKeyIfSyntax(fm);
                ifSyntax.SetFileMetaConditionExpressSyntax(ifCond);
                return ifSyntax;
            }

            if (first.type == ETokenType.While)
            {
                // while (cond) { }  条件表达式由 tokens[1..] 提供，具体执行块仍由外部 BlockSyntax 表示
                var condTokens = tokens.GetRange(1, tokens.Count - 1);
                var cond = CreateFileMetaExpressFromTokens(
                    fm,
                    condTokens,
                    FileMetaTermExpress.EExpressType.Common);
                var dummyBlock = new FileMetaBlockSyntax(fm, null, null);
                return new FileMetaConditionExpressSyntax(fm, first, cond, dummyBlock);
            }

            if (first.type == ETokenType.DoWhile)
            {
                // do..while：先构造一个空的 FileMetaKeyOnlySyntax 占位，方便后续替换为复合语句
                var dummyBlock = new FileMetaBlockSyntax(fm, null, null);
                return new FileMetaKeyOnlySyntax(fm, first, dummyBlock);
            }

            if (first.type == ETokenType.For)
            {
                // for 语句：当前不拆分 init/cond/step，全部作为条件表达式处理
                var bodyTokens = tokens.GetRange(1, tokens.Count - 1);
                var expr = CreateFileMetaExpressFromTokens(
                    fm,
                    bodyTokens,
                    FileMetaTermExpress.EExpressType.Common);
                var dummyBlock = new FileMetaBlockSyntax(fm, null, null);
                var forSyntax = new FileMetaKeyForSyntax(fm, first, dummyBlock);
                if (expr != null)
                {
                    forSyntax.SetConditionExpress(expr);
                }
                return forSyntax;
            }

            if (first.type == ETokenType.Switch)
            {
                // switch 语句：switch 后面的表达式作为变量引用，case/default 仍由 Node 流程解析；这里仅占位
                var exprTokens = tokens.GetRange(1, tokens.Count - 1);
                var expr = CreateFileMetaExpressFromTokens(
                    fm,
                    exprTokens,
                    FileMetaTermExpress.EExpressType.Common);
                // 暂时无法直接构造 FileMetaKeySwitchSyntax（需要 FileMetaCallLink 和 case block 信息），
                // 因此返回 null，交由旧 Node 流或后续扩展处理。
                return null;
            }

            // as / is 表达式语句：cond as T / cond is T
            // 这类语句通常作为表达式使用，这里统一走表达式构造即可
            bool hasAsOrIs = false;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].type == ETokenType.As || tokens[i].type == ETokenType.Is)
                {
                    hasAsOrIs = true;
                    break;
                }
            }
            if (hasAsOrIs)
            {
                // 仅构造表达式，不生成单独语句节点
                var _ = CreateFileMetaExpressFromTokens(
                    fm,
                    tokens,
                    FileMetaTermExpress.EExpressType.Common);
                return null;
            }

            // 其它关键字(for/switch/..) 更精细的语法，将来可以在这里继续扩展

            // 默认：暂不对普通表达式生成语句节点，避免和旧 Node 流流程不一致
            return null;
        }
    }
}
