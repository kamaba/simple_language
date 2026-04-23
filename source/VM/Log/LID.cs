namespace SimpleLanguage.Logging
{
    public enum LID
    {
        None = 0,
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
        /// <summary>VM 算术 (Add/等) 中 null 与数字混合 — Add.VMLog 专用</summary>
        RuntimeVMAddArithmeticNullLog = 10021,
        /// <summary>操作符运算中出现 null 与数字混合，模板: 在操作符[{0}]的运算中，出现了null的在[{1}]</summary>
        VMOperatorNotShouldHaveNull = 10022,

        RuntimeVMInstructPayLoadGetValueError = 10021,
        RuntimeVMRuntimeTypeIsNull = 10022,

        AutoLogManagerL190 = 13000,
        AutoLogManagerL194 = 13001,
        AutoClassObjectL95 = 13002,
        AutoClassObjectL100 = 13003,
        AutoClassObjectL109 = 13004,
        AutoCLRRRuntimeVML171 = 13005,
        AutoCLRRRuntimeVML176 = 13006,
        AutoCLRRRuntimeVML208 = 13007,
        AutoCLRRRuntimeVML226 = 13008,
        AutoCLRRRuntimeVML231 = 13009,
        AutoRuntimeAssemblyL41 = 13010,
        AutoRuntimeCLRCallL225 = 13011,
        AutoRuntimeObjectL344 = 13012,
        AutoRuntimeObjectL369 = 13013,
        AutoRuntimeObjectL392 = 13014,
        AutoRuntimeObjectL415 = 13015,
        AutoRuntimeObjectL438 = 13016,
        AutoRuntimeObjectL460 = 13017,
        AutoRuntimeObjectL483 = 13018,
        AutoRuntimeObjectL506 = 13019,
        AutoRuntimeObjectL529 = 13020,
        AutoRuntimeObjectL552 = 13021,
        AutoRuntimeObjectL575 = 13022,
        AutoRuntimeObjectL598 = 13023,
    }
}
