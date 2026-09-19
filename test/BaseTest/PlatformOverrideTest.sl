# PlatformOverrideTest - P1.8 runtime override test case (design §16 Y group).
#
# jsonc-channel fixture (ProjectTest.jsonc platform.override):
#   "memory": 256          -> SET    (mask a numeric probe field)
#   "cpu.avx2": true       -> ENABLE (force a capability to true)
#   "device.gpu": false    -> DISABLE (mark a capability as disabled)
#   "network.online": true -> ENABLE
#
# Verifies the SL-side three read forms (§8.5.6):
#   Environment.current  = effective values (after override)
#   Environment.probe    = raw probed values (never affected by override)
#   Environment.Override = isDisabled / source / getValue
#
# CLI / ENV channels and the full diagnostic output are verified by
# csimple_lang/scripts/test-platform-override.ps1 (exe direct run).
# Note: SystemPlatform* dispatch is CVM-only (same as EnvironmentTest 6b).

PlatformOverrideTest
{
    # 统一断言辅助：cond 为 true 打印 OK，否则打印 FAIL
    static check( string name, bool cond )
    {
        if cond
        {
            SystemPrintln( "[PlatformOverrideTest] " + name + " : OK" )
        }
        else
        {
            SystemPrintln( "[PlatformOverrideTest] " + name + " : FAIL" )
        }
    }

    static fun()
    {
        SystemPrintln( "========== PlatformOverrideTest (start) ==========" )

        Int64 rawMem = Environment.probe.totalMemMB
        bool rawAvx2 = Environment.probe.isaHas( Environment.Platform.isa.avx2 )

        # --- 1. SET: memory=256 (jsonc channel, §8.5.4) ---
        check( "SET source(memory) == jsonc", Environment.Override.source( "memory" ) == "jsonc" )
        check( "SET getValue(memory) == 256", Environment.Override.getValue( "memory" ) == "256" )
        check( "SET current.totalMemMB == 256", Environment.current.totalMemMB == 256 )
        check( "SET probe.totalMemMB unchanged", Environment.probe.totalMemMB == rawMem )
        check( "SET probe not masked (raw != 256)", Environment.probe.totalMemMB != 256 )

        # --- 2. ENABLE: cpu.avx2=true (jsonc channel, §8.5.2) ---
        check( "ENABLE source(cpu.avx2) == jsonc", Environment.Override.source( "cpu.avx2" ) == "jsonc" )
        check( "ENABLE isDisabled(cpu.avx2) == false", Environment.Override.isDisabled( "cpu.avx2" ) == false )
        check( "ENABLE current.isaHas(avx2) forced true", Environment.current.isaHas( Environment.Platform.isa.avx2 ) == true )
        check( "ENABLE probe.isaHas(avx2) stays raw", Environment.probe.isaHas( Environment.Platform.isa.avx2 ) == rawAvx2 )

        # --- 3. DISABLE: device.gpu=false (jsonc channel, §8.5.5) ---
        check( "DISABLE isDisabled(device.gpu) == true", Environment.Override.isDisabled( "device.gpu" ) == true )
        check( "DISABLE source(device.gpu) == jsonc", Environment.Override.source( "device.gpu" ) == "jsonc" )

        # --- 4. keys without any override entry ---
        check( "no-override source(os) is empty", Environment.Override.source( "os" ) == "" )
        check( "no-override isDisabled(os) == false", Environment.Override.isDisabled( "os" ) == false )
        check( "no-override getValue(os) is empty", Environment.Override.getValue( "os" ) == "" )

        SystemPrintln( "========== PlatformOverrideTest (end) ==========" )
    }
}
