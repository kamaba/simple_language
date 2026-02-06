using System;
using System.Collections.Generic;

namespace SimpleLanguage.VM.LanguageRuntime
{
    public sealed class SLModulePackage
    {
        public string moduleName { get; set; } = string.Empty;
        public List<SLNamespacePackage> namespaceList { get; set; } = new();
        public List<SLMethodPackage> methodList { get; set; } = new();
    }

    public sealed class SLNamespacePackage
    {
        public string fullName { get; set; } = string.Empty;
        public List<SLTypePackage> typeList { get; set; } = new();
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
        public List<SLIRInstructionPackage> instructionList { get; set; } = new();
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
}
