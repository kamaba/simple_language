import Std;

# ============================================================================
# SerializeTest.sl — @Serializable 序列化/反序列化测试
#
# 覆盖点（C 层 json_system_method.c vm_json_ser_* 家族）：
#   1. @Serializable class 正向成员过滤：public 参与；protected/private
#      默认排除；@SerializeField() 补票；@NonSerialized() 淘汰；static 排除
#   2. @Serializable class 反向填充：按 JSON key 对名填充，被排除成员
#      保持默认值（正反向对称，同一过滤函数）
#   3. toJson → fromJson → toJson 往返一致性
#   4. 嵌套 @Serializable class 成员递归展开/还原（PTR 槽放宽）
#   5. 未标注 class 回归：不走成员展开，维持 toString() 嫁接路径
#   6. data 回归：默认全量序列化（@Serializable 为可选显式标注）
#
# 门面：Serialize.toJson / toJsonPretty / fromJson（Core/IO/Serialize.sl）
# ============================================================================

# ── 用例类型 1：@Serializable class，成员可见性全组合 ──
@Serializable()
public class SerPoint
{
    public Int32 x = 0
    public Int32 y = 0
    protected Int32 z = 0                 # 默认不参与
    private Int32 w = 0                   # 默认不参与
    @SerializeField()
    protected Int32 forced = 0            # 补票参与
    @NonSerialized()
    public Int32 skipped = 0              # public 被淘汰
    public static Int32 Version = 9       # static 排除

    _init_()
    {
    }

    # 类内读写 protected/private（类外不可见，断言经此透出）
    public void setup( Int32 _x, Int32 _y, Int32 _z, Int32 _w, Int32 _forced, Int32 _skipped )
    {
        this.x = _x
        this.y = _y
        this.z = _z
        this.w = _w
        this.forced = _forced
        this.skipped = _skipped
    }

    public string dump()
    {
        ret "(" + this.x.toString() + "," + this.y.toString() + "," + this.z.toString() + "," + this.w.toString() + "," + this.forced.toString() + "," + this.skipped.toString() + ")"
    }
}

# ── 用例类型 2：嵌套 @Serializable class（成员引用递归）──
@Serializable()
public class SerInner
{
    public Int32 v = 0

    _init_()
    {
    }
}

@Serializable()
public class SerOuter
{
    public SerInner inner = SerInner()
    public string tag = ""

    _init_()
    {
    }
}

# ── 用例类型 3：未标注 class（回归：toString() 嫁接路径不变）──
public class PlainPoint
{
    public Int32 x = 0
    public Int32 y = 0

    _init_()
    {
    }

    override string toString()
    {
        ret "Plain(" + this.x.toString() + "," + this.y.toString() + ")"
    }
}

# ── 用例类型 4：data（回归：默认全量）──
data SerDataInfo
{
    id = 0
    name = "guest"
}


SerializeTest
{
    # 断言辅助：OK/FAIL 单行输出
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[Serialize] " + name + " : OK" )
        }
        else
        {
            Console.println( "[Serialize] " + name + " : FAIL" )
        }
    }

    # 用例1：@Serializable class 正向成员过滤
    static testClassForward()
    {
        Console.println( "===== SerializeTest testClassForward =====" )
        SerPoint p = SerPoint()
        p.setup( 1, 2, 3, 4, 5, 6 )
        string json = Serialize.toJson<SerPoint>( p )
        Console.println( "toJson = " + json )
        check( "正向全串: public 参与 / protected+private 排除 / @SerializeField 补票 / @NonSerialized 淘汰 / static 排除",
               json == "{\"x\":1,\"y\":2,\"forced\":5}" )
    }

    # 用例2：@Serializable class 反向填充（排除成员保持默认）
    static testClassBackward()
    {
        Console.println( "===== SerializeTest testClassBackward =====" )
        string json = "{\"x\":10,\"y\":20,\"z\":30,\"w\":40,\"forced\":50,\"skipped\":60}"
        SerPoint q = Serialize.fromJson<SerPoint>( json )
        Console.println( "fromJson dump = " + q.dump() )
        check( "反向填充: x/y/forced 命中, z/w/skipped 排除保默认",
               q.dump() == "(10,20,0,0,50,0)" )
    }

    # 用例3：往返一致性
    static testRoundTrip()
    {
        Console.println( "===== SerializeTest testRoundTrip =====" )
        SerPoint p = SerPoint()
        p.setup( 7, 8, 9, 10, 11, 12 )
        string r1 = Serialize.toJson<SerPoint>( p )
        SerPoint p2 = Serialize.fromJson<SerPoint>( r1 )
        string r2 = Serialize.toJson<SerPoint>( p2 )
        Console.println( "r1 = " + r1 )
        Console.println( "r2 = " + r2 )
        check( "往返一致: toJson→fromJson→toJson 等值", r1 == r2 )
        check( "往返后排除成员归默认值", p2.dump() == "(7,8,0,0,11,0)" )
    }

    # 用例4：嵌套 @Serializable class 递归
    static testNested()
    {
        Console.println( "===== SerializeTest testNested =====" )
        SerOuter o = SerOuter()
        o.inner.v = 7
        o.tag = "root"
        string json = Serialize.toJson<SerOuter>( o )
        Console.println( "toJson = " + json )
        check( "嵌套正向: inner 子树展开", json == "{\"inner\":{\"v\":7},\"tag\":\"root\"}" )

        SerOuter o2 = Serialize.fromJson<SerOuter>( "{\"inner\":{\"v\":9},\"tag\":\"leaf\"}" )
        Console.println( "fromJson inner.v = " + o2.inner.v.toString() + ", tag = " + o2.tag )
        check( "嵌套反向: inner 递归还原", o2.inner.v == 9 && o2.tag == "leaf" )

        # toJsonPretty 缩进美化（非全串比对，输出供人工核对）
        Console.println( "toJsonPretty:" )
        Console.println( Serialize.toJsonPretty<SerOuter>( o ) )
    }

    # 用例5：未标注 class 回归（toString 嫁接，不走成员展开）
    static testPlainClass()
    {
        Console.println( "===== SerializeTest testPlainClass =====" )
        PlainPoint pp = PlainPoint()
        pp.x = 1
        pp.y = 2
        string json = Serialize.toJson<PlainPoint>( pp )
        Console.println( "toJson = " + json )
        check( "未标注 class: toString 字符串叶子嫁接", json == "\"Plain(1,2)\"" )
    }

    # 用例6：data 回归（默认全量，无需 @Serializable）
    static testDataDefault()
    {
        Console.println( "===== SerializeTest testDataDefault =====" )
        SerDataInfo d = new()
        d.id = 7
        d.name = "sam"
        string json = Serialize.toJson<SerDataInfo>( d )
        Console.println( "toJson = " + json )
        check( "data 正向默认全量", json == "{\"id\":7,\"name\":\"sam\"}" )

        SerDataInfo d2 = Serialize.fromJson<SerDataInfo>( "{\"id\":42,\"name\":\"bob\"}" )
        Console.println( "fromJson id = " + d2.id.toString() + ", name = " + d2.name )
        check( "data 反向还原", d2.id == 42 && d2.name == "bob" )
    }

    static fun()
    {
        testClassForward()
        testClassBackward()
        testRoundTrip()
        testNested()
        testPlainClass()
        testDataDefault()
    }
}
