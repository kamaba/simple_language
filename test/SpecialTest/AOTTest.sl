# ============================================================
# AOTTest - @AOT() 标记的静态函数 AOT 编译测试
# （自 test/AOTTest/AOTTest.sl 合并到 SpecialScript）
# 阶段1：验证 @AOT() 标记贯通前端
#   - IRMethod.isAot（attributeList 提取）
#   - module.json flags bit 256
#   - aot.mlir 候选导出
# ============================================================

class AOTMath
{
    # 加法：AOT 候选（非模板静态函数）
    @AOT()
    static int Add( int a, int b )
    {
        ret a + b
    }

    # 乘法：AOT 候选（非模板静态函数）
    @AOT()
    static int Mul( int a, int b )
    {
        ret a * b
    }

    # 非常量输入也走同一条指令序列
    @AOT()
    static int SumLoop( int n )
    {
        int sum = 0
        int i = 1
        for i = 1, i <= n, i += 1
        {
            sum = sum + i
        }
        ret sum
    }

    # 非 AOT：对照（CVM 解释执行）
    static int Sub( int a, int b )
    {
        ret a - b
    }

    # 阶段5：AOT 反向调用解释器（int 路径，sl_value kind 0 / i64 位模式）
    @AOT()
    static int CallVmSub( int a, int b )
    {
        ret Sub( a, b )
    }

    # 非 AOT：f64 对照（CVM 解释执行，被下面的 AOT 函数反调）
    static double Avg( double a, double b )
    {
        ret ( a + b ) / 2
    }

    # 阶段5：AOT 反向调用解释器（double 路径，sl_value kind 1 / f64 位模式）
    @AOT()
    static double CallVmAvg( double a, double b )
    {
        ret Avg( a, b )
    }

    # 模板函数：阶段1应被跳过（日志 skip）
    @AOT()
    static fun<T>( T a )
    {
        ret a
    }

    # 非静态函数：阶段1应被跳过（日志 skip）
    @AOT()
    int InstanceFun( int a )
    {
        ret a * 2
    }
}
