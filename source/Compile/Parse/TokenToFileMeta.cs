//****************************************************************************
//  File:      TokenToFileMeta.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/01/15 12:00:00
//  Description: 直接从 Token 转换为 FileMeta 结构，不生成中间 Node 树
//               Token → FileMeta 的直接转换器
//****************************************************************************

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

                if (token.type == SimpleLanguage.ETokenType.LineEnd || token.type == SimpleLanguage.ETokenType.Space || token.type == SimpleLanguage.ETokenType.SemiColon)
                {
                    Consume();
                    continue;
                }

                if (token.type == SimpleLanguage.ETokenType.Import) ParseImportDirective();
                else if (token.type == SimpleLanguage.ETokenType.Namespace) ParseNamespaceDeclaration();
                else if (IsClassDeclarationStart(token)) ParseClassDeclaration();
                else Consume();
            }
        }

        private void ParseImportDirective()
        {
            TransitionState(DFAState.InImport);
            if (!Match(SimpleLanguage.ETokenType.Import)) return;

            Token importToken = Consume();
            List<Token> importPath = ParseQualifiedName();
            List<Token> allTokens = new List<Token>() { importToken };
            allTokens.AddRange(importPath);

            if (Match(SimpleLanguage.ETokenType.SemiColon)) allTokens.Add(Consume());

            if (importPath.Count > 0) m_FileMeta.AddFileImportSyntaxFromTokens(allTokens);
            TransitionState(DFAState.Initial);
        }

        private void ParseNamespaceDeclaration()
        {
            TransitionState(DFAState.InNamespace);
            if (!Match(SimpleLanguage.ETokenType.Namespace)) return;

            Token nsToken = Consume();
            List<Token> namespacePath = ParseQualifiedName();

            // 这里只将命名空间路径（标识符部分）传入 FileMeta，不包含 namespace 关键字本身
            if (Match(SimpleLanguage.ETokenType.SemiColon)) Consume();
            else if (Match(SimpleLanguage.ETokenType.LeftBrace)) { /* 块处理简化 */ }

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

            Token classNameToken = null;
            // 这里不仅接受 Identifier，还接受被词法阶段标记为 Type 的内建类型名
            // 例如：object/int/string 等在 LexerParse.ReadIdentifier 中被解析为 ETokenType.Type
            if (Match(SimpleLanguage.ETokenType.Identifier) || Match(SimpleLanguage.ETokenType.Type))
            {
                classNameToken = Consume();
            }
            else
            {
                // 如果没有显式标识符，记录错误并构造一个占位符名称，避免后续空引用
                Log.AddInStructFileMeta(EError.StructFileMetaStart, "Error 解析类型名称错误: 缺少标识符");
                classNameToken = new Token(m_FileMeta.path, SimpleLanguage.ETokenType.Identifier, "<anonymous>", 0, 0);
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

            //FileMetaClass fmc = new FileMetaClass(m_FileMeta, classNameToken, classModifiers, classKeyword);
            //m_FileMeta.AddFileClassFromTokens(new List<Token>());
            //m_FileMeta.AddFileMetaClass(fmc);

            List<Token> classHeaderTokens = new List<Token>();
            classHeaderTokens.AddRange(classModifiers);
            if (classKeyword != null) classHeaderTokens.Add(classKeyword);
            classHeaderTokens.Add(classNameToken);
            if (typeParameters.Count > 0) classHeaderTokens.AddRange(typeParameters);
            if (extendsKeyword != null) classHeaderTokens.Add(extendsKeyword);
            if (baseClass.Count > 0) classHeaderTokens.AddRange(baseClass);
            if (interfaceKeyword != null) classHeaderTokens.Add(interfaceKeyword);
            if (interfaceList.Count > 0)
            {
                foreach (var interfaceItem in interfaceList)
                {
                    classHeaderTokens.AddRange(interfaceItem);
                    // 在不同接口名之间插入逗号 token
                    classHeaderTokens.Add(new Token(m_FileMeta.path, SimpleLanguage.ETokenType.Comma, ",", 0, 0));
                }
                classHeaderTokens.RemoveAt(classHeaderTokens.Count - 1); // 去掉最后一个多余的逗号
            }

            FileMetaClass fmc = new FileMetaClass(m_FileMeta, classHeaderTokens);
            m_FileMeta.AddFileClassFromTokens(new List<Token>());
            m_FileMeta.AddFileMetaClass(fmc);

            if (Match(SimpleLanguage.ETokenType.LineEnd))
            {
                Consume();
            }
            if( Match(SimpleLanguage.ETokenType.LeftBrace))
            {
                ParseClassBody(fmc);
            }
            TransitionState(DFAState.Initial);
        }

        private void ParseClassBody(FileMetaClass fmc)
        {
            if (!Match(SimpleLanguage.ETokenType.LeftBrace)) return;
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

                // At top level inside class (not inside any (), {}, []), a semicolon ends a member
                if (parenDepth == 0 && braceDepth == 0 && bracketDepth == 0 && t.type == ETokenType.SemiColon)
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

            List<Token> typeTokens = new List<Token>();
            if (Match(SimpleLanguage.ETokenType.Void) || Match(SimpleLanguage.ETokenType.Type) || Match(SimpleLanguage.ETokenType.Identifier))
            {
                 typeTokens.Add(Consume());
                 if (Match(SimpleLanguage.ETokenType.Less)) typeTokens.AddRange(ParseTypeParameters());
                 while (Match(SimpleLanguage.ETokenType.LeftBracket))
                 {
                     typeTokens.Add(Consume());
                     if (Match(SimpleLanguage.ETokenType.RightBracket)) typeTokens.Add(Consume());
                 }
            }

            Token nameToken = null;
            if (Match(SimpleLanguage.ETokenType.Identifier)) nameToken = Consume();

            if (Match(SimpleLanguage.ETokenType.LeftPar)) 
            {
                List<Token> sigTokens = new List<Token>();
                sigTokens.AddRange(modifiers);
                sigTokens.AddRange(typeTokens);
                if (nameToken != null) sigTokens.Add(nameToken);
                
                List<Token> paramTokens = ParseParameters();
                sigTokens.AddRange(paramTokens);

                List<Token> bodyTokens = null;
                // allow optional LineEnd / Space between parameter list and body/semicolon
                while (Match(ETokenType.LineEnd) || Match(ETokenType.Space))
                {
                    Consume();
                }

                if (Match(ETokenType.LeftBrace)) bodyTokens = ParseBlockTokens();
                else if (Match(ETokenType.SemiColon)) Consume();
                
                FileMetaMemberFunction fmmf = new FileMetaMemberFunction(m_FileMeta, sigTokens, bodyTokens);

                // 使用纯 Token 的方式解析函数体内容，填充到 fmmf.fileMetaBlockSyntax
                if (bodyTokens != null && bodyTokens.Count > 0 && fmmf.fileMetaBlockSyntax != null)
                {
                    ParseFunctionBodyTokens(bodyTokens, fmmf.fileMetaBlockSyntax);
                }

                fmc.AddFileMemberFunction(fmmf);
            }
            else 
            {
                List<Token> varTokens = new List<Token>();
                varTokens.AddRange(modifiers);
                varTokens.AddRange(typeTokens);
                if (nameToken != null) varTokens.Add(nameToken);

                if (Match(ETokenType.Assign))
                {
                    varTokens.Add(Consume());
                    while (m_TokenIndex < m_TokenList.Count && !Match(ETokenType.SemiColon) && !Match(ETokenType.LineEnd))
                    {
                         if (Match(ETokenType.LeftBrace)) varTokens.AddRange(ParseBlockTokens());
                         else varTokens.Add(Consume());
                    }
                }
                
                if (Match(ETokenType.SemiColon)) varTokens.Add(Consume());

                if(varTokens.Count > 0 )
                {
                    FileMetaMemberVariable fmmv = new FileMetaMemberVariable(m_FileMeta, varTokens);
                    fmc.AddFileMemberVariable(fmmv);
                }
            }
        }
        
        private List<Token> ParseParameters()
        {
             List<Token> tokens = new List<Token>();
             if (Match(SimpleLanguage.ETokenType.LeftPar))
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
              if (Match(SimpleLanguage.ETokenType.LeftBrace))
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
                if (current.type == SimpleLanguage.ETokenType.Identifier || current.type == SimpleLanguage.ETokenType.Type) names.Add(Consume());
                else if (current.type == SimpleLanguage.ETokenType.Period) { Consume(); continue; }
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
            if (!Match(SimpleLanguage.ETokenType.Less))
                return result;

            int depth = 0;
            // 从第一个 '<' 开始收集
            while (m_TokenIndex < m_TokenList.Count)
            {
                Token t = Consume();
                result.Add(t);

                if (t.type == SimpleLanguage.ETokenType.Less)
                {
                    depth++;
                }
                else if (t.type == SimpleLanguage.ETokenType.Greater)
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
                if (interfaceName.Count > 0) interfaces.Add(interfaceName);
                else break;
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
