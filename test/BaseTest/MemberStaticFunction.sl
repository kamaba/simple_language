
# ============================================================
# MemberStaticFunction - 静态函数全面测试
# 覆盖：基础静态函数、重载、默认参数、关键字参数(新)、
#       静态工厂、继承中的静态函数、toString/hashCode override
# ============================================================


# ============================================================
# Section 1: 原有 override/toString/hashCode 测试 (保留并扩展)
# ============================================================
VClass
{
    fun()
    {
        System.Console.Write( "vclass fun" )
    }

    override get int hashCode()
    {
        ret 100
    }

    override string toString()
    {
        ret "VClass.toString()"
    }

    static staticFun()
    {
        global.println( "VClass.staticFun called" )
    }

    final finalFun()
    {
    }
}

OClass extends VClass
{
    override fun()
    {
        System.Console.Write( "oclass fun" )
    }

    override get int hashCode()
    {
        ret 1001
    }

    override string toString()
    {
        ret "OClass.toString()"
    }

    static staticFun()
    {
        global.println( "OClass.staticFun called" )
    }
}


# ============================================================
# Section 2: 基础静态函数
# ============================================================
class StaticBasic
{
    static int count = 0

    # 无参无返回值静态函数
    static void greet()
    {
        global.println( "Hello from StaticBasic!" )
    }

    # 无参有返回值
    static int getCount()
    {
        ret StaticBasic.count
    }

    # 单参函数
    static int square( int n )
    {
        ret n * n
    }

    # 多参函数
    static int add( int a, int b )
    {
        ret a + b
    }

    # 返回 string
    static string repeat( string s, int n )
    {
        string result = ""
        int i = 0
        while i < n
        {
            result = result + s
            i = i + 1
        }
        ret result
    }

    # 返回 bool
    static bool isPositive( int n )
    {
        ret n > 0
    }

    # 返回 double
    static double circleArea( double r )
    {
        ret 3.14159d * r * r
    }

    # 静态函数操作静态变量
    static void increment()
    {
        StaticBasic.count = StaticBasic.count + 1
    }

    static void reset()
    {
        StaticBasic.count = 0
    }
}


# ============================================================
# Section 3: 静态函数重载
# ============================================================
class StaticOverload
{
    # 重载1: 无参
    static string format()
    {
        ret "[empty]"
    }

    # 重载2: int 单参
    static string format( int n )
    {
        ret "[int:" + n.toString() + "]"
    }

    # 重载3: string 单参
    static string format( string s )
    {
        ret "[str:" + s + "]"
    }

    # 重载4: 双参 int + string
    static string format( int n, string s )
    {
        ret "[" + n.toString() + ":" + s + "]"
    }

    # 重载5: 双参 string + int
    static string format( string s, int n )
    {
        ret "[" + s + ":" + n.toString() + "]"
    }

    # 重载6: 三参
    static string format( int a, int b, int c )
    {
        ret "[" + ( a + b + c ).toString() + "]"
    }
}


# ============================================================
# Section 4: 静态函数 + 默认参数
# ============================================================
class StaticDefault
{
    # 单个默认参数
    static int inc( int n = 1 )
    {
        ret n + 1
    }

    # 多个默认参数
    static int muladd( int a = 1, int b = 2, int c = 3 )
    {
        ret a * b + c
    }

    # 必选 + 默认
    static string greet( string name, string greeting = "Hello" )
    {
        ret greeting + ", " + name + "!"
    }

    # 默认值为表达式
    static int calc( int x = 5 + 5, int y = 2 * 3 )
    {
        ret x - y
    }

    # bool 默认值
    static string format( int n, bool hex = false )
    {
        if hex
        {
            ret "0x" + n.toString()
        }
        else
        {
            ret n.toString()
        }
    }

    # 全默认参数
    static string config( string host = "localhost", int port = 8080, bool ssl = false )
    {
        string proto = "http"
        if ssl { proto = "https" }
        ret proto + "://" + host + ":" + port.toString()
    }
}


