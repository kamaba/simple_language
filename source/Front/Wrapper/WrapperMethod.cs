//****************************************************************************
//  File:      WrapperMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/21 12:00:00
//  Description: 
//****************************************************************************


using System.Collections.Generic;

namespace SimpleLanguage.Wrapper
{
    public class WrapperInstruction
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
        public EWrapperPayloadType payloadType { get; set; } = EWrapperPayloadType.None;
    }


    public class WrapperMethod
    {
        public string id { get; set; }
        public string onlyFunctionName { get; set; }
        public int argumentCount { get; set; }
        public int localCount { get; set; }
        public bool isPublic { get; set; } = true;
        public bool isStatic { get; set; } = true;
        public List<WrapperInstruction> instructions { get; set; } = new List<WrapperInstruction>();
    }
}
