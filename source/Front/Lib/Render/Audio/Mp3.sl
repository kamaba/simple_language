# =========================================================================
# Audio/Mp3.sl —— MPEG 音频（MP3）与 ID3 标签
#
# 纯 SL 实现：
#   - 4 字节 MPEG 帧头位域解析 -> 版本 / 层 / 码率 / 采样率 / 声道 / 填充
#   - 码率表与采样率表查表、帧长计算
#   - ID3v2 头识别与 **syncsafe 整数** 解码（每字节只用低 7 位）
#   - ID3v1（文件尾 128 字节 "TAG"）识别
# 依赖后端：
#   - decode / encode（Huffman 解码 + IMDCT + 心理声学模型）
#
# 注：Jpeg.sl 里有一份同样的 MPEG 帧头解析（用于 MP3 封面/音频轨），
#     这里在 Audio 命名空间独立实现一份，避免跨命名空间耦合。
# =========================================================================

namespace Audio
{
    public enum EMp3Version
    {
        Mpeg25 = 0
        Reserved = 1
        Mpeg2 = 2
        Mpeg1 = 3
    }

    public enum EMp3Layer
    {
        Reserved = 0
        Layer3 = 1
        Layer2 = 2
        Layer1 = 3
    }

    public enum EMp3ChannelMode
    {
        Stereo = 0
        JointStereo = 1
        DualChannel = 2
        Mono = 3
    }

    # ── 帧头 ───────────────────────────────────────────────
    public class Mp3FrameHeader
    {
        public EMp3Version version = EMp3Version.Mpeg1
        public EMp3Layer layer = EMp3Layer.Layer3
        public Int32 bitrateKbps = 0
        public Int32 sampleRate = 0
        public EMp3ChannelMode channelMode = EMp3ChannelMode.Stereo
        public Int32 padding = 0
        public bool hasCrc = false
        public Int32 frameLength = 0

        _init_()
        {
            this.version = EMp3Version.Mpeg1
            this.layer = EMp3Layer.Layer3
            this.bitrateKbps = 0
            this.sampleRate = 0
            this.channelMode = EMp3ChannelMode.Stereo
            this.padding = 0
            this.hasCrc = false
            this.frameLength = 0
        }

        public get bool isValid()
        {
            ret this.bitrateKbps > 0 && this.sampleRate > 0 && this.frameLength > 0
        }

        public get Int32 channelCount()
        {
            if this.channelMode == EMp3ChannelMode.Mono
            {
                ret 1
            }
            ret 2
        }

        # 每帧采样数：Layer3 MPEG1 = 1152，MPEG2/2.5 = 576
        public get Int32 samplesPerFrame()
        {
            if this.layer == EMp3Layer.Layer1
            {
                ret 384
            }
            if this.layer == EMp3Layer.Layer2
            {
                ret 1152
            }
            if this.version == EMp3Version.Mpeg1
            {
                ret 1152
            }
            ret 576
        }

        public get Float32 frameDuration()
        {
            if this.sampleRate <= 0
            {
                ret 0.0f
            }
            ret 1.0f * this.samplesPerFrame() / this.sampleRate
        }

        override string toString()
        {
            ret "Mp3Frame(" + this.bitrateKbps.toString() + "kbps, " +
                this.sampleRate.toString() + "Hz, len=" + this.frameLength.toString() + ")"
        }
    }

    # ── ID3 ────────────────────────────────────────────────
    public class ID3Frame
    {
        # 四字符帧 ID：TIT2 标题 / TPE1 艺术家 / TALB 专辑 / TYER 年份 …
        public string id = ""
        public string value = ""

        _init_()
        {
            this.id = ""
            this.value = ""
        }

        _init_( string _id, string _value )
        {
            this.id = _id
            this.value = _value
        }

        override string toString()
        {
            ret "ID3Frame(" + this.id + "=" + this.value + ")"
        }
    }

    public class ID3v2Tag
    {
        public Int32 versionMajor = 3
        public Int32 versionMinor = 0
        public Int32 size = 0
        public bool hasFooter = false
        public bool unsynchronisation = false
        public bool hasExtendedHeader = false

        public string title = ""
        public string artist = ""
        public string album = ""
        public string year = ""
        public string genre = ""
        public string trackNumber = ""
        public string comment = ""

