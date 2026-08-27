
# ============================================================
# MemberFunction2Test - 继承与多态全面测试
# 覆盖：override/final/static、多级继承、interface抽象方法、
#       构造函数继承、多态调用、参数兼容性、跨命名空间继承
# ============================================================


# ============================================================
# Section 1: 原有 override/final/static 测试 (保留并扩展)
# ============================================================
Class1
{
    a1func(){ }
    b1func(){ }
    c1func(){}
    override d1func(){}
}

Class2 extends Class1
{
    _init_()
    {
    }
    # 以下两个与类名不同，验证不允许与类名同名的构造
    # Class1() { }    # 不允许
    # Class3() { }    # 不允许与类名相同

    a1func(){ }

    final b1func(){ }

    c1func(){}

    d1func(){}
}

Class3 extends Class2
{
    # b1func(){}   #报错: 父类final方法不可覆盖
    # c1func(){}   #报错: 应该有override标记

    final override d1func(){}   #成功: final override 组合
}

Class4 extends Class1
{
    # static b1func(){}    #报错: 已有该函数名 (static不能隐藏实例方法)
    # virtual c1func(){}   #报错: 已有函数名 (virtual不能用于已有方法)
    override d1func(){}      #成功: 带override标记覆盖
}


# ============================================================
# Section 2: 多级继承 (4级继承链)
# ============================================================
class ML_Base
{
    int level = 1

    int getLevel()
    {
        ret this.level
    }

    string name()
    {
        ret "ML_Base"
    }

    override int calc()
    {
        ret 100
    }
}

class ML_Mid extends ML_Base
{
    int level = 2

    override string name()
    {
        ret "ML_Mid"
    }

    override int calc()
    {
        ret base.calc() + 20
    }
}

class ML_Child extends ML_Mid
{
    int level = 3

    override string name()
    {
        ret "ML_Child"
    }

    override int calc()
    {
        ret base.calc() + 3
    }
}

class ML_GrandChild extends ML_Child
{
    int level = 4

    override string name()
    {
        ret "ML_GrandChild"
    }

    override int calc()
    {
        ret base.calc() + 4
    }

    string chainInfo()
    {
        ret base.name() + " -> " + this.name() + " level=" + this.level.toString()
    }
}


# ============================================================
# Section 3: interface 方法 (抽象方法) 与继承
# ============================================================

# interface 方法声明 + 子类实现
class AbsBase
{
    int baseVal = 50

    # interface 方法: 有默认实现, 子类可覆盖
    interface int getValue()
    {
        ret this.baseVal
    }

    # interface 方法: 无默认实现 (纯抽象)
    interface string describe()

    # 普通方法
    int doubled()
    {
        ret this.getValue() * 2
    }
}

class AbsChild extends AbsBase
{
    int childVal = 200

    # 覆盖 interface 方法
    interface int getValue()
    {
        ret this.childVal
    }

    # 实现抽象 interface 方法
    interface string describe()
    {
        ret "AbsChild childVal=" + this.childVal.toString() + " baseVal=" + base.baseVal.toString()
    }
}

class AbsGrandChild extends AbsChild
{
    int grandVal = 500

    # 再次覆盖
    interface int getValue()
    {
        ret this.grandVal
    }

    interface string describe()
    {
        ret "AbsGrandChild grand=" + this.grandVal.toString()
    }
}


# ============================================================
# Section 4: final 方法继承
# ============================================================
class FinalBase
{
    override int process()
    {
        ret 10
    }

    final int lockedMethod()
    {
        ret 999
    }
}

class FinalChild extends FinalBase
{
    override int process()
    {
        ret base.process() + 5
    }

    # lockedMethod()  #报错: final 方法不可覆盖

    int callLocked()
    {
        ret base.lockedMethod()
    }
}

class FinalGrandChild extends FinalChild
{
    final override int process()
    {
        ret base.process() + 1
    }

    # process()  #报错: 上一级已 final override, 不可再覆盖

    int callChain()
    {
        ret this.process() + this.callLocked()
    }
}


# ============================================================
# Section 5: 继承中的参数兼容性
# ============================================================

