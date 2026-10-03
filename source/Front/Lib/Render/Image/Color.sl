# =========================================================================
# Image/Color.sl —— RGBA 颜色
#
# 分量以 0..255 整数存储（与 8 位图像天然对齐）。
# HSL / YCbCr / alpha 合成 / 亮度 全部用纯 SL 实现，不依赖任何后端。
#
# 注意：浮点转整数统一走 SystemConvertInt32（显式、可读、不依赖隐式截断）。
# =========================================================================

namespace Image
{
    public class Color
    {
        public Int32 r = 0
        public Int32 g = 0
        public Int32 b = 0
        public Int32 a = 255

        _init_()
        {
            this.r = 0
            this.g = 0
            this.b = 0
            this.a = 255
        }

        _init_( Int32 _r, Int32 _g, Int32 _b )
        {
            this.r = _r
            this.g = _g
            this.b = _b
            this.a = 255
            this.clamp()
        }

        _init_( Int32 _r, Int32 _g, Int32 _b, Int32 _a )
        {
            this.r = _r
            this.g = _g
            this.b = _b
            this.a = _a
            this.clamp()
        }

        # ── 构造 ─────────────────────────────────────────────
        public static Color fromRgb( Int32 r, Int32 g, Int32 b )
        {
            ret Color( r, g, b )
        }

        public static Color fromRgba( Int32 r, Int32 g, Int32 b, Int32 a )
        {
            ret Color( r, g, b, a )
        }

        # 0xRRGGBBAA
        public static Color fromPackedRgba( Int32 packed )
        {
            Int32 rr = ( packed >> 24 ) & 0xFF
            Int32 gg = ( packed >> 16 ) & 0xFF
            Int32 bb = ( packed >> 8 ) & 0xFF
            Int32 aa = packed & 0xFF
            ret Color( rr, gg, bb, aa )
        }

        # 0xAARRGGBB
        public static Color fromPackedArgb( Int32 packed )
        {
            Int32 aa = ( packed >> 24 ) & 0xFF
            Int32 rr = ( packed >> 16 ) & 0xFF
            Int32 gg = ( packed >> 8 ) & 0xFF
            Int32 bb = packed & 0xFF
            ret Color( rr, gg, bb, aa )
        }

        public static Color black()
        {
            ret Color( 0, 0, 0 )
        }

        public static Color white()
        {
            ret Color( 255, 255, 255 )
        }

        public static Color transparent()
        {
            ret Color( 0, 0, 0, 0 )
        }

        # ── 打包 ─────────────────────────────────────────────
        public Int32 toPackedRgba()
        {
            ret ( ( this.r & 0xFF ) << 24 ) | ( ( this.g & 0xFF ) << 16 ) |
                ( ( this.b & 0xFF ) << 8 ) | ( this.a & 0xFF )
        }

        public Int32 toPackedArgb()
        {
            ret ( ( this.a & 0xFF ) << 24 ) | ( ( this.r & 0xFF ) << 16 ) |
                ( ( this.g & 0xFF ) << 8 ) | ( this.b & 0xFF )
        }

        # ── 属性 ─────────────────────────────────────────────
        public get bool isOpaque()
        {
            ret this.a >= 255
        }

        public get bool isTransparent()
        {
            ret this.a <= 0
        }

        # Rec.709 亮度，范围 0..1
        public get Float32 luma()
        {
            Float32 rr = 1.0f * this.r / 255.0f
            Float32 gg = 1.0f * this.g / 255.0f
            Float32 bb = 1.0f * this.b / 255.0f
            ret 0.2126f * rr + 0.7152f * gg + 0.0722f * bb
        }

        # ── 运算 ─────────────────────────────────────────────
        public void clamp()
        {
            if this.r < 0 { this.r = 0 }
            if this.r > 255 { this.r = 255 }
            if this.g < 0 { this.g = 0 }
            if this.g > 255 { this.g = 255 }
            if this.b < 0 { this.b = 0 }
            if this.b > 255 { this.b = 255 }
            if this.a < 0 { this.a = 0 }
            if this.a > 255 { this.a = 255 }
        }

