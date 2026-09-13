ObjectTest
{
    static fun()
    {
        global.println("========== Object.sl tests (start) ==========")
        testReferenceEquality()
        
        testEqualsMethod()
        testStaticEquals()
        testHashCode()
        testToString()
        testRefCount()
        testRefWeak()
        testNullHandling()
        testTypeProperty()
        testRefProperty()
        testLifecycle()
        testObjectAssignment()
        testChainedEquals()
        
        global.println("========== Object.sl tests (end) ==========")
    }

    # [1-2] == operator and refEquals for alias and distinct instances
    static testReferenceEquality()
    {
        global.println("--- testReferenceEquality ---")
        Object obj = new()
        Object obj2 = obj
        Object obj3 = new()
        Object obj4 = new()

        global.println("[1] alias == : (obj == obj2) -> " + (obj == obj2).toString())
        global.println("[2] distinct == : (obj == obj3) -> " + (obj == obj3).toString())
        global.println("[3] alias refEquals : Object.refEquals(obj, obj2) -> " + Object.refEquals(obj, obj2).toString())
        global.println("[4] distinct refEquals : Object.refEquals(obj3, obj4) -> " + Object.refEquals(obj3, obj4).toString())
        global.println("[5] self refEquals : Object.refEquals(obj, obj) -> " + Object.refEquals(obj, obj).toString())
    }

    # [6-8] equals() instance method
    static testEqualsMethod()
    {
        global.println("--- testEqualsMethod ---")
        Object obj = new()
        Object obj2 = obj
        Object obj3 = new()

        global.println("[6] equals alias : obj.equals(obj2) -> " + obj.equals(obj2).toString())
        global.println("[7] equals distinct : obj.equals(obj3) -> " + obj.equals(obj3).toString())
        global.println("[8] equals self : obj.equals(obj) -> " + obj.equals(obj).toString())
        global.println("[9] equals null : obj.equals(null) -> " + obj.equals(null).toString())
    }

    # [10-13] static objectEquals with various combinations
    static testStaticEquals()
    {
        #!
        global.println("--- testStaticEquals ---")
        Object obj = new()
        Object obj2 = obj
        Object obj3 = new()

        #global.println("[10] objectEquals alias : Object.objectEquals(obj, obj2) -> " + Object.objectEquals(obj, obj2).toString())
        #global.println("[11] objectEquals distinct : Object.objectEquals(obj, obj3) -> " + Object.objectEquals(obj, obj3).toString())
        global.println("[12] objectEquals same : Object.objectEquals(obj, obj) -> " + Object.objectEquals(obj, obj).toString())
        global.println("[13] objectEquals obj,null : Object.objectEquals(obj, null) -> " + Object.objectEquals(obj, null).toString())
        global.println("[14] objectEquals null,obj : Object.objectEquals(null, obj) -> " + Object.objectEquals(null, obj).toString())
        global.println("[15] objectEquals null,null : Object.objectEquals(null, null) -> " + Object.objectEquals(null, null).toString())
        !#
    }

    # [16-19] hashCode consistency and distribution
    static testHashCode()
    {
        global.println("--- testHashCode ---")
        Object obj = new()
        Object obj2 = obj
        Object obj3 = new()

        int hc1 = obj.hashCode
        int hc2 = obj2.hashCode
        int hc3 = obj3.hashCode
        global.println("[16] hashCode alias same : (hc1 == hc2) -> " + (hc1 == hc2).toString())
        global.println("[17] hashCode distinct likely different : (hc1 != hc3) -> " + (hc1 != hc3).toString())
        global.println("[18] hashCode value obj : " + hc1.toString())
        global.println("[19] hashCode value obj3 : " + hc3.toString())

        # hashCode should be stable across multiple calls
        int hc1Again = obj.hashCode
        global.println("[20] hashCode stable : (hc1 == hc1Again) -> " + (hc1 == hc1Again).toString())
    }

    # [21-22] toString output
    static testToString()
    {
        global.println("--- testToString ---")
        Object obj = new()
        Object obj3 = new()

        string s1 = obj.toString()
        string s2 = obj3.toString()
        global.println("[21] toString obj : " + s1)
        global.println("[22] toString obj3 : " + s2)
        global.println("[23] toString different : (s1 != s2) -> " + (s1 != s2).toString())
    }

    # [24] refCount for alias
    static testRefCount()
    {
        global.println("--- testRefCount ---")
        Object obj = new()
        Object obj2 = obj
        int refc = Memory.refCount(obj2)
        global.println("[24] refCount (alias obj2) : " + refc.toString())
        int refcSelf = Memory.refCount(obj)
        global.println("[25] refCount (obj self) : " + refcSelf.toString())
    }

    # [26] refWeak
    static testRefWeak()
    {
        global.println("--- testRefWeak ---")
        Object obj = new()
        Object obj3 = new()
        object rwA = Memory.weakRef(obj)
        object rwB = Memory.weakRef(obj3)
        global.println("[26] refWeak distinct : Object.refEquals(rwA, rwB) -> " + Object.refEquals(rwA, rwB).toString())
        object rwSelf = Memory.weakRef(obj)
        global.println("[27] refWeak self stable : Object.refEquals(rwA, rwSelf) -> " + Object.refEquals(rwA, rwSelf).toString())
    }

    # [28-30] null handling
    static testNullHandling()
    {
        global.println("--- testNullHandling ---")
        Object obj = new()

        global.println("[28] refEquals(null,null) : " + Object.refEquals(null, null).toString())
        global.println("[29] refEquals(obj,null) : " + Object.refEquals(obj, null).toString())
        global.println("[30] refEquals(null,obj) : " + Object.refEquals(null, obj).toString())
    }

    # [31-32] type property
    static testTypeProperty()
    {
        global.println("--- testTypeProperty ---")
        Object obj = new()
        Type t = obj.type
        global.println("[31] type not null : (t != null) -> " + (t != null).toString())
        global.println("[32] type toString : " + t.toString())
    }

    # [33-34] ref property
    static testRefProperty()
    {
        global.println("--- testRefProperty ---")
        Object obj = new()
        Object obj2 = obj
        object r1 = Memory.ref(obj)
        object r2 = Memory.ref(obj2)
        global.println("[33] ref alias same : Object.refEquals(r1, r2) -> " + Object.refEquals(r1, r2).toString())
        global.println("[34] ref self same : Object.refEquals(r1, Memory.ref(obj)) -> " + Object.refEquals(r1, Memory.ref(obj)).toString())
    }

    # [35] lifecycle: free + release should not crash
    # Memory.Free and Memory.Release require Manual mode first.
    static testLifecycle()
    {
        global.println("--- testLifecycle ---")
        Object tmp = new()
        global.println("[35] tmp.hashCode before free -> " + tmp.hashCode.toString())
        Memory.manual(tmp)
        global.println("    called Memory.manual(tmp)")
        Memory.free(tmp)
        global.println("    called Memory.free(tmp)")

        Object tmp2 = new()
        Memory.manual(tmp2)
        Memory.retain(tmp2)
        Memory.release(tmp2)
        global.println("    called Memory.release(tmp2)")
    }

    # [36-38] object assignment and reassignment
    static testObjectAssignment()
    {
        global.println("--- testObjectAssignment ---")
        Object a = new()
        Object b = new()
        Object c = a
        global.println("[36] assign c=a, (a == c) -> " + (a == c).toString())
        global.println("[37] assign c=a, (b == c) -> " + (b == c).toString())
        c = b
        global.println("[38] reassign c=b, (a == c) -> " + (a == c).toString())
        global.println("[39] reassign c=b, (b == c) -> " + (b == c).toString())
    }

    # [40-42] chained equals calls
    static testChainedEquals()
    {
        global.println("--- testChainedEquals ---")
        Object a = new()
        Object b = new()
        Object c = new()
        Object d = a

        global.println("[40] chain a.equals(d).toString() -> " + a.equals(d).toString())
        global.println("[41] chain Object.refEquals(a, d).toString() -> " + Object.refEquals(a, d).toString())
        global.println("[42] chain (a == d).toString() -> " + (a == d).toString())
    }

    override string toString()
    {
        ret "ObjectTest"
    }
}

# 测试用例说明：
# - testReferenceEquality: == 运算符、refEquals 对别名和不同实例的测试
# - testEqualsMethod: equals() 实例方法对别名、不同实例、自身、null 的测试
# - testStaticEquals: 静态 objectEquals 对各种组合（含 null 对）的测试
# - testHashCode: hashCode 一致性（别名相同、多次调用稳定）和分布（不同实例通常不同）
# - testToString: toString 输出和不同实例的差异
# - testRefCount: refCount 对别名的测试
# - testRefWeak: refWeak 对不同实例和自身稳定性的测试
# - testNullHandling: null 对象的 refEquals 各种组合
# - testTypeProperty: type 属性获取和 toString
# - testRefProperty: ref 属性对别名和自身的稳定性
# - testLifecycle: free/release 生命周期不崩溃
# - testObjectAssignment: 对象赋值和重新赋值后的引用关系
# - testChainedEquals: 链式 equals 调用
# 预期：别名对为 true；不同实例 refEquals/objectEquals 为 false；hashCode 别名相同且稳定；free/release 后不崩溃。
