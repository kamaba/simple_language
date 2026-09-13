# =========================================================================
# Environment - platform environment API (P1.7 namespace form).
#
# Design doc PLATFORM_CAPABILITY_DESIGN.md §13.2:
#     namespace Environment
#     ├── namespace Platform   (5 part files: definition enums + defs)
#     ├── class current        (runtime environment values, CVM probed)
#     ├── class probe          (raw probed values, ignores overrides, §8.5.6)
#     ├── class Override       (override query: isDisabled/source/getValue)
#     ├── class env            (process environment variables)
#     ├── class custom         (jsonc require.custom)
#     ├── class sys            (directory / newline / timing)
#     └── class legacy         (P1.6 compat shell: getVariable / setVariable)
#
# ── Definition vs runtime (§13.2) ──
#   Environment.Platform.os.linux   definition constant (compile time)
#   Environment.current.os          current runtime value (CVM probed)
#   Comparison is the equality of the two:
#     if Environment.current.os == Environment.Platform.os.window { ... }
#     if Environment.current.isaHas( Environment.Platform.isa.avx2 ) { ... }
#
# ── current vs probe (§8.5.6) ──
#   current = effective values (after --disable/--enable/--set overrides);
#   probe   = raw probed values (never affected by overrides). Judge and
#   branch on current; use probe only for diagnostics / reporting:
#     if Environment.current.isaHas( Environment.Platform.isa.avx2 ) { ... }
#     string src = Environment.Override.source( "cpu.avx2" )   # "" / "cli" ...
#     bool off = Environment.Override.isDisabled( "device.gpu" )
#   Class name uses capital Override — "override" is the SL method
#   override keyword (lexer is case-sensitive, like get/set naming rule).
#   source/getValue return "" when the key has no override (SL has no
#   string? syntax; same convention as custom.getValue).
#
# ── Migration from P1.6 (§13.12 / §13.13) ──
#   Environment.current().os    → Environment.current.os          (no parens)
#   OS.window                   → Environment.Platform.os.window
#   ISA.avx2                    → Environment.Platform.isa.avx2
#   Environment.getVariable(n)   → Environment.env.getValue(n)
#   Environment.setVariable(n,v)→ Environment.env.setValue(n, v)
#   Environment.currentDirectory()      → Environment.sys.currentDirectory()
#   Environment.tickCount() / 64 / now  → Environment.sys.tickCount() / ...
#
# Part files (namespace Environment { namespace Platform { ... } }):
#   Environment/Platform_OS.sl     os / osVersion / form
#   Environment/Platform_Cpu.sl    arch / cpu / isa
#   Environment/Platform_Dev.sl    device / ai / render / shaderModel
#   Environment/Platform_Misc.sl   network / link / embedded / rtos /
#                                  runtime / build / endian
#   Environment/Platform_Defs.sl   DefItem / DefKind / defs registry
# =========================================================================

namespace Environment
{
    # -------------------------------------------------------------------------
    # OSVersionNumber - numeric OS version (§13.5: numeric layer, for >=).
    # -------------------------------------------------------------------------

    public class OSVersionNumber extends Object
    {
        public get Int32 major()
        {
            ret SystemPlatformEnvGetInt( "osVerMajor" )
        }

        public get Int32 minor()
        {
            ret SystemPlatformEnvGetInt( "osVerMinor" )
        }

        public get Int32 build()
        {
            ret SystemPlatformEnvGetInt( "osVerBuild" )
        }

        public get Int32 patch()
        {
            ret SystemPlatformEnvGetInt( "osVerPatch" )
        }

        override string toString()
        {
            ret this.major.toString() + "." + this.minor.toString() + "." + this.build.toString() + "." + this.patch.toString()
        }
    }

    # -------------------------------------------------------------------------
    # ProbeOSVersionNumber - numeric OS version, raw probe (§8.5.6).
    # Mirrors OSVersionNumber but reads the probe channel, so it ignores
    # the "osversion" SET override. Used by probe.osVersionNumber.
    # -------------------------------------------------------------------------

    public class ProbeOSVersionNumber extends Object
    {
        public get Int32 major()
        {
            ret SystemPlatformEnvProbeGetInt( "osVerMajor" )
        }

        public get Int32 minor()
        {
            ret SystemPlatformEnvProbeGetInt( "osVerMinor" )
        }

        public get Int32 build()
        {
            ret SystemPlatformEnvProbeGetInt( "osVerBuild" )
        }

        public get Int32 patch()
        {
            ret SystemPlatformEnvProbeGetInt( "osVerPatch" )
        }

        override string toString()
        {
            ret this.major.toString() + "." + this.minor.toString() + "." + this.build.toString() + "." + this.patch.toString()
        }
    }

    # -------------------------------------------------------------------------
    # Cpu / Ai / Render / Network / Script / Embedded - six new category
    # detail objects (PLATFORM_CAPABILITY_DESIGN §12.12-12.17, P2.7b).
    # Bound to Environment.current (effective view) or Environment.probe
    # (raw values). Members are instance getters — reach them through
    #     Environment.current.cpu.physicalCores
    #     Environment.current.script.has( "python", "3.8" )
    # Enum-typed members return Int32 — compare against the Platform
    # member (e.g. Environment.Platform.ai.cann).
    # -------------------------------------------------------------------------