# ============================================================
# Section 5: 静态函数 + 关键字参数 (新特性)
# ============================================================
class StaticKeyword
{
    # 基础方法用于关键字参数测试
    static int add( int a, int b, int c )
    {
        ret a + b + c
    }

    static int subtract( int a, int b )
    {
        ret a - b
    }

    # 带默认参数 + 关键字参数
    static int compute( int x, int y = 10, int z = 20 )
    {
        ret x * 100 + y * 10 + z
    }

    static string greet( string greeting = "Hello", string name = "World", string punct = "!" )
    {
        ret greeting + ", " + name + punct
    }

    # 静态函数调用其他静态函数时使用关键字参数
    static int calc( int base, int tax = 0, int discount = 0 )
    {
        ret StaticKeyword.add( a = base, b = tax, c = -discount )
    }

    # 静态函数调用实例方法(通过对象参数)
    static int processObj( StaticBasic obj, int multiplier = 1 )
    {
        ret obj.square( multiplier )
    }
}


# ============================================================
# Section 6: 静态函数与继承
# ============================================================
class StaticBase
{
    static int baseVal = 100

    static int getBase()
    {
        ret StaticBase.baseVal
    }

    static string tag()
    {
        ret "StaticBase"
    }

    static int doubled( int n )
    {
        ret n * 2
    }
}

class StaticChild extends StaticBase
{
    static int childVal = 200

    # 静态方法 - 不override, 而是隐藏父类同名方法
    static string tag()
    {
        ret "StaticChild"
    }

    # 调用父类静态方法
    static int getCombined()
    {
        ret StaticBase.getBase() + StaticChild.childVal
    }

    # 静态方法使用父类静态方法
    static int quadruple( int n )
    {
        ret StaticBase.doubled( StaticBase.doubled( n ) )
    }
}


# ============================================================
# Section 7: 静态工厂方法
# ============================================================
data ProductData
{
    int id = 0
    string name = ""
    double price = 0.0d
}

class ProductFactory
{
    # 工厂方法1: 默认创建
    static ProductData create()
    {
        ret ProductData()
    }

    # 工厂方法2: 指定id
    static ProductData create( int id )
    {
        ProductData p = ProductData()
        p.id = id
        ret p
    }

    # 工厂方法3: 全参数
    static ProductData create( int id, string name, double price )
    {
        ProductData p = ProductData()
        p.id = id
        p.name = name
        p.price = price
        ret p
    }

    # 工厂方法 + 默认参数
    static ProductData make( int id = 0, string name = "default", double price = 0.0d )
    {
        ret ProductData(){ id = id, name = name, price = price }
    }

    # 工厂方法 + 关键字参数
    static ProductData build( int id, string name, string category )
    {
        ProductData p = ProductData()
        p.id = id
        p.name = "[" + category + "] " + name
        ret p
    }
}


# ============================================================
# Section 8: 静态变量与静态函数交互
# ============================================================
class StaticCounter
{
    static int total = 0
    static int max = 0

    static void add( int n )
    {
        StaticCounter.total = StaticCounter.total + n
        if StaticCounter.total > StaticCounter.max
        {
            StaticCounter.max = StaticCounter.total
        }
    }

    static int getTotal()
    {
        ret StaticCounter.total
    }

    static int getMax()
    {
        ret StaticCounter.max
    }

    static void reset()
    {
        StaticCounter.total = 0
        StaticCounter.max = 0
    }
}

# 静态递归
class StaticMath
{
    static int factorial( int n )
    {
        if n <= 1 { ret 1 }
        ret n * StaticMath.factorial( n - 1 )
    }

    static int fib( int n )
    {
        if n <= 0 { ret 0 }
        if n == 1 { ret 1 }
        ret StaticMath.fib( n - 1 ) + StaticMath.fib( n - 2 )
    }

