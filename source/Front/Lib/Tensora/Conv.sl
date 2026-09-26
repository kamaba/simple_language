# Conv2D —— 二维卷积层
#
# 约定：
#   输入   x     : [C, H, W]
#   权重   weight: [outChannels, C, k, k]
#   偏置   bias  : [1, outChannels]
#   输出   out   : [1, outH, outW] 每个输出通道一张特征图，再按通道堆叠成 [outC, outH, outW]
#
# 反向为简化实现：
#   dWeight[o] = 输入块与 gradOutput[o] 的相关
#   dx          = 用旋转 180° 的核做"全卷积"近似（教学版，够跑通）

@Nickname("Conv2d")
public class Conv2D extends Layer
{
    Int32 _inChannels = 0
    Int32 _outChannels = 0
    Int32 _kernel = 3
    Int32 _stride = 1
    Int32 _padding = 0
    Int32 _activation = 0

    public Tensor weight = null
    public Tensor bias = null
    public Tensor preActivation = null

    public void _init_( Int32 inChannels, Int32 outChannels, Int32 kernelSize )
    {
        this._init_( inChannels, outChannels, kernelSize, 1, 0, EActivation.None )
    }

    public void _init_( Int32 inChannels, Int32 outChannels, Int32 kernelSize, Int32 stride, Int32 padding, Int32 activationKind )
    {
        this.name = "Conv2D"
        this._inChannels = inChannels
        this._outChannels = outChannels
        this._kernel = kernelSize
        this._stride = stride
        this._padding = padding
        this._activation = activationKind

        Array<Int32> dims = Array<Int32>( 4 )
        dims[0] = outChannels
        dims[1] = inChannels
        dims[2] = kernelSize
        dims[3] = kernelSize
        this.weight = Tensor( Shape( dims ) )
        Init.fillHe( this.weight )
        this.bias = Tensor( Shape.matrix( 1, outChannels ) )
        this.addParam( this.weight )
        this.addParam( this.bias )
    }

    # 取出第 o 个输出通道的卷积核 [C, k, k]
    Tensor kernelOf( Int32 o )
    {
        Tensor k = Tensor( Shape.cube( this._inChannels, this._kernel, this._kernel ) )
        for c = 0, c < this._inChannels, c++
        {
            for ky = 0, ky < this._kernel, ky++
            {
                for kx = 0, kx < this._kernel, kx++
                {
                    k.set( c, ky, kx, this.weight.get( o, c, ky, kx ) )
                }
            }
        }
        ret k
    }

    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        int c = x.shape().dims[0]
        int h = x.shape().dims[1]
        int w = x.shape().dims[2]
        int outH = ( h + 2 * this._padding - this._kernel ) / this._stride + 1
        int outW = ( w + 2 * this._padding - this._kernel ) / this._stride + 1
        Tensor out = Tensor( Shape.cube( this._outChannels, outH, outW ) )
        for o = 0, o < this._outChannels, o++
        {
            Tensor k = this.kernelOf( o )
            Tensor fm = Ops.conv2d( x, k, this._stride, this._padding )
            for oy = 0, oy < outH, oy++
            {
                for ox = 0, ox < outW, ox++
                {
                    out.set( o, oy, ox, fm.get( 0, oy, ox ) + this.bias.get( 0, o ) )
                }
            }
        }
        this.preActivation = out
        this.outputCache = Activation.apply( out, this._activation )
        ret this.outputCache
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

        Tensor x = this.inputCache
        int h = x.shape().dims[1]
        int w = x.shape().dims[2]
        Array<Tensor> gs = this.gradients()

        # dWeight：gradOutput[o] 与输入块相关
        for o = 0, o < this._outChannels, o++
        {
            for c = 0, c < this._inChannels, c++
            {
                for ky = 0, ky < this._kernel, ky++
                {
                    for kx = 0, kx < this._kernel, kx++
                    {
                        Float32 acc = 0.0f
                        for oy = 0, oy < g.shape().dims[1], oy++
                        {
                            for ox = 0, ox < g.shape().dims[2], ox++
                            {
                                int iy = oy * this._stride - this._padding + ky
                                int ix = ox * this._stride - this._padding + kx
                                if iy >= 0
                                {
                                    if iy < h
                                    {
                                        if ix >= 0
                                        {
                                            if ix < w
                                            {
                                                acc = acc + x.get( c, iy, ix ) * g.get( o, oy, ox )
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        gs[0].set( o, c, ky, kx, gs[0].get( o, c, ky, kx ) + acc )
                    }
                }
            }
            # dbias
            Float32 bacc = 0.0f
            for oy = 0, oy < g.shape().dims[1], oy++
            {
                for ox = 0, ox < g.shape().dims[2], ox++
                {
                    bacc = bacc + g.get( o, oy, ox )
                }
            }
            gs[1].set( 0, o, gs[1].get( 0, o ) + bacc )
        }

        # dx：梯度按核权重回填（full 卷积近似）
        Tensor dx = Tensor( x.shape() )
        for o = 0, o < this._outChannels, o++
        {
            for c = 0, c < this._inChannels, c++
            {
                for oy = 0, oy < g.shape().dims[1], oy++
                {
                    for ox = 0, ox < g.shape().dims[2], ox++
                    {
                        for ky = 0, ky < this._kernel, ky++
                        {
                            for kx = 0, kx < this._kernel, kx++
                            {
                                int iy = oy * this._stride - this._padding + ky
                                int ix = ox * this._stride - this._padding + kx
                                if iy >= 0
                                {
                                    if iy < h
                                    {
                                        if ix >= 0
                                        {
                                            if ix < w
                                            {
                                                Float32 v = dx.get( c, iy, ix )
                                                dx.set( c, iy, ix, v + g.get( o, oy, ox ) * this.weight.get( o, c, ky, kx ) )
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        ret dx
    }

    override string summary()
    {
        ret "Conv2D(" + SystemConvertString( this._inChannels ) + "->" + SystemConvertString( this._outChannels ) + ", k=" + SystemConvertString( this._kernel ) + ")"
    }
}
