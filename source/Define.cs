﻿
using System;

namespace SimpleLanguage
{
    //前置权限
    public enum EPermission
    {
        Null,
        Public,
        Internal,
        Protected,
        Private
    }
    //前置类型
    public enum EType : Byte
    {
        None,
        Null,
        Void,
        Class,
        Enum,
        Boolean,
        Bit,
        Byte,
        SByte,
        Char,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Float,
        Int64,
        UInt64,
        Double,
        Int128,
        UInt128,
        Decimal,
        Array,
        String,
    }

    //token类型
    public enum ETokenType
    {
        /// <summary>  </summary>
        None = 0,
        /// <summary> void </summary>
        Void,
        /// <summary>
        /// int/int32/int16/short/long/float/double
        /// </summary>
        Type,
        /// <summary> { </summary>
        LeftBrace,      
        /// <summary> } </summary>
        RightBrace,
        /// <summary> ( </summary>
        LeftPar,
        /// <summary> ) </summary>
        RightPar,
        /// <summary> [ </summary>
        LeftBracket,
        /// <summary> ] </summary>
        RightBracket,
        /// <summary> . </summary>
        Period,
        /// <summary> @ </summary>
        At,
        /// <summary> $ </summary>
        Dollar,
        /// <summary> & </summary>
        Address,
        /// <summary> , </summary>
        Comma,
        /// <summary> : </summary>
        Colon,
        /// <summary> :: </summary>
        ColonDouble,
        /// <summary> ; </summary>
        SemiColon,
        /// <summary> ? </summary>
        QuestionMark,
        /// <summary> ?? </summary>
        EmptyRet,
        /// <summary> ?. </summary>
        QuestionMarkDot,
        /// <summary> + </summary>
        Plus,
        ///<summary> ++ </summary>
        DoublePlus,
        /// <summary> += </summary>
        PlusAssign,
        /// <summary> - </summary>
        Minus,
        ///<summary> ++ </summary>
        DoubleMinus,
        /// <summary> -= </summary>
        MinusAssign,
        /// <summary> * </summary>
        Multiply,
        /// <summary> *= </summary>
        MultiplyAssign,
        /// <summary> / </summary>
        Divide,
        /// <summary> /= </summary>
        DivideAssign,
        /// <summary> % 模运算 </summary>
        Modulo,
        /// <summary> %= </summary>
        ModuloAssign,
        /// <summary> | 或运算 </summary>
        InclusiveOr,
        /// <summary> |= </summary>
        InclusiveOrAssign,
        /// <summary> || </summary>
        Or,
        /// <summary> & 并运算 </summary>
        Combine,
        /// <summary> &= </summary>
        CombineAssign,
        /// <summary> && </summary>
        And,
        /// <summary> ^ 异或 </summary>
        XOR,
        /// <summary> ^= </summary>
        XORAssign,
        /// <summary>  ~ 取反操作 </summary>
        Negative,
        /// <summary> << 左移 </summary>
        Shi,
        /// <summary> >> 右移 </summary>
        Shr,
        /// <summary> # </summary>
        Sharp,
        /// <summary> ! </summary>
        Not,
        /// <summary> = </summary>
        Assign,
        /// <summary> == </summary>
        Equal,
        /// <summary> === </summary>
        ValueEqual,
        /// <summary> != </summary>
        NotEqual,
        /// <summary> !== </summary>
        ValueNotEqual,
        /// <summary> > </summary>
        Greater,
        /// <summary> >= </summary>
        GreaterOrEqual,
        /// <summary>  < </summary>
        Less,
        /// <summary> <= </summary>
        LessOrEqual,
        /// <summary> 1..2 </summary>
        NumberArrayLink,
        /// <summary> => </summary>
        Lambda,
        /// <summary> if </summary>
        If,
        /// <summary> else </summary>
        Else,
        /// <summary> elif </summary>
        ElseIf,
        /// <summary> $define </summary>
        MacroDefine,
        /// <summary> $if </summary>
        MacroIf,
        /// <summary> $ifndef </summary>
        MacroIfndef,
        /// <summary> $else </summary>
        MacroElse,
        /// <summary> $elif </summary>
        MacroElif,
        /// <summary> $endif </summary>
        MacroEndif,
        /// <summary> import </summary>
        Import,
        /// <summary> as </summary>
        As,
        /// <summary> switch </summary>
        Switch,
        /// <summary> case </summary>
        Case,
        /// <summary> default </summary>
        Default,
        /// <summary> break </summary>
        Public,
        /// <summary> public </summary>
        Internal,
        /// <summary> internal </summary>
        Projected,
        /// <summary> projected</summary>
        Private,
        /// <summary> private </summary>
        Interface,
        /// <summary> virtual </summary>
        Override,
        /// <summary> override </summary>
        Const,
        /// <summary> const </summary>
        Final,
        /// <summary> final </summary>
        Static,
        /// <summary> static </summary>
        Get,
        /// <summary> get </summary>
        Set,
        /// <summary> set </summary>
        New,
        /// <summary> new </summary>
        Partial,
        /// <summary> partial </summary>
        Namespace,
        /// <summary> namespace </summary>
        Class,
        /// <summary> class </summary>
        Break,
        /// <summary> continue </summary>
        Next,
        /// <summary> next </summary>
        Continue,
        /// <summary> goto </summary>
        Goto,
        /// <summary> transience </summary>
        Transience,
        /// <summary> return </summary>
        Return,
        /// <summary> label </summary>
        Label,
        /// <summary> while </summary>
        While,
        /// <summary> dowhile </summary>
        DoWhile,
        /// <summary> for </summary>
        For,
        /// <summary> in </summary>
        In,
        /// <summary> function </summary>
        Function,
        /// <summary> try </summary>
        Try,
        /// <summary> catch </summary>
        Catch,
        /// <summary> throw </summary>
        Throw,
        /// <summary> number </summary>
        Number,
        /// <summary> string </summary>
        String,
        /// <summary> null </summary>
        Null,
        /// <summary> object </summary>
        Object,
        /// <summary> this </summary>
        This,
        /// <summary> base </summary>
        Base,
        /// <summary> boolean </summary>
        Boolean,
        /// <summary> 标识符 </summary>
        Identifier,
        /// <summary> 结束 </summary>
        Finished,
    }

