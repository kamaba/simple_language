# 对应文档：`md/syntax/base.md`（类、字段、方法、字符串插值）
# 输出走 Core.sp → Project 的 global.println（与 BaseTest 一致）
# 被调用的类型需写在入口类之前，避免 Meta 阶段解析 new() 时类尚未注册。

Rectangle
{
    width = 0
    height = 0

    toString()
    {
        ret "Rectangle(${this.width} x ${this.height})"
    }
}

MdEx01BaseRectangle
{
    static Run()
    {
        Rectangle r = new()
        r.width = 10
        r.height = 4
        global.println(r.toString())
    }
}
