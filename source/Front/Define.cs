//****************************************************************************
//  File:      Define.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************

using System;

namespace SimpleLanguage
{
    //前置权限
    public enum EPermission
    {
        Null,
        Export,
        Public,
        Protected,
        Private
    }
    //前置类型
    public enum EType : byte
    {
        None,
        Null,
        Void,
        Class,
        Enum,
        Data,
        Boolean,
        Num,
        Bit,
        UInt8,
        Int8,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Float16,
        Float32,
        Int64,
        UInt64,
        Float64,
        Int128,
        UInt128,
        Array,
        Range,
        String,
        Object,
        Type,
        Float2,
        Member,
        Ptr,
        Result,
        ResultT,
    }
    //token类型
    public enum ETokenType : byte
    {
        /// <summary>  </summary>
        None = 0,
        /// <summary> space  </summary>
        Space,
        /// <summary> void </summary>
        Void,
        /// <summary>
        /// int/int32/int16/short/long/half/float/double/float3x3
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
        /// <summary> ; </summary>
        SemiColon,
        /// <summary> \n </summary>
        LineEnd,
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
        ///// <summary> << 左移 </summary>
        Shi,
        ///// <summary> >> 右移 </summary>
        Shr,
        /// <summary> <<= 左移赋值 </summary>
        ShiAssign,
        /// <summary> >>= 右移赋值 </summary>
        ShrAssign,
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
        /// <summary> params </summary>
        Params,
        /// <summary> => </summary>
        Lambda,
        /// <summary> if </summary>
        If,
        /// <summary> else </summary>
        Else,
        /// <summary> elif </summary>
        ElseIf,
        /// <summary> !if </summary>
        MacroIf,
        /// <summary> !else </summary>
        MacroElse,
        /// <summary> !endif </summary>
        MacroEndif,
        /// <summary> import </summary>
        Import,
        /// <summary> as </summary>
        As,
        /// <summary> is </summary>
        Is,
        /// <summary> isnot </summary>
        IsNot,
        /// <summary> switch </summary>
        Switch,
        /// <summary> case </summary>
        Case,
        /// <summary> default </summary>
        Default,
        /// <summary> extern  </summary>
        Extern,
        /// <summary> public </summary>
        Public,
        /// <summary> Export </summary>
        Export,
        /// <summary> projected </summary>
        Projected,
        /// <summary> private</summary>
        Private,
        /// <summary> Interface </summary>
        Interface,
        /// <summary> extends </summary>
        Extends,
        /// <summary> bind </summary>
        Bind,
        /// <summary> virtual </summary>
        //Virtual,
        /// <summary> override </summary>
        Override,
        /// <summary> const </summary>
        Const,
        /// <summary> mut </summary>
        Mut,
        /// <summary> final </summary>
        Final,
        /// <summary> static </summary>
        Static,
        /// <summary> get </summary>
        Get,
        /// <summary> set </summary>
        Set,
        /// <summary> let </summary>
        Let,
        /// <summary> new </summary>
        New,
        /// <summary> partial </summary>
        Partial,
        /// <summary> abstract </summary>
        Abstract,
        /// <summary> namespace </summary>
        Namespace,
        /// <summary> class </summary>
        Class,
        /// <summary> enum </summary>
        Enum,
        /// <summary> data </summary>
        Data,
        /// <summary> dynamic </summary>
        Dynamic,
        /// <summary> break </summary>
        Break,
        /// <summary> next </summary>
        Next,
        /// <summary> continue </summary>
        Continue,
        /// <summary> goto </summary>
        Goto,
        /// <summary> transience </summary>
        Transience,
        /// <summary> return </summary>
        Return,
        /// <summary> operator </summary>
        Operator,
        /// <summary> local </summary>
        Local,
        /// <summary> global </summary>
        Global,
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
        /// <summary> out </summary>
        Out,
        /// <summary> function </summary>
        Function,
        /// <summary> try </summary>
        Try,
        /// <summary> try? </summary>
        TryQuestion,
        /// <summary> try! </summary>
        TryExclamation,
        /// <summary> catch </summary>
        Catch,
        /// <summary> finally </summary>
        Finally,
        /// <summary> throw </summary>
        Throw,
        /// <summary> defer </summary>
        Defer,
        /// <summary> errdefer </summary>
        ErrDefer,
        /// <summary> checked </summary>
        Checked,
        /// <summary> unchecked </summary>
        Unchecked,
        /// <summary> BoolValue </summary>
        BoolValue,
        /// <summary> number </summary>
        Number,
        /// <summary> NumberReal </summary>
        NumberReal,
        /// <summary> string </summary>
        String,
        /// <summary> null </summary>
        Null,
        /// <summary> var </summary>
        Var,
        /// <summary> object </summary>
        Object,
        /// <summary> this </summary>
        This,
        /// <summary> base </summary>
        Base,
        /// <summary> array </summary>     
        Array,
        /// <summary> range </summary>     
        Range,
        /// <summary> boolean </summary>   
        Boolean,
        /// <summary> complex </summary>
        Complex,
        /// <summary> 标识符 </summary>
        Identifier,
        /// <summary> async </summary>
        Async,
        /// <summary> await </summary>
        Await,
        /// <summary> throws </summary>
        Throws,

