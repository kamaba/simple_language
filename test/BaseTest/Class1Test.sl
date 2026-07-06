import Std;

#namespace Std.Math

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

namespace Class1Expand
{
    partial ClassDefineBase
    {
        public static int staticSeed = 7
        int baseValue = 10

        _init_(int value)
        {
            this.baseValue = value
        }

        int get baseProp()
        {
            ret this.baseValue
        }

        virtualName()
        {
            ret "base:" + this.baseValue.toString()
        }

        ClassDefineNested
        {
            nestedValue = 31
        }
    }

    ClassDefineChild extends ClassDefineBase
    {
        childValue = 30

        _init_(int value, int child)
        {
            base._init_(value)
            this.childValue = child
        }

        override virtualName()
        {
            ret "virtualName baseValue:" + this.baseValue.toString() + "  childValue:" + this.childValue.toString()
        }

        int sumAll()
        {
            ret int(this.sumBase()) + this.childValue
        }
    }
}

Class1TestSmoke
{
    static fun()
    {
        global.println("========== Class1Test (start) ==========")
        global.println("本文件为 partial、Std 前缀类、多级 namespace 与 extends 链的声明样例，无运行时断言。")
        c1 = Class1Expand.ClassDefineChild(11, 22)
        nested = Class1Expand.ClassDefineBase.ClassDefineNested()
        global.println("Class1Expand child virtualName -> " + c1.virtualName())
        global.println("Class1Expand child sumAll -> " + c1.sumAll().toString())
        global.println("Class1Expand baseProp/static/nested -> " + c1.baseProp.toString() + "/" + Class1Expand.ClassDefineBase.staticSeed.toString() + "/" + nested.nestedValue.toString())
        global.println("========== Class1Test (end) ==========")
    }
}

# 测试面向：namespace 嵌套（N2.N3 / N4.N5）、partial 与 extends 的符号组织；扩展覆盖类构造、base._init_、override、getter、static 成员与嵌套类实例化。
# 预期：原有声明继续编译；Class1Expand.ClassDefineChild 可实例化并打印 child:11:22、sumAll=53、baseProp/static/nested=11/7/31。
