import Std;
import Core;

# ============================================================================
# JavaTest — java_hotspot 插件端到端测试（P3，HOTSPOT_JNI_INTEGRATION_DESIGN.md §8）
#
# 链路：@java_hotspot(){} 块 → Front 哨兵脱糖 CallAtSignLabel(124) → 导出期
# AtSignLabelBuildManager 转调插件 frontend（SLLabelParser 生成 Entry_N.java
# + SLLabelBuilder javac 合并编译部署 classes 目录树）→ CVM 经模块
# atSignLabel[] 绑定懒 resolve 到 sl_java_hotspot labelExec →
# jvm_bridge_run_entry（URLClassLoader 挂 classes → loadClass("SLAtSign.Entry_N")
# → GetStaticMethodID 按完整方法规格 → CallStatic*MethodA）→ 出通道五态
# （V/J/D/String/OBJECT 树镜像）编组回推。
#
# 通道类型路由（MapJavaType，§5.5）：整型族 → long（JNI J，int64 全域
# 无窄化）、浮点族 → double（D）、String 族 → String（Ljava/lang/String;）；
# Boolean 拒收；P4 起放行 Object/SL 类名 → sl.SLLabelValue 树镜像
# （Lsl/SLLabelValue;，读写双向递归，深度上限 32 同步）。entryMethod
# 完整方法规格由 Parse 期下发（如 "run:(JD)Ljava/lang/String;"），C 侧
# 零拼接（§12.7）。
#
# 四用法覆盖（§8）：① 无通道纯块 ② 出通道 Int ③ 入+出通道 String
# ④ 块内 throw 上抛 SL 异常 try-catch 捕获；附加：int64 全域往返（无窄化
# 决策回归锁）、double 通道、三类型混合入通道、异常后 JVM 会话存活
#（JVM 驻留策略 §5.2：异常经 bridge drain 不破坏会话，exit 不 DestroyJavaVM）。
# 前置：本机 JDK 17（javac 编译期 + jvm.dll 运行期）；无 JDK 时 Build 报
# kind=notFound Info 跳过（块运行期报错码），本用例按需在装机后执行。
# 注意：本文件 SL 层禁用行内注释（Lexer 单行 # 注释会吞换行，导致成员丢失）。
# ============================================================================

# OBJECT 树镜像往返载体（P4，§5.5 OBJECT 行）：普通 SL 类，两 int 字段
class JavaPoint
{
    int x = 0
    int y = 0
}

