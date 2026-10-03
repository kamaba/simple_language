# =========================================================================
# Audio/AudioInfo.sl —— 音频元数据 + 格式嗅探
#
# 纯 SL 实现：
#   - sniff()        ：按 magic bytes 判定容器 / 编码格式
#   - fromExtension()：按扩展名判定
#   - probe()        ：WAV 直接解析 RIFF 头取参数；其余交给后端
#
# 用途：只想知道时长 / 采样率 / 码率时不必把整个文件解成 PCM。
# =========================================================================

namespace Audio
{
    public class AudioInfo
    {
        public EAudioFormat format = EAudioFormat.Unknown
        public ESampleFormat sampleFormat = ESampleFormat.Unknown

        public Int32 sampleRate = 0
        public Int32 channels = 0
        public Int32 bitDepth = 0
        public Int32 bitrateKbps = 0
        public EBitrateMode bitrateMode = EBitrateMode.Unknown

        public Float32 duration = 0.0f
        public Int32 frameCount = 0
        public Int64 byteSize = 0

        # ── 通用标签 ─────────────────────────────────────────
        public string title = ""
        public string artist = ""
        public string album = ""
        public string albumArtist = ""
        public string year = ""
        public string genre = ""
        public string trackNumber = ""
        public string comment = ""

        _init_()
        {
            this.format = EAudioFormat.Unknown
            this.sampleFormat = ESampleFormat.Unknown
            this.sampleRate = 0
            this.channels = 0
            this.bitDepth = 0
            this.bitrateKbps = 0
            this.bitrateMode = EBitrateMode.Unknown
            this.duration = 0.0f
            this.frameCount = 0
            this.byteSize = 0
            this.title = ""
            this.artist = ""
            this.album = ""
            this.albumArtist = ""
            this.year = ""
            this.genre = ""
            this.trackNumber = ""
            this.comment = ""
        }

        public get bool isValid()
        {
            ret this.format != EAudioFormat.Unknown && this.sampleRate > 0 && this.channels > 0
        }

        # 是否含任意标签
        public get bool hasTags()
        {
            ret this.title != "" || this.artist != "" || this.album != ""
        }

        # ── 字节工具（复用与 Image 相同的约定）──────────────
        public static Int32 readU8( Array<UInt8> b, Int32 i )
        {
            if b == null || i < 0 || i >= b.length
            {
                ret 0
            }
            ret SystemConvertInt32( b[ i ] )
        }

        public static Int32 readLe32( Array<UInt8> b, Int32 i )
        {
            ret AudioInfo.readU8( b, i ) | ( AudioInfo.readU8( b, i + 1 ) << 8 ) |
                ( AudioInfo.readU8( b, i + 2 ) << 16 ) | ( AudioInfo.readU8( b, i + 3 ) << 24 )
        }

        public static Int32 readBe32( Array<UInt8> b, Int32 i )
        {
            ret ( AudioInfo.readU8( b, i ) << 24 ) | ( AudioInfo.readU8( b, i + 1 ) << 16 ) |
                ( AudioInfo.readU8( b, i + 2 ) << 8 ) | AudioInfo.readU8( b, i + 3 )
        }

        public static Int32 readLe16( Array<UInt8> b, Int32 i )
        {
            ret AudioInfo.readU8( b, i ) | ( AudioInfo.readU8( b, i + 1 ) << 8 )
        }

