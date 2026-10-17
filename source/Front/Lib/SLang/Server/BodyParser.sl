import Std;
import Core;

namespace SLang
{
    # ============================================================================
    # SLang/Server/BodyParser.sl — 请求体格式判定与解码（P1）
    # 设计契约：SERVER_DESIGN.md §4.4 / §6.4
    #
    # 范围（P1）：EBodyFormat 判定（json/form/multipart/text/binary/none）+
    #   urlencoded form 解码（_UrlCodec：percent-decode + '+'→空格）。
    #   multipart 解析随 P1.5（UploadedFile）落地。
    # ============================================================================

    # 请求体格式（extends Int32，枚举字段先例：HttpMethod / WsOpCode）
    public enum EBodyFormat extends Int32
    {
        # 无 body（GET/HEAD/长度 0）
        None = 0
        # application/json（含 +json 后缀）
        Json = 1
        # application/x-www-form-urlencoded
        Form = 2
        # multipart/form-data（P1.5 全量解析；P1 仅判定）
        Multipart = 3
        # text/*
        Text = 4
        # 其余（application/octet-stream 等）
        Binary = 5
    }

    # ============================================================================
    # _UrlCodec — 内部 URL 解码工具（percent-decode + '+'→空格；UTF-8 组装）。
    # Uri 无现成解码（SERVER_DESIGN.md §2.3），本类为查询参数与 form 共用解码器。
    # 非法 %HH 序列原样保留（RFC 宽容语义）。
    # ============================================================================
    class _UrlCodec
    {
        static string decode( string s )
        {
            if s == null
            {
                ret ""
            }
            Int32 len = SystemStringLength( s )
            if len == 0
            {
                ret ""
            }
            ByteBuffer b = ByteBuffer( len )
            Int32 i = 0
            while i < len
            {
                Int32 c = SystemStringCharCodeAt( s, i )
                if c == 43
                {
                    # '+' → 空格（form / query 通用约定）
                    b.writeU8( 32 )
                    i = i + 1
                }
                elif c == 37 && i + 2 < len
                {
                    # %HH：需 s[i+1]、s[i+2] 在界内；两位十六进制合法才解码，
                    # 否则原样保留（RFC 宽容语义）
                    Int32 h = _UrlCodec._hexVal( SystemStringCharCodeAt( s, i + 1 ) )
                    Int32 l = _UrlCodec._hexVal( SystemStringCharCodeAt( s, i + 2 ) )
                    if h >= 0 && l >= 0
                    {
                        UInt8 v = SystemConvertUInt8( h * 16 + l )
                        b.writeU8( v )
                        i = i + 3
                    }
                    else
                    {
                        b.writeU8( SystemConvertUInt8( c ) )
                        i = i + 1
                    }
                }
                else
                {
                    b.writeU8( SystemConvertUInt8( c ) )
                    i = i + 1
                }
            }
            string decoded = b.toString()
            b.release()
            ret decoded
        }

        # 十六进制字符 → 0..15；非法返回 -1
        static Int32 _hexVal( Int32 c )
        {
            if c >= 48 && c <= 57
            {
                ret c - 48
            }
            if c >= 65 && c <= 70
            {
                ret c - 55
            }
            if c >= 97 && c <= 102
            {
                ret c - 87
            }
            ret 0 - 1
        }
    }

    # ============================================================================
    # BodyParser — 格式判定与 form 解码（静态工具）
    # ============================================================================
    public class BodyParser
    {
        # Content-Type → EBodyFormat（忽略大小写；参数段 ';' 后剥离后比较）。
        # 无 body（len == 0）且无 Content-Type → None
        static EBodyFormat detect( string contentType, Int32 bodyLen )
        {
            Int32 ctLen = SystemStringLength( contentType )
            if ctLen == 0
            {
                if bodyLen == 0
                {
                    ret EBodyFormat.None
                }
                ret EBodyFormat.Binary
            }
            # 剥离参数段（"; charset=..."）
            Int32 cut = ctLen
            for Int32 i = 0, i < ctLen, i = i + 1
            {
                if SystemStringCharCodeAt( contentType, i ) == 59
                {
                    cut = i
                    break
                }
            }
            string mime = SystemStringRange( contentType, 0, cut )
            Int32 mLen = SystemStringLength( mime )
            if mLen == 0
            {
                ret EBodyFormat.Binary
            }

            if BodyParser._endsWithFold( mime, "json" )
            {
                ret EBodyFormat.Json
            }
            if Net.Uri.eqFold( mime, "application/x-www-form-urlencoded" )
            {
                ret EBodyFormat.Form
            }
            if Net.Uri.eqFold( mime, "multipart/form-data" )
            {
                ret EBodyFormat.Multipart
            }
            if BodyParser._startsWithFold( mime, "text/" )
            {
                ret EBodyFormat.Text
            }
            ret EBodyFormat.Binary
        }

        # urlencoded form 解码："a=1&b=hello+world&c=%E4%BD%A0" → Map；
        # 键重复后者覆盖前者（m[k]=v put 语义，Map._getItem_ 头注记）；
        # 无 '=' 的段跳过；值解码经 _UrlCodec（percent + '+'→空格）
        static Map<string,string> parseForm( string text )
        {
            Map<string,string> m = new()
            Int32 len = SystemStringLength( text )
            if len == 0
            {
                ret m
            }
            Int32 segStart = 0
            Int32 i = 0
            # 尾部哨兵：按 '&' 逐段切；最后一段在循环外处理
            while i <= len
            {
                if i == len || SystemStringCharCodeAt( text, i ) == 38
                {
                    Int32 segLen = i - segStart
                    if segLen > 0
                    {
                        string seg = SystemStringRange( text, segStart, i )
                        Int32 eq = -1
                        Int32 sl = SystemStringLength( seg )
                        for Int32 k = 0, k < sl, k = k + 1
                        {
                            if SystemStringCharCodeAt( seg, k ) == 61
                            {
                                eq = k
                                break
                            }
                        }
                        if eq > 0
                        {
                            string key = _UrlCodec.decode( SystemStringRange( seg, 0, eq ) )
                            string val = _UrlCodec.decode( SystemStringRange( seg, eq + 1, sl ) )
                            m[key] = val
                        }
                    }
                    segStart = i + 1
                }
                i = i + 1
            }
            ret m
        }

        # ── 文本辅助（大小写折叠前缀/后缀）──

        static bool _startsWithFold( string s, string prefix )
        {
            Int32 sl = SystemStringLength( s )
            Int32 pl = SystemStringLength( prefix )
            if pl == 0
            {
                ret true
            }
            if sl < pl
            {
                ret false
            }
            for Int32 i = 0, i < pl, i = i + 1
            {
                if Net.Uri.fold( SystemStringCharCodeAt( s, i ) ) != Net.Uri.fold( SystemStringCharCodeAt( prefix, i ) )
                {
                    ret false
                }
            }
            ret true
        }

        static bool _endsWithFold( string s, string suffix )
        {
            Int32 sl = SystemStringLength( s )
            Int32 fl = SystemStringLength( suffix )
            if fl == 0
            {
                ret true
            }
            if sl < fl
            {
                ret false
            }
            Int32 off = sl - fl
            for Int32 i = 0, i < fl, i = i + 1
            {
                if Net.Uri.fold( SystemStringCharCodeAt( s, off + i ) ) != Net.Uri.fold( SystemStringCharCodeAt( suffix, i ) )
                {
                    ret false
                }
            }
            ret true
        }
    }
}
