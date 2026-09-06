//****************************************************************************
//  File:      DllImportSourceRewriter.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/9/4 12:00:00
//  Description: @DllImport C# P/Invoke 风格函数声明的源码文本预改写器
//****************************************************************************

using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    /// <summary>
    /// 把 C# 风格 FFI 函数声明改写为等价两段式源码（Token 解析前执行）：
    /// <code>
    ///     @DllImport( "lib", "sym"[, "sig"] )
    ///     static Ret name( T1 a1, T2 a2, ... ) { ...fallback 本体... }
    /// </code>
    /// 改写为（原文切片逐字符保留，换行结构不变，行号严格不变）：
    /// <code>
    ///     @DllImport( "lib", "sym"[, "sig"] ) static Func&lt;Ret,T1,T2,...&gt; __dll_name
    ///     static Ret name( T1 a1, T2 a2, ... ) { if ( __dll_name != null ) { ret __dll_name( a1, a2, ... ) } else { ...fallback 本体... } }
    /// </code>
    /// 运行时语义：dll 绑定可用（隐藏字段非 null）时转发 dll 导入函数；
    /// 不可用（运行平台不符 / LoadLibrary 失败 / 符号不存在 -> bindFunction
    /// 返回 null）时走 else 分支执行原函数体（fallback 本体）。
    /// 函数体为必填：无体形态不接管，交由语法阶段按普通函数定义报错；
    /// 有返回值时原体必须自带 ret（语言一般规则）。
    /// 隐藏字段 __dll_name 由 MetaMemberVariable.TryCreateDllImportExpress 注入
    /// FFI.StaticLibrary.bindFunction(路径,符号,sig) 初始化（别名查表/sig 推导
    /// 均复用现有 @DllImport 成员变量机制，内部仍走 LoadLibrary 体系）；
    /// wrapper 为编译期合成的同类静态函数，可直接调用。void 返回时转发分支不带 ret。
    /// 普通（无 @DllImport 标签的）函数与旧 static Func&lt;...&gt; 字段不受影响。
    /// </summary>
    public static class DllImportSourceRewriter
    {
        /// <summary>改写 m_ContentBuffer 中的 C# 风格 @DllImport 声明；无匹配时原 buffer 返回。</summary>
        public static char[] Rewrite( char[] buffer, string filePath )
        {
            string source = new string(buffer);
            if (source.IndexOf("@DllImport", StringComparison.Ordinal) < 0)
                return buffer;

            var sb = new StringBuilder(source.Length + 256);
            int i = 0;
            int len = source.Length;
            int rewrites = 0;
            while (i < len)
            {
                char c = source[i];
                // 注释与字符串字面量：整段原样透传（内部不识别 @DllImport）
                if (c == '#' || c == '"' || c == '\'')
                {
                    int next = SkipCommentOrString(source, i);
                    sb.Append(source, i, next - i);
                    i = next;
                    continue;
                }
                if (c == '@')
                {
                    // @ 后必须紧跟字母/下划线才构成 attribute（对齐 LexerParseToToken.ReadAt）
                    int j = i + 1;
                    if (j < len && (char.IsLetter(source[j]) || source[j] == '_'))
                    {
                        int nameStart = j;
                        while (j < len && IsIdentChar(source[j]))
                            j++;
                        string attrName = source.Substring(nameStart, j - nameStart);
                        if (attrName == "DllImport")
                        {
                            var result = TryMatchDllImportDecl(source, i, j);
                            if (result != null)
                            {
                                sb.Append(result.replacement);
                                i = result.end;
                                rewrites++;
                                continue;
                            }
                        }
                    }
                    // 非 DllImport attribute 或形态不匹配：'@' 原样透传
                    sb.Append(c);
                    i++;
                    continue;
                }
                sb.Append(c);
                i++;
            }

            if (rewrites == 0)
                return buffer;

            Log.AddProcessLog(LID.ShowExtendMessage,
                "DllImportSourceRewriter: '{0}' rewritten {1} C#-style declaration(s)", filePath, rewrites);
            return sb.ToString().ToCharArray();
        }

        private class MatchResult
        {
            public string replacement;
            public int end;   // 替换覆盖区间 [atPos, end)
        }

        /// <summary>
        /// 尝试匹配 atPos 处开始的完整 C# 风格声明：
        /// @DllImport( "str"[, "str"]... ) static RetType FuncName( [Type name][, ...] ) { ...fallback 本体... }
        /// 函数体为必填（原体保留为 fallback 本体，dll 绑定不可用时执行）。
        /// 匹配失败返回 null（原样透传，不影响其余编译流程）。
        /// </summary>
        private static MatchResult TryMatchDllImportDecl( string s, int atPos, int pos )
        {
            // ── attribute 实参列表：字符串字面量 + 逗号 ──
            int p = SkipSpaceAndComments(s, pos);
            if (p >= s.Length || s[p] != '(')
                return null;
            p++;
            int stringArgCount = 0;
            while (true)
            {
                p = SkipSpaceAndComments(s, p);
                if (p >= s.Length)
                    return null;
                if (s[p] == ')')
                {
                    p++;
                    break;
                }
                if (s[p] != '"')
                    return null;   // 只接管字符串实参形态
                int strEnd = SkipCommentOrString(s, p);
                if (strEnd <= p)
                    return null;
                stringArgCount++;
                p = strEnd;
                p = SkipSpaceAndComments(s, p);
                if (p < s.Length && s[p] == ',')
                {
                    p++;
                    continue;
                }
                if (p < s.Length && s[p] == ')')
                {
                    p++;
                    break;
                }
                return null;
            }
            int attrRParen = p - 1;
            if (stringArgCount < 2)
                return null;   // (路径, 符号) 两实参由现有 Meta 层校验，这里前置把关

            // ── static RetType FuncName( params ) ──
            string staticKw = ReadIdentifierAfterSpace(s, ref p);
            if (staticKw != "static")
                return null;

            string retType = ReadIdentifierAfterSpace(s, ref p);
            if (retType == null || retType == "static")
                return null;

            string funcName = ReadIdentifierAfterSpace(s, ref p);
            if (funcName == null)
                return null;

            p = SkipSpaceAndComments(s, p);
            if (p >= s.Length || s[p] != '(')
                return null;
            p++;

            var paramTypes = new List<string>();
            var paramNames = new List<string>();
            p = SkipSpaceAndComments(s, p);
            if (p < s.Length && s[p] == ')')
            {
                p++;   // 零参
            }
            else
            {
                while (true)
                {
                    string ptype = ReadIdentifierAfterSpace(s, ref p);
                    if (ptype == null)
                        return null;
                    string pname = ReadIdentifierAfterSpace(s, ref p);
                    if (pname == null)
                        return null;
                    paramTypes.Add(ptype);
                    paramNames.Add(pname);
                    p = SkipSpaceAndComments(s, p);
                    if (p >= s.Length)
                        return null;
                    if (s[p] == ',')
                    {
                        p++;
                        continue;
                    }
                    if (s[p] == ')')
                    {
                        p++;
                        break;
                    }
                    return null;
                }
            }
            int declRParen = p - 1;

            // ── 函数体：必填 '{ ... }'（原体保留为 fallback 本体）──
            // dll 绑定可用时转发 dll 导入函数；不可用（平台不符 / 加载失败 /
            // 符号不存在 -> 隐藏字段为 null）时执行原函数体。
            // 无体形态不接管：交由语法阶段按普通函数定义报"缺少函数体"。
            int after = SkipSpaceAndComments(s, p);
            if (after >= s.Length || s[after] != '{')
                return null;
            int bodyEnd = SkipBracedBlock(s, after);
            if (bodyEnd < 0)
                return null;   // 花括号未闭合：不接管，交由后续阶段报错
            int end = bodyEnd;

            // ── 组装修换文本（原文切片逐字符保留 → 换行结构不变，行号严格不变）──
            // midText 覆盖 [attrRParen+1, after)：参数表右括号与 '{' 之间的
            // 空白/注释/换行也原样保留，追加文本均为单行 → 换行数天然相等。
            string attrText = s.Substring(atPos, attrRParen + 1 - atPos);          // @DllImport( ... ) 原文
            string midText = s.Substring(attrRParen + 1, after - (attrRParen + 1)); // 空白+static Ret name(params)+空白 原文
            string bodyText = s.Substring(after, end - after);                     // 原函数体 { ... } 原文（fallback 本体）

            var funcType = new StringBuilder("Func<").Append(retType);
            for (int k = 0; k < paramTypes.Count; k++)
                funcType.Append(',').Append(paramTypes[k]);
            funcType.Append('>');

            string hidden = "__dll_" + funcName;
            bool isVoid = retType == "void" || retType == "Void";

            // wrapper 头：dll 绑定成功（隐藏字段非 null）-> 转发 dll 导入函数；
            // 失败 -> else 分支执行原函数体（fallback 本体原文逐字符保留）
            var wrapperHead = new StringBuilder(" { if ( ").Append(hidden).Append(" != null ) { ");
            if (!isVoid)
                wrapperHead.Append("ret ");
            wrapperHead.Append(hidden).Append('(');
            for (int k = 0; k < paramNames.Count; k++)
            {
                if (k > 0)
                    wrapperHead.Append(", ");
                wrapperHead.Append(paramNames[k]);
            }
            wrapperHead.Append(" ) } else ");

            var replacement = new StringBuilder();
            replacement.Append(attrText).Append(" static ").Append(funcType.ToString())
                .Append(' ').Append(hidden).Append(midText).Append(wrapperHead.ToString())
                .Append(bodyText).Append(" }");

            return new MatchResult { replacement = replacement.ToString(), end = end };
        }

        /// <summary>从 s[lbrace]（为 '{'）开始扫描平衡花括号块（跳过字符串/注释），返回右花括号后一位置；未闭合返回 -1。</summary>
        private static int SkipBracedBlock( string s, int lbrace )
        {
            int depth = 0;
            int i = lbrace;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '#' || c == '"' || c == '\'')
                {
                    i = SkipCommentOrString(s, i);
                    continue;
                }
                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                        return i + 1;
                }
                i++;
            }
            return -1;
        }

        /// <summary>
        /// 跳过 i 处开始的注释或字符串字面量，返回结束位置（不含）。
        /// 注释/字符串词法对齐 LexerParseToToken：
        /// '#' 行注释到 '\n'；'#'+! ... '!'+# 块注释（'#' 数匹配，可嵌套计级）；
        /// '"' 双引号串（'\\' 转义）；'"""' 三引号 f-string；'\'' 原始串（仅 '\'' 转义）。
        /// </summary>
        private static int SkipCommentOrString( string s, int i )
        {
            char c = s[i];
            if (c == '#')
            {
                int j = i + 1;
                int nsharp = 1;
                while (j < s.Length && s[j] == '#')
                {
                    nsharp++;
                    j++;
                }
                if (j < s.Length && s[j] == '!')
                {
                    // 块注释：闭合标记 '!' + nsharp 个 '#'
                    int k = j + 1;
                    while (k < s.Length)
                    {
                        if (s[k] == '!')
                        {
                            int m = k + 1;
                            int cnt = 0;
                            while (m < s.Length && s[m] == '#' && cnt < nsharp)
                            {
                                cnt++;
                                m++;
                            }
                            if (cnt == nsharp)
                                return m;
                            k++;
                        }
                        else
                        {
                            k++;
                        }
                    }
                    return s.Length;   // 未闭合：按 Lexer 行为跳到文件尾
                }
                // 行注释到 '\n'（不含换行）
                j = i + 1;
                while (j < s.Length && s[j] != '\n')
                    j++;
                return j;
            }
            if (c == '"')
            {
                // f""" 三引号字符串
                if (i + 2 < s.Length && s[i + 1] == '"' && s[i + 2] == '"')
                {
                    int j = i + 3;
                    while (j < s.Length)
                    {
                        if (s[j] == '"' && j + 2 < s.Length && s[j + 1] == '"' && s[j + 2] == '"')
                            return j + 3;
                        j++;
                    }
                    return s.Length;
                }
                // 普通字符串：'\\' 转义；跨行未闭合按 Lexer 容错停行尾
                int k = i + 1;
                while (k < s.Length)
                {
                    if (s[k] == '\\')
                    {
                        k += 2;
                        continue;
                    }
                    if (s[k] == '"')
                        return k + 1;
                    if (s[k] == '\n')
                        return k;
                    k++;
                }
                return s.Length;
            }
            if (c == '\'')
            {
                // 原始字符串：仅 '\'' 转义
                int k = i + 1;
                while (k < s.Length)
                {
                    if (s[k] == '\\')
                    {
                        k += 2;
                        continue;
                    }
                    if (s[k] == '\'')
                        return k + 1;
                    k++;
                }
                return s.Length;
            }
            return i + 1;
        }

        /// <summary>跳过空白与注释，返回下一个有效字符位置。</summary>
        private static int SkipSpaceAndComments( string s, int i )
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                {
                    i++;
                    continue;
                }
                if (c == '#')
                {
                    i = SkipCommentOrString(s, i);
                    continue;
                }
                return i;
            }
            return i;
        }

        /// <summary>跳过空白/注释后读一个标识符；非标识符开头返回 null 并停在原地有效字符处。</summary>
        private static string ReadIdentifierAfterSpace( string s, ref int i )
        {
            int p = SkipSpaceAndComments(s, i);
            if (p >= s.Length || !(char.IsLetter(s[p]) || s[p] == '_'))
            {
                i = p;
                return null;
            }
            int start = p;
            while (p < s.Length && IsIdentChar(s[p]))
                p++;
            i = p;
            return s.Substring(start, p - start);
        }

        private static bool IsIdentChar( char c )
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }
    }
}
