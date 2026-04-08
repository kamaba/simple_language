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
        SystemReadLine,
        SystemReadKey,
        SystemConvertInt8,
        SystemConvertSInt8,
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
        SystemArrayGetValueThis,
        SystemArraySetValueThis,
    }
}
