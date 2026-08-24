//****************************************************************************
//  File:      IROpEnum.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage
{
    public enum EIROpCode : byte
    {
        Nop,

        LoadBegin = 1,
        LoadConstNull,
        LoadConstUInt8,
        LoadConstInt8,
        //LoadConstChar,
        LoadConstInt16,
        LoadConstUInt16,
        LoadConstInt32,
        LoadConstUInt32,
        LoadConstInt64,         
        LoadConstUInt64,
        LoadConstFloat8_E4M3,
        LoadConstFloat8_E5M2,
        LoadConstFloat16,
        LoadConstFloat16_Brain,
        LoadConstFloat32,
        LoadConstFloat64,
        LoadConstBoolean,
        LoadConstString,
        LoadConstType,
        
        LoadArgument,
        LoadLocal,  
        LoadNotStaticField,
        LoadArrayIndex,          //数组类成员获取
        LoadArrayIndexField,
        LoadStaticField,
        LoadGlobal,
        
        //ClassInit,

        NewObject,
        NewTemplateObject,
        NewArray,
        Dup,
        Pop,

        StoreLocal,
        StoreNotStaticField1,
        StoreNotStaticField2,
        StoreArrayIndex,             //数组类成员操作
        StoreArrayIndexField,
        StoreStaticField,
        StoreGlobal,
        StoreReturn,

        //SetCurrentClassCallClass,
        //SetCallClass,
        //UnSetCallClass,

        //运算指令
        Add,                   // +
        Minus,                  // -
        Multiply,               // *
        Divide,                 // /
        Modulo,                 // %
        InclusiveOr,            // |
        Combine,                // &
        XOR,                    // ^
        Shr,                    // >>
        Shi,                    // <<
        Not,                    //!
        Neg,                    //-

        Ceq,                    // == 
        Cne,                    // !=
        Cgt,                    // >
        Cge,                    // >=
        Clt,                    // <
        Cle,                    // <=

        And,                    //&&
        Or,                     //||

        Label,   
        Beq,                    // if x1 == x2 then execute(code) equal move instruct index
        Bge,                    // if x1 >= x2 then execute(code)
        Bgt,                    // if x1 > x2 then execute(code)
        Ble,                    // if x1 <= x2 then execute(code)
        Bne,                    // if x1 != x2 then execute(code)
        Br,                     // 
        Break,                  //
        Jmp,
        BrLabel,
        BrFalse,
        BrTrue,
        Switch,
            
        CallStatic,
        CallDynamic,
        CallVirt,
        CallSystemMethod,
        CastClass,

        Convert_I8,
        Convert_SI8,
        Convert_I16,
        Convert_UI16,
        Convert_I32,
        Convert_UI32,
        Convert_I64,
        Convert_UI64,
        Convert_R4,
        Convert_R8,
        Convert_ToString,

        Ret,

        // Exception handling opcodes
        BeginTry,       // Push a try frame. Payload: catchIndex(int32) + finallyIndex(int32)
        EndTry,         // Pop try frame (normal completion). Branch to finally or end.
        Throw,          // Throw exception (value on stack)
        LeaveTry,       // Leave try/catch block. Pop try frame, branch to target.
        EndFinally,     // End of finally. If exception pending, re-throw; else continue.

        // Checked context opcodes (overflow checking for integer arithmetic +, -, *, /, %)
        BeginChecked,   // Enter checked arithmetic context (increment depth)
        EndChecked,     // Exit checked arithmetic context (decrement depth)
        // Unchecked context opcodes (temporarily disable checked within a checked scope)
        BeginUnchecked, // Save current checked depth, set to 0 (opt-out of overflow checking)
        EndUnchecked,   // Restore saved checked depth

        // Parameter slot store: argument/local use independent index spaces,
        // assigning to a parameter must write the argument slot (not StoreLocal).
        StoreArgument,  // = 99

        // Low-precision float conversions (stack top value -> target float bit pattern)
        Convert_F8E4M3, // = 100 float -> float8(e4m3) bits(byte)
        Convert_F8E5M2, // = 101 float -> float8(e5m2) bits(byte)
        Convert_F16,    // = 102 float -> float16 bits(ushort)
        Convert_F16B,   // = 103 float -> float16brain(bfloat16) bits(ushort)
    }

    /// <summary>
    /// Operand-order flag for StoreArrayIndex.
    /// </summary>
    public enum EStoreArrayIndexFlag : byte
    {
        // stack: [..., value, array]
        StoreTopMinus1_ValueTopMinus2 = 0,
        // stack: [..., array, value]
        StoreTopMinus2_ValueTopMinus1 = 1,
    }
}
