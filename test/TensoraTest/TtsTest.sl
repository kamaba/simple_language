import Tensora

# TtsTest —— 极简 TTS（文本 -> 声学特征 -> 波形）示例
#
# 参考 Wav2Vec2 的结构思路（只取骨架，规模极小）：
#   Wav2Vec2  = Conv1d 特征编码栈 + GELU + Transformer 上下文网络（+ 量化/对比，这里省略）
#   FastSpeech= 时长预测 + Length Regulator + Mel 解码
#   声码器     = 谐波叠加（替代 HiFi-GAN / Griffin-Lim，只为演示能出波形）
#
# 流程：文本 -> 字符 id -> Embedding -> [Conv1d×2 + GELU] -> TransformerBlock
#        -> 时长预测 -> 长度扩展 -> Mel 解码 -> 谐波声码器 -> 波形

# ── 1D 卷积（Wav2Vec2 特征编码器就是 1D 卷积栈）──────────
public class Conv1d
{
    public Int32 inChannels = 0
    public Int32 outChannels = 0
    public Int32 kernel = 3
    public Int32 stride = 1

    public Tensor weight = null   # [outChannels, inChannels * kernel]
    public Tensor bias = null     # [1, outChannels]

    public void _init_( Int32 inChannels, Int32 outChannels, Int32 kernel, Int32 stride )
    {
        this.inChannels = inChannels
        this.outChannels = outChannels
        this.kernel = kernel
        this.stride = stride
        this.weight = Init.xavierUniform( inChannels * kernel, outChannels )
        this.bias = Tensor( Shape.matrix( 1, outChannels ) )
    }

    # x: [T, inChannels] -> [T', outChannels]，T' = (T - kernel) / stride + 1
    public Tensor forward( Tensor x )
    {
        int t = x.rows()
        int outT = ( t - this.kernel ) / this.stride + 1
        Tensor y = Tensor( Shape.matrix( outT, this.outChannels ) )
        int winSize = this.inChannels * this.kernel
        for i = 0, i < outT, i++
        {
            Tensor win = Tensor( Shape.matrix( 1, winSize ) )
            int c2 = 0
            for p = 0, p < this.kernel, p++
            {
                for c = 0, c < this.inChannels, c++
                {
                    win.set( 0, c2, x.get( i * this.stride + p, c ) )
                    c2++
                }
            }
            Tensor z = win.matmul( this.weight.transpose() ).add( this.bias )
            for o = 0, o < this.outChannels, o++
            {
                y.set( i, o, z.get( 0, o ) )
            }
        }
        ret y
    }
}

# ── Wav2Vec2 风格编码器 ─────────────────────────────────
public class Wav2VecLikeEncoder
{
    public Array<Conv1d> convs = null
    public PositionalEncoding pos = null
    public TransformerBlock context = null
    public Int32 dim = 0
    public Int32 maxLen = 128

    public void _init_( Int32 dim, Int32 convLayers )
    {
        this.dim = dim
        this.convs = Array<Conv1d>( convLayers )
        for i = 0, i < convLayers, i++
        {
            this.convs[i] = Conv1d( dim, dim, 3, 1 )
        }
        this.pos = PositionalEncoding( this.maxLen, dim )
        this.context = TransformerBlock( dim, 4 )
    }

    # x: [T, dim] -> [T - 2*convLayers, dim]
    public Tensor forward( Tensor x )
    {
        Tensor h = x
        for i = 0, i < this.convs.length, i++
        {
            h = this.convs[i].forward( h )
            h = Activation.gelu( h )
        }
        h = this.pos.forward( h )
        ret this.context.forward( h )
    }
}

# ── 文本前端 ───────────────────────────────────────────
public class TtsFrontend
{
    public Tokenizer tokenizer = null
    public Int32 maxLen = 24

    public void _init_()
    {
        this.tokenizer = Tokenizer()
        this.maxLen = 24
    }

    public void build( Array<string> texts )
    {
        this.tokenizer.buildChars( texts )
    }

    public Array<Int32> encode( string text )
    {
        ret this.tokenizer.encodeWithSpecial( text )
    }

    public Array<Int32> encodePadded( string text )
    {
        ret this.tokenizer.pad( this.tokenizer.truncate( this.encode( text ), this.maxLen ), this.maxLen )
    }

    public string decode( Array<Int32> ids )
    {
        ret this.tokenizer.decode( ids )
    }

    # id 序列 -> [T,1] 浮点张量（Embedding 查表用）
    public Tensor toTensor( Array<Int32> ids )
    {
        Tensor t = Tensor( Shape.matrix( ids.length, 1 ) )
        for i = 0, i < ids.length, i++
        {
            t.set( i, 0, SystemConvertFloat32( ids[i] ) )
        }
        ret t
    }

    public get int vocabSize()
    {
        ret this.tokenizer.vocabSize()
    }
}

