# 颜色（RGBA，各分量 Float32，范围 [0,1]）。
# 与 Math 库的 Float32_3 / Float32_4 互转，便于直接送入着色器 / 顶点缓冲。
public class Color
{
    public Float32 r = 0.0f
    public Float32 g = 0.0f
    public Float32 b = 0.0f
    public Float32 a = 1.0f

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.r = 0.0f
        this.g = 0.0f
        this.b = 0.0f
        this.a = 1.0f
    }

    public void _init_( Float32 _r, Float32 _g, Float32 _b )
    {
        this.r = _r
        this.g = _g
        this.b = _b
        this.a = 1.0f
    }

    public void _init_( Float32 _r, Float32 _g, Float32 _b, Float32 _a )
    {
        this.r = _r
        this.g = _g
        this.b = _b
        this.a = _a
    }

    public void _init_( Float32_3 rgb )
    {
        this.r = rgb.x
        this.g = rgb.y
        this.b = rgb.z
        this.a = 1.0f
    }

    public void _init_( Float32_3 rgb, Float32 _a )
    {
        this.r = rgb.x
        this.g = rgb.y
        this.b = rgb.z
        this.a = _a
    }

    public void _init_( Float32_4 rgba )
    {
        this.r = rgba.x
        this.g = rgba.y
        this.b = rgba.z
        this.a = rgba.w
    }

    # ── 索引访问 ─────────────────────────────────────────
    Float32 _getItem_( int index )
    {
        if index == 0
        {
            ret this.r
        }
        if index == 1
        {
            ret this.g
        }
        if index == 2
        {
            ret this.b
        }
        ret this.a
    }

    void _setItem_( int index, Float32 value )
    {
        if index == 0
        {
            this.r = value
        }
        elif index == 1
        {
            this.g = value
        }
        elif index == 2
        {
            this.b = value
        }
        else
        {
            this.a = value
        }
    }

    # ── 运算符重载 ───────────────────────────────────────
    override Color _add_( Object obj1 )
    {
        if obj1 is Color c
        {
            ret Color( this.r + c.r, this.g + c.g, this.b + c.b, this.a + c.a )
        }
        ret this
    }

    override Color _sub_( Object obj1 )
    {
        if obj1 is Color c
        {
            ret Color( this.r - c.r, this.g - c.g, this.b - c.b, this.a - c.a )
        }
        ret this
    }

    # 支持 Color * Color（分量乘）与 Color * Float32（整体缩放）
    override Color _mul_( Object obj1 )
    {
        if obj1 is Color c
        {
            ret Color( this.r * c.r, this.g * c.g, this.b * c.b, this.a * c.a )
        }
        if obj1 is Float32 s
        {
            ret Color( this.r * s, this.g * s, this.b * s, this.a * s )
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Color c
        {
            ret this.r == c.r && this.g == c.g && this.b == c.b && this.a == c.a
        }
        ret false
    }

    override bool _ne_( Object obj1 )
    {
        ret !this._eq_( obj1 )
    }

    # ── 实例方法 ─────────────────────────────────────────
    Color lerp( Color other, Float32 t )
    {
        Float32 k = Mathf.clamp( t, 0.0f, 1.0f )
        ret Color( this.r + ( other.r - this.r ) * k,
                   this.g + ( other.g - this.g ) * k,
                   this.b + ( other.b - this.b ) * k,
                   this.a + ( other.a - this.a ) * k )
    }

    # 保持 rgb，替换 alpha
    Color withAlpha( Float32 alpha )
    {
        ret Color( this.r, this.g, this.b, alpha )
    }

    # 各分量限制到 [0,1]
    Color clamp01()
    {
        ret Color( Mathf.clamp( this.r, 0.0f, 1.0f ),
                   Mathf.clamp( this.g, 0.0f, 1.0f ),
                   Mathf.clamp( this.b, 0.0f, 1.0f ),
                   Mathf.clamp( this.a, 0.0f, 1.0f ) )
    }

    # 灰度亮度（Rec.709）
    Float32 luminance()
    {
        ret 0.2126f * this.r + 0.7152f * this.g + 0.0722f * this.b
    }

    Float32_3 toFloat32_3()
    {
        ret Float32_3( this.r, this.g, this.b )
    }

    Float32_4 toFloat32_4()
    {
        ret Float32_4( this.r, this.g, this.b, this.a )
    }

    Color set( Float32 _r, Float32 _g, Float32 _b, Float32 _a )
    {
        this.r = _r
        this.g = _g
        this.b = _b
        this.a = _a
        ret this
    }

    Color clone()
    {
        ret Color( this.r, this.g, this.b, this.a )
    }

    # ── 静态常量 ─────────────────────────────────────────
    public static get Color white()
    {
        ret Color( 1.0f, 1.0f, 1.0f, 1.0f )
    }

    public static get Color black()
    {
        ret Color( 0.0f, 0.0f, 0.0f, 1.0f )
    }

    public static get Color clear()
    {
        ret Color( 0.0f, 0.0f, 0.0f, 0.0f )
    }

    public static get Color red()
    {
        ret Color( 1.0f, 0.0f, 0.0f, 1.0f )
    }

    public static get Color green()
    {
        ret Color( 0.0f, 1.0f, 0.0f, 1.0f )
    }

    public static get Color blue()
    {
        ret Color( 0.0f, 0.0f, 1.0f, 1.0f )
    }

    public static get Color yellow()
    {
        ret Color( 1.0f, 1.0f, 0.0f, 1.0f )
    }

    public static get Color cyan()
    {
        ret Color( 0.0f, 1.0f, 1.0f, 1.0f )
    }

    public static get Color magenta()
    {
        ret Color( 1.0f, 0.0f, 1.0f, 1.0f )
    }

    public static get Color gray()
    {
        ret Color( 0.5f, 0.5f, 0.5f, 1.0f )
    }

    # ── 静态工具 ─────────────────────────────────────────
    public static Color lerp( Color a, Color b, Float32 t )
    {
        ret a.lerp( b, t )
    }

    # HSV -> RGB，h/s/v 均在 [0,1]
    public static Color fromHSV( Float32 h, Float32 s, Float32 v )
    {
        ret Color.fromHSV( h, s, v, 1.0f )
    }

    public static Color fromHSV( Float32 h, Float32 s, Float32 v, Float32 alpha )
    {
        if s <= 0.0f
        {
            ret Color( v, v, v, alpha )
        }

        Float32 hh = h
        while hh < 0.0f
        {
            hh = hh + 1.0f
        }
        while hh >= 1.0f
        {
            hh = hh - 1.0f
        }
        Float32 sector = hh * 6.0f
        Int32 i = Mathf.truncate( sector )
        Float32 f = sector - i
        Float32 p = v * ( 1.0f - s )
        Float32 q = v * ( 1.0f - s * f )
        Float32 t = v * ( 1.0f - s * ( 1.0f - f ) )

        if i == 0
        {
            ret Color( v, t, p, alpha )
        }
        if i == 1
        {
            ret Color( q, v, p, alpha )
        }
        if i == 2
        {
            ret Color( p, v, t, alpha )
        }
        if i == 3
        {
            ret Color( p, q, v, alpha )
        }
        if i == 4
        {
            ret Color( t, p, v, alpha )
        }
        ret Color( v, p, q, alpha )
    }

    # RGB -> HSV，返回 Float32_3(h, s, v)
    public static Float32_3 toHSV( Color c )
    {
        Float32 maxV = Mathf.max( c.r, Mathf.max( c.g, c.b ) )
        Float32 minV = Mathf.min( c.r, Mathf.min( c.g, c.b ) )
        Float32 delta = maxV - minV

        Float32 h = 0.0f
        if delta > 0.0f
        {
            if maxV == c.r
            {
                h = ( c.g - c.b ) / delta
            }
            elif maxV == c.g
            {
                h = 2.0f + ( c.b - c.r ) / delta
            }
            else
            {
                h = 4.0f + ( c.r - c.g ) / delta
            }
            h = h / 6.0f
            if h < 0.0f
            {
                h = h + 1.0f
            }
        }

        Float32 s = 0.0f
        if maxV > 0.0f
        {
            s = delta / maxV
        }
        ret Float32_3( h, s, maxV )
    }

    # 0xRRGGBB 整数 -> Color
    public static Color fromHexRGB( Int32 hex )
    {
        Float32 r = ( ( hex & 0xFF0000 ) / 0x010000 ) / 255.0f
        Float32 g = ( ( hex & 0x00FF00 ) / 0x000100 ) / 255.0f
        Float32 b = ( hex & 0x0000FF ) / 255.0f
        ret Color( r, g, b, 1.0f )
    }

    # 0xAARRGGBB 整数 -> Color
    public static Color fromHexARGB( Int32 hex )
    {
        Float32 a = ( ( hex & 0xFF000000 ) / 0x01000000 ) / 255.0f
        Float32 r = ( ( hex & 0x00FF0000 ) / 0x00010000 ) / 255.0f
        Float32 g = ( ( hex & 0x0000FF00 ) / 0x00000100 ) / 255.0f
        Float32 b = ( hex & 0x000000FF ) / 255.0f
        ret Color( r, g, b, a )
    }

    override string toString()
    {
        ret String.toFormat( "Color(r={0}, g={1}, b={2}, a={3})", this.r, this.g, this.b, this.a )
    }
}
