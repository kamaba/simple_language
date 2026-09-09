using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SimpleLanguage.Project
{
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
