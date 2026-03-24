#nullable enable
using System.Collections.Generic;

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
    }

    /// <summary>
    /// Wire shape for one <see cref="SimpleLanguage.IR.IRData"/> in JSON (same field contract VM deserializes into <c>Instruction</c>).
    /// </summary>
    public sealed class SLIRInstructionPackage
    {
        public int id { get; set; }
        public byte opCode { get; set; }
        public byte[]? payload { get; set; }
        public int index { get; set; }
        public int byteLength { get; set; }
        public int offset { get; set; }
    }

    public sealed class SLVariablePackage { public int id { get; set; } public int index { get; set; } public string name { get; set; } = string.Empty; public SLRuntimeDefTypePackage? typeDef { get; set; } }

    public sealed class SLMethodPackage { public string id { get; set; } = string.Empty; public string name { get; set; } = string.Empty; public string declaringTypeFullName { get; set; } = string.Empty; public List<SLVariablePackage> returnList { get; set; } = new(); public List<SLVariablePackage> argumentList { get; set; } = new(); public List<SLVariablePackage> localList { get; set; } = new(); public List<SLIRInstructionPackage> instructionList { get; set; } = new(); }

    public sealed class SLFieldPackage { public string name { get; set; } = string.Empty; public SLRuntimeDefTypePackage? typeDef { get; set; } public int flags { get; set; } public int index { get; set; } public List<SLIRInstructionPackage> express { get; set; } = new(); }

    public sealed class SLClassPackage
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string fullName { get; set; } = string.Empty;
        public string sourcePath { get; set; } = string.Empty;
        public List<SLFieldPackage> fieldList { get; set; } = new();
        public List<SLMethodMeta> nonStaticMethodList { get; set; } = new();
        public List<SLMethodMeta> operatorMethodList { get; set; } = new();
        public List<SLMethodMeta> staticMethodList { get; set; } = new();

        // template export fields used by writer
        public int templateCount { get; set; }
        public List<SLRuntimeDefTypePackage> templateTypeList { get; set; } = new();
        public List<SLTemplateRelationPackage> templateRelationList { get; set; } = new();

    }

    public sealed class SLTypePackage { public string fullName { get; set; } = string.Empty; public string name { get; set; } = string.Empty; public List<SLMethodMeta> methodList { get; set; } = new(); }

    public sealed class SLNamespacePackage { public string fullName { get; set; } = string.Empty; public List<SLTypePackage> typeList { get; set; } = new(); }

    public sealed class SLGlobalStaticVariablePackage { public int id { get; set; } public string name { get; set; } = string.Empty; public int ownerClassId { get; set; } public int index { get; set; } public SLRuntimeDefTypePackage? typeDef { get; set; } public List<SLIRInstructionPackage> express { get; set; } = new(); }

    public sealed class SLAssemblyPackage
    {
        public string moduleName { get; set; } = string.Empty;
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
    /// JSON root written to <c>module.package.json</c>: only <see cref="entryModule"/> and <see cref="moduleList"/>.
    /// Each item in <see cref="moduleList"/> is a full <see cref="SLAssemblyPackage"/> (legacy module shape).
    /// </summary>
    public sealed class SLPackageRootJson
    {
        public string entryModule { get; set; } = string.Empty;
        public List<SLAssemblyPackage> moduleList { get; set; } = new();
    }

    /// <summary>
    /// In-memory / deserialization model: may include legacy top-level fields when reading old files.
    /// </summary>
    public sealed class SLModulePackage
    {
        public string moduleName { get; set; } = string.Empty;
        /// <summary>Which <see cref="SLAssemblyPackage.moduleName"/> in <see cref="moduleList"/> is the entry module.</summary>
        public string? entryModule { get; set; }
        public List<SLAssemblyPackage> moduleList { get; set; } = new();
        /// <summary>Optional copy of the entry module's <c>entryMethodId</c> for loaders that only read the root.</summary>
        public string? entryMethodId { get; set; }
        public List<string> moduleReferences { get; set; } = new();
        public List<IRStringItem> irStringDict { get; set; } = new();
        public List<SLNamespacePackage> namespaceList { get; set; } = new();
        public List<SLClassPackage> classList { get; set; } = new();
        public List<SLGlobalStaticVariablePackage> globalStaticVariableList { get; set; } = new();
        public List<SLMethodPackage> methodList { get; set; } = new();
    }

    public sealed class SLRuntimeDefTypePackage { public int classId { get; set; } public string className { get; set; } = string.Empty; public int ownerClassId { get; set; } public string ownerClassName { get; set; } = string.Empty; public int templateIndex { get; set; } = -1; public bool isTemplate { get; set; } public List<SLRuntimeDefTypePackage> runtimeDefTypeList { get; set; } = new(); }
}
