@Nickname("Decimal")
public class BigDecimal extends Num
{
    # ── 内部表示 ─────────────────────────────────────────
    # 值 = _unscaled / 10^_scale
    # 例：_unscaled = 12345, _scale = 3  →  12.345
    BigNumber _unscaled = null
    public Int32 scale = 0

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this._unscaled = BigNumber( 0 )
        this.scale = 0
    }

    public void _init_( Int32 v )
    {
        this._unscaled = BigNumber( v )
        this.scale = 0
    }

    public void _init_( BigNumber unscaled, Int32 _scale )
    {
        this._unscaled = unscaled
        this.scale = _scale
    }

    # 由整数 + 小数位数构造：value / 10^scale
    public void _init_( Int32 value, Int32 _scale )
    {
        this._unscaled = BigNumber( value )
        this.scale = _scale
    }

    public void _init_( string text )
    {
        BigDecimal parsed = BigDecimal.parse( text )
        this._unscaled = parsed._unscaled
        this.scale = parsed.scale
    }

    # ── 内部工具 ─────────────────────────────────────────
    BigNumber _unscaledAtScale( Int32 targetScale )
    {
        if targetScale == this.scale
        {
            ret this._unscaled.clone()
        }
        if targetScale > this.scale
        {
            Int32 diff = targetScale - this.scale
            ret this._unscaled.multiply( BigNumber.pow( BigNumber( 10 ), diff ) )
        }
        Int32 diff = this.scale - targetScale
        ret this._unscaled.div( BigNumber.pow( BigNumber( 10 ), diff ) )
    }

    # 对齐两个操作数的 scale，返回公共 scale（写入 outA / outB）
    Int32 _align( BigDecimal other, BigNumber outA, BigNumber outB )
    {
        Int32 target = Mathd.max( this.scale, other.scale )
        BigNumber a = this._unscaledAtScale( target )
        BigNumber b = other._unscaledAtScale( target )
        outA._copyFrom( a )
        outB._copyFrom( b )
        ret target
    }

    # ── 算术 ─────────────────────────────────────────────
    public BigDecimal add( BigDecimal other )
    {
        BigNumber a = BigNumber()
        BigNumber b = BigNumber()
        Int32 target = this._align( other, a, b )
        ret BigDecimal( a.add( b ), target )
    }

    public BigDecimal sub( BigDecimal other )
    {
        BigNumber a = BigNumber()
        BigNumber b = BigNumber()
        Int32 target = this._align( other, a, b )
        ret BigDecimal( a.sub( b ), target )
    }

    public BigDecimal multiply( BigDecimal other )
    {
        BigNumber r = this._unscaled.multiply( other._unscaled )
        ret BigDecimal( r, this.scale + other.scale )
    }

    # 除法：结果保留 dividend.scale + extraScale 位小数（默认补足到 8 位）
    public BigDecimal div( BigDecimal other, Int32 extraScale = 8 )
    {
        if other._unscaled._isZero()
        {
            ret BigDecimal( BigNumber( 0 ), 0 )
        }
        Int32 targetScale = this.scale + extraScale
        BigNumber a = this._unscaled.multiply( BigNumber.pow( BigNumber( 10 ), targetScale + other.scale ) )
        BigNumber r = a.div( other._unscaled )
        ret BigDecimal( r, targetScale )
    }

    public BigDecimal negate()
    {
        ret BigDecimal( this._unscaled.negate(), this.scale )
    }

    public BigDecimal abs()
    {
        ret BigDecimal( this._unscaled.abs() as BigNumber, this.scale )
    }

    public BigDecimal clone()
    {
        ret BigDecimal( this._unscaled.clone(), this.scale )
    }

    # 调整小数位数（截断）
    public BigDecimal rescale( Int32 targetScale )
    {
        ret BigDecimal( this._unscaledAtScale( targetScale ), targetScale )
    }

    # 四舍五入到指定小数位
    public BigDecimal roundTo( Int32 targetScale )
    {
        if targetScale >= this.scale
        {
            ret this.clone()
        }
        BigNumber pow = BigNumber.pow( BigNumber( 10 ), this.scale - targetScale )
        BigNumber q = this._unscaled.div( pow )
        BigNumber r = this._unscaled.mod( pow )
        BigNumber half = pow.div( BigNumber( 2 ) )
        if r._absCompare( half ) >= 0
        {
            q = q.add( BigNumber( 1 ) )
        }
        ret BigDecimal( q, targetScale )
    }

    public int compare( BigDecimal other )
    {
        BigNumber a = BigNumber()
        BigNumber b = BigNumber()
        this._align( other, a, b )
        ret a.compare( b )
    }

    # ── 运算符重载 ───────────────────────────────────────
    override BigDecimal _add_( Object obj1 )
    {
        if obj1 is BigDecimal d
        {
            ret this.add( d )
        }
        if obj1 is Int32 i
        {
            ret this.add( BigDecimal( i ) )
        }
        ret this
    }

    override BigDecimal _sub_( Object obj1 )
    {
        if obj1 is BigDecimal d
        {
            ret this.sub( d )
        }
        if obj1 is Int32 i
        {
            ret this.sub( BigDecimal( i ) )
        }
        ret this
    }

    override BigDecimal _mul_( Object obj1 )
    {
        if obj1 is BigDecimal d
        {
            ret this.multiply( d )
        }
        if obj1 is Int32 i
        {
            ret this.multiply( BigDecimal( i ) )
        }
        ret this
    }

    override BigDecimal _truediv_( Object obj1 )
    {
        if obj1 is BigDecimal d
        {
            ret this.div( d )
        }
        if obj1 is Int32 i
        {
            ret this.div( BigDecimal( i ) )
        }
        ret this
    }

    override bool _lt_( Object obj1 )
    {
        ret this._compareBoxed( obj1 ) < 0
    }

    override bool _le_( Object obj1 )
    {
        ret this._compareBoxed( obj1 ) <= 0
    }

    override bool _gt_( Object obj1 )
    {
        ret this._compareBoxed( obj1 ) > 0
    }

    override bool _ge_( Object obj1 )
    {
        ret this._compareBoxed( obj1 ) >= 0
    }

    override bool _eq_( Object obj1 )
    {
        ret this._compareBoxed( obj1 ) == 0
    }

    override bool _ne_( Object obj1 )
    {
        ret this._compareBoxed( obj1 ) != 0
    }

    int _compareBoxed( Object obj1 )
    {
        if obj1 is BigDecimal d
        {
            ret this.compare( d )
        }
        if obj1 is Int32 i
        {
            ret this.compare( BigDecimal( i ) )
        }
        ret 1
    }

    # ── Num 接口实现 ─────────────────────────────────────
    override get int size()
    {
        ret this._unscaled.size()
    }

    override get int byteLength()
    {
        ret this._unscaled.byteLength()
    }

    public override Num abs()
    {
        ret this.abs()
    }

    # 向下取整（去掉小数部分）
    public override Num floor()
    {
        if this.scale <= 0
        {
            ret this.clone()
        }
        BigNumber pow = BigNumber.pow( BigNumber( 10 ), this.scale )
        BigNumber q = this._unscaled.div( pow )
        if this._unscaled._sign < 0 && this._unscaled.mod( pow )._isZero() == false
        {
            q = q.sub( BigNumber( 1 ) )
        }
        ret BigDecimal( q, 0 )
    }

    # 向上取整
    public override Num ceil()
    {
        if this.scale <= 0
        {
            ret this.clone()
        }
        BigNumber pow = BigNumber.pow( BigNumber( 10 ), this.scale )
        BigNumber q = this._unscaled.div( pow )
        if this._unscaled._sign > 0 && this._unscaled.mod( pow )._isZero() == false
        {
            q = q.add( BigNumber( 1 ) )
        }
        ret BigDecimal( q, 0 )
    }

    public override Int8 compareTo( Num other )
    {
        if other == null
        {
            ret 1
        }
        if other is BigDecimal d
        {
            int cmp = this.compare( d )
            ret cmp < 0 ? 0 - 1 : ( cmp > 0 ? 1 : 0 )
        }
        ret this.compare( BigDecimal( other.toInt32() ) ) < 0 ? 0 - 1 : 1
    }

    # 取整数部分（截断小数）
    override Int32 toInt32()
    {
        if this.scale <= 0
        {
            ret this._unscaled.toInt32()
        }
        BigNumber pow = BigNumber.pow( BigNumber( 10 ), this.scale )
        ret this._unscaled.div( pow ).toInt32()
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
    public static get BigDecimal zero()
    {
        ret BigDecimal( BigNumber( 0 ), 0 )
    }

    public static get BigDecimal one()
    {
        ret BigDecimal( BigNumber( 1 ), 0 )
    }

    public static BigDecimal fromInt32( Int32 v )
    {
        ret BigDecimal( v )
    }

    # 由未缩放整数 + 小数位构造
    public static BigDecimal fromUnscaled( BigNumber unscaled, Int32 scale )
    {
        ret BigDecimal( unscaled, scale )
    }

    # 解析十进制字符串：支持 "12.345" / "-0.5" / "100"
    # 说明：依赖运行时的字符串长度与字符访问能力；若运行时未提供字符索引，
    #      请改用 fromUnscaled(BigNumber.parse(intPart), fracLen) 组合构造。
    public static BigDecimal parse( string text )
    {
        if text == null
        {
            ret BigDecimal.zero()
        }
        BigNumber n = BigNumber.parse( text )
        ret BigDecimal( n, BigDecimal._countFractionDigits( text ) )
    }

    # 统计小数点后的位数（无小数点返回 0）
    static Int32 _countFractionDigits( string text )
    {
        int len = text.length
        int i = len - 1
        int digits = 0
        while i >= 0
        {
            int c = BigDecimal._charCodeAt( text, i )
            if c == 46
            {
                ret digits
            }
            if c >= 48 && c <= 57
            {
                digits = digits + 1
            }
            i = i - 1
        }
        ret 0
    }

    static int _charCodeAt( string s, int index )
    {
        ret s.charAt( index )
    }

    public static BigDecimal maxValue( BigDecimal a, BigDecimal b )
    {
        if a.compare( b ) >= 0
        {
            ret a
        }
        ret b
    }

    public static BigDecimal minValue( BigDecimal a, BigDecimal b )
    {
        if a.compare( b ) <= 0
        {
            ret a
        }
        ret b
    }

    override string toString()
    {
        if this._unscaled._isZero()
        {
            ret "0"
        }
        if this.scale <= 0
        {
            ret this._unscaled.toString()
        }

        BigNumber pow = BigNumber.pow( BigNumber( 10 ), this.scale )
        BigNumber intPart = this._unscaled.div( pow )
        BigNumber fracPart = ( this._unscaled.mod( pow ) ).abs() as BigNumber

        string fracStr = fracPart.toString()
        int pad = this.scale - fracStr.length
        while pad > 0
        {
            fracStr = "0" + fracStr
            pad = pad - 1
        }
        ret intPart.toString() + "." + fracStr
    }
}
