import Std
import CSharp.System

TypeTest
{
    ArrClass
    {
        int i = 0
    }

    static bool IsIntType(Type t)
    {
        ret t == int.type
    }

    static fun()
    {
        global.println("========== TypeTest (start) ==========")

        tPrim = int.type()
        int i2 = 20
        tInst = i2.type
        global.println("primitive int.type() -> " + tPrim.toString())
        global.println("instance i2.type -> " + tInst.toString())

        if tPrim == tInst
        {
            global.println("int.type() == 20.type : true")
        }
        else
        {
            global.println("int.type() == 20.type : false")
        }

        tFloat = float.type()
        global.println("float.type() -> " + tFloat.toString())

        if tFloat == tInst
        {
            global.println("float.type == int instance.type : true")
        }
        else
        {
            global.println("float.type == int instance.type : false (expected)")
        }

        tgArrInt = Array<int>.type
        global.println("Array<int>.type -> " + tgArrInt.toString())

        tgArrStr = Array<string>.type
        if tgArrInt == tgArrStr
        {
            global.println("Array<int>.type == Array<string>.type : true")
        }
        else
        {
            global.println("Array<int>.type == Array<string>.type : false (expected)")
        }

        bool arrIsInt = IsIntType(ArrClass.type)
        global.println("IsIntType(ArrClass.type) -> " + arrIsInt.toString())

        Object obj3 = new()
        tObj = obj3.type
        global.println("new() instance .type -> " + tObj.toString())

        global.println("========== TypeTest (end) ==========")
    }
}

# 测试用例说明：
# - int.type() 与整型字面量实例的 .type：原始类型与实例反射应指向同一逻辑类型。
# - float.type() 与 int 实例 .type：不同类型，比较应为 false。
# - Array<int>.type 与 Array<string>.type：不同元素类型的数组类型元数据应不相等。
# - ArrClass.type 传给 IsIntType：自定义类类型不是 int，应为 false。
# - Object 实例的 .type：应为 Object（或运行时映射名），用于对象与元类型 smoke。
#
# 预期结果（人工对照输出）：
# - 多处 “true/false” 与上表一致；不应出现未定义变量或重复声明错误。
# - 泛型开放/闭合类型元数据（如 Level<T>.type）可在单独模板测试文件中覆盖。
