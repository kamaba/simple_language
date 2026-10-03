
# ============================================================
# MemberFunction1Test - 成员函数基础与传参全面测试
# 覆盖：基础函数、函数重载、默认参数、关键字参数(新)、
#       this/base调用、递归、interface方法
# ============================================================


# ============================================================
# Section 1: 基础函数定义与调用
# ============================================================
namespace MFT{
class BasicFuncClass
{
    int val = 0

    _init_( int v = 0 )
    {
        this.val = v
    }

    # 无参函数 - 无返回值
    void sayHello()
    {
        global.println( "Hello, BasicFuncClass!" )
    }

    # 无参函数 - 有返回值
    int getVal()
    {
        ret this.val
    }

    # 单参函数
    void setVal( int v )
    {
        this.val = v
    }

    # 多参函数 - int 返回值
    int add( int a, int b )
    {
        ret a + b
    }

    # 多参函数 - string 返回值
    string greet( string greeting, string name )
    {
        ret greeting + ", " + name + "!"
    }

    # 多参函数 - bool 返回值
    bool isGreater( int a, int b )
    {
        ret a > b
    }

    # 多参函数 - double 返回值
    double multiply( double a, double b )
    {
        ret a * b
    }

    # void 函数带多参
    void printSum( int a, int b )
    {
        global.println( "sum = " + ( a + b ).toString() )
    }
}


# ============================================================
# Section 2: 函数重载
# ============================================================
class OverloadClass
{
    string log = ""

    # 重载1: 无参
    void append()
    {
        this.log = this.log + "[empty]"
    }

    # 重载2: 单参 int
    void append( int n )
    {
        this.log = this.log + "[int:" + n.toString() + "]"
    }

    # 重载3: 单参 string
    void append( string s )
    {
        this.log = this.log + "[str:" + s + "]"
    }

    # 重载4: 双参 int + string
    void append( int n, string s )
    {
        this.log = this.log + "[int:" + n.toString() + ",str:" + s + "]"
    }

    # 重载5: 双参 string + int
    void append( string s, int n )
    {
        this.log = this.log + "[str:" + s + ",int:" + n.toString() + "]"
    }

    # 重载6: 三参
    void append( int a, int b, int c )
    {
        this.log = this.log + "[3int:" + ( a + b + c ).toString() + "]"
    }

    string getLog()
    {
        ret this.log
    }

    void clear()
    {
        this.log = ""
    }
}


# ============================================================
# Section 3: 默认参数
# ============================================================
class DefaultParamClass
{
    # 单个默认参数
    int square( int x = 5 )
    {
        ret x * x
    }

    # 多个默认参数
    int power( int base222 = 2, int exp = 3 )
    {
        int resu = 1
        int i = 0
        while i < exp
        {
            resu = resu * base222
            i = i + 1
        }
        ret resu
    }

    # 默认参数为字符串字面量
    string greet( string name = "World", string greeting = "Hello" )
    {
        ret greeting + ", " + name + "!"
    }

    # 默认参数为布尔值
    string format( int num, bool showSign = false )
    {
        if showSign
        {
            if num >= 0 { ret "+" + num.toString() }
            else { ret num.toString() }
        }
        else
        {
            ret num.toString()
        }
    }

    # 默认参数为复杂表达式
    int calc( int a = 10 + 5, int b = 3 * 4 )
    {
        ret a + b
    }

    # 必选参数 + 默认参数混合
    int divide( int a, int b = 2 )
    {
        ret a / b
    }

    # 必选 + 多个默认
    string info( int id, string name = "unknown", bool active = true )
    {
        ret "id=" + id.toString() + " name=" + name + " active=" + active.toString()
    }
}


# ============================================================
# Section 4: 关键字参数 (新特性 - Python风格)
# 使用 defineName = paramInstance 格式
# ============================================================
class KeywordArgClass
{
    # 基础方法用于关键字参数测试
    int add( int a, int b, int c )
    {
        ret a + b + c
    }

    int subtract( int a, int b )
    {
        ret a - b
    }

    string format( string prefix, string name, string suffix )
    {
        ret prefix + name + suffix
    }

    # 带默认参数的方法 + 关键字参数
    string greet( string greeting = "Hello", string name = "World", string punct = "!" )
    {
        ret greeting + ", " + name + punct
    }

    int compute( int x, int y = 10, int z = 20 )
    {
        ret x * 100 + y * 10 + z
    }

    # 方法间调用使用关键字参数
    int calcTotal( int base11, int tax = 0, int discount = 0 )
    {
        ret base11 + tax - discount
    }

