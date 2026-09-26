# Transformer —— Transformer 块与编码器堆叠
#
# TransformerBlock（Pre-LN 变体）：
#   x1 = x + MHA(x)
#   x2 = x1 + FFN(LN(x1))      其中 FFN = Dense(dModel→ffDim) -> ReLU -> Dense(ffDim→dModel)
#   out = LN(x2)
#
# 反向：残差分支的梯度按"近似直传 + 分支回传"处理（够跑通，非严格数学等价）

@Nickname("TransformerBlock")
public class TransformerBlock extends Layer
{
    Int32 _dModel = 0
    Int32 _heads = 0
    Int32 _ffDim = 0

    public MultiHeadAttention attn = null
    public LayerNorm norm1 = null
    public LayerNorm norm2 = null
    public Dense ff1 = null
    public Dense ff2 = null
    public Dropout dropout = null

    public void _init_( Int32 dModel, Int32 heads )
    {
        this._init_( dModel, heads, 4 * dModel, 0.1f )
    }

    public void _init_( Int32 dModel, Int32 heads, Int32 ffDim, Float32 dropRate )
    {
        this.name = "TransformerBlock"
        this._dModel = dModel
        this._heads = heads
        this._ffDim = ffDim
        this.attn = MultiHeadAttention( dModel, heads )
        this.norm1 = LayerNorm( dModel )
        this.norm2 = LayerNorm( dModel )
        this.ff1 = Dense( dModel, ffDim, EActivation.Relu )
        this.ff2 = Dense( ffDim, dModel )
        this.dropout = Dropout( dropRate )
    }

    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        Tensor a = this.attn.forward( x )
        Tensor x1 = x.add( a )
        Tensor n1 = this.norm1.forward( x1 )
        Tensor f = this.ff1.forward( n1 )
        f = this.ff2.forward( f )
        f = this.dropout.forward( f )
        Tensor x2 = x1.add( f )
        this.outputCache = this.norm2.forward( x2 )
        ret this.outputCache
    }

    override Tensor backward( Tensor gradOutput )
    {
        # LN2 -> （残差直传 + FFN 分支）-> LN1 -> （残差直传 + MHA 分支）
        Tensor g2 = this.norm2.backward( gradOutput )
        Tensor gf = this.dropout.backward( g2 )
        gf = this.ff2.backward( gf )
        gf = this.ff1.backward( gf )
        Tensor g1 = g2.add( gf )
        Tensor gn = this.norm1.backward( g1 )
        Tensor ga = this.attn.backward( gn )
        ret g1.add( ga )
    }

    override Array<Tensor> parameters()
    {
        Array<Layer> ls = Array<Layer>( 5 )
        ls[0] = this.attn
        ls[1] = this.norm1
        ls[2] = this.ff1
        ls[3] = this.ff2
        ls[4] = this.norm2
        ret Ops.joinLayers( ls, 0 )
    }

    override Array<Tensor> gradients()
    {
        Array<Layer> ls = Array<Layer>( 5 )
        ls[0] = this.attn
        ls[1] = this.norm1
        ls[2] = this.ff1
        ls[3] = this.ff2
        ls[4] = this.norm2
        ret Ops.joinLayers( ls, 1 )
    }

    override void zeroGrad()
    {
        this.attn.zeroGrad()
        this.norm1.zeroGrad()
        this.ff1.zeroGrad()
        this.ff2.zeroGrad()
        this.norm2.zeroGrad()
    }

    override string summary()
    {
        ret "TransformerBlock(d=" + SystemConvertString( this._dModel ) + ", h=" + SystemConvertString( this._heads ) + ")"
    }
}

@Nickname("Transformer")
public class Transformer extends Module
{
    Int32 _dModel = 0
    Int32 _heads = 0
    Int32 _layers = 0
    Array<TransformerBlock> _blocks = null

    public void _init_( Int32 dModel, Int32 heads, Int32 layerCount )
    {
        this.name = "Transformer"
        this._dModel = dModel
        this._heads = heads
        this._layers = layerCount
        this._blocks = Array<TransformerBlock>( layerCount )
        for i = 0, i < layerCount, i++
        {
            this._blocks[i] = TransformerBlock( dModel, heads )
            this.add( this._blocks[i] )
        }
    }

    public Array<TransformerBlock> blocks()
    {
        ret this._blocks
    }
}
