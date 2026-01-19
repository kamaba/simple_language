using System;
using System.Collections.Generic;

namespace SimpleLanguage.Export.SLVM
{
    public class SLVMInstruction
    {
        public string opcode { get; set; }
        public int index { get; set; }
        // index into module string pool (-1 if none)
        public int opValueIndex { get; set; } = -1;
        // resolved value (for convenience)
        public string opValue { get; set; }
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
}