    # 嵌套调用验证
    int outerFunc( int a, int b )
    {
        ret this.innerFunc( x = a + b, y = a - b )
    }

    int innerFunc( int x, int y )
    {
        ret x * y
    }
}


# ============================================================
# Section 5: 函数调用方式 (this / base / direct)
# ============================================================
class CallStyleBase
{
    int baseVal = 100

    int getValue()
    {
        ret this.baseVal
    }

    string describe()
    {
        ret "CallStyleBase val=" + this.baseVal.toString()
    }

    int compute()
    {
        ret this.getValue() + 1
    }
}

class CallStyleChild extends CallStyleBase
{
    int childVal = 200

    override int getValue()
    {
        ret this.childVal + base.baseVal
    }

    override string describe()
    {
        ret "CallStyleChild child=" + this.childVal.toString() + " base=" + base.baseVal.toString()
    }

    int combinedCompute()
    {
        # 直接调用本类方法
        int v1 = this.getValue()
        # 通过 this 调用本类方法
        int v2 = this.getValue()
        # 通过 base 调用父类方法
        int v3 = base.getValue()
        # 调用父类未被覆盖的方法
        int v4 = base.compute()
        ret v1 + v2 + v3 + v4
    }
}


# ============================================================
# Section 6: 递归调用
# ============================================================
class RecursiveClass
{
    # 直接递归 - 阶乘
    int factorial( int n )
    {
        if n <= 1 { ret 1 }
        ret n * this.factorial( n - 1 )
    }

    # 直接递归 - 斐波那契
    int fib( int n )
    {
        if n <= 0 { ret 0 }
        if n == 1 { ret 1 }
        ret this.fib( n - 1 ) + this.fib( n - 2 )
    }

    # 间接递归 - 偶数判断调用奇数判断
    bool isEven( int n )
    {
        if n == 0 { ret true }
        ret this.isOdd( n - 1 )
    }

    bool isOdd( int n )
    {
        if n == 0 { ret false }
        ret this.isEven( n - 1 )
    }

    # 递归求和
    int sumTo( int n )
    {
        if n <= 0 { ret 0 }
        ret n + this.sumTo( n - 1 )
    }

    # 递归幂运算
    int pow( int base111, int exp )
    {
        if exp <= 0 { ret 1 }
        ret base111 * this.pow( base111, exp - 1 )
    }
}


# ============================================================
# Section 7: 原有 interface 方法测试 (保留并扩展)
# ============================================================
Application.CI2
{
}

C22 extends Application.CI2
{
    Y = 10
    int getback(){ ret 100 }
    int C22222b(){ ret this.Y }
}

C23 extends C22
{
    M = 100
    override int C22222b(){ ret this.M }
}

interface CI3
{
    object C32222()
}

Application.C3 extends C22 interface Application.CI2, CI3
{
    int C2(){ ret 100 }
    override object C32222(){ ret Object() }
}

Application.C34 extends C22 interface CI3
{
    override object C32222(){ ret null }
}


# ============================================================
# Section 8: 不同命名空间定义的函数调用
# ============================================================
namespace NS_MF1.Outer
{
    class MathUtility
    {
        static int max( int a, int b )
        {
            if a >= b { ret a }
            ret b
        }

        static int min( int a, int b )
        {
            if a <= b { ret a }
            ret b
        }

