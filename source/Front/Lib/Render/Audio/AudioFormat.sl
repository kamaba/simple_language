# =========================================================================
# Audio/AudioFormat.sl —— 音频格式 / 采样格式 / 声道布局 / 错误码
#
# 约定同 DB/Sqlite.sl：所有类型为 namespace 下的顶级类型，不嵌套。
#
# 纯 SL 可完整实现：WAV（RIFF 头 + PCM），见 Wav.sl
# 需要后端编解码：MP3 / AAC / FLAC / OGG（熵编码 + 心理声学模型）
# =========================================================================

namespace Audio
{
    # 容器 / 编码格式
    public enum EAudioFormat
    {
        Unknown = 0
        Wav        # RIFF / PCM（纯 SL 可完整读写）
        Mp3
        Aac        # ADTS / ADIF / MP4 容器
        Flac       # 无损
        Ogg        # Ogg 容器（Vorbis / Opus / FLAC）
        Opus
        Wma
        Aiff
        Amr
        Midi
    }

    # PCM 采样格式（位深）
    public enum ESampleFormat
    {
        Unknown = 0
        U8        # 无符号 8 位，零点 128
        S16       # 有符号 16 位小端（最常见）
        S24       # 有符号 24 位，打包成 3 字节
        S32       # 有符号 32 位
        F32       # IEEE Float32
        F64       # IEEE Float64
    }

    # 声道布局
    public enum EChannelLayout
    {
        Unknown = 0
        Mono = 1
        Stereo = 2
        Quadro = 4
        Surround51 = 6
        Surround71 = 8
    }

    # 常用采样率
    public enum ESampleRate
    {
        Rate8000 = 8000      # 电话
        Rate16000 = 16000    # 语音识别常用
        Rate22050 = 22050
        Rate32000 = 32000
        Rate44100 = 44100    # CD
        Rate48000 = 48000    # 视频 / Opus 原生
        Rate96000 = 96000    # 高解析
    }

    # 码率控制模式
    public enum EBitrateMode
    {
        Unknown = 0
        CBR        # 恒定码率
        VBR        # 可变码率
        ABR        # 平均码率
    }

    public enum AudioErrorCode extends Error
    {
        OK = {code = 0}
        UnsupportedFormat = {code = 1}
        DecodeFailed = {code = 2}
        EncodeFailed = {code = 3}
        InvalidData = {code = 4}
        OutOfBounds = {code = 5}
        CodecMissing = {code = 6}
        IoError = {code = 7}
    }

    # ── 格式能力表（纯 SL）──────────────────────────────────
    public class AudioFormatInfo
    {
        # 是否无损
        public static bool isLossless( EAudioFormat format )
        {
            if format == EAudioFormat.Wav || format == EAudioFormat.Flac ||
               format == EAudioFormat.Aiff
            {
                ret true
            }
            ret false
        }

        # 是否支持元数据标签（ID3 / Vorbis Comment 等）
        public static bool supportsTags( EAudioFormat format )
        {
            if format == EAudioFormat.Mp3 || format == EAudioFormat.Flac ||
               format == EAudioFormat.Ogg || format == EAudioFormat.Opus
            {
                ret true
            }
            ret false
        }

        # 是否需要外部解码库
        public static bool needsNativeCodec( EAudioFormat format )
        {
            if format == EAudioFormat.Wav
            {
                ret false
            }
            ret true
        }

        public static string mimeType( EAudioFormat format )
        {
            if format == EAudioFormat.Wav  { ret "audio/wav" }
            if format == EAudioFormat.Mp3  { ret "audio/mpeg" }
            if format == EAudioFormat.Aac  { ret "audio/aac" }
            if format == EAudioFormat.Flac { ret "audio/flac" }
            if format == EAudioFormat.Ogg  { ret "audio/ogg" }
            if format == EAudioFormat.Opus { ret "audio/opus" }
            if format == EAudioFormat.Wma  { ret "audio/x-ms-wma" }
            if format == EAudioFormat.Aiff { ret "audio/aiff" }
            if format == EAudioFormat.Amr  { ret "audio/amr" }
            if format == EAudioFormat.Midi { ret "audio/midi" }
            ret "application/octet-stream"
        }

        # 首选扩展名（含点号）
        public static string extension( EAudioFormat format )
        {
            if format == EAudioFormat.Wav  { ret ".wav" }
            if format == EAudioFormat.Mp3  { ret ".mp3" }
            if format == EAudioFormat.Aac  { ret ".aac" }
            if format == EAudioFormat.Flac { ret ".flac" }
            if format == EAudioFormat.Ogg  { ret ".ogg" }
            if format == EAudioFormat.Opus { ret ".opus" }
            if format == EAudioFormat.Wma  { ret ".wma" }
            if format == EAudioFormat.Aiff { ret ".aiff" }
            if format == EAudioFormat.Amr  { ret ".amr" }
            if format == EAudioFormat.Midi { ret ".mid" }
            ret ""
        }

        override string toString()
        {
            ret "AudioFormatInfo"
        }
    }
}
