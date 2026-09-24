//****************************************************************************
//  File:      CSharpMonoSourceRewriter.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/9/24 12:00:00
//  Description:  @csharp_mono(){} 内联 C# 块的源码文本预改写器 + 块收集器
//                （PLUGIN_SYSTEM_DESIGN.md §A20 工程分离式集成的首期实现：
//                Token 解析前把整块脱糖为 CSharpCallXxx 系统调用）
//****************************************************************************

using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    /// <summary>单个 @csharp_mono(){} 块的改写产物（C# 入口源码 + 元数据）。</summary>
    public class CSharpMonoBlock
    {
        /// <summary>SL 源文件路径。</summary>
        public string FilePath { get; set; } = string.Empty;
        /// <summary>块起始行（1-based，用于诊断）。</summary>
        public int Line { get; set; }
        /// <summary>生成的 C# 入口类名（Entry_N，全局唯一）。</summary>
        public string EntryClassName { get; set; } = string.Empty;
        /// <summary>生成的完整 C# 源码（供 csc 编译）。</summary>
        public string CSharpSource { get; set; } = string.Empty;
    }

    /// <summary>
    /// @csharp_mono(){} 块收集器：改写器登记块体生成的 C# 入口源码，
    /// 导出阶段由 CscAtSignBuildManager 统一 csc 编译为 SLAtSign.dll 并
    /// 部署到 csharp_mono 插件 lib 目录（运行期靠 assemblies_path 兜底加载）。
    /// 每次项目编译开始（ProjectManager.Run）时 Clear。
    /// </summary>
    public static class CSharpMonoBlockCollector
    {
        private static readonly List<CSharpMonoBlock> s_Blocks = new List<CSharpMonoBlock>();
        private static int s_EntryCounter = 0;

        /// <summary>当前项目编译期登记的全部块（只读）。</summary>
        public static IReadOnlyList<CSharpMonoBlock> Blocks
        {
            get { return s_Blocks; }
        }

        public static void Clear()
        {
            s_Blocks.Clear();
            s_EntryCounter = 0;
        }

        /// <summary>登记一个块并分配全局唯一入口类名（Entry_N）。</summary>
        public static CSharpMonoBlock Add( string filePath, int line )
        {
            s_EntryCounter++;
            var block = new CSharpMonoBlock();
            block.FilePath = filePath;
            block.Line = line;
            block.EntryClassName = "Entry_" + s_EntryCounter.ToString();
            s_Blocks.Add( block );
            return block;
        }
    }

    /// <summary>
    /// 把 @csharp_mono(){} 内联 C# 块改写为等价的 CSharpCallXxx 系统调用
    /// （Token 解析前执行，Lexer~Meta 全管线零感知）。块体 C# 源码经通道解析
    /// 后生成独立入口方法登记到 CSharpMonoBlockCollector，导出阶段统一编译。
    /// <code>
    ///     @csharp_mono()
    ///     {
    ///         var a <- $a          # 入通道：C# 形参 a 接收 SL 变量 a（§A4.5：箭头指向数据目的地）
    ///         var b <- $b
    ///         import SLCSharp;     # -> using SLCSharp;（块内 import 提升为 C# using）
    ///         var c = MathUtil.Add( a, b );
    ///         $c <- c;             # 出通道：C# 变量 c 作为 Run 返回值写回 SL 变量 c
    ///     }
    /// </code>
    /// 脱糖为（裸赋值：SL 变量未声明时 Meta 层按右侧表达式自动定义，已声明则赋值）：
    /// <code>
    ///     c = CSharpCallInt( "SLAtSign.dll", "SLAtSign", "Entry_1", "Run", 1, a, b )
    /// </code>
    /// 无出通道时脱糖为裸调用（ret_kind=0 走 CSharpCallVoid）。
    /// 行号保持：多行块替换为单行脱糖语句后补齐原换行数（Lexer 的 '\r' 按空白处理，
    /// 只补 '\n' 不产生脏 Token）。块内通道/import 行形态不完整时报错并整块原样
    /// 透传（后续 Lexer 会报不认识语法；本错误在前更精确，符合"看最早 Error"）。
    /// 首期类型边界：入通道形参一律 int（SL 侧按槽型自动编组），出通道一律
    /// int 返回（CSharpCallInt）。
    /// </summary>
    public static class CSharpMonoSourceRewriter
    {
        /// <summary>生成的托管程序集/命名空间名（CSharpCallXxx 前两元参）。</summary>
        public const string AssemblyName = "SLAtSign.dll";
        public const string NamespaceName = "SLAtSign";
        private const string EntryMethod = "Run";

        /// <summary>改写 m_ContentBuffer 中的 @csharp_mono(){} 块；无匹配时原 buffer 返回。</summary>
        public static char[] Rewrite( char[] buffer, string filePath )
        {
            string source = new string(buffer);
            if (source.IndexOf("@csharp_mono", StringComparison.Ordinal) < 0)
                return buffer;

            var sb = new StringBuilder(source.Length + 256);
            int i = 0;
            int len = source.Length;
            int rewrites = 0;
            while (i < len)
            {
                char c = source[i];
                // 注释与字符串字面量：整段原样透传（内部不识别块）
                if (c == '#' || c == '"' || c == '\'')
                {
                    int next = SkipCommentOrString(source, i);
                    sb.Append(source, i, next - i);
                    i = next;
                    continue;
                }
                if (c == '@')
                {
                    // @ 后必须紧跟字母/下划线才构成标签（对齐 LexerParseToToken.ReadAt）
                    int j = i + 1;
                    if (j < len && (char.IsLetter(source[j]) || source[j] == '_'))
                    {
                        int nameStart = j;
                        while (j < len && IsIdentChar(source[j]))
                            j++;
                        string attrName = source.Substring(nameStart, j - nameStart);
                        if (attrName == "csharp_mono")
                        {
                            var result = TryMatchBlock(source, i, j, filePath);
                            if (result != null)
                            {
                                sb.Append(result.replacement);
                                i = result.end;
                                rewrites++;
                                continue;
                            }
                        }
                    }
                    // 非 csharp_mono 标签或形态不匹配：'@' 原样透传
                    sb.Append(c);
                    i++;
                    continue;
                }
                sb.Append(c);
                i++;
            }

            if (rewrites == 0)
                return buffer;

            Log.AddProcessLog(LID.ProcessCSharpMonoSourceRewriterRewrittenBlocks,
                "CSharpMonoSourceRewriter: '{0}' rewritten {1} @csharp_mono(){{}} block(s)", filePath, rewrites);
            return sb.ToString().ToCharArray();
        }

        private class MatchResult
        {
            public string replacement;
            public int end;   // 替换覆盖区间 [atPos, end)
        }

        /// <summary>
        /// 尝试匹配 atPos 处开始的完整块：@csharp_mono ( ) { ...块体... }。
        /// 参数表首期必须为空；块体花括号配平（跳字符串/注释）。
        /// 块内通道/import 行形态不完整时报错并返回 null（整块透传）。
        /// </summary>
        private static MatchResult TryMatchBlock( string s, int atPos, int pos, string filePath )
        {
            // ── 空参数表 ( ) ──
            int p = SkipSpaceAndComments(s, pos);
            if (p >= s.Length || s[p] != '(')
                return null;
            p++;
            p = SkipSpaceAndComments(s, p);
            if (p >= s.Length || s[p] != ')')
                return null;
            p++;

            // ── 块体 { ... } ──
            int lbrace = SkipSpaceAndComments(s, p);
            if (lbrace >= s.Length || s[lbrace] != '{')
                return null;
            int bodyEnd = SkipBracedBlock(s, lbrace);
            if (bodyEnd < 0)
                return null;   // 花括号未闭合：不接管，交由后续阶段报错

            int startLine = 1 + CountNewlines(s, 0, atPos);

            // ── 解析块体（通道行 / import 行 / C# 中段行） ──
            var parse = ParseBlockBody(s, lbrace + 1, bodyEnd - 1);
            if (parse.error != null)
            {
                Log.AddProcessLog(LID.ProcessCSharpMonoChannelSyntaxError,
                    "@csharp_mono(){} 块通道语法错误: '" + filePath + "' 行 " + (startLine + parse.errorLine - 1) + ": " + parse.error,
                    filePath, startLine + parse.errorLine - 1, parse.error);
                return null;
            }
            if (parse.outCountError)
            {
                Log.AddProcessLog(LID.ProcessCSharpMonoOutChannelMultiple,
                    "@csharp_mono(){} 块出通道变量首期仅支持 1 个: '" + filePath + "' 行 " + startLine,
                    filePath, startLine);
                return null;
            }

            // ── 登记块（分配 Entry_N）并生成 C# 入口源码 ──
            var block = CSharpMonoBlockCollector.Add(filePath, startLine);
            block.CSharpSource = BuildCSharpSource(block.EntryClassName, parse);

            // ── 脱糖 SL 语句（单行；裸赋值/裸调用，不带分号与现有无分号风格一致） ──
            var call = new StringBuilder();
            if (parse.OutSlVar != null)
                call.Append(parse.OutSlVar).Append(" = CSharpCallInt( ");
            else
                call.Append("CSharpCallVoid( ");
            call.Append('"').Append(AssemblyName).Append('"').Append(", ")
                .Append('"').Append(NamespaceName).Append('"').Append(", ")
                .Append('"').Append(block.EntryClassName).Append('"').Append(", ")
                .Append('"').Append(EntryMethod).Append('"').Append(", ")
                .Append(parse.OutSlVar != null ? "1" : "0");
            foreach (var ch in parse.InChannels)
            {
                call.Append(", ").Append(ch[1]);   // 实参 = SL 变量名
            }
            call.Append(" )");

            // ── 行号保持：补齐原块换行数（'\r' 在 Lexer 按空白处理） ──
            int newlines = CountNewlines(s, atPos, bodyEnd);
            for (int k = 0; k < newlines; k++)
                call.Append('\n');

            return new MatchResult { replacement = call.ToString(), end = bodyEnd };
        }

        // ------------------------------------------------------------------
        // 块体解析
        // ------------------------------------------------------------------

        private class BlockParse
        {
            /// <summary>入通道：[C# 变量名, SL 变量名] 序列（形参表 + 实参表同序）。</summary>
            public List<string[]> InChannels = new List<string[]>();
            /// <summary>出通道 SL 变量名（null = 无出通道）。</summary>
            public string OutSlVar;
            /// <summary>出通道 C# 变量名（Run 的返回表达式）。</summary>
            public string OutCsVar;
            /// <summary>块内 import 提升的 using 命名空间列表。</summary>
            public List<string> Usings = new List<string>();
            /// <summary>C# 中段行（原样保留，含缩进与跨行语句）。</summary>
            public List<string> BodyLines = new List<string>();
            /// <summary>非空 = 通道/import 行形态错误。</summary>
            public string error;
            /// <summary>出通道行错误行号（块内 1-based，error 时有效）。</summary>
            public int errorLine;
            /// <summary>true = 出通道变量超过 1 个。</summary>
            public bool outCountError;
        }

        /// <summary>解析块体 [begin, end)：逐行识别入/出通道、import 与 C# 中段。</summary>
        private static BlockParse ParseBlockBody( string s, int begin, int end )
        {
            var parse = new BlockParse();
            if (begin >= end)
                return parse;   // 空块体（只有通道才有效，此处无通道也无中段）

            string body = s.Substring(begin, end - begin);
            string[] lines = body.Split('\n');
            for (int idx = 0; idx < lines.Length; idx++)
            {
                string raw = lines[idx];
                string t = raw.Trim(' ', '\t', '\r');
                if (t.Length == 0)
                {
                    parse.BodyLines.Add(raw);
                    continue;
                }
                if (t.StartsWith("var ", StringComparison.Ordinal))
                {
                    string[] ch = TryParseInChannel(t);
                    if (ch != null)
                    {
                        parse.InChannels.Add(ch);
                        continue;
                    }
                    // 非通道形态的 var 行：C# 局部变量声明，原样走中段
                    parse.BodyLines.Add(raw);
                    continue;
                }
                if (t.StartsWith("$", StringComparison.Ordinal))
                {
                    string[] ch = TryParseOutChannel(t);
                    if (ch != null)
                    {
                        if (parse.OutSlVar != null)
                        {
                            parse.outCountError = true;
                            parse.errorLine = idx + 1;
                            return parse;
                        }
                        parse.OutSlVar = ch[0];
                        parse.OutCsVar = ch[1];
                        continue;
                    }
                    parse.error = "出通道行形态须为 $<SL变量> <- <C#变量>（箭头指向数据目的地，右端不能带 $）";
                    parse.errorLine = idx + 1;
                    return parse;
                }
                if (t.StartsWith("import ", StringComparison.Ordinal))
                {
                    string ns = TryParseImport(t);
                    if (ns != null)
                    {
                        parse.Usings.Add(ns);
                        continue;
                    }
                    parse.error = "import 行形态须为 import <命名空间>;（块内 import 提升为 C# using）";
                    parse.errorLine = idx + 1;
                    return parse;
                }
                parse.BodyLines.Add(raw);
            }
            return parse;
        }

        /// <summary>匹配入通道行 "var &lt;cs&gt; <- $&lt;sl&gt; [;]"（完整匹配）；失败返回 null。</summary>
        private static string[] TryParseInChannel( string t )
        {
            int p = 4;   // "var " 之后
            string csName = ReadIdent(t, ref p);
            if (csName == null)
                return null;
            p = SkipInlineSpace(t, p);
            if (!MatchArrow(t, ref p))
                return null;
            p = SkipInlineSpace(t, p);
            if (p >= t.Length || t[p] != '$')
                return null;
            p++;
            string slName = ReadIdent(t, ref p);
            if (slName == null)
                return null;
            p = SkipInlineSpace(t, p);
            if (p < t.Length && t[p] == ';')
                p++;
            p = SkipInlineSpace(t, p);
            if (p != t.Length)
                return null;
            return new[] { csName, slName };
        }

        /// <summary>匹配出通道行 "$&lt;sl&gt; <- &lt;cs&gt; [;]"（完整匹配，右端禁 $）；失败返回 null。</summary>
        private static string[] TryParseOutChannel( string t )
        {
            int p = 1;   // '$' 之后
            string slName = ReadIdent(t, ref p);
            if (slName == null)
                return null;
            p = SkipInlineSpace(t, p);
            if (!MatchArrow(t, ref p))
                return null;
            p = SkipInlineSpace(t, p);
            string csName = ReadIdent(t, ref p);
            if (csName == null)
                return null;
            p = SkipInlineSpace(t, p);
            if (p < t.Length && t[p] == ';')
                p++;
            p = SkipInlineSpace(t, p);
            if (p != t.Length)
                return null;
            return new[] { slName, csName };
        }

        /// <summary>匹配 import 行 "import &lt;ns&gt;;"；返回命名空间，失败返回 null。</summary>
        private static string TryParseImport( string t )
        {
            int p = 7;   // "import " 之后
            int semi = t.IndexOf(';', p);
            if (semi < 0)
                return null;
            string ns = t.Substring(p, semi - p).Trim(' ', '\t');
            if (ns.Length == 0)
                return null;
            if (t.Substring(semi + 1).Trim(' ', '\t', '\r').Length != 0)
                return null;
            return ns;
        }

        // ------------------------------------------------------------------
        // C# 入口源码生成（C#5 兼容：csc v4.0.30319 编译）
        // ------------------------------------------------------------------

        private static string BuildCSharpSource( string entryName, BlockParse parse )
        {
            var sb = new StringBuilder(1024);
            sb.Append("using System;\n");
            foreach (var ns in parse.Usings)
                sb.Append("using ").Append(ns).Append(";\n");
            sb.Append('\n');
            sb.Append("namespace ").Append(NamespaceName).Append('\n');
            sb.Append("{\n");
            sb.Append("    public static class ").Append(entryName).Append('\n');
            sb.Append("    {\n");
            sb.Append("        public static ").Append(parse.OutSlVar != null ? "int" : "void").Append(" Run(");
            for (int k = 0; k < parse.InChannels.Count; k++)
            {
                if (k > 0)
                    sb.Append(", ");
                sb.Append("int ").Append(parse.InChannels[k][0]);
            }
            sb.Append(")\n");
            sb.Append("        {\n");
            foreach (var line in parse.BodyLines)
                sb.Append("            ").Append(line.TrimEnd('\r')).Append('\n');
            if (parse.OutSlVar != null)
                sb.Append("            return ").Append(parse.OutCsVar).Append(";\n");
            sb.Append("        }\n");
            sb.Append("    }\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        // ------------------------------------------------------------------
        // 文本工具（与 DllImportSourceRewriter 同词法，独立副本避免跨类依赖）
        // ------------------------------------------------------------------

        /// <summary>统计 [begin, end) 内 '\n' 个数（行号保持用）。</summary>
        private static int CountNewlines( string s, int begin, int end )
        {
            int count = 0;
            for (int i = begin; i < end; i++)
            {
                if (s[i] == '\n')
                    count++;
            }
            return count;
        }

        /// <summary>从 t[p] 起读一个标识符；非标识符开头返回 null（p 原地不动）。</summary>
        private static string ReadIdent( string t, ref int p )
        {
            if (p >= t.Length || !(char.IsLetter(t[p]) || t[p] == '_'))
                return null;
            int start = p;
            while (p < t.Length && IsIdentChar(t[p]))
                p++;
            return t.Substring(start, p - start);
        }

        /// <summary>跳过行内空白（空格/Tab/\r）。</summary>
        private static int SkipInlineSpace( string t, int p )
        {
            while (p < t.Length && (t[p] == ' ' || t[p] == '\t' || t[p] == '\r'))
                p++;
            return p;
        }

        /// <summary>匹配通道箭头 "&lt;-"（紧邻两字符）；命中则推进 p 并返回 true。</summary>
        private static bool MatchArrow( string t, ref int p )
        {
            if (p + 1 >= t.Length || t[p] != '<' || t[p + 1] != '-')
                return false;
            p += 2;
            return true;
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

        private static bool IsIdentChar( char c )
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }
    }
}
