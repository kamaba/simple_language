# =========================================================================
# Image/ImageInfo.sl —— 图像元数据 + 格式嗅探
#
# 纯 SL 实现（**不需要解码整张图**）：
#   - sniff()          ：按 magic bytes 判定容器格式
#   - fromExtension()  ：按文件扩展名判定格式
#   - probe()          ：对 PNG / BMP / GIF 直接解析头部取宽高；
#                        其余格式交给后端 SystemCallExternalFunction("Image.probe")
#
# 用途典型场景：只想知道尺寸 / 格式时不必把整张图解进内存。
# =========================================================================

namespace Image
{
    public class ImageInfo
    {
        public EImageFormat format = EImageFormat.Unknown
        public EPixelFormat pixelFormat = EPixelFormat.Unknown

        public Int32 width = 0
        public Int32 height = 0
        public Int32 bitDepth = 0
        public Int32 frameCount = 1

        public bool hasAlpha = false
        public bool isAnimated = false
        public EColorSpace colorSpace = EColorSpace.Unknown

        # 压缩后字节数（若已知）
        public Int64 byteSize = 0

        _init_()
        {
            this.format = EImageFormat.Unknown
            this.pixelFormat = EPixelFormat.Unknown
            this.width = 0
            this.height = 0
            this.bitDepth = 0
            this.frameCount = 1
            this.hasAlpha = false
            this.isAnimated = false
            this.colorSpace = EColorSpace.Unknown
            this.byteSize = 0
        }

        public get bool isValid()
        {
            ret this.format != EImageFormat.Unknown && this.width > 0 && this.height > 0
        }

        # 单像素占多少字节（按 bitDepth 估算，仅对已探明的条目有意义）
        public get Int32 bytesPerPixel()
        {
            if this.pixelFormat != EPixelFormat.Unknown
            {
                ret ImageBuffer.bytesPerPixelOf( this.pixelFormat )
            }
            if this.bitDepth <= 8
            {
                ret 1
            }
            ret this.bitDepth / 8
        }

        # ── 字节读取工具（小端 / 大端）───────────────────────
        public static Int32 readU8( Array<UInt8> b, Int32 i )
        {
            if b == null || i < 0 || i >= b.length
            {
                ret 0
            }
            ret SystemConvertInt32( b[ i ] )
        }

        # 大端 32 位（PNG / JPEG 长度字段）
        public static Int32 readBe32( Array<UInt8> b, Int32 i )
        {
            ret ( ImageInfo.readU8( b, i ) << 24 ) | ( ImageInfo.readU8( b, i + 1 ) << 16 ) |
                ( ImageInfo.readU8( b, i + 2 ) << 8 ) | ImageInfo.readU8( b, i + 3 )
        }

        # 小端 32 位（BMP / RIFF）
        public static Int32 readLe32( Array<UInt8> b, Int32 i )
        {
            ret ImageInfo.readU8( b, i ) | ( ImageInfo.readU8( b, i + 1 ) << 8 ) |
                ( ImageInfo.readU8( b, i + 2 ) << 16 ) | ( ImageInfo.readU8( b, i + 3 ) << 24 )
        }

        # 小端 16 位（BMP / GIF）
        public static Int32 readLe16( Array<UInt8> b, Int32 i )
        {
            ret ImageInfo.readU8( b, i ) | ( ImageInfo.readU8( b, i + 1 ) << 8 )
        }

        # 从 offset 起是否匹配给定 ASCII 序列（用十进制字节表示）
        public static bool matches( Array<UInt8> b, Int32 offset, Array<Int32> sig )
        {
            if b == null || sig == null
            {
                ret false
            }
            if offset < 0 || offset + sig.length > b.length
            {
                ret false
            }
            Int32 i = 0
            while i < sig.length
            {
                if ImageInfo.readU8( b, offset + i ) != sig[ i ]
                {
                    ret false
                }
                i = i + 1
            }
            ret true
        }

