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

    # ── 数组批量计算：AOT 原生读 VMArray（length@+48 / data@+64）──

    # 数组平均：数组参数经 CVM 编组为 kind 0 对象指针，原生侧读 length/data
    @AOT()
    static double ArrayAvg( double[] arr )
    {
        int n = arr.length
        if n <= 0
        {
            ret 0.0
        }
        double s = 0.0
        int i = 0
        for i = 0, i < n, i += 1
        {
            s = s + arr[i]
        }
        ret s / n
    }

    # 高斯因子（标准正态密度 exp(-x*x/2)/sqrt(2*pi)）：泰勒级数纯算术实现
    #（Math.sqrt/exp 是系统调用无法内联，30 项在 |x|<=5 内收敛）
    @AOT()
    static double GaussFactor( double x )
    {
        double t = -0.5 * x * x
        double term = 1.0
        double sum = 1.0
        int k = 1
        for k = 1, k <= 30, k += 1
        {
            term = term * t / k
            sum = sum + term
        }
        ret 0.3989422804014327 * sum
    }

    # 高斯因子对照：同泰勒实现但不 @AOT（CVM 解释执行，验证两边结果一致）
    static double GaussFactorVm( double x )
    {
        double t = -0.5 * x * x
        double term = 1.0
        double sum = 1.0
        int k = 1
        for k = 1, k <= 30, k += 1
        {
            term = term * t / k
            sum = sum + term
        }
        ret 0.3989422804014327 * sum
    }

    # 数组高斯因子平均：泰勒级数直接内联进循环体（元素加载纯原生，不跨 AOT 调用）
    @AOT()
    static double ArrayGaussAvg( double[] arr )
    {
        int n = arr.length
        if n <= 0
        {
            ret 0.0
        }
        double s = 0.0
        int i = 0
        for i = 0, i < n, i += 1
        {
            double t = -0.5 * arr[i] * arr[i]
            double term = 1.0
            double g = 1.0
            int k = 1
            for k = 1, k <= 30, k += 1
            {
                term = term * t / k
                g = g + term
            }
            s = s + 0.3989422804014327 * g
        }
        ret s / n
    }
}
