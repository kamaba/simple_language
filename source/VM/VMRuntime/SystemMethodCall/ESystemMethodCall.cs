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
    }
}
