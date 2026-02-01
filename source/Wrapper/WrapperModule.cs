//****************************************************************************
//  File:      WrapperModule.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/21 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Collections.Generic;

namespace SimpleLanguage.Wrapper
{
    public class WrapperModule
    {
        public string name { get; set; }
        public string guid { get; set; } = "";
        public int versionMajor { get; set; } = 0;
        public int versionMinor { get; set; } = 0;
        public int versionPatch { get; set; } = 0;
        public List<WrapperImportModule> importModules { get; set; } = new List<WrapperImportModule>();
        // unified string pool for literals, names, metadata
        public List<string> stringPool { get; set; } = new List<string>();
        public List<WrapperGlobal> globals { get; set; } = new List<WrapperGlobal>();
        // namespaces
        public List<WrapperPathNode> nodeTree { get; set; } = new List<WrapperPathNode>();
        // IR meta classes exported for runtime/linker
        public List<WrapperClass> wrapperClassList { get; set; } = new List<WrapperClass>();
        public List<WrapperMethod> methods { get; set; } = new List<WrapperMethod>();
        public List<WrapperMethod> methodDefs { get; set; } = new List<WrapperMethod>();
        // heaps
        public List<byte> blobHeap { get; set; } = new List<byte>();
        public int AddString(string s)
        {
            if (s == null) return -1;
            var i = stringPool.IndexOf(s);
            if (i >= 0) return i;
            stringPool.Add(s);
            return stringPool.Count - 1;
        }

        public WrapperMethod GetMethodById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < methods.Count; i++) if (methods[i].id == id) return methods[i];
            return null;
        }
    }
    public class WrapperImportModule
    {
        public string name { get; set; }
    }
    public class WrapperEnv
    {
    }
    public class WrapperGlobal
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
    public class WrapperPathNode
    {
        public string name { get; set; }
        public List<WrapperPathNode> children { get; set; } = new List<WrapperPathNode>();
        public List<int> typeDefIndices { get; set; } = new List<int>();
    }
}
