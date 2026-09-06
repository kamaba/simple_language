# 数论 / 因子工具类
# 说明：本类只提供静态方法，不保存状态（原骨架 _init_ 为 vec4 风格的复制残留，已移除）。
# 覆盖：质数判定、质因数分解、约数枚举、最大公约数 / 最小公倍数、整数快速幂、阶乘、2 的幂。
public class Factor
{
    # ── 质数 ─────────────────────────────────────────────
    # 试除法判定（n < 2 一律返回 false）
    public static bool isPrime( Int32 n )
    {
        if n < 2
        {
            ret false
        }
        if n == 2 || n == 3
        {
            ret true
        }
        if n % 2 == 0 || n % 3 == 0
        {
            ret false
        }
        Int32 i = 5
        while i * i <= n
        {
            if n % i == 0 || n % ( i + 2 ) == 0
            {
                ret false
            }
            i = i + 6
        }
        ret true
    }

    # ── 质因数分解 ───────────────────────────────────────
    # 返回升序质因数数组（含重复），如 12 -> [2, 2, 3]；n < 2 返回空数组
    public static Array<Int32> primeFactors( Int32 n )
    {
        Array<Int32> list = Array<Int32>( 32 )
        int count = 0
        Int32 v = n

        if v < 2
        {
            ret Factor._slice( list, 0 )
        }

        while v % 2 == 0
        {
            Factor._push( list, count, 2 )
            count = count + 1
            v = v / 2
        }

        Int32 i = 3
        while i * i <= v
        {
            while v % i == 0
            {
                Factor._push( list, count, i )
                count = count + 1
                v = v / i
            }
            i = i + 2
        }

        if v > 1
        {
            Factor._push( list, count, v )
            count = count + 1
        }
        ret Factor._slice( list, count )
    }

    # 去重后的质因数集合
    public static Array<Int32> distinctPrimeFactors( Int32 n )
    {
        Array<Int32> all = Factor.primeFactors( n )
        Array<Int32> list = Array<Int32>( 32 )
        int count = 0
        int i = 0
        while i < all.length
        {
            bool exists = false
            int j = 0
            while j < count
            {
                if list[j] == all[i]
                {
                    exists = true
                }
                j = j + 1
            }
            if !exists
            {
                Factor._push( list, count, all[i] )
                count = count + 1
            }
            i = i + 1
        }
        ret Factor._slice( list, count )
    }

    # ── 约数 ─────────────────────────────────────────────
    # 返回 n 的所有正约数（升序）
    public static Array<Int32> divisors( Int32 n )
    {
        Array<Int32> small = Array<Int32>( 64 )
        Array<Int32> large = Array<Int32>( 64 )
        int sc = 0
        int lc = 0

        if n < 1
        {
            ret Factor._slice( small, 0 )
        }

        Int32 i = 1
        while i * i <= n
        {
            if n % i == 0
            {
                Factor._push( small, sc, i )
                sc = sc + 1
                if i != n / i
                {
                    Factor._push( large, lc, n / i )
                    lc = lc + 1
                }
            }
            i = i + 1
        }

        Array<Int32> result = Array<Int32>( sc + lc )
        int idx = 0
        int k = 0
        while k < sc
        {
            result[idx] = small[k]
            idx = idx + 1
            k = k + 1
        }
        k = lc - 1
        while k >= 0
        {
            result[idx] = large[k]
            idx = idx + 1
            k = k - 1
        }
        ret result
    }

    # 约数个数
    public static Int32 divisorCount( Int32 n )
    {
        ret Factor.divisors( n ).length
    }

    # 约数之和
    public static Int32 divisorSum( Int32 n )
    {
        Array<Int32> ds = Factor.divisors( n )
        Int32 sum = 0
        int i = 0
        while i < ds.length
        {
            sum = sum + ds[i]
            i = i + 1
        }
        ret sum
    }

    # ── 公约数 / 公倍数 ───────────────────────────────────
    public static Int32 gcd( Int32 a, Int32 b )
    {
        Int32 x = Factor.absInt( a )
        Int32 y = Factor.absInt( b )
        while y != 0
        {
            Int32 t = x % y
            x = y
            y = t
        }
        ret x
    }

    public static Int32 lcm( Int32 a, Int32 b )
    {
        if a == 0 || b == 0
        {
            ret 0
        }
        ret Factor.absInt( a / Factor.gcd( a, b ) * b )
    }