    static int gcd( int a, int b )
    {
        if b == 0 { ret a }
        ret StaticMath.gcd( b, a % b )
    }

    static int lcm( int a, int b )
    {
        if a == 0 || b == 0 { ret 0 }
        ret ( a / StaticMath.gcd( a, b ) ) * b
    }
}


# ============================================================
# 测试入口
# ============================================================
MemberStaticFunction
{
    static testOverride()
    {
        global.println( "----- testOverride -----" )
        VClass oclass = OClass()
        oclass.fun()
        global.println( "" )
        oc2 = OClass()
        global.println( "oc2.toString -> " + oc2.toString() )
        global.println( "oc2.hashCode -> " + oc2.hashCode.toString() )

        # 静态方法调用
        VClass.staticFun()
        OClass.staticFun()
    }

    static testBasic()
    {
        global.println( "----- testBasic -----" )
        StaticBasic.greet()
        StaticBasic.reset()
        StaticBasic.increment()
        StaticBasic.increment()
        StaticBasic.increment()
        global.println( "count -> " + StaticBasic.getCount().toString() )
        global.println( "square(7) -> " + StaticBasic.square( 7 ).toString() )
        global.println( "add(3,5) -> " + StaticBasic.add( 3, 5 ).toString() )
        global.println( "repeat('ab',3) -> " + StaticBasic.repeat( "ab", 3 ) )
        global.println( "isPositive(-5) -> " + StaticBasic.isPositive( -5 ).toString() )
        global.println( "circleArea(2.0d) -> " + StaticBasic.circleArea( 2.0d ).toString() )
    }

    static testOverload()
    {
        global.println( "----- testOverload -----" )
        global.println( StaticOverload.format() )
        global.println( StaticOverload.format( 42 ) )
        global.println( StaticOverload.format( "hello" ) )
        global.println( StaticOverload.format( 1, "two" ) )
        global.println( StaticOverload.format( "three", 3 ) )
        global.println( StaticOverload.format( 4, 5, 6 ) )
    }

    static testDefault()
    {
        global.println( "----- testDefault -----" )
        global.println( "inc() -> " + StaticDefault.inc().toString() )
        global.println( "inc(10) -> " + StaticDefault.inc( 10 ).toString() )
        global.println( "muladd() -> " + StaticDefault.muladd().toString() )
        global.println( "muladd(5) -> " + StaticDefault.muladd( 5 ).toString() )
        global.println( "muladd(5,10) -> " + StaticDefault.muladd( 5, 10 ).toString() )
        global.println( "muladd(1,2,3) -> " + StaticDefault.muladd( 1, 2, 3 ).toString() )
        global.println( "greet('SLang') -> " + StaticDefault.greet( "SLang" ) )
        global.println( "greet('SLang','Hi') -> " + StaticDefault.greet( "SLang", "Hi" ) )
        global.println( "calc() -> " + StaticDefault.calc().toString() )
        global.println( "calc(20) -> " + StaticDefault.calc( 20 ).toString() )
        global.println( "format(255, true) -> " + StaticDefault.format( 255, true ) )
        global.println( "format(255, false) -> " + StaticDefault.format( 255, false ) )
        global.println( "config() -> " + StaticDefault.config() )
        global.println( "config('example.com') -> " + StaticDefault.config( "example.com" ) )
        global.println( "config('api.com', 443, true) -> " + StaticDefault.config( "api.com", 443, true ) )
    }

    static testKeyword()
    {
        global.println( "----- testKeyword -----" )

        # --- 全部关键字参数 (顺序打乱) ---
        global.println( "add(c=3, a=1, b=2) -> " + StaticKeyword.add( c = 3, a = 1, b = 2 ).toString() )
        global.println( "add(b=20, c=30, a=10) -> " + StaticKeyword.add( b = 20, c = 30, a = 10 ).toString() )
        global.println( "subtract(b=5, a=10) -> " + StaticKeyword.subtract( b = 5, a = 10 ).toString() )

        # --- 位置 + 关键字混合 ---
        global.println( "add(1, b=2, c=3) -> " + StaticKeyword.add( 1, b = 2, c = 3 ).toString() )
        global.println( "add(1, 2, c=3) -> " + StaticKeyword.add( 1, 2, c = 3 ).toString() )
        global.println( "subtract(10, b=3) -> " + StaticKeyword.subtract( 10, b = 3 ).toString() )

        # --- 关键字 + 默认参数 ---
        global.println( "compute(x=5) -> " + StaticKeyword.compute( x = 5 ).toString() )
        global.println( "compute(x=5, z=0) -> " + StaticKeyword.compute( x = 5, z = 0 ).toString() )
        global.println( "compute(x=1, y=2, z=3) -> " + StaticKeyword.compute( x = 1, y = 2, z = 3 ).toString() )
        global.println( "compute(z=9, x=1, y=2) -> " + StaticKeyword.compute( z = 9, x = 1, y = 2 ).toString() )

        global.println( "greet(name='SLang') -> " + StaticKeyword.greet( name = "SLang" ) )
        global.println( "greet(greeting='Hi') -> " + StaticKeyword.greet( greeting = "Hi" ) )
        global.println( "greet(punct='?', name='World') -> " + StaticKeyword.greet( punct = "?", name = "World" ) )

        # --- 位置 + 关键字 + 默认混合 ---
        global.println( "compute(1, z=5) -> " + StaticKeyword.compute( 1, z = 5 ).toString() )
        global.println( "compute(1, y=2) -> " + StaticKeyword.compute( 1, y = 2 ).toString() )

        # --- 静态函数间调用使用关键字参数 ---
        global.println( "calc(base=100, tax=10, discount=5) -> " + StaticKeyword.calc( base = 100, tax = 10, discount = 5 ).toString() )
        global.println( "calc(discount=50, base=200) -> " + StaticKeyword.calc( discount = 50, base = 200 ).toString() )

        # --- 静态函数调用实例方法(通过对象参数) ---
        StaticBasic obj = StaticBasic()
        global.println( "processObj(obj, multiplier=5) -> " + StaticKeyword.processObj( obj, multiplier = 5 ).toString() )

        # 错误用例 (编译期应报错):
        # StaticKeyword.add( a = 1, 2, 3 )             # Error: positional follows keyword
        # StaticKeyword.add( a = 1, a = 2, b = 3 )      # Error: multiple values for 'a'
        # StaticKeyword.add( d = 1, e = 2, f = 3 )      # Error: no parameter named 'd'
    }

    static testInherit()
    {
        global.println( "----- testInherit -----" )
        global.println( "StaticBase.getBase -> " + StaticBase.getBase().toString() )
        global.println( "StaticBase.tag -> " + StaticBase.tag() )
        global.println( "StaticChild.tag -> " + StaticChild.tag() )
        global.println( "StaticChild.getCombined -> " + StaticChild.getCombined().toString() )
        global.println( "StaticChild.quadruple(3) -> " + StaticChild.quadruple( 3 ).toString() )
        global.println( "StaticBase.doubled(5) -> " + StaticBase.doubled( 5 ).toString() )
    }

    static testFactory()
    {
        global.println( "----- testFactory -----" )
        ProductData p1 = ProductFactory.create()
        global.println( "create() -> id=" + p1.id.toString() + " name=" + p1.name )

        ProductData p2 = ProductFactory.create( 5 )
        global.println( "create(5) -> id=" + p2.id.toString() )

        ProductData p3 = ProductFactory.create( 10, "Widget", 9.99d )
        global.println( "create(10,'Widget',9.99) -> id=" + p3.id.toString() + " name=" + p3.name + " price=" + p3.price.toString() )

        ProductData p4 = ProductFactory.make()
        global.println( "make() -> id=" + p4.id.toString() + " name=" + p4.name )

        ProductData p5 = ProductFactory.make( 20, "Gadget", 14.50d )
        global.println( "make(20,'Gadget') -> id=" + p5.id.toString() + " name=" + p5.name )

        # 工厂方法 + 关键字参数
        ProductData p6 = ProductFactory.build( name = "Thing", id = 99, category = "CAT" )
        global.println( "build(name='Thing', id=99, category='CAT') -> " + p6.name )

        ProductData p7 = ProductFactory.build( id = 1, name = "Item", category = "NEW" )
        global.println( "build(id=1, name='Item', category='NEW') -> " + p7.name )
    }

    static testStaticState()
    {
        global.println( "----- testStaticState -----" )
        StaticCounter.reset()
        StaticCounter.add( 5 )
        StaticCounter.add( 3 )
        StaticCounter.add( 10 )
        global.println( "total -> " + StaticCounter.getTotal().toString() )
        global.println( "max -> " + StaticCounter.getMax().toString() )
    }

    static testStaticMath()
    {
        global.println( "----- testStaticMath -----" )
        global.println( "factorial(6) -> " + StaticMath.factorial( 6 ).toString() )
        global.println( "fib(12) -> " + StaticMath.fib( 12 ).toString() )
        global.println( "gcd(48, 36) -> " + StaticMath.gcd( 48, 36 ).toString() )
        global.println( "lcm(4, 6) -> " + StaticMath.lcm( 4, 6 ).toString() )
    }

    static fun()
    {
        global.println( "========== MemberStaticFunction (start) ==========" )
        MemberStaticFunction.testOverride()
        MemberStaticFunction.testBasic()
        MemberStaticFunction.testOverload()
        MemberStaticFunction.testDefault()
        MemberStaticFunction.testKeyword()
        MemberStaticFunction.testInherit()
        MemberStaticFunction.testFactory()
        MemberStaticFunction.testStaticState()
        MemberStaticFunction.testStaticMath()
        global.println( "========== MemberStaticFunction (end) ==========" )
    }
}

