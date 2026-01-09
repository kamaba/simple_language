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
                //case ETokenType.Shi:               //  <<
                //case ETokenType.Shr:               //  >>
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

            // 单一 Token 的快速路径（常量、标识符等）
            if (cleaned.Count == 1)
            {
                var t = cleaned[0];
                // 常量值: 数字/字符串/布尔/null/Const
                if (t.type == ETokenType.Number
                    || t.type == ETokenType.String
                    || t.type == ETokenType.BoolValue
                    || t.type == ETokenType.Null
                    || t.type == ETokenType.Const)
                {
                    return new FileMetaConstValueTerm(fm, t);
                }

                // 简单标识符: 退化为调用表达式 FileMetaCallTerm
                if (t.type == ETokenType.Identifier)
                {
                    return new FileMetaCallTerm(fm, cleaned);
                }

                // this/base/new 关键字也按调用处理
                if (t.type == ETokenType.This || t.type == ETokenType.Base || t.type == ETokenType.New)
                {
                    return new FileMetaCallTerm(fm, cleaned);
                }
            }

            // 其它表达式交给 TokenExpressionParser 解析（完全基于 Token）
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
                // 先解析可能的括号/数组/大括号/调用等，再按二元表达式组合
                return ParseBinaryExpression(0);
            }

            private FileMetaBaseTerm ParsePrimary()
            {
                var t = Current;
                if (t == null) return null;

                // 常量
                if (t.type == ETokenType.Number
                    || t.type == ETokenType.String
                    || t.type == ETokenType.BoolValue
                    || t.type == ETokenType.Null
                    || t.type == ETokenType.Const)
                {
                    Advance();
                    return new FileMetaConstValueTerm(m_FileMeta, t);
                }

                // 带括号的表达式 ( ... )
                if (t.type == ETokenType.LeftPar)
                {
                    // 收集完整的 () 片段并构造 FileMetaParTerm
                    int start = m_Index;
                    int depth = 0;
                    do
                    {
                        if (Current == null) break;
                        if (Current.type == ETokenType.LeftPar) depth++;
                        else if (Current.type == ETokenType.RightPar) depth--;
                        Advance();
                    } while (depth > 0 && m_Index < m_Tokens.Count);

                    var parTokens = m_Tokens.GetRange(start, m_Index - start);
                    return new FileMetaParTerm(m_FileMeta, parTokens, m_ExprType);
                }

                // 方括号 [ ... ]
                if (t.type == ETokenType.LeftBracket)
                {
                    int start = m_Index;
                    int depth = 0;
                    do
                    {
                        if (Current == null) break;
                        if (Current.type == ETokenType.LeftBracket) depth++;
                        else if (Current.type == ETokenType.RightBracket) depth--;
                        Advance();
                    } while (depth > 0 && m_Index < m_Tokens.Count);

                    var brTokens = m_Tokens.GetRange(start, m_Index - start);
                    return new FileMetaBracketTerm(m_FileMeta, brTokens, m_ExprType);
                }

                // 大括号 { ... }
                if (t.type == ETokenType.LeftBrace)
                {
                    int start = m_Index;
                    int depth = 0;
                    do
                    {
                        if (Current == null) break;
                        if (Current.type == ETokenType.LeftBrace) depth++;
                        else if (Current.type == ETokenType.RightBrace) depth--;
                        Advance();
                    } while (depth > 0 && m_Index < m_Tokens.Count);

                    var braceTokens = m_Tokens.GetRange(start, m_Index - start);
                    return new FileMetaBraceTerm(m_FileMeta, braceTokens);
                }

                // 标识符/调用/链式访问
                if (t.type == ETokenType.Identifier
                    || t.type == ETokenType.This
                    || t.type == ETokenType.Base
                    || t.type == ETokenType.New)
                {
                    var callTokens = new List<Token>();
                    while (Current != null &&
                           (Current.type == ETokenType.Identifier
                            || Current.type == ETokenType.This
                            || Current.type == ETokenType.Base
                            || Current.type == ETokenType.New
                            || Current.type == ETokenType.Period
                            || Current.type == ETokenType.LeftPar
                            || Current.type == ETokenType.RightPar
                            || Current.type == ETokenType.LeftBracket
                            || Current.type == ETokenType.RightBracket
                            || Current.type == ETokenType.Less
                            || Current.type == ETokenType.Greater
                            || Current.type == ETokenType.Comma))
                    {
                        callTokens.Add(Current);
                        Advance();
                        // 粗略终止条件：遇到分号/运算符/大括号等视为调用结束
                        if (Current == null ||
                            Current.type == ETokenType.SemiColon ||
                            IsSymbol(Current) ||
                            Current.type == ETokenType.LeftBrace ||
                            Current.type == ETokenType.RightBrace)
                        {
                            break;
                        }
                    }
                    return new FileMetaCallTerm(m_FileMeta, callTokens);
                }

                // as/is 表达式起始（假定前面已经被上层拆分好）
                if (t.type == ETokenType.As || t.type == ETokenType.Is)
                {
                    // 从当前位置到表达式结束全部交给 FileMetaAsOrIsTerm 解析
                    var remain = m_Tokens.GetRange(m_Index, m_Tokens.Count - m_Index);
                    m_Index = m_Tokens.Count;
                    return new FileMetaAsOrIsTerm(m_FileMeta, remain);
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
            return null;
        }
    }
}