        # ── magic bytes 嗅探（纯 SL）────────────────────────
        public static EImageFormat sniff( Array<UInt8> bytes )
        {
            if bytes == null || bytes.length < 8
            {
                ret EImageFormat.Unknown
            }

            # PNG: 89 50 4E 47 0D 0A 1A 0A
            if ImageInfo.readU8( bytes, 0 ) == 0x89 && ImageInfo.readU8( bytes, 1 ) == 0x50 &&
               ImageInfo.readU8( bytes, 2 ) == 0x4E && ImageInfo.readU8( bytes, 3 ) == 0x47
            {
                ret EImageFormat.Png
            }

            # JPEG: FF D8 FF
            if ImageInfo.readU8( bytes, 0 ) == 0xFF && ImageInfo.readU8( bytes, 1 ) == 0xD8 &&
               ImageInfo.readU8( bytes, 2 ) == 0xFF
            {
                ret EImageFormat.Jpeg
            }

            # GIF: "GIF8"
            if ImageInfo.readU8( bytes, 0 ) == 0x47 && ImageInfo.readU8( bytes, 1 ) == 0x49 &&
               ImageInfo.readU8( bytes, 2 ) == 0x46 && ImageInfo.readU8( bytes, 3 ) == 0x38
            {
                ret EImageFormat.Gif
            }

            # BMP: "BM"
            if ImageInfo.readU8( bytes, 0 ) == 0x42 && ImageInfo.readU8( bytes, 1 ) == 0x4D
            {
                ret EImageFormat.Bmp
            }

            # WebP: "RIFF" .... "WEBP"
            if ImageInfo.readU8( bytes, 0 ) == 0x52 && ImageInfo.readU8( bytes, 1 ) == 0x49 &&
               ImageInfo.readU8( bytes, 2 ) == 0x46 && ImageInfo.readU8( bytes, 3 ) == 0x46 &&
               ImageInfo.readU8( bytes, 8 ) == 0x57 && ImageInfo.readU8( bytes, 9 ) == 0x45 &&
               ImageInfo.readU8( bytes, 10 ) == 0x42 && ImageInfo.readU8( bytes, 11 ) == 0x50
            {
                ret EImageFormat.Webp
            }

            # TIFF: "II*\0" 或 "MM\0*"
            if ImageInfo.readU8( bytes, 0 ) == 0x49 && ImageInfo.readU8( bytes, 1 ) == 0x49 &&
               ImageInfo.readU8( bytes, 2 ) == 0x2A && ImageInfo.readU8( bytes, 3 ) == 0x00
            {
                ret EImageFormat.Tiff
            }
            if ImageInfo.readU8( bytes, 0 ) == 0x4D && ImageInfo.readU8( bytes, 1 ) == 0x4D &&
               ImageInfo.readU8( bytes, 2 ) == 0x00 && ImageInfo.readU8( bytes, 3 ) == 0x2A
            {
                ret EImageFormat.Tiff
            }

            # ICO: 00 00 01 00
            if ImageInfo.readU8( bytes, 0 ) == 0x00 && ImageInfo.readU8( bytes, 1 ) == 0x00 &&
               ImageInfo.readU8( bytes, 2 ) == 0x01 && ImageInfo.readU8( bytes, 3 ) == 0x00
            {
                ret EImageFormat.Ico
            }

            # PPM 家族: 'P' + '1'..'6'
            if ImageInfo.readU8( bytes, 0 ) == 0x50
            {
                Int32 d = ImageInfo.readU8( bytes, 1 )
                if d >= 0x31 && d <= 0x36
                {
                    ret EImageFormat.Ppm
                }
            }

            # AVIF / HEIC: .... "ftyp"
            if ImageInfo.readU8( bytes, 4 ) == 0x66 && ImageInfo.readU8( bytes, 5 ) == 0x74 &&
               ImageInfo.readU8( bytes, 6 ) == 0x79 && ImageInfo.readU8( bytes, 7 ) == 0x70
            {
                # 主品牌位（8..11）：avif / heic / heix / mif1 / msf1
                if ImageInfo.readU8( bytes, 8 ) == 0x61 && ImageInfo.readU8( bytes, 9 ) == 0x76
                {
                    ret EImageFormat.Avif
                }
                ret EImageFormat.Heic
            }

            # SVG：以 "<?xml" 或 "<svg" 开头（允许 UTF-8 BOM）
            Int32 svgStart = 0
            if ImageInfo.readU8( bytes, 0 ) == 0xEF && ImageInfo.readU8( bytes, 1 ) == 0xBB &&
               ImageInfo.readU8( bytes, 2 ) == 0xBF
            {
                svgStart = 3
            }
            if ImageInfo.readU8( bytes, svgStart ) == 0x3C
            {
                if ImageInfo.readU8( bytes, svgStart + 1 ) == 0x3F
                {
                    ret EImageFormat.Svg
                }
                if ImageInfo.readU8( bytes, svgStart + 1 ) == 0x73 &&
                   ImageInfo.readU8( bytes, svgStart + 2 ) == 0x76 &&
                   ImageInfo.readU8( bytes, svgStart + 3 ) == 0x67
                {
                    ret EImageFormat.Svg
                }
            }

            ret EImageFormat.Unknown
        }

        # ── 扩展名判定 ───────────────────────────────────────
        # 说明：String 目前只提供 length / front / end / range，
        # 没有 toLowerCase / endsWith，因此这里用
        # SystemStringCharCodeAt 逐字符做「忽略大小写」比较。
        public static bool endsWithFold( string path, string suffix )
        {
            if path == null || suffix == null
            {
                ret false
            }
            Int32 n = path.length()
            Int32 m = suffix.length()
            if m <= 0 || n < m
            {
                ret false
            }
            Int32 start = n - m
            Int32 i = 0
            while i < m
            {
                Int32 a = SystemStringCharCodeAt( path, start + i )
                Int32 b = SystemStringCharCodeAt( suffix, i )
                # 大写转小写
                if a >= 65 && a <= 90 { a = a + 32 }
                if b >= 65 && b <= 90 { b = b + 32 }
                if a != b
                {
                    ret false
                }
                i = i + 1
            }
            ret true
        }

