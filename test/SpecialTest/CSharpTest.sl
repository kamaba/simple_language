import Std;
import Core;

# ============================================================================
# CSharpTest — csharp_mono 插件端到端测试（P1-f）
#
# 链路：SL systemCall（CSharpCallInt/CSharpCallString，ProjectTest.jsonc
# systemCalls 段）→ CVM plugin registry 装配期注册 → csharp_exec 栈协议
# （cvm_csharp_mono.c）→ mono_runtime_invoke → 返回编组回推。
# 设计文档：csimple_lang/md/design/MONO_INTEGRATION_DESIGN.md（§4.3）
#          csimple_lang/md/design/PLUGIN_SYSTEM_DESIGN.md（§10.1/§11）
# 栈协议：CSharpCallXxx(asm, ns, class, method, ret_kind, 实参...)；
#         asm 裸文件名经插件 dll 同目录兜底解析（P1-e 修复）。
# 测试程序集：SLPlugin/csharp_mono/probe/SLCSharpTestLib.cs（net4x）。
# 降级负向（P1-f-2/P1-f-3）：插件侧失败经 V8 host->throw_error 上抛 VM 异常
# （vm_sys_throw 置码 + try 栈展开，求值栈恢复到 BeginTry 快照，实参不残留），
# SL 侧 label{}try{}catch{} 捕获。码值：NOT_FOUND=-93（asm/类/方法解析失败
# 均走 mono_bridge_find_static_method）、MANAGED_EXC=-95（invoke 抛 managed 异常）。
# 注意：本文件禁用行内注释（Lexer 单行 # 注释会吞换行，导致成员丢失）
# ============================================================================

class CSharpTest
{
    # 断言计数（供 fun 尾部汇总）
    static int passed = 0
    static int failed = 0

    # 简单断言辅助：打印 PASS / FAIL
    static check( string name, bool cond )
    {
        if ( cond )
        {
            passed = passed + 1
            Console.println( "  [PASS] " + name )
        }
        else
        {
            failed = failed + 1
            Console.println( "  [FAIL] " + name )
        }
    }

    # ---------------- int 往返（ret_kind=1：unbox → slvm_push_i32） ----------------
    static testIntRoundtrip()
    {
        Console.println( "===== CSharpTest.testIntRoundtrip =====" )
        int r1 = CSharpCallInt( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "Add", 1, 40, 2 )
        check( "Add(40,2) == 42", r1 == 42 )
        int r2 = CSharpCallInt( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "Add", 1, -5, 7 )
        check( "Add(-5,7) == 2", r2 == 2 )
        int r3 = CSharpCallInt( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "Add", 1, 0, 0 )
        check( "Add(0,0) == 0", r3 == 0 )
    }

    # ---------------- string 往返（ret_kind=2：宿主 V7 push_string） ----------------
    static testStringRoundtrip()
    {
        Console.println( "===== CSharpTest.testStringRoundtrip =====" )
        string s1 = CSharpCallString( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "Greet", 2, "mono" )
        check( "Greet(mono) == 'hello, mono from mono'", s1 == "hello, mono from mono" )
        string s2 = CSharpCallString( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "Greet", 2, "sl" )
        check( "Greet(sl) == 'hello, sl from mono'", s2 == "hello, sl from mono" )
    }

    # ---------------- 混合实参（string 槽 + int 槽同调用混压） ----------------
    static testMixedArgs()
    {
        Console.println( "===== CSharpTest.testMixedArgs =====" )
        string t1 = CSharpCallString( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "Tag", 2, "x", 7 )
        check( "Tag(x,7) == 'x:7'", t1 == "x:7" )
        string t2 = CSharpCallString( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "Tag", 2, "n", -3 )
        check( "Tag(n,-3) == 'n:-3'", t2 == "n:-3" )
    }