    # ── cpu topology (§12.12; current channel) ──

    public class Cpu extends Object
    {
        # 0=unknown 1=single 2=smp 3=bigLittle 4=hybrid 5=numa.
        public get Int32 topology()
        {
            ret SystemPlatformEnvGetInt( "cpuTopology" )
        }

        public get Int32 physicalCores()
        {
            ret SystemPlatformEnvGetInt( "cpuPhysicalCores" )
        }

        public get Int32 logicalCores()
        {
            ret SystemPlatformEnvGetInt( "cpuLogicalCores" )
        }

        public get bool hasHyperThreading()
        {
            ret SystemPlatformEnvGetInt( "cpuHasHt" ) == 1
        }

        public get Int32 numaNodes()
        {
            ret SystemPlatformEnvGetInt( "cpuNumaNodes" )
        }

        public get Int32 freqMHz()
        {
            ret SystemPlatformEnvGetInt( "cpuFreqMHz" )
        }

        public get Int32 maxFreqMHz()
        {
            ret SystemPlatformEnvGetInt( "cpuMaxFreqMHz" )
        }

        public get Int32 cacheL1KB()
        {
            ret SystemPlatformEnvGetInt( "cpuCacheL1KB" )
        }

        public get Int32 cacheL2KB()
        {
            ret SystemPlatformEnvGetInt( "cpuCacheL2KB" )
        }

        public get Int32 cacheL3KB()
        {
            ret SystemPlatformEnvGetInt( "cpuCacheL3KB" )
        }

        public get Int32 perfCores()
        {
            ret SystemPlatformEnvGetInt( "cpuPerfCores" )
        }

        public get Int32 efficiencyCores()
        {
            ret SystemPlatformEnvGetInt( "cpuEfficiencyCores" )
        }
    }

    # ── cpu topology (§12.12; probe channel — raw values) ──

    public class ProbeCpu extends Object
    {
        public get Int32 topology()
        {
            ret SystemPlatformEnvProbeGetInt( "cpuTopology" )
        }

        public get Int32 physicalCores()
        {
            ret SystemPlatformEnvProbeGetInt( "cpuPhysicalCores" )
        }

        public get Int32 logicalCores()
        {
            ret SystemPlatformEnvProbeGetInt( "cpuLogicalCores" )
        }

        public get bool hasHyperThreading()
        {
            ret SystemPlatformEnvProbeGetInt( "cpuHasHt" ) == 1
        }

        public get Int32 numaNodes()
        {
            ret SystemPlatformEnvProbeGetInt( "cpuNumaNodes" )
        }

        public get Int32 freqMHz()
        {
            ret SystemPlatformEnvProbeGetInt( "cpuFreqMHz" )
        }

        public get Int32 maxFreqMHz()
        {
            ret SystemPlatformEnvProbeGetInt( "cpuMaxFreqMHz" )
        }

        public get Int32 cacheL1KB()
        {
            ret SystemPlatformEnvProbeGetInt( "cpuCacheL1KB" )
        }

        public get Int32 cacheL2KB()
        {
            ret SystemPlatformEnvProbeGetInt( "cpuCacheL2KB" )
        }

        public get Int32 cacheL3KB()
        {
            ret SystemPlatformEnvProbeGetInt( "cpuCacheL3KB" )
        }

        public get Int32 perfCores()
        {
            ret SystemPlatformEnvProbeGetInt( "cpuPerfCores" )
        }

        public get Int32 efficiencyCores()
        {
            ret SystemPlatformEnvProbeGetInt( "cpuEfficiencyCores" )
        }
    }

    # ── ai stacks (§12.13; current channel) ──

    public class Ai extends Object
    {
        # Whether the AI stack member is available (effective view: honours
        # --disable / --enable ai.<name> overrides).
        public bool has( Int32 stack )
        {
            ret SystemPlatformEnvAiHas( stack )
        }

        # Detected version of the AI stack ("" when unknown or absent).
        public string version( Int32 stack )
        {
            ret SystemPlatformAiVersion( stack )
        }

        # Whether any AI stack is available.
        public bool any()
        {
            ret SystemStringLength( SystemPlatformEnvGetStr( "aiPreferred" ) ) > 0
        }

        # Best available AI stack name ("" when none, §12.13).
        public get string preferred()
        {
            ret SystemPlatformEnvGetStr( "aiPreferred" )
        }
    }

    # ── ai stacks (§12.13; probe channel — raw values) ──

    public class ProbeAi extends Object
    {
        public bool has( Int32 stack )
        {
            ret SystemPlatformEnvProbeAiHas( stack )
        }

        public string version( Int32 stack )
        {
            ret SystemPlatformAiVersion( stack )
        }

        public bool any()
        {
            ret SystemStringLength( SystemPlatformEnvProbeGetStr( "aiPreferred" ) ) > 0
        }

        public get string preferred()
        {
            ret SystemPlatformEnvProbeGetStr( "aiPreferred" )
        }
    }

    # ── render (§12.14; current channel) ──

