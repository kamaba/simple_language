# PlatformOverrideNegTest - negative test case for platform.override key
# validation: illegal override keys -> Error 20036 (with suggestions) and
# compile aborted in MetaCore (ValidatePlatformConfig / ValidateOverrideKey).
#
# Illegal keys in PlatformOverrideNegTest.jsonc:
#   "cpu.avxxx"  -> prefix ok, unknown member (expects suggestion "cpu.avx2")
#   "cp.avx2"    -> unknown prefix (expects suggestion "cpu")
#   "avxxx"      -> bare single-segment unknown member
PlatformOverrideNegTest
{
    static fun()
    {
        SystemPrintln("unreachable: compile aborts in MetaCore (ValidatePlatformOverrideKeys)")
    }
}