        # 忽略大小写的后缀比较（String 无 toLowerCase / endsWith）
        public static bool endsWithFold( string text, string suffix )
        {
            if text == null || suffix == null
            {
                ret false
            }
            Int32 n = text.length()
            Int32 m = suffix.length()
            if m <= 0 || n < m
            {
                ret false
            }
            Int32 start = n - m
            Int32 i = 0
            while i < m
            {
                Int32 a = SystemStringCharCodeAt( text, start + i )
                Int32 b = SystemStringCharCodeAt( suffix, i )
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

        # ── magic bytes 嗅探（纯 SL）────────────────────────
        public static EAudioFormat sniff( Array<UInt8> bytes )
        {
            if bytes == null || bytes.length < 12
            {
                ret EAudioFormat.Unknown
            }

            # WAV: "RIFF" .... "WAVE"
            if AudioInfo.readU8( bytes, 0 ) == 0x52 && AudioInfo.readU8( bytes, 1 ) == 0x49 &&
               AudioInfo.readU8( bytes, 2 ) == 0x46 && AudioInfo.readU8( bytes, 3 ) == 0x46 &&
               AudioInfo.readU8( bytes, 8 ) == 0x57 && AudioInfo.readU8( bytes, 9 ) == 0x41 &&
               AudioInfo.readU8( bytes, 10 ) == 0x56 && AudioInfo.readU8( bytes, 11 ) == 0x45
            {
                ret EAudioFormat.Wav
            }

            # AIFF: "FORM" .... "AIFF"
            if AudioInfo.readU8( bytes, 0 ) == 0x46 && AudioInfo.readU8( bytes, 1 ) == 0x4F &&
               AudioInfo.readU8( bytes, 2 ) == 0x52 && AudioInfo.readU8( bytes, 3 ) == 0x4D
            {
                ret EAudioFormat.Aiff
            }

            # FLAC: "fLaC"
            if AudioInfo.readU8( bytes, 0 ) == 0x66 && AudioInfo.readU8( bytes, 1 ) == 0x4C &&
               AudioInfo.readU8( bytes, 2 ) == 0x61 && AudioInfo.readU8( bytes, 3 ) == 0x43
            {
                ret EAudioFormat.Flac
            }

            # Ogg: "OggS"
            if AudioInfo.readU8( bytes, 0 ) == 0x4F && AudioInfo.readU8( bytes, 1 ) == 0x67 &&
               AudioInfo.readU8( bytes, 2 ) == 0x67 && AudioInfo.readU8( bytes, 3 ) == 0x53
            {
                ret EAudioFormat.Ogg
            }

            # ID3v2 -> MP3
            if AudioInfo.readU8( bytes, 0 ) == 0x49 && AudioInfo.readU8( bytes, 1 ) == 0x44 &&
               AudioInfo.readU8( bytes, 2 ) == 0x33
            {
                ret EAudioFormat.Mp3
            }

            # AMR: "#!AMR"
            if AudioInfo.readU8( bytes, 0 ) == 0x23 && AudioInfo.readU8( bytes, 1 ) == 0x21 &&
               AudioInfo.readU8( bytes, 2 ) == 0x41 && AudioInfo.readU8( bytes, 3 ) == 0x4D
            {
                ret EAudioFormat.Amr
            }

            # MIDI: "MThd"
            if AudioInfo.readU8( bytes, 0 ) == 0x4D && AudioInfo.readU8( bytes, 1 ) == 0x54 &&
               AudioInfo.readU8( bytes, 2 ) == 0x68 && AudioInfo.readU8( bytes, 3 ) == 0x64
            {
                ret EAudioFormat.Midi
            }

            # MP4 / M4A: .... "ftyp"
            if AudioInfo.readU8( bytes, 4 ) == 0x66 && AudioInfo.readU8( bytes, 5 ) == 0x74 &&
               AudioInfo.readU8( bytes, 6 ) == 0x79 && AudioInfo.readU8( bytes, 7 ) == 0x70
            {
                ret EAudioFormat.Aac
            }

            # 裸 MPEG 帧同步：FF Ex（MPEG1/2 Layer3）或 FF Fx（AAC ADTS）
            Int32 b0 = AudioInfo.readU8( bytes, 0 )
            Int32 b1 = AudioInfo.readU8( bytes, 1 )
            if b0 == 0xFF
            {
                if ( b1 & 0xE0 ) == 0xE0
                {
                    ret EAudioFormat.Mp3
                }
                if ( b1 & 0xF0 ) == 0xF0
                {
                    ret EAudioFormat.Aac
                }
            }

            ret EAudioFormat.Unknown
        }

        # ── 扩展名判定 ───────────────────────────────────────
        public static EAudioFormat fromExtension( string path )
        {
            if path == null || path == ""
            {
                ret EAudioFormat.Unknown
            }
            if AudioInfo.endsWithFold( path, ".wav" )   { ret EAudioFormat.Wav }
            if AudioInfo.endsWithFold( path, ".wave" )  { ret EAudioFormat.Wav }
            if AudioInfo.endsWithFold( path, ".mp3" )   { ret EAudioFormat.Mp3 }
            if AudioInfo.endsWithFold( path, ".aac" )   { ret EAudioFormat.Aac }
            if AudioInfo.endsWithFold( path, ".m4a" )   { ret EAudioFormat.Aac }
            if AudioInfo.endsWithFold( path, ".mp4" )   { ret EAudioFormat.Aac }
            if AudioInfo.endsWithFold( path, ".flac" )  { ret EAudioFormat.Flac }
            if AudioInfo.endsWithFold( path, ".ogg" )   { ret EAudioFormat.Ogg }
            if AudioInfo.endsWithFold( path, ".oga" )   { ret EAudioFormat.Ogg }
            if AudioInfo.endsWithFold( path, ".opus" )  { ret EAudioFormat.Opus }
            if AudioInfo.endsWithFold( path, ".wma" )   { ret EAudioFormat.Wma }
            if AudioInfo.endsWithFold( path, ".aiff" )  { ret EAudioFormat.Aiff }
            if AudioInfo.endsWithFold( path, ".aif" )   { ret EAudioFormat.Aiff }
            if AudioInfo.endsWithFold( path, ".amr" )   { ret EAudioFormat.Amr }
            if AudioInfo.endsWithFold( path, ".mid" )   { ret EAudioFormat.Midi }
            if AudioInfo.endsWithFold( path, ".midi" )  { ret EAudioFormat.Midi }
            ret EAudioFormat.Unknown
        }

        # ── 探测元数据（不解码 PCM）─────────────────────────
        public static AudioInfo probe( Array<UInt8> bytes )
        {
            AudioInfo info = AudioInfo()
            if bytes == null
            {
                ret info
            }
            info.byteSize = bytes.length
            info.format = AudioInfo.sniff( bytes )

            if info.format == EAudioFormat.Wav
            {
                WavHeader h = Wav.readHeader( bytes )
                info.sampleRate = h.sampleRate
                info.channels = h.channels
                info.bitDepth = h.bitsPerSample
                info.frameCount = h.frameCount()
                info.duration = h.duration()
                info.bitrateKbps = h.bitrateKbps()
                info.sampleFormat = h.sampleFormat()
                info.bitrateMode = EBitrateMode.CBR
                ret info
            }

            if info.format == EAudioFormat.Unknown
            {
                ret info
            }

            # 其余格式交给后端
            object result = SystemCallExternalFunction( "Audio.probe", bytes, info.format )
            AudioInfo remote = result as AudioInfo
            if remote != null
            {
                ret remote
            }
            ret info
        }

        override string toString()
        {
            ret "AudioInfo(fmt=" + this.format.toString() + ", " +
                this.sampleRate.toString() + "Hz, " + this.channels.toString() + "ch, " +
                this.duration.toString() + "s)"
        }
    }
}
