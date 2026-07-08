using System.Diagnostics;

namespace SimpleLanguage.VM.Runtime
{
    internal static class BridgeSystemMethodCall
    {
        public static void ExecuteSystemCallCLRMethod(RuntimeVM vm, Instruction iri)
        {
            if (!vm.TryInvokeRegisteredBridgeByIndex(iri))
            {
                vm.TryInvokeLegacyBridgeSignature(iri, "CallCLRMethod");
            }
        }

        public static void ExecuteSystemCallNativeMethod(RuntimeVM vm, Instruction iri)
        {
            if (!vm.TryInvokeRegisteredBridgeByIndex(iri))
            {
                vm.TryInvokeLegacyBridgeSignature(iri, "CallNativeMethod");
            }
        }

        public static void ExecuteSystemCallJVMMethod(RuntimeVM vm, Instruction iri)
        {
            if (!vm.TryInvokeLegacyBridgeSignature(iri, "CallJVMMethod"))
            {
                Debug.Assert(false, "CallJVMMethod is not configured");
            }
        }
    }
}