    # ---------------- 方法缓存幂等（§4.3 方式 A：重复调用命中缓存） ----------------
    static testMethodCache()
    {
        Console.println( "===== CSharpTest.testMethodCache =====" )
        int a1 = CSharpCallInt( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "Add", 1, 1, 2 )
        int a2 = CSharpCallInt( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "Add", 1, 10, 20 )
        check( "Add 缓存重入: 1+2 == 3", a1 == 3 )
        check( "Add 缓存重入: 10+20 == 30", a2 == 30 )
        # 同类不同方法交替（缓存键含 method 名，须互不串扰）
        string g = CSharpCallString( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "Greet", 2, "mix" )
        int a3 = CSharpCallInt( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "Add", 1, 5, 6 )
        check( "Add/Greet 交替后 Greet 正确", g == "hello, mix from mono" )
        check( "Add/Greet 交替后 Add 正确", a3 == 11 )
    }

    # ---------------- 降级负向（P1-f-3：失败上抛 VM 异常 → try-catch 捕获） ----------------
    # 每组：label 块内 try 裸调用（返回值丢弃），catch 兜底置 caught；
    # asm/类/方法不存在均命中 mono_bridge_find_static_method 失败（NOT_FOUND -93），
    # ThrowAlways 为 probe 库注入的 managed 异常（MANAGED_EXC -95）。
    static testDegradation()
    {
        Console.println( "===== CSharpTest.testDegradation =====" )

        int caught = 0

        caught = 0
        label noAsmBlock
        {
            try CSharpCallInt( "NoSuchAsm.dll", "SLCSharp", "MathUtil", "Add", 1, 1, 2 )
        }
        catch
        {
            caught = 1
        }
        check( "no-asm: caught (NOT_FOUND -93)", caught == 1 )

        caught = 0
        label noClassBlock
        {
            try CSharpCallInt( "SLCSharpTestLib.dll", "SLCSharp", "NoSuchClass", "Add", 1, 1, 2 )
        }
        catch
        {
            caught = 1
        }
        check( "no-class: caught (NOT_FOUND -93)", caught == 1 )

        caught = 0
        label noMethodBlock
        {
            try CSharpCallInt( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "NoSuchMethod", 1, 1, 2 )
        }
        catch
        {
            caught = 1
        }
        check( "no-method: caught (NOT_FOUND -93)", caught == 1 )

        caught = 0
        label excBlock
        {
            try CSharpCallInt( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "ThrowAlways", 1, 1, 2 )
        }
        catch
        {
            caught = 1
        }
        check( "managed-exc: caught (MANAGED_EXC -95)", caught == 1 )

        # 捕获后求值栈已恢复（BeginTry 快照回滚），正向调用不受残留实参污染
        # （P1-e 复盘症状「VM 不清参致实参残留」的端到端回归锁）
        int ok = CSharpCallInt( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "Add", 1, 2, 3 )
        check( "after-catch stack clean: Add(2,3) == 5", ok == 5 )
    }

    # ---------------- @csharp_mono(){} 内联块（通道脱糖 → csc 融合 SLAtSign.dll） ----------------
    # 块体行分类（由 csharp_mono 插件 frontendLibs 解析器 SLCSharpMonoFrontend 判定）：
    # var x <- $x 入通道（$ 引外层 SL 变量）；import NS; 提升 using；
    # 其余为 C# 中段原样保留；$x <- expr 出通道（ret_kind=1 取 Main 返回值写回）。
    # Front 只圈地（括号/块配平+原文截取）并按插件解析结果脱糖为裸赋值
    #   c = CSharpCallInt( "SLAtSign.dll", "SLAtSign", "Entry_N", "Main", 1, a, b )
    # 导出期 CscAtSignBuildManager 用 .NET csc 把块体（含 Main 包装）编译为
    # SLAtSign.dll 部署到 csharp_mono 插件 lib 目录；运行期复用上面 CSharpCallInt 的 mono 链路。
    static testAtSignMono()
    {
        Console.println( "===== CSharpTest.testAtSignMono =====" )
        int a = 30
        int b = 12
        int c = 0
        @csharp_mono()
        {
            var a <- $a
            var b <- $b
            import SLCSharp;
            var c = MathUtil.Add( a, b );
            $c <- c;
        }
        check( "at-sign mono: 30+12 == 42", c == 42 )

        # 无出通道块（中段副作用 + ret_kind=0 CSharpCallVoid）
        int d = 0
        d = CSharpCallInt( "SLCSharpTestLib.dll", "SLCSharp", "MathUtil", "Add", 1, 100, 23 )
        @csharp_mono(){
            var x <- $d
            import SLCSharp;
            var r = MathUtil.Add( x, 1 );
        }
        check( "at-sign mono: void block keeps outer d", d == 123 )
    }

