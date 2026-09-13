# Ops —— 张量算子集合（放 Tensor 之外的高层算子）
#
# 包含：
#   卷积相关（im2col / conv2d / maxPool2d / avgPool2d）
#   组合类（concat / stack / oneHot / topK）
#   度量与归一化（cosine / normalize / standardize）
#   自动微分辅助（reduceTo：把梯度按广播逆运算还原到目标形状）

@Nickname("Ops")
public class Ops
{
    # ── im2col：把 [C,H,W] 的输入展开成 [outH*outW, C*k*k] ──
    public static Tensor im2col( Tensor input, Int32 kernel, Int32 stride, Int32 padding )
    {
        int c = input.shape().dims[0]
        int h = input.shape().dims[1]
        int w = input.shape().dims[2]
        int outH = ( h + 2 * padding - kernel ) / stride + 1
        int outW = ( w + 2 * padding - kernel ) / stride + 1
        Tensor col = Tensor( Shape.matrix( outH * outW, c * kernel * kernel ) )
        int row = 0
        for oy = 0, oy < outH, oy++
        {
            for ox = 0, ox < outW, ox++
            {
                int col2 = 0
                for ch = 0, ch < c, ch++
                {
                    for ky = 0, ky < kernel, ky++
                    {
                        for kx = 0, kx < kernel, kx++
                        {
                            int iy = oy * stride - padding + ky
                            int ix = ox * stride - padding + kx
                            Float32 v = 0.0f
                            if iy >= 0
                            {
                                if iy < h
                                {
                                    if ix >= 0
                                    {
                                        if ix < w
                                        {
                                            v = input.get( ch, iy, ix )
                                        }
                                    }
                                }
                            }
                            col.set( row, col2, v )
                            col2++
                        }
                    }
                }
                row++
            }
        }
        ret col
    }

