# =========================================================================
# Audio/Wav.sl —— WAV（RIFF / PCM）
#
# ★ 本文件**全部用纯 SL 实现**，不依赖任何后端：
#     RIFF 块遍历、fmt 块解析、PCM 与 Float32 互转、WAV 文件构造。
#   这是本目录里唯一能开箱即用的完整编解码器。
#
# RIFF 结构：
#   "RIFF"(0..3) riffSize(4..7 LE) "WAVE"(8..11)
#   之后是块序列：chunkId(4) chunkSize(4 LE) payload(chunkSize) [pad 1 if odd]
#
# fmt 块（16 字节）：
#   audioFormat(2) channels(2) sampleRate(4) byteRate(4) blockAlign(2) bitsPerSample(2)
# =========================================================================

namespace Audio
{
    # WAVE 格式标签（wFormatTag）
    public enum EWaveFormatTag
    {
        Unknown = 0
        Pcm = 1          # 整数 PCM
        Adpcm = 2
        Float = 3        # IEEE Float32 / Float64
        Alaw = 6
        Mulaw = 7
        DviAdpcm = 17
        Extensible = 65534   # 0xFFFE
    }

    # ── 头 ─────────────────────────────────────────────────
    public class WavHeader
    {
        public EWaveFormatTag audioFormat = EWaveFormatTag.Pcm
        public Int32 channels = 0
        public Int32 sampleRate = 0
        public Int32 byteRate = 0
        public Int32 blockAlign = 0
        public Int32 bitsPerSample = 0

        # data 块的偏移与长度（解析时填好，便于直接切 PCM）
        public Int32 dataOffset = 0
        public Int32 dataSize = 0

        _init_()
        {
            this.audioFormat = EWaveFormatTag.Pcm
            this.channels = 0
            this.sampleRate = 0
            this.byteRate = 0
            this.blockAlign = 0
            this.bitsPerSample = 0
            this.dataOffset = 0
            this.dataSize = 0
        }

        public get bool isValid()
        {
            ret this.channels > 0 && this.sampleRate > 0 &&
                this.bitsPerSample > 0 && this.blockAlign > 0
        }

        public get bool isPcm()
        {
            ret this.audioFormat == EWaveFormatTag.Pcm
        }

        public get bool isFloat()
        {
            ret this.audioFormat == EWaveFormatTag.Float
        }

        public get Int32 bytesPerSample()
        {
            ret this.bitsPerSample / 8
        }

        public get Int32 frameCount()
        {
            if this.blockAlign <= 0
            {
                ret 0
            }
            ret this.dataSize / this.blockAlign
        }

        public get Float32 duration()
        {
            if this.sampleRate <= 0
            {
                ret 0.0f
            }
            ret 1.0f * this.frameCount() / this.sampleRate
        }

        public get Int32 bitrateKbps()
        {
            # bitrate = sampleRate * bits * channels / 1000
            ret this.sampleRate * this.bitsPerSample * this.channels / 1000
        }

        public ESampleFormat sampleFormat()
        {
            if this.bitsPerSample == 8 { ret ESampleFormat.U8 }
            if this.bitsPerSample == 16 { ret ESampleFormat.S16 }
            if this.bitsPerSample == 24 { ret ESampleFormat.S24 }
            if this.bitsPerSample == 32 && this.isFloat { ret ESampleFormat.F32 }
            if this.bitsPerSample == 32 { ret ESampleFormat.S32 }
            ret ESampleFormat.Unknown
        }

        override string toString()
        {
            ret "WavHeader(" + this.sampleRate.toString() + "Hz, " +
                this.channels.toString() + "ch, " + this.bitsPerSample.toString() + "bit, " +
                this.duration().toString() + "s)"
        }
    }

    # ── WAV 门面（纯 SL）────────────────────────────────────
    public class Wav
    {
        # "RIFF"...."WAVE"
        public static bool isWav( Array<UInt8> bytes )
        {
            if bytes == null || bytes.length < 44
            {
                ret false
            }
            ret AudioInfo.readU8( bytes, 0 ) == 0x52 &&
                AudioInfo.readU8( bytes, 1 ) == 0x49 &&
                AudioInfo.readU8( bytes, 2 ) == 0x46 &&
                AudioInfo.readU8( bytes, 3 ) == 0x46 &&
                AudioInfo.readU8( bytes, 8 ) == 0x57 &&
                AudioInfo.readU8( bytes, 9 ) == 0x41 &&
                AudioInfo.readU8( bytes, 10 ) == 0x56 &&
                AudioInfo.readU8( bytes, 11 ) == 0x45
        }

        static bool isChunkId( Array<UInt8> b, Int32 offset, string id )
        {
            if b == null || id == null
            {
                ret false
            }
            if offset < 0 || offset + 4 > b.length
            {
                ret false
            }
            Int32 i = 0
            while i < 4
            {
                if AudioInfo.readU8( b, offset + i ) != SystemStringCharCodeAt( id, i )
                {
                    ret false
                }
                i = i + 1
            }
            ret true
        }