    public class Render extends Object
    {
        # Preferred render backend enum value (0 = none; §12.21 M: hosts
        # without a GPU report Software instead of failing).
        public get Int32 api()
        {
            ret SystemPlatformEnvGetInt( "renderApi" )
        }

        # All available render backend names, in value order.
        public Array<string> apiList()
        {
            Array<string> r = null
            r = SystemPlatformRenderApiList()
            ret r
        }

        # Shader model code: 0=unknown 50=sm_5_0 60/65/66/67 (§12.14).
        public get Int32 shaderModel()
        {
            ret SystemPlatformEnvGetInt( "renderShaderModel" )
        }

        public get bool hasRayTracing()
        {
            ret ( ( SystemPlatformEnvGetInt( "renderFlags" ) >> 0 ) & 1 ) == 1
        }

        public get bool hasMeshShader()
        {
            ret ( ( SystemPlatformEnvGetInt( "renderFlags" ) >> 1 ) & 1 ) == 1
        }

        public get bool hasCompute()
        {
            ret ( ( SystemPlatformEnvGetInt( "renderFlags" ) >> 2 ) & 1 ) == 1
        }

        public get bool hasHdr()
        {
            ret ( ( SystemPlatformEnvGetInt( "renderFlags" ) >> 3 ) & 1 ) == 1
        }

        public get Int32 maxTextureSize()
        {
            ret SystemPlatformEnvGetInt( "renderMaxTextureSize" )
        }

        public get Int32 displays()
        {
            ret SystemPlatformEnvGetInt( "renderDisplays" )
        }

        public get Int32 primaryWidth()
        {
            ret SystemPlatformEnvGetInt( "renderPrimaryWidth" )
        }

        public get Int32 primaryHeight()
        {
            ret SystemPlatformEnvGetInt( "renderPrimaryHeight" )
        }

        public get Int32 refreshHz()
        {
            ret SystemPlatformEnvGetInt( "renderRefreshHz" )
        }
    }

    # ── render (§12.14; probe channel — raw values) ──

    public class ProbeRender extends Object
    {
        public get Int32 api()
        {
            ret SystemPlatformEnvProbeGetInt( "renderApi" )
        }

        public Array<string> apiList()
        {
            Array<string> r = null
            r = SystemPlatformRenderApiList()
            ret r
        }

        public get Int32 shaderModel()
        {
            ret SystemPlatformEnvProbeGetInt( "renderShaderModel" )
        }

        public get bool hasRayTracing()
        {
            ret ( ( SystemPlatformEnvProbeGetInt( "renderFlags" ) >> 0 ) & 1 ) == 1
        }

        public get bool hasMeshShader()
        {
            ret ( ( SystemPlatformEnvProbeGetInt( "renderFlags" ) >> 1 ) & 1 ) == 1
        }

        public get bool hasCompute()
        {
            ret ( ( SystemPlatformEnvProbeGetInt( "renderFlags" ) >> 2 ) & 1 ) == 1
        }

        public get bool hasHdr()
        {
            ret ( ( SystemPlatformEnvProbeGetInt( "renderFlags" ) >> 3 ) & 1 ) == 1
        }

        public get Int32 maxTextureSize()
        {
            ret SystemPlatformEnvProbeGetInt( "renderMaxTextureSize" )
        }

        public get Int32 displays()
        {
            ret SystemPlatformEnvProbeGetInt( "renderDisplays" )
        }

        public get Int32 primaryWidth()
        {
            ret SystemPlatformEnvProbeGetInt( "renderPrimaryWidth" )
        }

        public get Int32 primaryHeight()
        {
            ret SystemPlatformEnvProbeGetInt( "renderPrimaryHeight" )
        }

        public get Int32 refreshHz()
        {
            ret SystemPlatformEnvProbeGetInt( "renderRefreshHz" )
        }
    }

    # ── network (§12.15; current channel) ──

    public class Network extends Object
    {
        # 0=None 1=Loopback 2=Ethernet 3=WiFi 4=Cellular 5=Unknown.
        # (§12.15 names this "type"; renamed to netType because Object.type
        # is final and cannot be overridden.)
        public get Int32 netType()
        {
            ret SystemPlatformEnvGetInt( "netType" )
        }

        # 0=Down 1=Local 2=Online.
        public get Int32 linkState()
        {
            ret SystemPlatformEnvGetInt( "netLink" )
        }

        # linkState == Online (§12.21 N: L1/L2 must not block offline use).
        public get bool isOnline()
        {
            ret SystemPlatformEnvGetInt( "netLink" ) == 2
        }

        public get bool isLocalOnly()
        {
            ret SystemPlatformEnvGetInt( "netLink" ) == 1
        }

        public get Int32 interfaces()
        {
            ret SystemPlatformEnvGetInt( "netInterfaces" )
        }

        public get bool hasProxy()
        {
            ret SystemPlatformEnvGetInt( "netHasProxy" ) == 1
        }

        public get string proxyUrl()
        {
            ret SystemPlatformEnvGetStr( "netProxyUrl" )
        }
    }

    # ── network (§12.15; probe channel — raw values) ──

    public class ProbeNetwork extends Object
    {
        # (§12.15 "type" renamed: Object.type is final.)
        public get Int32 netType()
        {
            ret SystemPlatformEnvProbeGetInt( "netType" )
        }