        static int abs( int n )
        {
            if n < 0 { ret -n }
            ret n
        }
    }
}


# ============================================================
# 测试入口
# ============================================================
MemberFunction1Test
{
    static testBasicFunc()
    {
        global.println( "----- testBasicFunc -----" )
        BasicFuncClass obj = BasicFuncClass( 42 )
        obj.sayHello()
        global.println( "getVal -> " + obj.getVal().toString() )
        obj.setVal( 99 )
        global.println( "setVal(99) getVal -> " + obj.getVal().toString() )
        global.println( "add(3,5) -> " + obj.add( 3, 5 ).toString() )
        global.println( "greet -> " + obj.greet( "Hi", "SLang" ) )
        global.println( "isGreater(10,3) -> " + obj.isGreater( 10, 3 ).toString() )
        global.println( "multiply(2.5d, 4.0d) -> " + obj.multiply( 2.5d, 4.0d ).toString() )
        obj.printSum( 7, 8 )
    }

    static testOverload()
    {
        global.println( "----- testOverload -----" )
        OverloadClass obj = OverloadClass()
        obj.append()
        obj.append( 10 )
        obj.append( "text" )
        obj.append( 1, "two" )
        obj.append( "three", 3 )
        obj.append( 4, 5, 6 )
        global.println( "overload log -> " + obj.getLog() )
    }

    static testDefaultParam()
    {
        global.println( "----- testDefaultParam -----" )
        DefaultParamClass obj = DefaultParamClass()
        global.println( "square() -> " + obj.square().toString() )
        global.println( "square(7) -> " + obj.square( 7 ).toString() )
        global.println( "power() -> " + obj.power().toString() )
        global.println( "power(3) -> " + obj.power( 3 ).toString() )
        global.println( "power(3,4) -> " + obj.power( 3, 4 ).toString() )
        global.println( "greet() -> " + obj.greet() )
        global.println( "greet('SLang') -> " + obj.greet( "SLang" ) )
        global.println( "greet('Hi','World') -> " + obj.greet( "Hi", "World" ) )
        global.println( "format(-5, true) -> " + obj.format( -5, true ) )
        global.println( "format(42, false) -> " + obj.format( 42, false ) )
        global.println( "calc() -> " + obj.calc().toString() )
        global.println( "calc(20) -> " + obj.calc( 20 ).toString() )
        global.println( "calc(20, 100) -> " + obj.calc( 20, 100 ).toString() )
        global.println( "divide(10) -> " + obj.divide( 10 ).toString() )
        global.println( "divide(10, 3) -> " + obj.divide( 10, 3 ).toString() )
        global.println( "info(1) -> " + obj.info( 1 ) )
        global.println( "info(2,'Alice') -> " + obj.info( 2, "Alice" ) )
        global.println( "info(3,'Bob',false) -> " + obj.info( 3, "Bob", false ) )
    }

    static testKeywordArg()
    {
        global.println( "----- testKeywordArg -----" )
        KeywordArgClass obj = KeywordArgClass()

        # --- 全部使用关键字参数 (顺序打乱) ---
        global.println( "add(c=3, a=1, b=2) -> " + obj.add( c = 3, a = 1, b = 2 ).toString() )
        global.println( "add(b=2, a=1, c=3) -> " + obj.add( b = 2, a = 1, c = 3 ).toString() )
        global.println( "subtract(b=5, a=10) -> " + obj.subtract( b = 5, a = 10 ).toString() )

        # --- 位置参数 + 关键字参数混合 ---
        global.println( "add(1, b=2, c=3) -> " + obj.add( 1, b = 2, c = 3 ).toString() )
        global.println( "add(1, 2, c=3) -> " + obj.add( 1, 2, c = 3 ).toString() )
        global.println( "subtract(10, b=3) -> " + obj.subtract( 10, b = 3 ).toString() )

        # --- 关键字参数值为表达式 ---
        global.println( "add(a=1+0, b=2*1, c=3+0) -> " + obj.add( a = 1 + 0, b = 2 * 1, c = 3 + 0 ).toString() )

        # --- 关键字参数 + 默认参数组合 ---
        global.println( "greet(name='SLang') -> " + obj.greet( name = "SLang" ) )
        global.println( "greet(greeting='Hi') -> " + obj.greet( greeting = "Hi" ) )
        global.println( "greet(punct='?') -> " + obj.greet( punct = "?" ) )
        global.println( "greet(name='World', greeting='Hey') -> " + obj.greet( name = "World", greeting = "Hey" ) )
        global.println( "greet(punct='!!', name='SLang', greeting='Hi') -> " + obj.greet( punct = "!!", name = "SLang", greeting = "Hi" ) )

        # --- 关键字参数省略默认值参数 ---
        global.println( "compute(x=1) -> " + obj.compute( x = 1 ).toString() )
        global.println( "compute(x=1, z=5) -> " + obj.compute( x = 1, z = 5 ).toString() )
        global.println( "compute(x=1, y=2) -> " + obj.compute( x = 1, y = 2 ).toString() )
        global.println( "compute(y=2, x=1, z=3) -> " + obj.compute( y = 2, x = 1, z = 3 ).toString() )

        # --- 位置 + 关键字 + 默认混合 ---
        global.println( "compute(1, z=5) -> " + obj.compute( 1, z = 5 ).toString() )
        global.println( "compute(1, y=2) -> " + obj.compute( 1, y = 2 ).toString() )

        # --- 关键字参数在方法间调用 ---
        global.println( "calcTotal(base=100, tax=10, discount=5) -> " + obj.calcTotal( base11 = 100, tax = 10, discount = 5 ).toString() )
        global.println( "calcTotal(discount=20, base=200) -> " + obj.calcTotal( discount = 20, base11 = 200 ).toString() )
        global.println( "calcTotal(150, discount=10) -> " + obj.calcTotal( 150, discount = 10 ).toString() )

        # --- 嵌套调用中使用关键字参数 ---
        global.println( "outerFunc(5, 3) -> " + obj.outerFunc( 5, 3 ).toString() )

        # --- format 三个必选参数全用关键字 ---
        global.println( "format(suffix='!', name='SLang', prefix='[') -> " + obj.format( suffix = "!", name = "SLang", prefix = "[" ) )

        # 错误用例 (编译期应报错，注释掉以保持文件可编译):
        # obj.add( a = 1, 2, 3 )                          # Error: positional argument follows keyword argument
        # obj.add( a = 1, a = 2, b = 3 )                   # Error: multiple values for parameter 'a'
        # obj.add( d = 1, e = 2, f = 3 )                   # Error: no parameter named 'd'
    }

    static testCallStyle()
    {
        global.println( "----- testCallStyle -----" )
        CallStyleChild obj = CallStyleChild()
        global.println( "getValue -> " + obj.getValue().toString() )
        global.println( "describe -> " + obj.describe() )
        global.println( "combinedCompute -> " + obj.combinedCompute().toString() )
    }

    static testRecursive()
    {
        global.println( "----- testRecursive -----" )
        RecursiveClass obj = RecursiveClass()
        global.println( "factorial(5) -> " + obj.factorial( 5 ).toString() )
        global.println( "fib(10) -> " + obj.fib( 10 ).toString() )
        global.println( "isEven(10) -> " + obj.isEven( 10 ).toString() )
        global.println( "isOdd(7) -> " + obj.isOdd( 7 ).toString() )
        global.println( "sumTo(100) -> " + obj.sumTo( 100 ).toString() )
        global.println( "pow(2,10) -> " + obj.pow( 2, 10 ).toString() )
    }

    static testCrossNamespace()
    {
        global.println( "----- testCrossNamespace -----" )
        global.println( "max(3,7) -> " + NS_MF1.Outer.MathUtility.max( 3, 7 ).toString() )
        global.println( "min(3,7) -> " + NS_MF1.Outer.MathUtility.min( 3, 7 ).toString() )
        global.println( "abs(-5) -> " + NS_MF1.Outer.MathUtility.abs( -5 ).toString() )
    }

    static testInterfaceMethod()
    {
        global.println( "----- testInterfaceMethod -----" )
        C22 c22 = C22()
        global.println( "C22.C2 -> " + c22.C22222b().toString() )
        global.println( "C22.getback -> " + c22.getback().toString() )
        C23 c23 = C23()
        global.println( "C23.C2 -> " + c23.C22222b().toString() )
        Application.C3 ac3 = Application.C3()
        global.println( "App.C3.C2 -> " + ac3.C22222b().toString() )
    }

    static fun()
    {
        global.println( "========== MemberFunction1Test (start) ==========" )
        MemberFunction1Test.testBasicFunc()
        MemberFunction1Test.testOverload()
        MemberFunction1Test.testDefaultParam()
        MemberFunction1Test.testKeywordArg()
        MemberFunction1Test.testCallStyle()
        MemberFunction1Test.testRecursive()
        MemberFunction1Test.testCrossNamespace()
        MemberFunction1Test.testInterfaceMethod()
        global.println( "========== MemberFunction1Test (end) ==========" )
    }
}
}
#!
测试规则说明：

1. 基础函数: 无参/单参/多参, 返回 int/string/bool/double/void
2. 函数重载: 参数个数不同, 参数类型不同, 参数顺序不同
3. 默认参数: 单个/多个默认值, 复杂表达式默认值, 必选+默认混合, 省略调用
4. 关键字参数 (Python风格):
   - 全部关键字 (顺序可乱): func(c=3, a=1, b=2)
   - 位置+关键字混合: func(1, b=2, c=3)
   - 关键字+默认参数: func(name='x') 省略有默认值的参数
   - 关键字值为表达式: func(a=1+0, b=2*1)
   - 关键字值为方法调用: 在方法间调用时使用
   - 错误1: 位置参数跟在关键字参数后面 (编译期报错)
   - 错误2: 同名参数重复传递 (编译期报错)
   - 错误3: 未知参数名 (编译期报错)
5. 函数调用: 直接 fun() / this.fun() / base.fun()
6. 递归: 直接递归(阶乘/斐波那契/幂) / 间接递归(isEven/isOdd)
7. interface 方法: 类内 interface 声明, 子类用 interface 覆盖
8. 跨命名空间: NS.Outer.MathUtility.max() 静态函数调用
!#
