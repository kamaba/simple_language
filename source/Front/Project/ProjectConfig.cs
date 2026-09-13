using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SimpleLanguage.Project
{
    /// <summary>
    /// 平台条件的稳定字符串名（PLATFORM_CAPABILITY_DESIGN.md §5.2 / §7.1）。
    /// 与 C 侧 csimple_lang/src/vm/platform/sl_requirement.c 的
    /// g_req_kind_names / g_req_cmp_names 逐项对称（全小写），
    /// 导出端必须使用这些常量拼 JSON，禁止手写字面量（防错位，同 R2 思想）。
    /// </summary>
    public static class PlatformReqNames
    {
        // ── kind（19 项）──
        public const string Os = "os";
        public const string OsVersion = "osversion";
        public const string Arch = "arch";
        public const string Cpu = "cpu";
        public const string CpuCount = "cpucount";
        public const string Memory = "memory";
        public const string Lib = "lib";
        public const string Sdk = "sdk";
        public const string Env = "env";
        public const string Runtime = "runtime";
        public const string Device = "device";
        public const string Environment = "environment";
        public const string CpuTopology = "cputopology";
        public const string Ai = "ai";
        public const string Render = "render";
        public const string Network = "network";
        public const string Script = "script";
        public const string Embedded = "embedded";
        public const string Custom = "custom";

        // ── cmp（9 项）──
        public const string CmpExists = "exists";
        public const string CmpEq = "eq";
        public const string CmpNe = "ne";
        public const string CmpGe = "ge";
        public const string CmpGt = "gt";
        public const string CmpLe = "le";
        public const string CmpLt = "lt";
        public const string CmpAny = "any";
        public const string CmpAll = "all";

        // ── op（4 项）──
        public const string OpAtom = "atom";
        public const string OpAnd = "and";
        public const string OpOr = "or";
        public const string OpNot = "not";

        /// <summary>compile.target 受控枚举（§11.1：加载时校验，非法值报错）。</summary>
        public static readonly string[] AllowedTargets =
        {
            "AnyCPU", "x86", "x64", "arm64", "wasm32"
        };

        /// <summary>compile.target → 同族架构名（arch 要求的可交集值，§11.1 一致性检查）。</summary>
        public static string[] TargetArchFamily(string target)
        {
            switch (target)
            {
                case "x86":
                    return new string[] { "x86" };
                case "x64":
                    return new string[] { "x86_64" };
                case "arm64":
                    return new string[] { "aarch64", "arm64" };
                case "wasm32":
                    return new string[] { "wasm32", "wasm" };
                default:
                    return Array.Empty<string>();
            }
        }
    }

    /// <summary>
    /// 原子条件（设计 §5.2）：kind + cmp + key/value/set + optional。
    /// 内存模型（jsonc 解析产物）；导出时打平到 module.json 的
    /// "platform"."root" 节点（§7.1，atom 字段直接放节点本层）。
    /// </summary>
    public sealed class PlatformReqAtom
    {
        /// <summary>条件类型（PlatformReqNames 的 19 个 kind 之一）。</summary>
        public string Kind { get; set; } = string.Empty;
        /// <summary>比较方式（PlatformReqNames 的 9 个 cmp 之一）。</summary>
        public string Cmp { get; set; } = PlatformReqNames.CmpExists;
        /// <summary>名：os 名 / arch 名 / 环境变量名 / lib 名 / custom key 等。</summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>期望值（cmp==exists 时可为空；数字统一转字符串）。</summary>
        public string Value { get; set; } = string.Empty;
        /// <summary>候选集合（cmp==any/all 时用）。</summary>
        public List<string> Set { get; set; } = new List<string>();
        /// <summary>软要求：不满足只警告降级，不阻止加载（§6.3）。</summary>
        public bool Optional { get; set; }
    }

    /// <summary>
    /// 条件表达式（设计 §5.3）：Atom 的 AND/OR/NOT 组合树。
    /// Op==atom 时 Atom 有效；否则 Children 有效（NOT 取 Children[0]）。
    /// </summary>
    public sealed class PlatformReqExpr
    {
        /// <summary>组合方式（PlatformReqNames 的 4 个 op 之一）。</summary>
        public string Op { get; set; } = PlatformReqNames.OpAtom;
        public PlatformReqAtom Atom { get; set; }
        public List<PlatformReqExpr> Children { get; set; } = new List<PlatformReqExpr>();

        /// <summary>用单个 atom 建一个 atom 表达式节点。</summary>
        public static PlatformReqExpr OfAtom(PlatformReqAtom atom)
        {
            return new PlatformReqExpr { Op = PlatformReqNames.OpAtom, Atom = atom };
        }

        /// <summary>把多个子表达式 AND 组合（0 个返回 null=无要求，1 个直接返回）。</summary>
        public static PlatformReqExpr AndOf(IReadOnlyList<PlatformReqExpr> children)
        {
            if (children == null || children.Count == 0)
            {
                return null;
            }
            if (children.Count == 1)
            {
                return children[0];
            }
            var e = new PlatformReqExpr { Op = PlatformReqNames.OpAnd };
            foreach (var c in children)
            {
                e.Children.Add(c);
            }
            return e;
        }
    }

    public class RuntimeEnv
    {
        public enum EEnvType
        {
            SLVM = 0,
            CLR = 1,
            Java = 2
        }
        public enum ERunInOS
        {
            None = 0,
            Windows = 1,
            Linux = 2,
            MacOS = 3,
            Android = 4,
            iOS = 5,
            Other,
        }
        public EEnvType envType { get; set; } = EEnvType.SLVM;
        public ERunInOS runInOS { get; set; } = ERunInOS.None;
    }
    // Strongly-typed representation of project <ProjectName>.jsonc
    public class ProjectConfig
    {
        public RuntimeEnv RuntimeEnvironment { get; set; } = new RuntimeEnv();
        public ProjectSection Project { get; set; } = new ProjectSection();
        public SourceSection Source { get; set; } = new SourceSection();
        public CompileSection Compile { get; set; } = new CompileSection();
        public CompileFilesSection CompileFiles { get; set; } = new CompileFilesSection();
        public CompileFilterSection CompileFilter { get; set; } = new CompileFilterSection();
        public GlobalSection Global { get; set; } = new GlobalSection();
        /// <summary>
        /// jsonc ???? <c>"data"</c>?????? <c>Project</c> ????????????????? <see cref="GlobalSection.Data"/>????
        /// </summary>
        public Dictionary<string, JsonElement> JsoncProjectData { get; set; } = new Dictionary<string, JsonElement>();
        public StructTreeNode StructTree { get; set; } = new StructTreeNode();
        public List<ReferenceSection> References { get; set; } = new List<ReferenceSection>();
        /// <summary>
        /// 外部 dll 导入配置（jsonc "dllImports" 段，兼容旧 "lib" 段）：
        /// path（库路径）/ name（名称）/ alias（别名）。
        /// @DllImport("别名",...) 与 global.dllImport.别名 通过别名解析为完整路径，
        /// 免在代码里写长路径；随 module.json 导出后引用方同样可用。
        /// </summary>
        public List<DllImportSection> DllImports { get; set; } = new List<DllImportSection>();
        /// <summary>
        /// cvm 扩展 DLL 导入配置（jsonc "vmDlls" 段）：
        /// project（VS 工程文件，相对 jsonc 所在目录）/ name（产出 DLL 文件名）/
        /// configuration（缺省 Debug）/ platform（缺省 x64）。
        /// 导出时 Front 负责用 MSBuild 编译该工程并把 DLL 拷到 module.json 同目录，
        /// 随 module.json 的 vmDllImports 字段导出；cvm 加载 module.json 时按
        /// package 目录预加载这些 DLL，供 systemCalls 的 "DllName!symbol" 解析。
        /// </summary>
        public List<VmDllSection> VmDlls { get; set; } = new List<VmDllSection>();
        public List<SystemCallItem> systemCalls { get; set; } = new List<SystemCallItem>();
        public ExportSection Export { get; set; } = new ExportSection();
        /// <summary>
        /// 平台能力声明（jsonc "platform" 段，PLATFORM_CAPABILITY_DESIGN.md §6.1）：
        /// targets（目标环境列表，仅元数据）+ require（运行条件，编译期转条件 AST）+
        /// when（表达式糖，v2 再做一致性校验）+ fallbackHint（不满足时给用户的建议）。
        /// require 各字段默认 AND、数组内默认 OR；默认不声明=导出时不写 platform 段。
        /// </summary>
        public PlatformSection Platform { get; set; } = new PlatformSection();

        /// <summary>
        /// jsonc "platform" 段（§6.1）。Declared 标记 jsonc 中是否真的出现了
        /// "platform" 键——只有声明过才随 module.json 导出 platform 段，
        /// 未声明导出 null（CVM 视为无平台要求，恒通过，§7.2）。
        /// </summary>
        public class PlatformSection
        {
            /// <summary>目标环境（"windows" / "any" 等小写标签，仅元数据不参与校验）。</summary>
            public List<string> Targets { get; set; } = new List<string>();
            /// <summary>require 的条件 AST（编译期产物）；null=无运行条件。</summary>
            public PlatformReqExpr Root { get; set; }
            /// <summary>when 表达式糖原文（仅存储；require 同时给时以 require 为准，§6.3）。</summary>
            public string When { get; set; } = string.Empty;
            /// <summary>不满足时的用户建议（随导出，诊断输出用，§10.1）。</summary>
            public string FallbackHint { get; set; } = string.Empty;
            /// <summary>override 通道（§8.5.2 jsonc 内嵌默认降级）：key→开/关/值。
            /// 编译期校验 + 归一 key（ValidatePlatformOverrideKeys），随 module.json 导出。</summary>
            public List<PlatformOverrideEntry> Override { get; set; } = new List<PlatformOverrideEntry>();
            /// <summary>require.network.probe（§6.1 / L1665）：加载期 L3 在线探测开关。
            /// 是行为指令而非条件，不进条件 AST；true 时随 platform 段导出
            /// networkProbe=true，CVM 装载期显式触发一次外网探测（默认关）。</summary>
            public bool NetworkProbe { get; set; }
            /// <summary>多目标变体（§14）：一套源码分发多种产物。
            /// CVM 装载期按数组顺序取第一个 sl_require_check 通过的变体；
            /// 全不匹配 → 诊断 + 拒绝。选中变体的 aot 路径覆盖模块级 aot.dll。
            /// 空 = 未声明（v1 单 require 语义，§14）。</summary>
            public List<PlatformVariantSection> Variants { get; set; } = new List<PlatformVariantSection>();
            /// <summary>jsonc 中是否出现 "platform" 键（导出条件）。</summary>
            public bool Declared { get; set; }
        }

        /// <summary>jsonc platform.variants 的单项（§14）：
        /// Target（目标标签，仅诊断展示）+ Aot（该变体的 AOT 产物文件名，
        /// 空=解释器兜底变体）+ Root（该变体的 require 条件 AST）。</summary>
        public class PlatformVariantSection
        {
            public string Target { get; set; } = string.Empty;
            public string Aot { get; set; } = string.Empty;
            /// <summary>变体 require 的条件 AST；null=无条件（恒真兜底）。</summary>
            public PlatformReqExpr Root { get; set; }
        }

        /// <summary>jsonc platform.override 的单项（§8.5.2）：
        /// Key 原样（校验时归一写回）；值语义——True/False → ENABLE/DISABLE，
        /// Number/String → SET（Value 存原文：数字 GetRawText、字符串 GetString）。</summary>
        public class PlatformOverrideEntry
        {
            public enum ValueKindEnum { True, False, Number, String }

            public string Key { get; set; } = string.Empty;
            public ValueKindEnum Kind { get; set; } = ValueKindEnum.String;
            /// <summary>SET 值原文（True/False 时为空串）。</summary>
            public string Value { get; set; } = string.Empty;
        }

        public class ProjectSection
        {
            public string Name { get; set; } = string.Empty;
            public Guid guid { get; set; } = Guid.NewGuid();
            public string Desc { get; set; } = string.Empty;
        }

        public class SourceSection
        {
            public string Root { get; set; } = "source";
            public string EntryFile { get; set; } = "Main.sl";
        }

        public class SystemCallItem
        {
            public string name;
            public string returnType;

            public string[] @params;

            public bool isVariadic;

            /// <summary>C VM builtin implementation symbol name (e.g. "vm_sys_ptr_alloc"); empty when no C implementation exists.</summary>
            public string cvmFunction;
        }

        public class StructTreeNode
        {
            public enum NodeType
            {
                Root,
                Namespace,
                Class,
                Data,
                Interface,
                Enum,
                Method,
                Property,
                Field
            }
            public string Name { get; set; } = string.Empty;
            public NodeType Type { get; set; }  = NodeType.Root;
            public List<StructTreeNode> Children { get; set; } = new List<StructTreeNode>();
    
            // Build or extend a path under this node using a dotted name like "Std.Console".
            // The last segment gets the specified leafType; intermediate segments default to Namespace.
            public StructTreeNode EnsurePath(string dottedName, NodeType leafType)
            {
                if (string.IsNullOrEmpty(dottedName))
                {
                    return this;
                }

                var parts = dottedName.Split('.');
                var current = this;

                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    bool isLeaf = (i == parts.Length - 1);
                    var expectedType = isLeaf ? leafType : NodeType.Namespace;

                    // Try to find an existing child with same name and type
                    StructTreeNode child = null;
                    for (int j = 0; j < current.Children.Count; j++)
                    {
                        var c = current.Children[j];
                        if (c.Name == part && c.Type == expectedType)
                        {
                            child = c;
                            break;
                        }
                    }

                    if (child == null)
                    {
                        child = new StructTreeNode
                        {
                            Name = part,
                            Type = expectedType
                        };
                        current.Children.Add(child);
                    }

                    current = child;
                }

                return current;
            }
        }

        public class CompileSection
        {
            public bool Optimize { get; set; }
            public string Target { get; set; } = "AnyCPU";
            public bool Debug { get; set; } = true;
            public bool IsUseForceSemiColonInLineEnd { get; set; } = true;
            // Force all classes to use class key even if not strictly necessary ??????class?????
            public bool IsForceUseKeyClass { get; set; }
            // Support C-style ++/-- operators ???++/--??????
            public bool IsSupportDoublePlus { get; set; }

            /// <summary>
            /// ? true ???????/��?????????????????????????? byte+byte??Int32+Int32??????? byte+Int32 ?????
            /// ? false??????????? <see cref="MetaTypeFactory.CalcETypeByLeftAndRight"/> ?????????????
            /// </summary>
            public bool RequireSameNumericTypes { get; set; } = false;
        }

        // mirror CompileFileData / CompileFileDataUnit
        public class CompileFilesSection
        {
            public List<CompileFileItem> Files { get; set; } = new List<CompileFileItem>();
        }

        public class CompileFileItem
        {
            public string Path { get; set; } = string.Empty;
            public string Group { get; set; } = string.Empty;
            public string Tag { get; set; } = string.Empty;
            public bool Ignore { get; set; } = false;
            public int Priority { get; set; } = 0;
        }

        // mirror CompileFilterData
        public class CompileFilterSection
        {
            public List<string> Groups { get; set; } = new List<string>();
            public List<string> Tags { get; set; } = new List<string>();
            public bool IsAllGroup { get; set; } = false;
            public bool IsAllTag { get; set; } = false;

            public bool IsIncludeInGroup(string group)
            {
                if (IsAllGroup) return true;
                if (Groups == null || Groups.Count == 0) return true;
                return Groups.Contains(group);
            }

            public bool IsIncludeInTag(string tag)
            {
                if (IsAllTag) return true;
                if (Tags == null || Tags.Count == 0) return true;
                return Tags.Contains(tag);
            }
        }

        // merge several global-related pieces into one section
        public class GlobalSection
        {
            public List<string> Imports { get; set; } = new List<string>();
            public Dictionary<string, string> Replace { get; set; } = new Dictionary<string, string>();
            // project jsonc: global.data = { key: primitive|object }����λ�ã������ "data" �ϲ�ע�� Project���������ȣ�
            public Dictionary<string, JsonElement> Data { get; set; } = new Dictionary<string, JsonElement>();

            // project jsonc: global.macro = { key: primitive }
            // 静态编译(static if)专用数据，初始值来自 jsonc，
            // 只允许在 Project.CompileBefore() 中通过 global.macro.xxx = 常量 修改。
            public Dictionary<string, JsonElement> Macro { get; set; } = new Dictionary<string, JsonElement>();
        }

        public class ExportSection
        {
            // Module name to produce (overrides Project.Name when non-empty)
            public string ModuleName { get; set; } = string.Empty;
            // Output directory for exported artifacts
            public string OutputDir { get; set; } = "Export/SLVMCode";
            // Pack string pool into a single blob with offsets/lengths
            public bool StringPoolAsBlob { get; set; } = true;
            // Only export public methods
            public bool ExportPublicOnly { get; set; } = false;
            // Include additional metadata like owner class id, visibility flags
            public bool IncludeMetadata { get; set; } = true;

            public int VersionMain { get; set; } = 0;
            public int VersionSub { get; set; } = 1;
            public int VersionPatch { get; set; } = 0;

            /// <summary>
            /// 原生 DLL 文件名（不含路径）。编译后写入 module.json，
            /// VM 加载模块时会自动在同目录下查找并加载此 DLL（实现 ISLExternalFunctionModule）。
            /// </summary>
            public string NativeDll { get; set; } = string.Empty;

            /// <summary>MLIR AOT 导出开关（jsonc "export"."aot" 段）。</summary>
            public AotExportSection Aot { get; set; } = new AotExportSection();

            public DebugTextExportSection DebugText { get; set; } = new DebugTextExportSection();
        }

        /// <summary>
        /// jsonc "export"."aot" 段：MLIR AOT 导出管线（stage 1-3.5）的开关。
        /// 工具链路径（mlir-opt/llc/link）不走这里——由 MLIRToolchain 按固定
        /// 顺序探测（exe 目录 auto-deploy 副本 → tools\llvm → vswhere）。
        /// </summary>
        public class AotExportSection
        {
            /// <summary>AOT 总开关。false = 整个导出管线跳过（全部走 CVM 解释执行）。</summary>
            public bool Enabled { get; set; } = true;
            /// <summary>stage-3 dll 构建。false = 只导出 aot.mlir，不构建 aot.dll。</summary>
            public bool BuildDll { get; set; } = true;
            /// <summary>llc -mtriple 目标三元组（如 "x86_64-pc-windows-msvc"，§11.4）。空 = llc 宿主默认。</summary>
            public string Triple { get; set; } = string.Empty;
            /// <summary>llc -mcpu 目标 CPU（如 "x86-64-v3"，§11.4）。空 = llc 宿主默认。</summary>
            public string Cpu { get; set; } = string.Empty;
            /// <summary>llc -mattr 特性列表（如 "+avx2,+fma"，"-name" 为禁用，§11.4）。空 = llc 宿主默认；非空时与 platform.require.cpu 做一致性校验（Error 20037）。</summary>
            public string Features { get; set; } = string.Empty;
        }

        public class DebugTextExportSection
        {
            public string OutputDir { get; set; } = "DebugCode";
            public bool Code { get; set; } = true;
            public bool Token { get; set; } = true;
            public bool Node { get; set; } = true;
            public bool File { get; set; } = true;
            public bool Meta { get; set; } = true;
            public bool IR { get; set; } = true;
        }

        public class ReferenceSection
        {
            public string Path { get; set; } = string.Empty;
            public string UUID { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        public class DllImportFunctionSection
        {
            public string Name { get; set; } = string.Empty;
            public string Symbol { get; set; } = string.Empty;
            public string Sig { get; set; } = string.Empty;
        }

        public class DllImportSection
        {
            public string Path { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Alias { get; set; } = string.Empty;
            /// <summary>静态绑定名称（jsonc "static" 字段）：非空时该库在 cvm 加载期即预载并注册到 FFI.StaticLibrary，句柄持续到进程退出。</summary>
            public string Static { get; set; } = string.Empty;
            public List<DllImportFunctionSection> Functions { get; set; } = new List<DllImportFunctionSection>();
        }

        /// <summary>cvm 扩展 DLL 导入条目（jsonc "vmDlls" 数组元素）。</summary>
        public class VmDllSection
        {
            /// <summary>VS 工程文件路径（.vcxproj 等），相对 jsonc 所在目录。</summary>
            public string Project { get; set; } = string.Empty;
            /// <summary>产出 DLL 文件名（不含路径），如 "MathVMLib.dll"。</summary>
            public string Name { get; set; } = string.Empty;
            /// <summary>MSBuild Configuration，缺省 Debug。</summary>
            public string Configuration { get; set; } = "Debug";
            /// <summary>MSBuild Platform，缺省 x64。</summary>
            public string Platform { get; set; } = "x64";
        }

        /// <summary>
        /// 按别名（或 name / 配置的 path 本身）查找外部 dll 的完整路径。
        /// 未命中返回 null，调用方按直接路径处理。
        /// </summary>
        public string ResolveDllImportPath(string aliasOrPath)
        {
            if (string.IsNullOrWhiteSpace(aliasOrPath) || DllImports == null)
            {
                return null;
            }
            foreach (var d in DllImports)
            {
                if (d == null || string.IsNullOrWhiteSpace(d.Path))
                {
                    continue;
                }
                if (d.Alias == aliasOrPath || d.Name == aliasOrPath || d.Path == aliasOrPath)
                {
                    return d.Path;
                }
            }
            return null;
        }
    }
}
