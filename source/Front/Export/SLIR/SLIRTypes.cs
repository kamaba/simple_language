#nullable enable
using SimpleLanguage.Core;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SimpleLanguage.Export.SLIR.Types
{
    public sealed class IRStringItem { public int id { get; set; } public string value { get; set; } = string.Empty; }

    /// <summary>
    /// 导出模块的引用关系条目。包含被引用模块的名称、UUID、版本号和路径。
    /// path 是相对于当前模块文件的相对路径，VM 用它来定位并加载引用的模块。
    /// </summary>
    public sealed class SLModuleReferencePackage
    {
        public string name { get; set; } = string.Empty;
        public string uuid { get; set; } = string.Empty;
        public string path { get; set; } = string.Empty;
        public int versionMain { get; set; }
        public int versionSub { get; set; }
        public int versionPatch { get; set; }
    }

    public sealed class SLTemplateRelationEntry { public int index { get; set; } public SLRuntimeDefTypePackage? type { get; set; } }
    public sealed class SLTemplateRelationPackage { public int relatedClassId { get; set; } public List<SLTemplateRelationEntry> mapping { get; set; } = new(); }

    /// <summary>
    /// 外部 dll 导入条目（project.jsonc "dllImports" 段，path/name/alias）。
    /// 随 module.json 导出；引用方加载时合并进自身配置，
    /// 使 @DllImport("别名",...) 与 global.dllImport.别名 免写长路径。
    /// </summary>
    public sealed class SLDllImportPackage
    {
        public string alias { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string path { get; set; } = string.Empty;
    }

    public sealed class SLMethodMeta { public string id { get; set; } = string.Empty; public string name { get; set; } = string.Empty; public int index { get; set; } }

    public sealed class SLRuntimeCallPackage
    {
        public SLRuntimeDefTypePackage? runtimeDefType { get; set; }
        public List<SLRuntimeDefTypePackage> templateRuntimeDefTypeList { get; set; } = new();
        public string methodId { get; set; } = string.Empty;
        public string methodName { get; set; } = string.Empty;
        public int paramCount { get; set; }
        public bool tryCatch { get; set; }
    }

    /// <summary>
    /// Wire payload for <see cref="SimpleLanguage.EIROpCode.CallSystemMethod"/> (system bridge builtins).
    /// </summary>
    public sealed class SLSystemMethodCallPackage
    {
        public string name { get; set; } = string.Empty;
        public int paramCount { get; set; }
        /// <summary>Matches <see cref="SimpleLanguage.ESystemMethodCall"/>; -1 if unknown.</summary>
        public int systemMethodKind { get; set; } = -1;
        /// <summary>Unique int id (<see cref="Project.SystemMethodCallDeclaration.GetIndex"/>);
        /// 0 when the declaration is unknown - VM falls back to name lookup.</summary>
        public int id { get; set; }
    }

    /// <summary>Optional debug snapshot (from <see cref="SimpleLanguage.IR.IRData.debugInfo"/> / token), deserialized into VM <see cref="SimpleLanguage.VM.DebugInfo"/>.</summary>
    public sealed class SLInstructionDebugInfo
    {
        public string path { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public int beginLine { get; set; }
        public int beginChar { get; set; }
        public int endLine { get; set; }
        public int endChar { get; set; }
        public string info { get; set; } = string.Empty;
    }

    /// <summary>
    /// Wire shape for one <see cref="SimpleLanguage.IR.IRData"/> in JSON (same field contract VM deserializes into <c>Instruction</c>).
    /// </summary>
    public sealed class SLIRInstructionPackage
    {
        public int id { get; set; }
        public byte opCode { get; set; }
        public byte[]? payload { get; set; }
        public int byteLength { get; set; }
        public SLInstructionDebugInfo? debugInfo { get; set; }
    }
    public sealed class SLVariablePackage 
    { 
        public int id { get; set; } 
        public int index { get; set; } 
        public string name { get; set; } = string.Empty; 
        public SLRuntimeDefTypePackage? typeDef { get; set; }
        public SLInstructionDebugInfo? debugInfo { get; set; }
        /// <summary>方法参数是否有默认表达式（影响 isMust 匹配）。</summary>
        public bool hasExpress { get; set; }
    }
    public sealed class SLMethodPackage
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        /// <summary>多个导出名称时用逗号分隔（含 @Nickname 别名）；为 null 或空时回退到 name。</summary>
        public string? exportNames { get; set; }
        public string declaringTypeFullName { get; set; } = string.Empty;
        /// <summary>声明该方法的类的 classId（按 allName 的确定型哈希）。
        /// 对于继承到子类的方法，此 id 指向声明类（如 Object），而非当前子类（如 Num）。
        /// 导入侧据此把 MetaMemberFunction 的 owner 设为声明类。</summary>
        public int declaringClassId { get; set; }
        public bool interfaceMethod { get; set; }
        /// <summary>Method modifier flags: 1=static, 2=final, 4=abstract, 8=override, 16=interface, 32=canRewrite, 64=constructInit, 128=extendParams(params 可变参数), 256=aot(@AOT() 标记)</summary>
        public int flags { get; set; }
        /// <summary>是否为模板函数（fun&lt;T&gt;()）。导入侧据此恢复 MetaTemplate 参数。</summary>
        public bool isTemplateFunction { get; set; }
        /// <summary>模板函数声明的模板参数名列表（如 fun&lt;TKey,TValue&gt;() -> ["TKey","TValue"]）。
        /// 导入侧用这些名称重建 MetaTemplate，使 functionAllName 和 classId 匹配导出端。</summary>
        public List<string> templateParameterNames { get; set; } = new();
        public List<SLVariablePackage> returnList { get; set; } = new();
        public List<SLVariablePackage> argumentList { get; set; } = new();
        public List<SLVariablePackage> localList { get; set; } = new();
        public List<SLIRInstructionPackage> instructionList { get; set; } = new();
        public List<SLAttributePackage> attributeList { get; set; } = new();
    }
    /// <summary>
    /// Serialized attribute data for export/import.
    /// Carries the attribute name and extracted string arguments so that
    /// the VM loader can reconstruct runtime attributes (Route, Condition, etc.)
    /// without needing the full MetaCore/FileMeta parse tree.
    /// </summary>
    public sealed class SLAttributePackage
    {
        public string name { get; set; } = string.Empty;
        public List<string> args { get; set; } = new();
        /// <summary>0=Compile, 1=Runtime - mirrors EAttributeHandleType from SL</summary>
        public int handleType { get; set; }
    }

    public sealed class SLFieldPackage
    {
        public string name { get; set; } = string.Empty;
        /// <summary>多个导出名称时用逗号分隔（含 @Nickname 别名）；为 null 或空时回退到 name。</summary>
        public string? exportNames { get; set; }
        public SLRuntimeDefTypePackage? typeDef { get; set; } 
        public int flags { get; set; } public int index { get; set; }
        /// <summary>
        /// Member parse order captured at MetaCore time (see <see cref="SimpleLanguage.Core.MetaMemberVariable.parseOrder"/>).
        /// VM loaders sort field initializer expressions by this value so dependent members initialize first.
        /// -1 means unspecified; loaders should treat it as "no order" and keep declaration order as fallback.
        /// </summary>
        public int order { get; set; } = -1;
        public List<SLIRInstructionPackage> express { get; set; } = new();
        public List<SLAttributePackage> attributeList { get; set; } = new();
    }

    public sealed class SLClassPackage
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string fullName { get; set; } = string.Empty;
        /// <summary>多个导出名称时用逗号分隔（含 @Nickname 别名）；为 null 或空时回退到 name。</summary>
        public string? exportNames { get; set; }
        public string sourcePath { get; set; } = string.Empty;
        /// <summary>Matches <see cref="SimpleLanguage.IR.IRMetaClassKind"/> (0=Class, 1=Enum, 2=Data, 3=Interface).</summary>
        public int metaClassKind { get; set; }
        /// <summary>True when this exported data type is anonymous/dynamic data.</summary>
        public bool isDynamic { get; set; }
        /// <summary>IR class id of the base/extend class (same id scheme as <see cref="id"/>); 0 if none.</summary>
        public int baseClassId { get; set; }
        /// <summary>IR class ids of interfaces this type implements (same id scheme as <see cref="id"/>), including from the base class chain in Meta.</summary>
        public List<int> implementsInterfaceIdList { get; set; } = new();
        public List<SLFieldPackage> fieldList { get; set; } = new();
        public List<SLMethodMeta> nonStaticMethodList { get; set; } = new();
        public List<SLMethodMeta> operatorMethodList { get; set; } = new();
        public List<SLMethodMeta> staticMethodList { get; set; } = new();

        // template export fields used by writer
        public int templateCount { get; set; }
        /// <summary>Declared template arity in source (e.g. <c>Foo&lt;T,U&gt;</c> → 2). <see cref="templateCount"/> is IR-generated template meta type count.</summary>
        public int templateParameterCount { get; set; }
        /// <summary>Declared template parameter names in source (e.g. Map&lt;TKey,TValue&gt; -> ["TKey","TValue"]).
        /// The importer must rebuild MetaTemplate with these exact names so that allName
        /// (e.g. "Std.Map&lt;TKey,TValue&gt;") and its FNV classId match the exporter;
        /// normalizing to T/T1 would break classId-based method lookup for multi-parameter templates.</summary>
        public List<string> templateParameterNames { get; set; } = new();
        public List<SLRuntimeDefTypePackage> templateTypeList { get; set; } = new();
        public List<SLTemplateRelationPackage> templateRelationList { get; set; } = new();

        /// <summary>Class-level attributes exported for runtime use (Route, Condition, Nickname, etc.)</summary>
        public List<SLAttributePackage> attributeList { get; set; } = new();

    }

    public sealed class SLTypePackage
    {
        public string fullName { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public List<SLMethodMeta> methodList { get; set; } = new();
        /// <summary>Same as <see cref="SLClassPackage.templateParameterCount"/> for this type.</summary>
        public int templateParameterCount { get; set; }
    }

    public sealed class SLNamespacePackage { public string fullName { get; set; } = string.Empty; public List<SLTypePackage> typeList { get; set; } = new(); }

    public sealed class SLGlobalStaticVariablePackage { public int id { get; set; } public string name { get; set; } = string.Empty; public int ownerClassId { get; set; } public int index { get; set; } public SLRuntimeDefTypePackage? typeDef { get; set; } public List<SLIRInstructionPackage> express { get; set; } = new(); }

    public sealed class SLAssemblyPackage
    {
        public string moduleName { get; set; } = string.Empty;
        public string uuid { get; set; } = string.Empty;
        public string? entryMethodId { get; set; }
        public List<SLModuleReferencePackage> moduleReferences { get; set; } = new();
        public List<IRStringItem> irStringDict { get; set; } = new();
        public List<SLNamespacePackage> namespaceList { get; set; } = new();
        public List<SLClassPackage> classList { get; set; } = new();
        public List<SLGlobalStaticVariablePackage> globalStaticVariableList { get; set; } = new();
        public List<SLMethodPackage> methodList { get; set; } = new();
        public List<SLSystemCallPackage> systemCalls { get; set; } = new();
        public SLAssemblyPackage() { }
        public SLAssemblyPackage(string moduleName)
        {
            this.moduleName = moduleName ?? string.Empty;
        }
    }

    public sealed class SLSystemCallPackage
    {
        public string name { get; set; } = string.Empty;
        public string returnType { get; set; } = string.Empty;
        public List<string> @params { get; set; } = new();
        public bool isVariadic { get; set; } = false;
        /// <summary>Unique int id (<see cref="Project.SystemMethodCallDeclaration.GetIndex"/>).
        /// The VM reads it at module load and registers id -> implementation for O(1) dispatch.</summary>
        public int id { get; set; }
        /// <summary>C VM builtin implementation symbol name (e.g. "vm_sys_ptr_alloc").
        /// The VM resolves it by symbol lookup at module load; empty when no C implementation exists.</summary>
        public string cvmFunction { get; set; } = string.Empty;

        public override string ToString()
        {
            //StringBuilder sb = new StringBuilder();
            //sb.Append(name);
            return name;
        }
    } 

    /// <summary>
    /// Legacy JSON root (entryModule + moduleList). Only used for reading old-format files;
    /// new exports use flat <see cref="SLModulePackage"/> directly.
    /// </summary>
    public sealed class SLPackageRootJson
    {
        public string entryModule { get; set; } = string.Empty;
        public string uuid { get; set; } = string.Empty;
        public List<SLAssemblyPackage> moduleList { get; set; } = new();
    }

    /// <summary>
    /// AOT 方法清单条目（module.json "aot.methods" 数组元素）。
    /// 与独立 &lt;name&gt;_manifest.json 的条目同构：
    /// { "id": "...", "symbol": "...", "status": "ok|failed", "reason": "..." }
    /// </summary>
    public sealed class SLAotMethodPackage
    {
        public string id { get; set; } = string.Empty;
        /// <summary>aot.dll 中导出的符号名（sl_aot_&lt;sanitized-id&gt;）。</summary>
        public string symbol { get; set; } = string.Empty;
        /// <summary>ok = 可原生分发；failed = 降级失败，回退 CVM 解释执行。</summary>
        public string status { get; set; } = string.Empty;
        public string? reason { get; set; }
    }

    /// <summary>
    /// module.json 的 "aot" 字段：AOT 导出清单（原独立 aot_manifest.json 的合并形态）。
    /// VM 加载模块时读取该字段定位并加载 aot.dll，把 status=="ok" 的方法
    /// 注册进原生调用注册表（stage-4）。
    /// </summary>
    public sealed class SLAotPackage
    {
        public string mlir { get; set; } = string.Empty;
        /// <summary>dll 文件名（相对 module.json 同目录）；空 = 仅导出 mlir（SIMPLELANG_AOT_DLL=0）。</summary>
        public string dll { get; set; } = string.Empty;
        public List<SLAotMethodPackage> methods { get; set; } = new();
    }

    /// <summary>
    /// In-memory / deserialization model. Exported as flat JSON (no moduleList wrapper).
    /// <see cref="moduleList"/> and <see cref="entryModule"/> are JsonIgnored:
    /// they exist only for in-memory backward compat with old-format readers.
    /// </summary>
    public sealed class SLModulePackage
    {
        public string moduleName { get; set; } = string.Empty;
        public string uuid { get; set; } = string.Empty;
        public int versionMain { get; set; }
        public int versionSub { get; set; }
        public int versionPatch { get; set; }
        /// <summary>Only used in-memory for old-format reads; not serialized.</summary>
        [JsonIgnore]
        public string? entryModule { get; set; }
        /// <summary>Only used in-memory for old-format reads; not serialized.</summary>
        [JsonIgnore]
        public List<SLAssemblyPackage> moduleList { get; set; } = new();
        public string? entryMethodId { get; set; }
        /// <summary>Raw JSON array text of the module's "systemCalls" declarations
        /// (copied verbatim from the module's .jsonc), so referencing projects can
        /// register them via SystemMethodCallDeclarationRegistry.</summary>
        public List<SLSystemCallPackage> systemCalls { get; set; } = new();
        /// <summary>
        /// 原生 DLL 文件名（如 "MathNativeImpl.dll"）。VM 加载模块时
        /// 自动在模块文件同目录下查找并加载此 DLL（实现 ISLExternalFunctionModule）。
        /// </summary>
        public string nativeDll { get; set; } = string.Empty;
        /// <summary>
        /// AOT 导出清单（原独立 aot_manifest.json 的合并形态）。
        /// MLIR AOT 管线（MLIRExportManager）的产物：dll 文件名 + 每方法
        /// symbol/status。VM 优先从此字段加载 aot.dll（stage-4），
        /// 旧格式（独立 manifest 文件）作为回退。null = 无 AOT 导出。
        /// </summary>
        public SLAotPackage? aot { get; set; }
        /// <summary>
        /// 外部 dll 导入配置（project.jsonc "dllImports" 段的别名/名称/路径）。
        /// 引用方加载本模块时合并进其配置，即可用别名免写长路径。
        /// </summary>
        public List<SLDllImportPackage> dllImports { get; set; } = new();
        public List<SLModuleReferencePackage> moduleReferences { get; set; } = new();
        public List<IRStringItem> irStringDict { get; set; } = new();
        public List<SLNamespacePackage> namespaceList { get; set; } = new();
        public List<SLClassPackage> classList { get; set; } = new();
        public List<SLGlobalStaticVariablePackage> globalStaticVariableList { get; set; } = new();
        public List<SLMethodPackage> methodList { get; set; } = new();
    }

    public sealed class SLRuntimeDefTypePackage { public int classId { get; set; } public string className { get; set; } = string.Empty; public int ownerClassId { get; set; } public string ownerClassName { get; set; } = string.Empty; public int templateIndex { get; set; } = -1; public bool isTemplate { get; set; } public List<SLRuntimeDefTypePackage> runtimeDefTypeList { get; set; } = new(); }
}
