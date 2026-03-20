using SimpleLanguage.VM;

namespace SimpleLanuageVM.Load
{

    public sealed class SLAssembly
    {
        public string id { get; }
        public IReadOnlyList<SLModulePackage> moduleList => m_ModuleList;

        private readonly List<SLModulePackage> m_ModuleList = new();

        private static Dictionary<int, string> s_ConstStringDict = new();

        public SLAssembly(string id)
        {
            this.id = id ?? string.Empty;
        }

        internal void AddModule(SLModulePackage m)
        {
            if (m == null) return;
            m_ModuleList.Add(m);
        }
        public static void SetConstStringDict(Dictionary<int, string>? dict)
        {
            s_ConstStringDict = dict ?? new Dictionary<int, string>();
        }

        public static string? TryGetConstString(int id)
        {
            if (s_ConstStringDict != null && s_ConstStringDict.TryGetValue(id, out var s)) return s;
            return null;
        }
    }
    public sealed class IRStringItem
    { 
        public int id { get; set; } 
        public string value { get; set; } = string.Empty; 
    }

    public sealed class SLModulePackage
    {
        public string name { get; }

        private Dictionary<string, SLNamespacePackage> m_NamespaceMap { get; set; } = new(StringComparer.Ordinal);

        public string moduleName { get; set; } = string.Empty;
        public string? entryMethodId { get; set; }
        public List<string> moduleReferences { get; set; } = new();
        public List<IRStringItem> irStringDict { get; set; } = new();
        public List<SLNamespacePackage> namespaceList { get; set; } = new();
        public List<SLClassPackage> classList { get; set; } = new();
        public List<SLClassModel> classes { get; set; } = new();
        public List<SLMethodModel> methods { get; set; } = new();
        public List<SLGlobalStaticVariablePackage> globalStaticVariableList { get; set; } = new();
        public List<SLGlobalStaticInstructionPackage> globalStaticInstructionList { get; set; } = new();
        public List<SLIRInstructionPackage> globalInitInstructionList { get; set; } = new();
        public List<SLMethodPackage> methodList { get; set; } = new();
        public SLModulePackage()
        {

        }
        public SLModulePackage(string name)
        {
            this.name = name ?? string.Empty;
        }

        internal SLNamespacePackage GetOrAddNamespace(string fullName)
        {
            fullName ??= string.Empty;
            if (m_NamespaceMap.TryGetValue(fullName, out var existed)) return existed;
            var ns = new SLNamespacePackage(fullName);
            m_NamespaceMap[fullName] = ns;
            //m_NamespaceList.Add(ns);
            return ns;
        }
    }

    public sealed class SLGlobalStaticVariablePackage
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public int ownerClassId { get; set; }
        public int index { get; set; }
        public string typeName { get; set; } = string.Empty;
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
        public string fullName { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string sourcePath { get; set; } = string.Empty;
        public List<SLFieldPackage> fieldList { get; set; } = new();
    }

    public sealed class SLClassModel 
    { 
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string sourcePath { get; set; } = string.Empty; 
        public List<SLFieldModel> fields { get; set; } = new();
    }
    public sealed class SLFieldModel
    { 
        public string name { get; set; } = string.Empty; 
        public string type { get; set; } = string.Empty;
        public bool isStatic { get; set; } 
        public int index { get; set; }
    }
    public sealed class SLMethodModel
    { 
        public string id { get; set; } = string.Empty;
        public string onlyName { get; set; } = string.Empty; 
        public int ownerClassId { get; set; }
        public int argumentCount { get; set; } 
        public int localCount { get; set; } 
        public int returnCount { get; set; } 
        public List<SLInstructionModel> instructions { get; set; } = new();
    }
    public sealed class SLInstructionModel
    { 
        public string opCode { get; set; } = string.Empty; 
        public int index { get; set; } 
        public int offset { get; set; } 
        public string? payloadBase64 { get; set; }
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

        public IReadOnlyList<SLMethodMeta> methodList => m_MethodList;
        private readonly List<SLMethodMeta> m_MethodList = new();

        internal void AddMethod(SLMethodMeta m)
        {
            if (m == null) return;
            m_MethodList.Add(m);
        }
    }


    public sealed class SLMethodMeta
    {
        public string id { get; init; } = string.Empty;
        public string name { get; init; } = string.Empty;
        public IReadOnlyList<object> irList { get; init; } = Array.Empty<object>();
        public IReadOnlyList<Instruction> vmInstructionList { get; init; } = Array.Empty<Instruction>();
    }


    public sealed class SLFieldPackage
    {
        public string name { get; set; } = string.Empty;
        public string typeName { get; set; } = string.Empty;
        public bool isStatic { get; set; }
        public bool isConst { get; set; }
        public int flags { get; set; }
        public int index { get; set; }
        public List<SLIRInstructionPackage> express { get; set; } = new();
    }

    public sealed class SLMethodPackage
    {
        public string id { get; set; } = string.Empty;
        public string declaringTypeFullName { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public List<SLVariablePackage> returnList { get; set; } = new();
        public List<SLVariablePackage> argumentList { get; set; } = new();
        public List<SLVariablePackage> localList { get; set; } = new();
        public List<SLIRInstructionPackage> instructionList { get; set; } = new();
    }

    public sealed class SLVariablePackage
    {
        public int id { get; set; }
        public int index { get; set; }
        public string name { get; set; } = string.Empty;
        public string typeName { get; set; } = string.Empty;
    }

    public sealed class SLIRInstructionPackage
    {
        public int id { get; set; }
        public byte opCode { get; set; }
        public object opValue { get; set; }
        public SLRuntimeCallPackage? runtimeCall { get; set; }
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
}
