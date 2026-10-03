InputArgsTest
{
    # ---------- 统一断言辅助：cond 为 true 打印 OK，否则打印 FAIL ----------
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[InputArgsTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[InputArgsTest] " + name + " : FAIL" )
        }
    }

    # ---------- _inputArgs 系统集成测试 ----------
    # 运行方式一（无参数）：csimple_lang run InputArgsTest.module.json
    #   -> 验证 T1/T2：数组始终存在、无参数时为空数组
    # 运行方式二（带参数）：csimple_lang run InputArgsTest.module.json -- hello 42 world
    #   -> 验证 T1/T3/T4/T5：长度一致、内容按序一致、元素为字符串可拼接
    static fun()
    {
        global.println( "========== InputArgsTest (start) ==========" )

        # T1: 系统注入的静态成员始终存在（前端自动生成，无参数时也是空数组而非 null）
        inputArgs = global._inputArgs
        check( "T1 global._inputArgs 始终非 null", inputArgs != null )

        argCount = inputArgs.length
        global.println( "  (本次运行参数个数: " + argCount.toString() + ")" )

        if argCount == 0
        {
            # T2: 无参数运行 -> 空数组
            check( "T2 无参数时 length == 0", argCount == 0 )
        }
        else
        {
            # T3: 有参数时元素个数与传入一致（约定传 3 个：-- hello 42 world）
            check( "T3 有参数时 length == 3", argCount == 3 )

            # T4: 元素内容按传入顺序一致（Object 槽取出后 toString 比较）
            check( "T4[0] 内容为 hello", inputArgs[0].toString() == "hello" )
            check( "T4[1] 内容为 42", inputArgs[1].toString() == "42" )
            check( "T4[2] 内容为 world", inputArgs[2].toString() == "world" )

            # T5: 元素为字符串，可直接参与拼接（隐式 toString / 装箱归一化链路）
            global.println( "  遍历拼接: [0]=" + inputArgs[0] + " [1]=" + inputArgs[1] + " [2]=" + inputArgs[2] )
            check( "T5 元素可直接字符串拼接", ( "" + inputArgs[0] ) == "hello" )

            # 遍历打印（顺序无关断言，任意参数个数均可观察）
            i = 0
            while i < argCount
            {
                global.println( "  global._inputArgs[" + i.toString() + "] -> " + inputArgs[i].toString() )
                i = i + 1
            }
        }

        # T6: 与 jsonc data 注入的静态成员共存互不影响
        check( "T6 jsonc global.var1 共存正常", global.var1 == 12 )

        global.println( "========== InputArgsTest (end) ==========" )
    }
}

# 测试说明：
# 1) global._inputArgs 为系统集成的静态成员（Array<Object>，前端注入 Project 类，
#    C VM 在运行前用 CLI 程序参数填充），源码无需声明即可直接使用
# 2) 覆盖：非 null / 空数组 / 长度一致 / 内容一致 / 字符串拼接 / jsonc 成员共存
# 3) 详见同目录 ProjectTest.md
