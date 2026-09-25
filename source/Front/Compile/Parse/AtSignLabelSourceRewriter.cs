//****************************************************************************
//  File:      AtSignLabelSourceRewriter.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/9/25 12:00:00
//  Description:  @<tag>(){} 内联标签块的源码文本预改写器 + 块收集器
//                （PLUGIN_SYSTEM_DESIGN.md §A20 通用化实现，语言无关：
//                tag = 插件 id = SLPlugin/<tag> 目录名，块体中段整编在
//                对应插件 frontendLibs 解析器，Front 不感知目标语言，
//                C#/C/CUDA/Vulkan 插件一视同仁）。
//                三层分工（与 csharp_mono 旧链 CSharpCallXxx 的关键差异）：
//                1) <- 通道统一在 Front 解析：头区入通道 "var target <- $sl"
//                   与尾区出通道 "$sl <- expr" 只由 Front 初判与复核
//                   （双出通道 20056；中段 <- 铁律 20055，插件可回传
//                   exemptArrows 豁免目标语言字符串/注释内的 <-）；
//                2) 块体中段整编（import 提升、临时代码生成）在插件：
//                   请求携带通道表（单 JSON 契约，见 PluginFrontendParser），
//                   插件回传 headExtra（头区额外吸收行）与 Source/Dll；
//                3) 脱糖为哨兵系统调用 AtSignLabelCall/AtSignLabelCallVoid
//                   （首个实参 = 块收集器位置 entryIndex），IRCall 拦截发射
//                   CallAtSignLabel(124)，CVM 装配期按模块 atSignLabel[] 表
//                   经插件 labelExec capability 统一接收/传出变量。
//****************************************************************************

