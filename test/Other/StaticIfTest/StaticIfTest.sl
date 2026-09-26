StaticIfTest
{
    static fun()
    {
        global.println("===== static if compile-time test =====")

        # 1. 字符串宏比较：CompileBefore 已把 platform 改为 "Linux"，应命中 static elif 分支
        static if global.macro.platform == "Win32"
        {
            global.println("[1] platform branch -> Win32 (should NOT print)")
        }
        static elif global.macro.platform == "Linux"
        {
            global.println("[1] platform branch -> Linux")
        }
        static else
        {
            global.println("[1] platform branch -> Other (should NOT print)")
        }

        # 2. 布尔宏直接引用
        static if global.macro.useFastMath
        {
            global.println("[2] useFastMath -> true")
        }

        static if !global.macro.useFastMath
        {
            global.println("[2] useFastMath -> false (should NOT print)")
        }

        # 3. 数值宏比较
        static if global.macro.maxThreads > 4
        {
            global.println("[3] maxThreads > 4")
        }
        static else
        {
            global.println("[3] maxThreads <= 4 (should NOT print)")
        }

        # 4. 逻辑运算组合
        static if global.macro.useFastMath && global.macro.maxThreads > 4
        {
            global.println("[4] useFastMath && maxThreads > 4")
        }

        # 5. 嵌套 static if
        static if global.macro.platform == "Linux"
        {
            static if global.macro.maxThreads == 8
            {
                global.println("[5] nested : Linux + maxThreads == 8")
            }
            static else
            {
                global.println("[5] nested : Linux + maxThreads != 8 (should NOT print)")
            }
        }

        # 6. static if 与普通 if 混用：静态分支选中后，内部 runtime if 正常执行
        int a = 10
        static if global.macro.platform == "Linux"
        {
            if a > 5
            {
                global.println("[6] runtime if inside static if -> a > 5")
            }
            else
            {
                global.println("[6] runtime if inside static if -> a <= 5")
            }
        }

        # 7. global.data 注入成员访问（jsonc 新结构 global.data）
        global.println("[7] data greeting = " + global.greeting)
        global.println("[7] data baseCount = " + global.baseCount.toString())
    }
}