        _init_()
        {
            this.versionMajor = 3
            this.versionMinor = 0
            this.size = 0
            this.hasFooter = false
            this.unsynchronisation = false
            this.hasExtendedHeader = false
            this.title = ""
            this.artist = ""
            this.album = ""
            this.year = ""
            this.genre = ""
            this.trackNumber = ""
            this.comment = ""
        }

        public get bool isValid()
        {
            ret this.size > 0
        }

        # 写入 AudioInfo 的通用标签字段
        public void applyTo( AudioInfo info )
        {
            if info == null
            {
                ret
            }
            if this.title != "" { info.title = this.title }
            if this.artist != "" { info.artist = this.artist }
            if this.album != "" { info.album = this.album }
            if this.year != "" { info.year = this.year }
            if this.genre != "" { info.genre = this.genre }
            if this.trackNumber != "" { info.trackNumber = this.trackNumber }
            if this.comment != "" { info.comment = this.comment }
        }

        override string toString()
        {
            ret "ID3v2(v" + this.versionMajor.toString() + ", size=" + this.size.toString() + ")"
        }
    }

    # ── 编码参数 ───────────────────────────────────────────
    public class Mp3EncodeOptions
    {
        # 目标码率 kbps：128 / 192 / 256 / 320 …
        public Int32 bitrateKbps = 192
        public EBitrateMode mode = EBitrateMode.CBR
        # VBR 质量 0..9（0 最好）
        public Int32 vbrQuality = 4
        public EMp3ChannelMode channelMode = EMp3ChannelMode.JointStereo
        public Int32 sampleRate = 44100

        _init_()
        {
            this.bitrateKbps = 192
            this.mode = EBitrateMode.CBR
            this.vbrQuality = 4
            this.channelMode = EMp3ChannelMode.JointStereo
            this.sampleRate = 44100
        }

        public void clamp()
        {
            if this.bitrateKbps < 32 { this.bitrateKbps = 32 }
            if this.bitrateKbps > 320 { this.bitrateKbps = 320 }
            if this.vbrQuality < 0 { this.vbrQuality = 0 }
            if this.vbrQuality > 9 { this.vbrQuality = 9 }
        }

        public static Mp3EncodeOptions defaultOptions()
        {
            ret Mp3EncodeOptions()
        }

        public static Mp3EncodeOptions ofBitrate( Int32 kbps )
        {
            Mp3EncodeOptions o = Mp3EncodeOptions()
            o.bitrateKbps = kbps
            o.clamp()
            ret o
        }
    }

    # ── MP3 门面 ───────────────────────────────────────────
    public class Mp3
    {
        # ── 采样率表 ───────────────────────────────────────
        public static Int32 sampleRateOf( EMp3Version version, Int32 index )
        {
            if index < 0 || index > 3
            {
                ret 0
            }
            if version == EMp3Version.Mpeg1
            {
                if index == 0 { ret 44100 }
                if index == 1 { ret 48000 }
                if index == 2 { ret 32000 }
                ret 0
            }
            if version == EMp3Version.Mpeg2
            {
                if index == 0 { ret 22050 }
                if index == 1 { ret 24000 }
                if index == 2 { ret 16000 }
                ret 0
            }
            if version == EMp3Version.Mpeg25
            {
                if index == 0 { ret 11025 }
                if index == 1 { ret 12000 }
                if index == 2 { ret 8000 }
                ret 0
            }
            ret 0
        }