    # 扩展欧几里得：返回 [gcd, x, y]，满足 a*x + b*y = gcd
    public static Array<Int32> extendedGcd( Int32 a, Int32 b )
    {
        Array<Int32> result = Array<Int32>( 3 )
        Int32 old_r = Factor.absInt( a )
        Int32 r = Factor.absInt( b )
        Int32 old_s = 1
        Int32 s = 0
        Int32 old_t = 0
        Int32 t = 1
        while r != 0
        {
            Int32 q = old_r / r
            Int32 tmp = old_r - q * r
            old_r = r
            r = tmp
            tmp = old_s - q * s
            old_s = s
            s = tmp
            tmp = old_t - q * t
            old_t = t
            t = tmp
        }
        result[0] = old_r
        result[1] = old_s
        result[2] = old_t
        ret result
    }

    # ── 幂 / 阶乘 ────────────────────────────────────────
    # 整数快速幂（exponent 需非负）
    public static Int32 powInt( Int32 baseValue, Int32 exponent )
    {
        Int32 result = 1
        Int32 b = baseValue
        Int32 e = exponent
        while e > 0
        {
            if e % 2 == 1
            {
                result = result * b
            }
            b = b * b
            e = e / 2
        }
        ret result
    }

    # 阶乘（大数版本，避免 Int32 溢出）
    public static BigNumber factorial( Int32 n )
    {
        ret BigNumber.factorial( n )
    }

    # 排列数 A(n, k)
    public static BigNumber permutation( Int32 n, Int32 k )
    {
        if k < 0 || k > n
        {
            ret BigNumber( 0 )
        }
        BigNumber r = BigNumber( 1 )
        Int32 i = n - k + 1
        while i <= n
        {
            r = r.multiply( BigNumber( i ) )
            i = i + 1
        }
        ret r
    }

    # 组合数 C(n, k)
    public static BigNumber combination( Int32 n, Int32 k )
    {
        if k < 0 || k > n
        {
            ret BigNumber( 0 )
        }
        if k > n / 2
        {
            k = n - k
        }
        BigNumber r = BigNumber( 1 )
        Int32 i = 1
        while i <= k
        {
            r = r.multiply( BigNumber( n - k + i ) ).div( BigNumber( i ) )
            i = i + 1
        }
        ret r
    }

    # ── 2 的幂 ───────────────────────────────────────────
    public static bool isPowerOfTwo( Int32 n )
    {
        ret n > 0 && ( n & ( n - 1 ) ) == 0
    }

    # 返回 >= n 的最小 2 的幂
    public static Int32 nextPowerOfTwo( Int32 n )
    {
        if n <= 1
        {
            ret 1
        }
        Int32 r = 1
        while r < n
        {
            r = r * 2
        }
        ret r
    }

    # ── 其他 ─────────────────────────────────────────────
    public static Int32 absInt( Int32 v )
    {
        if v < 0
        {
            ret 0 - v
        }
        ret v
    }

    # 整数平方根（向下取整）
    public static Int32 isqrt( Int32 n )
    {
        if n <= 0
        {
            ret 0
        }
        Int32 x = n
        Int32 y = ( x + 1 ) / 2
        while y < x
        {
            x = y
            y = ( x + n / x ) / 2
        }
        ret x
    }

    # 欧拉函数 φ(n)：小于 n 且与 n 互质的正整数个数
    public static Int32 eulerPhi( Int32 n )
    {
        if n <= 0
        {
            ret 0
        }
        Int32 result = n
        Int32 v = n
        Int32 p = 2
        while p * p <= v
        {
            if v % p == 0
            {
                while v % p == 0
                {
                    v = v / p
                }
                result = result - result / p
            }
            p = p + 1
        }
        if v > 1
        {
            result = result - result / v
        }
        ret result
    }

    # ── 内部辅助（Array 容量固定，写入前需保证下标可用）──────
    static void _push( Array<Int32> list, int index, Int32 value )
    {
        list[index] = value
    }

    static Array<Int32> _slice( Array<Int32> list, int count )
    {
        if count <= 0
        {
            ret Array<Int32>( 0 )
        }
        Array<Int32> r = Array<Int32>( count )
        int i = 0
        while i < count
        {
            r[i] = list[i]
            i = i + 1
        }
        ret r
    }

    override string toString()
    {
        ret "Factor(static utility)"
    }
}
