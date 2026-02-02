using System;
using System.Collections.Generic;

namespace SimpleLanguage.Meta
{
    // Java class representation
    public class PEJavaClass
    {
        public int minorVersion { get; set; }
        public int majorVersion { get; set; }
        public List<PEJavaConstantPoolEntry> constantPool { get; set; } = new List<PEJavaConstantPoolEntry>();
        public int accessFlags { get; set; }
        public int thisClassIndex { get; set; }
        public int superClassIndex { get; set; }
        public List<int> interfaces { get; set; } = new List<int>();
        public List<PEJavaFieldInfo> fields { get; set; } = new List<PEJavaFieldInfo>();
        public List<PEJavaMethodInfo> methods { get; set; } = new List<PEJavaMethodInfo>();
        public List<PEJavaAttribute> attributes { get; set; } = new List<PEJavaAttribute>();
    }

    public class PEJavaConstantPoolEntry { public byte tag; public object data; }
    public class PEJavaFieldInfo { public int accessFlags; public int nameIndex; public int descriptorIndex; public List<PEJavaAttribute> attributes = new List<PEJavaAttribute>(); }
    public class PEJavaMethodInfo { public int accessFlags; public int nameIndex; public int descriptorIndex; public List<PEJavaAttribute> attributes = new List<PEJavaAttribute>(); }
    public class PEJavaAttribute { public int nameIndex; public byte[] info; }
}
