# Dense —— 全连接层（线性变换 + 可选激活）
#
# 权重形状：[outFeatures, inFeatures]，forward = x · Wᵀ + b
# 梯度：
#   dW = gradOutputᵀ · x
#   db = Σ gradOutput（按列求和）
#   dx = gradOutput · W

@Nickname("Linear")
public class Dense extends Layer
{
    Int32 _inFeatures = 0
    Int32 _outFeatures = 0
    Int32 _activation = 0

    public Tensor weight = null
    public Tensor bias = null
    public Tensor preActivation = null

    public void _init_( Int32 inFeatures, Int32 outFeatures )
    {
        this._init_( inFeatures, outFeatures, EActivation.None )
    }

    public void _init_( Int32 inFeatures, Int32 outFeatures, Int32 activationKind )
    {
        this.name = "Dense"
        this._inFeatures = inFeatures
        this._outFeatures = outFeatures
        this._activation = activationKind
        this.weight = Init.xavierUniform( inFeatures, outFeatures )
        this.bias = Tensor( Shape.matrix( 1, outFeatures ) )
        this.addParam( this.weight )
        this.addParam( this.bias )
    }

    get int inFeatures()
    {
        ret this._inFeatures
    }

    get int outFeatures()
    {
        ret this._outFeatures
    }

    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        Tensor wt = this.weight.transpose()
        Tensor z = x.matmul( wt ).add( this.bias )
        this.preActivation = z
        Tensor out = Activation.apply( z, this._activation )
        this.outputCache = out
        ret out
    }

    override Tensor backward( Tensor gradOutput )
    {
        Tensor g = gradOutput
        if this._activation == EActivation.Relu
        {
            g = Activation.reluGrad( this.preActivation, g )
        }
        elif this._activation != EActivation.None
        {
            g = Activation.applyGrad( this._activation, this.outputCache, g )
        }

        # 参数梯度（累加，便于累计多个 batch 后统一更新）
        Tensor gw = g.transpose().matmul( this.inputCache )
        Tensor gb = g.sumAlong( 0 )
        Array<Tensor> gs = this.gradients()
        gs[0].addInPlace( gw )
        gs[1].addInPlace( gb )

        # 传给下一层的梯度
        ret g.matmul( this.weight )
    }

    override void update( Float32 lr )
    {
        Array<Tensor> ps = this.parameters()
        Array<Tensor> gs = this.gradients()
        for i = 0, i < ps.length, i++
        {
            ps[i].subInPlace( gs[i].scale( lr ) )
        }
    }

    override string summary()
    {
        ret "Dense(" + SystemConvertString( this._inFeatures ) + "->" + SystemConvertString( this._outFeatures ) + ")"
    }
}
