# =========================================================================
# Audio/AudioBuffer.sl —— PCM 采样缓冲（本目录的核心）
#
# 内部统一以 **Float32 交织** 存储，值域 [-1, 1]：
#   index = frame * channels + channel
#
# 纯 SL 实现：
#   - S16LE / U8 / S32 与 Float32 的相互转换
#   - 峰值 / RMS / 归一化 / 混音 / 淡入淡出 / 线性重采样 / 声道变换 / 切片反转
# 依赖后端：
#   - decode() / encode()（MP3 / AAC / FLAC / OGG 的熵编解码）
# =========================================================================

namespace Audio
{
    public class AudioBuffer
    {
        public Int32 sampleRate = 44100
        public Int32 channels = 2
        public ESampleFormat sourceFormat = ESampleFormat.S16

        # 交织样本，长度 = frameCount * channels
        Array<Float32> _samples = null

        _init_()
        {
            this.sampleRate = 44100
            this.channels = 2
            this.sourceFormat = ESampleFormat.S16
            this._samples = Array<Float32>( 0 )
        }

        _init_( Int32 _sampleRate, Int32 _channels )
        {
            this.sampleRate = _sampleRate
            this.channels = _channels
            this.sourceFormat = ESampleFormat.S16
            this._samples = Array<Float32>( 0 )
        }

        _init_( Int32 _sampleRate, Int32 _channels, Int32 _frameCount )
        {
            this.sampleRate = _sampleRate
            this.channels = _channels
            this.sourceFormat = ESampleFormat.S16
            if _frameCount > 0
            {
                this._samples = Array<Float32>( _frameCount * _channels )
            }
            else
            {
                this._samples = Array<Float32>( 0 )
            }
        }

        # ── 属性 ─────────────────────────────────────────────
        public get Int32 frameCount()
        {
            if this.channels <= 0
            {
                ret 0
            }
            ret this._samples.length / this.channels
        }

        public get Int32 sampleCount()
        {
            ret this._samples.length
        }

        public get Float32 duration()
        {
            if this.sampleRate <= 0
            {
                ret 0.0f
            }
            ret 1.0f * this.frameCount() / this.sampleRate
        }

        public get bool isValid()
        {
            ret this.channels > 0 && this.sampleRate > 0 && this._samples.length > 0
        }

        public get EChannelLayout layout()
        {
            if this.channels == 1 { ret EChannelLayout.Mono }
            if this.channels == 2 { ret EChannelLayout.Stereo }
            if this.channels == 4 { ret EChannelLayout.Quadro }
            if this.channels == 6 { ret EChannelLayout.Surround51 }
            if this.channels == 8 { ret EChannelLayout.Surround71 }
            ret EChannelLayout.Unknown
        }

        # ── 采样读写 ─────────────────────────────────────────
        public Float32 getSample( Int32 frame, Int32 channel )
        {
            if this.channels <= 0 || frame < 0 || frame >= this.frameCount()
            {
                ret 0.0f
            }
            if channel < 0 || channel >= this.channels
            {
                ret 0.0f
            }
            ret this._samples[ frame * this.channels + channel ]
        }

        public void setSample( Int32 frame, Int32 channel, Float32 value )
        {
            if this.channels <= 0 || frame < 0 || frame >= this.frameCount()
            {
                ret
            }
            if channel < 0 || channel >= this.channels
            {
                ret
            }
            this._samples[ frame * this.channels + channel ] = value
        }

        # ── S16LE 互转（纯 SL）──────────────────────────────
        # 16 位有符号小端 -> Float32（除以 32768）
        public static AudioBuffer fromS16Interleaved( Array<UInt8> bytes, Int32 channels, Int32 sampleRate )
        {
            if bytes == null || channels <= 0
            {
                ret null
            }
            Int32 frameCount = bytes.length / ( channels * 2 )
            if frameCount <= 0
            {
                ret null
            }
            AudioBuffer buf = AudioBuffer( sampleRate, channels, frameCount )
            buf.sourceFormat = ESampleFormat.S16
            Int32 i = 0
            Int32 total = frameCount * channels
            while i < total
            {
                Int32 lo = SystemConvertInt32( bytes[ i * 2 ] )
                Int32 hi = SystemConvertInt32( bytes[ i * 2 + 1 ] )
                Int32 v = lo | ( hi << 8 )
                if v >= 32768
                {
                    v = v - 65536
                }
                buf._samples[ i ] = 1.0f * v / 32768.0f
                i = i + 1
            }
            ret buf
        }

        # Float32 -> 16 位有符号小端
        public Array<UInt8> toS16Interleaved()
        {
            Int32 n = this._samples.length
            Array<UInt8> out = Array<UInt8>( n * 2 )
            Int32 i = 0
            while i < n
            {
                Float32 f = this._samples[ i ]
                if f > 1.0f { f = 1.0f }
                if f < -1.0f { f = -1.0f }
                Int32 v = SystemConvertInt32( f * 32767.0f )
                if v < 0
                {
                    v = v + 65536
                }
                out[ i * 2 ] = SystemConvertUInt8( v & 0xFF )
                out[ i * 2 + 1 ] = SystemConvertUInt8( ( v >> 8 ) & 0xFF )
                i = i + 1
            }
            ret out
        }

