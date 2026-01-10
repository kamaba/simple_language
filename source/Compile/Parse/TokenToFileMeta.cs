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

        // 容器栈：用于维护当前结构性节点的父子关系（FileMeta / FileMetaNamespace / FileMetaClass）
        private readonly Stack<object> m_ContainerStack = new Stack<object>();

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
                // 根容器为 FileMeta 本身
                m_ContainerStack.Clear();
                m_ContainerStack.Push(m_FileMeta);
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
                if (token == null || token.type == ETokenType.Finished)
                {
                    break;
                }

                if (token.type == ETokenType.LineEnd || token.type == ETokenType.Space || token.type == ETokenType.SemiColon)
                {
                    Consume();
                    continue;
                }

                if (token.type == ETokenType.Import)
                {
                    ParseImportDirective();
                }
                else if (token.type == ETokenType.Namespace)
                {
                    ParseNamespaceDeclaration();
                }
                else if (IsClassDeclarationStart(token))
                {
                    ParseClassDeclaration();
                }
                else
                {
                    // 处理 namespace 块内的隐式类定义：
                    // 形如：
                    //   namespace NS
                    //   {
                    //       ClassName
                    //       {
                    //       }
                    //   }
                    // 这里没有 class/interface/enum/data 关键字。
                    // 只在 namespace / class 容器内尝试隐式类识别，避免污染顶层其它语句。
                    if (m_ContainerStack.Count > 0 &&
                        (m_ContainerStack.Peek() is FileMetaNamespace || m_ContainerStack.Peek() is FileMetaClass))
                    {
                        // 从当前位置收集到行尾或分号的 token，尝试识别隐式类头。
                        var headTokens = new List<Token>();
                        int lookIndex = m_TokenIndex;
                        while (lookIndex < m_TokenList.Count)
                        {
                            var t = m_TokenList[lookIndex];
                            if (t.type == ETokenType.LineEnd || t.type == ETokenType.SemiColon)
                            {
                                break;
                            }
                            headTokens.Add(t);
                            lookIndex++;
                        }

                        if (headTokens.Count > 0 && IsImplicitClassDeclaration(headTokens, out var _))
                        {
                            // 将当前 token 流替换为 headTokens 调用 ParseClassDeclaration，
                            // 仅解析类头，类体由 ParseClassDeclaration/ParseClassBody 负责。
                            var oldList = m_TokenList;
                            int oldIndex = m_TokenIndex;

                            m_TokenList = headTokens;
                            m_TokenIndex = 0;

                            try
                            {
                                ParseClassDeclaration();
                            }
                            catch (Exception ex)
                            {
                                Log.AddInStructFileMeta(EError.None, $"TokenToFileMeta 隐式类解析异常: {ex.Message}");
                            }
                            finally
                            {
                                // 还原主 token 流，并把主索引跳到刚才那一行（或分号）之后
                                m_TokenList = oldList;
                                m_TokenIndex = lookIndex;
                                // 跳过行结束/分号
                                if (m_TokenIndex < m_TokenList.Count &&
                                    (m_TokenList[m_TokenIndex].type == ETokenType.LineEnd ||
                                     m_TokenList[m_TokenIndex].type == ETokenType.SemiColon))
                                {
                                    m_TokenIndex++;
                                }
                            }

                            continue;
                        }
                    }

                    Consume();
                }
            }
        }

        private void ParseImportDirective()
        {
            // import 语句只能在最外层（编译单元级别）出现
            Debug.Assert(m_Context.currentState == DFAState.Initial, "import 只能在最外层使用，不能出现在 namespace/class 内部");

            TransitionState(DFAState.InImport);
            if (!Match(ETokenType.Import)) return;

            Token importToken = Consume();
            List<Token> importPath = ParseQualifiedName();
            List<Token> allTokens = new List<Token>();
            allTokens.AddRange(importPath);

            if (Match(ETokenType.SemiColon)) allTokens.Add(Consume());

            if (importPath.Count > 0) m_FileMeta.AddFileImportSyntaxFromTokens(importToken, allTokens);
            TransitionState(DFAState.Initial);
        }

        private void ParseNamespaceDeclaration()
        {
            TransitionState(DFAState.InNamespace);
            if (!Match(ETokenType.Namespace)) return;

            Token nsToken = Consume();
            List<Token> namespacePath = ParseQualifiedName();

            // 这里只将命名空间路径（标识符部分）传入 FileMeta，不包含 namespace 关键字本身
            FileMetaNamespace currentNamespace = null;
            if (namespacePath.Count > 0)
            {
                currentNamespace = m_FileMeta.AddFileNamespaceFromTokens(nsToken, namespacePath);
            }
            else
            {
                Debug.Assert(false, "namespace 必须跟随名称");
            }

            // 支持两种形式：
            //   namespace Core;
            //   namespace Core { ... }
            //   namespace Core\n{ ... }

            // 跳过名称之后紧跟的空格和换行，再判断是 ';' 还是 '{'
            while (Match(ETokenType.Space) || Match(ETokenType.LineEnd))
            {
                Consume();
            }

            if (Match(ETokenType.SemiColon))
            {
                Consume();
            }
            else if (Match(ETokenType.LeftBrace))
            {
                // 进入命名空间块，沿用与 ParseClassBody 类似的 '{ }' 深度遍历逻辑
                int depth = 0;
                // 记录命名空间起始 '{' token
                Token namespaceLeftBrace = Consume();
                depth = 1;
                Token namespaceRightBrace = null;

                // 进入 namespace 容器
                if (currentNamespace != null)
                {
                    m_ContainerStack.Push(currentNamespace);
                }

                while (m_TokenIndex < m_TokenList.Count && depth > 0)
                {
                    var t = CurrentToken;
                    if (t.type == ETokenType.LeftBrace)
                    {
                        depth++;
                        Consume();
                    }
                    else if (t.type == ETokenType.RightBrace)
                    {
                        depth--;
                        namespaceRightBrace = t;
                        Consume();
                    }
                    else
                    {
                        // 在 namespace 内，复用顶层解析逻辑：import/namespace/class 等
                        ParseCompilationUnit();
                    }
                }

                // 将 namespace 的大括号位置信息记录到 FileMetaNamespace，用于结构性定位
                if (currentNamespace != null)
                {
                    currentNamespace.SetBraceToken(namespaceLeftBrace, namespaceRightBrace);
                }

                // 退出 namespace 容器
                if (currentNamespace != null && m_ContainerStack.Count > 0 && ReferenceEquals(m_ContainerStack.Peek(), currentNamespace))
                {
                    m_ContainerStack.Pop();
                }
            }

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

            // 类模板参数：紧随类名之后的 `<T1, T2:...>` 统一走 ParseTemplateTokens
            List<Token> typeParameters = new List<Token>();
            int tpIndex = m_TokenIndex;
            if (tpIndex < m_TokenList.Count && m_TokenList[tpIndex].type == ETokenType.Less)
            {
                typeParameters = ParseTemplateTokens(m_TokenList, ref tpIndex);
                m_TokenIndex = tpIndex;
            }

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
            // 根据当前容器栈决定类的父节点：namespace / 外层 class / FileMeta
            if (m_ContainerStack.Count > 0)
            {
                var container = m_ContainerStack.Peek();
                if (container is FileMetaNamespace ns)
                {
                    // 挂到 namespace 下，同时在 FileMetaClass 内部设置 m_TopLevelFileMetaNamespace
                    ns.AddFileMetaClass(fmc);
                }
                else if (container is FileMetaClass parentClass)
                {
                    // 类中类：由外层类维护 m_TopLevelFileMetaClass
                    parentClass.AddInnerFileMetaClass(fmc);
                }
                else if (container is FileMeta)
                {
                    // 顶层类：直接挂到 FileMeta
                    m_FileMeta.AddFileMetaClass(fmc);
                }
                else
                {
                    // 兜底：仍然挂到 FileMeta，避免丢失
                    m_FileMeta.AddFileMetaClass(fmc);
                }
            }
            else
            {
                // 理论上不会发生，没有容器时也挂到 FileMeta
                m_FileMeta.AddFileMetaClass(fmc);
            }

            if (Match(ETokenType.LineEnd))
            {
                Consume();
            }
            if (Match(ETokenType.LeftBrace))
            {
                // 进入类容器
                m_ContainerStack.Push(fmc);
                ParseClassBody(fmc);
                // 退出类容器
                if (m_ContainerStack.Count > 0 && ReferenceEquals(m_ContainerStack.Peek(), fmc))
                {
                    m_ContainerStack.Pop();
                }
            }
            TransitionState(DFAState.Initial);
        }

        private void ParseClassBody(FileMetaClass fmc)
        {
            if (!Match(ETokenType.LeftBrace)) return;

            // 消费类体起始左括号 '{'
            Consume();
            m_Context.braceDepth++;

            int depth = 1;              // 类体 brace 深度，从 1 开始
            int parenDepth = 0;         // 当前 () 深度
            int bracketDepth = 0;       // 当前 [] 深度
            var currentMember = new List<Token>();

            FileMetaMemberFunction lastParsedFunction = null;

            void flushCurrentMember()
            {
                // 修剪前后空白/分号
                int start = 0;
                int end = currentMember.Count - 1;
                while (start <= end && (currentMember[start].type == ETokenType.Space || currentMember[start].type == ETokenType.LineEnd || currentMember[start].type == ETokenType.SemiColon))
                    start++;
                while (end >= start && (currentMember[end].type == ETokenType.Space || currentMember[end].type == ETokenType.LineEnd || currentMember[end].type == ETokenType.SemiColon))
                    end--;
                if (end < start) { currentMember.Clear(); return; }

                var memberTokens = currentMember.GetRange(start, end - start + 1);

                var oldList = m_TokenList;
                int oldIndex = m_TokenIndex;
                m_TokenList = memberTokens;
                m_TokenIndex = 0;
                try
                {
                    // 解析成员声明，若为函数则返回对应的 FileMetaMemberFunction
                    lastParsedFunction = ParseClassMember(fmc);
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

            while (m_TokenIndex < m_TokenList.Count && depth > 0)
            {
                var t = CurrentToken;

                // 类体结束：遇到与起始 '{' 对应的 '}'，并且不是被 ParseFunctionBodyStreaming 消费掉的
                if (t.type == ETokenType.RightBrace && depth == 1 && parenDepth == 0 && bracketDepth == 0)
                {
                    Consume();
                    depth--;
                    break;
                }

                // 若当前是函数体起始 '{'，由 ParseFunctionBodyStreaming 处理整个函数体
                if (t.type == ETokenType.LeftBrace && currentMember.Count > 0)
                {
                    // 先把当前 member 声明部分解析为签名（可能是函数/内部类/成员变量）
                    flushCurrentMember();

                    // 如果上一条成员是函数，则在主 token 流上流式解析其函数体
                    if (lastParsedFunction != null && Match(ETokenType.LeftBrace))
                    {
                        ParseFunctionBodyStreaming(lastParsedFunction);
                        lastParsedFunction = null;
                    }
                    continue;
                }

                // 累积当前成员声明 token
                currentMember.Add(Consume());

                if (t.type == ETokenType.LeftPar) parenDepth++;
                else if (t.type == ETokenType.RightPar && parenDepth > 0) parenDepth--;
                else if (t.type == ETokenType.LeftBrace) depth++;
                else if (t.type == ETokenType.RightBrace && depth > 0) depth--;
                else if (t.type == ETokenType.LeftBracket) bracketDepth++;
                else if (t.type == ETokenType.RightBracket && bracketDepth > 0) bracketDepth--;

                // 在类体内，仅在顶层（未进入任何 () / []）且未进入嵌套类/函数体时，以分号或行结束来切分成员声明。
                if (depth == 1 && parenDepth == 0 && bracketDepth == 0 &&
                    (t.type == ETokenType.SemiColon || t.type == ETokenType.LineEnd))
                {
                    flushCurrentMember();
                }
            }

            m_Context.braceDepth--;

            if (currentMember.Count > 0)
            {
                flushCurrentMember();
                // 类体末尾如果刚好是函数声明且紧接着是函数体，也需要尝试函数体解析
                if (lastParsedFunction != null && Match(ETokenType.LeftBrace))
                {
                    ParseFunctionBodyStreaming(lastParsedFunction);
                    lastParsedFunction = null;
                }
            }
        }

        private FileMetaMemberFunction ParseClassMember(FileMetaClass fmc)
        {
            // 统一解析：成员变量 / 成员函数 / 类中类
            if (m_TokenList == null || m_TokenList.Count == 0)
                return null;

            var tokens = m_TokenList;

            // 去掉前后空白
            int start = 0;
            int end = tokens.Count - 1;
            while (start <= end && (tokens[start].type == ETokenType.Space || tokens[start].type == ETokenType.LineEnd)) start++;
            while (end >= start && (tokens[end].type == ETokenType.Space || tokens[end].type == ETokenType.LineEnd)) end--;
            if (end < start) return null;
            tokens = tokens.GetRange(start, end - start + 1);
            int index = 0;

            // 1. 权限关键字（可选）：public/projected/private/extern
            var modifiers = new List<Token>();
            while (index < tokens.Count)
            {
                var t = tokens[index];
                if (t.type == ETokenType.Public || t.type == ETokenType.Private
                    || t.type == ETokenType.Projected || t.type == ETokenType.Extern)
                {
                    modifiers.Add(t);
                    index++;
                }
                else
                {
                    break;
                }
            }
            // 2. 其他修饰符：static/final/override/mut/get/set 等（可选，顺序不限）
            while (index < tokens.Count)
            {
                var t = tokens[index];
                if (t.type == ETokenType.Static || t.type == ETokenType.Final
                    || t.type == ETokenType.Override || t.type == ETokenType.Mut
                    || t.type == ETokenType.Get || t.type == ETokenType.Set)
                {
                    modifiers.Add(t);
                    index++;
                }
                else
                {
                    break;
                }
            }

            if (index >= tokens.Count)
                return null;

            // === 1. 显式关键字的类中类：public class Class1{} / data Class2{} 等 ===
            Token firstNonMod = tokens[index];
            if (firstNonMod.type == ETokenType.Class || firstNonMod.type == ETokenType.Interface
                || firstNonMod.type == ETokenType.Enum || firstNonMod.type == ETokenType.Data)
            {
                var oldList = m_TokenList;
                int oldIndex = m_TokenIndex;
                m_TokenList = tokens;
                m_TokenIndex = index; // 指向 class 关键字
                try
                {
                    ParseClassDeclaration();
                }
                catch (Exception ex)
                {
                    Log.AddInStructFileMeta(EError.None, $"ParseClassMember 类中类解析异常: {ex.Message}");
                }
                finally
                {
                    m_TokenList = oldList;
                    m_TokenIndex = oldIndex;
                }
                return null;
            }

            // === 2. 无 class 关键字的内部类声明：Class1<T> extends Object { } ===
            // 从当前 index 开始，尝试识别 [类型部分] + 名称 + 可选模板 + extends/interface + '{'
            int scan = index;
            // 跳过可能存在的类型前缀，例如 outer.Type 或内建类型
            while (scan < tokens.Count &&
                   (tokens[scan].type == ETokenType.Identifier ||
                    tokens[scan].type == ETokenType.Type ||
                    tokens[scan].type == ETokenType.Period ||
                    tokens[scan].type == ETokenType.Void))
            {
                scan++;
            }

            // 名称 token 位置假定为最后一个标识符/类型名
            int nameCandidate = scan - 1;
            if (nameCandidate >= index &&
                (tokens[nameCandidate].type == ETokenType.Identifier || tokens[nameCandidate].type == ETokenType.Type))
            {
                int afterName = nameCandidate + 1;
                // 跳过空白
                while (afterName < tokens.Count &&
                       (tokens[afterName].type == ETokenType.Space || tokens[afterName].type == ETokenType.LineEnd))
                {
                    afterName++;
                }

                // 可选模板参数块 <T,...>
                if (afterName < tokens.Count && tokens[afterName].type == ETokenType.Less)
                {
                    // 仅用于向前跳过模板，不需要保存
                    ParseTemplateTokens(tokens, ref afterName);
                    while (afterName < tokens.Count &&
                           (tokens[afterName].type == ETokenType.Space || tokens[afterName].type == ETokenType.LineEnd))
                    {
                        afterName++;
                    }
                }

                // 跳过 extends/interface 等继承部分，直到遇到 '{' 或 ';'
                int inheritScan = afterName;
                while (inheritScan < tokens.Count &&
                       tokens[inheritScan].type != ETokenType.LeftBrace &&
                       tokens[inheritScan].type != ETokenType.SemiColon)
                {
                    inheritScan++;
                }

                if (inheritScan < tokens.Count && tokens[inheritScan].type == ETokenType.LeftBrace)
                {
                    // 认为是无关键字内部类声明：交给 ParseClassDeclaration，逻辑与外部类一致
                    var oldList = m_TokenList;
                    int oldIndex = m_TokenIndex;
                    m_TokenList = tokens;
                    m_TokenIndex = index; // 从修饰符后第一个 token 重新进入
                    try
                    {
                        ParseClassDeclaration();
                    }
                    catch (Exception ex)
                    {
                        Log.AddInStructFileMeta(EError.None, $"ParseClassMember 隐式类中类解析异常: {ex.Message}");
                    }
                    finally
                    {
                        m_TokenList = oldList;
                        m_TokenIndex = oldIndex;
                    }
                    return null;
                }
            }

            // === 3. 非类中类：成员函数 / 成员变量 ===

            // 从当前 index 到行尾的子序列作为候选，提取类型前缀和名称
            var tailTokens = tokens.GetRange(index, tokens.Count - index);
            List<Token> typeTokens;
            Token nameToken;
            if (!GetTypeAndNameTokens(tailTokens, out typeTokens, out nameToken))
            {
                return null;
            }

            // 计算 nameToken 在 tailTokens 中的索引
            int namePosInTail = -1;
            for (int i = 0; i < tailTokens.Count; i++)
            {
                if (tailTokens[i] == nameToken)
                {
                    namePosInTail = i;
                    break;
                }
            }
            if (namePosInTail == -1)
                return null;

            // 计算 Name 之后的位置，先越过空白
            int afterName2 = index + namePosInTail + 1;
            while (afterName2 < tokens.Count &&
                   (tokens[afterName2].type == ETokenType.Space || tokens[afterName2].type == ETokenType.LineEnd))
            {
                afterName2++;
            }

            // 可选的函数模板块：Fun<T>(...)
            List<Token> nameTemplateTokens = null;
            if (afterName2 < tokens.Count && tokens[afterName2].type == ETokenType.Less)
            {
                nameTemplateTokens = ParseTemplateTokens(tokens, ref afterName2);
                while (afterName2 < tokens.Count &&
                       (tokens[afterName2].type == ETokenType.Space || tokens[afterName2].type == ETokenType.LineEnd))
                {
                    afterName2++;
                }
            }

            bool hasPar = afterName2 < tokens.Count && tokens[afterName2].type == ETokenType.LeftPar;

            if (hasPar)
            {
                // === 成员函数 ===

                // 解析完整参数列表 ()，从 afterName2 位置开始
                int parStart = afterName2;
                int parEnd = -1;
                int parDepth = 0;
                for (int i = parStart; i < tokens.Count; i++)
                {
                    var pt = tokens[i];
                    if (pt.type == ETokenType.LeftPar) parDepth++;
                    else if (pt.type == ETokenType.RightPar)
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
                    paramTokens = tokens.GetRange(parStart, parEnd - parStart + 1);
                }

                var fmmf = new FileMetaMemberFunction(
                     m_FileMeta,
                     modifiers,
                     typeTokens,
                     nameToken,
                     paramTokens
                   );

                fmc.AddFileMemberFunction(fmmf);

                // 如果在同一成员声明行内紧接着存在函数体 '{...}'，直接在当前局部 token 视图中解析并绑定到该函数。
                // 这里不依赖主 token 流的 m_TokenIndex，而是使用局部 tokens 列表计算函数体 token 子序列。
                int bodyStart = parEnd + 1;
                while (bodyStart < tokens.Count &&
                       (tokens[bodyStart].type == ETokenType.Space ||
                        tokens[bodyStart].type == ETokenType.LineEnd ||
                        tokens[bodyStart].type == ETokenType.SemiColon))
                {
                    bodyStart++;
                }

                if (bodyStart < tokens.Count && tokens[bodyStart].type == ETokenType.LeftBrace)
                {
                    // 在局部 token 列表中截取完整的 '{...}' 函数体块
                    int depthBrace = 0;
                    int bodyEnd = -1;
                    for (int i = bodyStart; i < tokens.Count; i++)
                    {
                        var bt = tokens[i];
                        if (bt.type == ETokenType.LeftBrace)
                        {
                            depthBrace++;
                        }
                        else if (bt.type == ETokenType.RightBrace)
                        {
                            depthBrace--;
                            if (depthBrace == 0)
                            {
                                bodyEnd = i;
                                break;
                            }
                        }
                    }

                    if (bodyEnd >= bodyStart)
                    {
                        var bodyTokens = tokens.GetRange(bodyStart, bodyEnd - bodyStart + 1);
                        // 使用已有的基于 List<Token> 的函数体解析逻辑，将语句挂到 fmmf 的 blockSyntax 上
                        if (fmmf.fileMetaBlockSyntax != null)
                        {
                            ParseFunctionBodyTokens(bodyTokens, fmmf.fileMetaBlockSyntax);
                        }
                    }
                }

                return fmmf;
            }
            else
            {
                // === 成员变量 ===
                int nameGlobalIndex = index + namePosInTail;
                int exprStart = nameGlobalIndex + 1;
                // 成员变量必须具有 '=' 初始化表达式
                bool hasAssign = false;
                for (int i = exprStart; i < tokens.Count; i++)
                {
                    if (tokens[i].type == ETokenType.Assign)
                    {
                        hasAssign = true;
                        exprStart = i + 1;
                        break;
                    }
                }

                Debug.Assert(hasAssign, "成员变量必须包含初始化表达式（缺少 '='）");
                if (!hasAssign)
                {
                    Log.AddInStructFileMeta(EError.None, "Error 成员变量缺少 '=' 初始化表达式");
                    return null;
                }

                if (exprStart >= tokens.Count)
                {
                    Log.AddInStructFileMeta(EError.None, "Error 成员变量缺少初始化表达式");
                    return null;
                }

                var exprTokens = tokens.GetRange(exprStart, tokens.Count - exprStart);

                var fmmv = new FileMetaMemberVariable(
                    m_FileMeta,
                    modifiers,
                    typeTokens,
                    nameToken,
                    exprTokens);

                fmc.AddFileMemberVariable(fmmv);
                return null;
            }
        }
        /// <summary>
        /// 从一行成员声明 token 中提取类型前缀 token 列表和成员名 token：
        /// 仅在第一个 '(' 之前参与识别，避免把参数列表里的标识符当成成员名。
        /// 规则：
        ///  - 如果 '(' 之前只有一个 Identifier，则该标识符为 name（如: _init_()）。
        ///  - 如果有多个 Identifier / Type，则最后一个 Identifier 为 name，其前面的所有 token 视为类型部分
        ///    （支持复杂泛型/命名空间/数组，如 List<Map<NS.ClassName, Set<Core.String>>>[][][] name）。
        /// </summary>
        private bool GetTypeAndNameTokens(List<Token> lineTokens, out List<Token> typeTokens, out Token nameToken)
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
                var tt = lineTokens[i].type;
                // 把标识符和内建类型名 (ETokenType.Type) 都统计进来，
                // 这样 "override string toString()" 中的 string + toString 都参与判断，
                // 最终 toString 作为 name，string 归入 typeTokens。
                if (tt == ETokenType.Identifier || tt == ETokenType.Type || tt == ETokenType.Void)
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
                // 整行没有标识符/类型名，无法作为成员声明
                return false;
            }

            if (idCount == 1)
            {
                // 只有一个 Identifier/Type： `_init_()` / `_init_(){}` 之类 —— 该标识符就是成员名
                nameToken = lineTokens[firstIdIndex];
            }
            else
            {
                // 存在多个 Identifier/Type：最后一个 Identifier/Type（在 '(' 之前）作为 nameToken，其前面的全部视为类型部分
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
            var tokens = new List<Token>();
            if (!Match(ETokenType.LeftBrace))
            {
                return tokens;
            }

            // 流式消费一个完整的 '{...}' 块：只遍历一次主 token 列表
            int depth = 0;
            while (m_TokenIndex < m_TokenList.Count)
            {
                var t = Consume();
                tokens.Add(t);

                if (t.type == ETokenType.LeftBrace)
                {
                    depth++;
                }
                else if (t.type == ETokenType.RightBrace)
                {
                    depth--;
                    if (depth == 0)
                    {
                        // 当前 '}' 结束了与起始 '{' 对应的块
                        break;
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

                // 保留 '.' 作为连接符，让上层在需要时还可以看到完整的 NS1 . ClassName 结构
                if (current.type == ETokenType.Identifier || current.type == ETokenType.Type || current.type == ETokenType.Period)
                {
                    names.Add(Consume());
                    continue;
                }

                break;
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

        private bool IsImplicitClassDeclaration(List<Token> tokens, out Token nameToken)
        {
            nameToken = null;
            if (tokens == null || tokens.Count == 0) return false;

            int i = 0;
            // 跳过修饰符、空白
            while (i < tokens.Count &&
                   (tokens[i].type == ETokenType.Public ||
                    tokens[i].type == ETokenType.Private ||
                    tokens[i].type == ETokenType.Projected ||
                    tokens[i].type == ETokenType.Static ||
                    tokens[i].type == ETokenType.Final ||
                    tokens[i].type == ETokenType.Const ||
                    tokens[i].type == ETokenType.Partial ||
                    tokens[i].type == ETokenType.Space ||
                    tokens[i].type == ETokenType.LineEnd))
            {
                i++;
            }

            if (i >= tokens.Count) return false;

            // 期待 ClassName 标识符
            if (tokens[i].type != ETokenType.Identifier && tokens[i].type != ETokenType.Type)
                return false;

            nameToken = tokens[i];
            i++;

            // 在遇到 '{' 之前，不允许出现 '('，否则更像函数/表达式
            while (i < tokens.Count &&
                   (tokens[i].type == ETokenType.Space || tokens[i].type == ETokenType.LineEnd))
            {
                i++;
            }

            // 中间如果先看到 '('，就不是隐式类
            for (int j = i; j < tokens.Count; j++)
            {
                var t = tokens[j].type;
                if (t == ETokenType.LeftPar)
                    return false;
                if (t == ETokenType.LeftBrace)
                    return true;
                if (t == ETokenType.SemiColon)
                    break;
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
                Console.WriteLine("解析---------:" + t.ToLexemeAllString());

                if (t.type == ETokenType.LeftPar) depthPar++;
                else if (t.type == ETokenType.RightPar && depthPar > 0) depthPar--;
                else if (t.type == ETokenType.LeftBrace) depthBrace++;
                else if (t.type == ETokenType.RightBrace && depthBrace > 0) depthBrace--;
                else if (t.type == ETokenType.LeftBracket) depthBracket++;
                else if (t.type == ETokenType.RightBracket && depthBracket > 0) depthBracket--;

                // 1) 控制流语句 if/else/for/while/do/switch：
                //    由 FileMetatUtil.CreateFileMetaSyntaxFromTokens 负责识别，这里只在整个语句单元结束时 flush。
                //    我们依然以顶层分号或对应 block 结束 '}' 作为一条语句的结束标志。

                // 2) 普通表达式：顶层分号 ';' 结束一条语句。
                if (depthPar == 0 && depthBrace == 0 && depthBracket == 0 && t.type == ETokenType.SemiColon)
                {
                    flush();
                }
                // 3) as/is 这种可能不带分号、以换行分隔的表达式：在顶层遇到 LineEnd 也可以结束一条语句。
                else if (depthPar == 0 && depthBrace == 0 && depthBracket == 0 && t.type == ETokenType.LineEnd)
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
        /// <summary>
        /// 流式函数体解析：从当前 token 流上的 '{' 开始，基于 DFAState.InFunction 状态，
        /// 直接向前扫描直到匹配的右括号 '}'，并按顶层分号/换行切分语句，挂到给定的函数块语法节点中。
        /// 注意：调用方需要确保 CurrentToken 指向函数体起始的 '{'。
        /// </summary>
        private void ParseFunctionBodyStreaming(FileMetaMemberFunction fmmf)
        {
            if (fmmf == null) return;

            FileMetaBlockSyntax blockSyntax = fmmf.fileMetaBlockSyntax;
            if (blockSyntax == null) return;

            if (!Match(ETokenType.LeftBrace))
                return;

            // 进入函数体：记录当前状态，切换到 InFunction
            DFAState previousState = m_Context.currentState;
            TransitionState(DFAState.InFunction);

            int depthBrace = 0;
            int depthPar = 0;
            int depthBracket = 0;
            var current = new List<Token>();

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

            // 消费起始 '{'
            var first = Consume();
            current.Add(first);
            depthBrace = 1;

            while (m_TokenIndex < m_TokenList.Count && depthBrace > 0)
            {
                var t = Consume();
                if (t == null) break;

                current.Add(t);

                if (t.type == ETokenType.LeftPar) depthPar++;
                else if (t.type == ETokenType.RightPar && depthPar > 0) depthPar--;
                else if (t.type == ETokenType.LeftBrace) depthBrace++;
                else if (t.type == ETokenType.RightBrace && depthBrace > 0) depthBrace--;
                else if (t.type == ETokenType.LeftBracket) depthBracket++;
                else if (t.type == ETokenType.RightBracket && depthBracket > 0) depthBracket--;

                // 顶层语句分割：与 ParseFunctionBodyTokens 中的逻辑类似，
                // 这里 depthBrace==1 表示仍在当前函数体的顶层 block 内。
                if (depthPar == 0 && depthBrace == 1 && depthBracket == 0 && t.type == ETokenType.SemiColon)
                {
                    flush();
                }
                else if (depthPar == 0 && depthBrace == 1 && depthBracket == 0 && t.type == ETokenType.LineEnd)
                {
                    flush();
                }
            }

            // 函数体结束后，剩余内容也尝试解析一次
            if (current.Count > 0)
            {
                flush();
            }

            // 退出函数体状态
            m_Context.currentState = previousState;
        }

        /// <summary>
        /// 解析紧随某个标识符/类型名之后的模板参数块：形如 "<T1, T2:List<int>, Map<Core.Class, string>>"。
        /// 从给定列表的当前位置 (ref index) 시작，假定当前 토큰为 '<'，
        /// 收集完整的尖括号内容（支持嵌套），并将 index 移动到模板块结束之后的下一个位置。
        /// 
        /// 调用方既可以用于类模板定义，也可以用于函数模板定义，避免重复实现。
        /// </summary>
        private List<Token> ParseTemplateTokens(List<Token> tokens, ref int index)
        {
            var genericTokens = new List<Token>();
            if (tokens == null || index >= tokens.Count || tokens[index].type != ETokenType.Less)
            {
                return genericTokens;
            }

            int depthLess = 0;
            int i = index;
            for (; i < tokens.Count; i++)
            {
                var t = tokens[i];
                genericTokens.Add(t);
                if (t.type == ETokenType.Less)
                {
                    depthLess++;
                }
                else if (t.type == ETokenType.Greater)
                {
                    depthLess--;
                    if (depthLess == 0)
                    {
                        i++;
                        break;
                    }
                }
            }

            index = i;
            return genericTokens;
        }
    }
}
