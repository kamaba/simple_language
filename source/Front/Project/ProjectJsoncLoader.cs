using SimpleLanguage.Core;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SimpleLanguage.Project
{
    public static class ProjectJsoncLoader
    {
        public static ProjectConfig FromJsonc(string jsoncText)
        {
            var cfg = new ProjectConfig();
            if (string.IsNullOrWhiteSpace(jsoncText))
            {
                return cfg;
            }

            using var doc = JsonDocument.Parse(jsoncText, new JsonDocumentOptions()
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

            var root = doc.RootElement;

            if (TryGetObj(root, "project", out var project))
            {
                cfg.Project.Name = GetStr(project, "name", cfg.Project.Name);
                cfg.Project.Desc = GetStr(project, "desc", cfg.Project.Desc);
                cfg.Export.VersionMain = GetInt(project, "mainVersion", cfg.Export.VersionMain);
                cfg.Export.VersionSub = GetInt(project, "subVersion", cfg.Export.VersionSub);
                cfg.Export.VersionPatch = GetInt(project, "buildVersion", cfg.Export.VersionPatch);
            }

            if (TryGetObj(root, "source", out var source))
            {
                cfg.Source.Root = GetStr(source, "root", cfg.Source.Root);
                cfg.Source.EntryFile = GetStr(source, "entryFile", cfg.Source.EntryFile);
            }

            if (TryGetObj(root, "compile", out var compile))
            {
                cfg.Compile.Optimize = GetBool(compile, "optimize", cfg.Compile.Optimize);
                cfg.Compile.Target = GetStr(compile, "target", cfg.Compile.Target);
                cfg.Compile.Debug = GetBool(compile, "debug", cfg.Compile.Debug);
                cfg.Compile.IsUseForceSemiColonInLineEnd = GetBool(compile, "isUseForceSemiColonInLineEnd", cfg.Compile.IsUseForceSemiColonInLineEnd);
                cfg.Compile.IsForceUseKeyClass = GetBool(compile, "isForceUseClassKey", cfg.Compile.IsForceUseKeyClass);
                cfg.Compile.IsSupportDoublePlus = GetBool(compile, "isSupportDoublePlus", cfg.Compile.IsSupportDoublePlus);
                cfg.Compile.RequireSameNumericTypes = GetBool(compile, "requireSameNumericTypes", cfg.Compile.RequireSameNumericTypes);
            }

            if (TryGetObj(root, "compileFiles", out var compileFiles)
                && compileFiles.TryGetProperty("files", out var files)
                && files.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in files.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var f = new ProjectConfig.CompileFileItem();
                    f.Path = GetStr(item, "path", f.Path);
                    f.Group = GetStr(item, "group", f.Group);
                    f.Tag = GetStr(item, "tag", f.Tag);
                    f.Ignore = GetBool(item, "ignore", f.Ignore);
                    f.Priority = GetInt(item, "priority", f.Priority);
                    if (!string.IsNullOrWhiteSpace(f.Path))
                    {
                        cfg.CompileFiles.Files.Add(f);
                    }
                }

            }

            if (TryGetObj(root, "compileFilter", out var filter))
            {
                cfg.CompileFilter.IsAllGroup = GetBool(filter, "isAllGroup", cfg.CompileFilter.IsAllGroup);
                cfg.CompileFilter.IsAllTag = GetBool(filter, "isAllTag", cfg.CompileFilter.IsAllTag);

                if (filter.TryGetProperty("groups", out var groups) && groups.ValueKind == JsonValueKind.Array)
                {
                    foreach (var g in groups.EnumerateArray())
                    {
                        if (g.ValueKind == JsonValueKind.String)
                        {
                            cfg.CompileFilter.Groups.Add(g.GetString() ?? string.Empty);
                        }
                    }
                }
                if (filter.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in tags.EnumerateArray())
                    {
                        if (t.ValueKind == JsonValueKind.String)
                        {
                            cfg.CompileFilter.Tags.Add(t.GetString() ?? string.Empty);
                        }
                    }
                }

            }

            if (TryGetObj(root, "data", out var rootProjectData))
            {
                foreach (var kv in rootProjectData.EnumerateObject())
                {
                    cfg.JsoncProjectData[kv.Name] = kv.Value.Clone();
                }
            }

            if (TryGetObj(root, "global", out var global))
            {
                if (global.TryGetProperty("imports", out var imports) && imports.ValueKind == JsonValueKind.Array)
                {
                    foreach (var i in imports.EnumerateArray())
                    {
                        if (i.ValueKind == JsonValueKind.String)
                        {
                            cfg.Global.Imports.Add(i.GetString() ?? string.Empty);
                        }
                    }
                }

                if (TryGetObj(global, "data", out var dataObj))
                {
                    foreach (var kv in dataObj.EnumerateObject())
                    {
                        cfg.Global.Data[kv.Name] = kv.Value.Clone();
                    }
                }

                if (TryGetObj(global, "macro", out var macroObj))
                {
                    foreach (var kv in macroObj.EnumerateObject())
                    {
                        cfg.Global.Macro[kv.Name] = kv.Value.Clone();
                    }
                }

                if (TryGetObj(global, "replace", out var replace))
                {
                    foreach (var kv in replace.EnumerateObject())
                    {
                        if (kv.Value.ValueKind == JsonValueKind.String)
                        {
                            cfg.Global.Replace[kv.Name] = kv.Value.GetString() ?? string.Empty;
                        }
                    }
                }
            }

            if (root.TryGetProperty( "struct", out var structObj)
                && structObj.ValueKind == JsonValueKind.Array)
            {
                ParseStructNodes(structObj, cfg.StructTree);
            }

            if (TryGetObj(root, "export", out var exportObj))
            {
                cfg.Export.ModuleName = GetStr(exportObj, "moduleName", cfg.Export.ModuleName);
                cfg.Export.OutputDir = GetStr(exportObj, "outputDir", cfg.Export.OutputDir);
                cfg.Export.StringPoolAsBlob = GetBool(exportObj, "stringPoolAsBlob", cfg.Export.StringPoolAsBlob);
                cfg.Export.ExportPublicOnly = GetBool(exportObj, "exportPublicOnly", cfg.Export.ExportPublicOnly);
                cfg.Export.IncludeMetadata = GetBool(exportObj, "includeMetadata", cfg.Export.IncludeMetadata);

                cfg.Export.VersionMain = GetInt(exportObj, "versionMain", cfg.Export.VersionMain);
                cfg.Export.VersionSub = GetInt(exportObj, "versionSub", cfg.Export.VersionSub);
                cfg.Export.VersionPatch = GetInt(exportObj, "versionPatch", cfg.Export.VersionPatch);
                cfg.Export.NativeDll = GetStr(exportObj, "nativeDll", cfg.Export.NativeDll);

                if (TryGetObj(exportObj, "aot", out var aotObj))
                {
                    cfg.Export.Aot.Enabled = GetBool(aotObj, "enabled", cfg.Export.Aot.Enabled);
                    cfg.Export.Aot.BuildDll = GetBool(aotObj, "buildDll", cfg.Export.Aot.BuildDll);
                    // §11.4 AOT 目标三元组：triple/cpu/features → llc -mtriple/-mcpu/-mattr
                    cfg.Export.Aot.Triple = GetStr(aotObj, "triple", cfg.Export.Aot.Triple);
                    cfg.Export.Aot.Cpu = GetStr(aotObj, "cpu", cfg.Export.Aot.Cpu);
                    cfg.Export.Aot.Features = GetStr(aotObj, "features", cfg.Export.Aot.Features);
                }

                if (TryGetObj(exportObj, "debugText", out var debugTextObj))
                {
                    cfg.Export.DebugText.OutputDir = GetStr(debugTextObj, "outputDir", cfg.Export.DebugText.OutputDir);
                    cfg.Export.DebugText.Code = GetBool(debugTextObj, "code", cfg.Export.DebugText.Code);
                    cfg.Export.DebugText.Token = GetBool(debugTextObj, "token", cfg.Export.DebugText.Token);
                    cfg.Export.DebugText.Node = GetBool(debugTextObj, "node", cfg.Export.DebugText.Node);
                    cfg.Export.DebugText.File = GetBool(debugTextObj, "file", cfg.Export.DebugText.File);
                    cfg.Export.DebugText.Meta = GetBool(debugTextObj, "meta", cfg.Export.DebugText.Meta);
                    cfg.Export.DebugText.IR = GetBool(debugTextObj, "ir", cfg.Export.DebugText.IR);
                }
            }

            if (root.TryGetProperty("references", out var refs) && refs.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in refs.EnumerateArray())
                {
                    if (r.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }
                    var path = GetStr(r, "path", string.Empty);
                    var uuid = GetStr(r, "uuid", string.Empty);
                    var name = GetStr(r, "name", string.Empty);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        cfg.References.Add(new ProjectConfig.ReferenceSection() { Path = path, UUID = uuid, Name = name });
                    }
                }
            }

            // 外部 dll 导入：新 "dllImports" 段（path/name/alias），兼容旧 "lib" 段
            //（path/name，alias 缺省取 name）。alias 供 @DllImport("别名",...) 与
            // global.dllImport.别名 解析为完整路径；alias 为空时退化为 name 不可用。
            if (root.TryGetProperty("dllImports", out var dllImports) && dllImports.ValueKind == JsonValueKind.Array)
            {
                ParseDllImportArray(dllImports, cfg);
            }
            if (root.TryGetProperty("lib", out var lib) && lib.ValueKind == JsonValueKind.Array)
            {
                ParseDllImportArray(lib, cfg);
            }

            // cvm 扩展 DLL 导入："vmDlls" 段 [{ project, name, configuration, platform }]。
            // project 为 VS 工程文件（相对 jsonc 所在目录），导出时 Front 用 MSBuild
            // 编译并把产出 DLL 拷到 module.json 同目录；name 为产出 DLL 文件名，
            // 随 module.json 的 vmDllImports 导出，供 cvm 按 package 目录预加载。
            if (root.TryGetProperty("vmDlls", out var vmDlls) && vmDlls.ValueKind == JsonValueKind.Array)
            {
                foreach (var d in vmDlls.EnumerateArray())
                {
                    if (d.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }
                    var projPath = GetStr(d, "project", string.Empty);
                    var name = GetStr(d, "name", string.Empty);
                    if (string.IsNullOrWhiteSpace(projPath) || string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }
                    var sec = new ProjectConfig.VmDllSection()
                    {
                        Project = projPath,
                        Name = name,
                        Configuration = GetStr(d, "configuration", "Debug"),
                        Platform = GetStr(d, "platform", "x64"),
                    };
                    cfg.VmDlls.Add(sec);
                }
            }

            if( root.TryGetProperty("systemCalls", out var systemCalls ) && systemCalls.ValueKind == JsonValueKind.Array )
            {
                foreach (var r in systemCalls.EnumerateArray())
                {
                    if (r.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }
                    var name = GetStr(r, "name", string.Empty);
                    var returnType = GetStr(r, "returnType", string.Empty);
                    List<String> mtStr = new List<string>();
                    if (r.TryGetProperty("params", out var @params) && @params.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var r2 in @params.EnumerateArray())
                        {
                            mtStr.Add(r2.GetString() ?? string.Empty);
                        }
                    }
                    var isVariadic = GetBool(r, "isVariadic", true );
                    var cvmFunction = GetStr(r, "cvmFunction", string.Empty);

                    cfg.systemCalls.Add(new ProjectConfig.SystemCallItem() { name = name, returnType = returnType, @params = mtStr.ToArray(), isVariadic = isVariadic, cvmFunction = cvmFunction });

                    //SystemMethodCallDeclarationRegistry.AddDeclByMt( name, returnType, mtStr, isVariadic, true );
                }
            }

            if (TryGetObj(root, "platform", out var platform))
            {
                ParsePlatform(platform, cfg);
            }
            else if (root.TryGetProperty("platform", out var platformAny) && platformAny.ValueKind == JsonValueKind.Array)
            {
                // 兼容把 platform.targets 误写成顶层数组的写法
                foreach (var t in platformAny.EnumerateArray())
                {
                    if (t.ValueKind == JsonValueKind.String)
                    {
                        cfg.Platform.Declared = true;
                        cfg.Platform.Targets.Add(t.GetString() ?? string.Empty);
                    }
                }
            }

            // compile.target 受控枚举校验（§11.1：非法值报错；与 arch 矛盾只告警）
            ValidateCompileTarget(cfg);

            // platform.require 值域校验 + 别名归一（§13.8）不在 FromJsonc 期做：
            // 配置加载发生在 ProcessManager 阶段开始之前，此处的 Error 进不了任何
            // 阶段的 errorCount 差值（报错但编译继续、非法值仍被导出）。
            // 校验挪到 MetaCore 阶段 ValidatePlatformConfig 步骤（InjectProjectData 之后、
            // ParseStatements 之前，即 §13.8 规定的时机），由 AbortPhase 策略中止编译。

            return cfg;
        }

        // ── 平台能力声明（PLATFORM_CAPABILITY_DESIGN.md §6.1 / §6.2）──
        // 解析 jsonc "platform" 段：targets（元数据）+ require（→ 条件 AST）+
        // when（表达式糖，仅存储）+ fallbackHint。require 各字段默认 AND，
        // 数组元素各自成 atom；未知字段告警忽略（§7.2 兼容策略）。
        static void ParsePlatform(JsonElement platform, ProjectConfig cfg)
        {
            cfg.Platform.Declared = true;

            if (platform.TryGetProperty("targets", out var targets) && targets.ValueKind == JsonValueKind.Array)
            {
                foreach (var t in targets.EnumerateArray())
                {
                    if (t.ValueKind == JsonValueKind.String)
                    {
                        var s = t.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            cfg.Platform.Targets.Add(s);
                        }
                    }
                }
            }

            cfg.Platform.When = GetStr(platform, "when", string.Empty);
            cfg.Platform.FallbackHint = GetStr(platform, "fallbackHint", string.Empty);

            if (TryGetObj(platform, "require", out var require))
            {
                var atoms = new List<PlatformReqAtom>();
                foreach (var kv in require.EnumerateObject())
                {
                    ParseRequireField(kv.Name, kv.Value, atoms, cfg.Platform);
                }
                cfg.Platform.Root = PlatformReqExpr.AndOf(ToExprList(atoms));
            }

            // 多目标变体（§14）：数组顺序即优先级（CVM 取第一个通过的变体）。
            // 每项 target（标签）+ aot（产物文件名，缺省解释器兜底）+
            // require（复用 ParseRequireField 的字段表，未知字段同样告警忽略）。
            if (platform.TryGetProperty("variants", out var variantsNode)
                && variantsNode.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in variantsNode.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }
                    var variant = new ProjectConfig.PlatformVariantSection
                    {
                        Target = GetStr(item, "target", string.Empty),
                        Aot = GetStr(item, "aot", string.Empty)
                    };
                    if (TryGetObj(item, "require", out var vreq))
                    {
                        var atoms = new List<PlatformReqAtom>();
                        foreach (var kv in vreq.EnumerateObject())
                        {
                            ParseRequireField(kv.Name, kv.Value, atoms, cfg.Platform);
                        }
                        variant.Root = PlatformReqExpr.AndOf(ToExprList(atoms));
                    }
                    cfg.Platform.Variants.Add(variant);
                }
            }

            // override 通道（§8.5.2）：对象成员 true/false → ENABLE/DISABLE，
            // 字符串/数字 → SET（数字存 GetRawText 原文，导出时复原为数字）；
            // null/array/object 成员跳过（值形态只此四种）。key 校验 + 归一
            // 在 MetaCore 阶段 ValidatePlatformConfig 步骤做（§8.5.3 / §13.8 同时机）。
            if (TryGetObj(platform, "override", out var overrideObj))
            {
                foreach (var kv in overrideObj.EnumerateObject())
                {
                    var entry = new ProjectConfig.PlatformOverrideEntry { Key = kv.Name };
                    switch (kv.Value.ValueKind)
                    {
                        case JsonValueKind.True:
                            entry.Kind = ProjectConfig.PlatformOverrideEntry.ValueKindEnum.True;
                            break;
                        case JsonValueKind.False:
                            entry.Kind = ProjectConfig.PlatformOverrideEntry.ValueKindEnum.False;
                            break;
                        case JsonValueKind.Number:
                            entry.Kind = ProjectConfig.PlatformOverrideEntry.ValueKindEnum.Number;
                            entry.Value = kv.Value.GetRawText();
                            break;
                        case JsonValueKind.String:
                            entry.Kind = ProjectConfig.PlatformOverrideEntry.ValueKindEnum.String;
                            entry.Value = kv.Value.GetString() ?? string.Empty;
                            break;
                        default:
                            continue;
                    }
                    cfg.Platform.Override.Add(entry);
                }
            }
        }

        static List<PlatformReqExpr> ToExprList(List<PlatformReqAtom> atoms)
        {
            var list = new List<PlatformReqExpr>();
            foreach (var a in atoms)
            {
                list.Add(PlatformReqExpr.OfAtom(a));
            }
            return list;
        }

        // 单个 require 字段 → 0..n 个 atom（§6.2 映射表）；
        // section 用于落段级行为开关（如 network.probe，不产 atom）
        static void ParseRequireField(string name, JsonElement value, List<PlatformReqAtom> atoms, ProjectConfig.PlatformSection section)
        {
            switch (name)
            {
                // ── 单值对象：{any:[..]} / {eq} / {ne} / {min}（min→ge）──
                case "os":
                    ParseNameValueAtom(PlatformReqNames.Os, value, atoms, defaultOptional: false);
                    break;
                case "arch":
                    ParseNameValueAtom(PlatformReqNames.Arch, value, atoms, defaultOptional: false);
                    break;
                case "osVersion":
                    ParseVersionAtom(PlatformReqNames.OsVersion, value, atoms, defaultOptional: false);
                    break;
                case "cpuCount":
                    ParseVersionAtom(PlatformReqNames.CpuCount, value, atoms, defaultOptional: false);
                    break;
                case "memory":
                    ParseVersionAtom(PlatformReqNames.Memory, value, atoms, defaultOptional: false);
                    break;
                case "runtime":
                    ParseVersionAtom(PlatformReqNames.Runtime, value, atoms, defaultOptional: false);
                    break;
                case "network":
                    // network: {minLink, probe?}——minLink 产 atom（Link 定序比较）；
                    // probe 是行为开关（L3 加载期在线探测，L1665），落 section 不产 atom
                    {
                        var atom = MakeVersionAtom(PlatformReqNames.Network, value, "minLink", GetBool(value, "optional", false));
                        if (atom != null)
                        {
                            atoms.Add(atom);
                        }
                        section.NetworkProbe = GetBool(value, "probe", false);
                    }
                    break;

                // ── 复合对象：cpu（all/any/minCores/topology 各产一个 atom）──
                case "cpu":
                    if (value.ValueKind != JsonValueKind.Object)
                    {
                        return;
                    }
                    var cpuOptional = GetBool(value, "optional", false);
                    AddSetAtom(PlatformReqNames.Cpu, value, "all", PlatformReqNames.CmpAll, cpuOptional, atoms);
                    AddSetAtom(PlatformReqNames.Cpu, value, "any", PlatformReqNames.CmpAny, cpuOptional, atoms);
                    if (value.TryGetProperty("minCores", out var minCores) && minCores.ValueKind == JsonValueKind.Number)
                    {
                        atoms.Add(new PlatformReqAtom { Kind = PlatformReqNames.CpuCount, Cmp = PlatformReqNames.CmpGe, Value = minCores.GetRawText(), Optional = cpuOptional });
                    }
                    if (TryGetObj(value, "topology", out var topology))
                    {
                        AddSetAtom(PlatformReqNames.CpuTopology, topology, "any", PlatformReqNames.CmpAny, GetBool(topology, "optional", cpuOptional), atoms);
                    }
                    break;

                // ── 具名条件对象：render / embedded（api/family/min* 各产一个 atom）──
                case "render":
                    if (value.ValueKind != JsonValueKind.Object)
                    {
                        return;
                    }
                    var renderOptional = GetBool(value, "optional", true);
                    if (TryGetObj(value, "api", out var api))
                    {
                        AddSetAtom(PlatformReqNames.Render, api, "any", PlatformReqNames.CmpAny, GetBool(api, "optional", renderOptional), atoms);
                    }
                    var minShader = GetStr(value, "minShaderModel", string.Empty);
                    if (minShader.Length > 0)
                    {
                        atoms.Add(new PlatformReqAtom { Kind = PlatformReqNames.Render, Cmp = PlatformReqNames.CmpGe, Value = minShader, Optional = renderOptional });
                    }
                    break;
                case "embedded":
                    if (value.ValueKind != JsonValueKind.Object)
                    {
                        return;
                    }
                    var embOptional = GetBool(value, "optional", true);
                    if (TryGetObj(value, "family", out var family))
                    {
                        AddSetAtom(PlatformReqNames.Embedded, family, "any", PlatformReqNames.CmpAny, GetBool(family, "optional", embOptional), atoms);
                    }
                    if (value.TryGetProperty("minFlashKB", out var minFlash) && minFlash.ValueKind == JsonValueKind.Number)
                    {
                        atoms.Add(new PlatformReqAtom { Kind = PlatformReqNames.Embedded, Cmp = PlatformReqNames.CmpGe, Key = "flash", Value = minFlash.GetRawText(), Optional = embOptional });
                    }
                    if (value.TryGetProperty("minRamKB", out var minRam) && minRam.ValueKind == JsonValueKind.Number)
                    {
                        atoms.Add(new PlatformReqAtom { Kind = PlatformReqNames.Embedded, Cmp = PlatformReqNames.CmpGe, Key = "ram", Value = minRam.GetRawText(), Optional = embOptional });
                    }
                    break;

                // ── 数组：每元素一个 atom，元素可带 optional 覆盖默认 ──
                case "lib":
                    ParseNameItems(PlatformReqNames.Lib, value, defaultOptional: false, useEq: false, atoms);
                    break;
                case "sdk":
                    ParseNameItems(PlatformReqNames.Sdk, value, defaultOptional: true, useEq: false, atoms);
                    break;
                case "ai":
                    ParseNameItems(PlatformReqNames.Ai, value, defaultOptional: true, useEq: false, atoms);
                    break;
                case "script":
                    ParseNameItems(PlatformReqNames.Script, value, defaultOptional: true, useEq: false, atoms);
                    break;
                case "custom":
                    ParseNameItems(PlatformReqNames.Custom, value, defaultOptional: false, useEq: true, atoms);
                    break;
                case "env":
                    ParseNameItems(PlatformReqNames.Env, value, defaultOptional: false, useEq: true, atoms);
                    break;
                case "device":
                    ParseKindItems(PlatformReqNames.Device, value, defaultOptional: true, atoms);
                    break;
                case "environment":
                    ParseKindItems(PlatformReqNames.Environment, value, defaultOptional: true, atoms);
                    break;

                default:
                    Log.AddProjectLog(LID.ProjectPlatformRequireUnknownField, "", name, "os/osVersion/arch/cpu/cpuCount/memory/runtime/lib/sdk/env/device/environment/ai/render/network/script/embedded/custom");
                    break;
            }
        }

        // os / arch：{any:[..]} 或 {eq:".."} / {ne:".."}，对象级 optional（缺省 false）
        static void ParseNameValueAtom(string kind, JsonElement value, List<PlatformReqAtom> atoms, bool defaultOptional)
        {
            if (value.ValueKind == JsonValueKind.String)
            {
                // {"arch": "x86_64"} 简写 = eq
                atoms.Add(new PlatformReqAtom { Kind = kind, Cmp = PlatformReqNames.CmpEq, Value = value.GetString() ?? string.Empty });
                return;
            }
            if (value.ValueKind != JsonValueKind.Object)
            {
                return;
            }
            var optional = GetBool(value, "optional", defaultOptional);
            var eq = GetStr(value, "eq", string.Empty);
            if (eq.Length > 0)
            {
                atoms.Add(new PlatformReqAtom { Kind = kind, Cmp = PlatformReqNames.CmpEq, Value = eq, Optional = optional });
                return;
            }
            var ne = GetStr(value, "ne", string.Empty);
            if (ne.Length > 0)
            {
                atoms.Add(new PlatformReqAtom { Kind = kind, Cmp = PlatformReqNames.CmpNe, Value = ne, Optional = optional });
                return;
            }
            AddSetAtom(kind, value, "any", PlatformReqNames.CmpAny, optional, atoms);
        }

        // osVersion / cpuCount / memory / runtime / network.minLink：
        // {min:".."} → cmp=ge（数字版本号支持 min/eq/ne，统一转字符串）
        static void ParseVersionAtom(string kind, JsonElement value, List<PlatformReqAtom> atoms, bool defaultOptional)
        {
            if (value.ValueKind != JsonValueKind.Object)
            {
                return;
            }
            var atom = MakeVersionAtom(kind, value, "min", GetBool(value, "optional", defaultOptional));
            if (atom != null)
            {
                atoms.Add(atom);
            }
        }

        static PlatformReqAtom MakeVersionAtom(string kind, JsonElement obj, string minKey, bool optional)
        {
            var cmp = string.Empty;
            var val = string.Empty;
            if (obj.ValueKind == JsonValueKind.Object)
            {
                var min = GetStr(obj, minKey, string.Empty);
                if (min.Length == 0 && obj.TryGetProperty(minKey, out var minNode))
                {
                    if (minNode.ValueKind == JsonValueKind.Number)
                    {
                        min = minNode.GetRawText();
                    }
                }
                if (min.Length > 0)
                {
                    cmp = PlatformReqNames.CmpGe;
                    val = min;
                }
                else
                {
                    val = GetStr(obj, "eq", string.Empty);
                    if (val.Length > 0)
                    {
                        cmp = PlatformReqNames.CmpEq;
                    }
                    else
                    {
                        val = GetStr(obj, "ne", string.Empty);
                        if (val.Length > 0)
                        {
                            cmp = PlatformReqNames.CmpNe;
                        }
                    }
                }
            }
            if (cmp.Length == 0)
            {
                return null;
            }
            return new PlatformReqAtom { Kind = kind, Cmp = cmp, Value = val, Optional = optional };
        }

        // {any:["..",".."]} → cmp=any 的集合 atom（空集合不产 atom）
        static void AddSetAtom(string kind, JsonElement obj, string setKey, string cmp, bool optional, List<PlatformReqAtom> atoms)
        {
            if (obj.ValueKind != JsonValueKind.Object)
            {
                return;
            }
            if (!obj.TryGetProperty(setKey, out var setNode) || setNode.ValueKind != JsonValueKind.Array)
            {
                return;
            }
            var set = new List<string>();
            foreach (var s in setNode.EnumerateArray())
            {
                if (s.ValueKind == JsonValueKind.String)
                {
                    var v = s.GetString();
                    if (!string.IsNullOrWhiteSpace(v))
                    {
                        set.Add(v);
                    }
                }
            }
            if (set.Count == 0)
            {
                return;
            }
            atoms.Add(new PlatformReqAtom { Kind = kind, Cmp = cmp, Set = set, Optional = optional });
        }

        // lib/sdk/ai/script/custom/env 数组：[{name|key, min|eq, optional}]
        // lib 族用 min（无 min→exists）；env/custom 用 eq（无 eq→exists）
        static void ParseNameItems(string kind, JsonElement array, bool defaultOptional, bool useEq, List<PlatformReqAtom> atoms)
        {
            if (array.ValueKind != JsonValueKind.Array)
            {
                return;
            }
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }
                var key = GetStr(item, "name", GetStr(item, "key", string.Empty));
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }
                var atom = new PlatformReqAtom
                {
                    Kind = kind,
                    Key = key,
                    Optional = GetBool(item, "optional", defaultOptional),
                };
                if (useEq)
                {
                    atom.Cmp = PlatformReqNames.CmpEq;
                    atom.Value = GetStr(item, "eq", string.Empty);
                    if (atom.Value.Length == 0)
                    {
                        atom.Cmp = PlatformReqNames.CmpExists;
                    }
                }
                else
                {
                    atom.Cmp = PlatformReqNames.CmpGe;
                    atom.Value = GetStr(item, "min", string.Empty);
                    if (atom.Value.Length == 0 && item.TryGetProperty("min", out var minNode) && minNode.ValueKind == JsonValueKind.Number)
                    {
                        atom.Value = minNode.GetRawText();
                    }
                    if (atom.Value.Length == 0)
                    {
                        atom.Cmp = PlatformReqNames.CmpExists;
                    }
                }
                atoms.Add(atom);
            }
        }

        // device/environment 数组：[{kind|name, minCompute, optional}]
        static void ParseKindItems(string kind, JsonElement array, bool defaultOptional, List<PlatformReqAtom> atoms)
        {
            if (array.ValueKind != JsonValueKind.Array)
            {
                return;
            }
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }
                var key = GetStr(item, "kind", GetStr(item, "name", string.Empty));
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }
                var atom = new PlatformReqAtom
                {
                    Kind = kind,
                    Key = key,
                    Cmp = PlatformReqNames.CmpExists,
                    Optional = GetBool(item, "optional", defaultOptional),
                };
                if (item.TryGetProperty("minCompute", out var minCompute) && minCompute.ValueKind == JsonValueKind.Number)
                {
                    atom.Cmp = PlatformReqNames.CmpGe;
                    atom.Value = minCompute.GetRawText();
                }
                atoms.Add(atom);
            }
        }

        // compile.target 受控枚举（§11.1）：
        // 1. 非法值 → Error（20033）；
        // 2. target 与 require.arch 无交集 → Warning（20034，编译产物目标与运行期要求矛盾）
        static void ValidateCompileTarget(ProjectConfig cfg)
        {
            var target = cfg.Compile.Target ?? string.Empty;
            var allowed = false;
            foreach (var t in PlatformReqNames.AllowedTargets)
            {
                if (string.Equals(t, target, StringComparison.OrdinalIgnoreCase))
                {
                    cfg.Compile.Target = t;
                    allowed = true;
                    break;
                }
            }
            if (!allowed)
            {
                Log.AddProjectLog(LID.ProjectPlatformTargetInvalid, "", target, "AnyCPU/x86/x64/arm64/wasm32");
                return;
            }

            var family = PlatformReqNames.TargetArchFamily(cfg.Compile.Target);
            if (family.Length == 0 || cfg.Platform.Root == null)
            {
                return;
            }
            var archCandidates = new List<string>();
            CollectArchCandidates(cfg.Platform.Root, archCandidates);
            if (archCandidates.Count == 0)
            {
                return;
            }
            var intersect = false;
            foreach (var a in archCandidates)
            {
                foreach (var f in family)
                {
                    if (string.Equals(a, f, StringComparison.OrdinalIgnoreCase))
                    {
                        intersect = true;
                        break;
                    }
                }
                if (intersect)
                {
                    break;
                }
            }
            if (!intersect)
            {
                Log.AddProjectLog(LID.ProjectPlatformTargetArchConflict, "", cfg.Compile.Target, string.Join("/", archCandidates));
            }
        }

        // 收集 require 中 arch 的候选值（cmp==any 的 set ∪ cmp==eq 的 value；ne 是排除条件不参与）
        static void CollectArchCandidates(PlatformReqExpr node, List<string> result)
        {
            if (node == null)
            {
                return;
            }
            if (node.Op == PlatformReqNames.OpAtom)
            {
                var a = node.Atom;
                if (a != null && a.Kind == PlatformReqNames.Arch && a.Cmp != PlatformReqNames.CmpNe)
                {
                    if (a.Cmp == PlatformReqNames.CmpAny || a.Cmp == PlatformReqNames.CmpAll)
                    {
                        result.AddRange(a.Set);
                    }
                    else if (a.Value.Length > 0)
                    {
                        result.Add(a.Value);
                    }
                }
                return;
            }
            foreach (var c in node.Children)
            {
                CollectArchCandidates(c, result);
            }
        }

        // ── export.aot.features 与 platform.require.cpu 一致性校验（§11.4，P2.5）──
        // 仅当 AOT 开启且 features 非空时检查：features 列表（"," 切分；"+name"/裸
        // name = 启用、"-name" = 禁用）须满足 require 树中全部 cpu atom——
        //   all → Set 每个成员都已启用；eq → Value 已启用；
        //   any → Set 至少一个已启用；   ne  → Value 不得启用。
        // 矛盾 → Error 20037（计入 ValidatePlatformConfig 步骤的 errorCount，中止编译）。
        public static void ValidateAotTargetConsistency(ProjectConfig cfg)
        {
            if (cfg?.Export?.Aot == null || !cfg.Export.Aot.Enabled)
            {
                return;
            }
            var features = cfg.Export.Aot.Features;
            if (string.IsNullOrWhiteSpace(features) || cfg.Platform?.Root == null)
            {
                return;
            }
            var enabled = ParseAotFeatures(features);
            ValidateAotCpuConsistency(cfg.Platform.Root, enabled, features);
        }

        // features 串 → 启用集（大小写不敏感）："+avx2"/"avx2" 入集，"-avx2" 不入集。
        // 每个名字过一遍 ISA 定义表归一（"sse4.2"/"sse4_2" → "sse42"，与 require
        // 侧的归一保持同一规范名再比对；表外名（如 "+aes"）保持原样）。
        static HashSet<string> ParseAotFeatures(string features)
        {
            var enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in features.Split(','))
            {
                var f = part.Trim();
                if (f.Length == 0 || f[0] == '-')
                {
                    continue;
                }
                if (f[0] == '+')
                {
                    f = f.Substring(1);
                }
                if (f.Length > 0)
                {
                    if (PlatformDefTables.TryNormalize(PlatformDefTables.KindIsa, f, out var canonical))
                    {
                        f = canonical;
                    }
                    enabled.Add(f);
                }
            }
            return enabled;
        }

        // require 侧名字（可能未归一，如 cpu eq 的 Value）→ 归一后查启用集
        static bool IsAotFeatureEnabled(string name, HashSet<string> enabled)
        {
            if (PlatformDefTables.TryNormalize(PlatformDefTables.KindIsa, name, out var canonical))
            {
                name = canonical;
            }
            return enabled.Contains(name);
        }

        // 递归遍历 require 树，对每个 cpu atom 检查与 features 启用集的一致性
        static void ValidateAotCpuConsistency(PlatformReqExpr node, HashSet<string> enabled, string features)
        {
            if (node == null)
            {
                return;
            }
            if (node.Op == PlatformReqNames.OpAtom)
            {
                if (node.Atom != null && node.Atom.Kind == PlatformReqNames.Cpu)
                {
                    ValidateAotCpuAtom(node.Atom, enabled, features);
                }
                return;
            }
            foreach (var c in node.Children)
            {
                ValidateAotCpuConsistency(c, enabled, features);
            }
        }

        static void ValidateAotCpuAtom(PlatformReqAtom a, HashSet<string> enabled, string features)
        {
            switch (a.Cmp)
            {
                case PlatformReqNames.CmpAll:
                {
                    var missing = new List<string>();
                    foreach (var s in a.Set)
                    {
                        if (!IsAotFeatureEnabled(s, enabled))
                        {
                            missing.Add(s);
                        }
                    }
                    if (missing.Count > 0)
                    {
                        ReportAotConflict(RenderCpuAtom(a), "all 要求全部启用，缺 " + string.Join(",", missing), features);
                    }
                    break;
                }
                case PlatformReqNames.CmpEq:
                {
                    if (a.Value.Length > 0 && !IsAotFeatureEnabled(a.Value, enabled))
                    {
                        ReportAotConflict(RenderCpuAtom(a), "eq 要求启用该特性", features);
                    }
                    break;
                }
                case PlatformReqNames.CmpAny:
                {
                    var ok = false;
                    foreach (var s in a.Set)
                    {
                        if (IsAotFeatureEnabled(s, enabled))
                        {
                            ok = true;
                            break;
                        }
                    }
                    if (!ok && a.Set.Count > 0)
                    {
                        ReportAotConflict(RenderCpuAtom(a), "any 要求至少启用一个", features);
                    }
                    break;
                }
                case PlatformReqNames.CmpNe:
                {
                    if (a.Value.Length > 0 && IsAotFeatureEnabled(a.Value, enabled))
                    {
                        ReportAotConflict(RenderCpuAtom(a), "ne 要求不启用该特性", features);
                    }
                    break;
                }
            }
        }

        // cpu atom 渲染（诊断用）：cpu all(avx2,avx512) / cpu eq(avx2) / cpu any(...) / cpu ne(...)
        static string RenderCpuAtom(PlatformReqAtom a)
        {
            if (a.Cmp == PlatformReqNames.CmpAll || a.Cmp == PlatformReqNames.CmpAny)
            {
                return "cpu " + a.Cmp + "(" + string.Join(",", a.Set) + ")";
            }
            return "cpu " + a.Cmp + "(" + a.Value + ")";
        }

        static void ReportAotConflict(string atomRender, string reason, string features)
        {
            Log.AddProjectLog(LID.ProjectPlatformAotFeatureConflict, "", atomRender, reason, features);
        }

        // ── platform.require 值域校验 + 别名归一（§13.8）──
        // 封闭值域字段的值必须在 PlatformDefTables 定义表中（成员名或别名，
        // 大小写不敏感）；未命中 → Error 20036（含可选值列表 + 纠错建议）；
        // 命中别名 → 归一为规范成员名（随 module.json 导出，运行期不再字符串比较）。
        // 开放值域字段不校验：osVersion/cpuCount/memory/runtime（版本号/数字）、
        // lib/sdk/script/env/custom/environment（外部名）。
        // 调用时机：MetaCore 阶段 ValidatePlatformConfig 步骤（InjectProjectData 之后、
        // ParseStatements 之前，§13.8）——报出的 Error 计入阶段 errorCount，
        // 由 AbortPhase 策略中止编译；引用模块的 jsonc（ProjectReferenceModuleLoader）
        // 不触发本校验（已编译产物，不应被重新挑错）。
        public static void ValidatePlatformRequireValues(ProjectConfig cfg)
        {
            if (cfg == null || cfg.Platform == null || cfg.Platform.Root == null)
            {
                return;
            }
            ValidateRequireValues(cfg.Platform.Root);
        }

        static void ValidateRequireValues(PlatformReqExpr node)
        {
            if (node == null)
            {
                return;
            }
            if (node.Op == PlatformReqNames.OpAtom)
            {
                if (node.Atom != null)
                {
                    ValidateRequireAtom(node.Atom);
                }
                return;
            }
            foreach (var c in node.Children)
            {
                ValidateRequireValues(c);
            }
        }

        // atom kind + cmp → 定义表（值域映射见 §6.1/§6.2 示例）：
        //   os/arch → 同名表；cpu(all/any Set) → isa；cpuTopology(any) → cpu（拓扑形态）；
        //   render(any Set) → gfx；render(ge Value) → shaderModel（minShaderModel，
        //   "6_0" 容错补 sm_ 前缀）；network(ge Value) → link（minLink）；
        //   embedded(any Set) → mcuFamily（family）；ai/device 的 Key → 同名表。
        static void ValidateRequireAtom(PlatformReqAtom a)
        {
            switch (a.Kind)
            {
                case PlatformReqNames.Os:
                    ValidateAtomValue(a, PlatformDefTables.KindOs, "os", false);
                    ValidateAtomSet(a, PlatformDefTables.KindOs, "os");
                    break;
                case PlatformReqNames.Arch:
                    ValidateAtomValue(a, PlatformDefTables.KindArch, "arch", false);
                    ValidateAtomSet(a, PlatformDefTables.KindArch, "arch");
                    break;
                case PlatformReqNames.Cpu:
                    ValidateAtomSet(a, PlatformDefTables.KindIsa, "cpu");
                    break;
                case PlatformReqNames.CpuTopology:
                    ValidateAtomSet(a, PlatformDefTables.KindCpu, "cpu.topology");
                    break;
                case PlatformReqNames.Render:
                    if (a.Cmp == PlatformReqNames.CmpGe)
                    {
                        ValidateAtomValue(a, PlatformDefTables.KindShaderModel, "render.minShaderModel", true);
                    }
                    else
                    {
                        ValidateAtomSet(a, PlatformDefTables.KindGfx, "render.api");
                    }
                    break;
                case PlatformReqNames.Network:
                    ValidateAtomValue(a, PlatformDefTables.KindLink, "network.minLink", false);
                    break;
                case PlatformReqNames.Embedded:
                    if (a.Cmp == PlatformReqNames.CmpAny || a.Cmp == PlatformReqNames.CmpAll)
                    {
                        ValidateAtomSet(a, PlatformDefTables.KindMcuFamily, "embedded.family");
                    }
                    break;
                case PlatformReqNames.Ai:
                    ValidateAtomKey(a, PlatformDefTables.KindAi, "ai");
                    break;
                case PlatformReqNames.Device:
                    ValidateAtomKey(a, PlatformDefTables.KindDevice, "device");
                    break;
                default:
                    // 开放值域（版本号/数字/外部名）：不校验
                    break;
            }
        }

        // eq/ne/ge 的 Value：命中（含别名）→ 归一为规范名；未命中 → 报错。
        // smTolerance：shaderModel 允许 "6_0" 形式（首查失败后补 sm_ 前缀重试）。
        static void ValidateAtomValue(PlatformReqAtom a, int kind, string fieldName, bool smTolerance)
        {
            if (a.Value.Length == 0)
            {
                return;
            }
            if (PlatformDefTables.TryNormalize(kind, a.Value, out var canonical))
            {
                a.Value = canonical;
                return;
            }
            if (smTolerance && PlatformDefTables.TryNormalize(kind, "sm_" + a.Value, out canonical))
            {
                a.Value = canonical;
                return;
            }
            ReportValueInvalid(fieldName, kind, a.Value);
        }

        // any/all 的 Set：逐个归一/报错；归一后保序去重（如 "win64"+"window" → 单个 "window"）
        static void ValidateAtomSet(PlatformReqAtom a, int kind, string fieldName)
        {
            for (var i = 0; i < a.Set.Count; i++)
            {
                if (PlatformDefTables.TryNormalize(kind, a.Set[i], out var canonical))
                {
                    a.Set[i] = canonical;
                }
                else
                {
                    ReportValueInvalid(fieldName, kind, a.Set[i]);
                }
            }
            for (var i = a.Set.Count - 1; i > 0; i--)
            {
                if (a.Set.IndexOf(a.Set[i]) < i)
                {
                    a.Set.RemoveAt(i);
                }
            }
        }

        // ai/device 的 Key（具名条目）
        static void ValidateAtomKey(PlatformReqAtom a, int kind, string fieldName)
        {
            if (a.Key.Length == 0)
            {
                return;
            }
            if (PlatformDefTables.TryNormalize(kind, a.Key, out var canonical))
            {
                a.Key = canonical;
            }
            else
            {
                ReportValueInvalid(fieldName, kind, a.Key);
            }
        }

        // 报错格式（§13.8）：可选值列表（= Environment.Platform.<枚举>.* 成员名）+ 别名 + 编辑距离纠错
        static void ReportValueInvalid(string fieldName, int kind, string raw)
        {
            var options = "可选的 " + fieldName + " 值（= Environment.Platform."
                + PlatformDefTables.SlEnumName(kind) + ".* 成员名）：" + PlatformDefTables.MembersText(kind);
            var aliases = PlatformDefTables.AliasesText(kind);
            if (aliases.Length > 0)
            {
                options += "；别名：" + aliases;
            }
            var suggest = PlatformDefTables.Suggest(kind, raw);
            var note = suggest.Length > 0 ? "是否想写 '" + suggest + "'？" : "";
            Log.AddProjectLog(LID.ProjectPlatformRequireValueInvalid, "", fieldName, raw, options, note);
        }

        // ── platform.override key 校验 + 归一（§8.5.3，与 §13.8 同款编译前期校验）──
        // key 三形态（§8.5.3 表）：<大类>.<成员> / <大类>整类 / 单段裸成员名
        // （在定义表搜到则补全前缀，"gpu" → "device.gpu"）。带定义表的前缀成员
        // 必须在表中（含别名 → 归一为规范名）；自由文本前缀（osversion/memory/lib/
        // sdk/env/runtime/environment/script/custom…）成员原样放行。归一后的 key
        // 写回 entry 并随 module.json 导出（与 require 别名归一同一理由）。
        // 未命中 → Error 20036（field="platform.override"，含可选值 + 纠错建议）。
        // 调用时机：ValidatePlatformConfig 步骤（与 require 值域校验同时机）。
        public static void ValidatePlatformOverrideKeys(ProjectConfig cfg)
        {
            if (cfg == null || cfg.Platform == null)
            {
                return;
            }
            foreach (var entry in cfg.Platform.Override)
            {
                ValidateOverrideKey(entry);
            }
        }

        static void ValidateOverrideKey(ProjectConfig.PlatformOverrideEntry entry)
        {
            var key = entry.Key;
            var dot = key.IndexOf('.');
            if (dot < 0)
            {
                // 单段：大类名 → 归一为规范小写；裸成员名 → 在定义表搜补全前缀
                var p = PlatformDefTables.FindOvPrefix(key);
                if (p != null)
                {
                    entry.Key = p.Name;
                    return;
                }
                foreach (var q in PlatformDefTables.OvPrefixes)
                {
                    if (PlatformDefTables.OvPrefixMemberKnown(q, key))
                    {
                        TryNormalizePrefixMember(q, key, out var canonical);
                        entry.Key = q.Name + "." + canonical;
                        return;
                    }
                }
                ReportOverrideKeyInvalid(key, null);
                return;
            }
            // 两段：前缀 + 成员（第一个点之后整体为成员，custom 自由文本可再含点）
            var prefix = key.Substring(0, dot);
            var member = key.Substring(dot + 1);
            var prefixEntry = PlatformDefTables.FindOvPrefix(prefix);
            if (prefixEntry == null)
            {
                ReportOverrideKeyInvalid(key, null);
                return;
            }
            if (prefixEntry.DefKind < 0 && prefixEntry.DefKind2 < 0)
            {
                // 自由文本成员：仅归一前缀（成员原样）
                entry.Key = prefixEntry.Name + "." + member;
                return;
            }
            if (member.Length == 0)
            {
                ReportOverrideKeyInvalid(key, prefixEntry);
                return;
            }
            if (TryNormalizePrefixMember(prefixEntry, member, out var canonicalMember))
            {
                entry.Key = prefixEntry.Name + "." + canonicalMember;
                return;
            }
            ReportOverrideKeyInvalid(key, prefixEntry);
        }

        // 前缀成员归一（主表 + 第二表，network 联查 link；大小写不敏感含别名）
        static bool TryNormalizePrefixMember(PlatformDefTables.OvPrefixEntry p, string member, out string canonical)
        {
            if (p.DefKind >= 0 && PlatformDefTables.TryNormalize(p.DefKind, member, out canonical))
            {
                return true;
            }
            if (p.DefKind2 >= 0 && PlatformDefTables.TryNormalize(p.DefKind2, member, out canonical))
            {
                return true;
            }
            canonical = null;
            return false;
        }

        // 报错格式（§8.5.3，复用 20036）：
        // 前缀错/单段未知 → options 列 21 个大类名 + note 建议最近大类；
        // 前缀对成员错 → options 列该前缀全部可选成员（含第二表）+ note 建议最近成员。
        static void ReportOverrideKeyInvalid(string raw, PlatformDefTables.OvPrefixEntry prefixEntry)
        {
            string options;
            string note;
            if (prefixEntry == null)
            {
                var names = new List<string>();
                foreach (var p in PlatformDefTables.OvPrefixes)
                {
                    names.Add(p.Name);
                }
                options = "可选的 platform.override key（21 个大类或 <大类>.<成员>）：" + string.Join(" ", names);
                // 建议只看前缀部分（两段 key 传完整原文会拉大编辑距离）
                var dot = raw.IndexOf('.');
                var prefixOnly = dot >= 0 ? raw.Substring(0, dot) : raw;
                var suggestPrefix = PlatformDefTables.SuggestOvPrefix(prefixOnly);
                note = suggestPrefix.Length > 0 ? "是否想写 '" + suggestPrefix + "'？" : "";
            }
            else
            {
                var kindName = PlatformDefTables.SlEnumName(prefixEntry.DefKind);
                options = "可选的 " + prefixEntry.Name + ".* 成员（= Environment.Platform."
                    + kindName + ".* 成员名）：" + PlatformDefTables.MembersText(prefixEntry.DefKind);
                if (prefixEntry.DefKind2 >= 0)
                {
                    options += "；或 Environment.Platform."
                        + PlatformDefTables.SlEnumName(prefixEntry.DefKind2) + ".*："
                        + PlatformDefTables.MembersText(prefixEntry.DefKind2);
                }
                var member = raw.Substring(raw.IndexOf('.') + 1);
                var suggest = PlatformDefTables.Suggest(prefixEntry.DefKind, member);
                if (suggest.Length == 0 && prefixEntry.DefKind2 >= 0)
                {
                    suggest = PlatformDefTables.Suggest(prefixEntry.DefKind2, member);
                }
                note = suggest.Length > 0 ? "是否想写 '" + prefixEntry.Name + "." + suggest + "'？" : "";
            }
            Log.AddProjectLog(LID.ProjectPlatformRequireValueInvalid, "", "platform.override", raw, options, note);
        }

        static bool TryGetObj(JsonElement root, string name, out JsonElement obj)
        {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out obj) && obj.ValueKind == JsonValueKind.Object)
            {
                return true;
            }
            obj = default;
            return false;
        }

        // 解析 dllImports / lib 数组段：[{ path, name, alias, functions }]，
        // alias 缺省取 name（旧 lib 段只有 path/name，name 即别名）。
        // functions：[{ name, symbol, sig }] 库函数变量（注入 global.<name>）。
        static void ParseDllImportArray(JsonElement array, ProjectConfig cfg)
        {
            foreach (var d in array.EnumerateArray())
            {
                if (d.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }
                var path = GetStr(d, "path", string.Empty);
                var name = GetStr(d, "name", string.Empty);
                var alias = GetStr(d, "alias", string.Empty);
                var staticName = GetStr(d, "static", string.Empty);
                if (string.IsNullOrWhiteSpace(alias))
                {
                    alias = name;
                }
                if (!string.IsNullOrWhiteSpace(path))
                {
                    var sec = new ProjectConfig.DllImportSection() { Path = path, Name = name, Alias = alias, Static = staticName };
                    if (d.TryGetProperty("functions", out var fns) && fns.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var f in fns.EnumerateArray())
                        {
                            if (f.ValueKind != JsonValueKind.Object)
                            {
                                continue;
                            }
                            var fname = GetStr(f, "name", string.Empty);
                            var fsym = GetStr(f, "symbol", string.Empty);
                            var fsig = GetStr(f, "sig", string.Empty);
                            if (string.IsNullOrWhiteSpace(fname) || string.IsNullOrWhiteSpace(fsym))
                            {
                                continue;
                            }
                            if (string.IsNullOrWhiteSpace(fsig))
                            {
                                fsig = "->void";
                            }
                            sec.Functions.Add(new ProjectConfig.DllImportFunctionSection() { Name = fname, Symbol = fsym, Sig = fsig });
                        }
                    }
                    cfg.DllImports.Add(sec);
                }
            }
        }

        static string GetStr(JsonElement obj, string name, string @default)
        {
            if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String)
            {
                return v.GetString() ?? @default;
            }
            return @default;
        }

        static bool GetBool(JsonElement obj, string name, bool @default)
        {
            if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False))
            {
                return v.GetBoolean();
            }
            return @default;
        }

        static int GetInt(JsonElement obj, string name, int @default)
        {
            if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number)
            {
                if (v.TryGetInt32(out var i))
                {
                    return i;
                }
            }
            return @default;
        }

        // Recursively walks the "struct" tree. Each item may carry one of
        // namespace/class/data/enum to identify a node under the current parent;
        // an optional "children" array is parsed the same way, allowing arbitrary
        // nesting depth.
        static void ParseStructNodes(JsonElement array, ProjectConfig.StructTreeNode parent)
        {
            if (array.ValueKind != JsonValueKind.Array || parent == null)
            {
                return;
            }

            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                // Register each identifier present on the item as a child of the
                // current parent. `node` tracks the most recently registered child
                // and serves as the parent for any nested `children`.
                ProjectConfig.StructTreeNode node = parent;

                var ns = GetStr(item, "namespace", null);
                if (!string.IsNullOrWhiteSpace(ns))
                {
                    node = parent.EnsurePath(ns, ProjectConfig.StructTreeNode.NodeType.Namespace);
                }

                var cls = GetStr(item, "class", null);
                if (!string.IsNullOrWhiteSpace(cls))
                {
                    node = parent.EnsurePath(cls, ProjectConfig.StructTreeNode.NodeType.Class);
                }

                var dls = GetStr(item, "data", null);
                if (!string.IsNullOrWhiteSpace(dls))
                {
                    node = parent.EnsurePath(dls, ProjectConfig.StructTreeNode.NodeType.Data);
                }

                var els = GetStr(item, "enum", null);
                if (!string.IsNullOrWhiteSpace(els))
                {
                    node = parent.EnsurePath(els, ProjectConfig.StructTreeNode.NodeType.Enum);
                }

                if (item.TryGetProperty("children", out var childArr) && childArr.ValueKind == JsonValueKind.Array)
                {
                    ParseStructNodes(childArr, node);
                }
            }
        }
    }
}
