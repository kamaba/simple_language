import Std;

# ============================================================================
# AttributeTest.sl — 自定义 attribute 子类 + 类/字段/方法三级标注样张
# 旧方括号语法（[Ab1("a")] / ["Am"="o"] / ["Am"="mmm"]）已废弃
# （File 层解析越界异常），统一 @Name(args) 新语法（ATTRIBUTE_DESIGN.md §7）
# ============================================================================

# ── 自定义子类 1：类级标签（原 webapi，get typeGet 改普通方法）──
public class webapi extends Attribute
{
    _init_()
    {
        this._attributeStage = EAttributeStage.Compiling
        this._attributeTargets = EAttributeTarget.All
    }

    public void typeGet( string path )
    {

    }
}

# ── 自定义子类 2：空标签（原 coda）──
public class coda extends Attribute
{
    _init_()
    {
        this._attributeStage = EAttributeStage.Compiling
        this._attributeTargets = EAttributeTarget.All
    }
}

# ── 自定义子类 3：带参构造 + 查询型（原 instance extens 笔误 /
#    construct / metaType 旧形态，按 P7 NumRange 模式重建）──
public class instanceAttr extends Attribute
{
    private string _tag = ""

    _init_( string tag )
    {
        this._tag = tag
        this._attributeStage = EAttributeStage.Runtime
        this._attributeTargets = EAttributeTarget.Field
    }

    public get string tag()
    {
        ret this._tag
    }
}

# ── 三级标注宿主（原 Class1/Class2，与 AttributeNicknameTest 同模块撞名 → 改名；
#    alias 同理换新，避免与该文件已注册的 OKK/Prt/你 冲突）──
@webapi()
AttrHost1
{
    @instanceAttr( "血量" )
    int a = 0;

    @Nickname("ApiPr", "Root.ApiPrint")
    pinrt()
    {
        # 原嵌套裸块语法已废弃（label 必显式命名），函数体简化
        int localA = 10;
        Console.println( "localA = " + localA );
    }
}

@coda()
AttrHost2
{
    static int m2 = 10;
    int m = 10;
    AttrHost2( int x )
    {
        this.m = 10;
    }
}