        public Color withAlpha( Int32 alpha )
        {
            ret Color( this.r, this.g, this.b, alpha )
        }

        public Color invert()
        {
            ret Color( 255 - this.r, 255 - this.g, 255 - this.b, this.a )
        }

        public Color grayscale()
        {
            Int32 y = Color.toByte( this.luma() )
            ret Color( y, y, y, this.a )
        }

        public Color lerp( Color other, Float32 t )
        {
            if t < 0.0f { t = 0.0f }
            if t > 1.0f { t = 1.0f }
            Int32 nr = this.r + Color.toInt( ( 1.0f * other.r - this.r ) * t )
            Int32 ng = this.g + Color.toInt( ( 1.0f * other.g - this.g ) * t )
            Int32 nb = this.b + Color.toInt( ( 1.0f * other.b - this.b ) * t )
            Int32 na = this.a + Color.toInt( ( 1.0f * other.a - this.a ) * t )
            ret Color( nr, ng, nb, na )
        }

        # 预乘 alpha（合成 / 上传 GPU 前的常见预处理）
        public Color premultiply()
        {
            Float32 f = 1.0f * this.a / 255.0f
            ret Color( Color.toInt( 1.0f * this.r * f ),
                       Color.toInt( 1.0f * this.g * f ),
                       Color.toInt( 1.0f * this.b * f ), this.a )
        }

        public Color unpremultiply()
        {
            if this.a <= 0
            {
                ret Color( 0, 0, 0, 0 )
            }
            Float32 f = 255.0f / ( 1.0f * this.a )
            ret Color( Color.toInt( 1.0f * this.r * f ),
                       Color.toInt( 1.0f * this.g * f ),
                       Color.toInt( 1.0f * this.b * f ), this.a )
        }

        # ── alpha 合成：src over dst ─────────────────────────
        public static Color blendOver( Color dst, Color src )
        {
            if src.a >= 255
            {
                ret src
            }
            if src.a <= 0
            {
                ret dst
            }
            Float32 sa = 1.0f * src.a / 255.0f
            Float32 da = 1.0f * dst.a / 255.0f
            Float32 oa = sa + da * ( 1.0f - sa )
            if oa <= 0.0f
            {
                ret Color( 0, 0, 0, 0 )
            }
            Float32 rr = ( 1.0f * src.r * sa + 1.0f * dst.r * da * ( 1.0f - sa ) ) / oa
            Float32 gg = ( 1.0f * src.g * sa + 1.0f * dst.g * da * ( 1.0f - sa ) ) / oa
            Float32 bb = ( 1.0f * src.b * sa + 1.0f * dst.b * da * ( 1.0f - sa ) ) / oa
            ret Color( Color.toInt( rr ), Color.toInt( gg ), Color.toInt( bb ),
                       Color.toByte( oa ) )
        }

        # ── HSL：返回 [h(0..360), s(0..1), l(0..1)] ──────────
        public Array<Float32> toHsl()
        {
            Array<Float32> out = Array<Float32>( 3 )
            Float32 rr = 1.0f * this.r / 255.0f
            Float32 gg = 1.0f * this.g / 255.0f
            Float32 bb = 1.0f * this.b / 255.0f
            Float32 max = Color.max3( rr, gg, bb )
            Float32 min = Color.min3( rr, gg, bb )
            Float32 l = ( max + min ) * 0.5f
            Float32 h = 0.0f
            Float32 s = 0.0f
            if max > min
            {
                Float32 d = max - min
                if l > 0.5f
                {
                    s = d / ( 2.0f - max - min )
                }
                else
                {
                    s = d / ( max + min )
                }
                if max == rr
                {
                    h = ( gg - bb ) / d
                    if gg < bb { h = h + 6.0f }
                }
                elif max == gg
                {
                    h = ( bb - rr ) / d + 2.0f
                }
                else
                {
                    h = ( rr - gg ) / d + 4.0f
                }
                h = h * 60.0f
            }
            out[0] = h
            out[1] = s
            out[2] = l
            ret out
        }

