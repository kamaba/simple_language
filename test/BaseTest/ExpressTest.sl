import Application

ClassX
{
    ClassXVal = 123321
}

Class1
{
    ClassX xx = new()
    int x = 20

    _init_(ix)
    {
        #x1 = x
        this.xx = ix as ClassX
    }

    Application.Class2 c3 = new(20)

    public static bool ab = 10 <= 10 / (20 + 1 * 35 - (32 / 15))

    int a = 10 + (1 - (3 * (3 / (-Class2.m2 - 100)) * 30)) / 10 * (15 - (22 / 2 + 1)) - 20 / -10

    private static bx = -20

    Application.Class2 c2 = Application.Class2(20)

    int x1 = 10 + (1 - 20) * 33 / 20 + 10 / 20 - 22 * 2 / 1

    static print2(int p1 = int(20 / (11 / 11) + 1), int p2 = int(2 + 20 / (10 * 10)) )
    {
        global.println("p1=" + p1 + " p2=" + p2)
    }

    print1()
    {
        int x2 = 20
        int a2 = this.c2.a
        {
            global.println("p1=" + x2 + " x=" + this.a)
        }
        global.println("aaaaaaaaa")
    }
}

namespace Application
{
        Class2
        {
            Class222
            {
                static a = 20
                int b = 20
            }

            int x = 20
            int a = -20 - -(Class2.x1 + Class2.x2)
            static x1 = (Class2.x2 * 1) + -2
            static x2 = Class2.x3 + 4
            static x3 = 13
            static m2 = 100
            static Class3 mc2 =new(10, 2, 10.0f - 2.0f, (20 + 12) / Class2.x1)
            static a2 = 20.0f + Class3.m / 30 * 100
            static Class3 mc33 = { a = 300, b = 400, c = Class222() }
            public int x22 = 0

            _init_( int x11)
            {
                this.x = x11
                Class2.x1 = this.x
            }

            public Class3 fun1(int pppp1 = int(20 + 11.1), int p2 = 21)
            {
                mc33 = Class3(p2 + 2){ b = 100, a = 20 }
                bool1 = Int16(pppp1 <= this.a + Class222.a) >= 0
                {
                    int b1 = 21
                    Class3 mc22 = new(pppp1, b1, 10.0f - 2.0f, 20 + 12)
                    Class3 m0 = new(20)
                    Class3 mc1 = { a = 30, b = 30, c = Class222() }
                    Class3 mc3 = Class3(20)
                    Class222.a = 10
                    Class3.m += 10
                    Class3.GetClass2().x -= 10
                    this.x22 = 20
                    this.x22 += 10
                    x33 = 30
                    {
                        if (this.x == 35)
                        {
                            global.println("x=" + this.x22)
                            global.println("x= $this.x22 " + x33)
                            ret Class2.mc2
                        }
                    }
                }

                {

                }
                ret null
            }


            static void Init()
            {
                Class3 m = new()
            }
        }

        Class3
        {
            static Class2 class2 = new()
            a = 20
            b = 20
            Class2.Class222 c = new()
            static m = 20
            Class2.Class222 m2 = null

            _init_(x)
            {
                this.a = int(x)
            }

            _init_(x, y, z, d)
            {
                this.a = 20
                this.b = int(x) + int(y) - int(z) + int(d)
            }

            public static Class2 GetClass2()
            {
                ret Class3.class2
            }
        }
}