        /// <summary> typealias </summary>
        TypeAlias,

        Float2, Float3, Float4,
        /// <summary> float extent </summary>
        Float2x2, 
        Float2x3, Float3x2, Float3x3,
        Float4x2, Float4x3, Float4x4, Float2x4, Float3x4,
        /// <summary> floatNxN extent </summary>
        Double2, Double3, Double4,
        /// <summary> double extent </summary>
        Double2x2,
        Double2x3, Double3x2, Double3x3,
        Double4x2, Double4x3, Double4x4, Double2x4, Double3x4,
        /// <summary> doubleNxN extent </summary>
        Matrix2x2,
        Matrix2x3, Matrix3x2, Matrix3x3,
        Matrix4x2, Matrix2x4, Matrix3x4, Matrix4x3, Matrix4x4,

        /// <summary> 结束 </summary>
        Finished,
    }

    public enum EOpSign
    {
        None,
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

    // System-level builtin method calls handled by the runtime/native bridge
    public enum ESystemMethodCall
    {
        SystemCallCLRMethod,
        SystemCallNativeMethod,
        SystemCallJVMMethod,
        SystemPrint,
        SystemPrintln,
        SystemReadLine,
        SystemReadKey,
        SystemConvertBool,
        SystemConvertInt8,
        SystemConvertUInt8,
        SystemConvertInt16,
        SystemConvertUInt16,
        SystemConvertInt32,
        SystemConvertUInt32,
        SystemConvertInt64,
        SystemConvertUInt64,
        SystemConvertFloat32,
        SystemConvertFloat64,
        SystemConvertString,
        SystemEqualObject,
        SystemObjectGetType,
        SystemObjectGetHashCode,
        SystemObjectRef,
        SystemObjectRefWeak,
        SystemObjectRefCount,
        SystemObjectFree,
        SystemObjectRelease,
        SystemArrayGetValueThis,
        SystemArraySetValueThis,
        SystemInt32Parse,
        SystemNumAbs,
        SystemNumFloor,
        SystemStringFormat,
        SystemStringFront,
        SystemStringEnd,
        SystemStringRange,
        SystemStringToUInt8Array,
        /// <summary>data 定义类型与成员缓冲区均相同（见 <c>md/syntax/data.md</c>）。</summary>
        DataAllEqual,
        /// <summary>data 字段排列的格式/形状相同（标量族、数组、嵌套 data 结构）。</summary>
        DataTypeEqual,
        /// <summary>data 字段名与各字段类型签名相同。</summary>
        DataNameAndTypeEqual,
        /// <summary>data 字段值相同（数值类型可宽化兼容，如 int8 与 int32）。</summary>
        DataDataEqual,
        /// <summary>Build data/anonymous-data string representation.</summary>
        SystemBuildDataString,
        SystemConvertSInt8,