        public static Color fromHsl( Float32 h, Float32 s, Float32 l )
        {
            if s <= 0.0f
            {
                Int32 v = Color.toByte( l )
                ret Color( v, v, v )
            }
            Float32 hh = h / 60.0f
            while hh >= 6.0f
            {
                hh = hh - 6.0f
            }
            while hh < 0.0f
            {
                hh = hh + 6.0f
            }
            Float32 q = 0.0f
            if l < 0.5f
            {
                q = l * ( 1.0f + s )
            }
            else
            {
                q = l + s - l * s
            }
            Float32 p = 2.0f * l - q
            Float32 rr = Color.hue2rgb( p, q, hh + 2.0f )
            Float32 gg = Color.hue2rgb( p, q, hh )
            Float32 bb = Color.hue2rgb( p, q, hh - 2.0f )
            ret Color( Color.toByte( rr ), Color.toByte( gg ), Color.toByte( bb ) )
        }

        # ── YCbCr（JPEG / 视频内部色彩空间）───────────────────
        public Array<Float32> toYCbCr()
        {
            Array<Float32> out = Array<Float32>( 3 )
            Float32 rr = 1.0f * this.r
            Float32 gg = 1.0f * this.g
            Float32 bb = 1.0f * this.b
            out[0] = 0.299f * rr + 0.587f * gg + 0.114f * bb
            out[1] = 128.0f - 0.168736f * rr - 0.331264f * gg + 0.5f * bb
            out[2] = 128.0f + 0.5f * rr - 0.418688f * gg - 0.081312f * bb
            ret out
        }

        public static Color fromYCbCr( Float32 y, Float32 cb, Float32 cr )
        {
            Float32 rr = y + 1.402f * ( cr - 128.0f )
            Float32 gg = y - 0.344136f * ( cb - 128.0f ) - 0.714136f * ( cr - 128.0f )
            Float32 bb = y + 1.772f * ( cb - 128.0f )
            ret Color( Color.toByte( rr / 255.0f ), Color.toByte( gg / 255.0f ),
                       Color.toByte( bb / 255.0f ) )
        }

        # ── 内部工具 ─────────────────────────────────────────
        # 0..1 的浮点转 0..255 字节（带四舍五入 + 饱和）
        public static Int32 toByte( Float32 v )
        {
            if v < 0.0f
            {
                ret 0
            }
            if v > 1.0f
            {
                ret 255
            }
            ret SystemConvertInt32( v * 255.0f + 0.5f )
        }

        # 浮点转整数（四舍五入，支持负值）
        public static Int32 toInt( Float32 v )
        {
            if v >= 0.0f
            {
                ret SystemConvertInt32( v + 0.5f )
            }
            ret 0 - SystemConvertInt32( 0.0f - v + 0.5f )
        }

        public static Float32 max3( Float32 a, Float32 b, Float32 c )
        {
            Float32 m = a
            if b > m { m = b }
            if c > m { m = c }
            ret m
        }

        public static Float32 min3( Float32 a, Float32 b, Float32 c )
        {
            Float32 m = a
            if b < m { m = b }
            if c < m { m = c }
            ret m
        }

        static Float32 hue2rgb( Float32 p, Float32 q, Float32 t )
        {
            Float32 tt = t
            if tt < 0.0f { tt = tt + 6.0f }
            if tt >= 6.0f { tt = tt - 6.0f }
            if tt < 1.0f { ret p + ( q - p ) * tt }
            if tt < 3.0f { ret q }
            if tt < 4.0f { ret p + ( q - p ) * ( 4.0f - tt ) }
            ret p
        }

        override string toString()
        {
            ret "Color(" + this.r.toString() + "," + this.g.toString() + "," +
                this.b.toString() + "," + this.a.toString() + ")"
        }

        override bool equals( object obj )
        {
            Color o = obj as Color
            if o == null
            {
                ret false
            }
            ret this.r == o.r && this.g == o.g && this.b == o.b && this.a == o.a
        }
    }
}
