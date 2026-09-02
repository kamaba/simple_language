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
    }
}
