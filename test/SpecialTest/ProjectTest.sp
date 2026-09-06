Project
{
    _main_()
    {
        SystemPrintln( "Hello World" );
        nowMs = Environment.nowMillis()

        # AOT 测试用例（自 test/AOTTest/ProjectTest.sp 合并）
        r1 = AOTMath.Add( 1, 2 );
        r2 = AOTMath.Mul( 3, 4 );
        r3 = AOTMath.SumLoop( 10 );
        r4 = AOTMath.Sub( 10, 4 );
        r5 = AOTMath.CallVmSub( 10, 4 );
        r6 = AOTMath.CallVmAvg( 3.0, 5.0 );
        SystemPrintln( "Add(1,2)=$r1.toString()" );
        SystemPrintln( "Mul(3,4)=$r2.toString()" );
        SystemPrintln( "SumLoop(10)=$r3.toString()" );
        SystemPrintln( "Sub(10,4)=$r4.toString()" );
        SystemPrintln( "CallVmSub(10,4)=$r5.toString()" );
        SystemPrintln( "CallVmAvg(3,5)=$r6.toString()" );

        # 数组批量计算 AOT 测试（数组平均 / 高斯因子 / 数组高斯平均）
        double[] ga = Array<double>(5){ 1.0, 2.0, 3.0, 4.0, 5.0 }
        r7 = AOTMath.ArrayAvg( ga );
        r8 = AOTMath.GaussFactor( 1.0 );
        r9 = AOTMath.GaussFactorVm( 1.0 );
        r10 = AOTMath.ArrayGaussAvg( ga );
        SystemPrintln( "ArrayAvg([1..5])=$r7.toString()" );
        SystemPrintln( "GaussFactor(1.0)=$r8.toString()" );
        SystemPrintln( "GaussFactorVm(1.0)=$r9.toString()" );
        SystemPrintln( "ArrayGaussAvg([1..5])=$r10.toString()" );

        # ---- AOT struct ABI 测试（data/class 跨界编组，AOTStructTest.sl）----
        # 1) kind=2 data 参数 + 成员写 + 调用后回写源对象（void）
        AotVec2 sv1 = AotVec2(){ x = 3.0, y = 4.0 }
        AOTStructMath.VecScale( sv1, 2.0 );
        SystemPrintln( "VecScale(3,4)*2 -> x=$sv1.x.toString() y=$sv1.y.toString()" );
        AotVec2 sv2 = AotVec2(){ x = 3.0, y = 4.0 }
        AOTStructMath.VecScaleVm( sv2, 2.0 );
        SystemPrintln( "VecScaleVm(3,4)*2 -> x=$sv2.x.toString() y=$sv2.y.toString()" );

        # 2) struct 返回（ret 预置协议；源参数按引用回写）
        AotVec2 sv3 = AotVec2(){ x = 1.5, y = 2.5 }
        AotVec2 sr1 = AOTStructMath.VecScaled( sv3, 4.0 );
        SystemPrintln( "VecScaled(1.5,2.5)*4 ret -> x=$sr1.x.toString() y=$sr1.y.toString()" );
        SystemPrintln( "VecScaled src after call -> x=$sv3.x.toString() y=$sv3.y.toString()" );
        AotVec2 sv4 = AotVec2(){ x = 1.5, y = 2.5 }
        AotVec2 sr2 = AOTStructMath.VecScaledVm( sv4, 4.0 );
        SystemPrintln( "VecScaledVm(1.5,2.5)*4 ret -> x=$sr2.x.toString() y=$sr2.y.toString()" );

        # 3) 双 struct 参数纯读（GEP 直读）
        AotVec2 da = AotVec2(){ x = 1.0, y = 2.0 }
        AotVec2 db = AotVec2(){ x = 3.0, y = 4.0 }
        r11 = AOTStructMath.VecDot( da, db );
        r12 = AOTStructMath.VecDotVm( da, db );
        r13 = AOTStructMath.VecDotViaVm( da, db );
        SystemPrintln( "VecDot((1,2),(3,4)) AOT=$r11.toString() VM=$r12.toString() bridge=$r13.toString()" );

        # 4) 桥 kind=2 双向同步：解释器突变 data，AOT 读回新值
        AotVec2 bv = AotVec2(){ x = 10.0, y = 0.0 }
        r14 = AOTStructMath.BumpAndRead( bv );
        SystemPrintln( "BumpAndRead(10) -> ret=$r14.toString() src.x=$bv.x.toString()" );

        # 5) 嵌套 data 成员链式读（ptr-32 trick）
        AotParticle pp = new()
        pp.mass = 2.0
        pp.vel = AotVec2(){ x = 3.0, y = 4.0 }
        r15 = AOTStructMath.ParticleEnergy( pp );
        r16 = AOTStructMath.ParticleEnergyVm( pp );
        SystemPrintln( "ParticleEnergy(m=2,vel=(3,4)) AOT=$r15.toString() VM=$r16.toString()" );

        # 6) 嵌套 data 成员链式写
        AotParticle pq = new()
        pq.mass = 2.0
        pq.pos = AotVec2(){ x = 1.0, y = 2.0 }
        pq.vel = AotVec2(){ x = 3.0, y = 4.0 }
        AOTStructMath.ParticleStep( pq, 0.5 );
        SystemPrintln( "ParticleStep(pos=(1,2),vel=(3,4),dt=0.5) -> x=$pq.pos.x.toString() y=$pq.pos.y.toString()" );
        AotParticle pq2 = new()
        pq2.mass = 2.0
        pq2.pos = AotVec2(){ x = 1.0, y = 2.0 }
        pq2.vel = AotVec2(){ x = 3.0, y = 4.0 }
        AOTStructMath.ParticleStepVm( pq2, 0.5 );
        SystemPrintln( "ParticleStepVm same -> x=$pq2.pos.x.toString() y=$pq2.pos.y.toString()" );

        # 7) kind=3 class 引用透传（AOT -> 桥 -> 解释器读成员）
        AotBox ab = new()
        ab.v = 7.5
        r17 = AOTStructMath.PassBoxThrough( ab );
        SystemPrintln( "PassBoxThrough(v=7.5) -> $r17.toString()" );

        # FFI 测试用例（动态库加载/调用/回调/Float8 struct 等）
        #FFITest.fun();

        # ---- AOT GPU 矩阵乘测试（大张量，矩阵用一维数组） ----
        gM = 512
        gN = 512
        gK = 512
        gAK = gM * gK
        gBK = gK * gN
        gMN = gM * gN
        double[] gA = Array<double>.create( gAK )
        double[] gB = Array<double>.create( gBK )
        double[] gCG = Array<double>.create( gMN )
        double[] gCC = Array<double>.create( gMN )
        # 数据初始化（确定性小值域公式）
        gi = 0
        for gi = 0, gi < gAK, gi += 1 { gA[gi] = 0.001 * ( gi % 7 ) }
        gi = 0
        for gi = 0, gi < gBK, gi += 1 { gB[gi] = 0.001 * ( gi % 5 ) }

        SystemPrintln( "===== AOT GPU MatMul $gM.toString() x $gN.toString() x $gK.toString() =====" )

        # GPU 版两轮（首轮含 CUDA 上下文/PTX JIT 初始化）
        gT = Environment.nowMillis()
        AOTGPUTest.GpuMatMul( gA, gB, gCG, gM, gN, gK )
        gT = Environment.nowMillis() - gT
        SystemPrintln( "GPU  run1: $gT.toString() ms" )
        gT = Environment.nowMillis()
        AOTGPUTest.GpuMatMul( gA, gB, gCG, gM, gN, gK )
        gT = Environment.nowMillis() - gT
        SystemPrintln( "GPU  run2: $gT.toString() ms" )

        # CPU AOT 版两轮
        gT = Environment.nowMillis()
        AOTGPUTest.CpuMatMul( gA, gB, gCC, gM, gN, gK )
        gT = Environment.nowMillis() - gT
        SystemPrintln( "CPU  run1: $gT.toString() ms" )
        gT = Environment.nowMillis()
        AOTGPUTest.CpuMatMul( gA, gB, gCC, gM, gN, gK )
        gT = Environment.nowMillis() - gT
        SystemPrintln( "CPU  run2: $gT.toString() ms" )

        # 结果一致性：逐元素差值累加
        gDiff = 0.0
        gi = 0
        for gi = 0, gi < gMN, gi += 1
        {
            d = gCG[gi] - gCC[gi]
            if d < 0.0 { d = -d }
            gDiff = gDiff + d
        }
        SystemPrintln( "GPU/CPU max-diff-sum: $gDiff.toString()" )
        gv0 = gCG[0]
        gv1 = gCG[1]
        gvL = gCG[gMN - 1]
        cv0 = gCC[0]
        cv1 = gCC[1]
        cvL = gCC[gMN - 1]
        SystemPrintln( "GPU  c[0]=$gv0.toString() c[1]=$gv1.toString() c[last]=$gvL.toString()" )
        SystemPrintln( "CPU  c[0]=$cv0.toString() c[1]=$cv1.toString() c[last]=$cvL.toString()" )

        # 解释器对照（规模缩小到 128^3，解释执行全量太慢）
        vM = 128
        vN = 128
        vK = 128
        double[] vA = Array<double>.create( vM * vK )
        double[] vB = Array<double>.create( vK * vN )
        double[] vC = Array<double>.create( vM * vN )
        vi = 0
        for vi = 0, vi < vM * vK, vi += 1 { vA[vi] = 0.001 * ( vi % 7 ) }
        vi = 0
        for vi = 0, vi < vK * vN, vi += 1 { vB[vi] = 0.001 * ( vi % 5 ) }
        gT = Environment.nowMillis()
        AOTGPUTest.VmMatMul( vA, vB, vC, vM, vN, vK )
        gT = Environment.nowMillis() - gT
        SystemPrintln( "VM   (128^3): $gT.toString() ms" )
        vv0 = vC[0]
        SystemPrintln( "VM   c[0]=$vv0.toString()" )

        nowMs = Environment.nowMillis() - nowMs
        SystemPrintln("===== BenchMark _main_ end [$nowMs.toString() ms] =====")
    }
}
