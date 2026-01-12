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
using System.Diagnostics;
using System.Text;

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

            // label 语句：labelName:
            if (first.type == ETokenType.Identifier && tokens.Count >= 2 && tokens[1].type == ETokenType.Colon)
            {
                // 假定存在 FileMetaLabelSyntax(FileMeta fm, Token nameToken)
                return new FileMetaKeyGotoLabelSyntax(fm, first, null);
            }

            // goto 语句：goto labelName;
            if (first.type == ETokenType.Goto && tokens.Count >= 2)
            {
                var targetToken = tokens[1];
                // 假定存在 FileMetaGotoSyntax(FileMeta fm, Token gotoToken, Token labelToken)
                return new FileMetaKeyGotoLabelSyntax(fm, first, targetToken);
            }

            // break / continue / next / try / catch / throw 等关键字语句
            if (first.type == ETokenType.Break)
            {
                return new FileMetaKeyOnlySyntax(fm, first, null);
            }
            if (first.type == ETokenType.Continue || first.type == ETokenType.Next)
            {
                return new FileMetaKeyOnlySyntax(fm, first, null);
            }
            //if (first.type == ETokenType.Throw)
            //{
            //    var exprTokens = tokens.Count > 1 ? tokens.GetRange(1, tokens.Count - 1) : new List<Token>();
            //    var expr = CreateFileMetaExpressFromTokens(fm, exprTokens, FileMetaTermExpress.EExpressType.Common);
            //    return new FileMetaKeyThrowSyntax(fm, first, expr);
            //}

            // if / while / dowhile / for / switch 语句

            // 当前仅提供最小骨架：识别关键字，并构造对应的语法节点壳，“这种语法”能否通过编译还要看后续完整性检查
            if (first.type == ETokenType.If)
            {
                // if (cond) { } 条件=整个 tokens[1..]，实际 block 由 TokenToFileMeta 的 {} 解析统一维护
                var condTokens = tokens.GetRange(1, tokens.Count - 1);
                var cond = CreateFileMetaExpressFromTokens(
                    fm,
                    condTokens,
                    FileMetaTermExpress.EExpressType.Common);
                // 此处不再构造占位 block，让 TokenToFileMeta 在看到后续 '{ }' 时统一创建 FileMetaBlockSyntax
                var ifCond = new FileMetaConditionExpressSyntax(fm, first, cond, null);
                var ifSyntax = new FileMetaKeyIfSyntax(fm);
                ifSyntax.SetFileMetaConditionExpressSyntax(ifCond);
                return ifSyntax;
            }
            else if (first.type == ETokenType.ElseIf)
            {
                // else if (cond) { }：本身是一个条件表达式语句块，后续由 MetaIfStatements 绑定到前一个 if
                var condTokens = tokens.GetRange(1, tokens.Count - 1);
                var cond = CreateFileMetaExpressFromTokens(
                    fm,
                    condTokens,
                    FileMetaTermExpress.EExpressType.Common);
                return new FileMetaConditionExpressSyntax(fm, first, cond, null);
            }
            else if (first.type == ETokenType.Else)
            {
                // else { }：仅关键字壳，真正的块由 TokenToFileMeta 构造的 FileMetaBlockSyntax 提供
                return new FileMetaKeyOnlySyntax(fm, first, null);
            }

            if (first.type == ETokenType.While)
            {
                // while (cond) { } 条件表达式由 tokens[1..] 提供，{} 块由 TokenToFileMeta 统一处理
                var condTokens = tokens.GetRange(1, tokens.Count - 1);
                var cond = CreateFileMetaExpressFromTokens(
                    fm,
                    condTokens,
                    FileMetaTermExpress.EExpressType.Common);
                return new FileMetaConditionExpressSyntax(fm, first, cond, null);
            }

            if (first.type == ETokenType.DoWhile)
            {
                // do..while：这里仅返回关键字语法壳，实际块和 while 条件由 TokenToFileMeta 在 {} 和后续 while 语句中统一挂接
                return new FileMetaKeyOnlySyntax(fm, first, null);
            }

            if (first.type == ETokenType.For)
            {
                // for 语句：当前不拆分 init/cond/step，全部作为条件表达式处理；{} 块由 TokenToFileMeta 统一处理
                var bodyTokens = tokens.GetRange(1, tokens.Count - 1);
                var expr = CreateFileMetaExpressFromTokens(
                    fm,
                    bodyTokens,
                    FileMetaTermExpress.EExpressType.Common);
                var forSyntax = new FileMetaKeyForSyntax(fm, first, null);
                if (expr != null)
                {
                    forSyntax.SetConditionExpress(expr);
                }
                return forSyntax;
            }

            if (first.type == ETokenType.Switch)
            {
                // switch 语句：switch 后面的表达式作为变量引用，case/default 及 {} 块仍由 TokenToFileMeta/旧 Node 流程解析；这里仅占位
                var exprTokens = tokens.GetRange(1, tokens.Count - 1);
                var expr = CreateFileMetaExpressFromTokens(
                    fm,
                    exprTokens,
                    FileMetaTermExpress.EExpressType.Common);
                // 暂时无法直接构造 FileMetaKeySwitchSyntax（需要 FileMetaCallLink 和 case block 信息），
                // 因此返回 null，交由旧 Node 流或后续扩展处理。
                return null;
            }
 
            // ===== 无关键字前缀的普通语句：变量定义 / 赋值 / 调用 / 复杂表达式 =====

            // 1) 检测包含 '=' 的语句，区分：
            //    - FileMetaDefineVariableSyntax:  可能带 var/dynamic/data/static/类型前缀
            //    - FileMetaOpAssignSyntax:       纯变量引用在左侧
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

                // 左侧可能形态：
                //   [static] [TypeTokens...] Name
                //   var Name
                //   dynamic Name
                //   data Name
                //   objectRef.prop[index]   (赋值)
                
                // 先定位最后一个 Identifier 作为候选 Name
                int lastIdIndex = -1;
                for (int i = 0; i < leftTokens.Count; i++)
                {
                    if (leftTokens[i].type == ETokenType.Identifier)
                    {
                        lastIdIndex = i;
                    }
                }

                if (lastIdIndex >= 0)
                {
                    Token nameToken = leftTokens[lastIdIndex];

                    // 检查左侧前缀是否包含 var/dynamic/data/static/type，若有则视为定义语句
                    bool hasDefinePrefix = false;
                    Token dynamicToken = null;
                    Token dataToken = null;
                    Token varToken = null;
                    Token staticToken = null;
                    List<Token> typeTokens = new List<Token>();

                    for (int i = 0; i < lastIdIndex; i++)
                    {
                        var t = leftTokens[i];
                        if (t.type == ETokenType.Static)
                        {
                            staticToken = t;
                            hasDefinePrefix = true;
                        }
                        else if (t.type == ETokenType.Dynamic)
                        {
                            dynamicToken = t;
                            hasDefinePrefix = true;
                        }
                        else if (t.type == ETokenType.Data)
                        {
                            dataToken = t;
                            hasDefinePrefix = true;
                        }
                        else if (t.type == ETokenType.Var)
                        {
                            varToken = t;
                            hasDefinePrefix = true;
                        }
                        else if (t.type == ETokenType.Type)
                        {
                            typeTokens.Add(t);
                            hasDefinePrefix = true;
                        }
                        else if (t.type == ETokenType.Identifier)
                        {
                            // 作为类型前缀的一部分，例如 NS.ClassName 之类
                            typeTokens.Add(t);
                        }
                    }

                    var rightExpr = CreateFileMetaExpressFromTokens(
                        fm,
                        rightTokens,
                        FileMetaTermExpress.EExpressType.MemberVariable);

                    if (hasDefinePrefix)
                    {
                        // 变量定义形式：FileMetaDefineVariableSyntax 或 带 var/dynamic/data 的 FileMetaOpAssignSyntax(hasDefine)
                        FileMetaClassDefine classDefine = null;
                        if (typeTokens.Count > 0)
                        {
                            classDefine = new FileMetaClassDefine(fm, typeTokens);
                        }

                        // 如果存在显式类型或 static，则用 FileMetaDefineVariableSyntax 表达
                        if (classDefine != null || staticToken != null)
                        {
                            return new FileMetaDefineVariableSyntax(
                                fm,
                                classDefine,
                                nameToken,
                                tokens[assignIndex],
                                staticToken,
                                rightExpr);
                        }

                        // 否则用 hasDefine = true 的 FileMetaOpAssignSyntax 表达（var/dynamic/data）
                        var leftCall = CreateFileMetaExpressFromTokens(
                            fm,
                            new List<Token> { nameToken },
                            FileMetaTermExpress.EExpressType.Common) as FileMetaCallTerm;

                        if (leftCall != null)
                        {
                            return new FileMetaOpAssignSyntax(
                                leftCall.callLink,
                                tokens[assignIndex],
                                dynamicToken,
                                dataToken,
                                varToken,
                                rightExpr,
                                flag: true);
                        }
                    }
                    else
                    {
                        // 无定义前缀：视为普通赋值/更新表达式，左侧整体按调用/变量引用解析
                        var leftExpr = CreateFileMetaExpressFromTokens(
                            fm,
                            leftTokens,
                            FileMetaTermExpress.EExpressType.Common);

                        if (leftExpr is FileMetaCallTerm callTerm)
                        {
                            return new FileMetaOpAssignSyntax(
                                callTerm.callLink,
                                tokens[assignIndex],
                                null,
                                null,
                                null,
                                rightExpr,
                                flag: true);
                        }
                    }
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

            // 针对不同表达式类型做轻微差异化处理：
            // - MemberVariable/ParamVariable: 不允许出现分号/多条语句，仅视为单一表达式；
            // - Common: 语句内部的通用表达式。
            if (eType == FileMetaTermExpress.EExpressType.MemberVariable
                || eType == FileMetaTermExpress.EExpressType.ParamVariable)
            {
                // 简单防御：如果中间含有分号，截断为第一部分，避免把多句当成一个初始值/默认值
                int semiIndex = cleaned.FindIndex(t => t.type == ETokenType.SemiColon);
                if (semiIndex > 0)
                {
                    cleaned = cleaned.GetRange(0, semiIndex);
                }
            }

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

            // 支持一条 token 序列中包含多个顶层表达式片段：
            // 例如：a > b && c < d && e == f
            // 这里不将所有运算符折叠成一棵树，而是拆成若干 FileMetaBaseTerm 片段，
            // 若数量>1，则用 FileMetaTermExpress 作为容器，把这些片段依次加入 m_FileMetaExpressList；
            // 若数量==1，则直接返回该片段。

            var segmentList = new List<FileMetaBaseTerm>();
            int index = 0;
            while (index < cleaned.Count)
            {
                var term = ParseBinaryExpression(fm, cleaned, ref index, 0, eType);
                if (term == null)
                {
                    break;
                }
                segmentList.AddRange(term);

                // 防御性：避免死循环
                int safeIndex = index;
                while (safeIndex < cleaned.Count &&
                       (cleaned[safeIndex].type == ETokenType.Space ||
                        cleaned[safeIndex].type == ETokenType.LineEnd ||
                        cleaned[safeIndex].type == ETokenType.SemiColon))
                {
                    safeIndex++;
                }
                if (safeIndex == index)
                {
                    // 当前无法前进，跳出
                    break;
                }
                index = safeIndex;
            }

            if (segmentList.Count == 0)
            {
                return null;
            }

            if (segmentList.Count == 1)
            {
                var single = segmentList[0];
                single.BuildAST();
                return single;
            }

            // 多个表达式片段：使用 FileMetaTermExpress 作为容器
            var termExpress = new FileMetaTermExpress(fm, new List<Token>(), eType);
            termExpress.AddRangeFileMetaTerm(segmentList);
            termExpress.BuildAST();
            return termExpress;
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

            // (...)  / [...] / {...} / 调用 / as-is ...
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

            // 方括号 [ ... ]，数组访问或数组常量
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

            // 大括号 { ... }，常量初始化块或复合字面量
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

            // as/is 表达式起始：三种模式下都允许出现，例如参数默认值/成员默认值中的 as/is
            if (t.type == ETokenType.As || t.type == ETokenType.Is)
            {
                var remain = tokens.GetRange(index, tokens.Count - index);
                index = tokens.Count;
                return new FileMetaAsOrIsTerm(fm, remain);
            }

            return null;
        }

        private static List<FileMetaBaseTerm> ParseBinaryExpression(FileMeta fm, List<Token> tokens, ref int index, int parentPrecedence, FileMetaTermExpress.EExpressType eType)
        {
            var result = new List<FileMetaBaseTerm>();

            while (index < tokens.Count)
            {
                // 左操作数或单元表达式
                var left = ParsePrimary(fm, tokens, ref index, eType);
                if (left == null)
                {
                    break;
                }
                result.Add(left);

                if (index >= tokens.Count)
                {
                    break;
                }

                var op = tokens[index];
                int prec = GetPrecedenceConsideringContext(tokens, index);
                if (prec <= parentPrecedence || prec == 0)
                {
                    // 非二元运算符，结束本段解析
                    break;
                }

                // 运算符自身作为一个 FileMetaSymbolTerm 片段加入列表
                var opNode = new FileMetaSymbolTerm(fm, op);
                result.Add(opNode);

                // 消费运算符 token
                index++;
            }

            return result;
        }

         // 上下文敏感的优先级获取：当 < / > 处于泛型类型参数上下文中时，视为 0 优先级（非比较运算符）
         private static int GetPrecedenceConsideringContext(List<Token> tokens, int opIndex)
        {
            if (opIndex < 0 || opIndex >= tokens.Count)
                return 0;

            var op = tokens[opIndex];
            // 仅对 < / > 做上下文检查，其他操作符直接返回优先级
            if (op.type != ETokenType.Less && op.type != ETokenType.Greater)
            {
                return GetPrecedence(op);
            }

            int angleDepth = 0;
            for (int i = 0; i < opIndex; i++)
            {
                if (tokens[i].type == ETokenType.Less)
                {
                    angleDepth++;
                }
                else if (tokens[i].type == ETokenType.Greater && angleDepth > 0)
                {
                    angleDepth--;
                }
            }

            // 如果当前 < / > 还在泛型尖括号上下文中，则认为它属于类型参数，而不是比较运算符
            if (angleDepth > 0)
            {
                return 0;
            }

            return GetPrecedence(op);
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
            while (start <= end && (list[start].type == ETokenType.Sharp || list[start].type == ETokenType.LineEnd || list[start].type == ETokenType.SemiColon))
                start++;
            while (end >= start && (list[end].type == ETokenType.Sharp || list[end].type == ETokenType.LineEnd || list[end].type == ETokenType.SemiColon))
                end--;
            if (end < start) return new List<Token>();
            return list.GetRange(start, end - start + 1);
        }
    }
}
