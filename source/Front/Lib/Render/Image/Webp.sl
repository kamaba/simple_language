# =========================================================================
# Image/Webp.sl —— WebP 容器模型
#
# WebP 是 RIFF 容器：
#   "RIFF"(0..3) size(4..7 小端) "WEBP"(8..11) chunk(12..)
#   chunk = fourcc(4) + size(4 小端) + payload
#
#   fourcc 决定类型：
#     "VP8 "  有损（VP8 比特流）
#     "VP8L"  无损
#     "VP8X"  扩展（可含动画 / alpha / ICC / EXIF / XMP）
#
# 纯 SL 实现：
#   - isWebp / formatOf：fourcc 嗅探
#   - readVp8xSize：从 VP8X 块里取画布尺寸（各 24 位小端，存的是 -1 值）
# 依赖后端：
#   - decode / encode（libwebp）
# =========================================================================

namespace Image
{
    public enum EWebpFormat
    {
        Unknown = 0
        Lossy = 1        # VP8
        Lossless = 2     # VP8L
        Extended = 3     # VP8X：动画 / alpha / 元数据
    }

    # 图像类型预设（libwebp 的 WebPPreset）
    public enum EWebpPreset
    {
        Default = 0
        Picture
        Photo
        Chart
        Icon
        Text
    }

    # ── 编码参数 ───────────────────────────────────────────
    public class WebpEncodeOptions
    {
        # 有损质量 0..100（越大越好越大）
        public Int32 quality = 80
        # true 走无损（此时 quality 无意义，改用 compressionLevel）
        public bool lossless = false
        # 有损压缩力度 0..6（越慢越小）
        public Int32 method = 4
        # 无损压缩等级 0..9
        public Int32 compressionLevel = 6
        public EWebpPreset preset = EWebpPreset.Default
        # alpha 平面质量 0..100
        public Int32 alphaQuality = 100
        # 锐化 0..7
        public Int32 sharpness = 0
        # 目标体积（字节），0 = 不限制
        public Int32 targetSize = 0

        _init_()
        {
            this.quality = 80
            this.lossless = false
            this.method = 4
            this.compressionLevel = 6
            this.preset = EWebpPreset.Default
            this.alphaQuality = 100
            this.sharpness = 0
            this.targetSize = 0
        }

        public void clamp()
        {
            if this.quality < 0 { this.quality = 0 }
            if this.quality > 100 { this.quality = 100 }
            if this.method < 0 { this.method = 0 }
            if this.method > 6 { this.method = 6 }
            if this.compressionLevel < 0 { this.compressionLevel = 0 }
            if this.compressionLevel > 9 { this.compressionLevel = 9 }
            if this.alphaQuality < 0 { this.alphaQuality = 0 }
            if this.alphaQuality > 100 { this.alphaQuality = 100 }
            if this.sharpness < 0 { this.sharpness = 0 }
            if this.sharpness > 7 { this.sharpness = 7 }
            if this.targetSize < 0 { this.targetSize = 0 }
        }

        public static WebpEncodeOptions defaultOptions()
        {
            ret WebpEncodeOptions()
        }

        public static WebpEncodeOptions losslessOf( Int32 level )
        {
            WebpEncodeOptions o = WebpEncodeOptions()
            o.lossless = true
            o.compressionLevel = level
            o.clamp()
            ret o
        }

        public static WebpEncodeOptions lossyOf( Int32 q )
        {
            WebpEncodeOptions o = WebpEncodeOptions()
            o.lossless = false
            o.quality = q
            o.clamp()
            ret o
        }
    }

    # ── 动画帧信息 ─────────────────────────────────────────
    public class WebpFrameInfo
    {
        public Int32 x = 0
        public Int32 y = 0
        public Int32 width = 0
        public Int32 height = 0
        public Int32 durationMs = 0
        # 混合方式：0 = blend（与上一帧混合），1 = do not blend
        public Int32 blendMethod = 0
        # 处置方式：0 = 保留，1 = 清为背景
        public Int32 disposeMethod = 0

        _init_()
        {
            this.x = 0
            this.y = 0
            this.width = 0
            this.height = 0
            this.durationMs = 0
            this.blendMethod = 0
            this.disposeMethod = 0
        }

        override string toString()
        {
            ret "WebpFrame(" + this.width.toString() + "x" + this.height.toString() +
                ", " + this.durationMs.toString() + "ms)"
        }
    }

    public class WebpDecodeOptions
    {
        # 只解码动画的第一帧
        public bool firstFrameOnly = true
        # 输出统一转 RGBA8
        public bool forceRgba = true
        # 是否多线程解码
        public bool useThreads = true

