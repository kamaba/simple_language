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
