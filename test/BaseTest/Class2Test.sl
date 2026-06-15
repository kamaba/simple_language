import Math
import Std

namespace Application.Cat;

namespace Class1Expand
{

    partial ClassDefineBase
    {
        partialValue = 20

        sumBase()
        {
            ret this.baseValue + this.partialValue
        }
    }
}
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
            X54 = 100;

            sumX()
            {
                ret this.X41 + this.X42 + this.X52 + this.X53 + this.X54
            }
        }
    }

    Class6 extends N3.Class5
    {
        X61 = 6

        _init_(int value)
        {
            this.X61 = value
        }

        string name()
        {
            ret "Class6:" + this.X61.toString()
        }

        describe()
        {
            ret this.name() + ":" + this.sumX().toString()
        }

        Class6Inner
        {
            innerValue = 61
        }
    }
}

namespace N1.N4
{
    Class7 extends N1.Class6
    {
        X71 = 7

        _init_(int value, int x71)
        {
            base._init_(value)
            this.X71 = x71
        }

        total()
        {
            ret this.X61 + this.X71 + this.sumX()
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
        global.println("N1.N3.Class5 sumX -> " + c5.sumX().toString())
        c6 = N1.Class6(66)
        inner = N1.Class6.Class6Inner()
        global.println("N1.Class6 describe -> " + c6.describe())
        global.println("N1.Class6 innerValue -> " + inner.innerValue.toString())
        c7 = N1.N4.Class7(70, 71)
        global.println("N1.N4.Class7 total -> " + c7.total().toString())
        global.println("========== Class2Test (end) ==========")
    }
}

# 测试面向：Application 引入后 N1 下 partial、多级 namespace、继承链 Class5→Class4→N2.N3.Class3。
# 扩展覆盖：继承链新增方法、跨 namespace 继承 N1.N4.Class7→N1.Class6、构造 base._init_、嵌套类实例化与成员默认值读取。
# 预期：Class5 实例化成功，X52/X53/X54 等成员为类体默认值 100；sumX=500，Class6 describe=Class6:66:500，Class7 total=641；依赖 Application 模块解析。
