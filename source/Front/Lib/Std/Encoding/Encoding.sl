# ============================================================================
# Std/Encoding/Encoding.sl — 编码层：字符编码转换（UTF-8 / UTF-16 / UTF-32 /
# GB2312 / ASCII）
#
# API 风格参考 C# System.Text.Encoding 与 Dart dart:convert：
#   Encoding.encode(code, text)   UTF-8 文本 -> 目标编码字节
#   Encoding.decode(code, bytes)  目标编码字节 -> UTF-8 文本
# 本体在 C VM 端（csimple_lang/src/core/sl_core_encoding.c，GB2312 查表
# sl_core_gb2312_table.c），SL 层无状态；SL 字符串内部即 UTF-8 字节，
# 故 decode 产物可直接还原为 string。
# BOM 语义：*_BOM 编码变体在编码产物头部显式写入 BOM；decode 自动识别并
# 剥离任意变体开头的 BOM，UTF-16/UTF-32 遇反向 BOM 翻转端序解释
# （无 BOM 时按编码代码声明的端序）。
# 错误约定：入参缓冲已释放抛 BufferError.Released；非法编码名抛
# EncodingError.UnknownEncoding；编码失败（含目标字符集无法表示的码点）
# 抛 EncodeFailed；解码失败（非法字节序列 / 未配对代理 / GB2312 空位）
# 抛 DecodeFailed。
# 尺寸预查：maxByteCount/maxCharCount 入参均为字节数（前者 UTF-8 输入
# 字节，后者目标编码输入字节），上界超出 Int32 范围时返回 0。
# 跨模块访问：经 Core.ByteBuf 的 handle / fromHandle 桥接注册表 id。
# 便捷类：Utf8 / Unicode(UTF-16LE) / BigEndianUnicode / Gb2312 / Ascii，
# 常用编码零枚举样板；其余变体（含 *_BOM 与 UTF-32 系）经本类枚举访问。
# ============================================================================

# 编码代码（与 C 侧 sl_core_encoding.h 的 CORE_ENCODING_* 一一对应）
public enum EncodingCode extends Int32
{
    Utf8        = 0
    Utf8Bom     = 1
    Utf16LE     = 2
    Utf16LEBom  = 3
    Utf16BE     = 4
    Utf16BEBom  = 5
    Utf32LE     = 6
    Utf32LEBom  = 7
    Utf32BE     = 8
    Utf32BEBom  = 9
    Gb2312      = 10
    Ascii       = 11
}

public enum EncodingError extends Error
{
    EncodeFailed    = { code = 1 }
    DecodeFailed    = { code = 2 }
    UnknownEncoding = { code = 3 }
}

public class Encoding extends Object
{
    # ── 编码代码 ──

    # enum -> Int32 不能用 as 转换（Front 不支持），显式映射为 C 侧
    # sl_core_encoding.h 的 CORE_ENCODING_* 值（逐项一一对应）
    static Int32 codeValue( EncodingCode code )
    {
        Int32 v = -1
        if code == EncodingCode.Utf8
        {
            v = 0
        }
        elif code == EncodingCode.Utf8Bom
        {
            v = 1
        }
        elif code == EncodingCode.Utf16LE
        {
            v = 2
        }
        elif code == EncodingCode.Utf16LEBom
        {
            v = 3
        }
        elif code == EncodingCode.Utf16BE
        {
            v = 4
        }
        elif code == EncodingCode.Utf16BEBom
        {
            v = 5
        }
        elif code == EncodingCode.Utf32LE
        {
            v = 6
        }
        elif code == EncodingCode.Utf32LEBom
        {
            v = 7
        }
        elif code == EncodingCode.Utf32BE
        {
            v = 8
        }
        elif code == EncodingCode.Utf32BEBom
        {
            v = 9
        }
        elif code == EncodingCode.Gb2312
        {
            v = 10
        }
        else
        {
            v = 11
        }
        ret v
    }

