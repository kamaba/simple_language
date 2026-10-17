# Metrics —— 评估指标
#
# 分类：accuracy / topK / precision / recall / f1 / confusionMatrix
# 回归：mse / mae / rmse / r2
# 语言模型：perplexity

@Nickname("Metrics")
public class Metrics
{
    # 由 logits [N, C] 得到预测类别 [N]
    public static Array<Int32> predictLabels( Tensor logits )
    {
        int n = logits.rows()
        Array<Int32> res = Array<Int32>( n )
        for i = 0, i < n, i++
        {
            int best = 0
            Float32 m = logits.get( i, 0 )
            for j = 1, j < logits.cols(), j++
            {
                if logits.get( i, j ) > m
                {
                    m = logits.get( i, j )
                    best = j
                }
            }
            res[i] = best
        }
        ret res
    }

    public static Float32 accuracy( Tensor logits, Array<Int32> labels )
    {
        Array<Int32> pred = Metrics.predictLabels( logits )
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

    # 预测概率前 k 名里包含正确类别即算对
    public static Float32 topKAccuracy( Tensor logits, Array<Int32> labels, Int32 k )
    {
        int hit = 0
        for i = 0, i < logits.rows(), i++
        {
            Tensor row = logits.row( i )
            Array<Int32> top = Ops.topK( row, k )
            bool ok = false
            for j = 0, j < top.length, j++
            {
                if top[j] == labels[i]
                {
                    ok = true
                }
            }
            if ok
            {
                hit++
            }
        }
        ret SystemConvertFloat32( hit ) / SystemConvertFloat32( logits.rows() )
    }

    # 二分类指标：以 cls 为正类
    public static Float32 precision( Array<Int32> preds, Array<Int32> labels, Int32 cls )
    {
        int tp = 0
        int fp = 0
        for i = 0, i < preds.length, i++
        {
            if preds[i] == cls
            {
                if labels[i] == cls
                {
                    tp++
                }
                else
                {
                    fp++
                }
            }
        }
        if tp + fp == 0
        {
            ret 0.0f
        }
        ret SystemConvertFloat32( tp ) / SystemConvertFloat32( tp + fp )
    }

    public static Float32 recall( Array<Int32> preds, Array<Int32> labels, Int32 cls )
    {
        int tp = 0
        int fn = 0
        for i = 0, i < preds.length, i++
        {
            if labels[i] == cls
            {
                if preds[i] == cls
                {
                    tp++
                }
                else
                {
                    fn++
                }
            }
        }
        if tp + fn == 0
        {
            ret 0.0f
        }
        ret SystemConvertFloat32( tp ) / SystemConvertFloat32( tp + fn )
    }

    public static Float32 f1( Array<Int32> preds, Array<Int32> labels, Int32 cls )
    {
        Float32 p = Metrics.precision( preds, labels, cls )
        Float32 r = Metrics.recall( preds, labels, cls )
        if p + r <= 0.0f
        {
            ret 0.0f
        }
        ret 2.0f * p * r / ( p + r )
    }

    # 宏平均 F1
    public static Float32 macroF1( Array<Int32> preds, Array<Int32> labels, Int32 classCount )
    {
        Float32 acc = 0.0f
        for c = 0, c < classCount, c++
        {
            acc = acc + Metrics.f1( preds, labels, c )
        }
        ret acc / SystemConvertFloat32( classCount )
    }

    # 混淆矩阵 [C, C]：行=真实，列=预测
    public static Tensor confusionMatrix( Array<Int32> preds, Array<Int32> labels, Int32 classCount )
    {
        Tensor m = Tensor( Shape.matrix( classCount, classCount ) )
        for i = 0, i < labels.length, i++
        {
            Float32 v = m.get( labels[i], preds[i] )
            m.set( labels[i], preds[i], v + 1.0f )
        }
        ret m
    }

    # ── 回归 ─────────────────────────────────────────────
    public static Float32 mse( Tensor pred, Tensor target )
    {
        ret Loss.mse( pred, target )
    }

    public static Float32 mae( Tensor pred, Tensor target )
    {
        ret Loss.mae( pred, target )
    }

    public static Float32 rmse( Tensor pred, Tensor target )
    {
        ret Loss.rmse( pred, target )
    }

    # 决定系数 R²：越接近 1 越好
    public static Float32 r2( Tensor pred, Tensor target )
    {
        Float32 mean = target.mean()
        Float32 ssRes = 0.0f
        Float32 ssTot = 0.0f
        for i = 0, i < pred.size(), i++
        {
            Float32 d = pred.at( i ) - target.at( i )
            ssRes = ssRes + d * d
            Float32 t = target.at( i ) - mean
            ssTot = ssTot + t * t
        }
        if ssTot <= 0.0f
        {
            ret 0.0f
        }
        ret 1.0f - ssRes / ssTot
    }

    # 困惑度：exp(交叉熵)
    public static Float32 perplexity( Float32 crossEntropyValue )
    {
        ret Mathf.exp( crossEntropyValue )
    }
}