    # ---------------- 协程内 @csharp_mono(){}（spawn + await 往返） ----------------
    # 协程体即普通静态方法：块脱糖为 CSharpCallInt 后在协程私有帧上同步执行
    # （协作式单线程调度，mono 调用不含让出点，天然无重入）；外层 spawn 拿
    # Task 句柄、await 取回 ret 值。
    static int coroMonoAdd( int a, int b )
    {
        int c = 0
        @csharp_mono(){
            var a <- $a
            var b <- $b
            import SLCSharp;
            var c = MathUtil.Add( a, b );
            $c <- c;
        }
        ret c
    }

    static testAtSignCoro()
    {
        Console.println( "===== CSharpTest.testAtSignCoro =====" )
        int x = 20
        int y = 22
        # 裸方法名不是函数值（无方法组转换），经包装闭包转发
        function addFn = function( int a, int b )
        {
            ret coroMonoAdd( a, b )
        }
        Task h = spawn addFn( x, y )
        int r = await h as int
        check( "at-sign coro: spawn+await mono 20+22 == 42", r == 42 )
    }

    # ---------------- isolate 线程内 @csharp_mono(){}（Isolate.run 返回结果） ----------------
    # P2 (1:1) 线程模型：worker isolate 独占一条 OS 线程真并行执行；Isolate.run
    # 一次性计算语义（同步等待 worker 结束、深拷贝回传返回值）。块脱糖后的
    # CSharpCallInt 在 worker 线程上经插件 mono 链路执行。
    static int isoMonoAdd( int a, int b )
    {
        int c = 0
        @csharp_mono(){
            var a <- $a
            var b <- $b
            import SLCSharp;
            var c = MathUtil.Add( a, b );
            $c <- c;
        }
        ret c
    }

    static testAtSignIsolate()
    {
        Console.println( "===== CSharpTest.testAtSignIsolate =====" )
        function fn = function( int a, int b )
        {
            ret isoMonoAdd( a, b )
        }
        object r = Isolate.run( fn( 15, 27 ) )
        int n = r as int
        check( "at-sign isolate: Isolate.run mono 15+27 == 42", n == 42 )
    }

    # ---------------- 中段字面量 '<-' 豁免（字符串/行注释内的 <- 不算通道） ----------------
    # 中段三态扫描（SLCSharpMonoFrontend.MiddleScanner，C# 词法感知）：
    # 字符串/字符字面量/行注释/块注释内的 <- 回传 exemptArrows 豁免段号，
    # Front 朴素复核跳过——只有代码区 <- 才报 20055。
    # "a <- b" 长度 6 + 注释豁免行 u=7 → 出通道 13。
    static testAtSignExemptArrow()
    {
        Console.println( "===== CSharpTest.testAtSignExemptArrow =====" )
        int r = 0
        @csharp_mono()
        {
            var s = "a <- b";
            int u = 7;   // <- line-comment arrow is exempt, not a channel
            $r <- s.Length + u;
        }
        check( "at-sign exempt: literal/comment '<-' skipped (6+7)", r == 13 )
    }

    static fun()
    {
        Console.println( "========== CSharpTest start ==========" )
        testIntRoundtrip()
        testStringRoundtrip()
        testMixedArgs()
        testMethodCache()
        testDegradation()
        testAtSignMono()
        testAtSignCoro()
        testAtSignIsolate()
        testAtSignExemptArrow()
        Console.println( "========== CSharpTest end: passed=" + passed.toString() + " failed=" + failed.toString() + " ==========" )
    }
}