        // Memory management (Memory.sl)
        SystemMemoryRefCount,
        SystemMemoryRetain,
        SystemMemoryFree,
        SystemMemoryRelease,
        SystemMemoryManual,
        SystemMemoryAuto,
        SystemMemoryIsManual,
        SystemMemoryCollect,
        SystemMemoryCollectThreshold,
        SystemMemoryGetObjectCount,
        SystemMemoryGetGcCycleCount,
        SystemMemoryGetGcFreedCount,
        SystemMemorySetGcThreshold,
        SystemMemoryGetGcThreshold,
        SystemMemoryKeepAlive,
        SystemMemoryWeakRef,
        SystemMemoryIsWeakRefValid,
        SystemMemoryGetTotalAllocated,
        SystemMemoryGetTotalFreed,
        SystemMemorySetMode,
        SystemMemoryClone,
        SystemStringLength,

        // Math – must stay in sync with VM ESystemMethodCall.cs (ordinal parity;
        // the C# VM dispatches CallSystemMethod by systemMethodKind number).
        SystemMathSin,
        SystemMathCos,
        SystemMathTan,
        SystemMathAsin,
        SystemMathAcos,
        SystemMathAtan,
        SystemMathAtan2,
        SystemMathSinh,
        SystemMathCosh,
        SystemMathTanh,
        SystemMathPow,
        SystemMathSqrt,
        SystemMathExp,
        SystemMathLog,
        SystemMathLog10,
        SystemMathCeil,
        SystemMathFloor,
        SystemMathRound,
        SystemMathTruncate,

        /// <summary>调用外部 DLL 注册的函数，第一个参数为函数名（字符串），后续为实际参数。</summary>
        SystemCallExternalFunction,

        // List<T> native container operations (List.sl)
        /// <summary>初始化 List 内部存储，参数: (this, capacity)</summary>
        SystemListInit,
        /// <summary>获取 List 内部存储的元素，参数: (this, index)</summary>
        SystemListGetValueThis,
        /// <summary>设置 List 内部存储的元素，参数: (this, index, value)</summary>
        SystemListSetValueThis,
        /// <summary>获取 List 内部存储容量，参数: (this)</summary>
        SystemListGetCapacity,
        /// <summary>设置 List 内部存储容量（扩缩容），参数: (this, newCapacity)</summary>
        SystemListSetCapacity,

        SystemListRemoveValueThis,
        SystemListRemoveIndexValueThis,
        SystemListClearValueThis,

        // ---- Console I/O (input) ----
        /// <summary>从标准输入读取一行（直到回车），返回 string。</summary>
        SystemInput,

        /// <summary>数组区间填充，参数: (this, startIndex, length, value)</summary>
        SystemArrayFillValue,

        /// <summary>数组扩缩容并拷贝已有元素，参数: (this, newCapacity)，返回新数组</summary>
        SystemArrayResize,
        /// <summary>数组区间右移一位并插入元素，参数: (this, index, length, value)</summary>
        SystemArrayInsertValue,
        /// <summary>数组区间左移一位并清空末位，参数: (this, index, length)</summary>
        SystemArrayRemoveAtValue,
        /// <summary>数组前 length 个元素拷贝到新数组，参数: (this, length)，返回新数组</summary>
        SystemArrayCopy,
        /// <summary>数组按值查找并移除首个匹配元素（左移补位），参数: (this, item, length)，返回被移除的索引（-1 未找到）</summary>
        SystemArrayRemoveValue,
        /// <summary>Int32 按指定进制(2-36)转字符串，参数: (this, radix)，返回 string</summary>
        SystemConvertInt32ToRadixString,
        /// <summary>Map indexOfKey 原生查找：遍历内部数组，按 entity.key 比较定位，参数(this._list, key, length)，返回首个匹配下标（-1 未找到）</summary>
        SystemMapIndexOfKey,
        /// <summary>Map findEntry 哈希表查找：从 buckets[bucket] 读取链头，遍历 entries 桶链，按 entity.hashId + entity.key 比较定位，参数(entries, buckets, key, hash, bucket)，返回匹配下标（-1 未找到）</summary>
        SystemMapFindEntry,

