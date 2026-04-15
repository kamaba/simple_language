import Std;

namespace Std.Math

partial N1.N2.N3.ClassN1_2_3_1
{
    X1 = 100;
}

ClassG1
{

}
Std.Class_std1
{

}

namespace N1
{
    namespace N2.N3
    {
        Class1_2_3_1
        {
            Class1_2_3_11_1
            {

            }
        }
        namespace N4
        {
            namespace N5
            {
                ClassN1_2_3_4_3 extends N2.N3.Class1_2_3_1.Class1_2_3_11_1
                {

                }
            }
        }
    }
    ClassN1_1
    {

    }
}

Class1TestSmoke
{
    static fun()
    {
        global.println("========== Class1Test (start) ==========")
        global.println("本文件为 partial、Std 前缀类、多级 namespace 与 extends 链的声明样例，无运行时断言。")
        global.println("========== Class1Test (end) ==========")
    }
}

# 测试面向：namespace 嵌套（N2.N3 / N4.N5）、partial 与 extends 的符号组织；不含业务 static fun 直至 Class1TestSmoke。
# 预期：仅编译/索引 smoke；具体类型解析依赖 Std 与 N1.N2.N3 路径配置。

