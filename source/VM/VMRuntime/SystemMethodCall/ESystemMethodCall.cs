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

        /// <summary>数组区间填充，参数: (this, startIndex, length, value)；must stay in sync with Front Define.cs。</summary>
        SystemArrayFillValue,
        /// <summary>数组扩缩容并拷贝已有元素，参数: (this, newCapacity)，返回新数组；must stay in sync with Front Define.cs。</summary>
        SystemArrayResize,
        /// <summary>数组区间右移一位并插入元素，参数: (this, index, length, value)；must stay in sync with Front Define.cs。</summary>
        SystemArrayInsertValue,
        /// <summary>数组区间左移一位并清空末位，参数: (this, index, length)；must stay in sync with Front Define.cs。</summary>
        SystemArrayRemoveAtValue,
        /// <summary>数组前 length 个元素拷贝到新数组，参数: (this, length)，返回新数组；must stay in sync with Front Define.cs。</summary>
        SystemArrayCopy,
        /// <summary>数组按值查找并移除首个匹配元素（左移补位），参数: (this, item, length)，返回被移除的索引（-1 未找到）；must stay in sync with Front Define.cs。</summary>
        SystemArrayRemoveValue,
    }
}
