//****************************************************************************
//  File:      PluginFrontendParser.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/9/25 12:00:00
//  Description:  @<tag>(){} 内联块的插件前端宿主（语言无关，
//                PLUGIN_SYSTEM_DESIGN.md §A20 通用化契约）。
//                Front 与具体插件（C#/C/CUDA/Vulkan...）之间的唯一边界：
//                1) 从 SL 源文件目录逐级上溯定位 simple_language_plugins
//                   父目录，枚举各清单读 plugin.id 建索引，@<tag> 按声明的
//                   id 路由插件根（tag 与目录名解耦；缺 plugin.id 的旧
//                   清单退回目录名匹配）；
//                2) 读 plugin.jsonc 的 frontendLibs[0]（承载前端定制工程的
//                   程序集，位于插件 frontend/ 目录，标准三目录布局
//                   cvm/frontend/slang 见 PLUGIN_TEMPLATE.md）与两对入口声明：
//                   parserType / parserMethod（解析入口，缺省
//                   SLLabelParser.ParseLabel）与 builderType / builderMethod
//                   （构建入口，缺省 SLLabelBuilder.BuildLabels）；
//                   程序集加载优先复用 AppDomain 内同名已加载实例（调试
//                   宿主可能预加载，如 VS 直接工程引用），未命中才
//                   Assembly.LoadFrom；
//                3) 反射调用两个单 JSON 参数契约（请求/响应键名小驼峰对齐）：
//                   - Parse(string) -> PluginLabelParse：块体中段整编
//                     （import 提升、临时代码生成），<- 通道布局随请求下发；
//                   - Build(string) -> PluginLabelBuild：导出期把全部条目
//                     {entryName, source} 连同 outDir/libDir 上下文打包下发，
//                     由插件完成目标工具链编译与部署（编译过程不放 Front）。
//                <- 通道解析在 Front（AtSignLabelSourceRewriter 初判 +
//                随请求下发布局），块体中段整编与条目构建均在插件
//                frontend 工程；本宿主只做转发，不感知目标语言。
//                基础设施失败（找不到/加载失败/契约破裂）按标签只报一次
//                LID 20057（Assembly.LoadFrom 持文件锁，重试无意义），
//                之后该标签所有块透传降级（由后续 Lexer 报不认识语法）。
//                构建入口缺失（类型/方法不存在）不算失败：缓存 null，
//                Build 返回 null 由调用方按"插件自管产物"告警不中断。
//****************************************************************************