# 父类有默认参数, 子类override时签名一致
class ParamCompatBase
{
    int compute( int a, int b = 10, int c = 20 )
    {
        ret a + b + c
    }

    string format( string prefix = "[", string suffix = "]" )
    {
        ret prefix + "base" + suffix
    }
}

class ParamCompatChild extends ParamCompatBase
{
    override int compute( int a, int b = 10, int c = 20 )
    {
        ret base.compute( a, b, c ) * 2
    }

    override string format( string prefix = "[", string suffix = "]" )
    {
        ret prefix + "child" + suffix
    }
}

# 子类新增重载 (不覆盖父类方法, 而是添加新签名)
class ParamExtendChild extends ParamCompatBase
{
    # 新增重载: 不同参数个数
    int compute( int a, int b, int c, int d )
    {
        ret a + b + c + d
    }

    # 新增重载: 不同参数类型
    string compute( string a, string b )
    {
        ret a + b
    }
}


# ============================================================
# Section 6: 多态调用
# ============================================================
class PolyBase
{
    int val = 0

    _init_( int v = 0 )
    {
        this.val = v
    }

    override string tag()
    {
        ret "PolyBase"
    }

    override int calc()
    {
        ret this.val
    }
}

class PolyChild1 extends PolyBase
{
    _init_( int v )
    {
        base._init_( v )
    }

    override string tag()
    {
        ret "PolyChild1"
    }

    override int calc()
    {
        ret base.calc() + 100
    }
}

class PolyChild2 extends PolyBase
{
    _init_( int v )
    {
        base._init_( v )
    }

    override string tag()
    {
        ret "PolyChild2"
    }

    override int calc()
    {
        ret base.calc() + 200
    }
}


# ============================================================
# Section 7: 构造函数继承
# ============================================================
class CtorBase
{
    int x = 0
    int y = 0
    string tag = "CtorBase"

    override _init_()
    {
        this.x = 1
        this.y = 1
    }

    _init_( int _x, int _y )
    {
        this.x = _x
        this.y = _y
    }

    _init_( int _x )
    {
        this.x = _x
        this.y = -1
    }

    string describe()
    {
        ret this.tag + " x=" + this.x.toString() + " y=" + this.y.toString()
    }
}

class CtorChild extends CtorBase
{
    int z = 0
    string tag = "CtorChild"

    _init_( int _x, int _y, int _z )
    {
        base._init_( _x, _y )
        this.z = _z
    }

    _init_( int _x )
    {
        base._init_( _x )
        this.z = -1
    }

    override string describe()
    {
        ret base.describe() + " z=" + this.z.toString()
    }
}


# ============================================================
# Section 8: 跨命名空间继承
# ============================================================
namespace NS_MF2.Base
{
    class Counter
    {
        static int count = 0

        int get()
        {
            ret NS_MF2.Base.Counter.count
        }

        void inc()
        {
            NS_MF2.Base.Counter.count = NS_MF2.Base.Counter.count + 1
        }

        virtual string label()
        {
            ret "Counter"
        }
    }
}

namespace NS_MF2.Derived
{
    class FastCounter extends NS_MF2.Base.Counter
    {
        int step = 2

        override void inc()
        {
            NS_MF2.Base.Counter.count = NS_MF2.Base.Counter.count + this.step
        }

        override string label()
        {
            ret "FastCounter(step=" + this.step.toString() + ")"
        }
    }
}


# ============================================================
# Section 9: 继承中方法调用链 (base 链式调用)
# ============================================================
class ChainBase
{
    override string build()
    {
        ret "B"
    }
}

class ChainMid extends ChainBase
{
    override string build()
    {
        ret "[" + base.build() + "]"
    }
}

class ChainTop extends ChainMid
{
    override string build()
    {
        ret "{" + base.build() + "}"
    }
}


