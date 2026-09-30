//****************************************************************************
//  File:      AtSignLabelBlockDispatch.cs
//  ------------------------------------------------
//  DateTime: 2026/9/29
//  Description: @<tag>(...){...} 内联块的 MetaCore 层解析器（统一 @ 处理第二步:
//               代码段内的 @ 走 AtSignLabel 语义, 替代源码文本预改写器
//               AtSignLabelSourceRewriter——Lexer raw 捕获整块为不透明
//               AtSignBlock token → StructParse 透传为
//               FileMetaAtSignBlockSyntax → 本类在 HandleMetaSyntax 消费,
//               就地脱糖合成 FileMetaSyntax 节点平铺喂回语句链）
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SimpleLanguage.Core
{
    /// <summary>单个 @<tag>(){} 内联块的登记产物（插件临时代码 + 通道元数据）。</summary>
    public class AtSignLabelBlock
    {
        /// <summary>SL 源文件路径。</summary>
        public string FilePath { get; set; } = string.Empty;
        /// <summary>块起始行（1-based，'@' 标签行，用于诊断）。</summary>
        public int Line { get; set; }
        /// <summary>标签名（= plugin.jsonc 声明的 plugin.id，与插件目录名解耦）。</summary>
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
        /// <summary>入通道 [target(插件侧形参), slVar(SL 变量), slType(类型标记，显式必填
        /// —— C# 已定义类型名/SL 类名，var/缺省形态已禁用)] 序列（与脱糖调用实参同序）。</summary>
        public List<string[]> InChannels { get; set; } = new List<string[]>();
        /// <summary>出通道 SL 变量名（null = 无出通道）。</summary>
        public string OutSlVar { get; set; }
        /// <summary>出通道目标语言侧表达式原文。</summary>
        public string OutExpr { get; set; } = string.Empty;
        /// <summary>出通道类型标记原文（null = 缺省 int；脱糖选 ChannelOutInt/String 变体）。</summary>
        public string OutType { get; set; }
        /// <summary>小括号参数 [name, value] 序列（插件解析回传）。</summary>
        public List<string[]> LabelParams { get; set; } = new List<string[]>();
    }

    /// <summary>
    /// @<tag>(){} 块收集器：MetaCore 层解析器登记块体的插件产物与通道元数据。
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
    /// @<tag>(){} 内联块的 MetaCore 层解析器（HandleMetaSyntax 的
    /// FileMetaAtSignBlockSyntax case 转调入口）。Front 只做"通道 + 插件转调"：
    /// 标签路由（插件清单声明的 plugin.id 命中才接管, 未知标签报 20055）、
    /// 头尾区 &lt;- 通道初判（双出通道 20056）、转调插件解析器整编代码段
    /// （单 JSON 契约, Front 零处理）、登记块（导出期 atSignLabel[] 条目表），
    /// 就地脱糖合成语句节点平铺喂回当前语句链（不再改写源码文本）:
    /// <code>
    ///     <channelClassPath>.ChannelIn<Kind>( entryIndex, inVar1 )   # 每入通道一条（Kind 按类型标记）
    ///     AtSignLabelCallVoid( entryIndex )                           # 块执行哨兵（IRCall 拦截发射 CallAtSignLabel(124)）
    ///     outVar = <channelClassPath>.ChannelOut<Kind>( entryIndex )  # 有出通道时（裸赋值, 未声明时 Meta 层自动定义）
    /// </code>
    /// （出通道裸赋值：SL 变量未声明时 Meta 层按右侧表达式自动定义,
    /// 已声明则赋值；入通道类型标记必须显式——唯一合法形态
    /// "&lt;类型&gt; target &lt;- $slVar", var/缺省形态报 20055——Kind 白名单
    /// 分流：Int32/int→Int、Int64/long→Long、Single/float→Float、
    /// Double/double→Double、String/string→String、Boolean/bool→Boolean、
    /// 其余（Object 与 SL 类名）→Object；出通道 Kind
    /// "string"→String, 缺省/数值族→Int, 其余→Object）。
    /// </summary>
    public static class AtSignLabelBlockDispatch
    {
        /// <summary>进程级缓存：标签名 -> 是否命中插件清单声明的 plugin.id
        /// （编译期插件目录内容不变；id 索引缓存见 PluginFrontendParser）。</summary>
        private static readonly Dictionary<string, bool> s_LabelCache = new Dictionary<string, bool>(StringComparer.Ordinal);

        /// <summary>
        /// 消费一个 @<tag>(...){...} 块语法：通道扫描 → 插件转调 → 块登记 →
        /// 就地脱糖合成语句节点逐条喂回当前语句链（beforeStatements 平铺接入,
        /// 与 static if 的子语句平铺同模式, 不引入作用域）。
        /// 任何解析失败均只报错不喂语句（块降级丢弃, 已报的 LID 即本块错误）。
        /// </summary>
        public static void HandleDispatch( FileMetaAtSignBlockSyntax fms,
            MetaBlockStatements currentBlockStatements, ref MetaStatements beforeStatements )
        {
            if (fms == null)
                return;
            var fm = fms.fileMeta;
            var mainToken = fms.atSignBlockToken;
            if (fm == null || mainToken == null)
                return;
            string filePath = fm.path ?? "";
            string label = fms.label ?? "";
            int startLine = mainToken.sourceBeginLine;   // 1-based, '@' 标签行
            int lbraceLine = fms.lbraceLine;             // 1-based, '{' 所在行

            // ── 1. 标签路由：插件清单声明的 plugin.id 命中才接管 ──
            //    （旧改写器对未命中标签原样透传走 Attribute 管线; 新链路 Lexer 已按
            //     @<tag>(...){...} 形态整块捕获, 形态即语义, 未命中直接报 20055）
            if (!IsPluginLabel(label, filePath))
            {
                Log.AddProcessLog(LID.ProcessAtSignLabelChannelSyntaxError,
                    "@<" + label + ">(){} 块标签未命中任何插件清单声明的 plugin.id: '" + filePath + "' 行 " + startLine,
                    filePath, startLine);
                return;
            }

            // ── 2. 头尾区初判（<- 通道铁律归 Front；双出通道直接报 20056）──
            var scan = ScanHeadTail(fms.bodyText);
            if (scan.OutDupBodyLine >= 0)
            {
                int errLine = lbraceLine + scan.OutDupBodyLine;
                Log.AddProcessLog(LID.ProcessAtSignLabelOutChannelMultiple,
                    "@<" + label + ">(){} 块出通道变量首期仅支持 1 个: '" + filePath + "' 行 " + errLine
                        + " ($" + scan.OutSlVar + " 与 $" + scan.OutDupSlVar + " 冲突)",
                    filePath, errLine);
                return;
            }
            if (scan.InErrorBodyLine >= 0)
            {
                // 入通道形态非法（var/缺省/多段）：统一 20055 拒收整块
                int errLine = lbraceLine + scan.InErrorBodyLine;
                Log.AddProcessLog(LID.ProcessAtSignLabelChannelSyntaxError,
                    "@<" + label + ">(){} 块入通道语法错误: '" + filePath + "' 行 " + errLine + ": " + scan.InErrorText,
                    filePath, errLine, scan.InErrorText);
                return;
            }

            // ── 3. 先占 Entry_N（解析失败编号跳空无害，仅要求唯一），构造请求并转调插件 ──
            string entryName = AtSignLabelBlockCollector.AllocateEntryName();
            var request = new PluginLabelRequest
            {
                Label = label,
                EntryName = entryName,
                ParamsText = fms.paramsText,
                BodyText = fms.bodyText,
                HeadLineCount = scan.HeadLineCount,
                TailLineCount = scan.TailLineCount,
                InChannels = new List<PluginLabelChannel>(scan.InChannels.Count),
                OutChannel = scan.OutSlVar == null
                    ? null
                    : new PluginLabelChannel
                    {
                        SlVar = scan.OutSlVar,
                        Target = scan.OutExpr,
                        SlType = scan.OutType,
                    },
            };
            foreach (var ch in scan.InChannels)
            {
                request.InChannels.Add(new PluginLabelChannel
                {
                    Target = ch[0],
                    SlVar = ch[1],
                    SlType = ch.Length > 2 ? ch[2] : null,
                });
            }
            var parse = PluginFrontendParser.Parse(label, filePath, JsonSerializer.Serialize(request));
            if (parse == null)
                return;   // 基础设施失败：已报 LID 20057，块降级丢弃

            // ── 4. 业务错误分流（插件侧参数/块体整合）──
            if (!parse.Ok || !string.IsNullOrEmpty(parse.Error))
            {
                int errLine = parse.ErrorLine > 0 ? lbraceLine + parse.ErrorLine - 1 : startLine;
                Log.AddProcessLog(LID.ProcessAtSignLabelChannelSyntaxError,
                    "@<" + label + ">(){} 块解析错误: '" + filePath + "' 行 " + errLine + ": " + parse.Error,
                    filePath, errLine, parse.Error);
                return;
            }

            // ── 5. 登记块（条目名已预分配）并回填插件产物 ──
            var block = AtSignLabelBlockCollector.Add(filePath, startLine, label, entryName);
            block.Source = parse.Source ?? string.Empty;
            block.DllName = parse.Dll ?? string.Empty;
            block.EntryMethod = parse.EntryMethod ?? string.Empty;
            block.InChannels = scan.InChannels;
            block.OutSlVar = scan.OutSlVar;
            block.OutExpr = scan.OutExpr;
            block.OutType = scan.OutType;
            block.LabelParams = parse.LabelParams;

            // ── 6. 通道原语宿主类路径（plugin.jsonc refModule.channelClassPath）──
            string channelClass = PluginFrontendParser.GetChannelClassPath(label, filePath);
            if (channelClass == null && (scan.InChannels.Count > 0 || scan.OutSlVar != null))
            {
                Log.AddProcessLog(LID.ProcessAtSignLabelChannelClassMissing,
                    "@<" + label + ">(){} 块通道原语缺 channelClassPath: '" + filePath + "' 行 " + startLine
                        + "（plugin.jsonc refModule.channelClassPath 未配置：通道包装函数宿主类全名）",
                    filePath, startLine);
                return;
            }

            // ── 7. 就地脱糖合成语句节点，逐条平铺喂回当前语句链 ──
            //    （合成 token 行号统一用块主 token 位置: Token ctor 收 0-based 行
            //     故 sourceBeginLine-1 还原, 诊断指向 '@' 块头行）
            int line = mainToken.sourceBeginLine - 1;
            int pos = mainToken.sourceBeginChar;

            // 入通道: <channelClass>.ChannelIn<Kind>( entryIndex, slVar )
            foreach (var ch in scan.InChannels)
            {
                var argsPar = new Node(new Token(filePath, ETokenType.LeftPar, "(", line, pos)) { nodeType = ENodeType.Par };
                argsPar.endToken = new Token(filePath, ETokenType.RightPar, ")", line, pos);
                argsPar.AddChild(MakeNumberConstNode(filePath, line, pos, block.EntryIndex));
                argsPar.AddChild(new Node(new Token(filePath, ETokenType.Comma, ",", line, pos)) { nodeType = ENodeType.Comma });
                argsPar.AddChild(MakeIdentLinkNode(filePath, line, pos, ch[1]));
                var callNode = BuildChannelCallChain(filePath, channelClass,
                    "ChannelIn" + MapChannelInKind(ch.Length > 2 ? ch[2] : null), argsPar, line, pos);
                MetaMemberFunction.HandleMetaSyntax(currentBlockStatements, ref beforeStatements,
                    new FileMetaCallSyntax(new FileMetaCallLink(fm, callNode, true)));
            }

            // 块执行哨兵: AtSignLabelCallVoid( entryIndex )
            {
                var argsPar = new Node(new Token(filePath, ETokenType.LeftPar, "(", line, pos)) { nodeType = ENodeType.Par };
                argsPar.endToken = new Token(filePath, ETokenType.RightPar, ")", line, pos);
                argsPar.AddChild(MakeNumberConstNode(filePath, line, pos, block.EntryIndex));
                var callNode = MakeIdentLinkNode(filePath, line, pos, "AtSignLabelCallVoid");
                callNode.SetParNode(argsPar);
                MetaMemberFunction.HandleMetaSyntax(currentBlockStatements, ref beforeStatements,
                    new FileMetaCallSyntax(new FileMetaCallLink(fm, callNode, true)));
            }

            // 出通道: outVar = <channelClass>.ChannelOut<Kind>( entryIndex )（裸赋值, 未声明时自动定义）
            if (scan.OutSlVar != null)
            {
                var argsPar = new Node(new Token(filePath, ETokenType.LeftPar, "(", line, pos)) { nodeType = ENodeType.Par };
                argsPar.endToken = new Token(filePath, ETokenType.RightPar, ")", line, pos);
                argsPar.AddChild(MakeNumberConstNode(filePath, line, pos, block.EntryIndex));
                var callRoot = BuildChannelCallChain(filePath, channelClass,
                    "ChannelOut" + MapChannelOutKind(scan.OutType), argsPar, line, pos);
                var fme = FileMetatUtil.CreateFileMetaExpress(fm, new List<Node> { callRoot },
                    FileMetaTermExpress.EExpressType.Common);
                if (fme != null)
                {
                    var leftNode = MakeIdentLinkNode(filePath, line, pos, scan.OutSlVar);
                    var assignToken = new Token(filePath, ETokenType.Assign, "=", line, pos);
                    MetaMemberFunction.HandleMetaSyntax(currentBlockStatements, ref beforeStatements,
                        new FileMetaOpAssignSyntax(new FileMetaCallLink(fm, leftNode, true),
                            assignToken, null, null, null, null, fme, true));
                }
            }
        }

        /// <summary>标签路由：插件清单声明的 plugin.id 命中即内联块标签
        /// （转调 PluginFrontendParser.FindPluginRootById；缺 plugin.id 的
        /// 清单退回目录名匹配。结果按标签名缓存）。</summary>
        private static bool IsPluginLabel( string tag, string filePath )
        {
            if (s_LabelCache.TryGetValue(tag, out bool known))
                return known;
            bool exists = PluginFrontendParser.FindPluginRootById(tag, filePath) != null;
            s_LabelCache[tag] = exists;
            return exists;
        }

        // ------------------------------------------------------------------
        // 头尾区初判（<- 通道铁律；body 0-based 段号体系）
        // ------------------------------------------------------------------

        private class HeadTailScan
        {
            public string[] Lines;
            public int HeadLineCount;
            public int TailLineCount;
            /// <summary>入通道 [target, slVar, slType] 序列（与脱糖调用实参同序；
            /// slType 显式必填——C# 已定义类型名或 SL 类名，var/缺省形态已禁用）。</summary>
            public List<string[]> InChannels = new List<string[]>();
            /// <summary>出通道 SL 变量名（null = 无出通道）。</summary>
            public string OutSlVar;
            /// <summary>出通道目标语言侧表达式原文。</summary>
            public string OutExpr;
            /// <summary>出通道 SL 侧类型标记（"string $c &lt;- expr" 的 string；
            /// null = 缺省，脱糖选 Int 变体；"string" 选 String 变体；
            /// "object"/SL 类名选 Object 变体（P4 OBJECT 树镜像））。</summary>
            public string OutType;
            /// <summary>双出通道：后遇到的出通道行（body 0-based；-1 = 无）。</summary>
            public int OutDupBodyLine = -1;
            /// <summary>双出通道：后遇到的出通道 SL 变量名。</summary>
            public string OutDupSlVar;
            /// <summary>入通道形态非法：疑似通道行的 body 0-based 段号（-1 = 无）。</summary>
            public int InErrorBodyLine = -1;
            /// <summary>入通道形态非法的诊断文本。</summary>
            public string InErrorText;
        }

        /// <summary>
        /// 头尾区初判：头区从段首连续消费空白行与入通道行，尾区倒序连续消费
        /// 空白行与出通道行（不越过头区，防空块体重复计数）。代码段
        /// [HeadLineCount, Lines.Length-TailLineCount) 原样透传插件，
        /// Front 不做任何处理（目标语言规则由插件自决）。
        /// </summary>
        private static HeadTailScan ScanHeadTail( string bodyText )
        {
            var scan = new HeadTailScan();
            scan.Lines = bodyText.Split('\n');
            string[] lines = scan.Lines;

            // 头区：连续消费空白行与入通道行（遇首个非入通道实质行停止；
            // 疑似通道行形态非法（var/缺省/多段）记 InError，由 HandleDispatch 统一拒收）
            int i = 0;
            while (i < lines.Length)
            {
                if (lines[i].Trim().Length == 0)
                {
                    i++;
                    continue;
                }
                var ch = TryMatchInChannelLine(lines[i], out string inErr);
                if (ch == null)
                {
                    if (inErr != null)
                    {
                        scan.InErrorBodyLine = i;
                        scan.InErrorText = inErr;
                    }
                    break;
                }
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
                scan.OutType = och.Length > 2 ? och[2] : null;
                k--;
            }
            scan.TailLineCount = lines.Length - 1 - k;
            return scan;
        }

        /// <summary>入通道行匹配：唯一合法形态 &lt;slType&gt; target &lt;- $sl [;]
        ///（2 段显式类型标记；slType = C# 已定义类型名或 SL 类名，var 与
        /// 缺省形态已禁用）。右值非 $ 前缀 → 非通道行（代码段原文，null
        /// 无错）；$ 后非标识符 → 同样按非通道行透传。形态非法（1 段缺类型
        /// 标记 / 首段 var / 非 2 段）→ null + error（HandleDispatch 统一
        /// 报 20055 拒收整块）。匹配返回 [target(插件形参), slVar(SL
        /// 变量), slType(类型标记，恒非 null)]。</summary>
        private static string[] TryMatchInChannelLine( string line, out string error )
        {
            error = null;
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
            if (right.Length < 2 || right[0] != '$')
                return null;   // 右值非 $ 前缀：非通道行（代码段原文）
            // 左值分段：唯一合法 = 2 段 [slType, target]；var 与缺省形态已禁用
            string[] segs = left.Split( (char[])null, StringSplitOptions.RemoveEmptyEntries );
            if (segs.Length == 0)
            {
                error = "入通道形态非法 '" + t + "'（唯一合法形态: <类型> target <- $slVar）";
                return null;
            }
            if (segs.Length == 1)
            {
                error = "入通道缺类型标记 '" + segs[0] + " <- ...'（var 与缺省形态已禁用，须写显式类型：Int32/Int64/Single/Double/String/Boolean/Object 或 SL 类名）";
                return null;
            }
            if (segs[0] == "var")
            {
                error = "入通道禁止使用 var '" + t + "'（var 与缺省形态已禁用，须写显式类型：Int32/Int64/Single/Double/String/Boolean/Object 或 SL 类名）";
                return null;
            }
            if (segs.Length != 2)
            {
                error = "入通道形态非法 '" + t + "'（唯一合法形态: <类型> target <- $slVar）";
                return null;
            }
            string slType = segs[0];
            string target = segs[1];
            if (!IsIdent(slType) || !IsIdent(target))
            {
                error = "入通道类型标记或形参名非法 '" + t + "'";
                return null;
            }
            string slVar = right.Substring(1);
            if (!IsIdent(slVar))
                return null;   // $ 后非标识符：按非通道行透传（代码段原文）
            return new string[] { target, slVar, slType };
        }

        /// <summary>入通道类型标记 → 通道包装 Kind 后缀（Int/Long/Float/
        /// Double/String/Boolean/Object；决定脱糖调用 ChannelIn&lt;Kind&gt;
        /// 变体与 CVM 侧 AtSignChannelIn 的类型编组）。白名单外
        ///（Object 与 SL 类名）统一走 Object 对象编组（跨界镜像 class）。</summary>
        private static string MapChannelInKind( string slType )
        {
            if (string.IsNullOrEmpty(slType))
                return "Object";   // 理论不可达（TryMatchInChannelLine 已禁缺省）
            switch (slType)
            {
                case "Int32": case "int": case "int32": case "i32": return "Int";
                case "Int64": case "long": case "int64": case "i64": return "Long";
                case "Single": case "float": case "float32": case "f32": return "Float";
                case "Double": case "double": case "float64": case "f64": return "Double";
                case "String": case "string": return "String";
                case "Boolean": case "bool": case "boolean": return "Boolean";
                default: return "Object";   // Object/object 与 SL 类名统一走对象编组
            }
        }

        /// <summary>出通道类型标记 → 通道包装 Kind 后缀（Int/String/Object
        /// 三态；决定脱糖调用 ChannelOut&lt;Kind&gt; 变体与 CVM 侧会话值
        /// 读取）。缺省与数值族 → Int（既有行为：会话值按整型读）；
        /// String 族 → String；Object/object 与 SL 类名 → Object
        ///（P4 OBJECT 树镜像：会话值是 CVM 写方向构造的 SL 对象，
        /// slType 已过插件侧出通道白名单校验，此处不重复拦截）。</summary>
        private static string MapChannelOutKind( string slType )
        {
            switch (slType)
            {
                case "String": case "string":
                    return "String";
                case null:
                case "":
                case "Int32": case "int": case "int32": case "i32":
                case "Int64": case "long": case "int64": case "i64":
                case "Single": case "float": case "float32": case "f32":
                case "Double": case "double": case "float64": case "f64":
                    return "Int";
                default:
                    return "Object";   // Object/object 与 SL 类名
            }
        }

        /// <summary>出通道行匹配：[slType] $sl &lt;- expr [;]（expr 非空且不以 $
        /// 开头，防误吞入通道形态；slType 为可选类型前缀标识符，与入通道
        /// "var string s &lt;- $x" 的 slType 对称："string" 脱糖选
        /// ChannelOutString 变体，"object"/SL 类名选 ChannelOutObject，
        /// 缺省/数值族选 ChannelOutInt）。匹配返回
        /// [slVar(SL 写回目标), expr, slType(类型标记，可 null)]，否则 null。</summary>
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
            // 左值分段：1 段 = $sl；2 段 = slType $sl（类型前缀）。
            string[] segs = left.Split( (char[])null, StringSplitOptions.RemoveEmptyEntries );
            string slType = null;
            string dollar = null;
            if (segs.Length == 1)
            {
                dollar = segs[0];
            }
            else if (segs.Length == 2)
            {
                slType = segs[0];
                dollar = segs[1];
            }
            else
            {
                return null;
            }
            if (slType != null && !IsIdent(slType))
                return null;
            if (dollar.Length < 2 || dollar[0] != '$')
                return null;
            string slVar = dollar.Substring(1);
            if (!IsIdent(slVar))
                return null;
            if (right.Length == 0 || right[0] == '$')
                return null;
            return new string[] { slVar, right, slType };
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

        private static bool IsIdentChar( char c )
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        // ------------------------------------------------------------------
        // 脱糖节点合成工具（程序化构造 FileMeta 节点链的通用约定:
        // 链头 SetIdentifierNode 必须先设置, 否则 AddLinkNode 静默失效;
        // 单节点链无需 SetIdentifierNode）
        // ------------------------------------------------------------------

        /// <summary>合成 <paramref name="channelClass"/>.<paramref name="methodName"/>(args)
        /// 完整调用链根节点（channelClass 可含 '.' 多级命名空间; 单段类名走同一
        /// 算法——循环不执行, 链头直接挂尾方法）。</summary>
        private static Node BuildChannelCallChain( string path, string channelClass, string methodName,
            Node argsPar, int line, int pos )
        {
            // 尾方法节点: methodName( args )
            var methodNode = MakeIdentLinkNode(path, line, pos, methodName);
            methodNode.SetParNode(argsPar);
            // 类路径链: seg0.seg1....segN.methodName
            string[] segs = channelClass.Split('.');
            var root = MakeIdentLinkNode(path, line, pos, segs[0]);
            root.SetIdentifierNode(root);
            for (int i = 1; i < segs.Length; i++)
            {
                root.AddLinkNode(MakePeriodNode(path, line, pos));
                root.AddLinkNode(MakeIdentLinkNode(path, line, pos, segs[i]));
            }
            root.AddLinkNode(MakePeriodNode(path, line, pos));
            root.AddLinkNode(methodNode);
            return root;
        }

        /// <summary>合成 int 数值常量节点（token 形态对齐 Lexer 的 Number 产出:
        /// lexeme = int 值、extend = EType.Int32）。</summary>
        private static Node MakeNumberConstNode( string path, int line, int pos, int value )
        {
            var numToken = new Token(path, ETokenType.Number, value, line, pos, EType.Int32);
            return new Node(numToken) { nodeType = ENodeType.ConstValue };
        }

        private static Node MakeIdentLinkNode( string path, int line, int pos, string name )
        {
            return new Node(new Token(path, ETokenType.Identifier, name, line, pos)) { nodeType = ENodeType.IdentifierLink };
        }

        private static Node MakePeriodNode( string path, int line, int pos )
        {
            return new Node(new Token(path, ETokenType.Period, ".", line, pos)) { nodeType = ENodeType.Period };
        }
    }
}
