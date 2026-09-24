import Std;

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

    static fun()
    {
        Console.println( "========== CSharpTest start ==========" )
        testIntRoundtrip()
        testStringRoundtrip()
        testMixedArgs()
        testMethodCache()
        testDegradation()
        Console.println( "========== CSharpTest end: passed=" + passed.toString() + " failed=" + failed.toString() + " ==========" )
    }
}
