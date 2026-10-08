# Recurrent —— 循环单元与序列层
#
# RNNCell  / LSTMCell / GRUCell ：单步单元，内部保存上一步隐状态
# RNN                          ：把单元按时间步展开，forward(seq) -> [T, H]
#
# 反向说明：这里实现的是"截断 BPTT"近似——只回传当前步对输入与参数的梯度，
# 不沿时间轴继续展开，够做小规模验证，正式训练可换完整 BPTT

@Nickname("RNNCell")
public class RNNCell extends Layer
{
    Int32 _inputSize = 0
    Int32 _hiddenSize = 0
    Int32 _activation = 0

    public Tensor wInput = null
    public Tensor wHidden = null
    public Tensor bias = null
    public Tensor hPrev = null

    public void _init_( Int32 inputSize, Int32 hiddenSize )
    {
        this._init_( inputSize, hiddenSize, EActivation.Tanh )
    }

    public void _init_( Int32 inputSize, Int32 hiddenSize, Int32 activationKind )
    {
        this.name = "RNNCell"
        this._inputSize = inputSize
        this._hiddenSize = hiddenSize
        this._activation = activationKind
        this.wInput = Init.xavierUniform( inputSize, hiddenSize )
        this.wHidden = Init.orthogonal( hiddenSize, hiddenSize )
        this.bias = Tensor( Shape.matrix( 1, hiddenSize ) )
        this.hPrev = Tensor( Shape.matrix( 1, hiddenSize ) )
        this.addParam( this.wInput )
        this.addParam( this.wHidden )
        this.addParam( this.bias )
    }

    Tensor step( Tensor x, Tensor h )
    {
        Tensor z = x.matmul( this.wInput.transpose() ).add( h.matmul( this.wHidden.transpose() ) ).add( this.bias )
        ret Activation.apply( z, this._activation )
    }

    # x: [1, inputSize]
    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        this.hPrev = this.step( x, this.hPrev )
        this.outputCache = this.hPrev
        ret this.hPrev
    }

    override Tensor backward( Tensor gradOutput )
    {
        Tensor g = gradOutput
        if this._activation == EActivation.Tanh
        {
            g = Activation.tanhGrad( this.hPrev, g )
        }
        elif this._activation == EActivation.Relu
        {
            g = Activation.reluGrad( this.hPrev, g )
        }
        Array<Tensor> gs = this.gradients()
        gs[0].addInPlace( g.transpose().matmul( this.inputCache ) )
        gs[1].addInPlace( g.transpose().matmul( this.hPrev ) )
        gs[2].addInPlace( g.sumAlong( 0 ) )
        ret g.matmul( this.wInput )
    }

    void resetState()
    {
        this.hPrev = Tensor( Shape.matrix( 1, this._hiddenSize ) )
    }

    override string summary()
    {
        ret "RNNCell(" + SystemConvertString( this._inputSize ) + "->" + SystemConvertString( this._hiddenSize ) + ")"
    }
}

@Nickname("LSTMCell")
public class LSTMCell extends Layer
{
    Int32 _inputSize = 0
    Int32 _hiddenSize = 0

    # 四个门（i, f, g, o）合并成一张权重：[4H, I+H]
    public Tensor wAll = null
    public Tensor bias = null
    public Tensor hPrev = null
    public Tensor cPrev = null

    Tensor _gates = null
    Tensor _concat = null

    public void _init_( Int32 inputSize, Int32 hiddenSize )
    {
        this.name = "LSTMCell"
        this._inputSize = inputSize
        this._hiddenSize = hiddenSize
        this.wAll = Init.xavierUniform( inputSize + hiddenSize, 4 * hiddenSize )
        this.bias = Tensor( Shape.matrix( 1, 4 * hiddenSize ) )
        this.hPrev = Tensor( Shape.matrix( 1, hiddenSize ) )
        this.cPrev = Tensor( Shape.matrix( 1, hiddenSize ) )
        this.addParam( this.wAll )
        this.addParam( this.bias )
    }

