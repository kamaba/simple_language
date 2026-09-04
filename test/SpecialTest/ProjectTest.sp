Project
{
    _main_()
    {
        SystemPrintln( "Hello World" );

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
        FFITest.fun();
    }
}
