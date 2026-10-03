# PlatformSevenTest - P3 device category SL access test (design
# PLATFORM_CAPABILITY_DESIGN.md §12.6; §13.6 device enum).
#
# Environment.current.device / Environment.probe.device plus the
# DeviceInfo detail object. Two assertion styles (same as
# PlatformSixTest):
#   check()   - host-independent facts: kind stays inside the enum
#               range (cpu=1..fpga=5, §13.6), the CPU entry is always
#               present (sys_info guarantees a cpu device row, P3.1b),
#               fake-kind lookups fail cleanly (has(99)==false,
#               count(99)==0), absent DeviceInfo entries return
#               ""/""/0 (Environment.sl §12.6 contract), the effective
#               and raw channels are each self-consistent (kind is a
#               present kind on its own channel), and fields without
#               a device override mirror across current/probe.
#   observe() - environment-dependent values (kind, per-kind counts,
#               gpu[0] detail when a GPU exists), printed raw.
# Note: SystemPlatform* dispatch is CVM-only (same as PlatformSixTest).

PlatformSevenTest
{
    # 统一断言辅助：cond 为 true 打印 OK，否则打印 FAIL
    static check( string name, bool cond )
    {
        if cond
        {
            SystemPrintln( "[PlatformSevenTest] " + name + " : OK" )
        }
        else
        {
            SystemPrintln( "[PlatformSevenTest] " + name + " : FAIL" )
        }
    }

    # 观测：打印原值（不断言），供人工 / exe 直跑脚本核对
    static observeS( string name, string val )
    {
        SystemPrintln( "[PlatformSevenTest] " + name + " : " + val )
    }

    static fun()
    {
        SystemPrintln( "========== PlatformSevenTest (start) ==========" )

        # --- device kinds (§12.6) ---
        SystemPrintln( "--- device (§12.6) ---" )
        cDev = Environment.current.device
        pDev = Environment.probe.device
        check( "device kind in enum 1..5", cDev.kind >= 1 && cDev.kind <= 5 )
        check( "device cpu always present", cDev.has( Environment.Platform.device.cpu ) == true )
        check( "device cpu count >= 1", cDev.count( Environment.Platform.device.cpu ) >= 1 )
        check( "device fake kind has(99) == false", cDev.has( 99 ) == false )
        check( "device fake kind count(99) == 0", cDev.count( 99 ) == 0 )
        check( "device kind is a present kind",
            cDev.has( cDev.kind ) == true && cDev.count( cDev.kind ) >= 1 )
        check( "device probe kind is a present kind (raw)",
            pDev.has( pDev.kind ) == true && pDev.count( pDev.kind ) >= 1 )
        check( "device current/probe mirror (cpu count)",
            pDev.count( Environment.Platform.device.cpu ) == cDev.count( Environment.Platform.device.cpu ) )

        # --- DeviceInfo absent-entry contract (§12.6) ---
        fakeInfo = cDev.info( 99 )
        check( "fake info vendor == empty", fakeInfo.vendor == "" )
        check( "fake info name == empty", fakeInfo.name == "" )
        check( "fake info memoryMB == 0", fakeInfo.memoryMB == 0 )
        fakeProbeInfo = pDev.info( 99 )
        check( "fake probe info vendor == empty", fakeProbeInfo.vendor == "" )

        # --- observations (environment-dependent) ---
        observeS( "device kind (1=cpu 2=gpu 3=npu 4=dsp 5=fpga)", cDev.kind.toString() )
        observeS( "device probe kind (raw)", pDev.kind.toString() )
        observeS( "device count cpu/gpu/npu",
            cDev.count( Environment.Platform.device.cpu ).toString() + "/" +
            cDev.count( Environment.Platform.device.gpu ).toString() + "/" +
            cDev.count( Environment.Platform.device.npu ).toString() )
        observeS( "device probe count cpu/gpu/npu (raw)",
            pDev.count( Environment.Platform.device.cpu ).toString() + "/" +
            pDev.count( Environment.Platform.device.gpu ).toString() + "/" +
            pDev.count( Environment.Platform.device.npu ).toString() )
        cpuInfo = cDev.info( Environment.Platform.device.cpu )
        observeS( "cpu[0] vendor/name", cpuInfo.vendor + "/" + cpuInfo.name )
        gpuCount = cDev.count( Environment.Platform.device.gpu )
        if gpuCount > 0
        {
            gpuInfo0 = cDev.info( Environment.Platform.device.gpu )
            gpuInfo1 = cDev.info( Environment.Platform.device.gpu, 0 )
            observeS( "gpu[0] vendor/name", gpuInfo0.vendor + "/" + gpuInfo0.name )
            observeS( "gpu[0] computeCapability", gpuInfo0.computeCapability.toString() )
            observeS( "gpu[0] memoryMB", gpuInfo0.memoryMB.toString() )
            check( "gpu info(dev) == info(dev,0)",
                gpuInfo0.vendor == gpuInfo1.vendor && gpuInfo0.name == gpuInfo1.name )
            pGpuInfo = pDev.info( Environment.Platform.device.gpu )
            check( "gpu current/probe mirror",
                pGpuInfo.vendor == gpuInfo0.vendor && pGpuInfo.name == gpuInfo0.name )
        }
        else
        {
            SystemPrintln( "[PlatformSevenTest] gpu[0] : (no gpu on this host, skipped)" )
        }

        SystemPrintln( "========== PlatformSevenTest (end) ==========" )
    }
}
