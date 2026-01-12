//****************************************************************************
//  File:      FileMetaUtil.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile.Grammer;
using SimpleLanguage.Logging;
using System.Collections.Generic;

namespace SimpleLanguage.Compile
{
    /// <summary>
    /// 统一的表达式处理工具类
    /// Token → Node 折叠和规范化 → FileMeta 创建的集中入口
    /// </summary>
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
 
            // ===== 无关键字前缀的普通语句：赋值 / 调用 / 复杂表达式 =====

            // 1) 检测简单形态的赋值语句：左侧是可调用/变量引用，右侧是表达式
            int assignIndex = -1;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].type == ETokenType.Assign)
                {
                    assignIndex = i;
                    break;
                }
            }

            if (assignIndex > 0)
            {
                var leftTokens = tokens.GetRange(0, assignIndex);
                var rightTokens = tokens.GetRange(assignIndex + 1, tokens.Count - assignIndex - 1);

                var leftExpr = CreateFileMetaExpressFromTokens(
                    fm,
                    leftTokens,
                    FileMetaTermExpress.EExpressType.Common);
                var rightExpr = CreateFileMetaExpressFromTokens(
                    fm,
                    rightTokens,
                    FileMetaTermExpress.EExpressType.Common);

                if (leftExpr is FileMetaCallTerm callTerm)
                {
                    // 将左侧调用视作变量引用，构造 FileMetaOpAssignSyntax
                    var opSyntax = new FileMetaOpAssignSyntax(
                        callTerm.callLink,
                        tokens[assignIndex],
                        null,
                        null,
                        null,
                        rightExpr,
                        flag: true);
                    return opSyntax;
                }
            }

            // 2) 其它情况，当作纯表达式/调用语句处理
            var exprOnly = CreateFileMetaExpressFromTokens(
                fm,
                tokens,
                FileMetaTermExpress.EExpressType.Common);

            if (exprOnly is FileMetaCallTerm callExpr)
            {
                return new FileMetaCallSyntax(callExpr.callLink);
            }

            // 其他复杂表达式暂不生成单独语句节点，交给后续流程处理
            return null;
        }
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
        // 统一的表达式入口：单 token 快速路径 + 多 token 使用 ParseBinaryExpression
        public static FileMetaBaseTerm CreateFileMetaExpressFromTokens(
             FileMeta fm,
             List<Token> tokens,
             FileMetaTermExpress.EExpressType eType)
         {
            if (tokens == null || tokens.Count == 0)
            {
                return null;
            }

            var cleaned = TrimTokenList(tokens);
            if (cleaned.Count == 0)
            {
                return null;
            }

            if (cleaned.Count == 1)
            {
                var t = cleaned[0];
                if (t.type == ETokenType.Number
                    || t.type == ETokenType.String
                    || t.type == ETokenType.BoolValue
                    || t.type == ETokenType.Null
                    || t.type == ETokenType.Const)
                {
                    return new FileMetaConstValueTerm(fm, t);
                }

                if (t.type == ETokenType.Identifier
                    || t.type == ETokenType.This
                    || t.type == ETokenType.Base
                    || t.type == ETokenType.New)
                {
                    return new FileMetaCallTerm(fm, cleaned);
                }
            }

            int index = 0;
            return ParseBinaryExpression(fm, cleaned, ref index, 0, eType);
        }
         // ====== 静态 Token 表达式解析函数（替代内部 TokenExpressionParser 类） ======
 
         private static FileMetaBaseTerm ParsePrimary(FileMeta fm, List<Token> tokens, ref int index, FileMetaTermExpress.EExpressType eType)
         {
            if (index >= tokens.Count) return null;
            var t = tokens[index];

            // 常量
            if (t.type == ETokenType.Number
                || t.type == ETokenType.String
                || t.type == ETokenType.BoolValue
                || t.type == ETokenType.Null
                || t.type == ETokenType.Const)
            {
                index++;
                return new FileMetaConstValueTerm(fm, t);
            }

            // 带括号的表达式 ( ... )
            if (t.type == ETokenType.LeftPar)
            {
                int start = index;
                int depth = 0;
                do
                {
                    if (index >= tokens.Count) break;
                    if (tokens[index].type == ETokenType.LeftPar) depth++;
                    else if (tokens[index].type == ETokenType.RightPar) depth--;
                    index++;
                } while (depth > 0 && index < tokens.Count);

                var parTokens = tokens.GetRange(start, index - start);
                return new FileMetaParTerm(fm, parTokens, eType);
            }

            // 方括号 [ ... ]
            if (t.type == ETokenType.LeftBracket)
            {
                int start = index;
                int depth = 0;
                do
                {
                    if (index >= tokens.Count) break;
                    if (tokens[index].type == ETokenType.LeftBracket) depth++;
                    else if (tokens[index].type == ETokenType.RightBracket) depth--;
                    index++;
                } while (depth > 0 && index < tokens.Count);

                var brTokens = tokens.GetRange(start, index - start);
                return new FileMetaBracketTerm(fm, brTokens, eType);
            }

            // 大括号 { ... }
            if (t.type == ETokenType.LeftBrace)
            {
                int start = index;
                int depth = 0;
                do
                {
                    if (index >= tokens.Count) break;
                    if (tokens[index].type == ETokenType.LeftBrace) depth++;
                    else if (tokens[index].type == ETokenType.RightBrace) depth--;
                    index++;
                } while (depth > 0 && index < tokens.Count);

                var braceTokens = tokens.GetRange(start, index - start);
                return new FileMetaBraceTerm(fm, braceTokens);
            }

            // 标识符/调用/链式访问；包括 this._metaClass 之类的链式引用
            if (t.type == ETokenType.Identifier
                || t.type == ETokenType.This
                || t.type == ETokenType.Base
                || t.type == ETokenType.New)
            {
                var callTokens = new List<Token>();
                while (index < tokens.Count)
                {
                    var cur = tokens[index];
                    if (cur.type == ETokenType.Identifier
                        || cur.type == ETokenType.This
                        || cur.type == ETokenType.Base
                        || cur.type == ETokenType.New
                        || cur.type == ETokenType.Period
                        || cur.type == ETokenType.LeftPar
                        || cur.type == ETokenType.RightPar
                        || cur.type == ETokenType.LeftBracket
                        || cur.type == ETokenType.RightBracket
                        || cur.type == ETokenType.Less
                        || cur.type == ETokenType.Greater
                        || cur.type == ETokenType.Comma)
                    {
                        callTokens.Add(cur);
                        index++;

                        if (index >= tokens.Count)
                            break;

                        var next = tokens[index];
                        if (next.type == ETokenType.SemiColon
                            || next.type == ETokenType.LeftBrace
                            || next.type == ETokenType.RightBrace)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                return new FileMetaCallTerm(fm, callTokens);
            }

            // as/is 表达式起始
            if (t.type == ETokenType.As || t.type == ETokenType.Is)
            {
                var remain = tokens.GetRange(index, tokens.Count - index);
                index = tokens.Count;
                return new FileMetaAsOrIsTerm(fm, remain);
            }

            return null;
        }

        private static FileMetaBaseTerm ParseBinaryExpression(FileMeta fm, List<Token> tokens, ref int index, int parentPrecedence, FileMetaTermExpress.EExpressType eType)
        {
            var left = ParsePrimary(fm, tokens, ref index, eType);
            while (true)
            {
                if (index >= tokens.Count) break;
                var op = tokens[index];
                int prec = GetPrecedence(op);
                if (prec <= parentPrecedence || prec == 0)
                    break;

                // 跳过运算符
                index++;
                var right = ParsePrimary(fm, tokens, ref index, eType);
                if (right == null)
                    break;
                // 目前保留最小结构，返回左侧表达式；后续可在此处构造二元表达式树
                left = left ?? right;
            }
            return left;
        }

        private static int GetPrecedence(Token op)
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
    }
}