# ── 时长预测器 + 长度调节器（FastSpeech 风格）────────────
public class DurationPredictor
{
    public Dense fc1 = null
    public Dense fc2 = null

    public void _init_( Int32 dim )
    {
        this.fc1 = Dense( dim, dim, EActivation.Relu )
        this.fc2 = Dense( dim, 1 )
    }

    public Array<Int32> predict( Tensor hidden )
    {
        Tensor d = this.fc2.forward( this.fc1.forward( hidden ) )
        Array<Int32> res = Array<Int32>( d.rows() )
        for i = 0, i < d.rows(), i++
        {
            Int32 n = SystemConvertInt32( 1.0f + TensorMath.abs( d.get( i, 0 ) ) * 2.0f )
            if n < 1
            {
                n = 1
            }
            res[i] = n
        }
        ret res
    }
}

public class LengthRegulator
{
    # hidden: [T, dim]，durations: 每个字符/音素持续几帧
    public Tensor forward( Tensor hidden, Array<Int32> durations )
    {
        int total = 0
        for i = 0, i < durations.length, i++
        {
            total = total + durations[i]
        }
        int dim = hidden.cols()
        Tensor out = Tensor( Shape.matrix( total, dim ) )
        int r = 0
        for i = 0, i < durations.length, i++
        {
            for k = 0, k < durations[i], k++
            {
                for j = 0, j < dim, j++
                {
                    out.set( r, j, hidden.get( i, j ) )
                }
                r++
            }
        }
        ret out
    }
}

# ── Mel 解码器 ────────────────────────────────────────
public class MelDecoder
{
    public Dense fc = null
    public Int32 nMels = 0

    public void _init_( Int32 dim, Int32 nMels )
    {
        this.nMels = nMels
        this.fc = Dense( dim, nMels )
    }

    public Tensor forward( Tensor x )
    {
        ret this.fc.forward( x )
    }
}

# ── 极简声码器：谐波叠加 ───────────────────────────────
public class SimpleVocoder
{
    public Int32 sampleRate = 16000
    public Int32 hopSize = 128
    public Int32 nHarmonics = 6
    public Float32 f0 = 120.0f

    # mel: [T, nMels] -> 波形 [T * hopSize]
    public Tensor forward( Tensor mel )
    {
        int frames = mel.rows()
        int nMels = mel.cols()
        int n = frames * this.hopSize
        Tensor wav = Tensor( Shape.vector( n ) )

        for i = 0, i < frames, i++
        {
            for k = 0, k < this.hopSize, k++
            {
                int idx = i * this.hopSize + k
                Float32 t = SystemConvertFloat32( idx ) / SystemConvertFloat32( this.sampleRate )
                Float32 v = 0.0f
                for h = 1, h <= this.nHarmonics, h++
                {
                    Float32 amp = mel.get( i, ( h - 1 ) % nMels ) / SystemConvertFloat32( h )
                    v = v + amp * Mathf.sin( 6.283185307f * this.f0 * SystemConvertFloat32( h ) * t )
                }
                # 帧边界做淡入淡出，避免拼接爆音
                Float32 wnd = SystemConvertFloat32( k ) / SystemConvertFloat32( this.hopSize )
                Float32 fade = 0.5f - 0.5f * Mathf.cos( 6.283185307f * wnd )
                wav.set( idx, v * fade )
            }
        }
        ret wav
    }
}

# ── 组装成完整 TTS ────────────────────────────────────
public class TtsModel
{
    public TtsFrontend frontend = null
    public Embedding embedding = null
    public Wav2VecLikeEncoder encoder = null
    public DurationPredictor dur = null
    public LengthRegulator reg = null
    public MelDecoder decoder = null
    public SimpleVocoder vocoder = null

    public Int32 dim = 0
    public Int32 nMels = 0

    public void _init_( Int32 dim, Int32 nMels )
    {
        this.dim = dim
        this.nMels = nMels
        this.frontend = TtsFrontend()
        this.encoder = Wav2VecLikeEncoder( dim, 2 )
        this.dur = DurationPredictor( dim )
        this.reg = LengthRegulator()
        this.decoder = MelDecoder( dim, nMels )
        this.vocoder = SimpleVocoder()
        # 词表建好之后才能建 Embedding，见 buildVocab()
        this.embedding = null
    }

    public void buildVocab( Array<string> texts )
    {
        this.frontend.build( texts )
        this.embedding = Embedding( this.frontend.vocabSize(), this.dim )
    }

    # 文本 -> mel [T, nMels]
    public Tensor forward( string text )
    {
        Array<Int32> ids = this.frontend.encode( text )
        Tensor x = this.frontend.toTensor( ids )
        Tensor e = this.embedding.forward( x )
        Tensor h = this.encoder.forward( e )
        Array<Int32> d = this.dur.predict( h )
        Tensor expanded = this.reg.forward( h, d )
        ret this.decoder.forward( expanded )
    }

    # 文本 -> 波形
    public Tensor synthesize( string text )
    {
        ret this.vocoder.forward( this.forward( text ) )
    }
}

TtsTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[TtsTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[TtsTest] " + name + " : FAIL" )
        }
    }

    # ── 1. 文本前端 ─────────────────────────────────────
    static frontendTest()
    {
        global.println( "===== frontendTest =====" )
        Array<string> corpus = [ "hello world", "simple tts" ]
        TtsFrontend fe = TtsFrontend()
        fe.build( corpus )
        global.println( "vocab size = " + fe.vocabSize().toString() )

        Array<Int32> ids = fe.encode( "hello" )
        check( "encode len == 7 (含 bos/eos)", ids.length == 7 )
        global.println( "decode -> " + fe.decode( ids ) )

        Array<Int32> padded = fe.encodePadded( "hello" )
        check( "padded len == maxLen", padded.length == fe.maxLen )

        Tensor t = fe.toTensor( ids )
        check( "id tensor == (7,1)", t.rows() == 7 && t.cols() == 1 )
    }

    # ── 2. Conv1d + Wav2Vec2 风格编码器 ─────────────────
    static encoderTest()
    {
        global.println( "===== encoderTest =====" )
        Tensor x = Tensor.randn( Shape.matrix( 10, 16 ) )

        Conv1d conv = Conv1d( 16, 16, 3, 1 )
        Tensor y = conv.forward( x )
        check( "conv1d out == (8,16)", y.rows() == 8 && y.cols() == 16 )

        Wav2VecLikeEncoder enc = Wav2VecLikeEncoder( 16, 2 )
        Tensor h = enc.forward( x )
        # 两层 k=3,s=1 -> 10 - 2 - 2 = 6
        check( "encoder out == (6,16)", h.rows() == 6 && h.cols() == 16 )
        global.println( "encoder out mean ~= " + SystemConvertString( h.mean() ) )
    }

    # ── 3. 时长预测 + 长度调节 ──────────────────────────
    static durationTest()
    {
        global.println( "===== durationTest =====" )
        Tensor hidden = Tensor.randn( Shape.matrix( 6, 16 ) )

        DurationPredictor dp = DurationPredictor( 16 )
        Array<Int32> d = dp.predict( hidden )
        check( "duration len == 6", d.length == 6 )
        int total = 0
        for i = 0, i < d.length, i++
        {
            total = total + d[i]
        }
        global.println( "durations total = " + total.toString() )

        LengthRegulator lr = LengthRegulator()
        Tensor out = lr.forward( hidden, d )
        check( "regulated rows == total", out.rows() == total )
        check( "regulated cols == 16", out.cols() == 16 )
    }

    # ── 4. Mel 解码 + 声码器 ───────────────────────────
    static vocoderTest()
    {
        global.println( "===== vocoderTest =====" )
        Tensor hidden = Tensor.randn( Shape.matrix( 8, 16 ) )

        MelDecoder dec = MelDecoder( 16, 8 )
        Tensor mel = dec.forward( hidden )
        check( "mel == (8,8)", mel.rows() == 8 && mel.cols() == 8 )

        SimpleVocoder voc = SimpleVocoder()
        Tensor wav = voc.forward( mel )
        check( "wav len == 8 * hop", wav.size() == 8 * voc.hopSize )
        global.println( "wav samples = " + wav.size().toString() )
        global.println( "wav rms ~= " + SystemConvertString( Mathf.sqrt( wav.mul( wav ).mean() ) ) )
        check( "wav 非全零", wav.abs().sum() > 0.0f )
    }

    # ── 5. 端到端：文本 -> mel -> 波形 ──────────────────
    static pipelineTest()
    {
        global.println( "===== pipelineTest =====" )
        Array<string> corpus = [ "hello world", "simple tts", "a b c" ]

        TtsModel tts = TtsModel( 16, 8 )
        tts.buildVocab( corpus )

        Tensor mel = tts.forward( "hello" )
        global.println( "mel shape = " + mel.shape().toString() )
        check( "mel cols == nMels", mel.cols() == 8 )
        check( "mel rows > 0", mel.rows() > 0 )

        Tensor wav = tts.synthesize( "hello" )
        global.println( "wav samples = " + wav.size().toString() + " (~" + SystemConvertString( SystemConvertFloat32( wav.size() ) / 16000.0f ) + " s)" )
        check( "wav == mel.rows * hop", wav.size() == mel.rows() * tts.vocoder.hopSize )

        # 换一句再跑一次，确认形状随文本长度变化
        Tensor mel2 = tts.forward( "simple tts" )
        global.println( "mel2 shape = " + mel2.shape().toString() )
        check( "mel2 rows != mel rows", mel2.rows() != mel.rows() )
    }

    static fun()
    {
        global.println( "========== TtsTest (start) ==========" )
        frontendTest()
        encoderTest()
        durationTest()
        vocoderTest()
        pipelineTest()
        global.println( "========== TtsTest (end) ==========" )
    }
}
