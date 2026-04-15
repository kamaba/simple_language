ObjectTest
{
    static fun()
    {        
        global.println("========== Object.sl tests (start) ==========")
        Object obj = new()
        Object obj2 = obj
        Object obj3 = new()
        Object obj4 = new()

        global.println("[1] new + alias: obj==obj2 (same reference) -> " + (obj == obj2).toString())
        global.println("[2] equals alias: obj.equals(obj2) -> " + obj.equals(obj2).toString())
        global.println("[3] static objectEquals alias: Object.objectEquals(obj, obj2) -> " + Object.objectEquals(obj, obj2).toString())
        global.println("[4] static refEquals alias: Object.refEquals(obj, obj2) -> " + Object.refEquals(obj, obj2).toString())

        global.println("[5] distinct instances: Object.objectEquals(obj3, obj4) -> " + Object.objectEquals(obj3, obj4).toString())
        global.println("[6] distinct refEquals: Object.refEquals(obj3, obj4) -> " + Object.refEquals(obj3, obj4).toString())
        global.println("[7] equals distinct: obj3.equals(obj4) -> " + obj3.equals(obj4).toString())

        int hc1 = obj.hashCode
        int hc2 = obj3.hashCode
        global.println("[8] hashCode: obj.hashCode -> " + hc1.toString() + " ; obj3.hashCode -> " + hc2.toString())
        

        global.println("[11] toString: obj.toString() -> " + obj.toString())
        global.println("[12] toString: obj3.toString() -> " + obj3.toString())

        int refc = obj2.refCount
        global.println("[13] refCount (alias obj2): " + refc.toString())

        global.println("[14] refEquals(null,null) -> " + Object.refEquals(null, null).toString())
        global.println("[15] objectEquals(null,null) -> " + Object.objectEquals(null, null).toString())
        global.println("[16] objectEquals(obj,null) / objectEquals(null,obj) -> " + Object.objectEquals(obj, null).toString() + " / " + Object.objectEquals(null, obj).toString())

        rwA = obj.refWeak
        rwB = obj3.refWeak
        global.println("[17] refWeak: Object.refEquals(obj.refWeak, obj3.refWeak) -> " + Object.refEquals(rwA, rwB).toString())

        global.println("[18] lifecycle smoke: call free() then release() on a throwaway Object")
        Object tmp = new()
        global.println("    tmp.hashCode before -> " + tmp.hashCode.toString())
        tmp.free()
        global.println("    called tmp.free()")
        tmp.release()
        global.println("    called tmp.release()")
        global.println("========== Object.sl tests (end) ==========")     
    }
}

# 测试用例说明：Object 引用相等（==）、equals、静态 objectEquals/refEquals、hashCode、toString、refCount、refWeak、null 对、free/release 生命周期。
# 预期：别名对 [1]–[4] 为 true；不同实例 objectEquals 一般为 false、refEquals 为 false；refEquals(null,null) 等语义以运行时为准；free/release 后不崩溃。

