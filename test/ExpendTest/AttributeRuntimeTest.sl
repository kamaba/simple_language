import Std;

# ============================================================================
# AttributeRuntimeTest.sl — P7 Runtime 时点 attribute 测试
#（ATTRIBUTE_DESIGN.md §6 P7：触发 + 查询双通道）
#
# 覆盖点：
#   1. 触发通道：@LogTrace 方法调用 → _enter_（正序）/ _exit_（逆序）执行
#   2. 同宿主多 attribute：@LogTrace + @Meter 叠加，enter 先 LogTrace 后
#      Meter、exit 先 Meter 后 LogTrace（逆序对称）
#   3. 查询通道：Attribute.getClassAttributes / getFieldAttributes /
#      getMethodAttributes 三 API（stage==Runtime 过滤 + targets 位匹配）
#   4. 参数注入：@Range("血量", 0, 9999) 构造参数经 _init_ 写入子类字段
#   5. 宿主信息注入：_ownerClassName / _ownerMemberName
#   6. 未命中 / 非法类名 → 空数组（非 null）回归
#   7. Serializable 回归：序列化过滤切换到 Runtime 查询通道后行为不变
#      （C 层 json_system_method.c vm_json_ser_has_attr）
# ============================================================================

# ── 共享打点缓冲（跨标签验证多 attribute 正/逆序对称）──
public class AttrTraceBuf
{
    public static string trace = ""
}

# ── Runtime · 触发型：方法进入/退出打点 ──
public class LogTrace extends Attribute
{
    _init_()
    {
        this._attributeStage = EAttributeStage.Runtime
        this._attributeTargets = EAttributeTarget.Method
    }

    # 协变返回：返回声明类自身（对齐 _add_ 等 CurrentClass 惯例）
    public override LogTrace _enter_()
    {
        AttrTraceBuf.trace = AttrTraceBuf.trace + "[enter]" + this._ownerMemberName
        ret this
    }

    public override void _exit_()
    {
        AttrTraceBuf.trace = AttrTraceBuf.trace + "[exit]" + this._ownerMemberName
    }
}

# ── Runtime · 触发型（第二标签：仅用于验证同宿主叠加的正/逆序）──
public class Meter extends Attribute
{
    _init_()
    {
        this._attributeStage = EAttributeStage.Runtime
        this._attributeTargets = EAttributeTarget.Method
    }

    public override Meter _enter_()
    {
        AttrTraceBuf.trace = AttrTraceBuf.trace + "[m-enter]"
        ret this
    }

    public override void _exit_()
    {
        AttrTraceBuf.trace = AttrTraceBuf.trace + "[m-exit]"
    }
}

# ── Runtime · 查询型：字段取值范围标签（数据 + getter，无回调）──
# 注：类名避开 Core 容器 Range<T>，防 ListTest 的模板解析被同名遮蔽
public class NumRange extends Attribute
{
    private string _title = ""
    private double _min = 0
    private double _max = 0

    _init_( string title, double min, double max )
    {
        this._title = title
        this._min = min
        this._max = max
        this._attributeStage = EAttributeStage.Runtime
        this._attributeTargets = EAttributeTarget.Field
    }

    public get string title()
    {
        ret this._title
    }
    public get double min()
    {
        ret this._min
    }
    public get double max()
    {
        ret this._max
    }
}

# ── 演示宿主 1：触发型 + 查询型混用 ──
public class AttrRuntimeDemo
{
    @NumRange( "血量", 0, 9999 )
    public double hp = 100

    @NumRange( "等级", 1, 99 )
    protected double level = 1

    @LogTrace()
    public void update()
    {
        this.hp = this.hp + 1
    }

    # 带返回值方法：_exit_ 在返回值求值之后触发
    @LogTrace()
    public Int32 step( Int32 num )
    {
        ret num + 1
    }

    # 同宿主双标签：enter 正序 LogTrace→Meter，exit 逆序 Meter→LogTrace
    @LogTrace()
    @Meter()
    public void tick()
    {
        this.hp = this.hp + 1
    }

    _init_()
    {
    }
}

# ── 演示宿主 2：@Serializable 类级标签（类级查询 + 序列化回归）──
@Serializable()
public class AttrSerDemo
{
    public Int32 v = 0

    _init_()
    {
    }
}

