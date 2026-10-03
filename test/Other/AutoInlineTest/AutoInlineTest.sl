# 自动 inline (-O 优化等级) 测试: 无 inline 修饰符的小函数按优化等级自动标记。
# 阈值: O1 顶层语句 <=2 / O2 <=4 / O3 <=7; 排除集 (throws/被 override/operator/…) 不标记。
# 全档 (-O0..-O3) 运行输出必须一致; 分档验证见 IR.txt (标记档调用点无 Call 指令)。

AutoInlineMath
{
    # ---------- 阈值阶梯正例 ----------
    # [1] 1 条体: O1+ 自动标记
    static Int32 one()
    {
        ret 100
    }

    # [2] 2 条体: O1+ (O1 阈值边界)
    static Int32 two()
    {
        Int32 t = 8
        ret t * 2
    }

    # [3] 3 条体: O2+ (O1 不标记)
    static Int32 three()
    {
        Int32 a = 10
        Int32 b = 20
        ret a + b
    }

    # [4] 4 条体: O2+ (O2 阈值边界)
    static Int32 four()
    {
        Int32 a = 1
        Int32 b = 2
        Int32 c = 3
        ret a + b + c
    }

    # [5] 5 条体: 仅 O3
    static Int32 five()
    {
        Int32 a = 1
        Int32 b = 2
        Int32 c = 3
        Int32 d = 4
        ret a + b + c + d
    }

    # [6] 6 条体: 仅 O3
    static Int32 six()
    {
        Int32 a = 2
        Int32 b = 3
        Int32 c = 4
        Int32 d = 5
        Int32 e = 6
        ret a + b + c + d + e
    }

    # [7] 7 条体: O3+ (O3 阈值边界)
    static Int32 seven()
    {
        Int32 a = 1
        Int32 b = 2
        Int32 c = 3
        Int32 d = 4
        Int32 e = 5
        Int32 f = 6
        ret a + b + c + d + e + f
    }

    # [8] 8 条体: 任何等级都不标记 (阈值外负边界)
    static Int32 eight()
    {
        Int32 a = 1
        Int32 b = 2
        Int32 c = 3
        Int32 d = 4
        Int32 e = 5
        Int32 f = 6
        Int32 g = 7
        ret a + b + c + d + e + f + g
    }

    # ---------- 排除集负例 ----------
    # [E1] throws: 与 inline 异常帧语义冲突, 1 条体也不标记
    static Int32 throwsOne() throws
    {
        ret 77
    }

    # [E2] 自引用递归: 预检命中违禁扫描, 静默不标记, 正常调用
    static Int32 fact( Int32 n )
    {
        ret n <= 1 ? 1 : n * AutoInlineMath.fact( n - 1 )
    }

    # [E3] 间接递归环 A->B->A: 预检可过 (非自引用), 展开期环守卫命中,
    #      内层调用静默回退正常 Call (自动 inline 不产生用户可见错误)
    static Int32 ringA( Int32 n )
    {
        ret AutoInlineMath.ringB( n - 1 )
    }

    static Int32 ringB( Int32 n )
    {
        ret n <= 0 ? 7 : AutoInlineMath.ringA( n )
    }

    # [9] void 1 条体: O1+ 标记; 体内调用 global.println (系统方法转发链)
    static void logTag( string s )
    {
        global.println( "[auto] " + s )
    }
}

# 实例小函数: 自动标记隐含 final (inline 按静态绑定展开)
AutoPoint
{
    Int32 x = 0
    Int32 y = 0

    # 构造函数不标记 (2 条体, 排除集: _init_)
    _init_( Int32 px, Int32 py )
    {
        this.x = px
        this.y = py
    }

    # 1 条体实例函数: O1+ 自动标记
    Int32 sum()
    {
        ret this.x + this.y
    }

    # 2 条体实例函数: O1+ 自动标记
    Int32 scaledSum( Int32 k )
    {
        Int32 t = ( this.x + this.y ) * k
        ret t
    }
}

# 被 override 的方法不标记 (静态绑定展开会破坏多态), 子类多态正常分发
AutoBase
{
    # 1 条体但被 AutoChild override: 排除集命中
    Int32 tag()
    {
        ret 1
    }
}

AutoChild extends AutoBase
{
    override Int32 tag()
    {
        ret 2
    }
}

# operator 重载不标记 (调用点由运算符语法触发, 不属于普通调用展开)
AutoNum
{
    Int32 v = 0

    # 2 条顶层语句 (if 块整体计 1 条), O1 阈值内也不标记
    override AutoNum _add_( Object obj1 )
    {
        if obj1 is AutoNum n
        {
            AutoNum r = new()
            r.v = this.v + n.v
            ret r
        }
        ret this
    }
}

# 字段初始化器调用 1 条体函数: 该场景自动 inline 静默回退正常 Call
# (初始化器无宿主函数, 展开依赖宿主语句流; 显式 inline 会报 21458, 自动标记不报错)
AutoInit
{
    Int32 f = AutoInlineMath.one()

    # 1 条体实例函数: O1+ 自动标记 (与初始化器回退场景并存)
    Int32 read()
    {
        ret this.f
    }
}

AutoInlineTest
{
    static fun()
    {
        global.println( "========== AutoInlineTest (start) ==========" )

        # 1. 阈值阶梯: 各档输出必须与 -O0 一致
        global.println( "[ladder] one() = " + AutoInlineMath.one().toString() )
        global.println( "[ladder] two() = " + AutoInlineMath.two().toString() )
        global.println( "[ladder] three() = " + AutoInlineMath.three().toString() )
        global.println( "[ladder] four() = " + AutoInlineMath.four().toString() )
        global.println( "[ladder] five() = " + AutoInlineMath.five().toString() )
        global.println( "[ladder] six() = " + AutoInlineMath.six().toString() )
        global.println( "[ladder] seven() = " + AutoInlineMath.seven().toString() )
        global.println( "[ladder] eight() = " + AutoInlineMath.eight().toString() )

        # 2. 排除集: throws
        global.println( "[exclude] throwsOne() = " + AutoInlineMath.throwsOne().toString() )

        # 3. 排除集: 自引用递归
        global.println( "[exclude] fact(5) = " + AutoInlineMath.fact( 5 ).toString() )

        # 4. 环守卫: 间接递归环, 内层静默回退 Call
        global.println( "[ring] ringA(2) = " + AutoInlineMath.ringA( 2 ).toString() )

        # 5. 实例小函数自动 inline (this 经 receiver 槽绑定)
        AutoPoint p = new( 10, 20 )
        global.println( "[instance] p.sum() = " + p.sum().toString() )
        global.println( "[instance] p.scaledSum(3) = " + p.scaledSum( 3 ).toString() )

        # 6. 排除集: 被 override, 多态保留
        AutoBase b1 = AutoBase()
        AutoBase b2 = AutoChild()
        global.println( "[override] b1.tag() = " + b1.tag().toString() )
        global.println( "[override] b2.tag() = " + b2.tag().toString() )

        # 7. 排除集: operator 重载
        AutoNum n1 = new()
        n1.v = 10
        AutoNum n2 = new()
        n2.v = 32
        AutoNum nsum = n1 + n2
        global.println( "[operator] n1 + n2 .v = " + nsum.v.toString() )

        # 8. 字段初始化器: 自动 inline 回退正常 Call, 不报错
        AutoInit ai = new()
        global.println( "[init] ai.read() = " + ai.read().toString() )

        # 9. void 1 条函数 (体内经 global.println 转发系统调用)
        AutoInlineMath.logTag( "inline-ok" )

        global.println( "========== AutoInlineTest (end) ==========" )
    }
}
