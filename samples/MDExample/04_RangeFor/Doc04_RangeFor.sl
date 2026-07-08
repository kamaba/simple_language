# 对应文档：`md/syntax/range.md`（`for v in r`）
MdEx04RangeFor
{
    static Run()
    {
        # `a..b` 整型区间字面量在部分工程组合下 Meta 会 NRE；此处用与 RangeTest 一致的泛型构造。
        r1 = Range<int>(1, 6, 1)
        for v in r1
        {
            global.println("value=$v ")
        }
    }
}