using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleLanguage.Compile
{
    /// <summary>
    /// 请求/响应共用通道 DTO：单个 &lt;- 变量通道。
    /// 入通道 "var target &lt;- $sl"：Target=插件侧形参名，SlVar=SL 变量名；
    /// 出通道 "$sl &lt;- expr"：SlVar=SL 写回目标，Target=目标语言侧表达式原文。
    /// </summary>
    public class PluginLabelChannel
    {
        [JsonPropertyName("target")]
        public string Target { get; set; }
        /// <summary>SL 侧变量名（$ 后的标识符）。</summary>
        [JsonPropertyName("slVar")]
        public string SlVar { get; set; }
        /// <summary>SL 侧类型标记原文（入通道 "var string s &lt;- $x" 的 string；
        /// 语言无关：语义由插件映射为目标语言类型；null/空 = 缺省（插件自决，
        /// csharp_mono 缺省 int，兼容无类型标记的既有块））。</summary>
        [JsonPropertyName("slType")]
        public string SlType { get; set; }
    }

    /// <summary>
    /// 请求 DTO：Front → 插件解析器的单 JSON 契约。HeadLineCount/TailLineCount
    /// 为 Front 初判的头尾区行数（bodyText.Split('\n') 0-based 段），
    /// InChannels/OutChannel 为 Front 初判的通道表；代码段
    /// [headLineCount, 行数-tailLineCount) 为目标语言原文，由插件自行
    /// 整编（含段首指令行吸收如 import/using），Front 零处理，
    /// 插件不得重复解析 &lt;- 通道语义，只做目标语言侧整合。
    /// </summary>
    public class PluginLabelRequest
    {
        /// <summary>标签名（= plugin.jsonc 声明的 plugin.id，与插件目录名解耦）。</summary>
        [JsonPropertyName("label")]
        public string Label { get; set; }
        /// <summary>条目名（Entry_N，全局唯一，由 Front 预分配）。</summary>
        [JsonPropertyName("entryName")]
        public string EntryName { get; set; }
        /// <summary>小括号参数表原文（形态语义由插件自决）。</summary>
        [JsonPropertyName("paramsText")]
        public string ParamsText { get; set; }
        /// <summary>大括号块体原文（'{' 之后到 '}' 之前，含头尾区行）。</summary>
        [JsonPropertyName("bodyText")]
        public string BodyText { get; set; }
        /// <summary>头区行数（含空白行；0-based 段计数）。</summary>
        [JsonPropertyName("headLineCount")]
        public int HeadLineCount { get; set; }
        /// <summary>尾区行数（含空白行；0-based 段计数）。</summary>
        [JsonPropertyName("tailLineCount")]
        public int TailLineCount { get; set; }
        /// <summary>入通道表（与脱糖调用实参同序）。</summary>
        [JsonPropertyName("inChannels")]
        public List<PluginLabelChannel> InChannels { get; set; } = new List<PluginLabelChannel>();
        /// <summary>出通道（null = 无出通道）。</summary>
        [JsonPropertyName("outChannel")]
        public PluginLabelChannel OutChannel { get; set; }
    }

    /// <summary>
    /// 响应 DTO：插件解析器 → Front。Source 非空时导出期经
    /// AtSignLabelBuildManager 分发给对应插件标签的构建 handler 生成
    /// 目标库（空 = 插件自管，不进构建器）；代码段的目标语言规则
    /// （含 &lt;- 语义与指令行位置）由插件自决，Front 不复核。
    /// </summary>
    public class PluginLabelParse
    {
        /// <summary>解析是否成功（成功时 Error/ErrorLine 无效）。</summary>
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }
        /// <summary>业务错误信息（null = 无；涵盖参数表形态、块体整合失败等）。</summary>
        [JsonPropertyName("error")]
        public string Error { get; set; }
        /// <summary>错误段号（bodyText.Split('\n') 1-based；0 = 与行号无关）。</summary>
        [JsonPropertyName("errorLine")]
        public int ErrorLine { get; set; }
        /// <summary>生成的临时代码全文（空 = 插件自管，不进构建器）。</summary>
        [JsonPropertyName("source")]
        public string Source { get; set; }
        /// <summary>承载该条目的库文件名（空 = 运行期由插件自决）。</summary>
        [JsonPropertyName("dll")]
        public string Dll { get; set; }
        /// <summary>条目名回显（冗余校验用）。</summary>
        [JsonPropertyName("entry")]
        public string Entry { get; set; }
        /// <summary>目标语言端入口方法/符号名。</summary>
        [JsonPropertyName("entryMethod")]
        public string EntryMethod { get; set; }
        /// <summary>小括号参数 [name, value] 序列（插件解析回传）。</summary>
        [JsonPropertyName("labelParams")]
        public List<string[]> LabelParams { get; set; } = new List<string[]>();
    }

    /// <summary>
    /// 构建请求条目 DTO：单个待构建条目（source = 插件解析器生成的
    /// 目标语言临时代码全文；entryName = Front 预分配的 Entry_N）。
    /// </summary>
    public class PluginLabelBuildEntry
    {
        [JsonPropertyName("entryName")]
        public string EntryName { get; set; }
        [JsonPropertyName("source")]
        public string Source { get; set; }
    }

    /// <summary>
    /// 构建请求 DTO：Front → 插件构建器的单 JSON 契约（导出期）。
    /// Front 只组装上下文（outDir = 模块包输出目录，libDir = 与运行期
    /// CVM 同语义解析出的插件 lib 部署目录），编译工具链与部署过程
    /// 全在插件 frontend 工程内完成。
    /// </summary>
    public class PluginLabelBuildRequest
    {
        /// <summary>标签名（= plugin.jsonc 声明的 plugin.id，与插件目录名解耦）。</summary>
        [JsonPropertyName("label")]
        public string Label { get; set; }
        /// <summary>模块包输出目录（module.json 所在目录）。</summary>
        [JsonPropertyName("outDir")]
        public string OutDir { get; set; }
        /// <summary>插件 lib 部署目录（null = Front 未能解析，插件按需分流）。</summary>
        [JsonPropertyName("libDir")]
        public string LibDir { get; set; }
        /// <summary>待构建条目清单（同标签全部块，合并构建与否由插件自决）。</summary>
        [JsonPropertyName("entries")]
        public List<PluginLabelBuildEntry> Entries { get; set; } = new List<PluginLabelBuildEntry>();
    }

    /// <summary>
    /// 构建响应 DTO：插件构建器 → Front。Kind 四态由 Front 分流日志
    /// （notFound→22135 / buildFailed·contract→22136 / deployFailed→22137
    /// / success→22138）；Error 为插件侧诊断详情（如 csc 输出）。
    /// </summary>
    public class PluginLabelBuild
    {
        /// <summary>契约是否正常完成（kind 语义可判读）。</summary>
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }
        /// <summary>结果态：success / notFound / buildFailed / deployFailed / contract。</summary>
        [JsonPropertyName("kind")]
        public string Kind { get; set; }
        /// <summary>诊断详情（null = 无；notFound/buildFailed/deployFailed 时有值）。</summary>
        [JsonPropertyName("error")]
        public string Error { get; set; }
        /// <summary>部署产物全路径（success 时有值）。</summary>
        [JsonPropertyName("dllPath")]
        public string DllPath { get; set; }
        /// <summary>构建条目数（success 时有值）。</summary>
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    /// <summary>
    /// 插件前端宿主（语言无关）：按清单声明的 plugin.id 路由插件根
    /// （FindPluginRootById，目录名仅作缺 id 时的兼容匹配）→ 加载 frontend
    /// 目录的程序集 → 反射转调 ParseLabel/BuildLabels（单 JSON 参数契约）。
    /// 两个入口按标签缓存（进程内首次成功后复用）；
    /// 基础设施失败也缓存（同标签只报一次 LID 20057）；构建入口缺失
    /// 缓存 null（插件自管产物，不算基础设施失败）。
    /// </summary>
    public static class PluginFrontendParser
    {
        private const string ManifestName = "plugin.jsonc";
        /// <summary>前端定制工程目录名（标准三目录布局 cvm/frontend/slang 之一）。</summary>
        private const string FrontendDirName = "frontend";
        /// <summary>解析器类型缺省名（plugin.jsonc parserType 缺省值）。</summary>
        private const string DefaultParserTypeName = "SLLabelParser";
        /// <summary>解析器方法缺省名（plugin.jsonc parserMethod 缺省值）。</summary>
        private const string DefaultParserMethodName = "ParseLabel";
        /// <summary>构建器类型缺省名（plugin.jsonc builderType 缺省值）。</summary>
        private const string DefaultBuilderTypeName = "SLLabelBuilder";
        /// <summary>构建器方法缺省名（plugin.jsonc builderMethod 缺省值）。</summary>
        private const string DefaultBuilderMethodName = "BuildLabels";

        private static readonly Dictionary<string, MethodInfo> s_ParseMethods =
            new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
        /// <summary>构建入口缓存：null 值 = 该标签无构建入口（插件自管产物）。</summary>
        private static readonly Dictionary<string, MethodInfo> s_BuildMethods =
            new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
        private static readonly HashSet<string> s_FailedLabels = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>插件清单索引缓存：plugins 父目录全路径 -> (plugin.id -> 插件根目录)。</summary>
        private static readonly Dictionary<string, Dictionary<string, string>> s_PluginIdIndex =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        /// <summary>通道原语类路径缓存：插件根全路径 -> refModule.channelClassPath
        ///（null 值 = 清单未配置；编译期清单内容不变，null 也缓存）。</summary>
        private static readonly Dictionary<string, string> s_ChannelClassPathCache =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 解析一个块（反射调用插件 parserType.parserMethod(requestJson)）。
        /// 返回 null = 基础设施失败（已报 LID 20057）；
        /// 业务结果（含 !Ok 的形态/整合错误）原样返回，由调用方分流报 20055。
        /// </summary>
        public static PluginLabelParse Parse( string label, string sourceFilePath, string requestJson )
        {
            if (!EnsureLoaded(label, sourceFilePath))
                return null;
            string json;
            try
            {
                json = (string)s_ParseMethods[label].Invoke(null, new object[] { requestJson });
            }
            catch (Exception ex)
            {
                ReportFailed(label, "ParseLabel invoke failed: " + ex.Message);
                return null;
            }
            if (string.IsNullOrEmpty(json))
            {
                ReportFailed(label, "ParseLabel returned empty result (contract broken)");
                return null;
            }
            try
            {
                return JsonSerializer.Deserialize<PluginLabelParse>(json);
            }
            catch (Exception ex)
            {
                ReportFailed(label, "ParseLabel result JSON invalid: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 构建一批条目（反射调用插件 builderType.builderMethod(requestJson)，
        /// 导出期由 AtSignLabelBuildManager 按标签分组后调用）。
        /// 返回 null = 无构建入口（插件未声明/程序集内找不到 builder，
        /// 调用方按"插件自管产物"告警不中断）或契约破裂（已报 LID 22136）；
        /// 业务结果（含各失败 kind）原样返回，由调用方分流 22135~22138。
        /// </summary>
        public static PluginLabelBuild Build( string label, string sourceFilePath, string requestJson )
        {
            if (!EnsureLoaded(label, sourceFilePath))
                return null;
            if (!s_BuildMethods.TryGetValue(label, out MethodInfo build) || build == null)
                return null;   // 无构建入口：插件自管产物，由调用方告警
            string json;
            try
            {
                json = (string)build.Invoke(null, new object[] { requestJson });
            }
            catch (Exception ex)
            {
                Log.AddIRLog(LID.ExportCscAtSignBuildFailed,
                    "AtSignLabel: build invoke failed for label '" + label + "': " + ex.Message);
                return null;
            }
            if (string.IsNullOrEmpty(json))
            {
                Log.AddIRLog(LID.ExportCscAtSignBuildFailed,
                    "AtSignLabel: build returned empty result (contract broken) for label '" + label + "'");
                return null;
            }
            try
            {
                return JsonSerializer.Deserialize<PluginLabelBuild>(json);
            }
            catch (Exception ex)
            {
                Log.AddIRLog(LID.ExportCscAtSignBuildFailed,
                    "AtSignLabel: build result JSON invalid for label '" + label + "': " + ex.Message);
                return null;
            }
        }

        private static bool EnsureLoaded( string label, string sourceFilePath )
        {
            if (s_ParseMethods.TryGetValue(label, out MethodInfo cached))
                return true;
            if (s_FailedLabels.Contains(label))
                return false;

            string pluginRoot = FindPluginRootById(label, sourceFilePath);
            if (pluginRoot == null)
            {
                ReportFailed(label, "plugin id '" + label + "' not found (no manifest declares it; searched upward from '" + sourceFilePath + "')");
                return false;
            }
            ReadManifest(pluginRoot, out string libName, out string parserType, out string parserMethod,
                out string builderType, out string builderMethod);
            if (string.IsNullOrEmpty(libName))
            {
                ReportFailed(label, "plugin.jsonc 'frontendLibs[0]' missing in '" + pluginRoot + "'");
                return false;
            }
            if (string.IsNullOrEmpty(parserType))
                parserType = DefaultParserTypeName;
            if (string.IsNullOrEmpty(parserMethod))
                parserMethod = DefaultParserMethodName;
            if (string.IsNullOrEmpty(builderType))
                builderType = DefaultBuilderTypeName;
            if (string.IsNullOrEmpty(builderMethod))
                builderMethod = DefaultBuilderMethodName;
            string dllPath = Path.Combine(pluginRoot, FrontendDirName, libName);
            try
            {
                // 优先复用当前 AppDomain 已加载的同名程序集：VS 调试宿主可能
                // 经工程引用预加载插件 frontend 程序集（断点调试场景），此时
                // 跳过 LoadFrom，既避免同一程序集双份类型（类型同一性断裂），
                // 也避开构建期文件锁；代价是磁盘上新编译的 dll 进程重启前不生效。
                var asm = FindLoadedAssembly(libName);
                if (asm == null)
                {
                    if (!File.Exists(dllPath))
                    {
                        ReportFailed(label, "frontend assembly missing: '" + dllPath + "' (rebuild the plugin frontend project)");
                        return false;
                    }
                    asm = Assembly.LoadFrom(dllPath);
                }
                var type = asm.GetType(parserType, false);
                var method = type == null
                    ? null
                    : type.GetMethod(parserMethod, BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    ReportFailed(label, "parser entry not found in '" + libName + "': " + parserType + "." + parserMethod);
                    return false;
                }
                s_ParseMethods[label] = method;
                // 构建入口：类型/方法缺失缓存 null（插件自管产物，不算失败）
                var builderEntry = ResolveEntry(asm, builderType, builderMethod);
                s_BuildMethods[label] = builderEntry;
                return true;
            }
            catch (Exception ex)
            {
                ReportFailed(label, "Assembly.LoadFrom('" + dllPath + "') failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>按简单名在当前 AppDomain 已加载程序集中查找插件 frontend
        /// 程序集（调试宿主可能经 VS 工程引用预加载，如 SLCSharpMonoFrontend）；
        /// 命中返回该实例（调用方复用，不再 LoadFrom），未命中返回 null。
        /// 跳过动态程序集。</summary>
        private static Assembly FindLoadedAssembly( string libName )
        {
            string simpleName = Path.GetFileNameWithoutExtension(libName);
            if (string.IsNullOrEmpty(simpleName))
                return null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic)
                    continue;
                if (string.Equals(asm.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase))
                    return asm;
            }
            return null;
        }

        /// <summary>解析程序集内的静态公共入口（string(string) 契约）；找不到返回 null。</summary>
        private static MethodInfo ResolveEntry( Assembly asm, string typeName, string methodName )
        {
            var type = asm.GetType(typeName, false);
            return type == null
                ? null
                : type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        }

        private static void ReportFailed( string label, string detail )
        {
            if (!s_FailedLabels.Add(label))
                return;
            Log.AddProcessLog(LID.ProcessAtSignLabelPluginParserLoadFailed,
                "@<" + label + ">(){} 插件前端解析器加载失败: " + detail, detail);
        }

        /// <summary>按插件清单声明的 plugin.id 路由插件根：从 SL 源文件目录
        /// 逐级上溯定位 simple_language_plugins 父目录，枚举各子目录清单的
        /// plugin.id 建索引（进程级缓存），@&lt;tag&gt; 命中即返回该插件根；
        /// 仅当目录名对应的清单存在且**未声明** plugin.id 时才退回目录名匹配
        /// （旧格式兼容）——清单声明了 id 即以 id 为唯一真源，目录名不再路由。
        /// 未找到返回 null。三处共用（本类 EnsureLoaded /
        /// AtSignLabelSourceRewriter.IsPluginLabel /
        /// AtSignLabelBuildManager.ResolvePluginLibDir），保证路由真源一致。</summary>
        public static string FindPluginRootById( string tag, string sourceFilePath )
        {
            if (string.IsNullOrEmpty(tag))
                return null;
            string parent = FindPluginsParent(sourceFilePath);
            if (parent == null)
                return null;
            var index = GetPluginIdIndex(parent);
            if (index.TryGetValue(tag, out string root))
                return root;
            string byDir = Path.Combine(parent, tag);
            return File.Exists(Path.Combine(byDir, ManifestName)) && ReadPluginId(byDir) == null
                ? byDir
                : null;
        }

        /// <summary>读插件清单 refModule.channelClassPath（@<tag>(){} 通道
        /// 原语包装函数的宿主类全名：Front 脱糖按此生成
        /// ChannelIn/ChannelOut 类路径调用，函数体在插件 refModule 内部
        /// 转调 Core 域 AtSignChannelIn/Out 系统方法，<- 赋值不再写死进
        /// opcode 124）；标签未路由到插件或清单未配置返回 null（带通道的
        /// 块由此报 LID 20058）。按插件根进程级缓存（null 也缓存）。</summary>
        public static string GetChannelClassPath( string tag, string sourceFilePath )
        {
            string root = FindPluginRootById(tag, sourceFilePath);
            if (root == null)
                return null;
            if (s_ChannelClassPathCache.TryGetValue(root, out string cached))
                return cached;
            string path = ReadChannelClassPath(root);
            s_ChannelClassPathCache[root] = path;
            return path;
        }

        /// <summary>读 plugin.jsonc 的 refModule.channelClassPath（JSONC：跳注释
        /// 与尾逗号）；无 refModule 段/非字符串/解析失败返回 null。</summary>
        private static string ReadChannelClassPath( string pluginRoot )
        {
            try
            {
                string manifest = File.ReadAllText(Path.Combine(pluginRoot, ManifestName));
                var options = new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                using (var doc = JsonDocument.Parse(manifest, options))
                {
                    if (doc.RootElement.TryGetProperty("refModule", out var refModule) &&
                        refModule.ValueKind == JsonValueKind.Object &&
                        refModule.TryGetProperty("channelClassPath", out var path) &&
                        path.ValueKind == JsonValueKind.String)
                    {
                        return path.GetString();
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        /// <summary>从 SL 源文件目录逐级上溯定位 simple_language_plugins 目录；未找到返回 null。</summary>
        private static string FindPluginsParent( string sourceFilePath )
        {
            try
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath));
                while (dir != null)
                {
                    string candidate = Path.Combine(dir, "simple_language_plugins");
                    if (Directory.Exists(candidate))
                        return candidate;
                    dir = Path.GetDirectoryName(dir);
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        /// <summary>扫描 plugins 目录下所有含 plugin.jsonc 的子目录，读各清单的
        /// plugin.id 建 (id -> 插件根目录) 索引；按父目录进程级缓存（插件
        /// 集在单次编译内不变）。同 id 重复声明取先注册者。扫描失败给空表。</summary>
        private static Dictionary<string, string> GetPluginIdIndex( string parent )
        {
            if (s_PluginIdIndex.TryGetValue(parent, out var cached))
                return cached;
            var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var dir in Directory.GetDirectories(parent))
                {
                    if (!File.Exists(Path.Combine(dir, ManifestName)))
                        continue;
                    string id = ReadPluginId(dir);
                    if (!string.IsNullOrEmpty(id) && !index.ContainsKey(id))
                        index[id] = dir;
                }
            }
            catch (Exception)
            {
            }
            s_PluginIdIndex[parent] = index;
            return index;
        }

        /// <summary>读插件清单 plugin 段的 id 字段（JSONC：跳注释与尾逗号）；
        /// 无 plugin 段/非字符串/解析失败返回 null。</summary>
        private static string ReadPluginId( string pluginRoot )
        {
            try
            {
                string manifest = File.ReadAllText(Path.Combine(pluginRoot, ManifestName));
                var options = new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                using (var doc = JsonDocument.Parse(manifest, options))
                {
                    if (doc.RootElement.TryGetProperty("plugin", out var plugin) &&
                        plugin.ValueKind == JsonValueKind.Object &&
                        plugin.TryGetProperty("id", out var id) &&
                        id.ValueKind == JsonValueKind.String)
                    {
                        return id.GetString();
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        /// <summary>读 plugin.jsonc 的 frontendLibs[0]、parserType、parserMethod、
        /// builderType、builderMethod（JSONC：跳注释与尾逗号）；缺失字段给
        /// null（调用方按缺省处理）。frontendLibs 键名保留（清单字段），
        /// 程序集从插件 frontend 目录加载。</summary>
        private static void ReadManifest( string pluginRoot, out string libName, out string parserType,
            out string parserMethod, out string builderType, out string builderMethod )
        {
            libName = null;
            parserType = null;
            parserMethod = null;
            builderType = null;
            builderMethod = null;
            try
            {
                string manifest = File.ReadAllText(Path.Combine(pluginRoot, ManifestName));
                var options = new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                using (var doc = JsonDocument.Parse(manifest, options))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("frontendLibs", out var libs) &&
                        libs.ValueKind == JsonValueKind.Array && libs.GetArrayLength() > 0)
                    {
                        libName = libs[0].GetString();
                    }
                    if (root.TryGetProperty("parserType", out var pt) && pt.ValueKind == JsonValueKind.String)
                    {
                        parserType = pt.GetString();
                    }
                    if (root.TryGetProperty("parserMethod", out var pm) && pm.ValueKind == JsonValueKind.String)
                    {
                        parserMethod = pm.GetString();
                    }
                    if (root.TryGetProperty("builderType", out var bt) && bt.ValueKind == JsonValueKind.String)
                    {
                        builderType = bt.GetString();
                    }
                    if (root.TryGetProperty("builderMethod", out var bm) && bm.ValueKind == JsonValueKind.String)
                    {
                        builderMethod = bm.GetString();
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
