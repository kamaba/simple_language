namespace SimpleLanguage.Logging
{
    public enum LID
    {
        None = 0,
        Unknown = 99999,

        ShowMessageTrace = 10001,
        ShowMessageInfo = 10002,
        ShowMessageWarning = 10003,
        ShowMessageError = 10004,
        ShowMessageAssert = 10005,

        RuntimeIRParseError = 10010,
        NotFoundRuntimeIRFile = 10011,
        NotFoundRuntimeEntry = 10012,
        RuntimeArrayIndexOutOfRange = 10013,
        RuntimeVMNotFoundHandleEVMType2 = 10014,
        RuntimeVMNotFoundHandleEVMType = 10015,
        RuntimeVMStackIndexNotEnough = 10016,
        RuntimeVMNotFoundRuntimeClass = 10017,
        RuntimeVMNotFoundRuntimeMethod = 10018,
        RuntimeVMNotFoundCurrentValue = 10019,
        RuntimeVMNotShouldIsNull = 10020,
    }
}
