InlineLambdaTest
{
    static fun()
    {
        global.println("========== InlineLambdaTest (start) ==========")

        # 0. 对照: 普通表达式 (无内联lambda), 与用例8同构
        Int32 c1 = 10 + 20
        Int32 c2 = 100 + 200
        Int32 c3 = c1 + c2
        global.println("plain: c1 + c2 = " + c3.toString())
        Int32 c4 = (10 + 20) + (100 + 200)
        global.println("plain: (10+20) + (100+200) = " + c4.toString())

        # 1. 双参基础: 加法
        var add = (a, b) => a + b
        Int32 r1 = add(1, 2)
        global.println("add(1, 2) = " + r1.toString())

        # 2. 单参(带括号): 平方
        var square = (x) => x * x
        Int32 r2 = square(5)
        global.println("square(5) = " + r2.toString())

        # 3. 实参为表达式
        Int32 r3 = add(1 + 2, 3 * 4)
        global.println("add(1+2, 3*4) = " + r3.toString())

        # 4. 体内嵌套调用另一个内联lambda: dbl(x) => add(x, x)
        var dbl = (x) => add(x, x)
        Int32 r4 = dbl(7)
        global.println("dbl(7) = " + r4.toString())

        # 5. 三元表达式体
        var maxOf = (a, b) => a > b ? a : b
        Int32 r5 = maxOf(10, 20)
        global.println("maxOf(10, 20) = " + r5.toString())

        # 6. var 接收 + 实参内嵌套调用
        var inc = (n) => n + 1
        var v1 = inc(41)
        var v2 = inc(inc(41))
        global.println("inc(41) = " + v1.toString())
        global.println("inc(inc(41)) = " + v2.toString())

        # 7. 括号改变优先级: (a+b)*c
        var cal = (a, b, c) => (a + b) * c
        Int32 r7 = cal(1, 2, 3)
        global.println("cal(1, 2, 3) = " + r7.toString())

        # 8. 同一内联lambda多处调用
        Int32 r8 = add(10, 20) + add(100, 200)
        global.println("add(10,20) + add(100,200) = " + r8.toString())

        # 9. M3 类型标注: 双参 Int32 标注, 调用点校验实参类型
        var typedAdd = (Int32 a, Int32 b) => a + b
        Int32 r9 = typedAdd(3, 4)
        global.println("typedAdd(3, 4) = " + r9.toString())

        # 10. 类型标注 + 体内嵌套调用另一内联lambda
        var typedDbl = (Int32 x) => typedAdd(x, x)
        Int32 r10 = typedDbl(9)
        global.println("typedDbl(9) = " + r10.toString())

        # 11. 混合形态: 一参类型标注 + 一参裸名 (标注可选逐参生效)
        var mixed = (Int32 a, b) => a * b
        Int32 r11 = mixed(6, 7)
        global.println("mixed(6, 7) = " + r11.toString())

        # 12. String 类型标注
        var greet = (String name) => "hello " + name
        global.println("greet(world) = " + greet("world"))

        # 13. object 类型标注: Int32/String 实参均为 object 子类, 赋值兼容放行
        var asObj = (object o) => o
        var o1 = asObj(42)
        var o2 = asObj("str")
        global.println("asObj(42) / asObj(str) compiled ok")

        global.println("========== InlineLambdaTest (end) ==========")
    }
}