        # ── 码率表（kbps），index 1..14 有效 ────────────────
        public static Int32 bitrateOf( EMp3Version version, EMp3Layer layer, Int32 index )
        {
            if index <= 0 || index >= 15
            {
                ret 0
            }
            bool isV1 = version == EMp3Version.Mpeg1

            if isV1 && layer == EMp3Layer.Layer3
            {
                if index == 1 { ret 32 }
                if index == 2 { ret 40 }
                if index == 3 { ret 48 }
                if index == 4 { ret 56 }
                if index == 5 { ret 64 }
                if index == 6 { ret 80 }
                if index == 7 { ret 96 }
                if index == 8 { ret 112 }
                if index == 9 { ret 128 }
                if index == 10 { ret 160 }
                if index == 11 { ret 192 }
                if index == 12 { ret 224 }
                if index == 13 { ret 256 }
                if index == 14 { ret 320 }
                ret 0
            }

            if isV1 && layer == EMp3Layer.Layer2
            {
                if index == 1 { ret 32 }
                if index == 2 { ret 48 }
                if index == 3 { ret 56 }
                if index == 4 { ret 64 }
                if index == 5 { ret 80 }
                if index == 6 { ret 96 }
                if index == 7 { ret 112 }
                if index == 8 { ret 128 }
                if index == 9 { ret 160 }
                if index == 10 { ret 192 }
                if index == 11 { ret 224 }
                if index == 12 { ret 256 }
                if index == 13 { ret 320 }
                if index == 14 { ret 384 }
                ret 0
            }

            if isV1 && layer == EMp3Layer.Layer1
            {
                ret index * 32
            }

            # MPEG2 / 2.5
            if layer == EMp3Layer.Layer1
            {
                if index == 1 { ret 32 }
                if index == 2 { ret 48 }
                if index == 3 { ret 56 }
                if index == 4 { ret 64 }
                if index == 5 { ret 80 }
                if index == 6 { ret 96 }
                if index == 7 { ret 112 }
                if index == 8 { ret 128 }
                if index == 9 { ret 144 }
                if index == 10 { ret 160 }
                if index == 11 { ret 176 }
                if index == 12 { ret 192 }
                if index == 13 { ret 224 }
                if index == 14 { ret 256 }
                ret 0
            }

            # MPEG2 / 2.5 的 Layer2 与 Layer3 共用
            if index == 1 { ret 8 }
            if index == 2 { ret 16 }
            if index == 3 { ret 24 }
            if index == 4 { ret 32 }
            if index == 5 { ret 40 }
            if index == 6 { ret 48 }
            if index == 7 { ret 56 }
            if index == 8 { ret 64 }
            if index == 9 { ret 80 }
            if index == 10 { ret 96 }
            if index == 11 { ret 112 }
            if index == 12 { ret 128 }
            if index == 13 { ret 144 }
            if index == 14 { ret 160 }
            ret 0
        }

        # ── 4 字节帧头解析（纯 SL）─────────────────────────
        #
        #   FF FB 90 64 这类同步头：
        #   byte1 = 111V VLLP    byte2 = BBBB SS P D    byte3 = MM ...
        #
        public static Mp3FrameHeader readFrameHeader( Array<UInt8> bytes, Int32 offset )
        {
            Mp3FrameHeader h = Mp3FrameHeader()
            if bytes == null || offset < 0 || offset + 4 > bytes.length
            {
                ret h
            }
            Int32 b0 = AudioInfo.readU8( bytes, offset )
            Int32 b1 = AudioInfo.readU8( bytes, offset + 1 )
            Int32 b2 = AudioInfo.readU8( bytes, offset + 2 )
            Int32 b3 = AudioInfo.readU8( bytes, offset + 3 )

            if b0 != 0xFF || ( b1 & 0xE0 ) != 0xE0
            {
                ret h
            }

            Int32 v = ( b1 >> 3 ) & 0x03
            if v == 0 { h.version = EMp3Version.Mpeg25 }
            elif v == 2 { h.version = EMp3Version.Mpeg2 }
            elif v == 3 { h.version = EMp3Version.Mpeg1 }
            else { h.version = EMp3Version.Reserved }

            Int32 l = ( b1 >> 1 ) & 0x03
            if l == 1 { h.layer = EMp3Layer.Layer3 }
            elif l == 2 { h.layer = EMp3Layer.Layer2 }
            elif l == 3 { h.layer = EMp3Layer.Layer1 }
            else { h.layer = EMp3Layer.Reserved }

            h.hasCrc = ( b1 & 0x01 ) == 0

            Int32 bitrateIndex = ( b2 >> 4 ) & 0x0F
            Int32 rateIndex = ( b2 >> 2 ) & 0x03
            h.padding = ( b2 >> 1 ) & 0x01

            Int32 cm = ( b3 >> 6 ) & 0x03
            if cm == 0 { h.channelMode = EMp3ChannelMode.Stereo }
            elif cm == 1 { h.channelMode = EMp3ChannelMode.JointStereo }
            elif cm == 2 { h.channelMode = EMp3ChannelMode.DualChannel }
            else { h.channelMode = EMp3ChannelMode.Mono }

            h.bitrateKbps = Mp3.bitrateOf( h.version, h.layer, bitrateIndex )
            h.sampleRate = Mp3.sampleRateOf( h.version, rateIndex )
            h.frameLength = Mp3.frameLengthOf( h )
            ret h
        }

