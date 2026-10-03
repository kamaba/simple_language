# EnvironmentTest - P1.7 new Environment API verification.
#
# Migration applied (design PLATFORM_CAPABILITY_DESIGN.md §13.12/§13.13):
#   Environment.getVariable/setVariable → Environment.env.getValue/setValue
#   Environment.currentDirectory()      → Environment.sys.currentDirectory()
#   Environment.tickCount()/nowMillis() → Environment.sys.tickCount()/nowMillis()
#   Environment.current()              → Environment.current   (no parens)
#   OS.window / ISA.sse2 / Device.cpu  → Environment.Platform.<enum>.<member>
#
# New in P1.7:
#   Environment.Platform.defs  lookup/contains/names (§13.3)
#   Environment.custom.getValue (jsonc require.custom)
#   Environment.legacy         (P1.6 compat shell)
#   link enum member: lan (renamed from reserved word `local`)

EnvironmentTest
{
    static fun()
    {
        SystemPrintln("========== EnvironmentTest (start) ==========")

        # 1. env.get / env.set
        SystemPrintln("--- Environment variable get/set (env) ---")
        Environment.env.setValue("SL_TEST_ENV", "hello_sl")
        val = Environment.env.getValue("SL_TEST_ENV")
        SystemPrintln("env.getValue(SL_TEST_ENV) = " + val)

        path = Environment.env.getValue("PATH")
        if (path != null && SystemStringLength(path) > 0)
        {
            SystemPrintln("env.getValue(PATH) has value, length = " + SystemConvertString(SystemStringLength(path)))
        }
        else
        {
            SystemPrintln("env.getValue(PATH) is empty")
        }

        notFound = Environment.env.getValue("SL_NO_SUCH_VAR_12345")
        SystemPrintln("env.getValue(nonexistent) = '" + notFound + "'")

        # 1b. legacy compat shell (§13.13)
        lv = Environment.legacy.getVariable("SL_TEST_ENV")
        SystemPrintln("legacy.getVariable(SL_TEST_ENV) = " + lv)

        # 2. sys.currentDirectory
        SystemPrintln("--- Current directory (sys) ---")
        cwd = Environment.sys.currentDirectory()
        SystemPrintln("sys.currentDirectory() = " + cwd)

        # 3. sys timing
        SystemPrintln("--- Timing (sys) ---")
        tick = Environment.sys.tickCount()
        SystemPrintln("sys.tickCount() = " + SystemConvertString(tick))

        tick64 = Environment.sys.tickCount64()
        SystemPrintln("sys.tickCount64() = " + tick64.toString())

        nowMs = Environment.sys.nowMillis()
        SystemPrintln("sys.nowMillis() = " + nowMs.toString())

        # 4. Platform environment classification (P1.7 static form)
        SystemPrintln("--- Platform environment (Environment.current) ---")
        SystemPrintln("os = " + Environment.current.os.toString() + " (Platform.os.window = " + Environment.Platform.os.window.value.toString() + ")")
        SystemPrintln("osName = " + Environment.current.osName)
        SystemPrintln("osFamily = " + Environment.current.osFamily.toString() + " (1=window 2=unix)")
        SystemPrintln("osVersionNumber = " + Environment.current.osVersionNumber.toString())
        SystemPrintln("form = " + Environment.current.form.toString() + " (1=desktop)")
        SystemPrintln("arch = " + Environment.current.arch.toString() + " (2=x64)")
        SystemPrintln("archBits = " + Environment.current.archBits.toString())
        SystemPrintln("endian = " + Environment.current.endian.toString() + " (1=little)")
        SystemPrintln("triple = " + Environment.current.triple)
        SystemPrintln("cpuCount = " + Environment.current.cpuCount.toString())
        SystemPrintln("cpuVendor = " + Environment.current.cpuVendor)
        SystemPrintln("cpuModel = " + Environment.current.cpuModel)

        # 5. ISA / device bit sets (§13: bare enum member as Int32 arg)
        hasSse2 = Environment.current.isaHas( Environment.Platform.isa.sse2 )
        SystemPrintln("isaHas(Platform.isa.sse2) = " + hasSse2.toString())
        hasCpu = Environment.current.deviceHas( Environment.Platform.device.cpu )
        SystemPrintln("deviceHas(Platform.device.cpu) = " + hasCpu.toString())
        SystemPrintln("deviceCount(Platform.device.cpu) = " + Environment.current.deviceCount( Environment.Platform.device.cpu ).toString())

        # 6. Runtime / build / memory
        SystemPrintln("runtime = " + Environment.current.runtime.toString() + " (0=slvm)")
        SystemPrintln("runtimeVersion = " + Environment.current.runtimeVersion)
        SystemPrintln("build = " + Environment.current.build.toString() + " (0=debug 1=release)")
        SystemPrintln("totalMemMB = " + Environment.current.totalMemMB.toString())
        SystemPrintln("availMemMB = " + Environment.current.availMemMB.toString())

        # 6b. probe / Override (§8.5.6: probe = raw, current = effective)
        SystemPrintln("--- Platform probe / Override ---")
        SystemPrintln("probe.cpuCount = " + Environment.probe.cpuCount.toString() + " (raw)")
        SystemPrintln("probe.totalMemMB = " + Environment.probe.totalMemMB.toString() + " (raw)")
        SystemPrintln("probe.isaHas(Platform.isa.avx2) = " + Environment.probe.isaHas( Environment.Platform.isa.avx2 ).toString())
        SystemPrintln("probe.osVersionNumber = " + Environment.probe.osVersionNumber.toString())
        SystemPrintln("current.isaHas(Platform.isa.avx2) = " + Environment.current.isaHas( Environment.Platform.isa.avx2 ).toString() + " (effective)")
        SystemPrintln("Override.isDisabled(cpu.avx2) = " + Environment.Override.isDisabled( "cpu.avx2" ).toString())
        SystemPrintln("Override.source(cpu.avx2) = '" + Environment.Override.source( "cpu.avx2" ) + "'")
        SystemPrintln("Override.isDisabled(device.gpu) = " + Environment.Override.isDisabled( "device.gpu" ).toString())
        SystemPrintln("Override.source(device.gpu) = '" + Environment.Override.source( "device.gpu" ) + "'")
        SystemPrintln("Override.getValue(memory) = '" + Environment.Override.getValue( "memory" ) + "'")

        # 7. OS enum compare branch (definition vs runtime, §13.2)
        if (Environment.current.os == Environment.Platform.os.window)
        {
            SystemPrintln("os check: running on Windows")
        }
        else
        {
            SystemPrintln("os check: not Windows, os = " + Environment.current.os.toString())
        }

        # 8. Definition constants (compile-time, §13.2)
        SystemPrintln("--- Platform definition constants ---")
        SystemPrintln("Platform.os.linux.value = " + Environment.Platform.os.linux.value.toString())
        SystemPrintln("Platform.link.lan.value = " + Environment.Platform.link.lan.value.toString())
        SystemPrintln("Platform.runtime.slvm.value = " + Environment.Platform.runtime.slvm.value.toString())
        SystemPrintln("Platform.endian.little.value = " + Environment.Platform.endian.little.value.toString())

        # 9. defs registry (§13.3): lookup / contains / names
        SystemPrintln("--- Platform.defs registry ---")
        it = Environment.Platform.defs.lookup( Environment.Platform.DefKind.OS, "windows" )
        if (it != null)
        {
            SystemPrintln("defs.lookup(OS, \"windows\") = " + it.value.toString() + " name=" + it.name)
        }
        else
        {
            SystemPrintln("defs.lookup(OS, \"windows\") = null  FAIL")
        }

        noItem = Environment.Platform.defs.lookup( Environment.Platform.DefKind.OS, "no_such_os" )
        if (noItem == null)
        {
            SystemPrintln("defs.lookup(OS, unknown) = null  OK")
        }
        else
        {
            SystemPrintln("defs.lookup(OS, unknown) != null  FAIL")
        }

        SystemPrintln("defs.contains(OS, \"linux\") = " + Environment.Platform.defs.contains( Environment.Platform.DefKind.OS, "linux" ).toString())
        SystemPrintln("defs.contains(OS, \"win\") alias = " + Environment.Platform.defs.contains( Environment.Platform.DefKind.OS, "win" ).toString())
        SystemPrintln("defs.contains(Link, \"local\") alias = " + Environment.Platform.defs.contains( Environment.Platform.DefKind.Link, "local" ).toString())
        SystemPrintln("defs.contains(OS, unknown) = " + Environment.Platform.defs.contains( Environment.Platform.DefKind.OS, "no_such_os" ).toString())

        osNames = Environment.Platform.defs.names( Environment.Platform.DefKind.OS )
        SystemPrintln("defs.names(OS).length = " + osNames.length.toString())
        if (osNames.length > 0)
        {
            SystemPrintln("defs.names(OS)[0] = " + osNames[0])
            SystemPrintln("defs.names(OS)[1] = " + osNames[1])
        }

        # 10. custom (jsonc require.custom values; absent key → "")
        SystemPrintln("--- Environment.custom ---")
        cv = Environment.custom.getValue("SL_NO_SUCH_CUSTOM_KEY")
        SystemPrintln("custom.getValue(nonexistent) = '" + cv + "'")

        SystemPrintln("========== EnvironmentTest (end) ==========")
    }
}