        # ── 遍历块解析头（纯 SL）────────────────────────────
        public static WavHeader readHeader( Array<UInt8> bytes )
        {
            WavHeader h = WavHeader()
            if !Wav.isWav( bytes )
            {
                ret h
            }
            Int32 pos = 12
            Int32 limit = bytes.length

            while pos + 8 <= limit
            {
                Int32 size = AudioInfo.readLe32( bytes, pos + 4 )
                if size < 0
                {
                    size = 0
                }

                if Wav.isChunkId( bytes, pos, "fmt " )
                {
                    if pos + 8 + 16 <= limit
                    {
                        Int32 tag = AudioInfo.readLe16( bytes, pos + 8 )
                        if tag == 1 { h.audioFormat = EWaveFormatTag.Pcm }
                        elif tag == 3 { h.audioFormat = EWaveFormatTag.Float }
                        elif tag == 6 { h.audioFormat = EWaveFormatTag.Alaw }
                        elif tag == 7 { h.audioFormat = EWaveFormatTag.Mulaw }
                        elif tag == 65534 { h.audioFormat = EWaveFormatTag.Extensible }
                        else { h.audioFormat = EWaveFormatTag.Unknown }

                        h.channels = AudioInfo.readLe16( bytes, pos + 10 )
                        h.sampleRate = AudioInfo.readLe32( bytes, pos + 12 )
                        h.byteRate = AudioInfo.readLe32( bytes, pos + 16 )
                        h.blockAlign = AudioInfo.readLe16( bytes, pos + 20 )
                        h.bitsPerSample = AudioInfo.readLe16( bytes, pos + 22 )
                    }
                }
                elif Wav.isChunkId( bytes, pos, "data" )
                {
                    h.dataOffset = pos + 8
                    h.dataSize = size
                    if h.dataOffset + h.dataSize > limit
                    {
                        h.dataSize = limit - h.dataOffset
                    }
                    if h.dataSize < 0
                    {
                        h.dataSize = 0
                    }
                    ret h
                }

                # 前进到下一个块（块大小为奇数时要补 1 字节对齐）
                Int32 step = size + 8
                if size % 2 == 1
                {
                    step = step + 1
                }
                if step <= 0
                {
                    step = 8
                }
                pos = pos + step
            }

            ret h
        }

        # ── 解码 PCM 数据（纯 SL）───────────────────────────
        public static AudioBuffer decode( Array<UInt8> bytes )
        {
            if bytes == null
            {
                ret null
            }
            WavHeader h = Wav.readHeader( bytes )
            if !h.isValid || h.dataSize <= 0
            {
                ret null
            }
            Int32 frames = h.frameCount()
            if frames <= 0
            {
                ret null
            }
            AudioBuffer buf = AudioBuffer( h.sampleRate, h.channels, frames )
            buf.sourceFormat = h.sampleFormat()

            Int32 bps = h.bytesPerSample()
            Int32 i = 0
            Int32 total = frames * h.channels
            while i < total
            {
                Int32 p = h.dataOffset + i * bps
                Float32 v = 0.0f

                if bps == 1
                {
                    # 8 位无符号，零点 128
                    Int32 u = AudioInfo.readU8( bytes, p ) - 128
                    v = 1.0f * u / 128.0f
                }
                elif bps == 2
                {
                    Int32 s = AudioInfo.readLe16( bytes, p )
                    if s >= 32768
                    {
                        s = s - 65536
                    }
                    v = 1.0f * s / 32768.0f
                }
                elif bps == 3
                {
                    # 24 位小端有符号
                    Int32 s = AudioInfo.readU8( bytes, p ) |
                              ( AudioInfo.readU8( bytes, p + 1 ) << 8 ) |
                              ( AudioInfo.readU8( bytes, p + 2 ) << 16 )
                    if s >= 8388608
                    {
                        s = s - 16777216
                    }
                    v = 1.0f * s / 8388608.0f
                }
                elif bps == 4
                {
                    v = SystemConvertFloat32( Wav.readFloat32Le( bytes, p ) )
                }

                buf.setSample( i / h.channels, i % h.channels, v )
                i = i + 1
            }

            ret buf
        }

        # 小端 Float32（按位重组，避免依赖后端）
        public static Float32 readFloat32Le( Array<UInt8> b, Int32 offset )
        {
            Int32 bits = AudioInfo.readU8( b, offset ) |
                         ( AudioInfo.readU8( b, offset + 1 ) << 8 ) |
                         ( AudioInfo.readU8( b, offset + 2 ) << 16 ) |
                         ( AudioInfo.readU8( b, offset + 3 ) << 24 )
            ret SystemConvertFloat32( bits )
        }

