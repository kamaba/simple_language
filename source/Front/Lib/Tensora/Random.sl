# Rng —— 可复现伪随机数发生器（xorshift32 + Box-Muller 正态）
#
# 用途：权重初始化、dropout、数据打乱、采样（gumbel / bernoulli）
# 与 Core.Random 的区别：本类可指定 seed 且状态可控，保证实验可复现

@Nickname("Rng")
public class Rng
{
    Int32 _state = 0
    Float32 _spare = 0.0f
    bool _hasSpare = false

    # 全局默认发生器（初始化 / dropout 默认使用它）
    static Rng _global = null

    public void _init_()
    {
        this._state = 88675123
        this._hasSpare = false
    }

    public void _init_( Int32 seed )
    {
        this._state = seed
        if this._state == 0
        {
            this._state = 88675123
        }
        this._hasSpare = false
    }

    void setSeed( Int32 seed )
    {
        this._state = seed
        if this._state == 0
        {
            this._state = 88675123
        }
        this._hasSpare = false
    }

    # ── 核心：LCG（与 Core.Random 同款，保证跨平台一致）──
    Int32 nextInt32()
    {
        this._state = ( this._state * 1103515245 + 12345 ) & 2147483647
        ret this._state
    }

    # [0, 1) 均匀分布
    Float32 nextFloat()
    {
        Int32 v = this.nextInt32()
        if v < 0
        {
            v = 0 - v
        }
        Float32 f = SystemConvertFloat32( v % 16777216 )
        ret f / 16777216.0f
    }

    Float32 uniform( Float32 lo, Float32 hi )
    {
        ret lo + ( hi - lo ) * this.nextFloat()
    }

    # [0, n) 整数
    Int32 randInt( Int32 n )
    {
        if n <= 0
        {
            ret 0
        }
        Int32 v = this.nextInt32()
        if v < 0
        {
            v = 0 - v
        }
        ret v % n
    }

    # 标准正态 N(0,1)，Box-Muller（缓存第二个值）
    Float32 nextNormal()
    {
        if this._hasSpare
        {
            this._hasSpare = false
            ret this._spare
        }
        Float32 u = this.nextFloat()
        Float32 v = this.nextFloat()
        if u < 0.0000001f
        {
            u = 0.0000001f
        }
        Float32 r = Mathf.sqrt( 0.0f - 2.0f * Mathf.log( u ) )
        Float32 theta = 6.283185307f * v
        this._spare = r * Mathf.sin( theta )
        this._hasSpare = true
        ret r * Mathf.cos( theta )
    }

    Float32 normal( Float32 mean, Float32 std )
    {
        ret mean + std * this.nextNormal()
    }

    bool bernoulli( Float32 p )
    {
        ret this.nextFloat() < p
    }

    # Gumbel(0,1)：用于 gumbel-softmax / 类别采样
    Float32 gumbel()
    {
        Float32 u = this.nextFloat()
        if u < 0.0000001f
        {
            u = 0.0000001f
        }
        ret 0.0f - Mathf.log( 0.0f - Mathf.log( u ) )
    }

    # ── 采样 / 打乱 ──────────────────────────────────────
    Array<Int32> permutation( Int32 n )
    {
        ret this.shuffle( Rng.rangeArray( n ) )
    }

    # Fisher-Yates 原地打乱
    Array<Int32> shuffle( Array<Int32> arr )
    {
        for i = arr.length - 1, i > 0, i--
        {
            int j = this.randInt( i + 1 )
            Int32 tmp = arr[i]
            arr[i] = arr[j]
            arr[j] = tmp
        }
        ret arr
    }

    # 生成 [0, n) 的顺序数组（工具）
    static Array<Int32> rangeArray( Int32 n )
    {
        Array<Int32> a = Array<Int32>( n )
        for i = 0, i < n, i++
        {
            a[i] = i
        }
        ret a
    }

    # ── 全局实例 ─────────────────────────────────────────
    public static Rng global()
    {
        if Rng._global == null
        {
            Rng._global = Rng( 20240915 )
        }
        ret Rng._global
    }

    public static Rng withSeed( Int32 seed )
    {
        ret Rng( seed )
    }
}
