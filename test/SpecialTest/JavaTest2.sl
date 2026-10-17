import Std;
import Core;

# ============================================================================
# JavaTest2 — java_hotspot 插件 callSL 通道端到端测试（P4，设计 §5.7）
#
# 链路（Java -> SL 回调方向，与 JavaTest.sl 的 SL -> Java 通道互补）：
# Java 块内 sl.SLChannel.call("类.方法", 实参...) → 插件 C 侧 RegisterNatives
# 绑定的 java_sl_sl_channel_call（jvm_bridge.c）→ 参数树 unmarshal →
# host->callSL（sl_plugin_host.c hostCallSL）→ 宿主 CVM 三级方法查找
# （FNV-1a(sig) 直查 -> full_id 线性 -> 末段名 + argc）→ 压实参 ->
# vm_execute_method_by_id -> 弹返回值 widen（整型族->INT / 浮点族->FLOAT /
# String->STRING 拷贝 / void->NULL）-> marshal 回 sl.SLLabelValue。
#
# sig 形态："类.方法"（如 "JavaTest2.HelperAdd"）与裸方法名（如 "HelperAdd"）
# 均可——三级回退末段兜底。实参出参当前均为标量四态（INT/FLOAT/STRING/
# NULL）；OBJECT 树第一版不进 callSL（宿主侧压栈/出参重建待后续项）。
#
# 覆盖：① int 出参 + 完整/裸名双 sig ② string 出参 ③ double 出参
# ④ void 方法返回 NULL 态 + 静态字段副作用 ⑤ 嵌套回归——callSL 触发含
# @java_hotspot 块的 SL 方法（块 B 在块 A 的 Java 执行段内再入 run_entry），
# 验证 s_callsl_lib/s_callsl_tid save/restore 成对恢复（块 B 完成后块 A 的
# 后续 callSL 仍指向正确上下文，设计 §5.7 嵌套场景）。
# 前置：本机 JDK 17（同 JavaTest.sl）；SL 层禁行内注释。
# ============================================================================

class JavaTest2
{
    # 断言计数（供 fun 尾部汇总）
    static int passed = 0
    static int failed = 0

    # void 方法副作用载体（testCallSLVoid 验证宿主侧真实执行）
    static string g_helperNote = ""

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

    # ---------------- 宿主侧被回调方法（callSL 目标） ----------------

    static int HelperAdd( int a, int b )
    {
        ret a + b
    }

    static string HelperGreet( string name )
    {
        ret "Hello, " + name + "!"
    }

    static double HelperArea( double w, double h )
    {
        ret w * h
    }

    static HelperLog( string msg )
    {
        g_helperNote = msg
    }

    # 嵌套用例目标：方法体内再进一个 @java_hotspot 块（块 B）——
    # 被块 A 的 callSL 触发时，run_entry 的 save/restore 保证块 B 完成后
    # 块 A 的 callSL 上下文（lib + 线程 id）正确恢复。
    static int HelperNested( int x )
    {
        int r = 0
        @java_hotspot()
        {
            long v <- $x
            $r <- v * 2;
        }
        ret r
    }

    # ---------------- ① int 出参 + 完整/裸名双 sig ----------------
    # 完整 sig "JavaTest2.HelperAdd" 与裸名 "HelperAdd" 走不同查找级，
    # 均应命中同一静态方法；int 返回经 widen -> INT -> asLong。
    static testCallSLInt()
    {
        Console.println( "===== JavaTest2.testCallSLInt =====" )
        int sum = 0
        @java_hotspot()
        {
            long s1 = sl.SLChannel.call( "JavaTest2.HelperAdd", sl.SLLabelValue.ofInt( 3 ), sl.SLLabelValue.ofInt( 4 ) ).asLong();
            long s2 = sl.SLChannel.call( "HelperAdd", sl.SLLabelValue.ofInt( 10 ), sl.SLLabelValue.ofInt( 20 ) ).asLong();
            $sum <- s1 + s2;
        }
        Console.println( "callSL int: 3+4 + 10+20 = " + sum.toString() )
        check( "callSL int: full sig 7 + bare sig 30 == 37", sum == 37 )
    }