    public enum EOpSign
    {
        Plus,
        Minus,
        Multiply,
        Divide,
        Modulo,
        InclusiveOr,
        Or,
        Combine,
        And,
        XOR, 
        Negative,
        Shi,
        Shr, 
        Not
    }

    public enum EParseState
    {
        Null,
        Begin,
        End
    }
    public class SignComputePriority
    {
        public const int Level1 = 1;                         //(a+b) [] . 优先操作，对象操作等
        public const int Level2_LinkOp = 2;                        // -负号 (int)强转 ++x x++ -- ! ~ 
        public const int Level3_Hight_Compute = 3;                 // / * % 
        public const int Level3_Low_Compute = 4;                   // + - 
        public const int Level5_BitMoveOp = 5;              // << >> 
        public const int Level6_Compare = 6;                //< > <= >=
        public const int Level7_EqualAb = 7;                // == !=
        public const int Level8_BitAndOp = 81;           // &
        public const int Level8_BitXOrOp = 82;            // ^
        public const int Level8_BitOrOp = 83;               // |
        public const int Level9_And = 91;                   // &&
        public const int Level9_Or = 92;                    // ||
        public const int Level10_ThirdOp = 100;             // ? : 
        public const int Level11_Assign = 120;              // = /= *= %= += -= <<= >>= &= ^= |= 
        public const int Level12_Split = 130;                //,
    }

    class Define
    {

    }
    public class Global
    {
        public const string tabChar = "    ";
    }
}