//****************************************************************************
//  File:      PluginLibExportManager.cs
// ------------------------------------------------
//  Description:  resolve plugin "lib" object (PLUGIN_SYSTEM_DESIGN.md §4.2)
//                with the 4-level platform dir fallback
//                (lib/<os>-<arch>/ → lib/<os>/ → lib/<arch>/ → lib/)
//                and copy the selected libraries to
//                out/export/<Module>/plugins/<id>/ so the package is
//                self-contained. Only relative filenames are recorded in
//                module.json ("plugins/<id>/<file>").
//****************************************************************************

using SimpleLanguage.Logging;
using SimpleLanguage.Project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SimpleLanguage.Export
{
    /// <summary>
    /// 插件平台库导出管理器（project.jsonc "plugins".&lt;id&gt;.lib 对象）：
    /// 导出 module.json 前，按四级目录回退选中平台库目录，把命中库拷到
    /// outDir/plugins/&lt;id&gt;/，并写回 PluginSection.Lib（首个主库）与
    /// Libs（全量，module.json 的 plugins[].libs 为包内路径关联记录，
    /// CVM 装配只消费 lib 主库）。本趟 AtSignLabelBuildManager 部署的
    /// frontend 构建产物（如 SLAtSign.dll）同样拷入 plugins/&lt;id&gt;/
    /// 并追加进 Libs 关联。lib 为字符串直给时不经此处（不读磁盘不拷贝）。
    /// 任何失败仅记日志不中断导出（CVM 运行期按 onUnavailable 降级）。
    /// </summary>
    public static class PluginLibExportManager
    {
        public static void Run(string outDir)
        {
            var plugins = ProjectManager.config?.Plugins;
            if (plugins == null || plugins.Count == 0 || string.IsNullOrWhiteSpace(outDir))
            {
                return;
            }

            var projectDir = !string.IsNullOrWhiteSpace(ProjectManager.projectPath)
                ? ProjectManager.projectPath
                : Environment.CurrentDirectory;

            foreach (var p in plugins)
            {
                // 只处理 lib 对象声明；字符串直给 / enabled=false 跳过
                if (p == null || string.IsNullOrWhiteSpace(p.Id) || p.LibSpec == null || !p.Enabled)
                {
                    continue;
                }
                try
                {
                    ResolveOne(p, projectDir, outDir);
                }
                catch (Exception e)
                {
                    Log.AddIRLog(LID.ExportPluginLibCopyFailed,
                        "plugin: lib resolve failed: " + p.Id + " " + e.Message);
                }
            }
        }

        private static void ResolveOne(ProjectConfig.PluginSection p, string projectDir, string outDir)
        {
            var spec = p.LibSpec;

            // 1) 解析插件目录：绝对路径 → 相对 jsonc 目录 → 从 jsonc 目录逐级上溯
            //   （path 常写 "SLPlugin/echo"，jsonc 在 test/SpecialTest 下，仓库根才命中）
            var pluginDir = ResolvePluginDir(p.Path, projectDir);
            if (pluginDir == null)
            {
                Log.AddIRLog(LID.ExportPluginLibNotFound,
                    "plugin: lib dir not found (path unresolved): " + p.Id + " path=" + p.Path);
                return;
            }

            // 2) 目标平台：auto = 宿主 OS/arch；显式 select="<os>-<arch>" 直取
            var libRoot = Path.Combine(pluginDir, spec.Dir);
            if (!Directory.Exists(libRoot))
            {
                Log.AddIRLog(LID.ExportPluginLibNotFound,
                    "plugin: lib dir not found (no lib root): " + p.Id + " " + libRoot);
                return;
            }

            string os;
            var candidates = new List<string>();
            if (string.IsNullOrWhiteSpace(spec.Select) || spec.Select == "auto")
            {
                var (hostOs, hostArch) = HostPlatform();
                os = hostOs;
                // 候选目录（§4.2 顺序）：<os>-<arch>（归一形+别名形）→ <os> → <arch> → 裸 lib/
                foreach (var a in ArchNames(hostArch))
                {
                    candidates.Add(hostOs + "-" + a);
                }
                candidates.Add(hostOs);
                candidates.AddRange(ArchNames(hostArch));
                candidates.Add(string.Empty);
            }
            else
            {
                // 显式指定：原样 + 归一变体；os 部分用于推导动态库后缀
                var parts = spec.Select.Split(new[] { '-' }, 2);
                os = parts[0];
                candidates.Add(spec.Select);
                if (parts.Length == 2)
                {
                    var norm = NormalizeArch(parts[1]);
                    if (norm != parts[1])
                    {
                        candidates.Add(parts[0] + "-" + norm);
                    }
                }
            }

            // 3) 四级回退：目录存在且能过滤出 ≥1 个库文件才算命中（空目录继续回退）
            string chosenDir = null;
            List<string> files = null;
            foreach (var c in candidates)
            {
                var dir = c.Length == 0 ? libRoot : Path.Combine(libRoot, c);
                if (!Directory.Exists(dir))
                {
                    continue;
                }
                var got = CollectFiles(dir, spec.Files, os);
                if (got.Count > 0)
                {
                    chosenDir = dir;
                    files = got;
                    break;
                }
            }
            if (chosenDir == null)
            {
                Log.AddIRLog(LID.ExportPluginLibNotFound,
                    "plugin: lib platform dir not found: " + p.Id
                    + " (select=" + spec.Select + ", root=" + libRoot + ")");
                return;
            }

            // 4) 拷贝到 outDir/plugins/<id>/ 并写回相对包路径
            var dstDir = Path.Combine(outDir, "plugins", p.Id);
            Directory.CreateDirectory(dstDir);
            foreach (var f in files)
            {
                var dstPath = Path.Combine(dstDir, f);
                File.Copy(Path.Combine(chosenDir, f), dstPath, overwrite: true);
            }

            p.Libs.Clear();
            foreach (var f in files)
            {
                p.Libs.Add("plugins/" + p.Id + "/" + f);
            }
            p.Lib = p.Libs[0];

            // 5) frontend 构建产物路径关联：本趟 AtSignLabelBuildManager 构建
            //    并部署的插件 frontend dll（如 csharp_mono 的 SLAtSign.dll）
            //    拷入模块包同目录并追加进 plugins[].libs（纯关联记录，CVM
            //    装配只消费 lib 主库；运行期插件 dll 从包内加载后，同目录
            //    的 frontend 产物即可被其运行时按裸名定位）
            try
            {
                var deployed = AtSignLabelBuildManager.FindDeployedDll(p.Id);
                if (deployed != null)
                {
                    var dllName = Path.GetFileName(deployed);
                    var dstPath = Path.Combine(dstDir, dllName);
                    // 同路径守卫：lib 直给值已指向包内目录时部署即落位，跳过拷贝
                    if (!string.Equals(Path.GetFullPath(deployed), Path.GetFullPath(dstPath),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(deployed, dstPath, overwrite: true);
                    }
                    var rel = "plugins/" + p.Id + "/" + dllName;
                    if (!p.Libs.Contains(rel))
                    {
                        p.Libs.Add(rel);
                    }
                    Log.AddIRLog(LID.ExportPluginLibResolved,
                        "plugin: frontend dll linked: " + p.Id + " " + deployed + " -> " + rel);
                }
            }
            catch (Exception e)
            {
                Log.AddIRLog(LID.ExportPluginLibCopyFailed,
                    "plugin: frontend dll link failed: " + p.Id + " " + e.Message);
            }

            Log.AddIRLog(LID.ExportPluginLibResolved,
                "plugin: lib resolved: " + p.Id + " " + chosenDir
                + " -> " + dstDir + " (" + files.Count + " file(s))");
        }

        /// <summary>
        /// 选中目录内的库文件清单：
        /// files 未声明 / 含 "*" = 全部动态库（按平台后缀过滤，文件名字典序）；
        /// 否则按显式列表顺序（须存在于该目录，缺失记日志跳过）。
        /// </summary>
        private static List<string> CollectFiles(string dir, List<string> files, string os)
        {
            var result = new List<string>();
            if (files == null || files.Count == 0 || files.Contains("*"))
            {
                var exts = DynamicLibExtensions(os);
                foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly)
                             .Select(Path.GetFileName)
                             .Where(n => !string.IsNullOrEmpty(n) && exts.Contains(Path.GetExtension(n).ToLowerInvariant()))
                             .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(f);
                }
                return result;
            }
            foreach (var f in files)
            {
                if (File.Exists(Path.Combine(dir, f)))
                {
                    result.Add(f);
                }
                else
                {
                    Log.AddIRLog(LID.ExportPluginLibCopyFailed,
                        "plugin: lib file not found in selected dir (skip): " + dir + " " + f);
                }
            }
            return result;
        }

        /// <summary>宿主 OS/arch（§4.5 归一化受控值：windows/linux/macos…；x86_64/arm64…）。</summary>
        internal static (string os, string arch) HostPlatform()
        {
            string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macos"
                : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux"
                : "unix";
            string arch = RuntimeInformation.OSArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.Arm => "arm",
                Architecture.Arm64 => "arm64",
                _ => "x86_64",
            };
            return (os, arch);
        }

        /// <summary>arch 归一化（§4.5：amd64/x64 是 x86_64 的别名，aarch64 是 arm64 的别名）。</summary>
        internal static string NormalizeArch(string arch)
        {
            switch (arch)
            {
                case "amd64":
                case "x64":
                    return "x86_64";
                case "aarch64":
                    return "arm64";
                default:
                    return arch;
            }
        }

        /// <summary>arch 的全部目录名变体（归一形在前，别名形在后，供目录探测）。</summary>
        private static string[] ArchNames(string arch)
        {
            switch (arch)
            {
                case "x86_64":
                    return new[] { "x86_64", "x64", "amd64" };
                case "arm64":
                    return new[] { "arm64", "aarch64" };
                default:
                    return new[] { arch };
            }
        }

        /// <summary>平台动态库后缀（§4.2：files:["*"] 按平台后缀过滤）。</summary>
        private static string[] DynamicLibExtensions(string os)
        {
            switch (os)
            {
                case "windows":
                    return new[] { ".dll" };
                case "macos":
                case "ios":
                    return new[] { ".dylib" };
                case "browser":
                case "wasi":
                    return new[] { ".wasm", ".a" };
                default:
                    // linux/android/freebsd/unix
                    return new[] { ".so" };
            }
        }

        /// <summary>
        /// 解析 plugins.&lt;id&gt;.path：绝对路径 → 相对 jsonc 目录存在 →
        /// 从 jsonc 目录逐级上溯（兼容 "SLPlugin/echo" 写在仓库根、jsonc 在
        /// test/ 子目录的场景）→ 相对当前工作目录。
        /// </summary>
        private static string ResolvePluginDir(string path, string projectDir)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            try
            {
                if (Path.IsPathRooted(path))
                {
                    return Directory.Exists(path) ? Path.GetFullPath(path) : null;
                }
                var direct = Path.GetFullPath(Path.Combine(projectDir, path));
                if (Directory.Exists(direct))
                {
                    return direct;
                }
                // 逐级上溯（最多到盘符根）
                var dir = projectDir;
                while (!string.IsNullOrEmpty(dir))
                {
                    var up = Path.GetDirectoryName(dir);
                    if (string.IsNullOrEmpty(up) || up == dir)
                    {
                        break;
                    }
                    dir = up;
                    var cand = Path.GetFullPath(Path.Combine(dir, path));
                    if (Directory.Exists(cand))
                    {
                        return cand;
                    }
                }
                var cwd = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));
                return Directory.Exists(cwd) ? cwd : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
