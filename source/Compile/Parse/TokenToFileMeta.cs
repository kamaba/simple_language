//****************************************************************************
//  File:      TokenToFileMeta.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/01/15 12:00:00
//  Description: 直接从 Token 转换为 FileMeta 结构，不生成中间 Node 树
//               Token → FileMeta 的直接转换器
//****************************************************************************

using SimpleLanguage;
using SimpleLanguage.Compile;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleLanguage.Compile
{
    public class TokenToFileMeta
    {
        private enum DFAState { Initial, InClass, InFunction, InBlock, InExpression, InImport, InNamespace }

        private class ParseContext
        {
            public DFAState currentState = DFAState.Initial;
            public Stack<DFAState> stateStack = new Stack<DFAState>();
            public int braceDepth = 0;
            public int parenDepth = 0;
            public int bracketDepth = 0;
        }

        private FileMeta m_FileMeta;
        private List<Token> m_TokenList;
        private int m_TokenIndex = 0;
        private ParseContext m_Context;

        public TokenToFileMeta(FileMeta fm, List<Token> tokenList)
        {
            m_FileMeta = fm;
            m_TokenList = tokenList ?? new List<Token>();
            m_TokenIndex = 0;
            m_Context = new ParseContext();
        }

        public void ParseTokensToFileMeta()
        {
            try
            {
                TransitionState(DFAState.Initial);
                ParseCompilationUnit();
            }
            catch (Exception ex)
            {
                Log.AddInStructFileMeta(EError.None, $"TokenToFileMeta 解析错误: {ex.Message}");
            }
        }

        private void ParseCompilationUnit()
        {
            while (m_TokenIndex < m_TokenList.Count)
            {
                Token token = CurrentToken;
                if (token == null || token.type == ETokenType.Finished) break;

                if (token.type == ETokenType.LineEnd || token.type == ETokenType.Space || token.type == ETokenType.SemiColon)
                {
                    Consume();
                    continue;
                }

                if (token.type == ETokenType.Import) ParseImportDirective();
                else if (token.type == ETokenType.Namespace) ParseNamespaceDeclaration();
                else if (IsClassDeclarationStart(token)) ParseClassDeclaration();
                else Consume();
            }
        }

        private void ParseImportDirective()
        {
            TransitionState(DFAState.InImport);
            if (!Match(ETokenType.Import)) return;

            Token importToken = Consume();
            List<Token> importPath = ParseQualifiedName();
            List<Token> allTokens = new List<Token>() { importToken };
            allTokens.AddRange(importPath);

            if (Match(ETokenType.SemiColon)) allTokens.Add(Consume());

            if (importPath.Count > 0) m_FileMeta.AddFileImportSyntaxFromTokens(allTokens);
            TransitionState(DFAState.Initial);
        }

        private void ParseNamespaceDeclaration()
        {
            TransitionState(DFAState.InNamespace);
            if (!Match(ETokenType.Namespace)) return;

            Token nsToken = Consume();
            List<Token> namespacePath = ParseQualifiedName();

            // 这里只将命名空间路径（标识符部分）传入 FileMeta，不包含 namespace 关键字本身
            if (Match(ETokenType.SemiColon)) Consume();
            else if (Match(ETokenType.LeftBrace)) { /* 块处理简化 */ }

            if (namespacePath.Count > 0) m_FileMeta.AddFileNamespaceFromTokens(namespacePath);
            TransitionState(DFAState.Initial);
        }

        private void ParseClassDeclaration()
        {
            TransitionState(DFAState.InClass);
            List<Token> classModifiers = ParseModifiers();
            Token classKeyword = null;

            if (MatchAny(SimpleLanguage.ETokenType.Class, SimpleLanguage.ETokenType.Interface, SimpleLanguage.ETokenType.Enum, SimpleLanguage.ETokenType.Data)) classKeyword = Consume();
            else { TransitionState(DFAState.Initial); return; }

            // 支持多段类名：ClassP1.ClassC1.ClassC2
            List<Token> classNameTokens = new List<Token>();
            // 这里不仅接受 Identifier，还接受被词法阶段标记为 Type 的内建类型名
            // 例如：object/int/string 等在 LexerParse.ReadIdentifier 中被解析为 ETokenType.Type
            if (Match(SimpleLanguage.ETokenType.Identifier) || Match(SimpleLanguage.ETokenType.Type))
            {
                classNameTokens.Add(Consume());
                // 后续若存在 .Name 形式的多段类名，一并收集
                while (Match(SimpleLanguage.ETokenType.Period))
                {
                    Consume();
                    if (Match(SimpleLanguage.ETokenType.Identifier) || Match(SimpleLanguage.ETokenType.Type))
                    {
                        classNameTokens.Add(Consume());
                    }
                    else
                    {
                        Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 多段类名解析错误: '.' 后缺少标识符");
                        break;
                    }
                }
            }
            else
            {
                // 如果没有显式标识符，记录错误并构造一个占位符名称，避免后续空引用
                Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析类型名称错误: 缺少标识符");
                classNameTokens.Add(new Token(m_FileMeta.path, SimpleLanguage.ETokenType.Identifier, "<anonymous>", 0, 0));
            }

            List<Token> typeParameters = new List<Token>();
            if (Match(SimpleLanguage.ETokenType.Less)) typeParameters = ParseTypeParameters();

            Token extendsKeyword = null;
            List<Token> baseClass = new List<Token>();
            if (Match(SimpleLanguage.ETokenType.Extends))
            {
                extendsKeyword = Consume();
                baseClass = ParseQualifiedName();
            }

            Token interfaceKeyword = null;
            List<List<Token>> interfaceList = new List<List<Token>>();
            if (Match(SimpleLanguage.ETokenType.Interface))
            {
                interfaceKeyword = Consume();
                interfaceList = ParseInterfaceList();
            }

            // 将各部分拆分后的 Token 列表分别传入 FileMetaClass，避免在此处重新拼接
            FileMetaClass fmc = new FileMetaClass(
                m_FileMeta,
                classModifiers,
                classKeyword,
                classNameTokens,
                typeParameters,
                extendsKeyword,
                baseClass,
                interfaceKeyword,
                interfaceList);
             m_FileMeta.AddFileClassFromTokens(new List<Token>());
             m_FileMeta.AddFileMetaClass(fmc);

            if (Match(ETokenType.LineEnd))
            {
                Consume();
            }
            if( Match(ETokenType.LeftBrace))
            {
                ParseClassBody(fmc);
            }
            TransitionState(DFAState.Initial);
        }

        private void ParseClassBody(FileMetaClass fmc)
        {
            if (!Match(ETokenType.LeftBrace)) return;
            // consume '{' starting the class body

            Token leftBraceToken = m_TokenList[m_TokenIndex++];

            m_Context.braceDepth++;

            // Collect tokens for the body until matching '}'
            var bodyTokens = new List<Token>();
            int depth = 1;
            while (m_TokenIndex < m_TokenList.Count && depth > 0)
            {
                var t = CurrentToken;
                if (t.type == SimpleLanguage.ETokenType.LeftBrace)
                {
                    depth++;
                    bodyTokens.Add(Consume());
                }
                else if (t.type == SimpleLanguage.ETokenType.RightBrace)
                {
                    depth--;
                    if (depth > 0)
                    {
                        bodyTokens.Add(Consume());
                    }
                    else
                    {
                        // consume the closing '}' for this class body
                        Consume();
                        break;
                    }
                }
                else
                {
                    bodyTokens.Add(Consume());
                }
            }

            m_Context.braceDepth--;

            // Now parse members from bodyTokens using a simple top-level splitter
            if (bodyTokens.Count == 0)
            {
                Debug.Assert(false, "使用类的情况 要在类中必须 有{}这样的范围限定!");
                return;
            }

            int index = 0;
            int parenDepth = 0;
            int braceDepth = 0;
            int bracketDepth = 0;
            var currentMember = new List<Token>();

            void flushCurrent()
            {
                // trim leading/trailing spaces/lineEnds
                int start = 0;
                int end = currentMember.Count - 1;
                while (start <= end && (currentMember[start].type == ETokenType.Space || currentMember[start].type == ETokenType.LineEnd || currentMember[start].type == ETokenType.SemiColon))
                    start++;
                while (end >= start && (currentMember[end].type == ETokenType.Space || currentMember[end].type == ETokenType.LineEnd || currentMember[end].type == ETokenType.SemiColon))
                    end--;
                if (end < start) { currentMember.Clear(); return; }

                var memberTokens = currentMember.GetRange(start, end - start + 1);
                // temporarily switch token stream to this member and reuse ParseClassMember
                var oldList = m_TokenList;
                int oldIndex = m_TokenIndex;
                m_TokenList = memberTokens;
                m_TokenIndex = 0;
                try
                {
                    ParseClassMember(fmc);
                }
                catch (Exception ex)
                {
                    Log.AddInStructFileMeta(EError.None, $"ParseClassBody 成员解析异常: {ex.Message}");
                }
                finally
                {
                    m_TokenList = oldList;
                    m_TokenIndex = oldIndex;
                }

                currentMember.Clear();
            }

            while (index < bodyTokens.Count)
            {
                var t = bodyTokens[index++];
                currentMember.Add(t);

                if (t.type == ETokenType.LeftPar) parenDepth++;
                else if (t.type == ETokenType.RightPar && parenDepth > 0) parenDepth--;
                else if (t.type == ETokenType.LeftBrace) braceDepth++;
                else if (t.type == ETokenType.RightBrace && braceDepth > 0) braceDepth--;
                else if (t.type == ETokenType.LeftBracket) bracketDepth++;
                else if (t.type == ETokenType.RightBracket && bracketDepth > 0) bracketDepth--;

                // At top level inside class (not inside any (), {}, []), a semicolon or line end ends a member
                if (parenDepth == 0 && braceDepth == 0 && bracketDepth == 0 &&
                    (t.type == ETokenType.SemiColon || t.type == ETokenType.LineEnd))
                {
                    flushCurrent();
                }
                // Or a complete function/property body ending with '}' at top level
                else if (parenDepth == 0 && braceDepth == 0 && bracketDepth == 0 && t.type == ETokenType.RightBrace)
                {
                    flushCurrent();
                }
            }

            // flush any remaining tokens that didn't end with ';' or '}'
            if (currentMember.Count > 0)
            {
                flushCurrent();
            }
        }

        private void ParseClassMember(FileMetaClass fmc)
        {
             List<Token> modifiers = ParseModifiers();
            
            if (MatchAny(ETokenType.Class, ETokenType.Enum, ETokenType.Interface, ETokenType.Data))
            {
                Log.AddInStructFileMeta(EError.None, "暂不支持嵌套类解析");
                SkipClassBody(); 
                return;
            }

            // 1. 首先收集一行内的所有 token（直到行结束或分号），然后在该行内部做一次性判定
            var lineTokens = new List<Token>();
            while (m_TokenIndex < m_TokenList.Count &&
                   !Match(ETokenType.LineEnd) && !Match(ETokenType.SemiColon))
            {
                lineTokens.Add(Consume());
            }

            // 吃掉结束符，但不放入 lineTokens
            if (Match(ETokenType.SemiColon) || Match(ETokenType.LineEnd))
            {
                Consume();
            }

            if (lineTokens.Count == 0)
                return;

            // 跳过前导空白
            int firstIndex = 0;
            while (firstIndex < lineTokens.Count &&
                   (lineTokens[firstIndex].type == ETokenType.Space || lineTokens[firstIndex].type == ETokenType.LineEnd))
            {
                firstIndex++;
            }
            if (firstIndex >= lineTokens.Count)
                return;

            Token testToken = lineTokens[0];
            int line = testToken.sourceBeginLine;

            // 如果整行以 if/for/while/return/switch 等语句关键字开头，说明这不是类成员声明，而是误切到函数体里的语句，直接返回
            var firstType = lineTokens[firstIndex].type;
            if (firstType == ETokenType.If || firstType == ETokenType.For || firstType == ETokenType.While
                || firstType == ETokenType.Return || firstType == ETokenType.Switch)
            {
                return;
            }

            // 2. 提取复杂类型前缀和成员名：
            //    使用统一的帮助方法，从一行 token 中（在第一个 '(' 之前）拆出类型 token 列表和 nameToken。
            List<Token> typeTokens;
            Token nameToken;
            if (!GetTypeAndNameTokens(lineTokens, out typeTokens, out nameToken))
            {
                // 无法识别出合法的成员名，直接返回
                return;
            }

            // 3. 判定函数或变量：仅根据 nameToken 后是否紧跟 '('
            int nameEndIndex = -1;
            for (int i = 0; i < lineTokens.Count; i++)
            {
                if (ReferenceEquals(lineTokens[i], nameToken))
                {
                    nameEndIndex = i;
                    break;
                }
            }
            if (nameEndIndex == -1)
                return;

            int afterName = nameEndIndex + 1;
            while (afterName < lineTokens.Count &&
                   (lineTokens[afterName].type == ETokenType.Space || lineTokens[afterName].type == ETokenType.LineEnd))
            {
                afterName++;
            }

            bool isFunction = afterName < lineTokens.Count && lineTokens[afterName].type == ETokenType.LeftPar;

            if (isFunction)
            {
                // 函数：这里仅负责拆分：修饰符 / 返回类型 token / 名称 token / 参数部分 token / 可选函数体 block token
                // 具体的类型定义和参数解析逻辑在 FileMetaMemberFunction 内部完成。

                // 提取参数部分：从 '(' 开始到匹配的 ')' 结束
                int parStart = afterName; // 指向 '('
                int parDepth = 0;
                int parEnd = -1;
                for (int i = parStart; i < lineTokens.Count; i++)
                {
                    var t = lineTokens[i];
                    if (t.type == ETokenType.LeftPar)
                    {
                        parDepth++;
                    }
                    else if (t.type == ETokenType.RightPar)
                    {
                        parDepth--;
                        if (parDepth == 0)
                        {
                            parEnd = i;
                            break;
                        }
                    }
                }

                List<Token> paramTokens = null;
                if (parEnd >= parStart)
                {
                    paramTokens = lineTokens.GetRange(parStart, parEnd - parStart + 1);
                }

                // 提取函数体 block（如果存在）：在参数列表之后查找 '{' 开始的块
                List<Token> blockTokens = null;
                int bodyStart = parEnd + 1;
                while (bodyStart < lineTokens.Count &&
                       (lineTokens[bodyStart].type == ETokenType.Space || lineTokens[bodyStart].type == ETokenType.LineEnd))
                {
                    bodyStart++;
                }
                if (bodyStart < lineTokens.Count && lineTokens[bodyStart].type == ETokenType.LeftBrace)
                {
                    int braceDepth = 0;
                    int bodyEnd = -1;
                    for (int i = bodyStart; i < lineTokens.Count; i++)
                    {
                        var t = lineTokens[i];
                        if (t.type == ETokenType.LeftBrace)
                        {
                            braceDepth++;
                        }
                        else if (t.type == ETokenType.RightBrace)
                        {
                            braceDepth--;
                            if (braceDepth == 0)
                            {
                                bodyEnd = i;
                                break;
                            }
                        }
                    }

                    if (bodyEnd >= bodyStart)
                    {
                        blockTokens = lineTokens.GetRange(bodyStart, bodyEnd - bodyStart + 1);
                    }
                }

                FileMetaMemberFunction fmmf = new FileMetaMemberFunction(
                    m_FileMeta,
                    modifiers,
                    typeTokens,
                    nameToken,
                    paramTokens,
                    blockTokens);

                fmc.AddFileMemberFunction(fmmf);
            }
             else 
             {
                 // 变量/字段：类型 + 名称 + 可能的初始化表达式，交给 FileMetaMemberVariable 解析
                 List<Token> varTokens = new List<Token>();
                 varTokens.AddRange(modifiers);
                 varTokens.AddRange(lineTokens);
 
                if (varTokens.Count > 0)
                {
                    FileMetaMemberVariable fmmv = new FileMetaMemberVariable(m_FileMeta, varTokens);
                    fmc.AddFileMemberVariable(fmmv);
                }
             }
         }
        /// <summary>
        /// 从一行成员声明 token 中提取类型前缀 token 列表和成员名 token：
        /// 仅在第一个 '(' 之前参与识别，避免把参数列表里的标识符当成成员名。
        /// 规则：
        ///  - 如果 '(' 之前只有一个 Identifier，则该标识符为 name（如: _init_()）。
        ///  - 如果有多个 Identifier，则最后一个为 name，其前面的所有 token 视为类型部分
        ///    （支持复杂泛型/命名空间/数组，如 List<Map<NS.ClassName, Set<Core.String>>>[][][] name）。
        /// </summary>
        private bool GetTypeAndNameTokens(List<Token> lineTokens, out List<Token> typeTokens, out Token nameToken )
        {
            typeTokens = new List<Token>();
            nameToken = null;

            if (lineTokens == null || lineTokens.Count == 0)
                return false;

            // 找到第一个 '('
            int parIndex = -1;
            for (int i = 0; i < lineTokens.Count; i++)
            {
                if (lineTokens[i].type == ETokenType.LeftPar)
                {
                    parIndex = i;
                    break;
                }
            }

            int searchEnd = parIndex >= 0 ? parIndex : lineTokens.Count;

            int firstIdIndex = -1;
            int lastIdIndex = -1;
            int idCount = 0;

            for (int i = 0; i < searchEnd; i++)
            {
                if (lineTokens[i].type == ETokenType.Identifier)
                {
                    if (firstIdIndex == -1)
                    {
                        firstIdIndex = i;
                    }
                    lastIdIndex = i;
                    idCount++;
                }
            }

            if (idCount == 0)
            {
                // 整行没有标识符，无法作为成员声明
                return false;
            }

            if (idCount == 1)
            {
                // 只有一个 Identifier： `_init_()` / `_init_(){}` 之类 —— 该标识符就是成员名
                nameToken = lineTokens[firstIdIndex];
            }
            else
            {
                // 存在多个 Identifier：最后一个 Identifier（在 '(' 之前）作为 nameToken，其前面的全部视为类型部分
                if (lastIdIndex > 0)
                {
                    typeTokens.AddRange(lineTokens.GetRange(0, lastIdIndex));
                }
                nameToken = lineTokens[lastIdIndex];
            }

            return nameToken != null;
        }
        private List<Token> ParseParameters()
        {
             List<Token> tokens = new List<Token>();
             if (Match(ETokenType.LeftPar))
             {
                 m_Context.parenDepth++;
                 tokens.Add(Consume());
                 while (m_Context.parenDepth > 0 && m_TokenIndex < m_TokenList.Count)
                 {
                     Token t = CurrentToken;
                     if (t.type == SimpleLanguage.ETokenType.LeftPar) m_Context.parenDepth++;
                     else if (t.type == SimpleLanguage.ETokenType.RightPar) 
                     {
                         m_Context.parenDepth--;
                         if (m_Context.parenDepth == 0) { tokens.Add(Consume()); break; }
                     }
                     tokens.Add(Consume());
                 }
             }
             return tokens;
        }

        private List<Token> ParseBlockTokens()
        {
              List<Token> tokens = new List<Token>();
              if (Match(ETokenType.LeftBrace))
              {
                   // include opening '{' token
                   int depth = 1;
                   var left = Consume();
                   tokens.Add(left);
                   while (depth > 0 && m_TokenIndex < m_TokenList.Count)
                   {
                        Token t = CurrentToken;
                        if (t.type == SimpleLanguage.ETokenType.LeftBrace)
                        {
                            depth++;
                            tokens.Add(Consume());
                        }
                        else if (t.type == SimpleLanguage.ETokenType.RightBrace)
                        {
                            depth--;
                            // always include closing '}' token belonging to this block
                            var right = Consume();
                            tokens.Add(right);
                            if (depth == 0)
                            {
                                break;
                            }
                        }
                        else
                        {
                            tokens.Add(Consume());
                        }
                   }
              }
              return tokens;
        }

        private List<Token> ParseModifiers()
        {
            List<Token> modifiers = new List<Token>();
            while (MatchAny(SimpleLanguage.ETokenType.Public, SimpleLanguage.ETokenType.Private, SimpleLanguage.ETokenType.Projected, SimpleLanguage.ETokenType.Static, SimpleLanguage.ETokenType.Final, SimpleLanguage.ETokenType.Const, SimpleLanguage.ETokenType.Partial))
                modifiers.Add(Consume());
            return modifiers;
        }

        private List<Token> ParseQualifiedName()
        {   
            List<Token> names = new List<Token>();
            while (m_TokenIndex < m_TokenList.Count)
            {
                Token current = CurrentToken;
                if (current == null) break;
                if (current.type == ETokenType.Identifier || current.type == ETokenType.Type) names.Add(Consume());
                else if (current.type == ETokenType.Period) { Consume(); continue; }
                else break;
            }
            return names;
        }

        private List<Token> ParseTypeParameters()
        {
            // 统一的泛型参数/类型参数解析：从当前的 '<' 开始，到匹配的 '>' 结束，
            // 支持嵌套泛型与约束（例如 <T1:Collections.List<Map<int,string>>,T2:Core.String>）。
            return ParseGenericBracketedTokens();
        }

        /// <summary>
        /// 从当前位置开始解析一个完整的泛型参数块：
        /// 形如 <T1:Collections.List<Map<int,string>>,T2:Core.String>
        /// 结束后 m_TokenIndex 停在 '>' 之后的下一个位置。
        /// 该函数可在解析 Array<T>、extends、interface 等场景复用。
        /// </summary>
        private List<Token> ParseGenericBracketedTokens()
        {
            List<Token> result = new List<Token>();
            if (!Match(ETokenType.Less))
                return result;

            int depth = 0;
            // 从第一个 '<' 开始收集
            while (m_TokenIndex < m_TokenList.Count)
            {
                Token t = Consume();
                result.Add(t);

                if (t.type == ETokenType.Less)
                {
                    depth++;
                }
                else if (t.type == ETokenType.Greater)
                {
                    depth--;
                    if (depth == 0)
                    {
                        // 完整的泛型块结束
                        break;
                    }
                }
            }

            if (depth != 0)
            {
                Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 泛型参数解析时尖括号不匹配！");
            }

            return result;
        }

        private List<List<Token>> ParseInterfaceList()
        {
            List<List<Token>> interfaces = new List<List<Token>>();
            while (m_TokenIndex < m_TokenList.Count)
            {
                if (Match(ETokenType.LeftBrace)) break;
                if (Match(ETokenType.Comma)) { Consume(); continue; }

                List<Token> interfaceName = ParseQualifiedName();
                if (interfaceName.Count == 0) break;

                // 如果后面紧跟 '<'，把泛型参数块一起吃掉（保证 IIterable<T> 成为完整的一段）
                if (Match(ETokenType.Less))
                {
                    var genericTokens = ParseGenericBracketedTokens();
                    interfaceName.AddRange(genericTokens);
                }

                interfaces.Add(interfaceName);
            }
            return interfaces;
        }

        private void SkipClassBody() { ParseBlockTokens(); }

        private Token CurrentToken => m_TokenIndex < m_TokenList.Count ? m_TokenList[m_TokenIndex] : null;
        private Token PeekToken(int offset = 1) { int index = m_TokenIndex + offset; return index < m_TokenList.Count ? m_TokenList[index] : null; }
        private Token Consume() => m_TokenIndex < m_TokenList.Count ? m_TokenList[m_TokenIndex++] : null;
        private bool Match(ETokenType tokenType) { Token current = CurrentToken; return current != null && current.type == tokenType; }
        private bool MatchAny(params ETokenType[] tokenTypes)
        {
            Token current = CurrentToken;
            if (current == null) return false;
            foreach (var tt in tokenTypes) if (current.type == tt) return true;
            return false;
        }

        private void TransitionState(DFAState newState)
        {
            if (newState != m_Context.currentState)
            {
                m_Context.stateStack.Push(m_Context.currentState);
                m_Context.currentState = newState;
            }
        }

        private bool IsClassDeclarationStart(Token token)
        {
            if (token == null) return false;
            if (token.type == ETokenType.Class || token.type == ETokenType.Interface || token.type == ETokenType.Enum || token.type == ETokenType.Data) return true;
            if (MatchAny(ETokenType.Public, ETokenType.Private, ETokenType.Projected, ETokenType.Static, ETokenType.Final, ETokenType.Const, ETokenType.Partial))
            {
                Token next = PeekToken();
                if (next != null && (next.type == ETokenType.Class || next.type == ETokenType.Interface || next.type == ETokenType.Enum || next.type == ETokenType.Data)) return true;
            }
            return false;
        }

        /// <summary>
        /// 纯 Token 版本的函数体解析：将一个完整的 "{" 开始 "}" 结束的 token 序列
        /// 拆成若干语句的 Token 列表，并交由 FileMetatUtil.CreateFileMetaSyntaxFromTokens
        /// 创建对应的 FileMetaSyntax，挂到给定的 FileMetaBlockSyntax 下。
        /// TokenToFileMeta 只负责“截断”，不直接 new 各种 FileMeta*Syntax。
        /// </summary>
        private void ParseFunctionBodyTokens(List<Token> bodyTokens, FileMetaBlockSyntax blockSyntax)
        {
            if (bodyTokens == null || bodyTokens.Count == 0 || blockSyntax == null)
                return;

            // 跳过首尾的 { }
            int start = 0;
            int end = bodyTokens.Count - 1;
            if (bodyTokens[start].type == ETokenType.LeftBrace) start++;
            if (end >= start && bodyTokens[end].type == ETokenType.RightBrace) end--;
            if (end < start) return;

            int depthBrace = 0;
            int depthPar = 0;
            int depthBracket = 0;
            List<Token> current = new List<Token>();

            void flush()
            {
                if (current.Count == 0) return;
                var syntax = FileMetatUtil.CreateFileMetaSyntaxFromTokens(m_FileMeta, current);
                current.Clear();
                if (syntax != null)
                {
                    blockSyntax.AddFileMetaSyntax(syntax);
                }
            }

            for (int i = start; i <= end; i++)
            {
                var t = bodyTokens[i];
                current.Add(t);

                if (t.type == ETokenType.LeftPar) depthPar++;
                else if (t.type == ETokenType.RightPar && depthPar > 0) depthPar--;
                else if (t.type == ETokenType.LeftBrace) depthBrace++;
                else if (t.type == ETokenType.RightBrace && depthBrace > 0) depthBrace--;
                else if (t.type == ETokenType.LeftBracket) depthBracket++;
                else if (t.type == ETokenType.RightBracket && depthBracket > 0) depthBracket--;

                // 顶层分号结束一条语句
                if (depthPar == 0 && depthBrace == 0 && depthBracket == 0 && t.type == ETokenType.SemiColon)
                {
                    flush();
                }
            }

            // 最后一条语句如果没有分号结尾，也尝试解析一次
            if (current.Count > 0)
            {
                flush();
            }
        }
    }
}
