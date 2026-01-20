import CSharp.System
import CSharp.SimpleLanguage.Core

NumberTest
{
    static num1Test()
    {
        #floating point / numeric mix tests
        Num a = 1.0;
        num b = 333
        # basic ops
        Num na = -a
        System.Console.WriteLine("Num na: " + na )

        Num nanb = b - -a;
        System.Console.WriteLine("Num nanb: " + nanb )

        Num nanb2 = b + -a;
        System.Console.WriteLine("Num nanb2: " + nanb2 )
        
        Num nanb3 = b + (+a);
        System.Console.WriteLine("Num nanb3: " + nanb3 )

        Num c = a + b; 
        System.Console.WriteLine("Num add: " + c )
        Num s = c - 12.5
        System.Console.WriteLine("Num sub: " + s )
        Num m = c * 2
        System.Console.WriteLine("Num mul: " + m )
        Num dv = c / 2
        System.Console.WriteLine("Num div: " + dv )
        # modulo with integers
        var modv = 100 % 3
        System.Console.WriteLine("mod 100%3=" + modv )

        # mixing long/int with Num
        var d = (c + 100L)/10.0f
        System.Console.WriteLine("Num mix long: " + d )

        # comparisons
        System.Console.WriteLine("c > b: " + (c > b) )
        System.Console.WriteLine("c == (a+b): " + (c == (a + b)) )
        # negative and unary
        Num neg = -c
        System.Console.WriteLine("neg: " + neg )
    }
    static num2Test()
    {        
        #1. int 是 num
        System.Console.WriteLine(1 is num);
        #2. double 是 num
        System.Console.WriteLine(1.0 is num);
        # 3. num 不是 int
        num n = 1.5;
        System.Console.WriteLine( (n isnot int).toString() );

        # 4. num 不是 double（静态）
        num m = 1;
        System.Console.WriteLine(m is int);

        # 5. 运行时类型区分
        num a = 1;
        num b = 1.2;
        #System.Console.WriteLine(a.runtimeType == int);
        #System.Console.WriteLine(b.runtimeType == double);

        # 6. num 不能实例化
        num x = num(); #❌ 编译错误

        #7. int 不能隐式赋给 double
        double d = 1; # ❌

        # 8. double 可以赋给 num
        num x = 3.14;
        #System.Console.WriteLine(x == 3.14);
    }
    static num3Test()
    {
        #!
        # 9. int + int => int
        assert(1 + 2 is int);

        # 10. int + double => double
        assert((1 + 2.0) is double);

        # 11. num + num => num（运行时决定）
        num n1 = 1;
        num n2 = 2.5;
        assert((n1 + n2) == 3.5);

        # 12. / 永远返回 double
        assert((5 / 2) is double);

        # 13. ~/ 返回 int
        assert((5 ~/ 2) == 2);

        # 14. % 取余
        assert(5 % 2 == 1);

        # 15. 负数取余规则
        assert(-5 % 2 == 1);

        # 16. 乘法
        assert(3 * 2.5 == 7.5);

        # 17. 减法
        assert(5 - 2.5 == 2.5);

        # 18. 链式运算
        assert((1 + 2) * 3 == 9);
        !#
    }
    static num4Test()
    {
        #!
        #27. toInt 截断
        #assert(3.7.toInt() == 3);

        #28. floor
        assert(3.7.floor() == 3);

        # 29. ceil
        assert(3.1.ceil() == 4);

        # 30. round
        assert(3.5.round() == 4);

        # 31. truncate
        assert((-3.7).truncate() == -3);

        # 32. num -> double
        #num q = 3;
        assert(q.toDouble() == 3.0);

        # 33. num -> int
        #num w = 3.9;
        assert(w.toInt() == 3);

        # 34. abs
        assert((-5).abs() == 5);

        # 35. Infinity
        assert((1 / 0).isInfinite);

        # 36. -Infinity
        assert((-1 / 0).isInfinite);

        # 37. NaN
        assert((0 / 0).isNaN);

        # 38. isFinite
        assert(100.isFinite);

        # 39. double 精度问题
        assert(0.1 + 0.2 != 0.3);

        # 40. 使用误差容忍比较
        assert((0.1 + 0.2 - 0.3).abs() < 1e-10);
        !#

    }
    static byteTest()
    {
        # byte wrap and conversions
        byte x = 250
        System.Console.WriteLine("byte x=" + x)
        var y = x + 10
        System.Console.WriteLine("byte+int =" + y)
        #cast down
        byte z = y as byte
        System.Console.WriteLine("casted back byte=" + z)


        #!
                . byte 最大值
        assert(byte.MaxValue == 255);

        // 2. byte 最小值
        assert(byte.MinValue == 0);

        // 3. sbyte 最大值
        assert(sbyte.MaxValue == 127);

        // 4. sbyte 最小值
        assert(sbyte.MinValue == -128);

        // 5. byte 溢出（unchecked）
        unchecked {
        byte b = 255;
        assert((byte)(b + 1) == 0);
        }

        // 6. sbyte 溢出
        unchecked {
        sbyte sb = 127;
        assert((sbyte)(sb + 1) == -128);
        }

        // 7. byte → sbyte 显式转换
        assert((sbyte)128 == -128);

        // 8. sbyte → byte 显式转换
        assert((byte)-1 == 255);
        !#
    }
    static int64Test()
    {
        #!
        // 27. int64 边界
        assert(long.MaxValue == 9223372036854775807);

        // 28. uint64 边界
        assert(ulong.MaxValue == 18446744073709551615UL);

        // 29. int64 溢出
        unchecked {
        long l = long.MaxValue;
        assert(l + 1 == long.MinValue);
        }

        // 30. uint64 溢出
        unchecked {
        ulong ul = ulong.MaxValue;
        assert(ul + 1 == 0);
        }

        // 31. int64 乘法溢出
        unchecked {
        long a = 1L << 62;
        assert(a * 4 == 0);
        }

        // 32. int64 → int32 截断
        assert((int)0x1_0000_0000 == 0);

        // 33. uint64 → int64
        assert((long)ulong.MaxValue == -1);

        // 34. 位运算一致性
        assert((1L << 63) < 0);
        !#
    }
    static floatTest()
    {
        #!
        // 35. float 精度不足
        assert(0.1f + 0.2f != 0.3f);

        // 36. double 精度仍有限
        assert(0.1 + 0.2 != 0.3);

        // 37. float 最大值
        assert(float.MaxValue > 1e38f);

        // 38. double 最大值
        assert(double.MaxValue > 1e308);

        // 39. NaN 不等于自身
        assert(float.NaN != float.NaN);

        // 40. Infinity
        assert(1.0 / 0.0 == double.PositiveInfinity);

        // 41. -Infinity
        assert(-1.0 / 0.0 == double.NegativeInfinity);

        // 42. float → int 截断
        assert((int)3.9f == 3);

        // 43. double → long
        assert((long)3.9 == 3);

        // 44. 大整数 double 精度丢失
        double d = 9007199254740993; // 2^53 + 1
        assert(d == 9007199254740992);

        // 45. float 与 double 比较
        assert((float)0.1 != 0.1);
        !#

    }
    static int fibonacci(int n)
    {
        if (n == 0 || n == 1)
        { ret n; }
        ret fibonacci(n - 1) + fibonacci(n - 2);
    }
    static int16Test()
    {
        #!
                // 9. int16 范围
        assert(short.MaxValue == 32767);
        assert(short.MinValue == -32768);

        // 10. uint16 范围
        assert(ushort.MaxValue == 65535);
        assert(ushort.MinValue == 0);

        // 11. int16 溢出
        unchecked {
        short x = 32767;
        assert((short)(x + 1) == -32768);
        }

        // 12. uint16 溢出
        unchecked {
        ushort y = 65535;
        assert((ushort)(y + 1) == 0);
        }

        // 13. int16 + int16 → int32（提升）
        short a = 30000;
        short b = 30000;
        assert((a + b) is int);

        // 14. uint16 + int16 → int32
        ushort u = 60000;
        short s = -1;
        assert((u + s) is int);

        // 15. int16 左移
        assert((short)(1 << 15) == -32768);

        // 16. uint16 左移
        assert((ushort)(1 << 15) == 32768);
        !#

    }
    static int32Test()
    { 
        var result = fibonacci(20); 
        System.Console.WriteLine("result:" + result  )

        System.Console.WriteLine("intmax:" + int.MaxValue.toString() )

        a = 20
        b = 30
        c = a+b
        uint d = 40
        e = c + d
        System.Console.WriteLine( "c+uint=" + e )
        # bit operations
        System.Console.WriteLine("(a<<2)=" + (a << 2))
        System.Console.WriteLine("(b>>1)=" + (b >> 1))
        System.Console.WriteLine("(a&b)=" + (a & b))
        System.Console.WriteLine("(a|b)=" + (a | b))
        str = "a($a )+b($b )=$(a + b)"
        System.Console.WriteLine( str )
        str22 = 'a($a )+b($b )=$(a + b)'
        System.Console.WriteLine( str22 )
        System.Console.WriteLine( "a($a )+b($b )=${a + b}" )

        #!
        / 17. int32 边界
        assert(int.MaxValue == 2147483647);
        assert(int.MinValue == -2147483648);

        // 18. uint32 边界
        assert(uint.MaxValue == 4294967295);

        // 19. int32 溢出
        unchecked {
        int i = int.MaxValue;
        assert(i + 1 == int.MinValue);
        }

        // 20. uint32 溢出
        unchecked {
        uint ui = uint.MaxValue;
        assert(ui + 1 == 0);
        }

        // 21. int32 / int32 整数除法
        assert(5 / 2 == 2);

        // 22. uint32 / uint32
        assert((uint)5 / (uint)2 == 2);

        // 23. int32 右移（算术）
        assert(-1 >> 1 == -1);

        // 24. uint32 右移（逻辑）
        assert(((uint)0xFFFFFFFF >> 1) == 0x7FFFFFFF);

        // 25. int32 与 uint32 比较需显式
        int x = -1;
        uint y = 1;
        // x < y ❌（需转换）

        // 26. uint32 转 int32
        assert((int)0xFFFFFFFF == -1);
        !#
    }
    static fun()
    {
        #numTest();
        num2Test();
        #byteTest();
        #int32Test();
    }
}