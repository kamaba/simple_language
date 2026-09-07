# Pooling —— 二维池化层
#
# MaxPool2D：前向记录每个窗口最大值在输入中的扁平下标，反向按位置回填
# AvgPool2D：前向取均值，反向把梯度平均分摊回窗口内每个位置
#
# 约定输入输出形状均为 [C, H, W]

@Nickname("MaxPool")
public class MaxPool2D extends Layer
{
    Int32 _pool = 2
    Int32 _stride = 2

    # 每个输出位置对应的输入扁平下标（长度 = C * outH * outW）
    Array<Int32> _maxIndex = null

    public void _init_( Int32 poolSize )
    {
        this._init_( poolSize, poolSize )
    }

    public void _init_( Int32 poolSize, Int32 stride )
    {
        this.name = "MaxPool2D"
        this._pool = poolSize
        this._stride = stride
    }

    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        int c = x.shape().dims[0]
        int h = x.shape().dims[1]
        int w = x.shape().dims[2]
        int outH = ( h - this._pool ) / this._stride + 1
        int outW = ( w - this._pool ) / this._stride + 1
        Tensor out = Tensor( Shape.cube( c, outH, outW ) )
        this._maxIndex = Array<Int32>( c * outH * outW )
        int n = 0
        for ch = 0, ch < c, ch++
        {
            for oy = 0, oy < outH, oy++
            {
                for ox = 0, ox < outW, ox++
                {
                    Float32 m = x.get( ch, oy * this._stride, ox * this._stride )
                    int mi = ( ch * h + oy * this._stride ) * w + ox * this._stride
                    for py = 0, py < this._pool, py++
                    {
                        for px = 0, px < this._pool, px++
                        {
                            int iy = oy * this._stride + py
                            int ix = ox * this._stride + px
                            Float32 v = x.get( ch, iy, ix )
                            if v > m
                            {
                                m = v
                                mi = ( ch * h + iy ) * w + ix
                            }
                        }
                    }
                    out.set( ch, oy, ox, m )
                    this._maxIndex[n] = mi
                    n++
                }
            }
        }
        this.outputCache = out
        ret out
    }

    override Tensor backward( Tensor gradOutput )
    {
        Tensor x = this.inputCache
        Tensor dx = Tensor( x.shape() )
        int n = 0
        for i = 0, i < gradOutput.size(), i++
        {
            dx.setAt( this._maxIndex[n], dx.at( this._maxIndex[n] ) + gradOutput.at( i ) )
            n++
        }
        ret dx
    }

    override string summary()
    {
        ret "MaxPool2D(k=" + SystemConvertString( this._pool ) + ")"
    }
}

@Nickname("AvgPool")
public class AvgPool2D extends Layer
{
    Int32 _pool = 2
    Int32 _stride = 2

    public void _init_( Int32 poolSize )
    {
        this._init_( poolSize, poolSize )
    }

    public void _init_( Int32 poolSize, Int32 stride )
    {
        this.name = "AvgPool2D"
        this._pool = poolSize
        this._stride = stride
    }

    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        this.outputCache = Ops.avgPool2d( x, this._pool, this._stride )
        ret this.outputCache
    }

    override Tensor backward( Tensor gradOutput )
    {
        Tensor x = this.inputCache
        Tensor dx = Tensor( x.shape() )
        int c = x.shape().dims[0]
        int h = x.shape().dims[1]
        int w = x.shape().dims[2]
        Float32 inv = 1.0f / SystemConvertFloat32( this._pool * this._pool )
        for ch = 0, ch < c, ch++
        {
            for oy = 0, oy < gradOutput.shape().dims[1], oy++
            {
                for ox = 0, ox < gradOutput.shape().dims[2], ox++
                {
                    Float32 g = gradOutput.get( ch, oy, ox ) * inv
                    for py = 0, py < this._pool, py++
                    {
                        for px = 0, px < this._pool, px++
                        {
                            int iy = oy * this._stride + py
                            int ix = ox * this._stride + px
                            dx.set( ch, iy, ix, dx.get( ch, iy, ix ) + g )
                        }
                    }
                }
            }
        }
        ret dx
    }

    override string summary()
    {
        ret "AvgPool2D(k=" + SystemConvertString( this._pool ) + ")"
    }
}
