# PlatformSixTest - P2.7b six new categories SL access test (design
# PLATFORM_CAPABILITY_DESIGN.md §12.12-12.17 / §16 groups K/L/M/N/O/P).
#
# Environment.current.{cpu,ai,render,network,script,embedded} plus the
# Environment.probe raw mirror. Two assertion styles (same as
# PlatformLibTest):
#   check()   - host-independent facts: enum value ranges, current/probe
#               mirror, shortcut consistency (isOnline == linkState==2),
#               fake-name lookups, general-platform expectations
#               (bareMetal false, empty peripherals, isConstrained true
#               per §12.17: unknown flash/ram = 0 counts as constrained).
#   observe() - environment-dependent values (cpu model numbers, render
#               API, online state, script availability), printed raw.
# Note: SystemPlatform* dispatch is CVM-only (same as PlatformLibTest).

PlatformSixTest
{
    # 统一断言辅助：cond 为 true 打印 OK，否则打印 FAIL
    static check( string name, bool cond )
    {
        if cond
        {
            SystemPrintln( "[PlatformSixTest] " + name + " : OK" )
        }
        else
        {
            SystemPrintln( "[PlatformSixTest] " + name + " : FAIL" )
        }
    }

    # 观测：打印原值（不断言），供人工 / exe 直跑脚本核对
    static observeS( string name, string val )
    {
        SystemPrintln( "[PlatformSixTest] " + name + " : " + val )
    }

    static fun()
    {
        SystemPrintln( "========== PlatformSixTest (start) ==========" )

        # --- K. cpu topology (§12.12) ---
        SystemPrintln( "--- cpu (K, §12.12) ---" )
        cCpu = Environment.current.cpu
        pCpu = Environment.probe.cpu
        check( "cpu logicalCores >= 1", cCpu.logicalCores >= 1 )
        check( "cpu physicalCores >= 1", cCpu.physicalCores >= 1 )
        check( "cpu physical <= logical", cCpu.physicalCores <= cCpu.logicalCores )
        check( "cpu topology in enum 0..5", cCpu.topology >= 0 && cCpu.topology <= 5 )
        check( "cpu numaNodes >= 1", cCpu.numaNodes >= 1 )
        check( "cpu freqMHz >= 0", cCpu.freqMHz >= 0 )
        check( "cpu maxFreqMHz >= freqMHz", cCpu.maxFreqMHz >= cCpu.freqMHz )
        check( "cpu cache L1/L2/L3 >= 0",
            cCpu.cacheL1KB >= 0 && cCpu.cacheL2KB >= 0 && cCpu.cacheL3KB >= 0 )
        check( "cpu perf/eff cores >= 0", cCpu.perfCores >= 0 && cCpu.efficiencyCores >= 0 )
        check( "cpu current/probe mirror",
            pCpu.logicalCores == cCpu.logicalCores && pCpu.topology == cCpu.topology )
        observeS( "cpu logical/physical", cCpu.logicalCores.toString() + "/" + cCpu.physicalCores.toString() )
        observeS( "cpu topology (2=smp 3=bigLittle 4=hybrid 5=numa)", cCpu.topology.toString() )
        observeS( "cpu hasHyperThreading", cCpu.hasHyperThreading.toString() )
        observeS( "cpu freq/maxFreq MHz", cCpu.freqMHz.toString() + "/" + cCpu.maxFreqMHz.toString() )
        observeS( "cpu L1/L2/L3 KB", cCpu.cacheL1KB.toString() + "/" + cCpu.cacheL2KB.toString() + "/" + cCpu.cacheL3KB.toString() )
        observeS( "cpu perf/eff cores", cCpu.perfCores.toString() + "/" + cCpu.efficiencyCores.toString() )

        # --- L. ai stacks (§12.13) ---
        SystemPrintln( "--- ai (L, §12.13) ---" )
        cAi = Environment.current.ai
        pAi = Environment.probe.ai
        check( "ai fake stack has(99) == false", cAi.has( 99 ) == false )
        check( "ai fake stack version(99) == empty", cAi.version( 99 ) == "" )
        check( "ai none member has(0) == false", cAi.has( Environment.Platform.ai.none ) == false )
        check( "ai any() == (preferred non-empty)",
            cAi.any() == ( SystemStringLength( cAi.preferred ) > 0 ) )
        check( "ai current/probe mirror", pAi.any() == cAi.any() && pAi.preferred == cAi.preferred )
        observeS( "ai any()", cAi.any().toString() )
        observeS( "ai preferred", cAi.preferred )
        observeS( "ai has(onnxruntime)", cAi.has( Environment.Platform.ai.onnxruntime ).toString() )
        observeS( "ai has(tensorRT)", cAi.has( Environment.Platform.ai.tensorRT ).toString() )

        # --- M. render (§12.14) ---
        SystemPrintln( "--- render (M, §12.14) ---" )
        cRen = Environment.current.render
        pRen = Environment.probe.render
        check( "render api in enum 0..8", cRen.api >= 0 && cRen.api <= 8 )
        check( "render shaderModel 0 or 50..67",
            cRen.shaderModel == 0 || ( cRen.shaderModel >= 50 && cRen.shaderModel <= 67 ) )
        check( "render maxTextureSize >= 0", cRen.maxTextureSize >= 0 )
        check( "render displays >= 0", cRen.displays >= 0 )
        check( "render current/probe mirror",
            pRen.api == cRen.api && pRen.shaderModel == cRen.shaderModel )
        renApis = cRen.apiList()
        check( "render apiList() length >= 0", renApis.length >= 0 )
        observeS( "render api (8=software when no GPU)", cRen.api.toString() )
        observeS( "render apiList().length", renApis.length.toString() )
        if renApis.length > 0
        {
            observeS( "render apiList()[0]", renApis[0] )
        }
        observeS( "render shaderModel (0=unknown 50=sm_5_0)", cRen.shaderModel.toString() )
        observeS( "render hasRayTracing", cRen.hasRayTracing.toString() )
        observeS( "render hasMeshShader", cRen.hasMeshShader.toString() )
        observeS( "render hasCompute", cRen.hasCompute.toString() )
        observeS( "render hasHdr", cRen.hasHdr.toString() )
        observeS( "render primary WxH", cRen.primaryWidth.toString() + "x" + cRen.primaryHeight.toString() )
        observeS( "render refreshHz", cRen.refreshHz.toString() )

        # --- N. network (§12.15) ---
        SystemPrintln( "--- network (N, §12.15) ---" )
        cNet = Environment.current.network
        pNet = Environment.probe.network
        check( "net netType in enum 0..5", cNet.netType >= 0 && cNet.netType <= 5 )
        check( "net linkState in enum 0..2", cNet.linkState >= 0 && cNet.linkState <= 2 )
        check( "net isOnline == (linkState == 2)", cNet.isOnline == ( cNet.linkState == 2 ) )
        check( "net isLocalOnly == (linkState == 1)", cNet.isLocalOnly == ( cNet.linkState == 1 ) )
        check( "net interfaces >= 0 (L1/L2 no blocking)", cNet.interfaces >= 0 )
        check( "net current/probe mirror",
            pNet.netType == cNet.netType && pNet.linkState == cNet.linkState )
        observeS( "net netType (2=ethernet 3=wifi)", cNet.netType.toString() )
        observeS( "net linkState (1=lan 2=online)", cNet.linkState.toString() )
        observeS( "net isOnline", cNet.isOnline.toString() )
        observeS( "net interfaces", cNet.interfaces.toString() )
        observeS( "net hasProxy", cNet.hasProxy.toString() )
        observeS( "net proxyUrl", cNet.proxyUrl )

        # --- O. script runtimes (§12.16) ---
        SystemPrintln( "--- script (O, §12.16) ---" )
        cScr = Environment.current.script
        pScr = Environment.probe.script
        check( "script fake has(name) == false", cScr.has( "nosuchscript_p27" ) == false )
        check( "script fake has(name,min) == false", cScr.has( "nosuchscript_p27", "1.0" ) == false )
        check( "script fake version == empty", cScr.version( "nosuchscript_p27" ) == "" )
        check( "script fake path == empty", cScr.path( "nosuchscript_p27" ) == "" )
        check( "script has(name) == has(name,\"\") overload mirror",
            cScr.has( "lua" ) == cScr.has( "lua", "" ) )
        check( "script current/probe mirror",
            pScr.has( "lua" ) == cScr.has( "lua" ) && pScr.version( "lua" ) == cScr.version( "lua" ) )
        scrAvail = cScr.available()
        check( "script available() length >= 0", scrAvail.length >= 0 )
        observeS( "script has(lua)", cScr.has( "lua" ).toString() )
        observeS( "script version(lua)", cScr.version( "lua" ) )
        observeS( "script available().length", scrAvail.length.toString() )
        Int32 i = 0
        while i < scrAvail.length
        {
            observeS( "script available()[" + i.toString() + "]", scrAvail[i] )
            i = i + 1
        }

        # --- P. embedded (§12.17) ---
        SystemPrintln( "--- embedded (P, §12.17) ---" )
        cEmb = Environment.current.embedded
        pEmb = Environment.probe.embedded
        check( "emb family in enum 0..13", cEmb.family >= 0 && cEmb.family <= 13 )
        check( "emb rtos 0 or 50..57", cEmb.rtos == 0 || ( cEmb.rtos >= 50 && cEmb.rtos <= 57 ) )
        check( "emb general platform family == none", cEmb.family == Environment.Platform.embedded.none )
        check( "emb general platform bareMetal == false", cEmb.bareMetal == false )
        check( "emb general platform rtos == none", cEmb.rtos == Environment.Platform.rtos.none )
        check( "emb isConstrained() true on general platform (flash/ram 0)",
            cEmb.isConstrained() == true )
        check( "emb fake peripheral has == false", cEmb.has( "nosuch_peripheral_p27" ) == false )
        embPeris = cEmb.peripherals()
        check( "emb peripherals() empty on general platform", embPeris.length == 0 )
        check( "emb current/probe mirror",
            pEmb.family == cEmb.family && pEmb.isConstrained() == cEmb.isConstrained() )
        observeS( "emb family (0=none)", cEmb.family.toString() )
        observeS( "emb chip", cEmb.chip )
        observeS( "emb flashKB/ramKB", cEmb.flashKB.toString() + "/" + cEmb.ramKB.toString() )
        observeS( "emb hasFpu/hasMmu/hasMpu/hasDsp",
            cEmb.hasFpu.toString() + "/" + cEmb.hasMmu.toString() + "/" + cEmb.hasMpu.toString() + "/" + cEmb.hasDsp.toString() )

        SystemPrintln( "========== PlatformSixTest (end) ==========" )
    }
}
