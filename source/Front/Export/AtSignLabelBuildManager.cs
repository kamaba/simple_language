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

using SimpleLanguage.Core;
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

            var blocks = SimpleLanguage.Core.AtSignLabelBlockCollector.Blocks;
            if (blocks.Count == 0 || string.IsNullOrWhiteSpace(outDir))
            {
                return;
            }

            // 按标签分组（Source 空 = 插件自管，不进构建器）
            var groups = new Dictionary<string, List<SimpleLanguage.Core.AtSignLabelBlock>>(StringComparer.Ordinal);
            foreach (var block in blocks)
            {
                if (string.IsNullOrEmpty(block.Source))
                {
                    continue;
                }
                if (!groups.TryGetValue(block.Label, out var list))
                {
                    list = new List<SimpleLanguage.Core.AtSignLabelBlock>();
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

        private static void Dispatch( string label, List<SimpleLanguage.Core.AtSignLabelBlock> blocks, string outDir )
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

            // SL 类布局下发：收集本标签全部块入通道引用的 SL data/class，
            // 递归展开字段布局（插件 frontend 据此生成镜像 C# class）
            CollectSlTypes(request, blocks);

            // jsonc plugins.<id>.references / sources 下发：额外程序集引用
            //（拷入 libDir 供编译期扫描 + 运行期 assemblies_path 解析）与
            // 额外源码文件（读内容，插件与块体同批合并编译）
            CollectBuildExtras(request, label);

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
        // SL 类布局收集（镜像 class 下发）：Build 期 Meta 已装配，扫描本
        // 标签全部块入通道的类型标记，非 BCL 名（SL data/class 类名）查
        // ClassManager 递归展开字段布局，随构建请求 slTypes 下发；插件
        // frontend 据此生成公共字段 C# class（如 csharp_mono 的 namespace
        // SLAtSign），块内目标语言代码可直接引用。枚举/未解析名跳过
        // （标量白名单外查不到的不下发，运行期按 Object 兜底）。
        // ------------------------------------------------------------------

        /// <summary>入通道类型标记的标量白名单（含 C# BCL 别名与 SL Meta
        /// 标量名）；白名单外即 SL data/class 类名，进布局收集。</summary>
        private static readonly HashSet<string> s_CSharpBclTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Int32", "int", "int32", "i32",
            "Int64", "long", "int64", "i64",
            "Single", "float", "float32", "f32", "Float32",
            "Double", "double", "float64", "f64", "Float64",
            "String", "string",
            "Boolean", "bool", "boolean",
            "Object", "object",
        };

        /// <summary>扫描全部块入通道类型标记，收集 SL data/class 布局进请求 slTypes。</summary>
        private static void CollectSlTypes( SimpleLanguage.Compile.PluginLabelBuildRequest request,
            List<SimpleLanguage.Core.AtSignLabelBlock> blocks )
        {
            var collected = new Dictionary<string, SimpleLanguage.Compile.PluginLabelSlType>(StringComparer.Ordinal);
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            foreach (var block in blocks)
            {
                if (block.InChannels == null)
                {
                    continue;
                }
                foreach (var ch in block.InChannels)
                {
                    string slType = ch != null && ch.Length > 2 ? ch[2] : null;
                    if (string.IsNullOrEmpty(slType) || s_CSharpBclTypes.Contains(slType))
                    {
                        continue;
                    }
                    CollectSlType(slType, collected, visiting);
                }
            }
            request.SlTypes.AddRange(collected.Values);
        }

        /// <summary>按名解析 SL 类型并展开布局（data → exportMetaDataList；
        /// class → GetClassByName 短名兜底；均未命中（枚举/未知）跳过）。
        /// visiting 只防无限递归：环字段仍下发类型名（镜像类自引用/互引合法）。</summary>
        private static void CollectSlType( string name,
            Dictionary<string, SimpleLanguage.Compile.PluginLabelSlType> collected, HashSet<string> visiting )
        {
            if (collected.ContainsKey(name) || visiting.Contains(name))
            {
                return;
            }
            SimpleLanguage.Compile.PluginLabelSlType result = null;
            foreach (var d in ClassManager.instance.exportMetaDataList)
            {
                if (d == null || !string.Equals(d.name, name, StringComparison.Ordinal))
                {
                    continue;
                }
                visiting.Add(name);
                result = BuildSlType(name, "data", d.GetMetaMemberDataList(), collected, visiting);
                visiting.Remove(name);
                break;
            }
            if (result == null)
            {
                // class 路径：GetClassByName 的键是 allName_模板数（含模块前缀，
                // 如 "SpecialTest.AtSignCounter_0"），而入通道类型标记是纯类名，
                // 全名先试（支持显式模块限定名），落空后按短名扫全局类字典兜底
                // （引用模块 shell 无成员布局跳过；泛型类首期不支持，同
                // FindFirstMetaClassByShortName 口径）
                var mc = ClassManager.instance.GetClassByName(name);
                if (mc == null)
                {
                    foreach (var kv in ClassManager.instance.allClassDict)
                    {
                        var c = kv.Value;
                        if (c == null || c.refFromType == RefFromType.RefModule
                            || c.metaTemplateList.Count != 0
                            || !string.Equals(c.name, name, StringComparison.Ordinal))
                        {
                            continue;
                        }
                        mc = c;
                        break;
                    }
                }
                if (mc != null)
                {
                    visiting.Add(name);
                    result = BuildSlType(name, "class", mc.metaMemberVariableDict.Values, collected, visiting);
                    visiting.Remove(name);
                }
            }
            if (result != null)
            {
                collected[name] = result;
            }
        }

        /// <summary>展开单个 SL 类型布局（跳静态成员；字段类型名经
        /// FieldSlTypeName 归一并触发嵌套类递归收集）。</summary>
        private static SimpleLanguage.Compile.PluginLabelSlType BuildSlType( string name, string kind,
            IEnumerable<MetaVariable> members,
            Dictionary<string, SimpleLanguage.Compile.PluginLabelSlType> collected, HashSet<string> visiting )
        {
            var result = new SimpleLanguage.Compile.PluginLabelSlType { Name = name, Kind = kind };
            foreach (var mv in members)
            {
                if (mv == null || mv.isStatic)
                {
                    continue;
                }
                string fieldType = FieldSlTypeName(mv, collected, visiting);
                if (string.IsNullOrEmpty(fieldType))
                {
                    continue;
                }
                result.Fields.Add(new SimpleLanguage.Compile.PluginLabelSlField
                {
                    Name = mv.name,
                    SlType = fieldType,
                    IsArray = mv.isArray,
                });
            }
            return result;
        }

        /// <summary>字段 SL 类型名归一：优先声明类型；枚举 → Int32；数组 →
        /// 元素类型名（isArray 由调用方另行标记，CVM 首期编组传 null）；
        /// SL data/class 名 → 递归 CollectSlType 后返回类名；标量白名单
        /// 名原样直用（插件 MapCSType 同时识别 Float32/Float64 等 SL Meta 名）。</summary>
        private static string FieldSlTypeName( MetaVariable mv,
            Dictionary<string, SimpleLanguage.Compile.PluginLabelSlType> collected, HashSet<string> visiting )
        {
            var mt = mv.defineMetaType != null ? mv.defineMetaType : mv.realMetaType;
            if (mt == null)
            {
                return "Object";
            }
            string name;
            if (mt.IsArray())
            {
                var elems = mt.defineTemplateMetaTypeList;
                name = elems != null && elems.Count > 0 ? PlainTypeNameOf(elems[0]) : null;
            }
            else
            {
                name = PlainTypeNameOf(mt);
            }
            if (string.IsNullOrEmpty(name) || s_CSharpBclTypes.Contains(name))
            {
                return name ?? "Object";
            }
            CollectSlType(name, collected, visiting);
            return name;
        }

        /// <summary>MetaType → SL 类型名原文（枚举 Int32 化；标量 MetaClass.name
        /// 即 "Int32"/"Int64"/"Float32"/"Float64"/"String"/"Boolean"/"Object"）。</summary>
        private static string PlainTypeNameOf( MetaType mt )
        {
            if (mt == null)
            {
                return null;
            }
            if (mt.isEnum)
            {
                return "Int32";
            }
            if (mt.metaData != null)
            {
                return mt.metaData.name;
            }
            if (mt.metaClass != null)
            {
                return mt.metaClass.name;
            }
            return null;
        }

        // ------------------------------------------------------------------
        // jsonc plugins.<id>.references / sources 组装（额外引用与源码下发）
        // ------------------------------------------------------------------

        /// <summary>把 jsonc plugins 段声明的额外引用（references：拷入 libDir
        /// 供编译期 csc 自动扫描引用 + 运行期 mono assemblies_path 裸名解析，
        /// 与 SLAtSign.dll 部署同机制；并随请求下发绝对路径）与额外源码
        ///（sources：读文件内容下发，插件与块体同批合并编译）填进构建请求。
        /// 文件缺失/读取失败记日志跳过，不中断导出（csc 缺引用自然报错）。</summary>
        private static void CollectBuildExtras( SimpleLanguage.Compile.PluginLabelBuildRequest request, string label )
        {
            var section = FindPluginSection(label);
            if (section == null)
            {
                return;
            }
            foreach (var rel in section.References)
            {
                var abs = ResolveExtraPath(rel);
                if (abs == null)
                {
                    Log.AddIRLog(LID.ExportPluginLibCopyFailed,
                        "AtSignLabel: reference not found (skip): " + label + " " + rel);
                    continue;
                }
                request.References.Add(abs);
                if (!string.IsNullOrEmpty(request.LibDir))
                {
                    try
                    {
                        Directory.CreateDirectory(request.LibDir);
                        File.Copy(abs, Path.Combine(request.LibDir, Path.GetFileName(abs)), true);
                    }
                    catch (Exception e)
                    {
                        Log.AddIRLog(LID.ExportPluginLibCopyFailed,
                            "AtSignLabel: reference copy failed: " + label + " " + abs + " " + e.Message);
                    }
                }
            }
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rel in section.Sources)
            {
                var abs = ResolveExtraPath(rel);
                if (abs == null)
                {
                    Log.AddIRLog(LID.ExportPluginLibCopyFailed,
                        "AtSignLabel: source not found (skip): " + label + " " + rel);
                    continue;
                }
                string src;
                try
                {
                    src = File.ReadAllText(abs);
                }
                catch (Exception e)
                {
                    Log.AddIRLog(LID.ExportPluginLibCopyFailed,
                        "AtSignLabel: source read failed (skip): " + label + " " + abs + " " + e.Message);
                    continue;
                }
                // entryName 全局唯一防与条目 Entry_N / 镜像 SLMirrorTypes 撞名
                //（插件按 entryName + ".cs" 写盘编译）；同名文件追加序号
                var entryName = "CsSrc_" + Path.GetFileNameWithoutExtension(abs);
                for (int k = 2; usedNames.Contains(entryName); k++)
                {
                    entryName = "CsSrc_" + Path.GetFileNameWithoutExtension(abs) + "_" + k;
                }
                usedNames.Add(entryName);
                request.Sources.Add(new SimpleLanguage.Compile.PluginLabelBuildEntry
                {
                    EntryName = entryName,
                    Source = src,
                });
            }
        }

        /// <summary>按 label 找 jsonc plugins 段条目（id 忽略大小写，与
        /// ResolvePluginLibDir / FindDeployedDll 同口径）；未声明返回 null。</summary>
        private static ProjectConfig.PluginSection FindPluginSection( string label )
        {
            var plugins = ProjectManager.config?.Plugins;
            if (plugins == null)
            {
                return null;
            }
            foreach (var p in plugins)
            {
                if (p != null && string.Equals(p.Id, label, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }
            return null;
        }

        /// <summary>额外引用/源码路径解析：绝对路径直用；相对路径按 jsonc
        /// 目录（projectPath，缺省 CurrentDirectory，同 PluginLibExportManager
        /// 口径）拼；文件不存在返回 null。</summary>
        private static string ResolveExtraPath( string rel )
        {
            if (string.IsNullOrWhiteSpace(rel))
            {
                return null;
            }
            if (Path.IsPathRooted(rel))
            {
                return File.Exists(rel) ? Path.GetFullPath(rel) : null;
            }
            var baseDir = !string.IsNullOrWhiteSpace(ProjectManager.projectPath)
                ? ProjectManager.projectPath
                : Environment.CurrentDirectory;
            var abs = Path.GetFullPath(Path.Combine(baseDir, rel));
            return File.Exists(abs) ? abs : null;
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