        public get Int32 linkState()
        {
            ret SystemPlatformEnvProbeGetInt( "netLink" )
        }

        public get bool isOnline()
        {
            ret SystemPlatformEnvProbeGetInt( "netLink" ) == 2
        }

        public get bool isLocalOnly()
        {
            ret SystemPlatformEnvProbeGetInt( "netLink" ) == 1
        }

        public get Int32 interfaces()
        {
            ret SystemPlatformEnvProbeGetInt( "netInterfaces" )
        }

        public get bool hasProxy()
        {
            ret SystemPlatformEnvProbeGetInt( "netHasProxy" ) == 1
        }

        public get string proxyUrl()
        {
            ret SystemPlatformEnvProbeGetStr( "netProxyUrl" )
        }
    }

    # ── script runtimes (§12.16; current channel) ──

    public class Script extends Object
    {
        # Whether the script runtime is present (effective view; name is
        # case-insensitive, e.g. "python").
        public bool has( string name )
        {
            ret SystemPlatformEnvScriptHas( name, "" )
        }

        # Whether the runtime is present AND version >= minVersion
        # (empty minVersion = presence only; unknown version never
        # rejects the check, §9.2).
        public bool has( string name, string minVersion )
        {
            ret SystemPlatformEnvScriptHas( name, minVersion )
        }

        # Detected version ("" when absent).
        public string version( string name )
        {
            ret SystemPlatformScriptVersion( name )
        }

        # Detected install path ("" when absent).
        public string path( string name )
        {
            ret SystemPlatformScriptPath( name )
        }

        # Names of all detected script runtimes, in probe table order.
        public Array<string> available()
        {
            Array<string> r = null
            r = SystemPlatformScriptAvailable()
            ret r
        }
    }

    # ── script runtimes (§12.16; probe channel — raw values) ──

    public class ProbeScript extends Object
    {
        public bool has( string name )
        {
            ret SystemPlatformEnvProbeScriptHas( name, "" )
        }

        public bool has( string name, string minVersion )
        {
            ret SystemPlatformEnvProbeScriptHas( name, minVersion )
        }

        public string version( string name )
        {
            ret SystemPlatformScriptVersion( name )
        }

        public string path( string name )
        {
            ret SystemPlatformScriptPath( name )
        }

        public Array<string> available()
        {
            Array<string> r = null
            r = SystemPlatformScriptAvailable()
            ret r
        }
    }

    # ── embedded (§12.17; current channel) ──

    public class Embedded extends Object
    {
        # MCU family enum value (0 = not embedded, general platform).
        public get Int32 family()
        {
            ret SystemPlatformEnvGetInt( "mcuFamily" )
        }

        public get string chip()
        {
            ret SystemPlatformEnvGetStr( "embChip" )
        }

        public get string vendor()
        {
            ret SystemPlatformEnvGetStr( "embVendor" )
        }

        public get string coreName()
        {
            ret SystemPlatformEnvGetStr( "embCoreName" )
        }

        public get bool bareMetal()
        {
            ret SystemPlatformEnvGetInt( "embBareMetal" ) == 1
        }

        # RTOS kind enum value (0 = none, §12.17).
        public get Int32 rtos()
        {
            ret SystemPlatformEnvGetInt( "rtosKind" )
        }

        public get string rtosVersion()
        {
            ret SystemPlatformEnvGetStr( "rtosVersion" )
        }

        # Flash / RAM sizes; 0 = unknown (general platforms report 0).
        public get Int32 flashKB()
        {
            ret SystemPlatformEnvGetInt( "embFlashKB" )
        }

        public get Int32 ramKB()
        {
            ret SystemPlatformEnvGetInt( "embRamKB" )
        }

        public get Int32 stackFreeKB()
        {
            ret SystemPlatformEnvGetInt( "embStackFreeKB" )
        }

        public get Int32 heapFreeKB()
        {
            ret SystemPlatformEnvGetInt( "embHeapFreeKB" )
        }

        public get bool hasFpu()
        {
            ret ( ( SystemPlatformEnvGetInt( "embFlags" ) >> 0 ) & 1 ) == 1
        }

        public get bool hasMmu()
        {
            ret ( ( SystemPlatformEnvGetInt( "embFlags" ) >> 1 ) & 1 ) == 1
        }

        public get bool hasMpu()
        {
            ret ( ( SystemPlatformEnvGetInt( "embFlags" ) >> 2 ) & 1 ) == 1
        }

        public get bool hasDsp()
        {
            ret ( ( SystemPlatformEnvGetInt( "embFlags" ) >> 3 ) & 1 ) == 1
        }

        public get Int32 cpuFreqMHz()
        {
            ret SystemPlatformEnvGetInt( "embCpuFreqMHz" )
        }

        public get string toolchain()
        {
            ret SystemPlatformEnvGetStr( "embToolchain" )
        }

        # Names of all embedded peripherals, in probe table order
        # (empty on general-purpose platforms).
        public Array<string> peripherals()
        {
            Array<string> r = null
            r = SystemPlatformEmbPeripherals()
            ret r
        }

        # Whether the peripheral (case-insensitive free text, e.g. "wifi")
        # is present in the effective view.
        public bool has( string peripheral )
        {
            ret SystemPlatformEnvEmbHas( peripheral )
        }

