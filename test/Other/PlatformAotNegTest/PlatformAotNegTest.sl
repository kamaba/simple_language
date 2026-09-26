# PlatformAotNegTest - negative test case for export.aot.features vs
# platform.require.cpu consistency validation (design S11.4 / S16 J group):
#
# Conflict in PlatformAotNegTest.jsonc:
#   platform.require.cpu all = ["avx2"]  -> avx2 must be enabled
#   export.aot.features = "+sse42"       -> only sse42 enabled
#   -> Error 20037 ("export.aot.features and platform.require cpu all(avx2)
#      are contradictory...") and compile aborted in MetaCore
#      (ValidatePlatformConfig / ValidateAotTargetConsistency).
PlatformAotNegTest
{
    static fun()
    {
        SystemPrintln("unreachable: compile aborts in MetaCore (ValidateAotTargetConsistency)")
    }
}
