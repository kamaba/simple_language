# Attention —— 注意力机制
#
# Attention.scaledDotProduct：softmax(Q·Kᵀ / √d) · V
# MultiHeadAttention      ：多头自注意力/交叉注意力（Q/K/V 投影 + 输出投影）
# PositionalEncoding      ：sin/cos 位置编码（无参数，梯度直传）

@Nickname("Attention")
public class Attention
{
    # q/k/v 形状均为 [N, d]
    public static Tensor scaledDotProduct( Tensor q, Tensor k, Tensor v )
    {
        ret Attention.scaledDotProduct( q, k, v, null )
    }

    # mask: [N, N]，1 表示保留，0 表示屏蔽（内部转成 -1e9）
    public static Tensor scaledDotProduct( Tensor q, Tensor k, Tensor v, Tensor mask )
    {
        Float32 d = SystemConvertFloat32( q.cols() )
        Tensor scores = q.matmul( k.transpose() ).scale( 1.0f / Mathf.sqrt( d ) )
        if mask != null
        {
            for i = 0, i < scores.rows(), i++
            {
                for j = 0, j < scores.cols(), j++
                {
                    if mask.get( i, j ) < 0.5f
                    {
                        scores.set( i, j, 0.0f - 1000000000.0f )
                    }
                }
            }
        }
        Tensor attn = scores.softmax()
        ret attn.matmul( v )
    }

    # 只算注意力权重（可视化 / 分析用）
    public static Tensor weights( Tensor q, Tensor k )
    {
        Float32 d = SystemConvertFloat32( q.cols() )
        ret q.matmul( k.transpose() ).scale( 1.0f / Mathf.sqrt( d ) ).softmax()
    }

    # 因果掩码：下三角为 1，上三角（未来）为 0
    public static Tensor causalMask( Int32 n )
    {
        Tensor m = Tensor( Shape.matrix( n, n ) )
        for i = 0, i < n, i++
        {
            for j = 0, j < n, j++
            {
                if j <= i
                {
                    m.set( i, j, 1.0f )
                }
                else
                {
                    m.set( i, j, 0.0f )
                }
            }
        }
        ret m
    }
}

@Nickname("MHA")
public class MultiHeadAttention extends Layer
{
    Int32 _dModel = 0
    Int32 _heads = 0
    Int32 _headDim = 0

    public Dense qProj = null
    public Dense kProj = null
    public Dense vProj = null
    public Dense outProj = null

    public Tensor attnWeights = null

    public void _init_( Int32 dModel, Int32 heads )
    {
        this.name = "MultiHeadAttention"
        this._dModel = dModel
        this._heads = heads
        this._headDim = dModel / heads
        this.qProj = Dense( dModel, dModel )
        this.kProj = Dense( dModel, dModel )
        this.vProj = Dense( dModel, dModel )
        this.outProj = Dense( dModel, dModel )
    }

    # 参数与梯度由四个投影层聚合而来（不重复登记，避免梯度被覆盖）
    override Array<Tensor> parameters()
    {
        Array<Tensor> a = this.qProj.parameters()
        Array<Tensor> b = this.kProj.parameters()
        Array<Tensor> c = this.vProj.parameters()
        Array<Tensor> d = this.outProj.parameters()
        ret Ops.join4( a, b, c, d )
    }

    override Array<Tensor> gradients()
    {
        Array<Tensor> a = this.qProj.gradients()
        Array<Tensor> b = this.kProj.gradients()
        Array<Tensor> c = this.vProj.gradients()
        Array<Tensor> d = this.outProj.gradients()
        ret Ops.join4( a, b, c, d )
    }

    override void zeroGrad()
    {
        this.qProj.zeroGrad()
        this.kProj.zeroGrad()
        this.vProj.zeroGrad()
        this.outProj.zeroGrad()
    }

    public get int totalParams()
    {
        ret this.qProj.paramCount() + this.kProj.paramCount() + this.vProj.paramCount() + this.outProj.paramCount()
    }

    # 取列区间 [start, start+count)
    Tensor sliceCols( Tensor m, Int32 start, Int32 count )
    {
        Tensor r = Tensor( Shape.matrix( m.rows(), count ) )
        for i = 0, i < m.rows(), i++
        {
            for j = 0, j < count, j++
            {
                r.set( i, j, m.get( i, start + j ) )
            }
        }
        ret r
    }