        public static Int32 frameLengthOf( Mp3FrameHeader h )
        {
            if h == null || h.bitrateKbps <= 0 || h.sampleRate <= 0
            {
                ret 0
            }
            Int32 bps = h.bitrateKbps * 1000
            if h.version == EMp3Version.Mpeg1 && h.layer == EMp3Layer.Layer3
            {
                ret 144 * bps / h.sampleRate + h.padding
            }
            if h.layer == EMp3Layer.Layer1
            {
                ret ( 12 * bps / h.sampleRate + h.padding ) * 4
            }
            ret 72 * bps / h.sampleRate + h.padding
        }

        # ── ID3v2 ──────────────────────────────────────────
        public static bool hasId3v2( Array<UInt8> bytes )
        {
            if bytes == null || bytes.length < 10
            {
                ret false
            }
            ret AudioInfo.readU8( bytes, 0 ) == 0x49 &&
                AudioInfo.readU8( bytes, 1 ) == 0x44 &&
                AudioInfo.readU8( bytes, 2 ) == 0x33
        }

        # syncsafe：每字节只用低 7 位，共 28 位
        public static Int32 id3v2Size( Array<UInt8> bytes )
        {
            if !Mp3.hasId3v2( bytes )
            {
                ret 0
            }
            Int32 b0 = AudioInfo.readU8( bytes, 6 )
            Int32 b1 = AudioInfo.readU8( bytes, 7 )
            Int32 b2 = AudioInfo.readU8( bytes, 8 )
            Int32 b3 = AudioInfo.readU8( bytes, 9 )
            ret ( b0 << 21 ) | ( b1 << 14 ) | ( b2 << 7 ) | b3
        }

        # 标签区总长（含 10 字节头）
        public static Int32 id3v2TotalSize( Array<UInt8> bytes )
        {
            if !Mp3.hasId3v2( bytes )
            {
                ret 0
            }
            ret 10 + Mp3.id3v2Size( bytes )
        }

        # ID3v1：文件末尾 128 字节，以 "TAG" 开头
        public static bool hasId3v1( Array<UInt8> bytes )
        {
            if bytes == null || bytes.length < 128
            {
                ret false
            }
            Int32 p = bytes.length - 128
            ret AudioInfo.readU8( bytes, p ) == 0x54 &&
                AudioInfo.readU8( bytes, p + 1 ) == 0x41 &&
                AudioInfo.readU8( bytes, p + 2 ) == 0x47
        }

        # 第一个 MPEG 帧的偏移（跳过 ID3v2）
        public static Int32 firstFrameOffset( Array<UInt8> bytes )
        {
            if Mp3.hasId3v2( bytes )
            {
                ret Mp3.id3v2TotalSize( bytes )
            }
            ret 0
        }

        public static bool isMp3( Array<UInt8> bytes )
        {
            if bytes == null || bytes.length < 4
            {
                ret false
            }
            if Mp3.hasId3v2( bytes )
            {
                ret true
            }
            Int32 b0 = AudioInfo.readU8( bytes, 0 )
            Int32 b1 = AudioInfo.readU8( bytes, 1 )
            if b0 == 0xFF && ( b1 & 0xE0 ) == 0xE0
            {
                ret true
            }
            ret false
        }

        # 按 CBR 估算时长：byteSize * 8 / (bitrate * 1000)
        public static Float32 estimatedDuration( Int32 byteSize, Int32 bitrateKbps )
        {
            if bitrateKbps <= 0
            {
                ret 0.0f
            }
            ret ( 1.0f * byteSize * 8 ) / ( 1.0f * bitrateKbps * 1000 )
        }

        # ── 后端 ────────────────────────────────────────────
        public static AudioBuffer decode( Array<UInt8> bytes )
        {
            if bytes == null
            {
                ret null
            }
            object result = SystemCallExternalFunction( "Audio.mp3Decode", bytes )
            ret result as AudioBuffer
        }

        public static Array<UInt8> encode( AudioBuffer buffer, Mp3EncodeOptions options )
        {
            if buffer == null
            {
                ret null
            }
            object result = SystemCallExternalFunction( "Audio.mp3Encode",
                buffer.sampleRate, buffer.channels, buffer, options )
            ret result as Array<UInt8>
        }

        public static ID3v2Tag readTags( Array<UInt8> bytes )
        {
            if bytes == null
            {
                ret null
            }
            object result = SystemCallExternalFunction( "Audio.mp3Tags", bytes )
            ret result as ID3v2Tag
        }

        override string toString()
        {
            ret "Mp3"
        }
    }
}
