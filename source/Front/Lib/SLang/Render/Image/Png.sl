# =========================================================================
# Image/Png.sl —— PNG 容器模型
#
# PNG 结构：8 字节签名 + 若干 chunk
#   chunk = [length:4 大端][type:4 ASCII][data:length][crc:4]
#   关键块：IHDR（图像头） / PLTE（调色板） / IDAT（压缩像素） / IEND（结束）
#
# 纯 SL 实现：
#   - chunk 类型属性判定（关键 / 辅助 / 私有 / 安全复制，按字母大小写）
#   - 位深 × 颜色类型合法性表（PNG 规范表，写错就是非法文件）
#   - 每像素通道数
# 依赖后端：
#   - decode / encode（zlib inflate + 5 种行滤波器反演）
# =========================================================================

namespace Image
{
    # PNG 颜色类型（IHDR 第 10 字节）
    public enum EPngColorType
    {
        Gray = 0        # 灰度
        Rgb = 2         # 真彩
        Palette = 3     # 索引色
        GrayAlpha = 4   # 灰度 + alpha
        Rgba = 6        # 真彩 + alpha
    }

    public enum EPngInterlace
    {
        None = 0
        Adam7 = 1
    }

    # ── 块 ─────────────────────────────────────────────────
    public class PngChunk
    {
        public Int32 length = 0
        public string type = ""
        public Array<UInt8> data = null
        public Int32 crc = 0

        _init_()
        {
            this.length = 0
            this.type = ""
            this.data = Array<UInt8>( 0 )
            this.crc = 0
        }

        _init_( string _type, Array<UInt8> _data )
        {
            this.type = _type
            this.data = _data
            if _data == null
            {
                this.length = 0
            }
            else
            {
                this.length = _data.length
            }
            this.crc = 0
        }

        # 第一个字母大写 = 关键块（解码器必须识别）
        public get bool isCritical()
        {
            ret PngChunk.isUpper( this.type, 0 )
        }

        # 第二个字母大写 = 私有块
        public get bool isPrivate()
        {
            ret PngChunk.isUpper( this.type, 1 )
        }

        # 第三个字母必须为大写（保留位）
        public get bool isReservedValid()
        {
            ret PngChunk.isUpper( this.type, 2 )
        }

        # 第四个小写 = 可安全复制（即使不认识该块，编辑图片时也保留）
        public get bool isSafeToCopy()
        {
            ret PngChunk.isLower( this.type, 3 )
        }

        # 取字符的 Unicode 码位（Core 提供 SystemStringCharCodeAt）
        static Int32 charCodeAt( string s, Int32 index )
        {
            if s == null || index < 0 || index >= s.length()
            {
                ret 0
            }
            ret SystemStringCharCodeAt( s, index )
        }

        static bool isUpper( string s, Int32 index )
        {
            Int32 c = PngChunk.charCodeAt( s, index )
            ret c >= 65 && c <= 90
        }

        static bool isLower( string s, Int32 index )
        {
            Int32 c = PngChunk.charCodeAt( s, index )
            ret c >= 97 && c <= 122
        }

        override string toString()
        {
            ret "PngChunk(" + this.type + ", len=" + this.length.toString() + ")"
        }
    }

    # ── IHDR ───────────────────────────────────────────────
    public class PngHeader
    {
        public Int32 width = 0
        public Int32 height = 0
        public Int32 bitDepth = 8
        public EPngColorType colorType = EPngColorType.Rgba
        public Int32 compression = 0     # 恒为 0（zlib）
        public Int32 filterMethod = 0    # 恒为 0（自适应滤波）
        public EPngInterlace interlace = EPngInterlace.None

        _init_()
        {
            this.width = 0
            this.height = 0
            this.bitDepth = 8
            this.colorType = EPngColorType.Rgba
            this.compression = 0
            this.filterMethod = 0
            this.interlace = EPngInterlace.None
        }

        public get bool isValid()
        {
            if this.width <= 0 || this.height <= 0
            {
                ret false
            }
            if this.compression != 0 || this.filterMethod != 0
            {
                ret false
            }
            ret Png.isValidBitDepth( this.colorType, this.bitDepth )
        }

        public get Int32 channelCount()
        {
            ret Png.channelsOf( this.colorType )
        }

        public get bool hasAlpha()
        {
            ret this.colorType == EPngColorType.GrayAlpha ||
                this.colorType == EPngColorType.Rgba
        }

        # 未压缩的一行字节数（不含滤波器类型字节）
        public get Int32 rowBytes()
        {
            Int32 bits = this.width * this.channelCount() * this.bitDepth
            ret ( bits + 7 ) / 8
        }

        override string toString()
        {
            ret "PngHeader(" + this.width.toString() + "x" + this.height.toString() +
                ", depth=" + this.bitDepth.toString() +
                ", color=" + this.colorType.toString() + ")"
        }
    }

    # ── 编码参数 ───────────────────────────────────────────
    public class PngEncodeOptions
    {
        # zlib 压缩等级 0..9（9 最慢最小）
        public Int32 compressionLevel = 6
        public EPngInterlace interlace = EPngInterlace.None
        # 输出时不带 alpha 通道（源有 alpha 时丢弃）
        public bool stripAlpha = false
        # 位深 < 8 时是否用调色板（Palette）输出
        public bool usePalette = false
        # 逐行滤波器策略：0=自适应 1=无 2=Sub 3=Up 4=Average 5=Paeth
        public Int32 filterStrategy = 0

        _init_()
        {
            this.compressionLevel = 6
            this.interlace = EPngInterlace.None
            this.stripAlpha = false
            this.usePalette = false
            this.filterStrategy = 0
        }