    # 单通道卷积（输入 [C,H,W]，卷积核 [C,k,k]，输出 [1,outH,outW]）
    public static Tensor conv2d( Tensor input, Tensor kernel, Int32 stride, Int32 padding )
    {
        int c = input.shape().dims[0]
        int h = input.shape().dims[1]
        int w = input.shape().dims[2]
        int k = kernel.shape().dims[1]
        int outH = ( h + 2 * padding - k ) / stride + 1
        int outW = ( w + 2 * padding - k ) / stride + 1
        Tensor out = Tensor( Shape.cube( 1, outH, outW ) )
        for oy = 0, oy < outH, oy++
        {
            for ox = 0, ox < outW, ox++
            {
                Float32 sum = 0.0f
                for ch = 0, ch < c, ch++
                {
                    for ky = 0, ky < k, ky++
                    {
                        for kx = 0, kx < k, kx++
                        {
                            int iy = oy * stride - padding + ky
                            int ix = ox * stride - padding + kx
                            if iy >= 0
                            {
                                if iy < h
                                {
                                    if ix >= 0
                                    {
                                        if ix < w
                                        {
                                            sum = sum + input.get( ch, iy, ix ) * kernel.get( ch, ky, kx )
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                out.set( 0, oy, ox, sum )
            }
        }
        ret out
    }

    public static Tensor maxPool2d( Tensor input, Int32 pool, Int32 stride )
    {
        int c = input.shape().dims[0]
        int h = input.shape().dims[1]
        int w = input.shape().dims[2]
        int outH = ( h - pool ) / stride + 1
        int outW = ( w - pool ) / stride + 1
        Tensor out = Tensor( Shape.cube( c, outH, outW ) )
        for ch = 0, ch < c, ch++
        {
            for oy = 0, oy < outH, oy++
            {
                for ox = 0, ox < outW, ox++
                {
                    Float32 m = input.get( ch, oy * stride, ox * stride )
                    for py = 0, py < pool, py++
                    {
                        for px = 0, px < pool, px++
                        {
                            Float32 v = input.get( ch, oy * stride + py, ox * stride + px )
                            if v > m
                            {
                                m = v
                            }
                        }
                    }
                    out.set( ch, oy, ox, m )
                }
            }
        }
        ret out
    }

    public static Tensor avgPool2d( Tensor input, Int32 pool, Int32 stride )
    {
        int c = input.shape().dims[0]
        int h = input.shape().dims[1]
        int w = input.shape().dims[2]
        int outH = ( h - pool ) / stride + 1
        int outW = ( w - pool ) / stride + 1
        Tensor out = Tensor( Shape.cube( c, outH, outW ) )
        Float32 inv = 1.0f / SystemConvertFloat32( pool * pool )
        for ch = 0, ch < c, ch++
        {
            for oy = 0, oy < outH, oy++
            {
                for ox = 0, ox < outW, ox++
                {
                    Float32 acc = 0.0f
                    for py = 0, py < pool, py++
                    {
                        for px = 0, px < pool, px++
                        {
                            acc = acc + input.get( ch, oy * stride + py, ox * stride + px )
                        }
                    }
                    out.set( ch, oy, ox, acc * inv )
                }
            }
        }
        ret out
    }

    # ── 组合 / 转换 ──────────────────────────────────────
    # axis=0 纵向拼接（增加行），axis=1 横向拼接（增加列）
    public static Tensor concat( Array<Tensor> list, Int32 axis )
    {
        int total = 0
        for i = 0, i < list.length, i++
        {
            if axis == 0
            {
                total = total + list[i].rows()
            }
            else
            {
                total = total + list[i].cols()
            }
        }
        int other = list[0].cols()
        if axis != 0
        {
            other = list[0].rows()
        }
        Tensor r = null
        if axis == 0
        {
            r = Tensor( Shape.matrix( total, other ) )
        }
        else
        {
            r = Tensor( Shape.matrix( other, total ) )
        }
        int cursor = 0
        for i = 0, i < list.length, i++
        {
            Tensor t = list[i]
            for a = 0, a < t.rows(), a++
            {
                for b = 0, b < t.cols(), b++
                {
                    if axis == 0
                    {
                        r.set( cursor + a, b, t.get( a, b ) )
                    }
                    else
                    {
                        r.set( a, cursor + b, t.get( a, b ) )
                    }
                }
            }
            if axis == 0
            {
                cursor = cursor + t.rows()
            }
            else
            {
                cursor = cursor + t.cols()
            }
        }
        ret r
    }

    # 沿第 0 维堆成一批：每个元素必须为 1 行
    public static Tensor stack( Array<Tensor> list )
    {
        int rows = list.length
        int cols = list[0].size()
        Tensor r = Tensor( Shape.matrix( rows, cols ) )
        for i = 0, i < rows, i++
        {
            for j = 0, j < cols, j++
            {
                r.set( i, j, list[i].at( j ) )
            }
        }
        ret r
    }

    public static Tensor oneHot( Array<Int32> labels, Int32 classCount )
    {
        Tensor r = Tensor( Shape.matrix( labels.length, classCount ) )
        for i = 0, i < labels.length, i++
        {
            r.set( i, labels[i], 1.0f )
        }
        ret r
    }

    # 取一维张量中最大的 k 个下标
    public static Array<Int32> topK( Tensor v, Int32 k )
    {
        Array<Int32> idx = Rng.rangeArray( v.size() )
        # 简单选择排序取前 k（数据量大时可换堆）
        for i = 0, i < k, i++
        {
            int best = i
            for j = i + 1, j < idx.length, j++
            {
                if v.at( idx[j] ) > v.at( idx[best] )
                {
                    best = j
                }
            }
            Int32 tmp = idx[i]
            idx[i] = idx[best]
            idx[best] = tmp
        }
        Array<Int32> res = Array<Int32>( k )
        for i = 0, i < k, i++
        {
            res[i] = idx[i]
        }
        ret res
    }

    # ── 参数聚合（复合层用：把子层的参数/梯度摊平成一维数组）──
    public static Array<Tensor> join4( Array<Tensor> a, Array<Tensor> b, Array<Tensor> c, Array<Tensor> d )
    {
        int n = a.length + b.length + c.length + d.length
        Array<Tensor> r = Array<Tensor>( n )
        int k = 0
        for i = 0, i < a.length, i++
        {
            r[k] = a[i]
            k++
        }
        for i = 0, i < b.length, i++
        {
            r[k] = b[i]
            k++
        }
        for i = 0, i < c.length, i++
        {
            r[k] = c[i]
            k++
        }
        for i = 0, i < d.length, i++
        {
            r[k] = d[i]
            k++
        }
        ret r
    }

    public static Array<Tensor> joinLayers( Array<Layer> layers, Int32 kind )
    {
        int n = 0
        for i = 0, i < layers.length, i++
        {
            if kind == 0
            {
                n = n + layers[i].parameters().length
            }
            else
            {
                n = n + layers[i].gradients().length
            }
        }
        Array<Tensor> r = Array<Tensor>( n )
        int k = 0
        for i = 0, i < layers.length, i++
        {
            Array<Tensor> src = null
            if kind == 0
            {
                src = layers[i].parameters()
            }
            else
            {
                src = layers[i].gradients()
            }
            for j = 0, j < src.length, j++
            {
                r[k] = src[j]
                k++
            }
        }
        ret r
    }

    # ── 度量 / 归一化 ────────────────────────────────────
    public static Float32 cosine( Tensor a, Tensor b )
    {
        Float32 dot = 0.0f
        Float32 na = 0.0f
        Float32 nb = 0.0f
        for i = 0, i < a.size(), i++
        {
            Float32 x = a.at( i )
            Float32 y = b.at( i )
            dot = dot + x * y
            na = na + x * x
            nb = nb + y * y
        }
        ret dot / ( Mathf.sqrt( na ) * Mathf.sqrt( nb ) + 0.0000001f )
    }

    public static Tensor normalize( Tensor x )
    {
        Float32 acc = 0.0f
        for i = 0, i < x.size(), i++
        {
            acc = acc + x.at( i ) * x.at( i )
        }
        Float32 norm = Mathf.sqrt( acc ) + 0.0000001f
        ret x.scale( 1.0f / norm )
    }

    public static Tensor standardize( Tensor x, Float32 mean, Float32 std )
    {
        Tensor r = x.addScalar( 0.0f - mean )
        ret r.scale( 1.0f / ( std + 0.0000001f ) )
    }

    # ── 自动微分辅助 ─────────────────────────────────────
    # 把梯度 g 按广播的逆运算累加回 target 形状
    public static Tensor reduceTo( Tensor g, Shape target )
    {
        if g.shape().equals( target )
        {
            ret g.clone()
        }
        Tensor r = Tensor.zeros( target )
        for f = 0, f < g.size(), f++
        {
            Array<Int32> idx = g.shape().unflatten( f )
            Array<Int32> t = Array<Int32>( target.rank() )
            int shift = idx.length - target.rank()
            for i = 0, i < target.rank(), i++
            {
                int src = shift + i
                if src < 0
                {
                    t[i] = 0
                }
                elif target.dims[i] == 1
                {
                    t[i] = 0
                }
                else
                {
                    t[i] = idx[ src ]
                }
            }
            int off = target.offset( t )
            r.setAt( off, r.at( off ) + g.at( f ) )
        }
        ret r
    }
}
