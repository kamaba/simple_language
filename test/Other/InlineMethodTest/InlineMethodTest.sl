InlineMath
{
    static inline Int32 add( Int32 a, Int32 b )
    {
        ret a + b
    }

    static inline Int32 square( Int32 x )
    {
        ret x * x
    }

    static inline Int32 maxOf( Int32 a, Int32 b )
    {
        ret a > b ? a : b
    }

    static inline Int32 cal( Int32 a, Int32 b, Int32 c )
    {
        ret (a + b) * c
    }

    # [8] void 返回: 体内直接打印, 调用方不消费返回值 (栈效果: 只弹参不压回)
    static inline void logAdd( Int32 a, Int32 b )
    {
        global.println("logAdd(" + a.toString() + ", " + b.toString() + ") = " + (a + b).toString())
    }

    # [9] 非 Int32 类型: double 参数与返回
    static inline double avg( double a, double b )
    {
        ret (a + b) / 2.0d
    }

    # [10] result 变量: 显式 ret result
    static inline Result calcResultOk( Int32 code )
    {
        result.code = code
        result.message = "inline-ok"
        ret result
    }

    # [11] result 值返回改写: ret 999 => result.value = 999; ret result
    static inline Result calcResultValue( Int32 v )
    {
        ret v
    }

    # [12] result 自然结束路径: 无显式 ret, 由展开段 epilogue 兜底 [Load result][StoreLocal 结果槽]
    static inline Result calcResultFall( Int32 code )
    {
        result.code = code
        result.message = "inline-fall"
    }

    # [13] result 部分路径显式 ret: 其余路径自然结束, 两条路径在段尾锚汇合时结果槽均持有 result
    static inline Result calcResultMix( bool flag )
    {
        if flag
        {
            result.code = 1
            result.message = "mix-true"
            ret result
        }
        result.code = 2
        result.message = "mix-false"
    }

    # [14] 泛型 Result<T> 的 result 变量: NewTemplateObject 初始化
    static inline Result<int> calcResultT( Int32 v )
    {
        result.code = 300
        result.value = v
        ret result
    }
}

# 实例 inline: 带字段类 + _init_ 构造 + 实例 inline 方法
# (实例 inline 隐含 final: 展开按静态绑定, 不允许被 override; this 经 receiver 槽绑定)
Vec2
{
    Int32 x = 0
    Int32 y = 0

    _init_( Int32 px, Int32 py )
    {
        this.x = px
        this.y = py
    }

    inline Int32 dot( Vec2 o )
    {
        ret this.x * o.x + this.y * o.y
    }

    inline Int32 len2()
    {
        ret this.x * this.x + this.y * this.y
    }
}

InlineMethodTest
{
    static fun()
    {
        global.println("========== InlineMethodTest (start) ==========")

        # 0. 对照: 普通表达式 (无 inline 方法)
        Int32 c1 = 10 + 20
        Int32 c2 = 100 + 200
        Int32 c3 = c1 + c2
        global.println("plain: c1 + c2 = " + c3.toString())

        # 1. 双参基础: 加法
        Int32 r1 = InlineMath.add(1, 2)
        global.println("add(1, 2) = " + r1.toString())

        # 2. 单参: 平方
        Int32 r2 = InlineMath.square(5)
        global.println("square(5) = " + r2.toString())

        # 3. 实参为表达式
        Int32 r3 = InlineMath.add(1 + 2, 3 * 4)
        global.println("add(1+2, 3*4) = " + r3.toString())

        # 4. 实参内嵌套调用同一 inline 方法 (实参来源嵌套放行)
        Int32 r4 = InlineMath.add(InlineMath.add(1, 2), 3)
        global.println("add(add(1,2), 3) = " + r4.toString())

        # 5. 三元表达式体
        Int32 r5 = InlineMath.maxOf(10, 20)
        global.println("maxOf(10, 20) = " + r5.toString())

        # 6. 括号改变优先级
        Int32 r6 = InlineMath.cal(1, 2, 3)
        global.println("cal(1, 2, 3) = " + r6.toString())

        # 7. 同一 inline 方法多处调用 (防别名污染: 应得 330 而非 60)
        Int32 r7 = InlineMath.add(10, 20) + InlineMath.add(100, 200)
        global.println("add(10,20) + add(100,200) = " + r7.toString())

        # 8. void inline: 直接调用不消费返回值
        InlineMath.logAdd(11, 22)

        # 9. double inline: 应得 1.5
        double d1 = InlineMath.avg(1.0d, 2.0d)
        global.println("avg(1.0d, 2.0d) = " + d1.toString())

        # 10. result 变量: 显式 ret result
        Result rOk = InlineMath.calcResultOk(100)
        global.println("calcResultOk.code = " + rOk.code.toString())
        global.println("calcResultOk.message = " + rOk.message)

        # 11. result 值返回改写
        Result rVal = InlineMath.calcResultValue(999)
        global.println("calcResultValue.value = " + rVal.value.toString())

        # 12. result 自然结束路径 (epilogue 兜底)
        Result rFall = InlineMath.calcResultFall(42)
        global.println("calcResultFall.code = " + rFall.code.toString())
        global.println("calcResultFall.message = " + rFall.message)

        # 13. result 部分路径显式 ret
        Result rMixT = InlineMath.calcResultMix(true)
        global.println("calcResultMix(true).code = " + rMixT.code.toString())
        global.println("calcResultMix(true).message = " + rMixT.message)
        Result rMixF = InlineMath.calcResultMix(false)
        global.println("calcResultMix(false).code = " + rMixF.code.toString())
        global.println("calcResultMix(false).message = " + rMixF.message)

        # 14. 泛型 Result<T>
        Result<int> rT = InlineMath.calcResultT(777)
        global.println("calcResultT.code = " + rT.code.toString())
        global.println("calcResultT.value = " + rT.value.toString())

        # 15. 实例 inline: receiver 槽绑定 + this/形参字段访问 (dot = 3*1+4*2 = 11)
        Vec2 v1 = new(3, 4)
        Vec2 v2 = new(1, 2)
        Int32 dotVal = v1.dot(v2)
        global.println("v1.dot(v2) = " + dotVal.toString())

        # 16. 无参实例 inline + 展开结果链式调用 (len2 = 9+16 = 25)
        Int32 l2 = v1.len2()
        global.println("v1.len2() = " + l2.toString())
        global.println("v1.len2().toString() chain = " + v1.len2().toString())

        global.println("========== InlineMethodTest (end) ==========")
    }
}
