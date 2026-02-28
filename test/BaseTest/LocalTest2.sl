# LocalTest2.sl
# 用于测试 local{} 的：
# 1) 同名变量/函数在不同文件可以重复定义，不冲突
# 2) local{} 执行顺序在 LocalTest1 之后
# 3) local.xxx 绑定的是当前文件自己的 local instance

import Core.Debug;

local
{
    # 与 LocalTest1 重复定义同名变量 a，应该互不影响
    a = 100

    # 顺序标记
    order = "L2"

    int Add(x)
    {
        ret x + local.a
    }

    PrintLocal()
    {
        Debug.Write("LocalTest2 local.a=" + local.a)
        Debug.Write("LocalTest2 local.order=" + local.order)
    }
}

class LocalTest2
{
    static Test()
    {
        # 修改本文件 local.a，不应影响 LocalTest1 的 local.a
        local.a = local.a + 1

        v = local.Add(5)
        Debug.Write("LocalTest2 v=" + v)

        local.PrintLocal()
    }
}
