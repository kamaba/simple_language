//****************************************************************************
//  File:      AtSignLabelBuildManager.cs
//  ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/9/25 12:00:00
//  Description:  @<tag>(){} 内联块的导出期构建管理器（语言无关分发，
//                PLUGIN_SYSTEM_DESIGN.md §A20 通用化）。
//                把 AtSignLabelBlockCollector 登记的插件临时代码按块 Label
//                （= plugin.jsonc 声明的 plugin.id）分组，统一经
//                PluginFrontendParser.Build 转调对应插件的 frontend
//                构建器（编译工具链与部署过程
//                全在插件内，Front 不感知目标语言：csharp_mono 插件内为
//                .NET Framework csc 合并编译 SLAtSign.dll 并部署到插件
//                lib 目录，运行期靠 assemblies_path 兜底加载；后续
//                C/CUDA/Vulkan 插件按各自工具链在插件侧实现）。
//                Front 只组装上下文：outDir（模块包输出目录）与 libDir
//                （与运行期 CVM 同语义解析出的插件 lib 部署目录），
//                连同条目清单 entries[{entryName, source}] 打包下发。
//                响应按 kind 四态分流 LID 22135~22138；Source 为空的块
//                不进构建器（插件自管）；无构建入口的标签记告警不中断
//                导出。任何失败仅记日志不中断导出（运行期再告警）。
//                成功部署的产物按标签登记（DeployedDlls），供随后运行的
//                PluginLibExportManager 把 frontend 构建产物拷入模块包
//                plugins/<id>/ 并在 module.json plugins[].libs 做路径关联
//                （导出管线顺序保证先构建后拷贝，见 ExportLangManager）。
//****************************************************************************

