# LocalTest1.sl
# 用于测试 local{} 的：
# 1) 只能在 import 后、namespace/class 前
# 2) local{} 中的变量/函数只在当前文件可见
# 3) local{} 的执行顺序按工程编译文件顺序：LocalTest1 -> LocalTest2

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

    static fun()
    {
        global.println("========== LocalTest1 (start) ==========")
        LocalTest1.Test()
        global.println("========== LocalTest1 (end) ==========")
    }
}

# 运行入口：LocalTest1.fun 调用 Test()，验证本文件 local 块与 LocalTest2 文件隔离。
# 预期：local.a 累加后 Add(5) 与 PrintLocal 输出与实现一致；不访问他文件 local。