        # Resource-constrained judgement (§12.17): flash < 512KB or
        # ram < 256KB or no MMU or bare metal. On general-purpose platforms
        # flash / ram report 0 = unknown, which counts as constrained
        # (unknown resources are treated as limited, §12.21 P).
        public bool isConstrained()
        {
            ret this.flashKB < 512 || this.ramKB < 256 || this.hasMmu == false || this.bareMetal == true
        }
    }

    # ── embedded (§12.17; probe channel — raw values) ──

    public class ProbeEmbedded extends Object
    {
        public get Int32 family()
        {
            ret SystemPlatformEnvProbeGetInt( "mcuFamily" )
        }

        public get string chip()
        {
            ret SystemPlatformEnvProbeGetStr( "embChip" )
        }

        public get string vendor()
        {
            ret SystemPlatformEnvProbeGetStr( "embVendor" )
        }

        public get string coreName()
        {
            ret SystemPlatformEnvProbeGetStr( "embCoreName" )
        }

        public get bool bareMetal()
        {
            ret SystemPlatformEnvProbeGetInt( "embBareMetal" ) == 1
        }

        public get Int32 rtos()
        {
            ret SystemPlatformEnvProbeGetInt( "rtosKind" )
        }

        public get string rtosVersion()
        {
            ret SystemPlatformEnvProbeGetStr( "rtosVersion" )
        }

        public get Int32 flashKB()
        {
            ret SystemPlatformEnvProbeGetInt( "embFlashKB" )
        }

        public get Int32 ramKB()
        {
            ret SystemPlatformEnvProbeGetInt( "embRamKB" )
        }

        public get Int32 stackFreeKB()
        {
            ret SystemPlatformEnvProbeGetInt( "embStackFreeKB" )
        }

        public get Int32 heapFreeKB()
        {
            ret SystemPlatformEnvProbeGetInt( "embHeapFreeKB" )
        }

        public get bool hasFpu()
        {
            ret ( ( SystemPlatformEnvProbeGetInt( "embFlags" ) >> 0 ) & 1 ) == 1
        }

        public get bool hasMmu()
        {
            ret ( ( SystemPlatformEnvProbeGetInt( "embFlags" ) >> 1 ) & 1 ) == 1
        }

        public get bool hasMpu()
        {
            ret ( ( SystemPlatformEnvProbeGetInt( "embFlags" ) >> 2 ) & 1 ) == 1
        }

        public get bool hasDsp()
        {
            ret ( ( SystemPlatformEnvProbeGetInt( "embFlags" ) >> 3 ) & 1 ) == 1
        }

        public get Int32 cpuFreqMHz()
        {
            ret SystemPlatformEnvProbeGetInt( "embCpuFreqMHz" )
        }

        public get string toolchain()
        {
            ret SystemPlatformEnvProbeGetStr( "embToolchain" )
        }

        public Array<string> peripherals()
        {
            Array<string> r = null
            r = SystemPlatformEmbPeripherals()
            ret r
        }

        public bool has( string peripheral )
        {
            ret SystemPlatformEnvProbeEmbHas( peripheral )
        }

        public bool isConstrained()
        {
            ret this.flashKB < 512 || this.ramKB < 256 || this.hasMmu == false || this.bareMetal == true
        }
    }

    # ── device details (§12.6; P3) ──

    public class DeviceInfo extends Object
    {
        Int32 _dev = 0
        Int32 _index = 0

        _init_( Int32 dev, Int32 index )
        {
            this._dev = dev
            this._index = index
        }

        # Vendor name ("nvidia"/"amd"/"intel"/"arm"/"qualcomm"/"imagination";
        # "unknown" when unidentified; "" when the device entry is absent).
        public get string vendor()
        {
            ret SystemPlatformEnvDeviceInfoStr( this._dev, this._index, "vendor" )
        }

        # Device model name ("" when the entry is absent).
        public get string name()
        {
            ret SystemPlatformEnvDeviceInfoStr( this._dev, this._index, "name" )
        }

        # GPU shader-model-like capability (0 = unknown / not applicable).
        public get Int32 computeCapability()
        {
            ret SystemConvertInt32( SystemPlatformEnvDeviceInfoInt( this._dev, this._index, "computeCapability" ) )
        }

        # Dedicated memory in MB (0 = unknown).
        public get Int64 memoryMB()
        {
            ret SystemPlatformEnvDeviceInfoInt( this._dev, this._index, "memoryMB" )
        }

        override string toString()
        {
            ret this.vendor + " " + this.name
        }
    }

    # ── device kinds (§12.6; current channel) ──

    public class Device extends Object
    {
        # Main device kind: the highest priority kind present
        # (gpu > npu > dsp > fpga > cpu). Compare against
        # Environment.Platform.device members.
        public get Int32 kind()
        {
            ret SystemPlatformEnvGetInt( "deviceKind" )
        }

        # Whether the device kind is present (effective view: honours
        # --disable / --enable device.<name> overrides).
        public bool has( Int32 dev )
        {
            ret SystemPlatformEnvDeviceHas( dev )
        }

