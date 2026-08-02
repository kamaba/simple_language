#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleLanguage.Export.SLIR.Types
{
    public sealed class IRStringItem { public int id { get; set; } public string value { get; set; } = string.Empty; }

    public sealed class SLTemplateRelationEntry { public int index { get; set; } public SLRuntimeDefTypePackage? type { get; set; } }
    public sealed class SLTemplateRelationPackage { public int relatedClassId { get; set; } public List<SLTemplateRelationEntry> mapping { get; set; } = new(); }

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
    }
    public sealed class SLMethodPackage
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string declaringTypeFullName { get; set; } = string.Empty;
        public bool interfaceMethod { get; set; }
        /// <summary>Method modifier flags: 1=static, 2=final, 4=abstract, 8=override, 16=interface, 32=canRewrite, 64=constructInit, 128=extendParams(params 可变参数)</summary>
        public int flags { get; set; }
        public List<SLVariablePackage> returnList { get; set; } = new();
        public List<SLVariablePackage> argumentList { get; set; } = new();
        public List<SLVariablePackage> localList { get; set; } = new();
        public List<SLIRInstructionPackage> instructionList { get; set; } = new();
    }
    public sealed class SLFieldPackage
    { 
        public string name { get; set; } = string.Empty;
        public SLRuntimeDefTypePackage? typeDef { get; set; } 
        public int flags { get; set; } public int index { get; set; }
        /// <summary>
        /// Member parse order captured at MetaCore time (see <see cref="SimpleLanguage.Core.MetaMemberVariable.parseOrder"/>).
        /// VM loaders sort field initializer expressions by this value so dependent members initialize first.
        /// -1 means unspecified; loaders should treat it as "no order" and keep declaration order as fallback.
        /// </summary>
        public int order { get; set; } = -1;
        public List<SLIRInstructionPackage> express { get; set; } = new(); 
    }

    public sealed class SLClassPackage
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string fullName { get; set; } = string.Empty;
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
        public List<SLRuntimeDefTypePackage> templateTypeList { get; set; } = new();
        public List<SLTemplateRelationPackage> templateRelationList { get; set; } = new();

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
        public List<string> moduleReferences { get; set; } = new();
        public List<IRStringItem> irStringDict { get; set; } = new();
        public List<SLNamespacePackage> namespaceList { get; set; } = new();
        public List<SLClassPackage> classList { get; set; } = new();
        public List<SLGlobalStaticVariablePackage> globalStaticVariableList { get; set; } = new();
        public List<SLMethodPackage> methodList { get; set; } = new();
        public SLAssemblyPackage() { }
        public SLAssemblyPackage(string moduleName)
        {
            this.moduleName = moduleName ?? string.Empty;
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
    /// In-memory / deserialization model. Exported as flat JSON (no moduleList wrapper).
    /// <see cref="moduleList"/> and <see cref="entryModule"/> are JsonIgnored:
    /// they exist only for in-memory backward compat with old-format readers.
    /// </summary>
    public sealed class SLModulePackage
    {
        public string moduleName { get; set; } = string.Empty;
        public string uuid { get; set; } = string.Empty;
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
        public string systemCallsJson { get; set; } = string.Empty;
        public List<string> moduleReferences { get; set; } = new();
        public List<IRStringItem> irStringDict { get; set; } = new();
        public List<SLNamespacePackage> namespaceList { get; set; } = new();
        public List<SLClassPackage> classList { get; set; } = new();
        public List<SLGlobalStaticVariablePackage> globalStaticVariableList { get; set; } = new();
        public List<SLMethodPackage> methodList { get; set; } = new();
    }

    public sealed class SLRuntimeDefTypePackage { public int classId { get; set; } public string className { get; set; } = string.Empty; public int ownerClassId { get; set; } public string ownerClassName { get; set; } = string.Empty; public int templateIndex { get; set; } = -1; public bool isTemplate { get; set; } public List<SLRuntimeDefTypePackage> runtimeDefTypeList { get; set; } = new(); }
}