ExpressTest
{
    static fun()
    {
        global.println("========== ExpressTest (start) ==========")
        
        #!
        ExpressTest.arithmeticExpressionTest()
        ExpressTest.comparisonExpressionTest()
        ExpressTest.logicalExpressionTest()
        !#
        ExpressTest.assignmentExpressionTest()
        #!
        ExpressTest.ternaryExpressionTest()
        ExpressTest.nullCoalescingExpressionTest()
        ExpressTest.stringInterpolationTest()
        ExpressTest.lambdaExpressionTest()
        ExpressTest.typeConversionExpressionTest()
        ExpressTest.memberAccessExpressionTest()
        ExpressTest.arrayIndexExpressionTest()
        ExpressTest.newObjectExpressionTest()
        ExpressTest.staticMemberAccessTest()
        
        ExpressTest.complexNestedExpressionTest()
        !#
        #ExpressTest.classMemberInitTest()
        global.println("========== ExpressTest (end) ==========")
    }

    static arithmeticExpressionTest()
    {
        global.println("----- arithmeticExpressionTest -----")

        int a = 10
        int b = 3
        global.println("a + b = " + (a + b).toString())
        global.println("a - b = " + (a - b).toString())
        global.println("a * b = " + (a * b).toString())
        global.println("a / b = " + (a / b).toString())
        global.println("a % b = " + (a % b).toString())

        int c = -a
        global.println("-a = " + c.toString())

        int d = +a
        global.println("+a = " + d.toString())

        #int e = a++                     #不允许
        #global.println("a++ = " + e.toString() + ", a = " + a.toString())

        #int f = --a                     #不允许 
        #global.println("--a = " + f.toString() + ", a = " + a.toString())

        int g = a + b * 2 - 3 / 1
        global.println("a + b * 2 - 3 / 1 = " + g.toString())

        int h = (a + b) * (2 - 3) / 1
        global.println("(a + b) * (2 - 3) / 1 = " + h.toString())
    }

    static comparisonExpressionTest()
    {
        global.println("----- comparisonExpressionTest -----")

        int a = 10
        int b = 20
        global.println("a == b = " + (a == b).toString())
        global.println("a != b = " + (a != b).toString())
        global.println("a > b = " + (a > b).toString())
        global.println("a < b = " + (a < b).toString())
        global.println("a >= b = " + (a >= b).toString())
        global.println("a <= b = " + (a <= b).toString())

        string s1 = "hello"
        string s2 = "world"
        global.println("s1 == s2 = " + (s1 == s2).toString())
        global.println("s1 != s2 = " + (s1 != s2).toString())
    }

    static logicalExpressionTest()
    {
        global.println("----- logicalExpressionTest -----")

        bool a = true
        bool b = false
        global.println("a && b = " + (a && b).toString())
        global.println("a || b = " + (a || b).toString())
        global.println("!a = " + (!a).toString())
        global.println("!b = " + (!b).toString())

        bool c = a && b || !b
        global.println("a && b || !b = " + c.toString())

        bool d = !(a && b)
        global.println("!(a && b) = " + d.toString())
    }

    static assignmentExpressionTest()
    {
        global.println("----- assignmentExpressionTest -----")

        int a = 10
        a += 5
        global.println("a += 5 = " + a.toString())

        a -= 3
        global.println("a -= 3 = " + a.toString())

        a *= 2
        global.println("a *= 2 = " + a.toString())

        a /= 2
        global.println("a /= 2 = " + a.toString())

        a %= 3
        global.println("a %= 3 = " + a.toString())

        int b = 5
        b <<= 1
        global.println("b[$b.toString() ]<<= 1 = " + b.toString())

        b >>= 1
        global.println("b[$b.toString() ] >>= 1 = " + b.toString())

        b &= 3
        global.println("b[$b.toString() ] &= 3 = " + b.toString())

        b |= 1
        global.println("b[$b ] |= 1 = " + b.toString())

        b ^= 2
        global.println("b[$b ] ^= 2 = " + b.toString())
    }

    static ternaryExpressionTest()
    {
        global.println("----- ternaryExpressionTest -----")

        int a = 10
        int b = 20
        int max = a > b ? a : b
        global.println("max = " + max.toString())

        string result = a > b ? "a is greater" : "b is greater or equal"
        global.println("result = " + result)

        int c = a > 5 ? (b > 15 ? 100 : 50) : 0
        global.println("nested ternary = " + c.toString())
    }

    static nullCoalescingExpressionTest()
    {
        global.println("----- nullCoalescingExpressionTest -----")

        string s1 = null
        string s2 = "hello"
        string result1 = s1 ?? "default"
        string result2 = s2 ?? "default"
        global.println("null ?? default = " + result1)
        global.println("hello ?? default = " + result2)

        int n1 = null
        int n2 = 10
        int result3 = n1 ?? 0
        int result4 = n2 ?? 0
        global.println("null ?? 0 = " + result3.toString())
        global.println("10 ?? 0 = " + result4.toString())
    }

    static stringInterpolationTest()
    {
        global.println("----- stringInterpolationTest -----")

        string name = "World"
        int age = 25
        global.println("Hello, $name !")
        global.println("Age: $age")
        global.println("Next year: ${age + 1}")

        string complex = "Name: $name, Age: $age , Double: ${age * 2}"
        global.println("complex = " + complex)
    }

    static lambdaExpressionTest()
    {
        global.println("----- lambdaExpressionTest -----")

        #!
        func<int, int> square = (x) => x * x
        global.println("square(5) = " + square(5).toString())

        func<int, int, int> add = (a, b) => a + b
        global.println("add(3, 4) = " + add(3, 4).toString())

        func<int, bool> isEven = (x) => x % 2 == 0
        global.println("isEven(4) = " + isEven(4).toString())
        global.println("isEven(5) = " + isEven(5).toString())
        !#
    }

    static typeConversionExpressionTest()
    {
        global.println("----- typeConversionExpressionTest -----")

        int i = 10
        Num f = i as Num
        global.println("10 as Num = " + f.toString())

        Num d = 3.14
        int j = d as int
        global.println("3.14 as int = " + j.toString())

        string s = "123"
        int k = s as int
        global.println("123 as int = " + k.toString())

        object obj = 100
        int l = obj as int
        global.println("object(100) as int = " + l.toString())
    }

    static memberAccessExpressionTest()
    {
        global.println("----- memberAccessExpressionTest -----")

        Application.Class2 c2 = Application.Class2(10)
        global.println("c2.x = " + c2.x.toString())
        global.println("c2.a = " + c2.a.toString())

        Application.Class3 c3 = Application.Class3(1, 2, 3, 4)
        global.println("c3.a = " + c3.a.toString())
        global.println("c3.b = " + c3.b.toString())
    }

    static arrayIndexExpressionTest()
    {
        global.println("----- arrayIndexExpressionTest -----")

        int[] arr = [10, 20, 30, 40, 50]
        global.println("arr[0] = " + arr[0].toString())
        global.println("arr[2] = " + arr[2].toString())
        global.println("arr[4] = " + arr[4].toString())

        arr[1] = 25
        global.println("arr[1] after assignment = " + arr[1].toString())

        int[][] matrix = [[1, 2, 3], [4, 5, 6], [7, 8, 9]]
        global.println("matrix[1][2] = " + matrix[1][2].toString())
    }

    static newObjectExpressionTest()
    {
        global.println("----- newObjectExpressionTest -----")

        Application.Class2 c2 = new()
        global.println("new Class2().x = " + c2.x.toString())

        Application.Class2 c2WithArg = new(100)
        global.println("new Class2(100).x = " + c2WithArg.x.toString())

        Application.Class3 c3 = new(10, 20, 30, 40)
        global.println("new Class3(10,20,30,40).a = " + c3.a.toString())
    }

    static staticMemberAccessTest()
    {
        global.println("----- staticMemberAccessTest -----")

        global.println("Class2.m2 = " + Application.Class2.m2.toString())
        global.println("Class2.x1 = " + Application.Class2.x1.toString())
        global.println("Class2.x2 = " + Application.Class2.x2.toString())
        global.println("Class2.x3 = " + Application.Class2.x3.toString())

        Application.Class2.m2 = 200
        global.println("Class2.m2 after assignment = " + Application.Class2.m2.toString())
    }
    Level<T>
    {
        static T st = new()
        T t = new()
        _init_( T it )
        {
            this.t = it
        }
    }

    static complexNestedExpressionTest()
    {
        global.println("----- complexNestedExpressionTest -----")

        #!
        int a = 10
        int b = 20
        int c = 30
        int result = (a + b) * c / (b - a) + (a < b ? b:a ) - (a > b ? a : b) * 2
        global.println("complex expression result = " + result.toString())

        bool condition = a < b && b > c || a == 10
        global.println("complex bool expression = " + condition.toString())
        
        int nested = ((a + b) * (c - a)) / (b / 2) + (a % b)
        global.println("nested arithmetic = " + nested.toString())
        !#
        Level<int>.st = 100
        Level<short>.st = 200s
        condition2 = Level<int>.st < 10 || Level<short>.st > 20
        global.println("complex2 bool expression = " + condition2.toString())
    }

    static classMemberInitTest()
    {
        global.println("----- classMemberInitTest -----")

        Class1 c1 = new(ClassX())
        global.println("Class1.x = " + c1.x.toString())
        global.println("Class1.a = " + c1.a.toString())
        global.println("Class1.x1 = " + c1.x1.toString())
        global.println("Class1.ab = " + Class1.ab.toString())
        global.println("Class1.bx = " + Class1.bx.toString())
        global.println("Class1.c2.x = " + c1.c2.x.toString())
        global.println("Class1.c2.a = " + c1.c2.a.toString())
        global.println("Class1.c3.x = " + c1.c3.x.toString())
        global.println("Class1.xx.ClassXVal = " + c1.xx.ClassXVal.toString())

        Application.Class2 c2 = Application.Class2(10)
        global.println("Class2.x = " + c2.x.toString())
        global.println("Class2.a = " + c2.a.toString())
        global.println("Class2.x1 = " + Application.Class2.x1.toString())
        global.println("Class2.x2 = " + Application.Class2.x2.toString())
        global.println("Class2.x3 = " + Application.Class2.x3.toString())
        global.println("Class2.m2 = " + Application.Class2.m2.toString())
        global.println("Class2.a2 = " + Application.Class2.a2.toString())
        global.println("Class2.x22 = " + c2.x22.toString())
        global.println("Class2.mc33.a = " + Application.Class2.mc33.a.toString())
        global.println("Class2.mc33.b = " + Application.Class2.mc33.b.toString())

        Application.Class3 c3 = Application.Class3(1, 2, 3, 4)
        global.println("Class3.a = " + c3.a.toString())
        global.println("Class3.b = " + c3.b.toString())
        global.println("Class3.m = " + Application.Class3.m.toString())
        global.println("Class3.class2.x = " + Application.Class3.class2.x.toString())
        global.println("Class3.c.b = " + c3.c.b.toString())
        global.println("Class3.m2 = " + (c3.m2 == null).toString())
    }
}