using SimpleLanguage.Logging;
using SimpleLanguage.Project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SimpleLanguage.Export
{
    /// <summary>
    /// @<tag>(){} 块构建管理器：导出 module.json 前，按块 Label 分组
    /// AtSignLabelBlockCollector 登记的临时代码，统一转调对应插件
    /// frontend 工程的构建入口（builderType.builderMethod）。
    /// 插件块内中段代码引用的外部程序集（如 MathUtil）需用户自行放到
    /// 插件 lib 目录或对应运行时（如 mono 4.5 GAC）。
    /// </summary>
    public static class AtSignLabelBuildManager
    {
        // 插件构建响应 kind 协议值（与 SLLabelBuilder 等 frontend
        // 构建器约定一致；Front 按 kind 分流日志；buildFailed/contract
        // 及未知 kind 走 else 兜底 = LID 22136）
        private const string KindSuccess = "success";
        private const string KindNotFound = "notFound";
        private const string KindDeployFailed = "deployFailed";

        /// <summary>本趟导出的 frontend 构建产物登记（标签 → 部署后 dll
        /// 全路径；KindSuccess 时记录）。每趟 Run 起始清空，供随后运行的
        /// PluginLibExportManager 拷入模块包 plugins/&lt;id&gt;/ 并在
        /// module.json plugins[].libs 做路径关联。</summary>
        private static readonly Dictionary<string, string> s_DeployedDlls =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public static void Run( string outDir )
        {
            // 每趟导出重置部署登记（静态字典跨导出会话防残留）
            s_DeployedDlls.Clear();

            var blocks = SimpleLanguage.Compile.AtSignLabelBlockCollector.Blocks;
            if (blocks.Count == 0 || string.IsNullOrWhiteSpace(outDir))
            {
                return;
            }

            // 按标签分组（Source 空 = 插件自管，不进构建器）
            var groups = new Dictionary<string, List<SimpleLanguage.Compile.AtSignLabelBlock>>(StringComparer.Ordinal);
            foreach (var block in blocks)
            {
                if (string.IsNullOrEmpty(block.Source))
                {
                    continue;
                }
                if (!groups.TryGetValue(block.Label, out var list))
                {
                    list = new List<SimpleLanguage.Compile.AtSignLabelBlock>();
                    groups[block.Label] = list;
                }
                list.Add(block);
            }

            foreach (var pair in groups)
            {
                Dispatch(pair.Key, pair.Value, outDir);
            }
        }

        // ------------------------------------------------------------------
        // 统一分发：组装上下文（outDir/libDir）+ 条目清单，转调插件构建器，
        // 按响应 kind 分流 LID 22135~22138
        // ------------------------------------------------------------------

        private static void Dispatch( string label, List<SimpleLanguage.Compile.AtSignLabelBlock> blocks, string outDir )
        {
            var request = new SimpleLanguage.Compile.PluginLabelBuildRequest
            {
                Label = label,
                OutDir = outDir,
                LibDir = ResolvePluginLibDir(label, outDir, blocks[0].FilePath),
            };
            foreach (var block in blocks)
            {
                request.Entries.Add(new SimpleLanguage.Compile.PluginLabelBuildEntry
                {
                    EntryName = block.EntryClassName,
                    Source = block.Source,
                });
            }

            var build = SimpleLanguage.Compile.PluginFrontendParser.Build(
                label, blocks[0].FilePath, JsonSerializer.Serialize(request));
            if (build == null)
            {
                // 无构建入口（builder 未声明/程序集未加载成功，宿主已报）：
                // 记告警不中断（该标签条目运行期由插件 labelExec capability
                // 自行定位产物或报错）
                Log.AddIRLog(LID.ExportCscAtSignBuildFailed,
                    "AtSignLabel: no build handler for label '" + label
                        + "', " + blocks.Count + " block(s) skipped (plugin manages its own artifacts)");
                return;
            }

            // 按 kind 分流（error/dllPath/count 为插件侧诊断详情）
            if (string.Equals(build.Kind, KindSuccess, StringComparison.Ordinal))
            {
                // 登记部署产物，供随后运行的 PluginLibExportManager 拷入
                // 模块包并做 plugins[].libs 路径关联
                if (!string.IsNullOrEmpty(build.DllPath))
                {
                    s_DeployedDlls[label] = build.DllPath;
                }
                Log.AddIRLog(LID.ExportCscAtSignBuildSuccess,
                    "AtSignLabel: build success (" + build.Count + " block(s)): " + build.DllPath);
            }
            else if (string.Equals(build.Kind, KindNotFound, StringComparison.Ordinal))
            {
                Log.AddIRLog(LID.ExportCscAtSignCompilerNotFound,
                    "AtSignLabel: build skipped for label '" + label + "': " + build.Error);
            }
            else if (string.Equals(build.Kind, KindDeployFailed, StringComparison.Ordinal))
            {
                Log.AddIRLog(LID.ExportCscAtSignDeployFailed,
                    "AtSignLabel: deploy failed for label '" + label + "': " + build.Error);
            }
            else
            {
                // buildFailed / contract / 空·未知 kind（协议演进兜底）：
                // 构建失败不中断导出
                Log.AddIRLog(LID.ExportCscAtSignBuildFailed,
                    "AtSignLabel: build failed for label '" + label + "' (kind=" + build.Kind + "): " + build.Error);
            }
        }

        // ------------------------------------------------------------------
        // 部署登记查询：供随后运行的 PluginLibExportManager 把本趟构建的
        // frontend 产物拷入模块包 plugins/<id>/ 并在 module.json
        // plugins[].libs 做路径关联（pluginId = 块 Label = plugin.jsonc 的
        // plugin.id，忽略大小写，与 libDir 解析的 id 匹配同口径）
        // ------------------------------------------------------------------

        /// <summary>查本趟导出中该插件 frontend 构建并已部署的 dll 全路径；未构建返回 null。</summary>
        public static string FindDeployedDll( string pluginId )
        {
            if (string.IsNullOrEmpty(pluginId))
            {
                return null;
            }
            foreach (var pair in s_DeployedDlls)
            {
                if (string.Equals(pair.Key, pluginId, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------
        // libDir 解析（上下文组装，归 Front）：与运行期 CVM 同语义
        // （lib 直给值按包目录 outDir 解析），其次按插件清单声明的
        // plugin.id 路由插件根（与 @<tag> 路由同真源
        // PluginFrontendParser.FindPluginRootById），兜底从 outDir 逐级
        // 上溯找 simple_language_plugins\<label>\cvm\lib\windows-x64
        // （三目录标准布局，兼容旧扁平布局 <label>\lib\windows-x64）
        // ------------------------------------------------------------------

        private static string ResolvePluginLibDir( string label, string outDir, string filePath )
        {
            // 1) jsonc plugins 段该插件的 lib 直给值（如
            //    "../../../../simple_language_plugins/csharp_mono/cvm/lib/windows-x64/cvm_csharp_mono.dll"）：
            //    运行期 CVM 按 module.json 所在包目录解析，这里取同语义解析出 dll 目录
            var plugins = ProjectManager.config?.Plugins;
            if (plugins != null)
            {
                foreach (var p in plugins)
                {
                    if (p == null || !string.Equals(p.Id, label, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    var dir = ResolveLibDirFromValue(p.Lib, outDir);
                    if (dir != null)
                    {
                        return dir;
                    }
                    // 插件目录（Path）下：三目录布局 cvm\lib\windows-x64 优先，旧扁平 lib\windows-x64 兼容
                    if (!string.IsNullOrWhiteSpace(p.Path))
                    {
                        var pluginDir = Path.IsPathRooted(p.Path)
                            ? Path.GetFullPath(p.Path)
                            : Path.GetFullPath(Path.Combine(outDir, p.Path));
                        var d = Path.Combine(pluginDir, "cvm", "lib", "windows-x64");
                        if (Directory.Exists(d))
                        {
                            return d;
                        }
                        d = Path.Combine(pluginDir, "lib", "windows-x64");
                        if (Directory.Exists(d))
                        {
                            return d;
                        }
                    }
                }
            }

            // 2) 插件清单 id 索引解析插件根（plugin.id 路由，与 @<tag>
            //    路由同真源 FindPluginRootById，目录名仅缺 id 时兜底）：
            //    cvm\lib\windows-x64（三目录标准布局）优先，旧扁平兼容
            var pluginRoot = SimpleLanguage.Compile.PluginFrontendParser.FindPluginRootById(label, filePath);
            if (pluginRoot != null)
            {
                var pluginLib = Path.Combine(pluginRoot, "cvm", "lib", "windows-x64");
                if (Directory.Exists(pluginLib))
                {
                    return pluginLib;
                }
                pluginLib = Path.Combine(pluginRoot, "lib", "windows-x64");
                if (Directory.Exists(pluginLib))
                {
                    return pluginLib;
                }
            }

            // 3) 兜底：从 outDir 逐级上溯找 simple_language_plugins\<label>\cvm\lib\windows-x64
            //    （三目录标准布局；旧扁平 <label>\lib\windows-x64 兼容，如 echo）
            var cur = outDir;
            while (!string.IsNullOrEmpty(cur))
            {
                var d = Path.Combine(cur, "simple_language_plugins", label, "cvm", "lib", "windows-x64");
                if (Directory.Exists(d))
                {
                    return d;
                }
                d = Path.Combine(cur, "simple_language_plugins", label, "lib", "windows-x64");
                if (Directory.Exists(d))
                {
                    return d;
                }
                var parent = Path.GetDirectoryName(cur);
                if (parent == cur)
                {
                    break;
                }
                cur = parent;
            }
            return null;
        }

        private static string ResolveLibDirFromValue( string lib, string outDir )
        {
            if (string.IsNullOrWhiteSpace(lib))
            {
                return null;
            }
            try
            {
                var libPath = Path.IsPathRooted(lib)
                    ? Path.GetFullPath(lib)
                    : Path.GetFullPath(Path.Combine(outDir, lib));
                if (File.Exists(libPath))
                {
                    return Path.GetDirectoryName(libPath);
                }
                if (Directory.Exists(libPath))
                {
                    return libPath;
                }
            }
            catch
            {
                // malformed path
            }
            return null;
        }
    }
}
