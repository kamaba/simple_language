using SimpleLanguage.VM;

namespace SimpleLanuageVM.Load
{
    public sealed class SLAssembly
    {
        public string id { get; }
        public IReadOnlyList<SLAssemblyPackage> moduleList => m_ModuleList;

        private readonly List<SLAssemblyPackage> m_ModuleList = new();

        public readonly SLAssemblyPackage m_MainModule = new SLAssemblyPackage("main");

        public SLAssembly(string id)
        {
            this.id = id ?? string.Empty;
        }

        internal void AddModule(SLAssemblyPackage m)
        {
            if (m == null) return;
            m_ModuleList.Add(m);
        }
    }
    public sealed class IRStringItem
    { 
        public int id { get; set; } 
        public string value { get; set; } = string.Empty; 
    }
    public sealed class SLPackageModuleReferences
    {
        public int guid { get; set; }
        public string name { get; set; }
        public string path { get; set; }
    }

    public sealed class SLAssemblyPackage
    {
        public string name { get; } = string.Empty;
        public string? entryMethodId { get; set; }
        public List<SLPackageModuleReferences> moduleReferences { get; set; } = new();
        public List<IRStringItem> irStringDict { get; set; } = new();
        public List<SLNamespacePackage> namespaceList { get; set; } = new();
        public List<SLClassPackage> classList { get; set; } = new();
        public List<SLGlobalStaticVariablePackage> globalStaticVariableList { get; set; } = new();
        public List<SLMethodPackage> methodList { get; set; } = new();
        public SLAssemblyPackage()
        {

        }
        public SLAssemblyPackage(string name)
        {
            this.name = name ?? string.Empty;
        }

        internal SLNamespacePackage GetOrAddNamespace(string fullName)
        {
            fullName ??= string.Empty;
            //if (namespaceList.TryGetValue(fullName, out var existed)) return existed;
            //var ns = new SLNamespacePackage(fullName);
            //namespaceList[fullName] = ns;
            //m_NamespaceList.Add(ns);
            return null;// ns;
        }
        public void SetConstStringDict(Dictionary<int, string>? dict)
        {
            //irStringDict = dict ?? new Dictionary<int, string>();
        }

        public string? TryGetConstString(int id)
        {
            //if (irStringDict != null && irStringDict.TryGetValue(id, out var s)) return s;
            return null;
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
    public sealed class SLPackageGraph
    {
        public string rootPackagePath { get; init; } = string.Empty;
        public string rootDirectory { get; init; } = string.Empty;
        public List<SLModulePackage> packageList { get; init; } = new();
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

    // VM-side module package schema used by SLIR loader and parser.
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
    }
}
