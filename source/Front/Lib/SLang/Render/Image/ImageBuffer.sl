# =========================================================================
# Image/ImageBuffer.sl —— CPU 侧像素缓冲（本目录的核心）
#
# 数据布局：**紧密排布、无行对齐填充**，stride = width * bytesPerPixel。
# 这样 (x,y) 的偏移就是 (y * width + x) * bpp，纯 SL 可以高效寻址。
#
# 纯 SL 实现：读写像素 / 填充 / 裁剪 / 翻转 / 最近邻与双线性缩放 /
#             像素格式互转 / 预乘 alpha。
# 依赖后端：  decode() / encode()（PNG、JPEG、WebP 的熵编解码）。
#
# 数值转换统一走 SystemConvertInt32 / SystemConvertUInt8（Core 已实现），
# 不使用隐式截断，避免不同后端行为不一致。
# =========================================================================

namespace Image
{
    public class ImageBuffer
    {
        public Int32 width = 0
        public Int32 height = 0
        public EPixelFormat pixelFormat = EPixelFormat.Rgba8
        public EColorSpace colorSpace = EColorSpace.SRGB

        # 像素数据，长度 = width * height * bytesPerPixel
        Array<UInt8> _data = null

        _init_()
        {
            this.width = 0
            this.height = 0
            this.pixelFormat = EPixelFormat.Rgba8
            this.colorSpace = EColorSpace.SRGB
            this._data = Array<UInt8>( 0 )
        }

        _init_( Int32 _width, Int32 _height )
        {
            this.width = _width
            this.height = _height
            this.pixelFormat = EPixelFormat.Rgba8
            this.colorSpace = EColorSpace.SRGB
            this._data = Array<UInt8>( _width * _height * 4 )
        }

        _init_( Int32 _width, Int32 _height, EPixelFormat _fmt )
        {
            this.width = _width
            this.height = _height
            this.pixelFormat = _fmt
            this.colorSpace = EColorSpace.SRGB
            this._data = Array<UInt8>( _width * _height * ImageBuffer.bytesPerPixelOf( _fmt ) )
        }

        # ── 静态：每种像素格式占几个字节 ─────────────────────
        public static Int32 bytesPerPixelOf( EPixelFormat fmt )
        {
            if fmt == EPixelFormat.Gray8
            {
                ret 1
            }
            if fmt == EPixelFormat.Gray16
            {
                ret 2
            }
            if fmt == EPixelFormat.Rgb8
            {
                ret 3
            }
            if fmt == EPixelFormat.Rgba8
            {
                ret 4
            }
            if fmt == EPixelFormat.Bgr8
            {
                ret 3
            }
            if fmt == EPixelFormat.Bgra8
            {
                ret 4
            }
            if fmt == EPixelFormat.Rgb16
            {
                ret 6
            }
            if fmt == EPixelFormat.Rgba16
            {
                ret 8
            }
            if fmt == EPixelFormat.Rgb32F
            {
                ret 12
            }
            if fmt == EPixelFormat.Rgba32F
            {
                ret 16
            }
            ret 0
        }

        public static Int32 channelCountOf( EPixelFormat fmt )
        {
            if fmt == EPixelFormat.Gray8 || fmt == EPixelFormat.Gray16
            {
                ret 1
            }
            if fmt == EPixelFormat.Rgb8 || fmt == EPixelFormat.Bgr8 ||
               fmt == EPixelFormat.Rgb16 || fmt == EPixelFormat.Rgb32F
            {
                ret 3
            }
            if fmt == EPixelFormat.Rgba8 || fmt == EPixelFormat.Bgra8 ||
               fmt == EPixelFormat.Rgba16 || fmt == EPixelFormat.Rgba32F
            {
                ret 4
            }
            ret 0
        }

        # ── 属性 ─────────────────────────────────────────────
        public get Int32 bytesPerPixel()
        {
            ret ImageBuffer.bytesPerPixelOf( this.pixelFormat )
        }

        public get Int32 channelCount()
        {
            ret ImageBuffer.channelCountOf( this.pixelFormat )
        }

        public get Int32 stride()
        {
            ret this.width * this.bytesPerPixel()
        }

        public get Int32 pixelCount()
        {
            ret this.width * this.height
        }

        public get Int32 byteLength()
        {
            ret this._data.length
        }

        public get bool isValid()
        {
            ret this.width > 0 && this.height > 0 &&
                this.bytesPerPixel() > 0 &&
                this._data.length >= this.width * this.height * this.bytesPerPixel()
        }

        public get bool hasAlpha()
        {
            ret this.channelCount() == 4
        }

        public Array<UInt8> data()
        {
            ret this._data
        }