    # 按标准名查编码代码（精确匹配小写规范名，标准库暂无 toLower）：
    # utf-8 / utf-8-bom / utf-16le / utf-16le-bom / utf-16be / utf-16be-bom /
    # utf-32le / utf-32le-bom / utf-32be / utf-32be-bom / gb2312 / ascii；
    # 未知名抛 UnknownEncoding
    public static EncodingCode codeFromName( string name ) throws
    {
        if name == "utf-8"
        {
            ret EncodingCode.Utf8
        }
        elif name == "utf-8-bom"
        {
            ret EncodingCode.Utf8Bom
        }
        elif name == "utf-16le"
        {
            ret EncodingCode.Utf16LE
        }
        elif name == "utf-16le-bom"
        {
            ret EncodingCode.Utf16LEBom
        }
        elif name == "utf-16be"
        {
            ret EncodingCode.Utf16BE
        }
        elif name == "utf-16be-bom"
        {
            ret EncodingCode.Utf16BEBom
        }
        elif name == "utf-32le"
        {
            ret EncodingCode.Utf32LE
        }
        elif name == "utf-32le-bom"
        {
            ret EncodingCode.Utf32LEBom
        }
        elif name == "utf-32be"
        {
            ret EncodingCode.Utf32BE
        }
        elif name == "utf-32be-bom"
        {
            ret EncodingCode.Utf32BEBom
        }
        elif name == "gb2312"
        {
            ret EncodingCode.Gb2312
        }
        elif name == "ascii"
        {
            ret EncodingCode.Ascii
        }
        throw EncodingError.UnknownEncoding
    }

    # 编码代码 -> 标准名（小写规范名，codeFromName 的逆映射）
    public static string nameFromCode( EncodingCode code )
    {
        if code == EncodingCode.Utf8
        {
            ret "utf-8"
        }
        elif code == EncodingCode.Utf8Bom
        {
            ret "utf-8-bom"
        }
        elif code == EncodingCode.Utf16LE
        {
            ret "utf-16le"
        }
        elif code == EncodingCode.Utf16LEBom
        {
            ret "utf-16le-bom"
        }
        elif code == EncodingCode.Utf16BE
        {
            ret "utf-16be"
        }
        elif code == EncodingCode.Utf16BEBom
        {
            ret "utf-16be-bom"
        }
        elif code == EncodingCode.Utf32LE
        {
            ret "utf-32le"
        }
        elif code == EncodingCode.Utf32LEBom
        {
            ret "utf-32le-bom"
        }
        elif code == EncodingCode.Utf32BE
        {
            ret "utf-32be"
        }
        elif code == EncodingCode.Utf32BEBom
        {
            ret "utf-32be-bom"
        }
        elif code == EncodingCode.Gb2312
        {
            ret "gb2312"
        }
        ret "ascii"
    }

    # 该编码变体编码时是否写入 BOM（仅 *_BOM 系为 true）
    public static bool hasBom( EncodingCode code )
    {
        if code == EncodingCode.Utf8Bom
        {
            ret true
        }
        elif code == EncodingCode.Utf16LEBom
        {
            ret true
        }
        elif code == EncodingCode.Utf16BEBom
        {
            ret true
        }
        elif code == EncodingCode.Utf32LEBom
        {
            ret true
        }
        elif code == EncodingCode.Utf32BEBom
        {
            ret true
        }
        ret false
    }

    # ── 尺寸预查 ──

    # 编码 inputSize 字节 UTF-8 输入最坏情况下的目标编码字节数（含 BOM）；
    # inputSize 为负或超出 Int32 容器上限时返回 0
    public static Int32 maxByteCount( EncodingCode code, Int32 inputSize )
    {
        ret SystemEncodingMaxByteCount( Encoding.codeValue( code ), inputSize )
    }