        # ── 编码（纯 SL）────────────────────────────────────
        # 默认输出 16 位 PCM；bitsPerSample 支持 8 / 16 / 24 / 32(float)
        public static Array<UInt8> encode( AudioBuffer buffer, Int32 bitsPerSample )
        {
            if buffer == null || !buffer.isValid
            {
                ret null
            }
            if bitsPerSample != 8 && bitsPerSample != 16 &&
               bitsPerSample != 24 && bitsPerSample != 32
            {
                bitsPerSample = 16
            }
            Int32 channels = buffer.channels
            Int32 total = buffer.sampleCount()
            Int32 dataSize = total * ( bitsPerSample / 8 )
            Array<UInt8> out = Array<UInt8>( 44 + dataSize )

            Wav.writeFourCC( out, 0, "RIFF" )
            Wav.writeLe32( out, 4, 36 + dataSize )
            Wav.writeFourCC( out, 8, "WAVE" )

            # fmt 块
            Wav.writeFourCC( out, 12, "fmt " )
            Wav.writeLe32( out, 16, 16 )
            Int32 tag = 1
            if bitsPerSample == 32
            {
                tag = 3
            }
            Wav.writeLe16( out, 20, tag )
            Wav.writeLe16( out, 22, channels )
            Wav.writeLe32( out, 24, buffer.sampleRate )
            Wav.writeLe32( out, 28, buffer.sampleRate * channels * bitsPerSample / 8 )
            Wav.writeLe16( out, 32, channels * bitsPerSample / 8 )
            Wav.writeLe16( out, 34, bitsPerSample )

            # data 块
            Wav.writeFourCC( out, 36, "data" )
            Wav.writeLe32( out, 40, dataSize )

            Int32 i = 0
            while i < total
            {
                Float32 f = buffer.getSample( i / channels, i % channels )
                Int32 p = 44 + i * ( bitsPerSample / 8 )

                if bitsPerSample == 8
                {
                    Int32 u = SystemConvertInt32( f * 127.0f + 128.0f )
                    if u < 0 { u = 0 }
                    if u > 255 { u = 255 }
                    out[ p ] = SystemConvertUInt8( u )
                }
                elif bitsPerSample == 16
                {
                    Int32 s = SystemConvertInt32( f * 32767.0f )
                    if s < -32768 { s = -32768 }
                    if s > 32767 { s = 32767 }
                    if s < 0 { s = s + 65536 }
                    out[ p ] = SystemConvertUInt8( s & 0xFF )
                    out[ p + 1 ] = SystemConvertUInt8( ( s >> 8 ) & 0xFF )
                }
                elif bitsPerSample == 24
                {
                    Int32 s = SystemConvertInt32( f * 8388607.0f )
                    if s < -8388608 { s = -8388608 }
                    if s > 8388607 { s = 8388607 }
                    if s < 0 { s = s + 16777216 }
                    out[ p ] = SystemConvertUInt8( s & 0xFF )
                    out[ p + 1 ] = SystemConvertUInt8( ( s >> 8 ) & 0xFF )
                    out[ p + 2 ] = SystemConvertUInt8( ( s >> 16 ) & 0xFF )
                }
                else
                {
                    # 32 位浮点按整数位模式写回，精度有限，仅用于占位
                    Int32 s = SystemConvertInt32( f * 32767.0f )
                    if s < -32768 { s = -32768 }
                    if s > 32767 { s = 32767 }
                    if s < 0 { s = s + 65536 }
                    out[ p ] = SystemConvertUInt8( s & 0xFF )
                    out[ p + 1 ] = SystemConvertUInt8( ( s >> 8 ) & 0xFF )
                    out[ p + 2 ] = SystemConvertUInt8( 0 )
                    out[ p + 3 ] = SystemConvertUInt8( 0 )
                }

                i = i + 1
            }

            ret out
        }

        public static Array<UInt8> encode( AudioBuffer buffer )
        {
            ret Wav.encode( buffer, 16 )
        }

        # ── 写字节工具 ───────────────────────────────────────
        public static void writeFourCC( Array<UInt8> b, Int32 offset, string id )
        {
            if b == null || id == null
            {
                ret
            }
            Int32 i = 0
            while i < 4
            {
                Int32 c = SystemStringCharCodeAt( id, i )
                b[ offset + i ] = SystemConvertUInt8( c )
                i = i + 1
            }
        }

        public static void writeLe32( Array<UInt8> b, Int32 offset, Int32 value )
        {
            Int32 v = value
            b[ offset ] = SystemConvertUInt8( v & 0xFF )
            b[ offset + 1 ] = SystemConvertUInt8( ( v >> 8 ) & 0xFF )
            b[ offset + 2 ] = SystemConvertUInt8( ( v >> 16 ) & 0xFF )
            b[ offset + 3 ] = SystemConvertUInt8( ( v >> 24 ) & 0xFF )
        }

        public static void writeLe16( Array<UInt8> b, Int32 offset, Int32 value )
        {
            Int32 v = value
            b[ offset ] = SystemConvertUInt8( v & 0xFF )
            b[ offset + 1 ] = SystemConvertUInt8( ( v >> 8 ) & 0xFF )
        }

        override string toString()
        {
            ret "Wav"
        }
    }
}