        // ---- Timer / time (Timer.sl) ----
        /// <summary>高精度单调时钟（毫秒），用于 Stopwatch / Timer 场景，参数: 无，返回: int64</summary>
        SystemTimerClock,
        /// <summary>Unix 时间戳（毫秒），参数: 无，返回: int64</summary>
        SystemTimerNowMillis,
        /// <summary>睡眠指定毫秒数，参数: (int32 milliseconds)，返回: void</summary>
        SystemSleep,

        // ---- File operations (File.sl) ----
        /// <summary>判断文件是否存在，参数: (string path)，返回: bool</summary>
        SystemFileExists,
        /// <summary>删除文件，参数: (string path)，返回: bool</summary>
        SystemFileDelete,
        /// <summary>复制文件，参数: (string src, string dst)，返回: bool</summary>
        SystemFileCopy,
        /// <summary>移动/重命名文件，参数: (string src, string dst)，返回: bool</summary>
        SystemFileMove,
        /// <summary>获取文件大小（字节），参数: (string path)，返回: Int64</summary>
        SystemFileGetSize,
        /// <summary>读取文件全部文本，参数: (string path)，返回: string</summary>
        SystemFileReadAllText,
        /// <summary>写入文件全部文本，参数: (string path, string content)，返回: bool</summary>
        SystemFileWriteAllText,
        /// <summary>追加文本到文件，参数: (string path, string content)，返回: bool</summary>
        SystemFileAppendText,

        // ---- Directory operations (Directory.sl) ----
        /// <summary>判断目录是否存在，参数: (string path)，返回: bool</summary>
        SystemDirectoryExists,
        /// <summary>创建目录（含父目录），参数: (string path)，返回: bool</summary>
        SystemDirectoryCreate,
        /// <summary>删除空目录，参数: (string path)，返回: bool</summary>
        SystemDirectoryDelete,
        /// <summary>获取当前工作目录，参数: 无，返回: string</summary>
        SystemDirectoryGetCurrent,
        /// <summary>设置当前工作目录，参数: (string path)，返回: bool</summary>
        SystemDirectorySetCurrent,
        /// <summary>列出目录下的文件/子目录名（换行分隔），参数: (string path)，返回: string</summary>
        SystemDirectoryGetFiles,

        // ---- Path operations ----
        /// <summary>合并两个路径，参数: (string path1, string path2)，返回: string</summary>
        SystemPathCombine,
        /// <summary>获取路径中的目录部分，参数: (string path)，返回: string</summary>
        SystemPathGetDirectory,
        /// <summary>获取路径中的文件名部分，参数: (string path)，返回: string</summary>
        SystemPathGetFilename,
        /// <summary>获取路径中的扩展名，参数: (string path)，返回: string</summary>
        SystemPathGetExtension,
        /// <summary>获取绝对路径，参数: (string path)，返回: string</summary>
        SystemPathGetFull,
        /// <summary>判断是否为绝对路径，参数: (string path)，返回: bool</summary>
        SystemPathIsAbsolute,

        // ---- Environment operations (Environment.sl) ----
        /// <summary>获取环境变量值，参数: (string name)，返回: string</summary>
        SystemEnvironmentGetVariable,
        /// <summary>设置环境变量，参数: (string name, string value)，返回: bool</summary>
        SystemEnvironmentSetVariable,

        // ---- Guid operations (Guid.sl) ----
        /// <summary>生成新 GUID 字符串，参数: 无，返回: string</summary>
        SystemGuidNewGuid,

        // ---- Random operations (Random.sl) ----
        /// <summary>获取随机种子，参数: 无，返回: Int32</summary>
        SystemGeneralRandomSeed,
    }
    public class Global
    {
        public const string tabChar = "    ";

        /// <summary>
        /// VM_PTR_SIZE – byte width of pointer/handle values (PTR/STRING slots)
        /// stored on the eval stack.  Must be kept in sync across all three
        /// code-bases:
        ///   - cvm        : vm_runtime.h   #define VM_PTR_SIZE
        ///   - CSharpVM   : RuntimeVM.cs   VM_PTR_SIZE constant
        ///   - Frontend   : Define.cs      Global.VM_PTR_SIZE  (this field)
        ///
        /// Options: 2 (short), 4 (int), 8 (long).
        /// </summary>
        public const int VM_PTR_SIZE = 8;
    }
}