    # ---------------- ② string 出参 ----------------
    # String 返回经宿主 base_malloc 拷贝回推（插件消费后 host->free）；
    # Java 侧 asString 后经块出通道 string 标记回传 SL。
    static testCallSLString()
    {
        Console.println( "===== JavaTest2.testCallSLString =====" )
        string greet = ""
        @java_hotspot()
        {
            String s = sl.SLChannel.call( "JavaTest2.HelperGreet", sl.SLLabelValue.ofString( "SL" ) ).asString();
            string $greet <- s;
        }
        Console.println( "callSL string: " + greet )
        check( "callSL string: HelperGreet(SL) == 'Hello, SL!'", greet == "Hello, SL!" )
    }

    # ---------------- ③ double 出参 ----------------
    # 浮点族返回 widen -> FLOAT -> asDouble；SL 侧 double 变量接收。
    static testCallSLDouble()
    {
        Console.println( "===== JavaTest2.testCallSLDouble =====" )
        double area = 0.0
        @java_hotspot()
        {
            double a = sl.SLChannel.call( "JavaTest2.HelperArea", sl.SLLabelValue.ofFloat( 2.5 ), sl.SLLabelValue.ofFloat( 4.0 ) ).asDouble();
            double $area <- a;
        }
        Console.println( "callSL double: 2.5 x 4.0 = " + area.toString() )
        check( "callSL double: HelperArea(2.5,4.0) == 10.0", area == 10.0 )
    }

    # ---------------- ④ void 方法返回 NULL 态 + 副作用 ----------------
    # void 方法不弹返回值 -> out NULL（hostCallSL return_count==0 分支）；
    # Java 侧 isNull 判定 + SL 静态字段被真实写入验证宿主执行到位。
    static testCallSLVoid()
    {
        Console.println( "===== JavaTest2.testCallSLVoid =====" )
        g_helperNote = ""
        int flag = 0
        @java_hotspot()
        {
            sl.SLChannel.call( "JavaTest2.HelperLog", sl.SLLabelValue.ofString( "from-java" ) );
            long ok = 0;
            if ( sl.SLChannel.call( "JavaTest2.HelperLog", sl.SLLabelValue.ofString( "from-java" ) ).isNull() ) { ok = 1; }
            $flag <- ok;
        }
        Console.println( "callSL void: null-ret = " + flag.toString() + " note = " + g_helperNote )
        check( "callSL void: return value is NULL", flag == 1 )
        check( "callSL void: host side effect visible (g_helperNote)", g_helperNote == "from-java" )
    }

    # ---------------- ⑤ 嵌套回归：callSL 触发含块的 SL 方法 ----------------
    # 块 A call HelperNested -> HelperNested 执行块 B（run_entry save/restore
    # 嵌套一层）-> ret 42 -> 回块 A；块 A 再 call HelperAdd 验证上下文已恢复
    # （s_callsl_lib 未被块 B 污染，save/restore 成对）。
    static testCallSLNested()
    {
        Console.println( "===== JavaTest2.testCallSLNested =====" )
        int total = 0
        @java_hotspot()
        {
            long n1 = sl.SLChannel.call( "JavaTest2.HelperNested", sl.SLLabelValue.ofInt( 21 ) ).asLong();
            long n2 = sl.SLChannel.call( "JavaTest2.HelperAdd", sl.SLLabelValue.ofInt( n1 ), sl.SLLabelValue.ofInt( 1 ) ).asLong();
            $total <- n2;
        }
        Console.println( "callSL nested: 21*2 + 1 = " + total.toString() )
        check( "callSL nested: HelperNested(21)==42 via inner block, then +1 == 43", total == 43 )
    }

    static fun()
    {
        Console.println( "========== JavaTest2 start ==========" )
        testCallSLInt()
        testCallSLString()
        testCallSLDouble()
        testCallSLVoid()
        testCallSLNested()
        Console.println( "========== JavaTest2 end: passed=" + passed.toString() + " failed=" + failed.toString() + " ==========" )
    }
}
