# LocalTest2.sl
# 用于测试 local{} 的：
# 1) 同名变量/函数在不同文件可以重复定义，不冲突
# 2) local{} 执行顺序在 LocalTest1 之后
# 3) local.xxx 绑定的是当前文件自己的 local instance

local
{
    # 与 LocalTest1 重复定义同名变量 a，应该互不影响
    a = 100

    # 顺序标记
    order = "L2"

    int Add(int x)
    {
        ret x + local.a
    }

    PrintLocal()
    {
        global.println("LocalTest2 local.a=" + local.a)
        global.println("LocalTest2 local.order=" + local.order)
    }
}

class LocalTest2
{
    static Test()
    {
        # 修改本文件 local.a，不应影响 LocalTest1 的 local.a
        local.a = local.a + 1

        v = local.Add(5)
        global.println("LocalTest2 v=" + v)

        local.PrintLocal()
    }

    static fun()
    {
        global.println("========== LocalTest2 (start) ==========")
        LocalTest2.Test()
        global.println("========== LocalTest2 (end) ==========")
    }
}

# 运行入口：与 LocalTest1 成对，验证同名 local.a / Add 互不污染。
# 预期：本文件 a 从 100 递增；v=local.Add(5) 使用本文件 a；order 标记为 L2。
