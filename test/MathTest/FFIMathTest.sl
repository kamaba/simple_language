import Std;
import Math;

# ============================================================
# FFIMathTest - Mathf / Mathd / Mathh FFI 数学函数归类测试
# Math 模块经 @DllImport( "math_lib", "符号名", "参数签名" )
# 三参形式（含参数签名，如 "Float32,Float32->Float32"）绑定
# math_lib.dll（别名经 Math.jsonc dllImports 表编译期解析）；
# DLL 绑定失败时自动回落 SL 实现 —— 断言全部选数学恒等式，
# 对 FFI 直调与 SL 回落两条路径均精确成立。
# ============================================================
class FFIMathTest
{
    static check( string name, bool cond )
    {
        if ( cond )
        {
            Console.println( "  [PASS] " + name )
        }
        else
        {
            Console.println( "  [FAIL] " + name )
        }
    }

    # ── Mathf（Float32 FFI：mathf_* 符号）───────────────────
    static testFloat32()
    {
        Console.println( "===== FFIMathTest.testFloat32 =====" )

        # dot3（矩阵元素级运算的 FFI 底座）
        check( "dot3(1,2,3,4,5,6) == 32", Mathf.dot3( 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f ) == 32.0f )
        check( "dot3 zeros == 0", Mathf.dot3( 0.0f, 0.0f, 0.0f, 4.0f, 5.0f, 6.0f ) == 0.0f )

        # 三角恒等式（0/1 精确可表示）
        check( "sin(0) == 0", Mathf.sin( 0.0f ) == 0.0f )
        check( "cos(0) == 1", Mathf.cos( 0.0f ) == 1.0f )
        check( "tan(0) == 0", Mathf.tan( 0.0f ) == 0.0f )
        check( "asin(0) == 0", Mathf.asin( 0.0f ) == 0.0f )
        check( "acos(1) == 0", Mathf.acos( 1.0f ) == 0.0f )
        check( "atan(0) == 0", Mathf.atan( 0.0f ) == 0.0f )
        check( "atan2(0,1) == 0", Mathf.atan2( 0.0f, 1.0f ) == 0.0f )
        check( "sinh(0) == 0", Mathf.sinh( 0.0f ) == 0.0f )
        check( "cosh(0) == 1", Mathf.cosh( 0.0f ) == 1.0f )
        check( "tanh(0) == 0", Mathf.tanh( 0.0f ) == 0.0f )

        # 幂 / 指数 / 对数（整数结果精确）
        check( "sqrt(4) == 2", Mathf.sqrt( 4.0f ) == 2.0f )
        check( "sqrt(0) == 0", Mathf.sqrt( 0.0f ) == 0.0f )
        check( "sqrt(16) == 4", Mathf.sqrt( 16.0f ) == 4.0f )
        check( "pow(2,10) == 1024", Mathf.pow( 2.0f, 10.0f ) == 1024.0f )
        check( "pow(10,2) == 100", Mathf.pow( 10.0f, 2.0f ) == 100.0f )
        check( "exp(0) == 1", Mathf.exp( 0.0f ) == 1.0f )
        check( "log(1) == 0", Mathf.log( 1.0f ) == 0.0f )
        check( "log10(100) == 2", Mathf.log10( 100.0f ) == 2.0f )

        # 取整
        check( "floor(2.7) == 2", Mathf.floor( 2.7f ) == 2.0f )
        check( "ceil(2.1) == 3", Mathf.ceil( 2.1f ) == 3.0f )
        check( "round(2.4) == 2", Mathf.round( 2.4f ) == 2.0f )
        check( "truncate(3.7) == 3", Mathf.truncate( 3.7f ) == 3 )

        # SL 层辅助（abs/min/max/clamp/sign/lerp）
        check( "abs(-5) == 5", Mathf.abs( -5.0f ) == 5.0f )
        check( "abs(5) == 5", Mathf.abs( 5.0f ) == 5.0f )
        check( "min(2,3) == 2", Mathf.min( 2.0f, 3.0f ) == 2.0f )
        check( "max(2,3) == 3", Mathf.max( 2.0f, 3.0f ) == 3.0f )
        check( "clamp(5,1,3) == 3", Mathf.clamp( 5.0f, 1.0f, 3.0f ) == 3.0f )
        check( "clamp(0,1,3) == 1", Mathf.clamp( 0.0f, 1.0f, 3.0f ) == 1.0f )
        check( "sign(5) == 1", Mathf.sign( 5.0f ) == 1 )
        check( "sign(0) == 0", Mathf.sign( 0.0f ) == 0 )
        check( "lerp(1,3,0.5) == 2", Mathf.lerp( 1.0f, 3.0f, 0.5f ) == 2.0f )

        # Int32 重载
        check( "abs(-5i) == 5", Mathf.abs( -5 ) == 5 )
        check( "min(2i,3i) == 2", Mathf.min( 2, 3 ) == 2 )
        check( "max(2i,3i) == 3", Mathf.max( 2, 3 ) == 3 )
        check( "clamp(5i,1i,3i) == 3", Mathf.clamp( 5, 1, 3 ) == 3 )
        check( "powInt(2,10) == 1024", Mathf.powInt( 2, 10 ) == 1024 )
    }