        # ── 内部：字节读写（显式转换，避免隐式截断歧义）──────
        Int32 byteAt( Int32 index )
        {
            ret SystemConvertInt32( this._data[ index ] )
        }

        void setByteAt( Int32 index, Int32 value )
        {
            this._data[ index ] = SystemConvertUInt8( value )
        }

        Int32 offsetOf( Int32 x, Int32 y )
        {
            ret ( y * this.width + x ) * this.bytesPerPixel()
        }

        public bool inBounds( Int32 x, Int32 y )
        {
            ret x >= 0 && y >= 0 && x < this.width && y < this.height
        }

        # ── 像素读写 ─────────────────────────────────────────
        public Color getPixel( Int32 x, Int32 y )
        {
            if !this.inBounds( x, y )
            {
                ret Color.transparent()
            }
            Int32 o = this.offsetOf( x, y )

            if this.pixelFormat == EPixelFormat.Gray8
            {
                Int32 v = this.byteAt( o )
                ret Color( v, v, v, 255 )
            }
            if this.pixelFormat == EPixelFormat.Gray16
            {
                # 取高字节近似为 8 位
                Int32 v = this.byteAt( o + 1 )
                ret Color( v, v, v, 255 )
            }
            if this.pixelFormat == EPixelFormat.Rgb8
            {
                ret Color( this.byteAt( o ), this.byteAt( o + 1 ), this.byteAt( o + 2 ), 255 )
            }
            if this.pixelFormat == EPixelFormat.Rgba8
            {
                ret Color( this.byteAt( o ), this.byteAt( o + 1 ),
                           this.byteAt( o + 2 ), this.byteAt( o + 3 ) )
            }
            if this.pixelFormat == EPixelFormat.Bgr8
            {
                ret Color( this.byteAt( o + 2 ), this.byteAt( o + 1 ), this.byteAt( o ), 255 )
            }
            if this.pixelFormat == EPixelFormat.Bgra8
            {
                ret Color( this.byteAt( o + 2 ), this.byteAt( o + 1 ),
                           this.byteAt( o ), this.byteAt( o + 3 ) )
            }
            if this.pixelFormat == EPixelFormat.Rgb16 ||
               this.pixelFormat == EPixelFormat.Rgba16
            {
                # 每通道 2 字节，取高字节
                ret Color( this.byteAt( o + 1 ), this.byteAt( o + 3 ),
                           this.byteAt( o + 5 ), 255 )
            }
            ret Color.transparent()
        }

        public void setPixel( Int32 x, Int32 y, Color c )
        {
            if !this.inBounds( x, y ) || c == null
            {
                ret
            }
            Int32 o = this.offsetOf( x, y )

            if this.pixelFormat == EPixelFormat.Gray8
            {
                Int32 v = c.grayscale().r
                this.setByteAt( o, v )
            }
            elif this.pixelFormat == EPixelFormat.Gray16
            {
                Int32 v = c.grayscale().r
                this.setByteAt( o, 0 )
                this.setByteAt( o + 1, v )
            }
            elif this.pixelFormat == EPixelFormat.Rgb8
            {
                this.setByteAt( o, c.r )
                this.setByteAt( o + 1, c.g )
                this.setByteAt( o + 2, c.b )
            }
            elif this.pixelFormat == EPixelFormat.Rgba8
            {
                this.setByteAt( o, c.r )
                this.setByteAt( o + 1, c.g )
                this.setByteAt( o + 2, c.b )
                this.setByteAt( o + 3, c.a )
            }
            elif this.pixelFormat == EPixelFormat.Bgr8
            {
                this.setByteAt( o, c.b )
                this.setByteAt( o + 1, c.g )
                this.setByteAt( o + 2, c.r )
            }
            elif this.pixelFormat == EPixelFormat.Bgra8
            {
                this.setByteAt( o, c.b )
                this.setByteAt( o + 1, c.g )
                this.setByteAt( o + 2, c.r )
                this.setByteAt( o + 3, c.a )
            }
        }

        # ── 整块操作 ─────────────────────────────────────────
        public void fill( Color c )
        {
            if c == null || !this.isValid
            {
                ret
            }
            Int32 y = 0
            while y < this.height
            {
                Int32 x = 0
                while x < this.width
                {
                    this.setPixel( x, y, c )
                    x = x + 1
                }
                y = y + 1
            }
        }

        public ImageBuffer clone()
        {
            ImageBuffer b = ImageBuffer( this.width, this.height, this.pixelFormat )
            b.colorSpace = this.colorSpace
            Int32 i = 0
            while i < this._data.length
            {
                b._data[ i ] = this._data[ i ]
                i = i + 1
            }
            ret b
        }

