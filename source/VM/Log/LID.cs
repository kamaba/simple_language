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
    }
}