using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SimpleLanguage.Compile
{
    /// <summary>单个 @<tag>(){} 内联块的登记产物（插件临时代码 + 通道元数据）。</summary>
    public class AtSignLabelBlock
    {
        /// <summary>SL 源文件路径。</summary>
        public string FilePath { get; set; } = string.Empty;
        /// <summary>块起始行（1-based，'@' 标签行，用于诊断）。</summary>
        public int Line { get; set; }
        /// <summary>标签名（= 插件 id = SLPlugin/&lt;tag&gt; 目录名）。</summary>
        public string Label { get; set; } = string.Empty;
        /// <summary>块收集器列表位置（CallAtSignLabel payload 的 entryIndex 寻址）。</summary>
        public int EntryIndex { get; set; }
        /// <summary>条目名（Entry_N，全局唯一；与 EntryIndex 独立编号，失败块跳空无害）。</summary>
        public string EntryClassName { get; set; } = string.Empty;
        /// <summary>插件回传的临时代码全文（构建期生成目标库用；空 = 插件自管）。</summary>
        public string Source { get; set; } = string.Empty;
        /// <summary>插件回传的承载库名（空 = 运行期由插件自决）。</summary>
        public string DllName { get; set; } = string.Empty;
        /// <summary>目标语言端入口方法/符号名。</summary>
        public string EntryMethod { get; set; } = string.Empty;
        /// <summary>入通道 [target(插件侧形参), slVar(SL 变量)] 序列（与脱糖调用实参同序）。</summary>
        public List<string[]> InChannels { get; set; } = new List<string[]>();
        /// <summary>出通道 SL 变量名（null = 无出通道）。</summary>
        public string OutSlVar { get; set; }
        /// <summary>出通道目标语言侧表达式原文。</summary>
        public string OutExpr { get; set; } = string.Empty;
        /// <summary>小括号参数 [name, value] 序列（插件解析回传）。</summary>
        public List<string[]> LabelParams { get; set; } = new List<string[]>();
    }

    /// <summary>
    /// @<tag>(){} 块收集器：改写器登记块体生成的插件产物与通道元数据。
    /// 导出阶段两条消费路径：SLModulePackageWriter 把全部块按 EntryIndex
    /// 导出为 module.json "atSignLabel"[] 条目表（CVM 装配期绑定）；
    /// AtSignLabelBuildManager 把 Source 非空的块按 Label 分发给对应
    /// 构建 handler 生成目标库。每次项目编译开始（ProjectManager.Run）时 Clear。
    /// </summary>
    public static class AtSignLabelBlockCollector
    {
        private static readonly List<AtSignLabelBlock> s_Blocks = new List<AtSignLabelBlock>();
        private static int s_EntryCounter = 0;

        /// <summary>当前项目编译期登记的全部块（只读）。</summary>
        public static IReadOnlyList<AtSignLabelBlock> Blocks
        {
            get { return s_Blocks; }
        }

        public static void Clear()
        {
            s_Blocks.Clear();
            s_EntryCounter = 0;
        }

        /// <summary>
        /// 分配下一个全局唯一条目名（Entry_N）。
        /// 在插件解析前预分配：解析失败的块不登记但编号已消耗，
        /// 跳空无害（Entry_N 只要求唯一不要求连续）。
        /// </summary>
        public static string AllocateEntryName()
        {
            s_EntryCounter++;
            return "Entry_" + s_EntryCounter.ToString();
        }

        /// <summary>登记一个块（条目名须由 AllocateEntryName 预分配）；
        /// EntryIndex = 登记时的列表位置。</summary>
        public static AtSignLabelBlock Add( string filePath, int line, string label, string entryClassName )
        {
            var block = new AtSignLabelBlock();
            block.FilePath = filePath;
            block.Line = line;
            block.Label = label;
            block.EntryIndex = s_Blocks.Count;
            block.EntryClassName = entryClassName;
            s_Blocks.Add( block );
            return block;
        }
    }

    /// <summary>
    /// 把 @<tag>(){} 内联块改写为等价的哨兵系统调用（Token 解析前执行，
    /// Lexer~Meta 全管线零感知）。Front 只做"圈地 + 通道"：
    /// 识别标签（SLPlugin/&lt;tag&gt;/plugin.jsonc 存在即路由）、配平小括号
    /// 与大括号、原文截取参数表与块体、初判头尾区 &lt;- 通道行、转调插件
    /// 解析器整编中段并复核铁律、脱糖为
    /// <code>
    ///     outVar = AtSignLabelCall( entryIndex, inVar1, inVar2, ... )
    ///     AtSignLabelCallVoid( entryIndex, inVar1, ... )   # 无出通道
    /// </code>
    /// （裸赋值：SL 变量未声明时 Meta 层按右侧表达式自动定义，已声明则赋值）。
    /// <code>
    ///     @csharp_mono( 源=gpu )          # 小括号参数：语义由插件自决
    ///     {
    ///         var a <- $a                   # 头区：入通道（Front 初判）
    ///         var b <- $b
    ///         import SLCSharp;              # 头区延伸行：插件回传 headExtra=1 协商吸收
    ///         var c = MathUtil.Add( a, b );  # 中段：目标语言代码，<- 只允许出现在头尾区/豁免行
    ///         $c <- c;                      # 尾区：出通道（Front 初判）
    ///     }
    /// </code>
    /// 行号保持：多行块替换为单行脱糖语句后补齐原换行数（Lexer 的 '\r' 按
    /// 空白处理，只补 '\n' 不产生脏 Token）。解析错误报 20055/20056/20057
    /// 后整块原样透传（本错误在前更精确，符合"看最早 Error"）。
    /// 首期类型边界：通道值运行期按栈槽 kind 编组（atSignLabel[] 通道
    /// slType 留空），出通道 Int32 返回（AtSignLabelCall 哨兵 returnType）。
    /// </summary>
    public static class AtSignLabelSourceRewriter
    {
        /// <summary>进程级缓存：标签名 -> 是否存在 SLPlugin/&lt;tag&gt;/plugin.jsonc
        /// （编译期插件目录内容不变，且按源文件路径缓存 SLPlugin 父目录定位结果）。</summary>
        private static readonly Dictionary<string, bool> s_LabelCache = new Dictionary<string, bool>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> s_RootParentCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>改写 m_ContentBuffer 中的全部 @<tag>(){} 块；无匹配时原 buffer 返回。</summary>
        public static char[] Rewrite( char[] buffer, string filePath )
        {
            string source = new string(buffer);
            if (source.IndexOf('@') < 0)
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
                        if (IsPluginLabel(attrName, filePath))
                        {
                            var result = TryMatchBlock(source, i, j, attrName, filePath);
                            if (result != null)
                            {
                                sb.Append(result.replacement);
                                i = result.end;
                                rewrites++;
                                continue;
                            }
                        }
                    }
                    // 非插件标签或形态不匹配：'@' 原样透传
                    sb.Append(c);
                    i++;
                    continue;
                }
                sb.Append(c);
                i++;
            }

            if (rewrites == 0)
                return buffer;

            Log.AddProcessLog(LID.ProcessAtSignLabelRewrittenBlocks,
                "AtSignLabelSourceRewriter: '{0}' rewritten {1} @<tag>(){{}} block(s)", filePath, rewrites);
            return sb.ToString().ToCharArray();
        }

        /// <summary>标签路由：SLPlugin/&lt;tag&gt;/plugin.jsonc 存在即内联块标签（结果按标签名缓存）。</summary>
        private static bool IsPluginLabel( string tag, string filePath )
        {
            if (s_LabelCache.TryGetValue(tag, out bool known))
                return known;
            bool exists = false;
            string parent = FindPluginRootParent(filePath);
            if (parent != null && File.Exists(Path.Combine(parent, tag, "plugin.jsonc")))
                exists = true;
            s_LabelCache[tag] = exists;
            return exists;
        }

        /// <summary>从 SL 源文件目录逐级上溯定位 SLPlugin 父目录（按文件路径缓存）。</summary>
        private static string FindPluginRootParent( string sourceFilePath )
        {
            if (s_RootParentCache.TryGetValue(sourceFilePath, out string cached))
                return cached;
            string result = null;
            try
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath));
                while (dir != null)
                {
                    string candidate = Path.Combine(dir, "SLPlugin");
                    if (Directory.Exists(candidate))
                    {
                        result = candidate;
                        break;
                    }
                    dir = Path.GetDirectoryName(dir);
                }
            }
            catch (Exception)
            {
            }
            s_RootParentCache[sourceFilePath] = result;
            return result;
        }

        private class MatchResult
        {
            public string replacement;
            public int end;   // 替换覆盖区间 [atPos, end)
        }

        /// <summary>
        /// 尝试匹配 atPos 处开始的完整块：@tag ( 参数 ) { ...块体... }。
        /// 参数表与块体均原文截取；&lt;- 头尾通道由 Front 初判（ScanHeadTail），
        /// 中段整编转调插件前端解析器（PluginFrontendParser 单 JSON 契约），
        /// Front 按 headExtra/exemptArrows 复核铁律后报错（20055/20056）与脱糖。
        /// 基础设施失败由解析器宿主报 20057。任何失败均返回 null（整块透传）。
        /// </summary>
        private static MatchResult TryMatchBlock( string s, int atPos, int pos, string label, string filePath )
        {
            // ── 参数表 ( ... )：原文截取（形态语义在插件侧）──
            int p = SkipSpaceAndComments(s, pos);
            if (p >= s.Length || s[p] != '(')
                return null;
            int close = FindCloseParen(s, p);
            if (close < 0)
                return null;   // 参数表未闭合：不接管，交由后续阶段报错
            string paramsText = s.Substring(p + 1, close - p - 1);
            p = close + 1;

            // ── 块体 { ... }（花括号配平，跳字符串/注释）──
            int lbrace = SkipSpaceAndComments(s, p);
            if (lbrace >= s.Length || s[lbrace] != '{')
                return null;
            int bodyEnd = SkipBracedBlock(s, lbrace);
            if (bodyEnd < 0)
                return null;   // 花括号未闭合：不接管，交由后续阶段报错
            string bodyText = s.Substring(lbrace + 1, bodyEnd - 1 - (lbrace + 1));

            int startLine = 1 + CountNewlines(s, 0, atPos);
            // 块体行号基准 = '{' 所在行：bodyText 从 '{' 之后截取，其 Split('\n')
            // 段 0 即 '{' 行的行尾残余，故 body 0-based 段 i 换算为文件行时用
            // lbraceLine + i（'@' 标签与 '{' 分行时二者相差 1 行）。
            int lbraceLine = 1 + CountNewlines(s, 0, lbrace);

            // ── 头尾区初判（<- 通道铁律归 Front；双出通道直接报 20056）──
            var scan = ScanHeadTail(bodyText);
            if (scan.OutDupBodyLine >= 0)
            {
                int errLine = lbraceLine + scan.OutDupBodyLine;
                Log.AddProcessLog(LID.ProcessAtSignLabelOutChannelMultiple,
                    "@<" + label + ">(){} 块出通道变量首期仅支持 1 个: '" + filePath + "' 行 " + errLine
                        + " ($" + scan.OutSlVar + " 与 $" + scan.OutDupSlVar + " 冲突)",
                    filePath, errLine);
                return null;
            }

            // ── 先占 Entry_N（解析失败编号跳空无害，仅要求唯一），构造请求并转调插件 ──
            string entryName = AtSignLabelBlockCollector.AllocateEntryName();
            var request = new PluginLabelRequest
            {
                Label = label,
                EntryName = entryName,
                ParamsText = paramsText,
                BodyText = bodyText,
                HeadLineCount = scan.HeadLineCount,
                TailLineCount = scan.TailLineCount,
                InChannels = new List<PluginLabelChannel>(scan.InChannels.Count),
                OutChannel = scan.OutSlVar == null
                    ? null
                    : new PluginLabelChannel { SlVar = scan.OutSlVar, Target = scan.OutExpr },
            };
            foreach (var ch in scan.InChannels)
            {
                request.InChannels.Add(new PluginLabelChannel { Target = ch[0], SlVar = ch[1] });
            }
            var parse = PluginFrontendParser.Parse(label, filePath, JsonSerializer.Serialize(request));
            if (parse == null)
                return null;   // 基础设施失败：已报 LID 20057，整块透传降级

            // ── 业务错误分流（插件侧参数/块体整合）──
            if (!parse.Ok || !string.IsNullOrEmpty(parse.Error))
            {
                int errLine = parse.ErrorLine > 0 ? lbraceLine + parse.ErrorLine - 1 : startLine;
                Log.AddProcessLog(LID.ProcessAtSignLabelChannelSyntaxError,
                    "@<" + label + ">(){} 块解析错误: '" + filePath + "' 行 " + errLine + ": " + parse.Error,
                    filePath, errLine, parse.Error);
                return null;
            }

            // ── headExtra 复核：插件协商吸收的头区延伸行不得越过中段、不得含 <- ──
            int headExtra = parse.HeadExtra > 0 ? parse.HeadExtra : 0;
            int midStart = scan.HeadLineCount + headExtra;
            int midEnd = scan.Lines.Length - scan.TailLineCount;
            if (midStart > midEnd)
            {
                int errLine = lbraceLine + scan.HeadLineCount;
                Log.AddProcessLog(LID.ProcessAtSignLabelChannelSyntaxError,
                    "@<" + label + ">(){} 块 headExtra=" + headExtra + " 越界 (headLineCount="
                        + scan.HeadLineCount + ", tailLineCount=" + scan.TailLineCount
                        + ", 块体共 " + scan.Lines.Length + " 行): '" + filePath + "'",
                    filePath, errLine, "headExtra out of range");
                return null;
            }
            for (int bi = scan.HeadLineCount; bi < midStart; bi++)
            {
                if (scan.Lines[bi].Contains("<-"))
                {
                    int errLine = lbraceLine + bi;
                    Log.AddProcessLog(LID.ProcessAtSignLabelChannelSyntaxError,
                        "@<" + label + ">(){} 块 headExtra 区含 '<-'（<- 只允许出现在头尾区）: '"
                            + filePath + "' 行 " + errLine,
                        filePath, errLine, "headExtra line contains '<-'");
                    return null;
                }
            }

            // ── 中段复核：朴素扫 "<-"，跳过插件豁免行（目标语言字符串/注释内）──
            var exempt = new HashSet<int>(parse.ExemptArrows ?? (IEnumerable<int>)Array.Empty<int>());
            for (int bi = midStart; bi < midEnd; bi++)
            {
                if (exempt.Contains(bi))
                    continue;
                if (scan.Lines[bi].Contains("<-"))
                {
                    int errLine = lbraceLine + bi;
                    Log.AddProcessLog(LID.ProcessAtSignLabelChannelSyntaxError,
                        "@<" + label + ">(){} 块中段含 '<-'（<- 只允许出现在头尾区）: '"
                            + filePath + "' 行 " + errLine,
                        filePath, errLine, "body middle line contains '<-'");
                    return null;
                }
            }

            // ── 登记块（条目名已预分配）并回填插件产物 ──
            var block = AtSignLabelBlockCollector.Add(filePath, startLine, label, entryName);
            block.Source = parse.Source ?? string.Empty;
            block.DllName = parse.Dll ?? string.Empty;
            block.EntryMethod = parse.EntryMethod ?? string.Empty;
            block.InChannels = scan.InChannels;
            block.OutSlVar = scan.OutSlVar;
            block.OutExpr = scan.OutExpr;
            block.LabelParams = parse.LabelParams;

            // ── 脱糖 SL 语句（单行；裸赋值/裸调用，不带分号与现有无分号风格一致）──
            var call = new StringBuilder();
            if (scan.OutSlVar != null)
                call.Append(scan.OutSlVar).Append(" = AtSignLabelCall( ");
            else
                call.Append("AtSignLabelCallVoid( ");
            call.Append(block.EntryIndex.ToString());
            foreach (var ch in scan.InChannels)
            {
                call.Append(", ").Append(ch[1]);   // 实参 = SL 变量名（$ 后标识符）
            }
            call.Append(" )");

            // ── 行号保持：补齐原块换行数（'\r' 在 Lexer 按空白处理） ──
            int newlines = CountNewlines(s, atPos, bodyEnd);
            for (int k = 0; k < newlines; k++)
                call.Append('\n');

            return new MatchResult { replacement = call.ToString(), end = bodyEnd };
        }

        // ------------------------------------------------------------------
        // 头尾区初判（<- 通道铁律；body 0-based 段号体系）
        // ------------------------------------------------------------------

        private class HeadTailScan
        {
            public string[] Lines;
            public int HeadLineCount;
            public int TailLineCount;
            /// <summary>入通道 [target, slVar] 序列（与脱糖调用实参同序）。</summary>
            public List<string[]> InChannels = new List<string[]>();
            /// <summary>出通道 SL 变量名（null = 无出通道）。</summary>
            public string OutSlVar;
            /// <summary>出通道目标语言侧表达式原文。</summary>
            public string OutExpr;
            /// <summary>双出通道：后遇到的出通道行（body 0-based；-1 = 无）。</summary>
            public int OutDupBodyLine = -1;
            /// <summary>双出通道：后遇到的出通道 SL 变量名。</summary>
            public string OutDupSlVar;
        }

        /// <summary>
        /// 头尾区初判：头区从段首连续消费空白行与入通道行，尾区倒序连续消费
        /// 空白行与出通道行（不越过头区，防空块体重复计数）。中段
        /// [HeadLineCount, Lines.Length-TailLineCount) 的整编归插件，
        /// &lt;- 铁律由调用方复核（20055）。
        /// </summary>
        private static HeadTailScan ScanHeadTail( string bodyText )
        {
            var scan = new HeadTailScan();
            scan.Lines = bodyText.Split('\n');
            string[] lines = scan.Lines;

            // 头区：连续消费空白行与入通道行（遇首个非入通道实质行停止）
            int i = 0;
            while (i < lines.Length)
            {
                if (lines[i].Trim().Length == 0)
                {
                    i++;
                    continue;
                }
                var ch = TryMatchInChannelLine(lines[i]);
                if (ch == null)
                    break;
                scan.InChannels.Add(ch);
                i++;
            }
            scan.HeadLineCount = i;

            // 尾区：倒序连续消费空白行与出通道行（不越过头区）
            int k = lines.Length - 1;
            while (k >= scan.HeadLineCount)
            {
                if (lines[k].Trim().Length == 0)
                {
                    k--;
                    continue;
                }
                var och = TryMatchOutChannelLine(lines[k]);
                if (och == null)
                    break;
                if (scan.OutSlVar != null)
                {
                    scan.OutDupBodyLine = k;
                    scan.OutDupSlVar = och[0];
                    break;
                }
                scan.OutSlVar = och[0];
                scan.OutExpr = och[1];
                k--;
            }
            scan.TailLineCount = lines.Length - 1 - k;
            return scan;
        }

        /// <summary>入通道行匹配：[var] target &lt;- $sl [;]（var 后须空白，防 varx）。
        /// 匹配返回 [target(插件形参), slVar(SL 变量)]，否则 null。</summary>
        private static string[] TryMatchInChannelLine( string line )
        {
            string t = line.Trim();
            if (t.Length == 0)
                return null;
            if (t[t.Length - 1] == ';')
                t = t.Substring(0, t.Length - 1).TrimEnd();
            int arrow = t.IndexOf("<-");
            if (arrow < 0)
                return null;
            string left = t.Substring(0, arrow).Trim();
            string right = t.Substring(arrow + 2).Trim();
            if (left.StartsWith("var", StringComparison.Ordinal))
            {
                if (left.Length == 3 || !char.IsWhiteSpace(left[3]))
                    return null;
                left = left.Substring(3).Trim();
            }
            if (!IsIdent(left))
                return null;
            if (right.Length < 2 || right[0] != '$')
                return null;
            string slVar = right.Substring(1);
            if (!IsIdent(slVar))
                return null;
            return new string[] { left, slVar };
        }

        /// <summary>出通道行匹配：$sl &lt;- expr [;]（expr 非空且不以 $ 开头，
        /// 防误吞入通道形态）。匹配返回 [slVar(SL 写回目标), expr]，否则 null。</summary>
        private static string[] TryMatchOutChannelLine( string line )
        {
            string t = line.Trim();
            if (t.Length == 0)
                return null;
            if (t[t.Length - 1] == ';')
                t = t.Substring(0, t.Length - 1).TrimEnd();
            int arrow = t.IndexOf("<-");
            if (arrow < 0)
                return null;
            string left = t.Substring(0, arrow).Trim();
            string right = t.Substring(arrow + 2).Trim();
            if (left.Length < 2 || left[0] != '$')
                return null;
            string slVar = left.Substring(1);
            if (!IsIdent(slVar))
                return null;
            if (right.Length == 0 || right[0] == '$')
                return null;
            return new string[] { slVar, right };
        }

        /// <summary>SL 标识符校验（通道变量名：字母/下划线开头，仅字母数字下划线）。</summary>
        private static bool IsIdent( string s )
        {
            if (string.IsNullOrEmpty(s))
                return false;
            if (!char.IsLetter(s[0]) && s[0] != '_')
                return false;
            for (int i = 1; i < s.Length; i++)
            {
                if (!IsIdentChar(s[i]))
                    return false;
            }
            return true;
        }

        // ------------------------------------------------------------------
        // 圈地工具（SL 词法；块体中段整编在插件 frontendLibs）
        // ------------------------------------------------------------------

        /// <summary>从 s[p]（为 '('）起找参数表结束 ')'（首个，跳 SL 注释/字符串）；未找到返回 -1。</summary>
        private static int FindCloseParen( string s, int p )
        {
            int i = p + 1;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '#' || c == '"' || c == '\'')
                {
                    i = SkipCommentOrString(s, i);
                    continue;
                }
                if (c == ')')
                    return i;
                i++;
            }
            return -1;
        }

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