    # 解码 inputSize 字节目标编码输入最坏情况下的 UTF-8 输出字节数
    # （BOM 不产生输出）；inputSize 为负或超出 Int32 容器上限时返回 0
    public static Int32 maxCharCount( EncodingCode code, Int32 inputSize )
    {
        ret SystemEncodingMaxCharCount( Encoding.codeValue( code ), inputSize )
    }

    # ── 编码 / 解码 ──

    # 把 UTF-8 文本编码为目标编码字节流，返回新缓冲（中间缓冲即时释放）；
    # 目标字符集无法表示的码点抛 EncodeFailed
    public static ByteBuf encode( EncodingCode code, string text ) throws
    {
        var b = ByteBuf.fromString( text )
        var encoded = Encoding.encode( code, b )
        b.release()
        ret encoded
    }

    # 把 src 可读区的 UTF-8 字节编码为目标编码字节流，返回新缓冲；
    # src 索引不变。输入非法 UTF-8 或含不可表示码点抛 EncodeFailed
    public static ByteBuf encode( EncodingCode code, ByteBuf src ) throws
    {
        if SystemByteBufIsReleased( src.handle )
        {
            throw BufferError.Released
        }
        var id = SystemEncodingEncode( Encoding.codeValue( code ), src.handle )
        if id == 0
        {
            throw EncodingError.EncodeFailed
        }
        ret ByteBuf.fromHandle( id )
    }

    # 把 src 可读区的目标编码字节解码为 UTF-8 文本；src 索引不变。
    # 开头 BOM 自动识别并剥离；非法字节序列抛 DecodeFailed；
    # 内嵌 NUL 截断语义同 ByteBuf.toString
    public static string decode( EncodingCode code, ByteBuf src ) throws
    {
        var b = Encoding.decodeBytes( code, src )
        var text = b.toString()
        b.release()
        ret text
    }

    # 解码为 UTF-8 字节缓冲（不经 string，保留全部字节含 NUL），
    # decode 的字节级本体；中间无缓冲，直接返回注册表缓冲
    public static ByteBuf decodeBytes( EncodingCode code, ByteBuf src ) throws
    {
        if SystemByteBufIsReleased( src.handle )
        {
            throw BufferError.Released
        }
        var id = SystemEncodingDecode( Encoding.codeValue( code ), src.handle )
        if id == 0
        {
            throw EncodingError.DecodeFailed
        }
        ret ByteBuf.fromHandle( id )
    }

    # 跨编码转换（C# Encoding.Convert 对位）：fromCode 字节 -> UTF-8 ->
    # toCode 字节，返回新缓冲；src 索引不变，中间 UTF-8 缓冲即时释放
    public static ByteBuf convert( EncodingCode fromCode, EncodingCode toCode, ByteBuf src ) throws
    {
        var utf8 = Encoding.decodeBytes( fromCode, src )
        var encoded = Encoding.encode( toCode, utf8 )
        utf8.release()
        ret encoded
    }

    # ── BOM / 前导 ──

    # 返回该编码的 BOM 字节（新缓冲；无 BOM 编码为空缓冲，
    # 如 GB2312 / ASCII 与非 *_BOM 变体）
    public static ByteBuf preamble( EncodingCode code ) throws
    {
        var id = SystemEncodingPreamble( Encoding.codeValue( code ) )
        if id == 0
        {
            throw EncodingError.UnknownEncoding
        }
        ret ByteBuf.fromHandle( id )
    }
}

# ============================================================================
# 便捷类 — 常用编码零枚举样板（本体全部转发 Encoding 中枢类）
# ============================================================================

# UTF-8（无 BOM；解码自动识别并剥离 BOM）
public class Utf8 extends Object
{
    public static ByteBuf encode( string text ) throws
    {
        ret Encoding.encode( EncodingCode.Utf8, text )
    }

    # 显式带 BOM 变体（EF BB BF 头，Windows 记事本互通）
    public static ByteBuf encodeBom( string text ) throws
    {
        ret Encoding.encode( EncodingCode.Utf8Bom, text )
    }