# ============================================================
# 测试入口
# ============================================================
MemberFunction2Test
{
    static testOriginal()
    {
        global.println( "----- testOriginal -----" )
        Class1 c1 = Class1()
        Class2 c2 = Class2()
        Class4 c4 = Class4()
        c1.a1func()
        c1.b1func()
        c1.c1func()
        c1.d1func()
        c2.a1func()
        c2.b1func()
        c2.d1func()
        c4.d1func()
        global.println( "original override tests passed" )
    }

    static testMultiLevel()
    {
        global.println( "----- testMultiLevel -----" )
        ML_Base mb = ML_Base()
        ML_Mid mm = ML_Mid()
        ML_Child mc = ML_Child()
        ML_GrandChild mg = ML_GrandChild()
        global.println( "Base: name=" + mb.name() + " level=" + mb.getLevel().toString() + " calc=" + mb.calc().toString() )
        global.println( "Mid: name=" + mm.name() + " level=" + mm.getLevel().toString() + " calc=" + mm.calc().toString() )
        global.println( "Child: name=" + mc.name() + " level=" + mc.getLevel().toString() + " calc=" + mc.calc().toString() )
        global.println( "Grand: name=" + mg.name() + " level=" + mg.getLevel().toString() + " calc=" + mg.calc().toString() )
        global.println( "chainInfo -> " + mg.chainInfo() )

        # 多态: 父类引用指向子类对象
        ML_Base polyRef = ML_GrandChild()
        global.println( "polyRef.name -> " + polyRef.name() )
        global.println( "polyRef.calc -> " + polyRef.calc().toString() )
    }

    static testAbstract()
    {
        global.println( "----- testAbstract -----" )
        AbsBase ab = AbsBase()
        global.println( "AbsBase.getValue -> " + ab.getValue().toString() )
        global.println( "AbsBase.doubled -> " + ab.doubled().toString() )

        AbsChild ac = AbsChild()
        global.println( "AbsChild.getValue -> " + ac.getValue().toString() )
        global.println( "AbsChild.describe -> " + ac.describe() )
        global.println( "AbsChild.doubled -> " + ac.doubled().toString() )

        AbsGrandChild agc = AbsGrandChild()
        global.println( "AbsGrandChild.getValue -> " + agc.getValue().toString() )
        global.println( "AbsGrandChild.describe -> " + agc.describe() )
        global.println( "AbsGrandChild.doubled -> " + agc.doubled().toString() )

        # 多态: 抽象基类引用指向子类
        AbsBase polyRef = AbsGrandChild()
        global.println( "polyRef.getValue -> " + polyRef.getValue().toString() )
        global.println( "polyRef.describe -> " + polyRef.describe() )
    }

    static testFinal()
    {
        global.println( "----- testFinal -----" )
        FinalBase fb = FinalBase()
        global.println( "FinalBase.process -> " + fb.process().toString() )
        global.println( "FinalBase.lockedMethod -> " + fb.lockedMethod().toString() )

        FinalChild fc = FinalChild()
        global.println( "FinalChild.process -> " + fc.process().toString() )
        global.println( "FinalChild.callLocked -> " + fc.callLocked().toString() )

        FinalGrandChild fgc = FinalGrandChild()
        global.println( "FinalGrandChild.process -> " + fgc.process().toString() )
        global.println( "FinalGrandChild.callChain -> " + fgc.callChain().toString() )
    }

    static testParamCompat()
    {
        global.println( "----- testParamCompat -----" )
        ParamCompatBase pcb = ParamCompatBase()
        global.println( "base.compute(5) -> " + pcb.compute( 5 ).toString() )
        global.println( "base.compute(5, 20) -> " + pcb.compute( 5, 20 ).toString() )
        global.println( "base.compute(1, 2, 3) -> " + pcb.compute( 1, 2, 3 ).toString() )
        global.println( "base.format() -> " + pcb.format() )
        global.println( "base.format('<', '>') -> " + pcb.format( "<", ">" ) )

        ParamCompatChild pcc = ParamCompatChild()
        global.println( "child.compute(5) -> " + pcc.compute( 5 ).toString() )
        global.println( "child.compute(1, 2, 3) -> " + pcc.compute( 1, 2, 3 ).toString() )
        global.println( "child.format() -> " + pcc.format() )
        global.println( "child.format('<','>') -> " + pcc.format( "<", ">" ) )

        # 子类新增重载测试
        ParamExtendChild pec = ParamExtendChild()
        global.println( "extend.compute(1,2,3,4) -> " + pec.compute( 1, 2, 3, 4 ).toString() )
        global.println( "extend.compute('a','b') -> " + pec.compute( "a", "b" ) )
        # 父类方法仍可用
        global.println( "extend.compute(5) -> " + pec.compute( 5 ).toString() )
    }

    static testPolymorphism()
    {
        global.println( "----- testPolymorphism -----" )
        PolyBase[] arr = [ PolyChild1( 10 ), PolyChild2( 20 ), PolyBase( 30 ) ]
        for item in arr
        {
            global.println( item.tag() + " calc=" + item.calc().toString() )
        }
    }

    static testCtorInherit()
    {
        global.println( "----- testCtorInherit -----" )
        CtorBase cbDefault = CtorBase()
        global.println( cbDefault.describe() )

        CtorBase cbArgs = CtorBase( 5, 6 )
        global.println( cbArgs.describe() )

        CtorBase cbSingle = CtorBase( 7 )
        global.println( cbSingle.describe() )

        CtorChild ccFull = CtorChild( 10, 20, 30 )
        global.println( ccFull.describe() )

        CtorChild ccSingle = CtorChild( 99 )
        global.println( ccSingle.describe() )

        # 多态: 父类引用指向子类
        CtorBase polyRef = CtorChild( 1, 2, 3 )
        global.println( "polyRef -> " + polyRef.describe() )
    }

    static testCrossNamespace()
    {
        global.println( "----- testCrossNamespace -----" )
        NS_MF2.Base.Counter counter = NS_MF2.Derived.FastCounter()
        counter.inc()
        counter.inc()
        global.println( "counter.label -> " + counter.label() )
        global.println( "counter.get -> " + counter.get().toString() )
    }

    static testMethodChain()
    {
        global.println( "----- testMethodChain -----" )
        ChainTop top = ChainTop()
        global.println( "chain build -> " + top.build() )
    }

    static fun()
    {
        global.println( "========== MemberFunction2Test (start) ==========" )
        MemberFunction2Test.testOriginal()
        MemberFunction2Test.testMultiLevel()
        MemberFunction2Test.testAbstract()
        MemberFunction2Test.testFinal()
        MemberFunction2Test.testParamCompat()
        MemberFunction2Test.testPolymorphism()
        MemberFunction2Test.testCtorInherit()
        MemberFunction2Test.testCrossNamespace()
        MemberFunction2Test.testMethodChain()
        global.println( "========== MemberFunction2Test (end) ==========" )
    }
}

