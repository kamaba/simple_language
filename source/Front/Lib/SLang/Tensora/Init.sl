# Init —— 权重初始化器
#
# 约定：2D 权重形状为 [fanOut, fanIn]（与 Dense 一致，forward = x · Wᵀ + b）
#   Xavier/Glorot：var = 2 / (fanIn + fanOut)，适合 sigmoid / tanh
#   He          ：var = 2 / fanIn，适合 ReLU 系列
#   Orthogonal  ：正态采样后按列归一化（近似，够用）

@Nickname("Init")
public class Init
{
    public static Tensor zeros( Shape s )
    {
        ret Tensor.zeros( s )
    }

    public static Tensor ones( Shape s )
    {
        ret Tensor.ones( s )
    }

    public static Tensor value( Shape s, Float32 v )
    {
        ret Tensor.full( s, v )
    }

    public static Tensor uniform( Shape s, Float32 lo, Float32 hi )
    {
        Tensor t = Tensor( s )
        Init.fillUniform( t, lo, hi )
        ret t
    }

    public static Tensor normal( Shape s, Float32 mean, Float32 std )
    {
        Tensor t = Tensor( s )
        Init.fillNormal( t, mean, std )
        ret t
    }

    # Xavier 均匀：U[-a, a]，a = sqrt(6 / (fanIn + fanOut))
    public static Tensor xavierUniform( Int32 fanIn, Int32 fanOut )
    {
        Float32 a = Mathf.sqrt( 6.0f / SystemConvertFloat32( fanIn + fanOut ) )
        ret Init.uniform( Shape.matrix( fanOut, fanIn ), 0.0f - a, a )
    }

    # Xavier 正态：N(0, sqrt(2 / (fanIn + fanOut)))
    public static Tensor xavierNormal( Int32 fanIn, Int32 fanOut )
    {
        Float32 std = Mathf.sqrt( 2.0f / SystemConvertFloat32( fanIn + fanOut ) )
        ret Init.normal( Shape.matrix( fanOut, fanIn ), 0.0f, std )
    }

    # He 正态：N(0, sqrt(2 / fanIn))
    public static Tensor heNormal( Int32 fanIn, Int32 fanOut )
    {
        Float32 std = Mathf.sqrt( 2.0f / SystemConvertFloat32( fanIn ) )
        ret Init.normal( Shape.matrix( fanOut, fanIn ), 0.0f, std )
    }

    # He 均匀：U[-a, a]，a = sqrt(6 / fanIn)
    public static Tensor heUniform( Int32 fanIn, Int32 fanOut )
    {
        Float32 a = Mathf.sqrt( 6.0f / SystemConvertFloat32( fanIn ) )
        ret Init.uniform( Shape.matrix( fanOut, fanIn ), 0.0f - a, a )
    }

    # 正交初始化（近似版）
    public static Tensor orthogonal( Int32 rows, Int32 cols )
    {
        Tensor t = Init.normal( Shape.matrix( rows, cols ), 0.0f, 1.0f )
        for c = 0, c < cols, c++
        {
            Float32 acc = 0.0f
            for r = 0, r < rows, r++
            {
                Float32 v = t.get( r, c )
                acc = acc + v * v
            }
            Float32 norm = Mathf.sqrt( acc ) + 0.0000001f
            for r = 0, r < rows, r++
            {
                t.set( r, c, t.get( r, c ) / norm )
            }
        }
        ret t
    }

    # ── 就地填充（已有张量时用这些）───────────────────────
    public static void fillUniform( Tensor t, Float32 lo, Float32 hi )
    {
        Rng rng = Rng.global()
        for i = 0, i < t.size(), i++
        {
            t.setAt( i, rng.uniform( lo, hi ) )
        }
    }

    public static void fillNormal( Tensor t, Float32 mean, Float32 std )
    {
        Rng rng = Rng.global()
        for i = 0, i < t.size(), i++
        {
            t.setAt( i, rng.normal( mean, std ) )
        }
    }

    # 按张量自身形状 [fanOut, fanIn] 自动选择 Xavier
    public static void fillXavier( Tensor t )
    {
        int fanIn = t.cols()
        int fanOut = t.rows()
        Float32 a = Mathf.sqrt( 6.0f / SystemConvertFloat32( fanIn + fanOut ) )
        Init.fillUniform( t, 0.0f - a, a )
    }

    # 按张量自身形状 [fanOut, fanIn] 自动选择 He
    public static void fillHe( Tensor t )
    {
        int fanIn = t.cols()
        Float32 std = Mathf.sqrt( 2.0f / SystemConvertFloat32( fanIn ) )
        Init.fillNormal( t, 0.0f, std )
    }

    public static void fillZero( Tensor t )
    {
        t.zero()
    }
}
