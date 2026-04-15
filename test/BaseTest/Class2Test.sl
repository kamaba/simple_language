import Application
import Std

namespace N1
{
    N2.Class1
    {

    }
    namespace N2.N3
    {
        partial Class3 extends Class1
        {
            X2 = 100;
        }
    }

    Class4 extends N2.N3.Class3
    {
        X41 = 100;
        X42 = 100;
    }
    namespace N3
    {
        Class5 extends Class4
        {
            X52 = 100;
            X53 = 100;
        }
    }
}

Class2TestSmoke
{
    static fun()
    {
        global.println("========== Class2Test (start) ==========")
        c5 = N1.N3.Class5()
        global.println("N1.N3.Class5 X52 -> " + c5.X52.toString())
        global.println("========== Class2Test (end) ==========")
    }
}

# 测试面向：Application 引入后 N1 下 partial、多级 namespace、继承链 Class5→Class4→N2.N3.Class3。
# 预期：Class5 实例化成功，X52/X53 等成员为类体默认值 100；依赖 Application 模块解析。