        public void flipY()
        {
            if !this.isValid
            {
                ret
            }
            Int32 stride = this.stride()
            Array<UInt8> row = Array<UInt8>( stride )
            Int32 top = 0
            Int32 bottom = this.height - 1
            while top < bottom
            {
                Int32 topOff = top * stride
                Int32 bottomOff = bottom * stride
                Int32 i = 0
                while i < stride
                {
                    row[ i ] = this._data[ topOff + i ]
                    i = i + 1
                }
                i = 0
                while i < stride
                {
                    this._data[ topOff + i ] = this._data[ bottomOff + i ]
                    this._data[ bottomOff + i ] = row[ i ]
                    i = i + 1
                }
                top = top + 1
                bottom = bottom - 1
            }
        }

        public void flipX()
        {
            if !this.isValid
            {
                ret
            }
            Int32 bpp = this.bytesPerPixel()
            Int32 y = 0
            while y < this.height
            {
                Int32 left = 0
                Int32 right = this.width - 1
                while left < right
                {
                    Int32 a = ( y * this.width + left ) * bpp
                    Int32 b = ( y * this.width + right ) * bpp
                    Int32 k = 0
                    while k < bpp
                    {
                        UInt8 tmp = this._data[ a + k ]
                        this._data[ a + k ] = this._data[ b + k ]
                        this._data[ b + k ] = tmp
                        k = k + 1
                    }
                    left = left + 1
                    right = right - 1
                }
                y = y + 1
            }
        }

        public ImageBuffer crop( Int32 x0, Int32 y0, Int32 w, Int32 h )
        {
            if !this.isValid
            {
                ret null
            }
            if x0 < 0 { x0 = 0 }
            if y0 < 0 { y0 = 0 }
            if w <= 0 || h <= 0
            {
                ret null
            }
            if x0 + w > this.width { w = this.width - x0 }
            if y0 + h > this.height { h = this.height - y0 }
            if w <= 0 || h <= 0
            {
                ret null
            }
            ImageBuffer dst = ImageBuffer( w, h, this.pixelFormat )
            dst.colorSpace = this.colorSpace
            Int32 rowBytes = w * this.bytesPerPixel()
            Int32 y = 0
            while y < h
            {
                Int32 src = ( y0 + y ) * this.stride() + x0 * this.bytesPerPixel()
                Int32 dstOff = y * rowBytes
                Int32 i = 0
                while i < rowBytes
                {
                    dst._data[ dstOff + i ] = this._data[ src + i ]
                    i = i + 1
                }
                y = y + 1
            }
            ret dst
        }

        # ── 缩放（最近邻 / 双线性）───────────────────────────
        public ImageBuffer resize( Int32 newWidth, Int32 newHeight, EInterpolation mode )
        {
            if !this.isValid || newWidth <= 0 || newHeight <= 0
            {
                ret null
            }
            if mode == EInterpolation.Nearest
            {
                ret this.resizeNearest( newWidth, newHeight )
            }
            ret this.resizeBilinear( newWidth, newHeight )
        }

        ImageBuffer resizeNearest( Int32 nw, Int32 nh )
        {
            ImageBuffer dst = ImageBuffer( nw, nh, this.pixelFormat )
            dst.colorSpace = this.colorSpace
            Int32 y = 0
            while y < nh
            {
                Int32 sy = SystemConvertInt32( 1.0f * y * this.height / nh )
                if sy >= this.height { sy = this.height - 1 }
                Int32 x = 0
                while x < nw
                {
                    Int32 sx = SystemConvertInt32( 1.0f * x * this.width / nw )
                    if sx >= this.width { sx = this.width - 1 }
                    dst.setPixel( x, y, this.getPixel( sx, sy ) )
                    x = x + 1
                }
                y = y + 1
            }
            ret dst
        }

