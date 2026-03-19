using System;
using System.Collections.Generic;

namespace SimpleLanguage.VM.LanguageRuntime
{
    public sealed class SLModulePackage
    {
        public string moduleName { get; set; } = string.Empty;
        public string? entryMethodId { get; set; }
        public List<string> moduleReferences { get; set; } = new();
        public List<IRStringItem> irStringDict { get; set; } = new();
        public List<SLNamespacePackage> namespaceList { get; set; } = new();
        public List<SLClassPackage> classList { get; set; } = new();
        public List<SLGlobalStaticVariablePackage> globalStaticVariableList { get; set; } = new();
        public List<SLGlobalStaticInstructionPackage> globalStaticInstructionList { get; set; } = new();
        public List<SLIRInstructionPackage> globalInitInstructionList { get; set; } = new();
        public List<SLMethodPackage> methodList { get; set; } = new();
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

    public sealed class IRStringItem
    {
        public int id { get; set; }
        public string value { get; set; } = string.Empty;
    }

    public sealed class SLNamespacePackage
    {
        public string fullName { get; set; } = string.Empty;
        public List<SLTypePackage> typeList { get; set; } = new();
    }

    public sealed class SLClassPackage
    {
        public int id { get; set; }
        public string fullName { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string sourcePath { get; set; } = string.Empty;
        public List<SLFieldPackage> fieldList { get; set; } = new();
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

    public sealed class SLTypePackage
    {
        public string fullName { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
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