        public static EImageFormat fromExtension( string path )
        {
            if path == null || path == ""
            {
                ret EImageFormat.Unknown
            }
            if ImageInfo.endsWithFold( path, ".png" )   { ret EImageFormat.Png }
            if ImageInfo.endsWithFold( path, ".jpg" )   { ret EImageFormat.Jpeg }
            if ImageInfo.endsWithFold( path, ".jpeg" )  { ret EImageFormat.Jpeg }
            if ImageInfo.endsWithFold( path, ".jpe" )   { ret EImageFormat.Jpeg }
            if ImageInfo.endsWithFold( path, ".bmp" )   { ret EImageFormat.Bmp }
            if ImageInfo.endsWithFold( path, ".gif" )   { ret EImageFormat.Gif }
            if ImageInfo.endsWithFold( path, ".webp" )  { ret EImageFormat.Webp }
            if ImageInfo.endsWithFold( path, ".svgz" )  { ret EImageFormat.Svg }
            if ImageInfo.endsWithFold( path, ".svg" )   { ret EImageFormat.Svg }
            if ImageInfo.endsWithFold( path, ".tiff" )  { ret EImageFormat.Tiff }
            if ImageInfo.endsWithFold( path, ".tif" )   { ret EImageFormat.Tiff }
            if ImageInfo.endsWithFold( path, ".ico" )   { ret EImageFormat.Ico }
            if ImageInfo.endsWithFold( path, ".tga" )   { ret EImageFormat.Tga }
            if ImageInfo.endsWithFold( path, ".ppm" )   { ret EImageFormat.Ppm }
            if ImageInfo.endsWithFold( path, ".pgm" )   { ret EImageFormat.Ppm }
            if ImageInfo.endsWithFold( path, ".pbm" )   { ret EImageFormat.Ppm }
            if ImageInfo.endsWithFold( path, ".avif" )  { ret EImageFormat.Avif }
            if ImageInfo.endsWithFold( path, ".heic" )  { ret EImageFormat.Heic }
            if ImageInfo.endsWithFold( path, ".heif" )  { ret EImageFormat.Heic }
            ret EImageFormat.Unknown
        }

        # ── 探测元数据（不解码像素）─────────────────────────
        public static ImageInfo probe( Array<UInt8> bytes )
        {
            ImageInfo info = ImageInfo()
            if bytes == null
            {
                ret info
            }
            info.byteSize = bytes.length
            info.format = ImageInfo.sniff( bytes )

            if info.format == EImageFormat.Png
            {
                # IHDR：8 字节签名 + 4 长度 + 4 类型 = 16，宽 16..19，高 20..23
                if bytes.length >= 26
                {
                    info.width = ImageInfo.readBe32( bytes, 16 )
                    info.height = ImageInfo.readBe32( bytes, 20 )
                    info.bitDepth = ImageInfo.readU8( bytes, 24 )
                    Int32 colorType = ImageInfo.readU8( bytes, 25 )
                    info.hasAlpha = ( colorType == 4 ) || ( colorType == 6 )
                    info.pixelFormat = info.hasAlpha ? EPixelFormat.Rgba8 : EPixelFormat.Rgb8
                    info.colorSpace = EColorSpace.SRGB
                }
                ret info
            }

            if info.format == EImageFormat.Bmp
            {
                # BITMAPINFOHEADER：宽 18..21，高 22..25，位深 28..29
                if bytes.length >= 30
                {
                    info.width = ImageInfo.readLe32( bytes, 18 )
                    info.height = ImageInfo.readLe32( bytes, 22 )
                    info.bitDepth = ImageInfo.readLe16( bytes, 28 )
                    info.hasAlpha = info.bitDepth == 32
                    info.pixelFormat = info.hasAlpha ? EPixelFormat.Bgra8 : EPixelFormat.Bgr8
                }
                ret info
            }

            if info.format == EImageFormat.Gif
            {
                # 逻辑屏幕描述符：宽 6..7，高 8..9
                if bytes.length >= 10
                {
                    info.width = ImageInfo.readLe16( bytes, 6 )
                    info.height = ImageInfo.readLe16( bytes, 8 )
                    info.bitDepth = 8
                    info.isAnimated = true
                    info.pixelFormat = EPixelFormat.Rgba8
                }
                ret info
            }

            if info.format == EImageFormat.Webp
            {
                info.pixelFormat = EPixelFormat.Rgba8
                # 具体尺寸 / 是否动画由 Webp 模块或后端判定
                ret info
            }

            if info.format == EImageFormat.Unknown
            {
                ret info
            }

            # 其余格式（JPEG / TIFF / AVIF …）交给后端
            object result = SystemCallExternalFunction( "Image.probe", bytes, info.format )
            ImageInfo remote = result as ImageInfo
            if remote != null
            {
                ret remote
            }
            ret info
        }

        override string toString()
        {
            ret "ImageInfo(fmt=" + this.format.toString() + ", " +
                this.width.toString() + "x" + this.height.toString() +
                ", depth=" + this.bitDepth.toString() +
                ", alpha=" + this.hasAlpha.toString() + ")"
        }
    }
}
