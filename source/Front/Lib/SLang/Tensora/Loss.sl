# Loss —— 损失函数
#
# 约定：
#   pred    模型输出；回归时为 [N,1] 或 [N,C]，二分类时 pred 已经过 sigmoid
#   target  回归时为同形状张量；分类时为 [N,1] 的类别 id 或 [N,C] 的 one-hot
#   labels  Array<Int32>，长度为 N 的类别 id（交叉熵系使用）
#
# 每个损失都提供 value(标量) 与 grad(同 pred 形状的梯度张量)

@Nickname("Loss")
public class Loss
{
    # ── 回归 ─────────────────────────────────────────────
    public static Float32 mse( Tensor pred, Tensor target )
    {
        Float32 acc = 0.0f
        for i = 0, i < pred.size(), i++
        {
            Float32 d = pred.at( i ) - target.at( i )
            acc = acc + d * d
        }
        ret acc / SystemConvertFloat32( pred.size() )
    }

    public static Tensor mseGrad( Tensor pred, Tensor target )
    {
        Tensor g = pred.sub( target )
        ret g.scale( 2.0f / SystemConvertFloat32( pred.size() ) )
    }

    public static Float32 mae( Tensor pred, Tensor target )
    {
        Float32 acc = 0.0f
        for i = 0, i < pred.size(), i++
        {
            acc = acc + TensorMath.abs( pred.at( i ) - target.at( i ) )
        }
        ret acc / SystemConvertFloat32( pred.size() )
    }

    public static Tensor maeGrad( Tensor pred, Tensor target )
    {
        Tensor g = Tensor( pred.shape() )
        for i = 0, i < pred.size(), i++
        {
            Float32 d = pred.at( i ) - target.at( i )
            if d >= 0.0f
            {
                g.setAt( i, 1.0f / SystemConvertFloat32( pred.size() ) )
            }
            else
            {
                g.setAt( i, 0.0f - 1.0f / SystemConvertFloat32( pred.size() ) )
            }
        }
        ret g
    }

    public static Float32 rmse( Tensor pred, Tensor target )
    {
        ret Mathf.sqrt( Loss.mse( pred, target ) )
    }

    # Huber：误差小于 delta 走平方，大于 delta 走线性（对离群点鲁棒）
    public static Float32 huber( Tensor pred, Tensor target, Float32 delta )
    {
        Float32 acc = 0.0f
        for i = 0, i < pred.size(), i++
        {
            Float32 d = TensorMath.abs( pred.at( i ) - target.at( i ) )
            if d <= delta
            {
                acc = acc + 0.5f * d * d
            }
            else
            {
                acc = acc + delta * ( d - 0.5f * delta )
            }
        }
        ret acc / SystemConvertFloat32( pred.size() )
    }

    public static Tensor huberGrad( Tensor pred, Tensor target, Float32 delta )
    {
        Tensor g = Tensor( pred.shape() )
        for i = 0, i < pred.size(), i++
        {
            Float32 d = pred.at( i ) - target.at( i )
            if TensorMath.abs( d ) <= delta
            {
                g.setAt( i, d )
            }
            else
            {
                g.setAt( i, delta * TensorMath.sign( d ) )
            }
        }
        ret g.scale( 1.0f / SystemConvertFloat32( pred.size() ) )
    }

    # ── 二分类 ───────────────────────────────────────────
    # pred 为 sigmoid 之后的概率，target 为 0/1
    public static Float32 bce( Tensor pred, Tensor target )
    {
        Float32 acc = 0.0f
        for i = 0, i < pred.size(), i++
        {
            Float32 p = TensorMath.clamp( pred.at( i ), 0.0000001f, 1.0f - 0.0000001f )
            Float32 y = target.at( i )
            acc = acc + ( 0.0f - y * TensorMath.safeLog( p ) - ( 1.0f - y ) * TensorMath.safeLog( 1.0f - p ) )
        }
        ret acc / SystemConvertFloat32( pred.size() )
    }

    public static Tensor bceGrad( Tensor pred, Tensor target )
    {
        Tensor g = Tensor( pred.shape() )
        for i = 0, i < pred.size(), i++
        {
            Float32 p = TensorMath.clamp( pred.at( i ), 0.0000001f, 1.0f - 0.0000001f )
            g.setAt( i, ( p - target.at( i ) ) / ( p * ( 1.0f - p ) ) )
        }
        ret g.scale( 1.0f / SystemConvertFloat32( pred.size() ) )
    }

    # ── 多分类 ───────────────────────────────────────────
    # logits: [N, C] 未归一化分数；labels: 长度 N 的类别 id
    public static Float32 crossEntropy( Tensor logits, Array<Int32> labels )
    {
        Tensor logP = logits.logSoftmax()
        Float32 acc = 0.0f
        for i = 0, i < labels.length, i++
        {
            acc = acc + ( 0.0f - logP.get( i, labels[i] ) )
        }
        ret acc / SystemConvertFloat32( labels.length )
    }

    # 返回 [N, C]：(softmax - onehot) / N
    public static Tensor crossEntropyGrad( Tensor logits, Array<Int32> labels )
    {
        Tensor p = logits.softmax()
        for i = 0, i < labels.length, i++
        {
            p.set( i, labels[i], p.get( i, labels[i] ) - 1.0f )
        }
        ret p.scale( 1.0f / SystemConvertFloat32( labels.length ) )
    }

    # 负对数似然：输入为 logSoftmax 的结果
    public static Float32 nll( Tensor logProbs, Array<Int32> labels )
    {
        Float32 acc = 0.0f
        for i = 0, i < labels.length, i++
        {
            acc = acc + ( 0.0f - logProbs.get( i, labels[i] ) )
        }
        ret acc / SystemConvertFloat32( labels.length )
    }

    # ── 分布距离 ─────────────────────────────────────────
    # KL(logP || logQ)，输入均为 log 概率
    public static Float32 kl( Tensor logP, Tensor logQ )
    {
        Float32 acc = 0.0f
        for i = 0, i < logP.size(), i++
        {
            Float32 p = TensorMath.exp( logP.at( i ) )
            acc = acc + p * ( logP.at( i ) - logQ.at( i ) )
        }
        ret acc
    }
}
