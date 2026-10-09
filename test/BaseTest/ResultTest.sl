ResultTest
{
    static fun()
    {
        global.println("========== ResultTest (start) ==========")
        testAutoResult()
        testRetValue()
        testResultT()
        testCovariantAssign()
        testMixReturn()
        global.println("========== ResultTest (end) ==========")
    }

    # [1] 自动注入 result 变量: 函数内直接使用 result.code / result.message, ret result
    static Result useAutoResult()
    {
        result.code = 100
        result.message = "ok"
        ret result
    }

    # [2] 值返回改写: ret 100 等价 result.value = 100; ret result
    static Result retValue()
    {
        ret 100
    }

    # [2b] ret Object() 同样改写为 result.value
    static Result retObjectValue()
    {
        ret Object()
    }

    # [3] 泛型 Result<int> 的值返回改写: ret 200 => result.value = 200
    static Result<int> retValueT()
    {
        ret 200
    }

    # [3b] 泛型 Result<int> 的自动 result 变量
    static Result<int> useAutoResultT()
    {
        result.code = 300
        result.message = "T-ok"
        result.value = 400
        ret result
    }

    # [4] 部分路径显式 ret result, 其余路径掉落由 epilogue 兜底返回 result
    static Result mixReturn( bool flag )
    {
        if flag
        {
            result.code = 1
            result.message = "flag-true"
            ret result
        }
        result.code = 2
        result.message = "flag-false"
    }

    # [5] ret 其他 Result 对象: 走正常返回路径, 不做 value 提取
    static Result retOther()
    {
        Result r = new()
        r.code = 500
        r.message = "other"
        ret r
    }

    static testAutoResult()
    {
        Result r = useAutoResult()
        global.println("[1] code : " + r.code.toString())
        global.println("[1] message : " + r.message)

        Result r2 = retOther()
        global.println("[5] code : " + r2.code.toString())
        global.println("[5] message : " + r2.message)
    }

    static testRetValue()
    {
        Result r = retValue()
        global.println("[2] value : " + r.value.toString())

        Result r2 = retObjectValue()
        global.println("[2b] value is null : " + (r2.value == null).toString())
    }

    static testResultT()
    {
        Result<int> rt = retValueT()
        global.println("[3] value : " + rt.value.toString())

        Result<int> rt2 = useAutoResultT()
        global.println("[3b] code : " + rt2.code.toString())
        global.println("[3b] message : " + rt2.message)
        global.println("[3b] value : " + rt2.value.toString())
    }

    static testCovariantAssign()
    {
        # Result<T> 协变赋值给 Result (合法), 反向不合法
        Result r = retValueT()
        global.println("[4] covariant code : " + r.code.toString())
        global.println("[4] covariant value : " + r.value.toString())

        Result<int> rt = useAutoResultT()
        Result r2 = rt
        global.println("[4] covariant2 value : " + r2.value.toString())
    }

    static testMixReturn()
    {
        Result a = mixReturn( true )
        global.println("[6] mix-true code : " + a.code.toString())
        global.println("[6] mix-true message : " + a.message)

        Result b = mixReturn( false )
        global.println("[6] mix-false code : " + b.code.toString())
        global.println("[6] mix-false message : " + b.message)
    }
}
