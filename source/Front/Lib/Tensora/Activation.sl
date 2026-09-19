# Activation —— 激活函数集合（对 Tensor 逐元素/逐行）
#
# 每个函数都配一个 *Grad 版本：输入(前向输入或输出, 上游梯度) -> 下游梯度
# 统一入口 apply(x, kind) / applyGrad(kind, out, gradOut) 便于配置化网络

enum EActivation
{
    None = 0
    Relu
    LeakyRelu
    Elu
    Sigmoid
    Tanh
    Softplus
    Gelu
    Silu
    Mish
    Softmax
    LogSoftmax
}

@Nickname("Act")
public class Activation
{
    public static Tensor relu( Tensor x )
    {
        ret x.relu()
    }

    public static Tensor reluGrad( Tensor x, Tensor gradOut )
    {
        Tensor g = Tensor( x.shape() )
        for i = 0, i < x.size(), i++
        {
            g.setAt( i, gradOut.at( i ) * TensorMath.reluDeriv( x.at( i ) ) )
        }
        ret g
    }

    public static Tensor leakyRelu( Tensor x, Float32 alpha )
    {
        Tensor r = Tensor( x.shape() )
        for i = 0, i < x.size(), i++
        {
            r.setAt( i, TensorMath.leakyRelu( x.at( i ), alpha ) )
        }
        ret r
    }

    public static Tensor elu( Tensor x, Float32 alpha )
    {
        Tensor r = Tensor( x.shape() )
        for i = 0, i < x.size(), i++
        {
            r.setAt( i, TensorMath.elu( x.at( i ), alpha ) )
        }
        ret r
    }

    public static Tensor sigmoid( Tensor x )
    {
        ret x.sigmoid()
    }

    # out 为 sigmoid 输出
    public static Tensor sigmoidGrad( Tensor out, Tensor gradOut )
    {
        Tensor g = Tensor( out.shape() )
        for i = 0, i < out.size(), i++
        {
            g.setAt( i, gradOut.at( i ) * TensorMath.sigmoidDeriv( out.at( i ) ) )
        }
        ret g
    }

    public static Tensor tanh( Tensor x )
    {
        ret x.tanh()
    }

    public static Tensor tanhGrad( Tensor out, Tensor gradOut )
    {
        Tensor g = Tensor( out.shape() )
        for i = 0, i < out.size(), i++
        {
            g.setAt( i, gradOut.at( i ) * TensorMath.tanhDeriv( out.at( i ) ) )
        }
        ret g
    }

    public static Tensor softplus( Tensor x )
    {
        Tensor r = Tensor( x.shape() )
        for i = 0, i < x.size(), i++
        {
            r.setAt( i, TensorMath.softplus( x.at( i ) ) )
        }
        ret r
    }

    public static Tensor gelu( Tensor x )
    {
        Tensor r = Tensor( x.shape() )
        for i = 0, i < x.size(), i++
        {
            r.setAt( i, TensorMath.gelu( x.at( i ) ) )
        }
        ret r
    }

    public static Tensor silu( Tensor x )
    {
        Tensor r = Tensor( x.shape() )
        for i = 0, i < x.size(), i++
        {
            r.setAt( i, TensorMath.silu( x.at( i ) ) )
        }
        ret r
    }

    public static Tensor mish( Tensor x )
    {
        Tensor r = Tensor( x.shape() )
        for i = 0, i < x.size(), i++
        {
            r.setAt( i, TensorMath.mish( x.at( i ) ) )
        }
        ret r
    }

    public static Tensor softmax( Tensor x )
    {
        ret x.softmax()
    }

    # softmax 的雅可比：gx = s * (g - Σ g·s)
    public static Tensor softmaxGrad( Tensor out, Tensor gradOut )
    {
        Tensor g = Tensor( out.shape() )
        for i = 0, i < out.rows(), i++
        {
            Float32 dot = 0.0f
            for j = 0, j < out.cols(), j++
            {
                dot = dot + gradOut.get( i, j ) * out.get( i, j )
            }
            for j = 0, j < out.cols(), j++
            {
                g.set( i, j, out.get( i, j ) * ( gradOut.get( i, j ) - dot ) )
            }
        }
        ret g
    }

    public static Tensor logSoftmax( Tensor x )
    {
        ret x.logSoftmax()
    }

    # ── 配置化入口 ───────────────────────────────────────
    public static Tensor apply( Tensor x, Int32 kind )
    {
        if kind == EActivation.Relu
        {
            ret Activation.relu( x )
        }
        if kind == EActivation.Sigmoid
        {
            ret Activation.sigmoid( x )
        }
        if kind == EActivation.Tanh
        {
            ret Activation.tanh( x )
        }
        if kind == EActivation.Gelu
        {
            ret Activation.gelu( x )
        }
        if kind == EActivation.Silu
        {
            ret Activation.silu( x )
        }
        if kind == EActivation.Mish
        {
            ret Activation.mish( x )
        }
        if kind == EActivation.Softplus
        {
            ret Activation.softplus( x )
        }
        if kind == EActivation.Softmax
        {
            ret Activation.softmax( x )
        }
        if kind == EActivation.LogSoftmax
        {
            ret Activation.logSoftmax( x )
        }
        if kind == EActivation.LeakyRelu
        {
            ret Activation.leakyRelu( x, 0.01f )
        }
        if kind == EActivation.Elu
        {
            ret Activation.elu( x, 1.0f )
        }
        ret x
    }

    public static Tensor applyGrad( Int32 kind, Tensor src, Tensor gradOut )
    {
        if kind == EActivation.Relu
        {
            ret Activation.reluGrad( src, gradOut )
        }
        if kind == EActivation.LeakyRelu
        {
            ret Activation.reluGrad( src, gradOut )
        }
        if kind == EActivation.Sigmoid
        {
            ret Activation.sigmoidGrad( src, gradOut )
        }
        if kind == EActivation.Tanh
        {
            ret Activation.tanhGrad( src, gradOut )
        }
        if kind == EActivation.Softmax
        {
            ret Activation.softmaxGrad( src, gradOut )
        }
        if kind == EActivation.LogSoftmax
        {
            ret Activation.softmaxGrad( src, gradOut )
        }
        ret gradOut
    }
}