#!
测试规则说明：

1. override/final/static 基础:
   - override 在父类 = 虚方法(可覆盖)
   - final 在父类 = 不可覆盖
   - override 在子类 = 覆盖父类虚方法
   - final override = 最终覆盖(不可再覆盖)
   - 无 override 标记覆盖 = 编译错误
   - static/virtual 不能隐藏已有实例方法

2. 多级继承 (4级):
   - ML_Base -> ML_Mid -> ML_Child -> ML_GrandChild
   - 每级覆盖 name() 和 calc()
   - base.calc() 链式调用
   - 多态: 父类引用指向子类对象

3. interface 方法 (抽象):
   - 类内 interface 方法 = 有默认实现的虚方法
   - interface 无方法体 = 纯抽象, 子类必须实现
   - 子类用 interface 覆盖 (非 override)
   - 多级 interface 覆盖
   - 多态调用

4. final 方法:
   - final 方法不可被覆盖
   - final override = 覆盖后锁定
   - 子类可通过 base 调用 final 方法

5. 参数兼容性:
   - 父类默认参数, 子类 override 保持签名一致
   - 子类可新增重载(不覆盖父类方法)
   - 重载方法参数个数/类型不同
   - 父类方法仍可用

6. 多态调用:
   - 父类引用数组存放子类对象
   - for-in 遍历, 动态分发

7. 构造函数继承:
   - base._init_(...) 调用父类构造
   - 构造函数重载
   - 子类构造选择不同父类构造
   - 多态: 父类引用指向子类构造结果

8. 跨命名空间继承:
   - NS.Base.Counter 被 NS.Derived.FastCounter 继承
   - 子类覆盖父类方法
   - 父类引用指向子类对象(多态)

9. 方法调用链:
   - base.build() 在继承链中级联
   - ChainTop -> ChainMid -> ChainBase
!#