        _init_()
        {
            this.firstFrameOnly = true
            this.forceRgba = true
            this.useThreads = true
        }

        public static WebpDecodeOptions defaultOptions()
        {
            ret WebpDecodeOptions()
        }
    }

    # ── WebP 门面 ──────────────────────────────────────────
    public class Webp
    {
        # fourcc 比较（4 个 ASCII 字节）
        static bool isFourcc( Array<UInt8> b, Int32 offset, string cc )
        {
            if b == null || cc == null || cc.length() != 4
            {
                ret false
            }
            Int32 i = 0
            while i < 4
            {
                Int32 actual = ImageInfo.readU8( b, offset + i )
                Int32 expect = SystemStringCharCodeAt( cc, i )
                if actual != expect
                {
                    ret false
                }
                i = i + 1
            }
            ret true
        }

        # 是否是 WebP（"RIFF"...."WEBP"）
        public static bool isWebp( Array<UInt8> bytes )
        {
            if bytes == null || bytes.length < 16
            {
                ret false
            }
            ret Webp.isFourcc( bytes, 0, "RIFF" ) && Webp.isFourcc( bytes, 8, "WEBP" )
        }

        # 具体编码类型（看紧跟 RIFF 头的第一个 chunk fourcc）
        public static EWebpFormat formatOf( Array<UInt8> bytes )
        {
            if !Webp.isWebp( bytes )
            {
                ret EWebpFormat.Unknown
            }
            if Webp.isFourcc( bytes, 12, "VP8 " )
            {
                ret EWebpFormat.Lossy
            }
            if Webp.isFourcc( bytes, 12, "VP8L" )
            {
                ret EWebpFormat.Lossless
            }
            if Webp.isFourcc( bytes, 12, "VP8X" )
            {
                ret EWebpFormat.Extended
            }
            ret EWebpFormat.Unknown
        }

        public static bool isAnimated( Array<UInt8> bytes )
        {
            if !Webp.isWebp( bytes )
            {
                ret false
            }
            if !Webp.isFourcc( bytes, 12, "VP8X" )
            {
                ret false
            }
            # VP8X payload 第 0 字节为 flags，bit1(0x02) = 动画
            Int32 flags = ImageInfo.readU8( bytes, 20 )
            ret ( flags & 0x02 ) != 0
        }

        # VP8X 画布尺寸：payload 偏移 4..6 = width-1，7..9 = height-1（24 位小端）
        public static Int32 vp8xWidth( Array<UInt8> bytes )
        {
            if Webp.formatOf( bytes ) != EWebpFormat.Extended
            {
                ret 0
            }
            Int32 v = ImageInfo.readU8( bytes, 24 ) |
                      ( ImageInfo.readU8( bytes, 25 ) << 8 ) |
                      ( ImageInfo.readU8( bytes, 26 ) << 16 )
            ret v + 1
        }

        public static Int32 vp8xHeight( Array<UInt8> bytes )
        {
            if Webp.formatOf( bytes ) != EWebpFormat.Extended
            {
                ret 0
            }
            Int32 v = ImageInfo.readU8( bytes, 27 ) |
                      ( ImageInfo.readU8( bytes, 28 ) << 8 ) |
                      ( ImageInfo.readU8( bytes, 29 ) << 16 )
            ret v + 1
        }

        # ── 后端编解码 ──────────────────────────────────────
        public static ImageBuffer decode( Array<UInt8> bytes )
        {
            ret Webp.decode( bytes, WebpDecodeOptions.defaultOptions() )
        }

        public static ImageBuffer decode( Array<UInt8> bytes, WebpDecodeOptions options )
        {
            if bytes == null
            {
                ret null
            }
            object result = SystemCallExternalFunction( "Image.webpDecode", bytes, options )
            ret result as ImageBuffer
        }

        public static Array<UInt8> encode( ImageBuffer buffer )
        {
            ret Webp.encode( buffer, WebpEncodeOptions.defaultOptions() )
        }

        public static Array<UInt8> encode( ImageBuffer buffer, WebpEncodeOptions options )
        {
            if buffer == null
            {
                ret null
            }
            object result = SystemCallExternalFunction( "Image.webpEncode",
                buffer, buffer.width, buffer.height, buffer.pixelFormat, options )
            ret result as Array<UInt8>
        }

        # 动画帧表（后端）
        public static Array<WebpFrameInfo> frameTable( Array<UInt8> bytes )
        {
            if bytes == null
            {
                ret null
            }
            object result = SystemCallExternalFunction( "Image.webpFrames", bytes )
            ret result as Array<WebpFrameInfo>
        }

        override string toString()
        {
            ret "Webp"
        }
    }
}
