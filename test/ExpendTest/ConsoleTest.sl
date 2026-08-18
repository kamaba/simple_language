import Std;

ConsoleTest
{
    # 测试 write / print / println 基本输出
    static testBasicOutput()
    {
        Console.println("===== testBasicOutput =====")
        Console.write("write不换行>")
        Console.print("print也不换行>")
        Console.println("println换行")
        Console.println("单独一行")
    }

    # 测试 println() 空行 / newLine
    static testNewLine()
    {
        Console.println("===== testNewLine =====")
        Console.println("第一行")
        Console.newLine()
        Console.println("第三行（第二行是空行）")
        Console.println("第四行")
        Console.newLine()
        Console.println("第六行（第五行是 newLine 产生的空行）")
    }

    # 测试 writeLine（C# 风格别名）
    static testWriteLine()
    {
        Console.println("===== testWriteLine =====")
        Console.writeLine("使用 writeLine 输出")
        Console.writeLine("writeLine = println 别名")
    }

    # 测试字符串拼接输出
    static testConcat()
    {
        Console.println("===== testConcat =====")
        int a = 10
        int b = 20
        Console.println("a = " + a + ", b = " + b)
        Console.println("a + b = " + (a + b))
        Console.println("a - b = " + (a - b))
        Console.println("a * b = " + (a * b))
        Console.println("a / b = " + (a / b))
        string name = "World"
        Console.println("Hello, " + name + "!")
    }

    # 测试 readInt / readDouble 逻辑（非交互，用 parse 模拟）
    static testReadParse()
    {
        Console.println("===== testReadParse =====")
        # 模拟 readInt：用 Int32.parse 解析字符串
        string intStr = "42"
        int parsed = Int32.parse(intStr)
        Console.println("Int32.parse(\"" + intStr + "\") = " + parsed)

        # 模拟 readDouble：用 SystemConvertFloat64 解析字符串
        string dblStr = "3.14"
        double dbl = Float64(dblStr)
        Console.println("SystemConvertFloat64(\"" + dblStr + "\") = " + dbl.toString())

        # 负数解析
        int neg = Int32.parse("-100")
        Console.println("parse(\"-100\") = " + neg)
    }

    # 计算器核心逻辑（非交互，用硬编码值验证运算）
    static testCalculatorLogic()
    {
        Console.println("===== testCalculatorLogic =====")
        # 加法
        Console.println("10 + 5 = " + calc(10, "+", 5))
        # 减法
        Console.println("10 - 3 = " + calc(10, "-", 3))
        # 乘法
        Console.println("4 * 6 = " + calc(4, "*", 6))
        # 整数除法
        Console.println("20 / 4 = " + calc(20, "/", 4))
        # 除不尽（整数截断）
        Console.println("7 / 2 = " + calc(7, "/", 2))
        # 除以零
        Console.println("5 / 0 = " + calc(5, "/", 0))
        # 不支持的运算符
        Console.println("5 % 3 = " + calc(5, "%", 3))
        # 负数运算
        Console.println("-8 + 3 = " + calc(-8, "+", 3))
        Console.println("-8 * -2 = " + calc(-8, "*", -2))
    }

    # 计算器单次运算核心：传入 a, op, b 返回结果字符串
    static string calc(int a, string op, int b)
    {
        if (op == "+")
        {
            ret (a + b).toString()
        }
        else if (op == "-")
        {
            ret (a - b).toString()
        }
        else if (op == "*")
        {
            ret (a * b).toString()
        }
        else if (op == "/")
        {
            if (b == 0)
            {
                ret "错误:除数不能为零"
            }
            ret (a / b).toString()
        }
        else
        {
            ret "错误:不支持的运算符 " + op
        }
    }

    # 交互式计算器：从标准输入读取数字和运算符
    # 调用方式：ConsoleTest.calculator()
    static void calculator()
    {
        Console.println("===== 简易计算器 =====")
        Console.println("支持运算: +  -  *  /")
        Console.println("输入 q 退出")
        Console.newLine()

        while (true)
        {
            string aStr = Console.input("第一个数 (或输入 q 退出): ")
            if (aStr == "q")
            {
                break
            }
            int a = Int32.parse(aStr)

            string op = Console.input("运算符 (+ - * /): ")
            if (op == "q")
            {
                break
            }

            string bStr = Console.input("第二个数: ")
            if (bStr == "q")
            {
                break
            }
            int b = Int32.parse(bStr)

            string result = calc(a, op, b)
            Console.println(aStr + " " + op + " " + bStr + " = " + result)
            Console.newLine()
        }
        Console.println("计算器已退出")
    }

    # 测试多种数据类型输出
    static testMixedTypes()
    {
        Console.println("===== testMixedTypes =====")
        Console.println("int: " + 42)
        Console.println("负数: " + (-17))
        Console.println("bool: " + true)
        Console.println("string: " + "hello")
        Console.println("拼接: " + "a=" + 1 + " b=" + 2)
        Console.println("表达式: " + (3 + 4 * 2))
    }

    static fun()
    {
        Console.println("===== ConsoleTest =====")
        testBasicOutput()
        testNewLine()
        testWriteLine()
        testConcat()
        testReadParse()
        testCalculatorLogic()
        testMixedTypes()
        calculator() #是交互式的，需要标准输入
        # 如需使用，在 ProjectTest.sp 中单独调用 ConsoleTest.calculator()
    }
}