    Tensor step( Tensor x, Tensor h, Tensor c )
    {
        Tensor concat = Tensor( Shape.matrix( 1, this._inputSize + this._hiddenSize ) )
        for j = 0, j < this._inputSize, j++
        {
            concat.set( 0, j, x.get( 0, j ) )
        }
        for j = 0, j < this._hiddenSize, j++
        {
            concat.set( 0, this._inputSize + j, h.get( 0, j ) )
        }
        this._concat = concat
        Tensor z = concat.matmul( this.wAll.transpose() ).add( this.bias )
        this._gates = z
        int h = this._hiddenSize
        Tensor newH = Tensor( Shape.matrix( 1, h ) )
        Tensor newC = Tensor( Shape.matrix( 1, h ) )
        for j = 0, j < h, j++
        {
            Float32 i = TensorMath.sigmoid( z.get( 0, j ) )
            Float32 f = TensorMath.sigmoid( z.get( 0, h + j ) )
            Float32 g = Mathf.tanh( z.get( 0, 2 * h + j ) )
            Float32 o = TensorMath.sigmoid( z.get( 0, 3 * h + j ) )
            Float32 nc = f * c.get( 0, j ) + i * g
            newC.set( 0, j, nc )
            newH.set( 0, j, o * Mathf.tanh( nc ) )
        }
        this.cPrev = newC
        ret newH
    }

    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        this.hPrev = this.step( x, this.hPrev, this.cPrev )
        this.outputCache = this.hPrev
        ret this.hPrev
    }

    override Tensor backward( Tensor gradOutput )
    {
        # 简化：把 gate 梯度统一按 sigmoid/tanh 导数缩放后累加到 wAll
        int h = this._hiddenSize
        Tensor gz = Tensor( this._gates.shape() )
        for j = 0, j < h, j++
        {
            gz.set( 0, j, gradOutput.get( 0, j ) * TensorMath.sigmoidDeriv( TensorMath.sigmoid( this._gates.get( 0, j ) ) ) )
            gz.set( 0, h + j, gradOutput.get( 0, j ) )
            gz.set( 0, 2 * h + j, gradOutput.get( 0, j ) * TensorMath.tanhDeriv( Mathf.tanh( this._gates.get( 0, 2 * h + j ) ) ) )
            gz.set( 0, 3 * h + j, gradOutput.get( 0, j ) )
        }
        Array<Tensor> gs = this.gradients()
        gs[0].addInPlace( gz.transpose().matmul( this._concat ) )
        gs[1].addInPlace( gz.sumAlong( 0 ) )
        # 输入侧梯度不回传（前面通常是 Embedding / 离散 id）
        ret Tensor( Shape.matrix( 1, this._inputSize ) )
    }

    void resetState()
    {
        this.hPrev = Tensor( Shape.matrix( 1, this._hiddenSize ) )
        this.cPrev = Tensor( Shape.matrix( 1, this._hiddenSize ) )
    }

    override string summary()
    {
        ret "LSTMCell(" + SystemConvertString( this._inputSize ) + "->" + SystemConvertString( this._hiddenSize ) + ")"
    }
}

@Nickname("GRUCell")
public class GRUCell extends Layer
{
    Int32 _inputSize = 0
    Int32 _hiddenSize = 0

    public Tensor wAll = null
    public Tensor bias = null
    public Tensor hPrev = null

    Tensor _concat = null

