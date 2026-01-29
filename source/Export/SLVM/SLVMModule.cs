using System;
using System.Collections.Generic;

namespace SimpleLanguage.Export.SLVM
{
    public enum SLVMPayloadType : byte
    {
        None = 0,
        Int32 = 1,
        Int64 = 2,
        Float32 = 3,
        Float64 = 4,
        String = 5,
        Boolean = 6,
        Byte = 7,
        SByte = 8,
        Int16 = 9,
        UInt16 = 10,
        UInt32 = 11,
        UInt64 = 12
    }

    public class SLVMNamespace
    {
        public string name { get; set; }
        public List<string> children { get; set; } = new List<string>();
    }
    public class SLVMInstruction
    {
        public string opcode { get; set; }
        public int index { get; set; }
        // index into module string pool (-1 if none)
        public int opValueIndex { get; set; } = -1;
        // resolved value (for convenience)
        public string opValue { get; set; }
        // typed payload (for numeric/boolean bytes)
        public byte[] payload { get; set; }
        // payload semantic tag
        public SLVMPayloadType payloadType { get; set; } = SLVMPayloadType.None;
    }


    public class SLVMMethod
    {
        public string id { get; set; }
        public string onlyFunctionName { get; set; }
        public int argumentCount { get; set; }
        public int localCount { get; set; }
        public bool isPublic { get; set; } = true;
        public bool isStatic { get; set; } = true;
        public List<SLVMInstruction> instructions { get; set; } = new List<SLVMInstruction>();
    }

    public class SLVMGlobal
    {
        public string name { get; set; }
        public bool isStatic { get; set; }
        public bool isConst { get; set; }
        // index into string pool for initial value (if any)
        public int initValueIndex { get; set; } = -1;
        public string initValue { get; set; }
        // original IR meta variable id (if exported from IR)
        public int metaId { get; set; } = -1;
    }

    public class SLVMType
    {
        public string name { get; set; }
        public List<(string fieldName, string fieldType)> fields { get; set; } = new List<(string, string)>();
    }

    public class SLVMModule
    {
        public string name { get; set; }
        // unified string pool for literals, names, metadata
        public List<string> stringPool { get; set; } = new List<string>();
        public List<SLVMGlobal> globals { get; set; } = new List<SLVMGlobal>();
        public List<SLVMType> types { get; set; } = new List<SLVMType>();
        // IR meta classes exported for runtime/linker
        public List<SLVMIRMetaClass> irMetaClasses { get; set; } = new List<SLVMIRMetaClass>();
        // namespaces
        public List<SLVMNamespace> namespaces { get; set; } = new List<SLVMNamespace>();
        public List<SLVMMethod> methods { get; set; } = new List<SLVMMethod>();

        public int AddString(string s)
        {
            if (s == null) return -1;
            var i = stringPool.IndexOf(s);
            if (i >= 0) return i;
            stringPool.Add(s);
            return stringPool.Count - 1;
        }

        public SLVMMethod GetMethodById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < methods.Count; i++) if (methods[i].id == id) return methods[i];
            return null;
        }
    }

    public class SLVMIRMetaVariable
    {
        public int id { get; set; }
        public string name { get; set; }
        public int index { get; set; }
        public int from { get; set; }
        public string irMetaType { get; set; }
    }

    public class SLVMIRMetaClass
    {
        public int id { get; set; }
        public string name { get; set; }
        public int byteCount { get; set; }
        public int templateCount { get; set; }
        public bool needInitMemberVariable { get; set; }
        public List<SLVMIRMetaVariable> localVariables { get; set; } = new List<SLVMIRMetaVariable>();
        public List<SLVMIRMetaVariable> staticVariables { get; set; } = new List<SLVMIRMetaVariable>();
    }

    // CLR-like definitions
    public class SLVMFieldDef
    {
        public string name { get; set; }
        public string fieldType { get; set; }
    }

    public class SLVMTypeDef
    {
        public string name { get; set; }
        public List<SLVMFieldDef> fields { get; set; } = new List<SLVMFieldDef>();
    }

    public class SLVMMethodDef
    {
        public string id { get; set; }
        public string onlyFunctionName { get; set; }
        public bool isPublic { get; set; }
        public bool isStatic { get; set; }
        public int argumentCount { get; set; }
        public int localCount { get; set; }
        public int instructionCount { get; set; }
    }

    public class SLVMAssembly
    {
        public string name { get; set; }
        public List<SLVMTypeDef> typeDefs { get; set; } = new List<SLVMTypeDef>();
        public List<SLVMMethodDef> methodDefs { get; set; } = new List<SLVMMethodDef>();
    }
}