class JavaTest
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

    # ---------------- ① 无通道纯块（副作用型，System.out 直打进程控制台） ----------------
    # 无入无出 → run:()V → CallStaticVoidMethodA；块内局部计算不影响 SL 侧变量。
    static testPlainBlock()
    {
        Console.println( "===== JavaTest.testPlainBlock =====" )
        int keep = 42
        @java_hotspot()
        {
            System.out.println( "hello from HotSpot (plain block)" );
            long inner = 6 * 7;
            System.out.println( "inner = " + inner );
        }
        Console.println( "keep = " + keep.toString() )
        check( "plain block: run()V executed, outer variable untouched", keep == 42 )
    }

    # ---------------- ② 出通道 Int（块内 for 求和 1..10 → 55） ----------------
    # 出通道缺省类型标记 → long（J）；SL 侧 int 变量回写。
    static testIntOutChannel()
    {
        Console.println( "===== JavaTest.testIntOutChannel =====" )
        int count = 0
        @java_hotspot()
        {
            long total = 0;
            for ( long i = 1; i <= 10; i++ ) { total += i; }
            $count <- total;
        }
        Console.println( "sum(1..10) = " + count.toString() )
        check( "int out channel: for 1..10 == 55", count == 55 )
    }

    # ---------------- int64 全域往返（P0 无窄化决策回归锁，§5.5） ----------------
    # 4000000000 超 int32 max（2147483647）——若 C 侧误走 jint 窄化会回绕为负。
    static testInt64Roundtrip()
    {
        Console.println( "===== JavaTest.testInt64Roundtrip =====" )
        long big = 4000000000
        long halfVal = 0
        @java_hotspot()
        {
            long big <- $big
            $halfVal <- big / 2;
        }
        Console.println( "4000000000 / 2 = " + halfVal.toString() )
        check( "int64 full range: 4000000000 / 2 == 2000000000 (no jint narrowing)", halfVal == 2000000000 )
    }

    # ---------------- double 入+出通道（Math.sqrt 3-4-5 三角 → 5.0） ----------------
    # 浮点族 → double（D）；出通道显式 double 类型标记（double $hyp <- expr）。
    static testDoubleRoundtrip()
    {
        Console.println( "===== JavaTest.testDoubleRoundtrip =====" )
        double a = 3.0
        double b = 4.0
        double hyp = 0.0
        @java_hotspot()
        {
            double a <- $a
            double b <- $b
            double $hyp <- Math.sqrt( a * a + b * b );
        }
        Console.println( "hypot(3,4) = " + hyp.toString() )
        check( "double channels: Math.sqrt(3*3+4*4) == 5.0", hyp == 5.0 )
    }

    # ---------------- ③ 入+出通道 String（拼接回传，§8 用法三） ----------------
    # String 入通道（NewStringUTF）+ string 出通道（GetStringUTFChars 深拷贝
    # host->alloc 回推）；出通道带 slType 类型标记（string $slogan <- expr）。
    static testStringRoundtrip()
    {
        Console.println( "===== JavaTest.testStringRoundtrip =====" )
        string name = "SimpleLanguage"
        string slogan = ""
        @java_hotspot()
        {
            String name <- $name
            string $slogan <- "Hello, " + name + "!";
        }
        Console.println( "slogan = " + slogan )
        check( "string channels: roundtrip == 'Hello, SimpleLanguage!'", slogan == "Hello, SimpleLanguage!" )
    }

    # ---------------- 三类型混合入通道（long + double + String 同块） ----------------
    # 生成签名 run:(JDLjava/lang/String;)J，C 侧 marshal 按签名顺序编组；
    # target 形参名与 SL 变量名分离（base/weight/s vs n/weight/name）。
    static testMixedChannels()
    {
        Console.println( "===== JavaTest.testMixedChannels =====" )
        int n = 0
        double weight = 2.5
        string name = "HotSpot"
        @java_hotspot()
        {
            long base <- $n
            double w <- $weight
            String s <- $name
            $n <- base + (long)( w * 10.0 ) + s.length();
        }
        Console.println( "mixed = " + n.toString() )
        check( "mixed channels: 0 + (long)(2.5*10) + 'HotSpot'.length() == 32", n == 32 )
    }

    # ---------------- ④ 异常上抛（块内 throw → MANAGED_EXC -105 → try-catch 捕获） ----------------
    # JVM pending 异常经 bridge drain 为消息文本上抛（throw_error -105）→
    # VM 异常 → SL label{}try{}catch{} 捕获（求值栈恢复 BeginTry 快照）。
    static int javaThrow()
    {
        @java_hotspot()
        {
            throw new IllegalStateException( "boom" );
        }
        ret 0
    }

    static testThrow()
    {
        Console.println( "===== JavaTest.testThrow =====" )
        int caught = 0
        label javaThrowBlock
        {
            try javaThrow()
        }
        catch
        {
            caught = 1
        }
        check( "managed-exc: caught (MANAGED_EXC -105)", caught == 1 )

        # 异常后 JVM 会话存活（探针 5.5 同款回归）：pending 已 drain，
        # 同进程后续块调用不受污染（求值栈恢复，无实参残留）
        int after = 0
        @java_hotspot()
        {
            $after <- 20 + 22;
        }
        Console.println( "after-throw = " + after.toString() )
        check( "after-throw session alive: 20+22 == 42", after == 42 )
    }

    # ---------------- JVM 驻留复用（§5.2：多次块调用命中类/方法缓存） ----------------
    # 同进程第二个块起类缓存（dir|class）与方法缓存（dir|class|name:sig）命中，
    # 不重复 loadClass/GetStaticMethodID；块间值经 SL 变量接力。
    static testSessionReuse()
    {
        Console.println( "===== JavaTest.testSessionReuse =====" )
        int a = 0
        int b = 0
        @java_hotspot()
        {
            $a <- 40 + 2;
        }
        @java_hotspot()
        {
            long a <- $a
            $b <- a * 1;
        }
        Console.println( "reuse: a = " + a.toString() + " b = " + b.toString() )
        check( "session reuse: block1 40+2 == 42", a == 42 )
        check( "session reuse: block2 in+out echo == 42 (cache hit)", b == 42 )
    }

    # ---------------- ⑤ OBJECT 树镜像往返（P4，§5.5 OBJECT 行） ----------------
    # 入通道 SL 类名标记（JavaPoint pv <- $p）→ sl_atsign_marshal_object
    # 递归编组 SLLabelObject 树 → JNI 形参 sl.SLLabelValue（fieldByName/asLong
    # 读取）；出通道 sl.SLLabelValue.ofObject 构造树 → C 侧按树重建 JavaPoint
    # 实例（同名同型字段逐个回填）压 $q；深度上限 32 双向对称。
    static testObjectTree()
    {
        Console.println( "===== JavaTest.testObjectTree =====" )
        JavaPoint p = JavaPoint()
        p.x = 3
        p.y = 4
        JavaPoint q = JavaPoint()
        @java_hotspot()
        {
            JavaPoint pv <- $p
            long sx = pv.fieldByName( "x" ).asLong();
            long sy = pv.fieldByName( "y" ).asLong();
            JavaPoint $q <- sl.SLLabelValue.ofObject( "JavaPoint", new String[]{ "x", "y" }, new sl.SLLabelValue[]{ sl.SLLabelValue.ofInt( sx + sy ), sl.SLLabelValue.ofInt( sx * sy ) } );
        }
        Console.println( "objectTree: q.x = " + q.x.toString() + " q.y = " + q.y.toString() )
        check( "object tree: rebuilt q.x == 7 (3+4)", q.x == 7 )
        check( "object tree: rebuilt q.y == 12 (3*4)", q.y == 12 )
    }

    static fun()
    {
        Console.println( "========== JavaTest start ==========" )
        testPlainBlock()
        testIntOutChannel()
        testInt64Roundtrip()
        testDoubleRoundtrip()
        testStringRoundtrip()
        testMixedChannels()
        testThrow()
        testSessionReuse()
        testObjectTree()
        Console.println( "========== JavaTest end: passed=" + passed.toString() + " failed=" + failed.toString() + " ==========" )
    }
}
