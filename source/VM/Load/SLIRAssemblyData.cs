using SimpleLanguage.VM;

namespace SimpleLanuageVM.Load
{
    // Runtime assembly shell lives in SimpleLanguage.VM.SLAssembly (see SLIRAssemly.cs); JSON DTOs stay here.

    public sealed class IRStringItem
    { 
        public int id { get; set; } 
        public string value { get; set; } = string.Empty; 
    }
    /// <summary>
    /// One physical JSON package: optional root lists for legacy; canonical payload is in <see cref="moduleList"/> (each item is a full module).
    /// </summary>
    public sealed class SLPackageRootJson
    {
        public string? entryModule { get; set; } = string.Empty;
        public List<SLModulePackage> moduleList { get; set; } = new();
    }

    /// <summary>
    /// Loaded package-graph wrapper: one root directory contains one or more
    /// <see cref="SLPackageRootJson"/> nodes in execution/load order.
    /// </summary>
    public sealed class SLPackageGraph
    {
        public string rootPackagePath { get; init; } = string.Empty;
        public string rootDirectory { get; init; } = string.Empty;
        public List<SLPackageRootJson> packageList { get; init; } = new();
    }

    /// <summary>One physical module inside a package (matches Front <c>SLAssemblyPackage</c> JSON).</summary>
    public sealed class SLModulePackage
    {
        public string moduleName { get; set; } = string.Empty;
        public string? entryMethodId { get; set; }
        public List<string> moduleReferences { get; set; } = new();
        public List<IRStringItem> irStringDict { get; set; } = new();
        public List<SLNamespacePackage> namespaceList { get; set; } = new();
        public List<SLClassPackage> classList { get; set; } = new();
        public List<SLGlobalStaticVariablePackage> globalStaticVariableList { get; set; } = new();
        public List<SLMethodPackage> methodList { get; set; } = new();

        public SLModulePackage() { }

        public SLModulePackage(string moduleName)
        {
            this.moduleName = moduleName ?? string.Empty;
        }
    }


    public sealed class SLGlobalStaticVariablePackage
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public int ownerClassId { get; set; }
        public int index { get; set; }
        public SLRuntimeDefTypePackage? typeDef { get; set; }
        public List<SLIRInstructionPackage> express { get; set; } = new();
    }
    public sealed class SLGlobalStaticInstructionPackage
    {
        public int id { get; set; }
        public List<SLIRInstructionPackage> instructionList { get; set; } = new();
    }

    public sealed class SLNamespacePackage
    {
        public string fullName { get; set; } = string.Empty;
        public List<SLTypePackage> typeList { get; set; } = new();

        private readonly List<SLTypePackage> m_TypeList = new();
        public SLNamespacePackage(string fullName)
        {
            this.fullName = fullName ?? string.Empty;
        }

        internal void AddType(SLTypePackage t)
        {
            if (t == null) return;
            m_TypeList.Add(t);
        }
    }

    public sealed class SLClassPackage
    { 
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string fullName { get; set; } = string.Empty;
        public string sourcePath { get; set; } = string.Empty; 
        public List<SLFieldPackage> fieldList { get; set; } = new();
        // per-class method references separated by category
        public List<SLMethodMetaPackage> nonStaticMethodList { get; set; } = new();
        public List<SLMethodMetaPackage> operatorMethodList { get; set; } = new();
        public List<SLMethodMetaPackage> staticMethodList { get; set; } = new();
    }
    public sealed class SLFieldPackage
    {
        public string name { get; set; } = string.Empty;
        // structured type definition
        public SLRuntimeDefTypePackage? typeDef { get; set; }
        // flags: 1(private),2(public),4(export),8(protected),16(const),32(static)
        public int flags { get; set; }
        public int index { get; set; }
        public List<SLIRInstructionPackage> express { get; set; } = new();
    }
    public sealed class SLMethodPackage
    { 
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string onlyName { get; set; } = string.Empty;
        public string declaringTypeFullName { get; set; } = string.Empty;
        public int ownerClassId { get; set; }
        public List<SLVariablePackage> returnList { get; set; } = new();
        public List<SLVariablePackage> argumentList { get; set; } = new();
        public List<SLVariablePackage> localList { get; set; } = new();
        public List<SLIRInstructionPackage> instructionList { get; set; } = new();
    }

    public sealed class SLTypePackage
    {
        public string fullName { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;

        // per-type method references (refer to global methodList by id)
        public List<SLMethodMetaPackage> methodList { get; set; } = new();
        internal void AddMethod(SLMethodMetaPackage m)
        {
            if (m == null) return;
            methodList.Add(m);
        }
    }


    public sealed class SLMethodMetaPackage
    {
        public string id { get; init; } = string.Empty;
        public string name { get; init; } = string.Empty;
        // index marks ordering within the specific per-class list
        public int index { get; init; } = 0;
        // reserved for IR-level per-type representation if needed
        public List<object> irList { get; init; } = new();
        // vm instruction list produced by JSON loader for convenience
        public List<Instruction> vmInstructionList { get; init; } = new();
    }




    public sealed class SLVariablePackage
    {
        public int id { get; set; }
        public int index { get; set; }
        public string name { get; set; } = string.Empty;
        public SLRuntimeDefTypePackage? typeDef { get; set; }
    }

    public sealed class SLIRInstructionPackage
    {
        public int id { get; set; }
        public byte opCode { get; set; }
        public object opValue { get; set; }
        public byte[] payload { get; set; }
        public int index { get; set; }
        public int byteLength { get; set; }
        public int offset { get; set; }
        // Symmetric with Front export: call instructions may include structured runtimeCall object.
        public SLRuntimeCallPackage? runtimeCall { get; set; }
    }

    public sealed class SLRuntimeCallPackage
    {
        public SLRuntimeDefTypePackage? runtimeDefType { get; set; }
        public List<SLRuntimeDefTypePackage> templateRuntimeDefTypeList { get; set; } = new();
        public string methodId { get; set; } = string.Empty;
        public string methodName { get; set; } = string.Empty;
        public int paramCount { get; set; }
    }

    public sealed class SLRuntimeDefTypePackage
    {
        public int classId { get; set; }
        public string className { get; set; } = string.Empty;
        public int ownerClassId { get; set; }
        public string ownerClassName { get; set; } = string.Empty;
        public int templateIndex { get; set; } = -1;
        public bool isTemplate { get; set; }
        public List<SLRuntimeDefTypePackage> runtimeDefTypeList { get; set; } = new();
    }

}
