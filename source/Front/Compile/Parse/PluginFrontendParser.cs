//****************************************************************************
//  File:      PluginFrontendParser.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/9/25 12:00:00
//  Description:  @<tag>(){} 内联块的插件前端解析器宿主（语言无关，
//                PLUGIN_SYSTEM_DESIGN.md §A20 通用化契约）。
//                Front 与具体插件（C#/C/CUDA/Vulkan...）之间的唯一边界：
//                1) 从 SL 源文件目录逐级上溯定位 SLPlugin\<label> 插件根；
//                2) 读 plugin.jsonc 的 frontendLibs[0]（承载解析器的程序集）
//                   与 parserType / parserMethod（解析入口，缺省
//                   SLLabelParser.ParseLabel）；
//                3) 反射调用 string ParseLabel(string requestJson)（单 JSON
//                   参数契约：请求 PluginLabelRequest，响应 PluginLabelParse），
//                   Front 用 System.Text.Json 反序列化响应。
//                <- 通道解析在 Front（AtSignLabelSourceRewriter 初判 +
//                随请求下发布局），块体中段整编（import 提升、临时代码生成）
//                在插件；本宿主只做转发，不感知目标语言。
//                基础设施失败（找不到/加载失败/契约破裂）按标签只报一次
//                LID 20057（Assembly.LoadFrom 持文件锁，重试无意义），
//                之后该标签所有块透传降级（由后续 Lexer 报不认识语法）。
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
    }

    /// <summary>
    /// 请求 DTO：Front → 插件解析器的单 JSON 契约。HeadLineCount/TailLineCount
    /// 为 Front 初判的头尾区行数（bodyText.Split('\n') 0-based 段），
    /// InChannels/OutChannel 为 Front 初判的通道表；插件据此整编块体中段
    /// （[headLineCount, headLineCount+headExtra) 由插件协商吸收），
    /// 不得重复解析 &lt;- 通道语义，只做目标语言侧整合。
    /// </summary>
    public class PluginLabelRequest
    {
        /// <summary>标签名（= 插件 id = SLPlugin/&lt;label&gt; 目录名）。</summary>
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
    /// 目标库（空 = 插件自管，不进构建器）；HeadExtra/ExemptArrows 由
    /// Front 复核（铁律：&lt;- 只允许出现在头尾区与豁免行）。
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
        /// <summary>头区额外吸收行数（从 headLineCount 起算，如 import 行）；0 = 无。</summary>
        [JsonPropertyName("headExtra")]
        public int HeadExtra { get; set; }
        /// <summary>目标语言字符串/注释内含 "&lt;-" 的中段行号表（body 0-based 段）。</summary>
        [JsonPropertyName("exemptArrows")]
        public List<int> ExemptArrows { get; set; } = new List<int>();
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
    /// 插件前端解析器宿主（语言无关）：定位 SLPlugin\&lt;label&gt; → 加载
    /// frontendLibs 程序集 → 反射转调 ParseLabel(requestJson)。
    /// 解析器入口按标签缓存（进程内首次成功后复用）；基础设施失败也
    /// 缓存（同标签只报一次 LID 20057）。
    /// </summary>
    public static class PluginFrontendParser
    {
        private const string ManifestName = "plugin.jsonc";
        private const string FrontendLibsDirName = "frontendLibs";
        /// <summary>解析器类型缺省名（plugin.jsonc parserType 缺省值）。</summary>
        private const string DefaultParserTypeName = "SLLabelParser";
        /// <summary>解析器方法缺省名（plugin.jsonc parserMethod 缺省值）。</summary>
        private const string DefaultParserMethodName = "ParseLabel";

        private static readonly Dictionary<string, MethodInfo> s_ParseMethods =
            new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
        private static readonly HashSet<string> s_FailedLabels = new HashSet<string>(StringComparer.Ordinal);

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

        private static bool EnsureLoaded( string label, string sourceFilePath )
        {
            if (s_ParseMethods.TryGetValue(label, out MethodInfo cached))
                return true;
            if (s_FailedLabels.Contains(label))
                return false;

            string pluginRoot = FindPluginRoot(label, sourceFilePath);
            if (pluginRoot == null)
            {
                ReportFailed(label, "SLPlugin\\" + label + " not found (searched upward from '" + sourceFilePath + "')");
                return false;
            }
            ReadManifest(pluginRoot, out string libName, out string parserType, out string parserMethod);
            if (string.IsNullOrEmpty(libName))
            {
                ReportFailed(label, "plugin.jsonc 'frontendLibs[0]' missing in '" + pluginRoot + "'");
                return false;
            }
            if (string.IsNullOrEmpty(parserType))
                parserType = DefaultParserTypeName;
            if (string.IsNullOrEmpty(parserMethod))
                parserMethod = DefaultParserMethodName;
            string dllPath = Path.Combine(pluginRoot, FrontendLibsDirName, libName);
            if (!File.Exists(dllPath))
            {
                ReportFailed(label, "frontendLibs missing: '" + dllPath + "' (rebuild the plugin frontend assembly)");
                return false;
            }
            try
            {
                var asm = Assembly.LoadFrom(dllPath);
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
                return true;
            }
            catch (Exception ex)
            {
                ReportFailed(label, "Assembly.LoadFrom('" + dllPath + "') failed: " + ex.Message);
                return false;
            }
        }

        private static void ReportFailed( string label, string detail )
        {
            if (!s_FailedLabels.Add(label))
                return;
            Log.AddProcessLog(LID.ProcessAtSignLabelPluginParserLoadFailed,
                "@<" + label + ">(){} 插件前端解析器加载失败: " + detail, detail);
        }

        /// <summary>从 SL 源文件目录逐级上溯定位 SLPlugin\&lt;label&gt;（以 plugin.jsonc 存在为准）。</summary>
        private static string FindPluginRoot( string label, string sourceFilePath )
        {
            try
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath));
                while (dir != null)
                {
                    string candidate = Path.Combine(dir, "SLPlugin", label);
                    if (File.Exists(Path.Combine(candidate, ManifestName)))
                        return candidate;
                    dir = Path.GetDirectoryName(dir);
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        /// <summary>读 plugin.jsonc 的 frontendLibs[0]、parserType、parserMethod
        /// （JSONC：跳注释与尾逗号）；缺失字段给 null（调用方按缺省处理）。</summary>
        private static void ReadManifest( string pluginRoot, out string libName, out string parserType, out string parserMethod )
        {
            libName = null;
            parserType = null;
            parserMethod = null;
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
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