#!
测试规则说明：

1. override/toString/hashCode (保留扩展):
   - 子类 OClass override fun, toString, hashCode
   - 静态方法 VClass.staticFun() vs OClass.staticFun() (隐藏非覆盖)
   - 父类引用指向子类对象的多态调用

2. 基础静态函数:
   - 无参/单参/多参
   - 返回 int/string/bool/double/void
   - 静态函数操作静态变量 (increment/count)

3. 静态函数重载:
   - 参数个数不同
   - 参数类型不同 (int vs string)
   - 参数顺序不同 (int+string vs string+int)
   - 三参重载

4. 静态函数 + 默认参数:
   - 单个/多个默认值
   - 必选 + 默认混合
   - 默认值为表达式
   - bool 默认值
   - 全默认参数

5. 静态函数 + 关键字参数:
   - 全部关键字 (顺序可乱): Class.add(c=3, a=1, b=2)
   - 位置 + 关键字混合: Class.add(1, b=2, c=3)
   - 关键字 + 默认参数: Class.compute(x=5, z=0)
   - 静态函数间调用用关键字: calc(base=100, tax=10)
   - 静态函数通过对象参数调用实例方法

6. 静态函数与继承:
   - 子类定义同名静态方法 (隐藏, 非override)
   - 通过 ClassName.method() 调用指定类的静态方法
   - 子类静态方法调用父类静态方法

7. 静态工厂方法:
   - 多个重载工厂方法
   - 默认参数工厂方法
   - 关键字参数工厂方法: build(name='x', id=1, category='c')
   - data 类型构造

8. 静态变量与静态函数交互:
   - 静态计数器模式
   - max 记录
   - reset 重置

9. 静态递归:
   - factorial, fib
   - gcd (辗转相除), lcm
!#
