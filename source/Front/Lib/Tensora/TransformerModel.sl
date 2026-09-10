# TransformerModel —— BERT 风格 Transformer 编码器分类模型
#
# 结构（本库 Tensor 为 2D [行, 列]，序列长度占用行维，因此一次 forward = 一条序列）：
#   ids [T,1] -> Embedding -> PositionalEncoding -> N × TransformerBlock
#            -> LayerNorm -> SeqPool -> Dense -> logits [1, C]
#
# 池化方式（EPoolKind）：Mean 整条序列取平均 / Cls 取第 0 个位置
#
# 说明：
#   1. 继承 Module，把子层全部 add 进去，因此 parameters()/gradients()/zeroGrad()/
#      update()/train() 直接复用 Module 的聚合实现，无需重写。
#   2. Trainer 面向 [N, F] 特征矩阵，无法表达"一批变长序列"，因此这里自带
#      fitOnce/fit/evaluate 训练循环；forwardBatch 负责把逐条结果堆成 [N, C]。

enum EPoolKind
{
    Mean = 0
    Cls
}

# ── 序列池化层 ───────────────────────────────────────────
# 前向 [T, d] -> [1, d]；反向 [1, d] -> [T, d]
@Nickname("SeqPool")
public class SeqPool extends Layer
{
    Int32 _kind = 0
    Int32 _seqLen = 0

    public void _init_( Int32 kind )
    {
        this.name = "SeqPool"
        this._kind = kind
    }

    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        int t = x.rows()
        this._seqLen = t

        if this._kind == EPoolKind.Cls
        {
            int d = x.cols()
            Shape outShape = Shape.matrix( 1, d )
            Tensor res = Tensor( outShape )
            for j = 0, j < d, j++
            {
                Float32 v = x.get( 0, j )
                res.set( 0, j, v )
            }
            this.outputCache = res
            ret res
        }

        Tensor colSum = x.sumAlong( 0 )
        Float32 inv = 1.0f / SystemConvertFloat32( t )
        Tensor mean = colSum.scale( inv )
        this.outputCache = mean
        ret mean
    }

    override Tensor backward( Tensor gradOutput )
    {
        int t = this._seqLen

        if this._kind == EPoolKind.Cls
        {
            int d = gradOutput.cols()
            Shape gradShape = Shape.matrix( t, d )
            Tensor gout = Tensor.zeros( gradShape )
            for j = 0, j < d, j++
            {
                Float32 g = gradOutput.get( 0, j )
                gout.set( 0, j, g )
            }
            ret gout
        }

        Shape onesShape = Shape.matrix( t, 1 )
        Tensor ones = Tensor.ones( onesShape )
        Float32 inv = 1.0f / SystemConvertFloat32( t )
        Tensor scaled = gradOutput.scale( inv )
        ret ones.matmul( scaled )
    }

    override string summary()
    {
        ret "SeqPool(kind=" + SystemConvertString( this._kind ) + ")"
    }
}

# ── Transformer 文本分类模型 ─────────────────────────────
@Nickname("TransformerClassifier")
public class TransformerClassifier extends Module
{
    Int32 _vocabSize = 0
    Int32 _classCount = 0
    Int32 _dModel = 0
    Int32 _heads = 0
    Int32 _layers = 0
    Int32 _maxLen = 0

    public Embedding embedding = null
    public PositionalEncoding posEnc = null
    public LayerNorm finalNorm = null
    public SeqPool pool = null
    public Dense head = null

    Array<TransformerBlock> _blocks = null

    # 最简构造：vocab / 类别 + 默认超参
    public void _init_( Int32 vocabSize, Int32 classCount )
    {
        this._init_( vocabSize, classCount, 32, 4, 2, 64, EPoolKind.Mean, 0.1f )
    }

    public void _init_( Int32 vocabSize, Int32 classCount, Int32 dModel, Int32 heads, Int32 layerCount, Int32 maxLen, Int32 poolKind, Float32 dropRate )
    {
        this.name = "TransformerClassifier"
        this.ensureLayers()
        this._vocabSize = vocabSize
        this._classCount = classCount
        this._dModel = dModel
        this._heads = heads
        this._layers = layerCount
        this._maxLen = maxLen

        this.embedding = Embedding( vocabSize, dModel )
        this.posEnc = PositionalEncoding( maxLen, dModel )
        this.add( this.embedding )
        this.add( this.posEnc )

        this._blocks = Array<TransformerBlock>( layerCount )
        for i = 0, i < layerCount, i++
        {
            this._blocks[i] = TransformerBlock( dModel, heads, 4 * dModel, dropRate )
            this.add( this._blocks[i] )
        }

        this.finalNorm = LayerNorm( dModel )
        this.pool = SeqPool( poolKind )
        this.head = Dense( dModel, classCount )
        this.add( this.finalNorm )
        this.add( this.pool )
        this.add( this.head )
    }

