@Nickname("BigInt")
public class BigNumber extends Num
{
    # ── 内部表示 ─────────────────────────────────────────
    # 定点进制：每段存 4 位十进制（BASE = 10^4），低位在前（_digits[0] 是最低段）。
    # _sign：0 = 零，1 = 正，-1 = 负。
    # 容量固定 CAPACITY 段（默认 64 段 ≈ 256 位十进制），避免动态扩容。
    const static Int32 BASE = 10000
    const static Int32 BASE_DIGITS = 4
    const static Int32 CAPACITY = 64

    Array<Int32> _digits = null
    Int32 _length = 1
    Int32 _sign = 0

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this._digits = Array<Int32>( BigNumber.CAPACITY )
        int i = 0
        while i < BigNumber.CAPACITY
        {
            this._digits[i] = 0
            i++
        }
        this._length = 1
        this._sign = 0
    }

    public void _init_( Int32 v )
    {
        this._init_()
        if v == 0
        {
            ret
        }
        if v < 0
        {
            this._sign = -1
            v = -v
        }
        else
        {
            this._sign = 1
        }

        int idx = 0
        while v > 0 && idx < BigNumber.CAPACITY
        {
            this._digits[idx] = v % BigNumber.BASE
            v = v / BigNumber.BASE
            idx++
        }
        this._length = idx
    }

    public void _init_( string text )
    {
        this._init_()
        BigNumber parsed = BigNumber.parse( text )
        this._copyFrom( parsed )
    }

    void _copyFrom( BigNumber other )
    {
        int i = 0
        while i < BigNumber.CAPACITY
        {
            this._digits[i] = other._digits[i]
            i++
        }
        this._length = other._length
        this._sign = other._sign
    }

    # 去除高位无效 0，并维护 _sign
    void _trim()
    {
        while this._length > 1 && this._digits[ this._length - 1 ] == 0
        {
            this._length = this._length - 1
        }
        if this._length == 1 && this._digits[0] == 0
        {
            this._sign = 0
        }
    }

    bool _isZero()
    {
        ret this._sign == 0
    }

    # 绝对值比较：-1 / 0 / 1
    int _absCompare( BigNumber other )
    {
        if this._length != other._length
        {
            ret this._length > other._length ? 1 : 0 - 1
        }
        int i = this._length - 1
        while i >= 0
        {
            if this._digits[i] != other._digits[i]
            {
                ret this._digits[i] > other._digits[i] ? 1 : 0 - 1
            }
            i = i - 1
        }
        ret 0
    }

    # 无符号加法（结果写入新对象）
    BigNumber _addAbs( BigNumber other )
    {
        BigNumber r = BigNumber()
        int maxLen = Mathd.max( this._length, other._length )
        int carry = 0
        int i = 0
        while i < maxLen
        {
            int sum = this._digits[i] + other._digits[i] + carry
            r._digits[i] = sum % BigNumber.BASE
            carry = sum / BigNumber.BASE
            i = i + 1
        }
        r._length = maxLen
        if carry > 0 && maxLen < BigNumber.CAPACITY
        {
            r._digits[maxLen] = carry
            r._length = maxLen + 1
        }
        r._trim()
        ret r
    }

    # 无符号减法（要求 this 绝对值 >= other 绝对值）
    BigNumber _subAbs( BigNumber other )
    {
        BigNumber r = BigNumber()
        int borrow = 0
        int i = 0
        while i < this._length
        {
            int diff = this._digits[i] - borrow
            if i < other._length
            {
                diff = diff - other._digits[i]
            }
            if diff < 0
            {
                diff = diff + BigNumber.BASE
                borrow = 1
            }
            else
            {
                borrow = 0
            }
            r._digits[i] = diff
            i = i + 1
        }
        r._length = this._length
        r._trim()
        ret r
    }

    BigNumber _negate()
    {
        BigNumber r = this.clone()
        if r._sign != 0
        {
            r._sign = 0 - r._sign
        }
        ret r
    }

    # ── 算术 ─────────────────────────────────────────────
    public BigNumber add( BigNumber other )
    {
        if this._isZero()
        {
            ret other.clone()
        }
        if other._isZero()
        {
            ret this.clone()
        }
        if this._sign == other._sign
        {
            BigNumber r = this._addAbs( other )
            r._sign = this._sign
            ret r
        }
        # 异号 → 大减小，符号取绝对值大的一方
        int cmp = this._absCompare( other )
        if cmp == 0
        {
            ret BigNumber()
        }
        if cmp > 0
        {
            BigNumber r = this._subAbs( other )
            r._sign = this._sign
            ret r
        }
        BigNumber r = other._subAbs( this )
        r._sign = other._sign
        ret r
    }

    public BigNumber sub( BigNumber other )
    {
        ret this.add( other._negate() )
    }

    public BigNumber multiply( BigNumber other )
    {
        if this._isZero() || other._isZero()
        {
            ret BigNumber()
        }
        BigNumber r = BigNumber()
        int i = 0
        while i < this._length
        {
            int carry = 0
            int j = 0
            while j < other._length
            {
                int pos = i + j
                if pos >= BigNumber.CAPACITY
                {
                    j = j + 1
                    continue
                }
                int v = r._digits[pos] + this._digits[i] * other._digits[j] + carry
                r._digits[pos] = v % BigNumber.BASE
                carry = v / BigNumber.BASE
                j = j + 1
            }
            int pos = i + other._length
            while carry > 0 && pos < BigNumber.CAPACITY
            {
                int v = r._digits[pos] + carry
                r._digits[pos] = v % BigNumber.BASE
                carry = v / BigNumber.BASE
                pos = pos + 1
            }
            i = i + 1
        }
        r._length = Mathd.min( this._length + other._length, BigNumber.CAPACITY )
        r._sign = this._sign * other._sign
        r._trim()
        ret r
    }

    # 长除法：返回 [商, 余数]（余数符号与被除数一致）
    public Array<BigNumber> divMod( BigNumber other )
    {
        Array<BigNumber> result = Array<BigNumber>( 2 )
        if other._isZero()
        {
            # 除零：返回 0 / 0，交由调用方处理
            result[0] = BigNumber()
            result[1] = BigNumber()
            ret result
        }
        if this._isZero()
        {
            result[0] = BigNumber()
            result[1] = BigNumber()
            ret result
        }

        BigNumber quotient = BigNumber()
        BigNumber remainder = BigNumber()
        int i = this._length - 1
        while i >= 0
        {
            # remainder = remainder * BASE + this._digits[i]
            BigNumber shifted = remainder._shiftLeftOneDigit()
            BigNumber cur = shifted.add( BigNumber( this._digits[i] ) )

            # 二分试商 [0, BASE-1]
            int low = 0
            int high = BigNumber.BASE - 1
            int best = 0
            while low <= high
            {
                int mid = ( low + high ) / 2
                BigNumber probe = other.abs().multiply( BigNumber( mid ) )
                if probe._absCompare( cur ) <= 0
                {
                    best = mid
                    low = mid + 1
                }
                else
                {
                    high = mid - 1
                }
            }
            quotient._digits[i] = best
            remainder = cur.sub( other.abs().multiply( BigNumber( best ) ) )
            i = i - 1
        }

        quotient._length = this._length
        quotient._sign = this._sign * other._sign
        quotient._trim()
        remainder._sign = this._sign
        remainder._trim()

        result[0] = quotient
        result[1] = remainder
        ret result
    }

    # 左移一段（乘以 BASE）
    BigNumber _shiftLeftOneDigit()
    {
        BigNumber r = BigNumber()
        if this._isZero()
        {
            ret r
        }
        int i = this._length - 1
        while i >= 0
        {
            if i + 1 < BigNumber.CAPACITY
            {
                r._digits[ i + 1 ] = this._digits[i]
            }
            i = i - 1
        }
        r._digits[0] = 0
        r._length = Mathd.min( this._length + 1, BigNumber.CAPACITY )
        r._sign = this._sign
        r._trim()
        ret r
    }

    public BigNumber div( BigNumber other )
    {
        ret this.divMod( other )[0]
    }

    public BigNumber mod( BigNumber other )
    {
        ret this.divMod( other )[1]
    }

    public BigNumber negate()
    {
        ret this._negate()
    }

    public BigNumber clone()
    {
        BigNumber r = BigNumber()
        r._copyFrom( this )
        ret r
    }

    # ── 运算符重载 ───────────────────────────────────────
    override BigNumber _add_( Object obj1 )
    {
        if obj1 is BigNumber b
        {
            ret this.add( b )
        }
        if obj1 is Int32 i
        {
            ret this.add( BigNumber( i ) )
        }
        ret this
    }

    override BigNumber _sub_( Object obj1 )
    {
        if obj1 is BigNumber b
        {
            ret this.sub( b )
        }
        if obj1 is Int32 i
        {
            ret this.sub( BigNumber( i ) )
        }
        ret this
    }

    override BigNumber _mul_( Object obj1 )
    {
        if obj1 is BigNumber b
        {
            ret this.multiply( b )
        }
        if obj1 is Int32 i
        {
            ret this.multiply( BigNumber( i ) )
        }
        ret this
    }

    override BigNumber _truediv_( Object obj1 )
    {
        if obj1 is BigNumber b
        {
            ret this.div( b )
        }
        if obj1 is Int32 i
        {
            ret this.div( BigNumber( i ) )
        }
        ret this
    }

    override BigNumber _mod_( Object obj1 )
    {
        if obj1 is BigNumber b
        {
            ret this.mod( b )
        }
        if obj1 is Int32 i
        {
            ret this.mod( BigNumber( i ) )
        }
        ret this
    }

    override bool _lt_( Object obj1 )
    {
        ret this.compareToBoxed( obj1 ) < 0
    }

    override bool _le_( Object obj1 )
    {
        ret this.compareToBoxed( obj1 ) <= 0
    }

    override bool _gt_( Object obj1 )
    {
        ret this.compareToBoxed( obj1 ) > 0
    }

    override bool _ge_( Object obj1 )
    {
        ret this.compareToBoxed( obj1 ) >= 0
    }

    override bool _eq_( Object obj1 )
    {
        ret this.compareToBoxed( obj1 ) == 0
    }

    override bool _ne_( Object obj1 )
    {
        ret this.compareToBoxed( obj1 ) != 0
    }

    # Object 参数版比较（供运算符重载使用）
    int compareToBoxed( Object obj1 )
    {
        if obj1 is BigNumber b
        {
            ret this.compare( b )
        }
        if obj1 is Int32 i
        {
            ret this.compare( BigNumber( i ) )
        }
        ret 1
    }

    public int compare( BigNumber other )
    {
        if this._isZero() && other._isZero()
        {
            ret 0
        }
        if this._isZero()
        {
            ret other._sign > 0 ? 0 - 1 : 1
        }
        if other._isZero()
        {
            ret this._sign > 0 ? 1 : 0 - 1
        }
        if this._sign != other._sign
        {
            ret this._sign > 0 ? 1 : 0 - 1
        }
        int cmp = this._absCompare( other )
        if this._sign < 0
        {
            cmp = 0 - cmp
        }
        ret cmp
    }

    # ── Num 接口实现 ─────────────────────────────────────
    override get int size()
    {
        ret this._length * 32
    }

    override get int byteLength()
    {
        ret this._length * 4
    }

    public override Num abs()
    {
        BigNumber r = this.clone()
        if r._sign < 0
        {
            r._sign = 1
        }
        ret r
    }

    public override Num floor()
    {
        ret this.clone()
    }

    public override Num ceil()
    {
        ret this.clone()
    }

    public override Int8 compareTo( Num other )
    {
        if other == null
        {
            ret 1
        }
        if other is BigNumber b
        {
            int cmp = this.compare( b )
            ret cmp < 0 ? 0 - 1 : ( cmp > 0 ? 1 : 0 )
        }
        # 其他数值类型：先转到 Int32 再比较
        ret this.compare( BigNumber( other.toInt32() ) )
    }

    override Int32 toInt32()
    {
        Int32 r = 0
        int i = this._length - 1
        while i >= 0
        {
            r = r * BigNumber.BASE + this._digits[i]
            i = i - 1
        }
        if this._sign < 0
        {
            r = 0 - r
        }
        ret r
    }

    override Float32 toFloat32()
    {
        Float32 r = this.toInt32()
        ret r
    }

    override Float64 toFloat64()
    {
        Float64 r = this.toInt32()
        ret r
    }

    # ── 静态工具 ─────────────────────────────────────────
    public static get BigNumber zero()
    {
        ret BigNumber( 0 )
    }

    public static get BigNumber one()
    {
        ret BigNumber( 1 )
    }

    # 从十进制字符串解析（支持可选 +/- 号）
    public static BigNumber parse( string text )
    {
        BigNumber r = BigNumber()
        if text == null
        {
            ret r
        }

        int start = 0
        Int32 sign = 1
        # 首位符号判断依赖运行时字符串索引；这里以无符号解析后再定符号
        string body = text
        int len = body.length

        BigNumber acc = BigNumber()
        BigNumber pow = BigNumber( 1 )
        int i = len - 1
        while i >= 0
        {
            int digit = BigNumber._charToDigit( body, i )
            if digit >= 0
            {
                acc = acc.add( BigNumber( digit ).multiply( pow ) )
                pow = pow.multiply( BigNumber( 10 ) )
            }
            i = i - 1
        }

        # 负号：字符串首字符为 '-'
        if len > 0
        {
            int first = BigNumber._charToDigit( body, 0 )
            if first < 0
            {
                # 首字符非数字，按负号处理（'-'）
                acc._sign = 0 - acc._sign
            }
        }
        ret acc
    }

    # 取字符串指定位置的数字值，非数字返回 -1
    static int _charToDigit( string s, int index )
    {
        int code = s.charAt( index )
        if code >= 48 && code <= 57
        {
            ret code - 48
        }
        ret 0 - 1
    }

    public static BigNumber valueOf( Int32 v )
    {
        ret BigNumber( v )
    }

    public static BigNumber absValue( BigNumber v )
    {
        ret v.abs() as BigNumber
    }

    public static BigNumber maxValue( BigNumber a, BigNumber b )
    {
        if a.compare( b ) >= 0
        {
            ret a
        }
        ret b
    }

    public static BigNumber minValue( BigNumber a, BigNumber b )
    {
        if a.compare( b ) <= 0
        {
            ret a
        }
        ret b
    }

    # 幂运算（快速幂，指数为非负 Int32）
    public static BigNumber pow( BigNumber baseValue, Int32 exponent )
    {
        BigNumber result = BigNumber( 1 )
        BigNumber b = baseValue.clone()
        Int32 e = exponent
        while e > 0
        {
            if e % 2 == 1
            {
                result = result.multiply( b )
            }
            b = b.multiply( b )
            e = e / 2
        }
        ret result
    }

    # 阶乘
    public static BigNumber factorial( Int32 n )
    {
        BigNumber r = BigNumber( 1 )
        Int32 i = 2
        while i <= n
        {
            r = r.multiply( BigNumber( i ) )
            i = i + 1
        }
        ret r
    }

    # 最大公约数（欧几里得）
    public static BigNumber gcd( BigNumber a, BigNumber b )
    {
        BigNumber x = a.abs() as BigNumber
        BigNumber y = b.abs() as BigNumber
        while !y._isZero()
        {
            BigNumber t = x.mod( y )
            x = y
            y = t
        }
        ret x
    }

    override string toString()
    {
        if this._isZero()
        {
            ret "0"
        }
        string s = ""
        if this._sign < 0
        {
            s = "-"
        }
        s = s + this._digits[ this._length - 1 ].toString()
        int i = this._length - 2
        while i >= 0
        {
            s = s + BigNumber._pad4( this._digits[i] )
            i = i - 1
        }
        ret s
    }

    # 每段补足 4 位十进制
    static string _pad4( Int32 v )
    {
        string s = v.toString()
        while s.length < BigNumber.BASE_DIGITS
        {
            s = "0" + s
        }
        ret s
    }
}
