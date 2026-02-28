# LocalTest1.sl
# 用于测试 local{} 的：
# 1) 只能在 import 后、namespace/class 前
# 2) local{} 中的变量/函数只在当前文件可见
# 3) local{} 的执行顺序按工程编译文件顺序：LocalTest1 -> LocalTest2

import Core.Debug;

local
{
    # 变量定义 + 初始化
    a = 1

    # 依赖初始化顺序的累加测试
    order = "L1"

    Add(x)
    {
        return x + local.a
    }

    PrintLocal()
    {
        Debug.Write("LocalTest1 local.a=" + local.a)
        Debug.Write("LocalTest1 local.order=" + local.order)
    }
}

class LocalTest1
{
    static Test()
    {
        # 访问 local 变量
        local.a = local.a + 10

        # 调用 local 函数
        v = local.Add(5)
        Debug.Write("LocalTest1 v=" + v)

        local.PrintLocal()

        # 确认 local 只在本文件：这里不允许访问 LocalTest2 的 local
        # Debug.Write(local.db)  # LocalTest2 内部的 db 不可见
    }
}