    public Array<TransformerBlock> blocks()
    {
        ret this._blocks
    }

    # ── 前向 / 反向 ────────────────────────────────────────
    # ids: [T] 或 [T,1] 的 token id -> logits [1, C]
    override Tensor forward( Tensor ids )
    {
        Tensor x = this.embedding.forward( ids )
        x = this.posEnc.forward( x )
        for i = 0, i < this._layers, i++
        {
            x = this._blocks[i].forward( x )
        }
        x = this.finalNorm.forward( x )
        x = this.pool.forward( x )
        Tensor logits = this.head.forward( x )
        ret logits
    }

    override Tensor backward( Tensor gradOutput )
    {
        Tensor g = this.head.backward( gradOutput )
        g = this.pool.backward( g )
        g = this.finalNorm.backward( g )
        for i = this._layers - 1, i >= 0, i--
        {
            g = this._blocks[i].backward( g )
        }
        g = this.posEnc.backward( g )
        g = this.embedding.backward( g )
        ret g
    }

    # ── 批量推理：逐条前向再堆成 [N, C] ────────────────────
    public Tensor forwardBatch( Array<Tensor> seqs )
    {
        Array<Tensor> outs = Array<Tensor>( seqs.length )
        for i = 0, i < seqs.length, i++
        {
            outs[i] = this.forward( seqs[i] )
        }
        Tensor stacked = Ops.stack( outs )
        ret stacked
    }

    # ── 推理接口 ──────────────────────────────────────────
    public Int32 predictLabel( Tensor ids )
    {
        Tensor logits = this.forward( ids )
        ret logits.argmax()
    }

    public Tensor predictProba( Tensor ids )
    {
        Tensor logits = this.forward( ids )
        ret logits.softmax()
    }

    public Array<Int32> predictLabels( Array<Tensor> seqs )
    {
        this.train( false )
        Tensor logits = this.forwardBatch( seqs )
        Array<Int32> r = Metrics.predictLabels( logits )
        this.train( true )
        ret r
    }

    # ── 训练：本库无 batch 维，按"整批累加梯度再更新"的方式跑 ──
    # 返回本轮平均交叉熵
    public Float32 fitOnce( Array<Tensor> seqs, Array<Int32> labels, Optimizer opt )
    {
        this.train( true )
        this.zeroGrad()
        Array<Int32> oneLabel = Array<Int32>( 1 )
        Float32 lossSum = 0.0f
        for i = 0, i < seqs.length, i++
        {
            Tensor logits = this.forward( seqs[i] )
            oneLabel[0] = labels[i]
            lossSum = lossSum + Loss.crossEntropy( logits, oneLabel )
            Tensor g = Loss.crossEntropyGrad( logits, oneLabel )
            this.backward( g )
        }
        opt.step( this.parameters(), this.gradients() )
        ret lossSum / SystemConvertFloat32( seqs.length )
    }

    public void fit( Array<Tensor> seqs, Array<Int32> labels, Optimizer opt, Int32 epochs, Int32 logEvery )
    {
        for e = 0, e < epochs, e++
        {
            Float32 loss = this.fitOnce( seqs, labels, opt )
            if logEvery > 0
            {
                if e % logEvery == 0
                {
                    Float32 acc = this.evaluate( seqs, labels )
                    SystemPrintln( "epoch " + SystemConvertString( e ) + " loss=" + SystemConvertString( loss ) + " acc=" + SystemConvertString( acc ) )
                }
            }
        }
        this.train( false )
    }

    public Float32 evaluate( Array<Tensor> seqs, Array<Int32> labels )
    {
        Array<Int32> pred = this.predictLabels( seqs )
        int hit = 0
        for i = 0, i < pred.length, i++
        {
            if pred[i] == labels[i]
            {
                hit++
            }
        }
        ret SystemConvertFloat32( hit ) / SystemConvertFloat32( pred.length )
    }

    # TransformerBlock 未重写 paramCount，这里按 parameters() 实算
    public get int totalParams()
    {
        Array<Tensor> ps = this.parameters()
        int n = 0
        for i = 0, i < ps.length, i++
        {
            n = n + ps[i].size()
        }
        ret n
    }

    override string summary()
    {
        ret "TransformerClassifier(vocab=" + SystemConvertString( this._vocabSize ) + ", d=" + SystemConvertString( this._dModel ) + ", h=" + SystemConvertString( this._heads ) + ", L=" + SystemConvertString( this._layers ) + ", cls=" + SystemConvertString( this._classCount ) + ", params=" + SystemConvertString( this.totalParams() ) + ")"
    }
}