        # Count of devices of the kind (effective view).
        public Int32 count( Int32 dev )
        {
            ret SystemPlatformEnvDeviceCount( dev )
        }

        # Info of the first device of the kind: {vendor, name,
        # computeCapability, memoryMB} (§12.6).
        public DeviceInfo info( Int32 dev )
        {
            ret DeviceInfo( dev, 0 )
        }

        # Info of the n-th device of the kind (multi-GPU; index from 0).
        public DeviceInfo info( Int32 dev, Int32 index )
        {
            ret DeviceInfo( dev, index )
        }
    }

    # ── device details (§12.6; probe channel — raw values) ──

    public class ProbeDeviceInfo extends Object
    {
        Int32 _dev = 0
        Int32 _index = 0

        _init_( Int32 dev, Int32 index )
        {
            this._dev = dev
            this._index = index
        }

        public get string vendor()
        {
            ret SystemPlatformEnvProbeDeviceInfoStr( this._dev, this._index, "vendor" )
        }

        public get string name()
        {
            ret SystemPlatformEnvProbeDeviceInfoStr( this._dev, this._index, "name" )
        }

        public get Int32 computeCapability()
        {
            ret SystemConvertInt32( SystemPlatformEnvProbeDeviceInfoInt( this._dev, this._index, "computeCapability" ) )
        }

        public get Int64 memoryMB()
        {
            ret SystemPlatformEnvProbeDeviceInfoInt( this._dev, this._index, "memoryMB" )
        }

        override string toString()
        {
            ret this.vendor + " " + this.name
        }
    }

    # ── device kinds (§12.6; probe channel — raw values) ──

    public class ProbeDevice extends Object
    {
        public get Int32 kind()
        {
            ret SystemPlatformEnvProbeGetInt( "deviceKind" )
        }

        public bool has( Int32 dev )
        {
            ret SystemPlatformEnvProbeDeviceHas( dev )
        }

        public Int32 count( Int32 dev )
        {
            ret SystemPlatformEnvProbeDeviceCount( dev )
        }

        public ProbeDeviceInfo info( Int32 dev )
        {
            ret ProbeDeviceInfo( dev, 0 )
        }

        public ProbeDeviceInfo info( Int32 dev, Int32 index )
        {
            ret ProbeDeviceInfo( dev, index )
        }
    }
    #
    # Static getters forward to SystemPlatformEnv* system methods; the CVM
    # side probes + classifies once into a lazy singleton (sl_environment),
    # so each access is an O(1) lookup. Old modules without a platform
    # section work too.
    #
    # Enum-typed values return Int32 — compare against the Platform member:
    #     if Environment.current.os == Environment.Platform.os.window { ... }
    # isa / device are bit sets — query with isaHas / deviceHas / deviceCount:
    #     if Environment.current.isaHas( Environment.Platform.isa.avx2 ) { ... }
    # -------------------------------------------------------------------------

    public class current extends Object
    {
        # ── os ──
        public static get Int32 os()
        {
            ret SystemPlatformEnvGetInt( "os" )
        }

        public static get string osName()
        {
            ret SystemPlatformEnvGetStr( "osName" )
        }

        # 0=unknown 1=window 2=unix (OS family shortcut, §12.2).
        public static get Int32 osFamily()
        {
            ret SystemPlatformEnvGetInt( "osFamily" )
        }

        public static get OSVersionNumber osVersionNumber()
        {
            ret OSVersionNumber()
        }

        # ── form ──
        public static get Int32 form()
        {
            ret SystemPlatformEnvGetInt( "form" )
        }

        # ── arch ──
        public static get Int32 arch()
        {
            ret SystemPlatformEnvGetInt( "arch" )
        }

        # 32 / 64 / 0=unknown.
        public static get Int32 archBits()
        {
            ret SystemPlatformEnvGetInt( "archBits" )
        }

        public static get Int32 endian()
        {
            ret SystemPlatformEnvGetInt( "endian" )
        }

        public static get string triple()
        {
            ret SystemPlatformEnvGetStr( "triple" )
        }

        # ── cpu ──
        public static get Int32 cpuCount()
        {
            ret SystemPlatformEnvGetInt( "cpuCount" )
        }

        public static get string cpuVendor()
        {
            ret SystemPlatformEnvGetStr( "cpuVendor" )
        }

        public static get string cpuModel()
        {
            ret SystemPlatformEnvGetStr( "cpuModel" )
        }

        # ── isa (bit set; isa param takes an isa member value) ──
        public static bool isaHas( Int32 isa )
        {
            ret SystemPlatformEnvIsaHas( isa )
        }

        # ── device (bit set; dev param takes a device member value) ──
        public static bool deviceHas( Int32 dev )
        {
            ret SystemPlatformEnvDeviceHas( dev )
        }

        public static Int32 deviceCount( Int32 dev )
        {
            ret SystemPlatformEnvDeviceCount( dev )
        }

        # ── runtime / build ──
        public static get Int32 runtime()
        {
            ret SystemPlatformEnvGetInt( "runtime" )
        }

        public static get string runtimeVersion()
        {
            ret SystemPlatformEnvGetStr( "runtimeVersion" )
        }

        public static get Int32 build()
        {
            ret SystemPlatformEnvGetInt( "buildMode" )
        }