    # ── Mathd（Float64 FFI：mathd_* 符号）───────────────────
    static testFloat64()
    {
        Console.println( "===== FFIMathTest.testFloat64 =====" )

        check( "dot3(1,2,3,4,5,6) == 32", Mathd.dot3( 1.0d, 2.0d, 3.0d, 4.0d, 5.0d, 6.0d ) == 32.0d )
        check( "dot3 zeros == 0", Mathd.dot3( 0.0d, 0.0d, 0.0d, 4.0d, 5.0d, 6.0d ) == 0.0d )

        check( "sin(0) == 0", Mathd.sin( 0.0d ) == 0.0d )
        check( "cos(0) == 1", Mathd.cos( 0.0d ) == 1.0d )
        check( "tan(0) == 0", Mathd.tan( 0.0d ) == 0.0d )
        check( "asin(0) == 0", Mathd.asin( 0.0d ) == 0.0d )
        check( "acos(1) == 0", Mathd.acos( 1.0d ) == 0.0d )
        check( "atan(0) == 0", Mathd.atan( 0.0d ) == 0.0d )
        check( "atan2(0,1) == 0", Mathd.atan2( 0.0d, 1.0d ) == 0.0d )
        check( "sinh(0) == 0", Mathd.sinh( 0.0d ) == 0.0d )
        check( "cosh(0) == 1", Mathd.cosh( 0.0d ) == 1.0d )
        check( "tanh(0) == 0", Mathd.tanh( 0.0d ) == 0.0d )

        check( "sqrt(4) == 2", Mathd.sqrt( 4.0d ) == 2.0d )
        check( "sqrt(0) == 0", Mathd.sqrt( 0.0d ) == 0.0d )
        check( "sqrt(16) == 4", Mathd.sqrt( 16.0d ) == 4.0d )
        check( "pow(2,10) == 1024", Mathd.pow( 2.0d, 10.0d ) == 1024.0d )
        check( "pow(10,2) == 100", Mathd.pow( 10.0d, 2.0d ) == 100.0d )
        check( "exp(0) == 1", Mathd.exp( 0.0d ) == 1.0d )
        check( "log(1) == 0", Mathd.log( 1.0d ) == 0.0d )
        check( "log10(100) == 2", Mathd.log10( 100.0d ) == 2.0d )

        check( "floor(2.7) == 2", Mathd.floor( 2.7d ) == 2.0d )
        check( "ceil(2.1) == 3", Mathd.ceil( 2.1d ) == 3.0d )
        check( "round(2.4) == 2", Mathd.round( 2.4d ) == 2.0d )
        check( "truncate(3.7) == 3", Mathd.truncate( 3.7d ) == 3 )

        check( "abs(-5) == 5", Mathd.abs( -5.0d ) == 5.0d )
        check( "abs(5) == 5", Mathd.abs( 5.0d ) == 5.0d )
        check( "min(2,3) == 2", Mathd.min( 2.0d, 3.0d ) == 2.0d )
        check( "max(2,3) == 3", Mathd.max( 2.0d, 3.0d ) == 3.0d )
        check( "clamp(5,1,3) == 3", Mathd.clamp( 5.0d, 1.0d, 3.0d ) == 3.0d )
        check( "clamp(0,1,3) == 1", Mathd.clamp( 0.0d, 1.0d, 3.0d ) == 1.0d )
        check( "sign(5) == 1", Mathd.sign( 5.0d ) == 1 )
        check( "sign(0) == 0", Mathd.sign( 0.0d ) == 0 )
        check( "lerp(1,3,0.5) == 2", Mathd.lerp( 1.0d, 3.0d, 0.5d ) == 2.0d )

        # Int32 重载
        check( "abs(-5i) == 5", Mathd.abs( -5 ) == 5 )
        check( "min(2i,3i) == 2", Mathd.min( 2, 3 ) == 2 )
        check( "max(2i,3i) == 3", Mathd.max( 2, 3 ) == 3 )
        check( "clamp(5i,1i,3i) == 3", Mathd.clamp( 5, 1, 3 ) == 3 )

        # Mathf/Mathd 一致性（同一数学值经两条 FFI 宽度）
        check( "f32/f64 sqrt(4) agree", Mathf.sqrt( 4.0f ).toFloat64() == Mathd.sqrt( 4.0d ) )
        check( "f32/f64 pow(2,10) agree", Mathf.pow( 2.0f, 10.0f ).toFloat64() == Mathd.pow( 2.0d, 10.0d ) )
    }

