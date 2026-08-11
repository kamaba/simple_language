//****************************************************************************
//  System-level builtin method calls — must stay in sync with
//  <see cref="SimpleLanguage.ESystemMethodCall"/> (Front Define.cs).
//****************************************************************************

namespace SimpleLanguage.VM.Runtime
{
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
        SystemStringToByteArray,
        DataAllEqual,
        DataTypeEqual,
        DataNameAndTypeEqual,
        DataDataEqual,
        /// <summary>Build data/anonymous-data string representation.</summary>
        SystemBuildDataString,
        /// <summary>Convert to signed byte; must match <see cref="SimpleLanguage.ESystemMethodCall.SystemConvertSInt8"/> ordinal.</summary>
        SystemConvertSInt8,

        // Memory management (Memory.sl) – must stay in sync with Front Define.cs
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

        #region Math
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
        #endregion

        /// <summary>调用外部 DLL 注册的函数，第一个参数为函数名（字符串），后续为实际参数。</summary>
        SystemCallExternalFunction,

        // List<T> native container operations (List.sl)
        SystemListInit,
        SystemListGetValueThis,
        SystemListSetValueThis,
        SystemListGetCapacity,
        SystemListSetCapacity,
        SystemListRemoveValueThis,
        SystemListRemoveIndexValueThis,
        SystemListClearValueThis,

        // ---- Console I/O (input) – must stay in sync with Front Define.cs ----
        /// <summary>从标准输入读取一行（直到回车），返回 string。</summary>
        SystemInput,
    }
}