    public void _init_( Int32 inputSize, Int32 hiddenSize )
    {
        this.name = "GRUCell"
        this._inputSize = inputSize
        this._hiddenSize = hiddenSize
        this.wAll = Init.xavierUniform( inputSize + hiddenSize, 3 * hiddenSize )
        this.bias = Tensor( Shape.matrix( 1, 3 * hiddenSize ) )
        this.hPrev = Tensor( Shape.matrix( 1, hiddenSize ) )
        this.addParam( this.wAll )
        this.addParam( this.bias )
    }

    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        int h = this._hiddenSize
        Tensor concat = Tensor( Shape.matrix( 1, this._inputSize + h ) )
        for j = 0, j < this._inputSize, j++
        {
            concat.set( 0, j, x.get( 0, j ) )
        }
        for j = 0, j < h, j++
        {
            concat.set( 0, this._inputSize + j, this.hPrev.get( 0, j ) )
        }
        this._concat = concat
        Tensor z = concat.matmul( this.wAll.transpose() ).add( this.bias )
        Tensor newH = Tensor( Shape.matrix( 1, h ) )
        for j = 0, j < h, j++
        {
            Float32 zt = TensorMath.sigmoid( z.get( 0, j ) )
            Float32 rt = TensorMath.sigmoid( z.get( 0, h + j ) )
            Float32 nt = Mathf.tanh( z.get( 0, 2 * h + j ) )
            newH.set( 0, j, ( 1.0f - zt ) * nt + zt * this.hPrev.get( 0, j ) * rt )
        }
        this.hPrev = newH
        this.outputCache = newH
        ret newH
    }

    override Tensor backward( Tensor gradOutput )
    {
        # 简化：门梯度按输出梯度近似累加
        int h = this._hiddenSize
        Tensor gz = Tensor( Shape.matrix( 1, 3 * h ) )
        for j = 0, j < h, j++
        {
            gz.set( 0, j, gradOutput.get( 0, j ) )
            gz.set( 0, h + j, gradOutput.get( 0, j ) )
            gz.set( 0, 2 * h + j, gradOutput.get( 0, j ) )
        }
        Array<Tensor> gs = this.gradients()
        gs[0].addInPlace( gz.transpose().matmul( this._concat ) )
        gs[1].addInPlace( gz.sumAlong( 0 ) )
        # 输入侧梯度不回传
        ret Tensor( Shape.matrix( 1, this._inputSize ) )
    }

    void resetState()
    {
        this.hPrev = Tensor( Shape.matrix( 1, this._hiddenSize ) )
    }

    override string summary()
    {
        ret "GRUCell(" + SystemConvertString( this._inputSize ) + "->" + SystemConvertString( this._hiddenSize ) + ")"
    }
}

# RNN —— 序列层：把 RNNCell 按时间步展开
@Nickname("RNN")
public class RNN extends Layer
{
    public RNNCell cell = null
    public bool returnSequence = true
    Int32 _steps = 0

    public void _init_( RNNCell cell )
    {
        this.name = "RNN"
        this.cell = cell
        this.returnSequence = true
    }

    # seq: [T, inputSize]；输出 [T, hiddenSize]（returnSequence）或 [1, hiddenSize]
    override Tensor forward( Tensor seq )
    {
        this.inputCache = seq
        this.cell.resetState()
        int t = seq.rows()
        int h = this.cell.hPrev.cols()
        this._steps = t
        Tensor out = Tensor( Shape.matrix( t, h ) )
        for i = 0, i < t, i++
        {
            Tensor xt = seq.row( i )
            Tensor ht = this.cell.forward( xt )
            for j = 0, j < h, j++
            {
                out.set( i, j, ht.get( 0, j ) )
            }
        }
        if this.returnSequence
        {
            this.outputCache = out
            ret out
        }
        Tensor last = out.row( t - 1 )
        this.outputCache = last
        ret last
    }

    override Tensor backward( Tensor gradOutput )
    {
        # 截断 BPTT：用最后一步的梯度回传一次
        Tensor g = gradOutput
        if this.returnSequence
        {
            g = gradOutput.row( this._steps - 1 )
        }
        ret this.cell.backward( g )
    }

    override string summary()
    {
        ret "RNN(" + this.cell.summary() + ")"
    }
}