# ── 测试入口 ──
public class AttributeRuntimeTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[AttrRT] " + name + " : OK" )
        }
        else
        {
            Console.println( "[AttrRT] " + name + " : FAIL" )
        }
    }

    # 1. 触发通道：单标签 void 方法 / 带返回值方法
    static testTrigger()
    {
        AttrRuntimeDemo demo = AttrRuntimeDemo()

        AttrTraceBuf.trace = ""
        demo.update()
        check( "触发: _enter_/_exit_ 各执行一次", AttrTraceBuf.trace == "[enter]update[exit]update" )
        check( "触发: 方法体副作用生效", demo.hp == 101 )

        AttrTraceBuf.trace = ""
        Int32 got = demo.step( 41 )
        check( "触发: 带返回值方法返回值正确", got == 42 )
        check( "触发: 带返回值方法打点完整", AttrTraceBuf.trace == "[enter]step[exit]step" )
    }

    # 2. 同宿主多 attribute：enter 正序 / exit 逆序
    static testTriggerOrder()
    {
        AttrRuntimeDemo demo = AttrRuntimeDemo()

        AttrTraceBuf.trace = ""
        demo.tick()
        check( "多标签: enter 正序 + exit 逆序",
            AttrTraceBuf.trace == "[enter]tick[m-enter][m-exit][exit]tick" )
    }

    # 3. 字段级查询：命中 / 参数注入 / protected 字段 / 未命中空数组
    static testQueryField()
    {
        Array<Attribute> attrs = Attribute.getFieldAttributes( "AttrRuntimeDemo", "hp" )
        check( "查询: 字段 hp 命中 1 条", attrs != null && attrs.length == 1 )

        NumRange r = null
        if attrs != null && attrs.length == 1
        {
            r = attrs._getItem_( 0 ) as NumRange
        }
        check( "查询: 参数注入 title", r != null && r.title == "血量" )
        check( "查询: 参数注入 min", r != null && r.min == 0 )
        check( "查询: 参数注入 max", r != null && r.max == 9999 )

        Array<Attribute> lv = Attribute.getFieldAttributes( "AttrRuntimeDemo", "level" )
        check( "查询: protected 字段命中", lv != null && lv.length == 1 )

        Array<Attribute> miss = Attribute.getFieldAttributes( "AttrRuntimeDemo", "noSuchField" )
        check( "查询: 未命中字段返回空数组", miss != null && miss.length == 0 )
    }

    # 4. 类级 / 方法级查询：命中计数 / 未命中 / 非法类名
    static testQueryClassAndMethod()
    {
        Array<Attribute> cls = Attribute.getClassAttributes( "AttrSerDemo" )
        check( "查询: 类级 @Serializable 命中 1 条", cls != null && cls.length == 1 )
        Serializable ser = null
        if cls != null && cls.length == 1
        {
            ser = cls._getItem_( 0 ) as Serializable
        }
        check( "查询: 类级实例可下行转型", ser != null )

        Array<Attribute> m1 = Attribute.getMethodAttributes( "AttrRuntimeDemo", "update" )
        check( "查询: 方法 update 命中 1 条", m1 != null && m1.length == 1 )

        Array<Attribute> m2 = Attribute.getMethodAttributes( "AttrRuntimeDemo", "tick" )
        check( "查询: 方法 tick 命中 2 条", m2 != null && m2.length == 2 )

        Array<Attribute> m3 = Attribute.getMethodAttributes( "AttrRuntimeDemo", "noSuchMethod" )
        check( "查询: 未命中方法返回空数组", m3 != null && m3.length == 0 )

        Array<Attribute> bad = Attribute.getClassAttributes( "NoSuchClassAtAll" )
        check( "查询: 非法类名返回空数组", bad != null && bad.length == 0 )
    }

    # 5. Serializable 序列化回归（stage 过滤切换后行为不变）
    static testSerializableRegression()
    {
        AttrSerDemo d = AttrSerDemo()
        d.v = 7
        string json = Serialize.toJson<AttrSerDemo>( d )
        check( "序列化: @Serializable 类正常展开", json == "{\"v\":7}" )

        AttrSerDemo back = Serialize.fromJson<AttrSerDemo>( json )
        check( "序列化: 反向填充还原", back != null && back.v == 7 )
    }

    static fun()
    {
        Console.println( "==== AttributeRuntimeTest 开始 ====" )
        testTrigger()
        testTriggerOrder()
        testQueryField()
        testQueryClassAndMethod()
        testSerializableRegression()
        Console.println( "==== AttributeRuntimeTest 结束 ====" )
    }
}
