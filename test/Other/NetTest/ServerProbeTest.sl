import Std;
import Core;

# ============================================================================
# ServerProbeTest.sl -- SLangServer P0 前置探针（NetTest 旁路）
# 设计契约：SERVER_DESIGN.md §8 P0（风险 #1 / #15 / #19 前置排雷）
#
#   A 组  CallClosure(106) 多参直调（风险 #1）：
#       现有库回调先例仅单参 hMsg(msg)，(req,res) 双参 / (req,res,nx) 三参
#       直调依赖多参能力；auth 回调带值返回（风险 #19 ②）一并排雷。
#       不支持则 P1 起降级单参签名（设计已备该路径）。
#   B 组  handler 协程派发语义（风险 #15）：
#       B1 spawnClosure0 + awaitTask 带值返回；
#       B2 对已结束 Task 再次 awaitTask（安全包装，不得永久挂起）；
#       B3 waitTimeout 完成 -> true（方向）；
#       B4 waitTimeout 超时 -> false 且目标继续运行；
#       B5 cancel 后对目标 awaitTask（安全包装，不得永久挂起）；
#       B6 spawnClosure2 双参 handler 派发 + 等待（引擎派发路径）。
#
# 纯语言层探针，无网络端口占用。编写约定同 HttpTest.sl。
# ============================================================================

# 探针附件：Function 字段持有者（路由表 _CoreRoute._handler 形态）
class _ProbeHolder
{
    Function _h = null
}

ServerProbeTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[ServerProbeTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[ServerProbeTest] " + name + " : FAIL" )
        }
    }

    # ---- A 组：闭包多参直调 ----

    static probeClosures()
    {
        # A1 双参闭包变量直调（CallClosure 106 双实参）
        function f2 = function( Int32 a, Int32 b )
        {
            ret a + b
        }
        Int32 s = f2( 3, 4 )
        ServerProbeTest.check( "A1 two-arg closure direct call", s == 7 )

        # A2 三参闭包变量直调（中间件 (req,res,nx) 形态）
        function f3 = function( string a, string b, string c )
        {
            ret a + b + c
        }
        string j = f3( "x", "y", "z" )
        ServerProbeTest.check( "A2 three-arg closure direct call", j == "xyz" )

        # A3 单参 + object 传递 + 带值返回（auth 校验回调形态，风险 #19 ②）
        function fa = function( object o )
        {
            ret o
        }
        Int32 r3 = fa( 5 ) as Int32
        ServerProbeTest.check( "A3 closure object param + value return", r3 == 5 )

        # A4 双参 + 带值返回（并发聚合取结果形态）
        function fb = function( object a, object b )
        {
            Int32 x = a as Int32
            Int32 y = b as Int32
            ret x * y
        }
        Int32 r4 = fb( 6, 7 ) as Int32
        ServerProbeTest.check( "A4 two-arg object closure + value return", r4 == 42 )

        # A5 Function 字段 -> 拷局部 -> 调用（路由表派发形态；字段直传参数
        #    有参数收集风险，HttpHeaders 头注记纪律）。
        #    注：闭包字面量不能直接赋给字段（解析器不支持 X.f = function{}
        #    形态），先经局部再转存
        function fh = function( Int32 a, Int32 b )
        {
            ret a * b
        }
        _ProbeHolder holder = _ProbeHolder()
        holder._h = fh
        function hh = holder._h
        Int32 m = hh( 5, 6 )
        ServerProbeTest.check( "A5 closure field -> local -> call", m == 30 )
    }

    # ---- B 组：协程派发语义 ----

    static probeCoroutines()
    {
        # B1 spawnClosure0 + awaitTask 带值返回
        function f1 = function()
        {
            Coroutine.delay( 30 )
            ret 41
        }
        Task t1 = Coroutine.spawnClosure0( f1 )
        Int32 v1 = Coroutine.awaitTask( t1 ) as Int32
        ServerProbeTest.check( "B1 awaitTask value return", v1 == 41 )

        # B2 对已结束 Task 再次 awaitTask（安全包装 + waitTimeout 判定，
        #    挂死则判 FAIL 并取消包装协程，测试不卡死）
        function f2w = function()
        {
            Coroutine.awaitTask( t1 )
            ret 1
        }
        Task w2 = Coroutine.spawnClosure0( f2w )
        bool ok2 = Coroutine.waitTimeout( w2, 2000 )
        if ok2 == false
        {
            w2.cancel()
        }
        ServerProbeTest.check( "B2 awaitTask on dead task returns", ok2 )

        # B3 waitTimeout 方向：快速完成 -> true
        function f3 = function()
        {
            ret 0
        }
        Task t3 = Coroutine.spawnClosure0( f3 )
        ServerProbeTest.check( "B3 waitTimeout finished -> true", Coroutine.waitTimeout( t3, 2000 ) )

        # B4 waitTimeout 超时 -> false 且目标继续运行（等它自然结束验证未死）
        function f4 = function()
        {
            Coroutine.delay( 300 )
            ret 0
        }
        Task t4 = Coroutine.spawnClosure0( f4 )
        bool to4 = Coroutine.waitTimeout( t4, 50 )
        Int32 v4 = Coroutine.awaitTask( t4 ) as Int32
        ServerProbeTest.check( "B4 waitTimeout timeout -> false, task alive", to4 == false && v4 == 0 )

        # B5 cancel 后对目标 awaitTask（安全包装，不得永久挂起）
        function f5 = function()
        {
            Coroutine.delay( 300 )
            ret 9
        }
        Task t5 = Coroutine.spawnClosure0( f5 )
        t5.cancel()
        function f5w = function()
        {
            Coroutine.awaitTask( t5 )
            ret 1
        }
        Task w5 = Coroutine.spawnClosure0( f5w )
        bool ok5 = Coroutine.waitTimeout( w5, 2000 )
        if ok5 == false
        {
            w5.cancel()
        }
        ServerProbeTest.check( "B5 awaitTask on cancelled task returns", ok5 )

        # B6 spawnClosure2 双参 handler 派发 + 等待（引擎派发路径；
        #    handler 内 delay 即挂起，验证派发协程独立调度）
        function f6 = function( Int32 a, Int32 b )
        {
            Coroutine.delay( 30 )
            ret a + b
        }
        Task t6 = Coroutine.spawnClosure2( f6, 20, 22 )
        Int32 v6 = Coroutine.awaitTask( t6 ) as Int32
        ServerProbeTest.check( "B6 spawnClosure2 dispatch + await", v6 == 42 )
    }

    static fun()
    {
        ServerProbeTest.probeClosures()
        ServerProbeTest.probeCoroutines()
    }
}