    # ── Mathh（Float16：toFloat32 中转走 Mathf FFI）─────────
    static testFloat16()
    {
        Console.println( "===== FFIMathTest.testFloat16 =====" )

        check( "sin(0) == 0", Mathh.sin( 0.0h ) == 0.0h )
        check( "cos(0) == 1", Mathh.cos( 0.0h ) == 1.0h )
        check( "tan(0) == 0", Mathh.tan( 0.0h ) == 0.0h )
        check( "asin(0) == 0", Mathh.asin( 0.0h ) == 0.0h )
        check( "acos(1) == 0", Mathh.acos( 1.0h ) == 0.0h )
        check( "atan(0) == 0", Mathh.atan( 0.0h ) == 0.0h )
        check( "atan2(0,1) == 0", Mathh.atan2( 0.0h, 1.0h ) == 0.0h )

        check( "sqrt(4) == 2", Mathh.sqrt( 4.0h ) == 2.0h )
        check( "sqrt(0) == 0", Mathh.sqrt( 0.0h ) == 0.0h )
        check( "sqrt(16) == 4", Mathh.sqrt( 16.0h ) == 4.0h )
        check( "pow(2,10) == 1024", Mathh.pow( 2.0h, 10.0h ) == 1024.0h )
        check( "exp(0) == 1", Mathh.exp( 0.0h ) == 1.0h )
        check( "log(1) == 0", Mathh.log( 1.0h ) == 0.0h )

        check( "floor(2.7) == 2", Mathh.floor( 2.7h ) == 2.0h )
        check( "ceil(2.1) == 3", Mathh.ceil( 2.1h ) == 3.0h )
        check( "truncate(3.7) == 3", Mathh.truncate( 3.7h ) == 3 )

        check( "abs(-5) == 5", Mathh.abs( 0.0h - 5.0h ) == 5.0h )
        check( "abs(5) == 5", Mathh.abs( 5.0h ) == 5.0h )
        check( "min(2,3) == 2", Mathh.min( 2.0h, 3.0h ) == 2.0h )
        check( "max(2,3) == 3", Mathh.max( 2.0h, 3.0h ) == 3.0h )
        check( "clamp(5,1,3) == 3", Mathh.clamp( 5.0h, 1.0h, 3.0h ) == 3.0h )
        check( "clamp(0,1,3) == 1", Mathh.clamp( 0.0h, 1.0h, 3.0h ) == 1.0h )
        check( "lerp(1,3,0.5) == 2", Mathh.lerp( 1.0h, 3.0h, 0.5h ) == 2.0h )
    }

    static fun()
    {
        testFloat32()
        testFloat64()
        testFloat16()
    }
}