        # 8 位无符号（零点 128）
        public static AudioBuffer fromU8Interleaved( Array<UInt8> bytes, Int32 channels, Int32 sampleRate )
        {
            if bytes == null || channels <= 0
            {
                ret null
            }
            Int32 frameCount = bytes.length / channels
            if frameCount <= 0
            {
                ret null
            }
            AudioBuffer buf = AudioBuffer( sampleRate, channels, frameCount )
            buf.sourceFormat = ESampleFormat.U8
            Int32 i = 0
            Int32 total = frameCount * channels
            while i < total
            {
                Int32 v = SystemConvertInt32( bytes[ i ] ) - 128
                buf._samples[ i ] = 1.0f * v / 128.0f
                i = i + 1
            }
            ret buf
        }

        # ── 电平分析 ─────────────────────────────────────────
        public Float32 peak()
        {
            Float32 max = 0.0f
            Int32 i = 0
            while i < this._samples.length
            {
                Float32 a = this._samples[ i ]
                if a < 0.0f { a = 0.0f - a }
                if a > max { max = a }
                i = i + 1
            }
            ret max
        }

        public Float32 rms()
        {
            if this._samples.length == 0
            {
                ret 0.0f
            }
            Float32 acc = 0.0f
            Int32 i = 0
            while i < this._samples.length
            {
                Float32 s = this._samples[ i ]
                acc = acc + s * s
                i = i + 1
            }
            Float32 mean = acc / SystemConvertFloat32( this._samples.length )
            ret AudioBuffer.sqrt( mean )
        }

        # 牛顿迭代开平方（Std 不依赖 Math 库，这里自带一份）
        public static Float32 sqrt( Float32 x )
        {
            if x <= 0.0f
            {
                ret 0.0f
            }
            Float32 guess = x
            if guess > 1.0f
            {
                guess = x * 0.5f
            }
            else
            {
                guess = 1.0f
            }
            Int32 iter = 0
            while iter < 16
            {
                Float32 next = 0.5f * ( guess + x / guess )
                Float32 diff = next - guess
                if diff < 0.0f { diff = 0.0f - diff }
                guess = next
                if diff < 0.000001f
                {
                    break
                }
                iter = iter + 1
            }
            ret guess
        }

        # ── 处理 ─────────────────────────────────────────────
        public void scale( Float32 gain )
        {
            Int32 i = 0
            while i < this._samples.length
            {
                this._samples[ i ] = this._samples[ i ] * gain
                i = i + 1
            }
        }

        public void clamp()
        {
            Int32 i = 0
            while i < this._samples.length
            {
                if this._samples[ i ] > 1.0f { this._samples[ i ] = 1.0f }
                if this._samples[ i ] < -1.0f { this._samples[ i ] = -1.0f }
                i = i + 1
            }
        }

        # 归一化到目标峰值（默认 1.0）
        public void normalize( Float32 targetPeak )
        {
            Float32 p = this.peak()
            if p <= 0.0f
            {
                ret
            }
            this.scale( targetPeak / p )
            this.clamp()
        }

        public void normalize()
        {
            this.normalize( 1.0f )
        }

        # 把 other 混合进来（长度取较小值）
        public void mixWith( AudioBuffer other, Float32 gain )
        {
            if other == null
            {
                ret
            }
            Int32 n = this._samples.length
            if other._samples.length < n
            {
                n = other._samples.length
            }
            Int32 i = 0
            while i < n
            {
                this._samples[ i ] = this._samples[ i ] + other._samples[ i ] * gain
                i = i + 1
            }
            this.clamp()
        }

        # 淡入 / 淡出（线性）
        public void fade( Int32 fadeInFrames, Int32 fadeOutFrames )
        {
            Int32 frames = this.frameCount()
            Int32 i = 0
            while i < frames
            {
                Float32 g = 1.0f
                if fadeInFrames > 0 && i < fadeInFrames
                {
                    g = 1.0f * i / fadeInFrames
                }
                if fadeOutFrames > 0 && i >= frames - fadeOutFrames
                {
                    Float32 t = 1.0f * ( frames - i ) / fadeOutFrames
                    if t < g { g = t }
                }
                if g < 0.0f { g = 0.0f }
                Int32 c = 0
                while c < this.channels
                {
                    Int32 idx = i * this.channels + c
                    this._samples[ idx ] = this._samples[ idx ] * g
                    c = c + 1
                }
                i = i + 1
            }
        }

