# PlatformNegTest - negative test case for platform.require value validation
# (PLATFORM_CAPABILITY_DESIGN.md 13.8 / test group S).
#
# This project INTENTIONALLY fails to compile: the sibling jsonc declares
# illegal platform.require values (linuxx / arm65 / sse5 / vulkn).
# Expected: 4x Error 20036 (with option lists + suggestions) during the
# MetaCore step ValidatePlatformConfig -> phase aborts -> IR/Export skipped
# -> no module.json is produced.

PlatformNegTest
{
    static fun()
    {
        SystemPrintln("unreachable: compile aborts in MetaCore (ValidatePlatformConfig)")
    }
}
