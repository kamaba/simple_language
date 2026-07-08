import Std
import CSharp.System

Class1
{
}

BindCSharpTest
{
    static fun()
    {
        global.println("========== BindCSharpTest (start) ==========")
        global.println("面向：从 .sl 调用 CSharp.System 下的 Debug 等 CLR 绑定符号。")
        Debug.Write("test")
        CSharp.System.Debug.Write("Test2")
        System.Debug.Write("Test3")
        global.println("========== BindCSharpTest (end) ==========")
    }
}

# 测试面向：命名空间限定（Debug / CSharp.System / System）是否解析到同一宿主调试输出。
# 预期：三处 Write 均不报错；控制台可见 test / Test2 / Test3（顺序依实现）。