        # ── 重采样（线性插值）──────────────────────────────
        public AudioBuffer resample( Int32 newSampleRate )
        {
            if !this.isValid || newSampleRate <= 0
            {
                ret null
            }
            if newSampleRate == this.sampleRate
            {
                ret this.clone()
            }
            Float32 ratio = 1.0f * this.sampleRate / newSampleRate
            Int32 srcFrames = this.frameCount()
            Int32 dstFrames = SystemConvertInt32( 1.0f * srcFrames * newSampleRate / this.sampleRate )
            if dstFrames <= 0
            {
                ret null
            }
            AudioBuffer dst = AudioBuffer( newSampleRate, this.channels, dstFrames )
            dst.sourceFormat = this.sourceFormat

            Int32 f = 0
            while f < dstFrames
            {
                Float32 srcPos = 1.0f * f * ratio
                Int32 i0 = SystemConvertInt32( srcPos )
                Int32 i1 = i0 + 1
                Float32 w = srcPos - 1.0f * i0
                if i0 < 0 { i0 = 0 }
                if i1 < 0 { i1 = 0 }
                if i0 >= srcFrames { i0 = srcFrames - 1 }
                if i1 >= srcFrames { i1 = srcFrames - 1 }
                Int32 c = 0
                while c < this.channels
                {
                    Float32 a = this._samples[ i0 * this.channels + c ]
                    Float32 b = this._samples[ i1 * this.channels + c ]
                    dst._samples[ f * this.channels + c ] = a + ( b - a ) * w
                    c = c + 1
                }
                f = f + 1
            }
            ret dst
        }

        # ── 声道变换 ─────────────────────────────────────────
        # 多声道 -> 单声道（等功率平均近似：算术平均）
        public AudioBuffer toMono()
        {
            if !this.isValid
            {
                ret null
            }
            if this.channels == 1
            {
                ret this.clone()
            }
            Int32 frames = this.frameCount()
            AudioBuffer dst = AudioBuffer( this.sampleRate, 1, frames )
            dst.sourceFormat = this.sourceFormat
            Int32 f = 0
            while f < frames
            {
                Float32 acc = 0.0f
                Int32 c = 0
                while c < this.channels
                {
                    acc = acc + this._samples[ f * this.channels + c ]
                    c = c + 1
                }
                dst._samples[ f ] = acc / SystemConvertFloat32( this.channels )
                f = f + 1
            }
            ret dst
        }

        # 单声道 -> n 声道（复制）
        public AudioBuffer toChannels( Int32 targetChannels )
        {
            if !this.isValid || targetChannels <= 0
            {
                ret null
            }
            if targetChannels == this.channels
            {
                ret this.clone()
            }
            Int32 frames = this.frameCount()
            AudioBuffer dst = AudioBuffer( this.sampleRate, targetChannels, frames )
            dst.sourceFormat = this.sourceFormat
            Int32 f = 0
            while f < frames
            {
                Float32 v = this._samples[ f * this.channels ]
                Int32 c = 0
                while c < targetChannels
                {
                    dst._samples[ f * targetChannels + c ] = v
                    c = c + 1
                }
                f = f + 1
            }
            ret dst
        }

        # ── 切片 / 反转 / 克隆 ───────────────────────────────
        public AudioBuffer slice( Int32 startFrame, Int32 endFrame )
        {
            if !this.isValid
            {
                ret null
            }
            if startFrame < 0 { startFrame = 0 }
            if endFrame > this.frameCount() { endFrame = this.frameCount() }
            if endFrame <= startFrame
            {
                ret null
            }
            Int32 n = endFrame - startFrame
            AudioBuffer dst = AudioBuffer( this.sampleRate, this.channels, n )
            dst.sourceFormat = this.sourceFormat
            Int32 src = startFrame * this.channels
            Int32 i = 0
            while i < n * this.channels
            {
                dst._samples[ i ] = this._samples[ src + i ]
                i = i + 1
            }
            ret dst
        }

        public void reverse()
        {
            if !this.isValid
            {
                ret
            }
            Int32 frames = this.frameCount()
            Int32 f = 0
            Int32 last = frames - 1
            while f < last
            {
                Int32 a = f * this.channels
                Int32 b = last * this.channels
                Int32 c = 0
                while c < this.channels
                {
                    Float32 tmp = this._samples[ a + c ]
                    this._samples[ a + c ] = this._samples[ b + c ]
                    this._samples[ b + c ] = tmp
                    c = c + 1
                }
                f = f + 1
                last = last - 1
            }
        }

        public AudioBuffer clone()
        {
            AudioBuffer b = AudioBuffer( this.sampleRate, this.channels )
            b.sourceFormat = this.sourceFormat
            b._samples = Array<Float32>( this._samples.length )
            Int32 i = 0
            while i < this._samples.length
            {
                b._samples[ i ] = this._samples[ i ]
                i = i + 1
            }
            ret b
        }

        # ── 后端编解码 ───────────────────────────────────────
        public Array<UInt8> encode( EAudioFormat format, Int32 bitrateKbps )
        {
            object result = SystemCallExternalFunction( "Audio.encode",
                this.sampleRate, this.channels, this._samples, format, bitrateKbps )
            ret result as Array<UInt8>
        }

        public static AudioBuffer decode( Array<UInt8> bytes )
        {
            if bytes == null
            {
                ret null
            }
            object result = SystemCallExternalFunction( "Audio.decode", bytes )
            ret result as AudioBuffer
        }

        override string toString()
        {
            ret "AudioBuffer(" + this.sampleRate.toString() + "Hz, " +
                this.channels.toString() + "ch, " + this.duration().toString() + "s)"
        }
    }
}
