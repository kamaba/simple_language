# 对应文档：`md/syntax/string.md`（`$变量`、`$对象.成员`）

MdEx02Helper
{
    a1 = 0
}

MdEx02StringInterpolation
{
    static Run()
    {
        a4 = 10
        MdEx02Helper c1 = new()
        c1.a1 = 22
        a6 = "print a=$a4 "
        a7 = "print c1.a1=$c1.a1 "
        global.println(a6)
        global.println(a7)
    }
}