        # ── memory ──
        public static get Int64 totalMemMB()
        {
            ret SystemPlatformEnvGetInt64( "totalMemMB" )
        }

        public static get Int64 availMemMB()
        {
            ret SystemPlatformEnvGetInt64( "availMemMB" )
        }

        # ── lib (§9.2 dynamic library presence/version query; judgement
        # forms honour --disable / --enable lib.<name> overrides) ──
        public static bool libExists( string name )
        {
            ret SystemPlatformLibExists( name )
        }

        # Version string of a known lib ("" when the lib is absent or not
        # registered in the C version symbol table — §9.2 conservative
        # policy: only registered libs report a version).
        public static string libVersion( string name )
        {
            ret SystemPlatformLibVersion( name )
        }

        # Whether the lib is present AND version >= minVersion.
        # Empty minVersion = presence only; unknown version does not
        # fail the check (§9.2 no false negative).
        public static bool libHas( string name, string minVersion )
        {
            ret SystemPlatformLibHas( name, minVersion )
        }

        # Full path of the probed lib ("" when absent or unknown).
        public static string libPath( string name )
        {
            ret SystemPlatformLibPath( name )
        }

        # ── six new categories (§12.12-12.17, P2.7b) ──

        public static get Cpu cpu()
        {
            ret Cpu()
        }

        public static get Ai ai()
        {
            ret Ai()
        }

        public static get Render render()
        {
            ret Render()
        }

        public static get Network network()
        {
            ret Network()
        }

        public static get Script script()
        {
            ret Script()
        }

        public static get Embedded embedded()
        {
            ret Embedded()
        }

        # ── device (§12.6, P3) ──

        public static get Device device()
        {
            ret Device()
        }
    }

    # -------------------------------------------------------------------------
    # probe - raw probed values (§8.5.6: probe = raw, current = effective).
    #
    # Mirrors current, but every getter reads the Probe system methods, so
    # the values are never affected by --disable / --enable / --set
    # overrides. Use for diagnostics / reporting ("what is really there");
    # branch on current ("what the code sees").
    # -------------------------------------------------------------------------

    public class probe extends Object
    {
        # ── os ──
        public static get Int32 os()
        {
            ret SystemPlatformEnvProbeGetInt( "os" )
        }

        public static get string osName()
        {
            ret SystemPlatformEnvProbeGetStr( "osName" )
        }

        public static get Int32 osFamily()
        {
            ret SystemPlatformEnvProbeGetInt( "osFamily" )
        }

        public static get ProbeOSVersionNumber osVersionNumber()
        {
            ret ProbeOSVersionNumber()
        }

        # ── form ──
        public static get Int32 form()
        {
            ret SystemPlatformEnvProbeGetInt( "form" )
        }

        # ── arch ──
        public static get Int32 arch()
        {
            ret SystemPlatformEnvProbeGetInt( "arch" )
        }

        public static get Int32 archBits()
        {
            ret SystemPlatformEnvProbeGetInt( "archBits" )
        }

        public static get Int32 endian()
        {
            ret SystemPlatformEnvProbeGetInt( "endian" )
        }

        public static get string triple()
        {
            ret SystemPlatformEnvProbeGetStr( "triple" )
        }

        # ── cpu ──
        public static get Int32 cpuCount()
        {
            ret SystemPlatformEnvProbeGetInt( "cpuCount" )
        }

        public static get string cpuVendor()
        {
            ret SystemPlatformEnvProbeGetStr( "cpuVendor" )
        }

        public static get string cpuModel()
        {
            ret SystemPlatformEnvProbeGetStr( "cpuModel" )
        }

        # ── isa ──
        public static bool isaHas( Int32 isa )
        {
            ret SystemPlatformEnvProbeIsaHas( isa )
        }

        # ── device ──
        public static bool deviceHas( Int32 dev )
        {
            ret SystemPlatformEnvProbeDeviceHas( dev )
        }

        public static Int32 deviceCount( Int32 dev )
        {
            ret SystemPlatformEnvProbeDeviceCount( dev )
        }

        # ── runtime / build / memory ──
        public static get Int32 runtime()
        {
            ret SystemPlatformEnvProbeGetInt( "runtime" )
        }

        public static get string runtimeVersion()
        {
            ret SystemPlatformEnvProbeGetStr( "runtimeVersion" )
        }

        public static get Int32 build()
        {
            ret SystemPlatformEnvProbeGetInt( "buildMode" )
        }

        public static get Int64 totalMemMB()
        {
            ret SystemPlatformEnvProbeGetInt64( "totalMemMB" )
        }

        public static get Int64 availMemMB()
        {
            ret SystemPlatformEnvProbeGetInt64( "availMemMB" )
        }

        # ── lib (§9.2; raw probe: never affected by overrides, use for
        # diagnostics / "what is really there") ──
        public static bool libExists( string name )
        {
            ret SystemPlatformLibProbeExists( name )
        }

        public static string libVersion( string name )
        {
            ret SystemPlatformLibProbeVersion( name )
        }

        public static bool libHas( string name, string minVersion )
        {
            ret SystemPlatformLibProbeHas( name, minVersion )
        }

        public static string libPath( string name )
        {
            ret SystemPlatformLibProbePath( name )
        }

