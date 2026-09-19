
Std.Class1
{
    int a = 20
}

ImportTest
{
    static fun()
    {
        global.println("========== ImportTest1 (start) ==========")
        Class1 c1 = ()
        global.println("Std.Class1 实例化后 a=" + c1.a.toString())
        global.println("========== ImportTest1 (end) ==========")
    }
}

# import 是导入包与命名空间，只允许命名空间的导入
# 如果使用 然后在下边的代码中，通过import路径，可以在表达式中，或者是定义变量时，查找对应的类
#
# 测试说明：import Std 后通过短名 Class1 使用 Std.Class1，字段 a 默认 20。
# 预期：打印 a=20；无符号解析错误。