    public static string decode( ByteBuf src ) throws
    {
        ret Encoding.decode( EncodingCode.Utf8, src )
    }

    public static Int32 maxByteCount( Int32 inputSize )
    {
        ret Encoding.maxByteCount( EncodingCode.Utf8, inputSize )
    }

    public static Int32 maxCharCount( Int32 inputSize )
    {
        ret Encoding.maxCharCount( EncodingCode.Utf8, inputSize )
    }
}

# UTF-16 小端（C# Encoding.Unicode 对位；解码自动识别 BE BOM 翻转端序）
public class Unicode extends Object
{
    public static ByteBuf encode( string text ) throws
    {
        ret Encoding.encode( EncodingCode.Utf16LE, text )
    }

    # 显式带 BOM 变体（FF FE 头）
    public static ByteBuf encodeBom( string text ) throws
    {
        ret Encoding.encode( EncodingCode.Utf16LEBom, text )
    }

    public static string decode( ByteBuf src ) throws
    {
        ret Encoding.decode( EncodingCode.Utf16LE, src )
    }

    public static Int32 maxByteCount( Int32 inputSize )
    {
        ret Encoding.maxByteCount( EncodingCode.Utf16LE, inputSize )
    }

    public static Int32 maxCharCount( Int32 inputSize )
    {
        ret Encoding.maxCharCount( EncodingCode.Utf16LE, inputSize )
    }
}

# UTF-16 大端（C# Encoding.BigEndianUnicode 对位；解码自动识别 LE BOM）
public class BigEndianUnicode extends Object
{
    public static ByteBuf encode( string text ) throws
    {
        ret Encoding.encode( EncodingCode.Utf16BE, text )
    }

    # 显式带 BOM 变体（FE FF 头）
    public static ByteBuf encodeBom( string text ) throws
    {
        ret Encoding.encode( EncodingCode.Utf16BEBom, text )
    }

    public static string decode( ByteBuf src ) throws
    {
        ret Encoding.decode( EncodingCode.Utf16BE, src )
    }

    public static Int32 maxByteCount( Int32 inputSize )
    {
        ret Encoding.maxByteCount( EncodingCode.Utf16BE, inputSize )
    }

    public static Int32 maxCharCount( Int32 inputSize )
    {
        ret Encoding.maxCharCount( EncodingCode.Utf16BE, inputSize )
    }
}

# GB2312（EUC-CN：双字节 A1A1-F7FE 查表 + ASCII 透传；
# 表外字符抛 EncodeFailed，未赋值位抛 DecodeFailed）
public class Gb2312 extends Object
{
    public static ByteBuf encode( string text ) throws
    {
        ret Encoding.encode( EncodingCode.Gb2312, text )
    }

    public static string decode( ByteBuf src ) throws
    {
        ret Encoding.decode( EncodingCode.Gb2312, src )
    }

    public static Int32 maxByteCount( Int32 inputSize )
    {
        ret Encoding.maxByteCount( EncodingCode.Gb2312, inputSize )
    }

    public static Int32 maxCharCount( Int32 inputSize )
    {
        ret Encoding.maxCharCount( EncodingCode.Gb2312, inputSize )
    }
}

# 严格 7 位 ASCII（<0x80 透传；含任何高位字节的输入抛 EncodeFailed /
# DecodeFailed——不做 1:1 字节映射的宽松 latin-1 语义）
public class Ascii extends Object
{
    public static ByteBuf encode( string text ) throws
    {
        ret Encoding.encode( EncodingCode.Ascii, text )
    }

    public static string decode( ByteBuf src ) throws
    {
        ret Encoding.decode( EncodingCode.Ascii, src )
    }

    public static Int32 maxByteCount( Int32 inputSize )
    {
        ret Encoding.maxByteCount( EncodingCode.Ascii, inputSize )
    }

    public static Int32 maxCharCount( Int32 inputSize )
    {
        ret Encoding.maxCharCount( EncodingCode.Ascii, inputSize )
    }
}