        ImageBuffer resizeBilinear( Int32 nw, Int32 nh )
        {
            ImageBuffer dst = ImageBuffer( nw, nh, this.pixelFormat )
            dst.colorSpace = this.colorSpace
            Int32 y = 0
            while y < nh
            {
                Float32 fy = 1.0f * ( y + 0.5f ) * this.height / nh - 0.5f
                if fy < 0.0f { fy = 0.0f }
                Int32 y0 = SystemConvertInt32( fy )
                Int32 y1 = y0 + 1
                Float32 wy = fy - 1.0f * y0
                if y0 < 0 { y0 = 0 }
                if y1 < 0 { y1 = 0 }
                if y0 >= this.height { y0 = this.height - 1 }
                if y1 >= this.height { y1 = this.height - 1 }

                Int32 x = 0
                while x < nw
                {
                    Float32 fx = 1.0f * ( x + 0.5f ) * this.width / nw - 0.5f
                    if fx < 0.0f { fx = 0.0f }
                    Int32 x0 = SystemConvertInt32( fx )
                    Int32 x1 = x0 + 1
                    Float32 wx = fx - 1.0f * x0
                    if x0 < 0 { x0 = 0 }
                    if x1 < 0 { x1 = 0 }
                    if x0 >= this.width { x0 = this.width - 1 }
                    if x1 >= this.width { x1 = this.width - 1 }

                    Color c00 = this.getPixel( x0, y0 )
                    Color c10 = this.getPixel( x1, y0 )
                    Color c01 = this.getPixel( x0, y1 )
                    Color c11 = this.getPixel( x1, y1 )

                    Float32 w00 = ( 1.0f - wx ) * ( 1.0f - wy )
                    Float32 w10 = wx * ( 1.0f - wy )
                    Float32 w01 = ( 1.0f - wx ) * wy
                    Float32 w11 = wx * wy

                    Int32 rr = Color.toInt( 1.0f * c00.r * w00 + 1.0f * c10.r * w10 +
                                            1.0f * c01.r * w01 + 1.0f * c11.r * w11 )
                    Int32 gg = Color.toInt( 1.0f * c00.g * w00 + 1.0f * c10.g * w10 +
                                            1.0f * c01.g * w01 + 1.0f * c11.g * w11 )
                    Int32 bb = Color.toInt( 1.0f * c00.b * w00 + 1.0f * c10.b * w10 +
                                            1.0f * c01.b * w01 + 1.0f * c11.b * w11 )
                    Int32 aa = Color.toInt( 1.0f * c00.a * w00 + 1.0f * c10.a * w10 +
                                            1.0f * c01.a * w01 + 1.0f * c11.a * w11 )
                    dst.setPixel( x, y, Color( rr, gg, bb, aa ) )
                    x = x + 1
                }
                y = y + 1
            }
            ret dst
        }

        # ── 像素格式互转 ─────────────────────────────────────
        public ImageBuffer convertTo( EPixelFormat target )
        {
            if !this.isValid
            {
                ret null
            }
            if target == this.pixelFormat
            {
                ret this.clone()
            }
            ImageBuffer dst = ImageBuffer( this.width, this.height, target )
            dst.colorSpace = this.colorSpace
            Int32 y = 0
            while y < this.height
            {
                Int32 x = 0
                while x < this.width
                {
                    dst.setPixel( x, y, this.getPixel( x, y ) )
                    x = x + 1
                }
                y = y + 1
            }
            ret dst
        }

        public void premultiplyAlpha()
        {
            if !this.hasAlpha()
            {
                ret
            }
            Int32 y = 0
            while y < this.height
            {
                Int32 x = 0
                while x < this.width
                {
                    this.setPixel( x, y, this.getPixel( x, y ).premultiply() )
                    x = x + 1
                }
                y = y + 1
            }
        }

        public void unpremultiplyAlpha()
        {
            if !this.hasAlpha()
            {
                ret
            }
            Int32 y = 0
            while y < this.height
            {
                Int32 x = 0
                while x < this.width
                {
                    this.setPixel( x, y, this.getPixel( x, y ).unpremultiply() )
                    x = x + 1
                }
                y = y + 1
            }
        }

        # ── 字节互转 ─────────────────────────────────────────
        public Array<UInt8> toBytes()
        {
            ret this._data
        }

        public static ImageBuffer fromBytes( Array<UInt8> bytes, Int32 w, Int32 h, EPixelFormat fmt )
        {
            if bytes == null || w <= 0 || h <= 0
            {
                ret null
            }
            Int32 need = w * h * ImageBuffer.bytesPerPixelOf( fmt )
            if need <= 0 || bytes.length < need
            {
                ret null
            }
            ImageBuffer b = ImageBuffer( w, h, fmt )
            Int32 i = 0
            while i < need
            {
                b._data[ i ] = bytes[ i ]
                i = i + 1
            }
            ret b
        }

        # ── 后端编解码 ───────────────────────────────────────
        # 转交图像后端（libpng / libjpeg-turbo / libwebp …）。
        # 返回压缩后的字节；失败返回 null。
        public Array<UInt8> encode( EImageFormat format, Int32 quality )
        {
            object result = SystemCallExternalFunction( "Image.encode",
                this.width, this.height, this.pixelFormat,
                this._data, format, quality )
            ret result as Array<UInt8>
        }

        # 从压缩字节解码；失败返回 null
        public static ImageBuffer decode( Array<UInt8> bytes )
        {
            if bytes == null
            {
                ret null
            }
            object result = SystemCallExternalFunction( "Image.decode", bytes )
            ret result as ImageBuffer
        }

        override string toString()
        {
            ret "ImageBuffer(" + this.width.toString() + "x" + this.height.toString() +
                ", fmt=" + this.pixelFormat.toString() + ")"
        }
    }
}