    # 自注意力
    override Tensor forward( Tensor x )
    {
        ret this.forward( x, x, x, null )
    }

    # q/k/v 形状 [N, dModel]
    Tensor forward( Tensor q, Tensor k, Tensor v, Tensor mask )
    {
        Tensor Q = this.qProj.forward( q )
        Tensor K = this.kProj.forward( k )
        Tensor V = this.vProj.forward( v )
        Tensor ctx = Tensor( Shape.matrix( q.rows(), this._dModel ) )
        for h = 0, h < this._heads, h++
        {
            int off = h * this._headDim
            Tensor qh = this.sliceCols( Q, off, this._headDim )
            Tensor kh = this.sliceCols( K, off, this._headDim )
            Tensor vh = this.sliceCols( V, off, this._headDim )
            Float32 scale = 1.0f / Mathf.sqrt( SystemConvertFloat32( this._headDim ) )
            Tensor scores = qh.matmul( kh.transpose() ).scale( scale )
            if mask != null
            {
                for i = 0, i < scores.rows(), i++
                {
                    for j = 0, j < scores.cols(), j++
                    {
                        if mask.get( i, j ) < 0.5f
                        {
                            scores.set( i, j, 0.0f - 1000000000.0f )
                        }
                    }
                }
            }
            Tensor attn = scores.softmax()
            this.attnWeights = attn
            Tensor out = attn.matmul( vh )
            for i = 0, i < out.rows(), i++
            {
                for j = 0, j < this._headDim, j++
                {
                    ctx.set( i, off + j, out.get( i, j ) )
                }
            }
        }
        this.outputCache = this.outProj.forward( ctx )
        ret this.outputCache
    }

    override Tensor backward( Tensor gradOutput )
    {
        # 近似：先过输出投影，再把梯度按 1/3 分给 Q/K/V 三条分支
        Tensor g = this.outProj.backward( gradOutput )
        Tensor gq = this.qProj.backward( g.scale( 0.3333333f ) )
        Tensor gk = this.kProj.backward( g.scale( 0.3333333f ) )
        Tensor gv = this.vProj.backward( g.scale( 0.3333333f ) )
        ret gq.add( gk ).add( gv )
    }

    override string summary()
    {
        ret "MHA(d=" + SystemConvertString( this._dModel ) + ", h=" + SystemConvertString( this._heads ) + ")"
    }
}

@Nickname("PosEnc")
public class PositionalEncoding extends Layer
{
    Int32 _maxLen = 0
    Int32 _dModel = 0
    public Tensor table = null

    public void _init_( Int32 maxLen, Int32 dModel )
    {
        this.name = "PositionalEncoding"
        this._maxLen = maxLen
        this._dModel = dModel
        this.build()
    }

    void build()
    {
        Tensor t = Tensor( Shape.matrix( this._maxLen, this._dModel ) )
        for pos = 0, pos < this._maxLen, pos++
        {
            for i = 0, i < this._dModel, i++
            {
                Float32 angle = SystemConvertFloat32( pos ) / Mathf.pow( 10000.0f, SystemConvertFloat32( ( i / 2 ) * 2 ) / SystemConvertFloat32( this._dModel ) )
                if i % 2 == 0
                {
                    t.set( pos, i, Mathf.sin( angle ) )
                }
                else
                {
                    t.set( pos, i, Mathf.cos( angle ) )
                }
            }
        }
        this.table = t
    }

    # x: [T, dModel]
    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        Tensor out = Tensor( x.shape() )
        for i = 0, i < x.rows(), i++
        {
            for j = 0, j < x.cols(), j++
            {
                out.set( i, j, x.get( i, j ) + this.table.get( i, j ) )
            }
        }
        this.outputCache = out
        ret out
    }

    override Tensor backward( Tensor gradOutput )
    {
        # 无参数，梯度直传
        ret gradOutput
    }

    override string summary()
    {
        ret "PositionalEncoding(" + SystemConvertString( this._maxLen ) + ")"
    }
}
