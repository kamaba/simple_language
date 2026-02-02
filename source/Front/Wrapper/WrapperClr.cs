using System;
using System.Collections.Generic;

namespace SimpleLanguage.Meta
{
    // CLR-like definitions
    public class WrapperCLRTypeDef
    {
        public string namespaceName { get; set; }
        public string name { get; set; }
        public int attributes { get; set; }
        public int baseTypeDefIndex { get; set; } = -1; // index into typeDefs or typeRef
        public List<int> fieldList { get; set; } = new List<int>(); // indices into fieldDefs
        public List<int> methodList { get; set; } = new List<int>(); // indices into methodTable
    }

    public class WrapperCLRFieldDef
    {
        public string name { get; set; }
        public int flags { get; set; }
        public int signatureOffset { get; set; }
    }

    public class WrapperCLRMethodDef
    {
        public string name { get; set; }
        public int rvaOrBodyOffset { get; set; } = -1;
        public int implFlags { get; set; }
        public int flags { get; set; }
        public int signatureOffset { get; set; }
        public int paramListStart { get; set; }
    }

    public class WrapperCLRMethodBody
    {
        public int methodDefIndex { get; set; }
        public byte[] ilCode { get; set; }
        public int maxStack { get; set; }
        public int localSigIndex { get; set; }
        //public List<PEExceptionClause> exceptionClauses { get; set; } = new List<PEExceptionClause>();
        // other attributes may follow
    }
}
