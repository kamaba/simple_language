# ============================================================================
# WasmDemo.sl -- C VM WebAssembly 移植的浏览器示例
#
# 配套：csimple_lang/md/WASM_BUILD.md + csimple_lang/wasm/index.html
# 仅引用 Core 模块（容器/协程/系统方法都在 Core），保证浏览器侧
# systemCalls 注册面最小、demo 自包含。
# 语法对齐 BaseTest 用例（isSupportDoublePlus=false：不用 ++ / +=）。
# ============================================================================

class WasmGreeter
{
    string name = "SimpleLanguage"

    public string greet( string who )
    {
        ret "Hello, " + who + "! -- from " + this.name
    }
}

class WasmDemo
{
    public static int fib( int n )
    {
        if n < 2
        {
            ret n
        }
        ret WasmDemo.fib( n - 1 ) + WasmDemo.fib( n - 2 )
    }

    static void demoNumbers()
    {
        SystemPrintln( "--- 数值与递归 ---" )
        int r = WasmDemo.fib( 22 )
        SystemPrintln( "fib(22) = " + r.toString() )

        int sum = 0
        for i = 1, i <= 100, i = i + 1
        {
            sum = sum + i
        }
        SystemPrintln( "1+2+...+100 = " + sum.toString() )
    }

    static void demoStrings()
    {
        SystemPrintln( "--- 字符串与循环 ---" )
        string line = ""
        for j = 1, j <= 9, j = j + 1
        {
            int p = 9 * j
            line = line + "9x" + j.toString() + "=" + p.toString() + " "
        }
        SystemPrintln( line )
    }

    static void demoContainers()
    {
        SystemPrintln( "--- 容器 ---" )

        nums = list<int>()
        for i = 0, i < 10, i = i + 1
        {
            nums.add( i * i )
        }
        SystemPrintln( "squares: " + nums.toString() )

        words = map<int,string>()
        words.add( 1, "one" )
        words.add( 2, "two" )
        words.add( 3, "three" )
        SystemPrintln( "map length = " + words.length.toString() + ", map[2] = " + (words._getItem_(2) as string) )

        hs = hashset<int>()
        hs.add( 1 )
        hs.add( 2 )
        hs.add( 2 )
        SystemPrintln( "hashset length = " + hs.length.toString() + " (重复自动去重)" )
        SystemPrintln( "hashset: " + hs.toString() )
    }

    static void demoClass()
    {
        SystemPrintln( "--- 类与对象 ---" )
        WasmGreeter g = new()
        SystemPrintln( g.greet( "WebAssembly" ) )
    }

    static void demoCoroutine()
    {
        SystemPrintln( "--- 协程（VM 绿色线程，浏览器单线程可用）---" )
        function coroA = function( int n )
        {
            for i = 0, i < n, i = i + 1
            {
                SystemPrintln( "coro-A step " + i.toString() )
                Coroutine.yieldNow()
            }
            ret 0
        }
        function coroB = function( int n )
        {
            for i = 0, i < n, i = i + 1
            {
                SystemPrintln( "coro-B step " + i.toString() )
                Coroutine.yieldNow()
            }
            ret 0
        }
        Task ta = spawn coroA( 3 )
        Task tb = spawn coroB( 3 )
        Coroutine.waitAll2( ta, tb )
        SystemPrintln( "协程交替执行完成" )
    }

    static void runMain()
    {
        SystemPrintln( "===== SimpleLanguage @ WebAssembly =====" )
        WasmDemo.demoNumbers()
        WasmDemo.demoStrings()
        WasmDemo.demoContainers()
        WasmDemo.demoClass()
        WasmDemo.demoCoroutine()
        SystemPrintln( "===== WasmDemo end =====" )
    }
}