        public void clamp()
        {
            if this.compressionLevel < 0 { this.compressionLevel = 0 }
            if this.compressionLevel > 9 { this.compressionLevel = 9 }
            if this.filterStrategy < 0 { this.filterStrategy = 0 }
            if this.filterStrategy > 5 { this.filterStrategy = 5 }
        }

        public static PngEncodeOptions defaultOptions()
        {
            ret PngEncodeOptions()
        }

        # 最快（不压缩）
        public static PngEncodeOptions fastest()
        {
            PngEncodeOptions o = PngEncodeOptions()
            o.compressionLevel = 1
            o.filterStrategy = 1
            ret o
        }

        # 最小体积
        public static PngEncodeOptions smallest()
        {
            PngEncodeOptions o = PngEncodeOptions()
            o.compressionLevel = 9
            o.filterStrategy = 0
            ret o
        }
    }

    public class PngDecodeOptions
    {
        # 统一转成 RGBA8 输出（省去上层处理多种位深）
        public bool forceRgba = true
        # 忽略 alpha（合成到黑底或按背景色）
        public bool ignoreAlpha = false
        public Color backgroundColor = null

        _init_()
        {
            this.forceRgba = true
            this.ignoreAlpha = false
            this.backgroundColor = null
        }

        public static PngDecodeOptions defaultOptions()
        {
            ret PngDecodeOptions()
        }
    }

    # ── PNG 门面 ───────────────────────────────────────────
    public class Png
    {
        # 每个颜色类型允许的位深（PNG 规范 1.2 表）
        public static bool isValidBitDepth( EPngColorType colorType, Int32 bitDepth )
        {
            if colorType == EPngColorType.Gray
            {
                ret bitDepth == 1 || bitDepth == 2 || bitDepth == 4 ||
                    bitDepth == 8 || bitDepth == 16
            }
            if colorType == EPngColorType.Rgb
            {
                ret bitDepth == 8 || bitDepth == 16
            }
            if colorType == EPngColorType.Palette
            {
                ret bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8
            }
            if colorType == EPngColorType.GrayAlpha
            {
                ret bitDepth == 8 || bitDepth == 16
            }
            if colorType == EPngColorType.Rgba
            {
                ret bitDepth == 8 || bitDepth == 16
            }
            ret false
        }

        public static Int32 channelsOf( EPngColorType colorType )
        {
            if colorType == EPngColorType.Gray
            {
                ret 1
            }
            if colorType == EPngColorType.Rgb
            {
                ret 3
            }
            if colorType == EPngColorType.Palette
            {
                ret 1
            }
            if colorType == EPngColorType.GrayAlpha
            {
                ret 2
            }
            if colorType == EPngColorType.Rgba
            {
                ret 4
            }
            ret 0
        }

        public static bool isSignature( Array<UInt8> bytes )
        {
            # 89 50 4E 47 0D 0A 1A 0A
            if bytes == null || bytes.length < 8
            {
                ret false
            }
            ret ImageInfo.readU8( bytes, 0 ) == 0x89 &&
                ImageInfo.readU8( bytes, 1 ) == 0x50 &&
                ImageInfo.readU8( bytes, 2 ) == 0x4E &&
                ImageInfo.readU8( bytes, 3 ) == 0x47 &&
                ImageInfo.readU8( bytes, 4 ) == 0x0D &&
                ImageInfo.readU8( bytes, 5 ) == 0x0A &&
                ImageInfo.readU8( bytes, 6 ) == 0x1A &&
                ImageInfo.readU8( bytes, 7 ) == 0x0A
        }

        # 解析 IHDR（纯 SL）
        public static PngHeader readHeader( Array<UInt8> bytes )
        {
            PngHeader h = PngHeader()
            if !Png.isSignature( bytes ) || bytes.length < 26
            {
                ret h
            }
            h.width = ImageInfo.readBe32( bytes, 16 )
            h.height = ImageInfo.readBe32( bytes, 20 )
            h.bitDepth = ImageInfo.readU8( bytes, 24 )
            Int32 ct = ImageInfo.readU8( bytes, 25 )
            if ct == 0 { h.colorType = EPngColorType.Gray }
            elif ct == 2 { h.colorType = EPngColorType.Rgb }
            elif ct == 3 { h.colorType = EPngColorType.Palette }
            elif ct == 4 { h.colorType = EPngColorType.GrayAlpha }
            elif ct == 6 { h.colorType = EPngColorType.Rgba }
            if bytes.length > 28
            {
                h.interlace = EPngInterlace.None
                if ImageInfo.readU8( bytes, 28 ) == 1
                {
                    h.interlace = EPngInterlace.Adam7
                }
            }
            ret h
        }

        # ── 后端编解码 ──────────────────────────────────────
        public static ImageBuffer decode( Array<UInt8> bytes )
        {
            ret Png.decode( bytes, PngDecodeOptions.defaultOptions() )
        }

        public static ImageBuffer decode( Array<UInt8> bytes, PngDecodeOptions options )
        {
            if bytes == null
            {
                ret null
            }
            object result = SystemCallExternalFunction( "Image.pngDecode", bytes, options )
            ret result as ImageBuffer
        }

        public static Array<UInt8> encode( ImageBuffer buffer )
        {
            ret Png.encode( buffer, PngEncodeOptions.defaultOptions() )
        }

        public static Array<UInt8> encode( ImageBuffer buffer, PngEncodeOptions options )
        {
            if buffer == null
            {
                ret null
            }
            object result = SystemCallExternalFunction( "Image.pngEncode",
                buffer, buffer.width, buffer.height, buffer.pixelFormat, options )
            ret result as Array<UInt8>
        }

        override string toString()
        {
            ret "Png"
        }
    }
}