        # ── six new categories (§12.12-12.17, P2.7b; raw probe view,
        # never affected by --disable / --enable / --set) ──

        public static get ProbeCpu cpu()
        {
            ret ProbeCpu()
        }

        public static get ProbeAi ai()
        {
            ret ProbeAi()
        }

        public static get ProbeRender render()
        {
            ret ProbeRender()
        }

        public static get ProbeNetwork network()
        {
            ret ProbeNetwork()
        }

        public static get ProbeScript script()
        {
            ret ProbeScript()
        }

        public static get ProbeEmbedded embedded()
        {
            ret ProbeEmbedded()
        }

        # ── device (§12.6, P3; raw probe view) ──

        public static get ProbeDevice device()
        {
            ret ProbeDevice()
        }
    }

    # -------------------------------------------------------------------------
    # Override - runtime override query (§8.5.6: three read forms).
    #
    # Class name uses capital Override: "override" is the SL method override
    # keyword, and the lexer is case-sensitive (same naming rule as
    # get/set — no bare keyword identifiers).
    #   bool  off = Environment.Override.isDisabled( "device.gpu" )
    #   string src = Environment.Override.source( "cpu.avx2" )  # "" / "jsonc" / "env" / "cli" / "host"
    #   string val = Environment.Override.getValue( "memory" )  # SET value, "" when none
    # -------------------------------------------------------------------------

    public class Override extends Object
    {
        # Whether the override key is disabled (only DISABLE counts;
        # the key is normalized on the C side, e.g. "Device.GPU" matches).
        public static bool isDisabled( string key )
        {
            ret SystemPlatformOverrideIsDisabled( key )
        }

        # Which channel supplied the override for the key.
        # Returns "" when the key has no override (SL has no string?
        # syntax; same convention as custom.getValue).
        public static string source( string key )
        {
            ret SystemPlatformOverrideSource( key )
        }

        # The SET replacement value of the key ("" when not overridden
        # or the entry is a DISABLE/ENABLE, which carry no value).
        public static string getValue( string key )
        {
            ret SystemPlatformOverrideGetValue( key )
        }
    }

    # -------------------------------------------------------------------------
    # env - process environment variables (§13.2 ③).
    # -------------------------------------------------------------------------

    public class env extends Object
    {
        # Get the value of an environment variable by name.
        # Returns the value string, or an empty string if not found.
        public static string getValue( string name )
        {
            ret SystemEnvironmentGetVariable( name )
        }

        # Set an environment variable to the given value.
        # Returns true on success.
        public static bool setValue( string name, string value )
        {
            ret SystemEnvironmentSetVariable( name, value )
        }

        # Whether an environment variable exists and is non-empty.
        # (C side returns "" for missing entries, so length is the test.)
        public static bool exists( string name )
        {
            ret SystemStringLength( SystemEnvironmentGetVariable( name ) ) > 0
        }
    }

    # -------------------------------------------------------------------------
    # custom - custom condition values (jsonc require.custom, §13.2 ④).
    # -------------------------------------------------------------------------

    public class custom extends Object
    {
        # Get a custom value declared in the module's jsonc require.custom
        # section. Returns an empty string when the key is absent.
        public static string getValue( string key )
        {
            ret SystemPlatformCustomGet( key )
        }
    }

    # -------------------------------------------------------------------------
    # sys - directory / newline / timing (container for the old
    # Environment static members; a namespace cannot hold bare functions).
    # -------------------------------------------------------------------------

    public class sys extends Object
    {
        # Get the current working directory path.
        public static string currentDirectory()
        {
            ret SystemDirectoryGetCurrent()
        }

        # Set the current working directory.
        # Returns true on success.
        public static bool setCurrentDirectory( string path )
        {
            ret SystemDirectorySetCurrent( path )
        }

        # The platform newline string ("\n").
        public static get string newLine()
        {
            ret "\n"
        }

        # Monotonic tick count in milliseconds, as a 32-bit integer.
        # Wraps around every ~24.8 days (same semantics as C#
        # Environment.TickCount). Use tickCount64() if you need the
        # full 64-bit value without overflow.
        public static Int32 tickCount()
        {
            ret SystemConvertInt32( SystemTimerClock() )
        }

        # Monotonic tick count in milliseconds, as a 64-bit integer.
        # No overflow (same semantics as C# Environment.TickCount64).
        public static Int64 tickCount64()
        {
            ret SystemTimerClock()
        }

        # Unix timestamp in milliseconds since epoch (1970-01-01 UTC).
        public static Int64 nowMillis()
        {
            ret SystemTimerNowMillis()
        }
    }

    # -------------------------------------------------------------------------
    # legacy - P1.6 compatibility shell (§13.13).
    # New code should use Environment.env.getValue / Environment.env.setValue.
    # -------------------------------------------------------------------------

    public class legacy extends Object
    {
        # Deprecated: use Environment.env.getValue( name ).
        public static string getVariable( string name )
        {
            ret SystemEnvironmentGetVariable( name )
        }

        # Deprecated: use Environment.env.setValue( name, value ).
        public static bool setVariable( string name, string value )
        {
            ret SystemEnvironmentSetVariable( name, value )
        }
    }
}
