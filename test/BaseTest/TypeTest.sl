import Std
import CSharp.System
        
typealias AliasIntArray = Array<int>
typealias AliasObjArray = Array<Object>

TypeTest
{
    ArrClass
    {
        int i = 0
    }

    Box<T>
    {
        T value = null

        _init_(T v)
        {
            this.value = v
        }

        string toString()
        {
            ret "Box(" + this.value.toString() + ")"
        }
    }

    static bool IsIntType(Type t)
    {
        ret t == int.type
    }

    static primitiveAliasTypeTest()
    {
        global.println("----- primitiveAliasTypeTest -----")

        tIntLower = int.type
        tIntUpper = Int32.type
        tFloatLower = float.type
        tFloatUpper = Float32.type

        global.println("int.type   -> " + tIntLower.toString())
        global.println("Int32.type -> " + tIntUpper.toString())
        global.println("float.type -> " + tFloatLower.toString())
        global.println("Float.type -> " + tFloatUpper.toString())

        global.println("int.type == Int32.type -> " + (tIntLower == tIntUpper).toString())
        global.println("float.type == Float.type -> " + (tFloatLower == tFloatUpper).toString())
        global.println("int.type == float.type -> " + (tIntLower == tFloatLower).toString())
    }

    static arrayAliasTypeTest()
    {
        global.println("----- arrayAliasTypeTest -----")

        tObjArrAlias = ObjectArray.type
        tObjArrRaw = Array<Object>.type
        tIntArrAlias = Int32Array.type
        tIntArrRaw = Array<Int32>.type
        tAliasIntArray = AliasIntArray.type
        tAliasObjArray = AliasObjArray.type

        global.println("ObjectArray.type -> " + tObjArrAlias.toString())
        global.println("Array<Object>.type -> " + tObjArrRaw.toString())
        global.println("Int32Array.type -> " + tIntArrAlias.toString())
        global.println("Array<Int32>.type -> " + tIntArrRaw.toString())
        global.println("AliasIntArray.type -> " + tAliasIntArray.toString())
        global.println("AliasObjArray.type -> " + tAliasObjArray.toString())

        global.println("ObjectArray == Array<Object> -> " + (tObjArrAlias == tObjArrRaw).toString())
        global.println("Int32Array == Array<Int32> -> " + (tIntArrAlias == tIntArrRaw).toString())
        global.println("ObjectArray == Int32Array -> " + (tObjArrAlias == tIntArrAlias).toString())
        global.println("AliasIntArray == Array<Int32> -> " + (tAliasIntArray == tIntArrRaw).toString())
        global.println("AliasObjArray == Array<Object> -> " + (tAliasObjArray == tObjArrRaw).toString())
    }

    static templateTypeTest()
    {
        global.println("----- templateTypeTest -----")

        tBoxInt = Box<int>.type
        tBoxInt2 = Box<Int32>.type
        tBoxStr = Box<string>.type
        tBoxObj = Box<Object>.type

        global.println("Box<int>.type -> " + tBoxInt.toString())
        global.println("Box<Int32>.type -> " + tBoxInt2.toString())
        global.println("Box<string>.type -> " + tBoxStr.toString())
        global.println("Box<Object>.type -> " + tBoxObj.toString())

        global.println("Box<int> == Box<Int32> -> " + (tBoxInt == tBoxInt2).toString())
        global.println("Box<int> == Box<string> -> " + (tBoxInt == tBoxStr).toString())
        global.println("Box<Object> == Object.type -> " + (tBoxObj == Object.type).toString())

        Box<int> bi = Box<int>(10)
        Box<string> bs = Box<string>("txt")
        global.println("instance bi.type == Box<int>.type -> " + (bi.type == tBoxInt).toString())
        global.println("instance bs.type == Box<string>.type -> " + (bs.type == tBoxStr).toString())
    }

    static inferredAndDynamicTypeTest()
    {
        global.println("----- inferredAndDynamicTypeTest -----")

        var vi = 123
        var vf = 1.5
        var vs = "hello"

        global.println("vi.type -> " + vi.type.toString())
        global.println("vf.type -> " + vf.type.toString())
        global.println("vs.type -> " + vs.type.toString())

        global.println("vi.type == int.type -> " + (vi.type == int.type).toString())
        global.println("vf.type == float.type -> " + (vf.type == float.type).toString())
        global.println("vs.type == string.type -> " + (vs.type == string.type).toString())
        
        data dyn = { a = 10, b = "ok", c = ArrClass(){ i = 7 } }
        tDyn = dyn.type
        global.println("dynamic value .type -> " + tDyn.toString())
        global.println("dynamic self type compare -> " + (tDyn == dyn.type).toString())
        global.println("dyn.c.type == ArrClass.type -> " + (dyn.c.type == ArrClass.type).toString())
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

        primitiveAliasTypeTest()
        arrayAliasTypeTest()
        templateTypeTest()
        inferredAndDynamicTypeTest()

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
