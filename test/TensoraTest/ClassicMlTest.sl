import Tensora

# ClassicMlTest —— 传统机器学习示例（算法直接写在测试里，后续可提升进 Tensora 库）
#
# 包含：LinearRegression / LogisticRegression / KNN / KMeans / GaussianNB / DecisionStump / PCA
#
# 说明：这些实现只用于演示「用 Tensor 写算法」的写法，规模都很小，不追求数值最优

# ── 线性回归（梯度下降）────────────────────────────────
public class LinearRegression
{
    public Tensor weight = null
    public Float32 bias = 0.0f
    public Float32 lr = 0.05f
    public Int32 epochs = 200

    public void _init_()
    {
        this.weight = null
        this.bias = 0.0f
        this.lr = 0.05f
        this.epochs = 200
    }

    # x: [N, F]  y: [N, 1]
    public void fit( Tensor x, Tensor y )
    {
        int n = x.rows()
        int f = x.cols()
        this.weight = Tensor( Shape.matrix( f, 1 ) )
        for e = 0, e < this.epochs, e++
        {
            Tensor pred = x.matmul( this.weight ).addScalar( this.bias )
            Tensor diff = pred.sub( y )
            Tensor gw = x.transpose().matmul( diff ).scale( 2.0f / SystemConvertFloat32( n ) )
            Float32 gb = diff.sum() * 2.0f / SystemConvertFloat32( n )
            this.weight = this.weight.sub( gw.scale( this.lr ) )
            this.bias = this.bias - gb * this.lr
        }
    }

    public Tensor predict( Tensor x )
    {
        ret x.matmul( this.weight ).addScalar( this.bias )
    }
}

# ── 逻辑回归（梯度下降 + BCE）──────────────────────────
public class LogisticRegression
{
    public Tensor weight = null
    public Float32 bias = 0.0f
    public Float32 lr = 0.1f
    public Int32 epochs = 300

    public void _init_()
    {
        this.weight = null
        this.bias = 0.0f
        this.lr = 0.1f
        this.epochs = 300
    }

    public void fit( Tensor x, Tensor y )
    {
        int n = x.rows()
        int f = x.cols()
        this.weight = Tensor( Shape.matrix( f, 1 ) )
        for e = 0, e < this.epochs, e++
        {
            Tensor p = x.matmul( this.weight ).addScalar( this.bias ).sigmoid()
            Tensor g = p.sub( y ).scale( 1.0f / SystemConvertFloat32( n ) )
            Tensor gw = x.transpose().matmul( g )
            Float32 gb = g.sum()
            this.weight = this.weight.sub( gw.scale( this.lr ) )
            this.bias = this.bias - gb * this.lr
        }
    }

    public Array<Int32> predict( Tensor x )
    {
        Tensor p = x.matmul( this.weight ).addScalar( this.bias ).sigmoid()
        Array<Int32> res = Array<Int32>( p.rows() )
        for i = 0, i < p.rows(), i++
        {
            if p.get( i, 0 ) >= 0.5f
            {
                res[i] = 1
            }
            else
            {
                res[i] = 0
            }
        }
        ret res
    }
}

# ── K 近邻 ─────────────────────────────────────────────
public class KNN
{
    public Tensor trainX = null
    public Array<Int32> trainY = null
    public Int32 k = 3

    public void _init_( Int32 k )
    {
        this.k = k
    }

    public void fit( Tensor x, Array<Int32> y )
    {
        this.trainX = x
        this.trainY = y
    }

    # row 形状 [1, F]
    public Int32 predictOne( Tensor row )
    {
        int n = this.trainX.rows()
        Array<Float32> dist = Array<Float32>( n )
        Array<Int32> order = Rng.rangeArray( n )
        for i = 0, i < n, i++
        {
            Tensor d = this.trainX.row( i ).sub( row )
            Float32 acc = 0.0f
            for j = 0, j < d.cols(), j++
            {
                acc = acc + d.get( 0, j ) * d.get( 0, j )
            }
            dist[i] = acc
        }
        int kk = this.k
        if kk > n
        {
            kk = n
        }
        # 选择排序取前 k 个最小距离
        for a = 0, a < kk, a++
        {
            int best = a
            for b = a + 1, b < n, b++
            {
                if dist[ order[b] ] < dist[ order[best] ]
                {
                    best = b
                }
            }
            Int32 tmp = order[a]
            order[a] = order[best]
            order[best] = tmp
        }
        # 多数投票
        int bestCls = this.trainY[ order[0] ]
        int bestCount = 0
        for a = 0, a < kk, a++
        {
            Int32 c = this.trainY[ order[a] ]
            int cnt = 0
            for b = 0, b < kk, b++
            {
                if this.trainY[ order[b] ] == c
                {
                    cnt++
                }
            }
            if cnt > bestCount
            {
                bestCount = cnt
                bestCls = c
            }
        }
        ret bestCls
    }

    public Array<Int32> predict( Tensor x )
    {
        Array<Int32> res = Array<Int32>( x.rows() )
        for i = 0, i < x.rows(), i++
        {
            res[i] = this.predictOne( x.row( i ) )
        }
        ret res
    }
}

# ── K 均值聚类 ─────────────────────────────────────────
public class KMeans
{
    public Int32 k = 2
    public Tensor centers = null
    public Array<Int32> assign = null

    public void _init_( Int32 k )
    {
        this.k = k
    }

    Float32 dist2( Tensor a, Tensor b )
    {
        Tensor d = a.sub( b )
        Float32 acc = 0.0f
        for j = 0, j < d.cols(), j++
        {
            acc = acc + d.get( 0, j ) * d.get( 0, j )
        }
        ret acc
    }

    Int32 nearest( Tensor row )
    {
        int best = 0
        Float32 bd = this.dist2( this.centers.row( 0 ), row )
        for c = 1, c < this.k, c++
        {
            Float32 d = this.dist2( this.centers.row( c ), row )
            if d < bd
            {
                bd = d
                best = c
            }
        }
        ret best
    }

    public void fit( Tensor x, Int32 iters )
    {
        int n = x.rows()
        int f = x.cols()
        this.centers = Tensor( Shape.matrix( this.k, f ) )
        for c = 0, c < this.k, c++
        {
            for j = 0, j < f, j++
            {
                this.centers.set( c, j, x.get( c, j ) )
            }
        }
        this.assign = Array<Int32>( n )
        for it = 0, it < iters, it++
        {
            for i = 0, i < n, i++
            {
                this.assign[i] = this.nearest( x.row( i ) )
            }
            for c = 0, c < this.k, c++
            {
                Float32 cnt = 0.0f
                Tensor sum = Tensor( Shape.matrix( 1, f ) )
                for i = 0, i < n, i++
                {
                    if this.assign[i] == c
                    {
                        for j = 0, j < f, j++
                        {
                            sum.set( 0, j, sum.get( 0, j ) + x.get( i, j ) )
                        }
                        cnt = cnt + 1.0f
                    }
                }
                if cnt > 0.0f
                {
                    for j = 0, j < f, j++
                    {
                        this.centers.set( c, j, sum.get( 0, j ) / cnt )
                    }
                }
            }
        }
    }

    public Array<Int32> predict( Tensor x )
    {
        Array<Int32> res = Array<Int32>( x.rows() )
        for i = 0, i < x.rows(), i++
        {
            res[i] = this.nearest( x.row( i ) )
        }
        ret res
    }
}

# ── 高斯朴素贝叶斯 ─────────────────────────────────────
public class GaussianNB
{
    public Int32 classCount = 0
    public Tensor mean = null
    public Tensor var = null
    public Tensor prior = null

    public void _init_( Int32 classCount )
    {
        this.classCount = classCount
    }

    public void fit( Tensor x, Array<Int32> y )
    {
        int n = x.rows()
        int f = x.cols()
        int c = this.classCount
        this.mean = Tensor( Shape.matrix( c, f ) )
        this.var = Tensor( Shape.matrix( c, f ) )
        this.prior = Tensor( Shape.matrix( 1, c ) )
        Array<Int32> cnt = Array<Int32>( c )

        for i = 0, i < n, i++
        {
            Int32 k = y[i]
            cnt[k] = cnt[k] + 1
            for j = 0, j < f, j++
            {
                this.mean.set( k, j, this.mean.get( k, j ) + x.get( i, j ) )
            }
        }
        for k = 0, k < c, k++
        {
            if cnt[k] > 0
            {
                for j = 0, j < f, j++
                {
                    this.mean.set( k, j, this.mean.get( k, j ) / SystemConvertFloat32( cnt[k] ) )
                }
                this.prior.set( 0, k, SystemConvertFloat32( cnt[k] ) / SystemConvertFloat32( n ) )
            }
        }
        for i = 0, i < n, i++
        {
            Int32 k2 = y[i]
            for j = 0, j < f, j++
            {
                Float32 d = x.get( i, j ) - this.mean.get( k2, j )
                this.var.set( k2, j, this.var.get( k2, j ) + d * d )
            }
        }
        for k = 0, k < c, k++
        {
            if cnt[k] > 1
            {
                for j = 0, j < f, j++
                {
                    this.var.set( k, j, this.var.get( k, j ) / SystemConvertFloat32( cnt[k] - 1 ) + 0.0001f )
                }
            }
        }
    }

    public Array<Int32> predict( Tensor x )
    {
        int n = x.rows()
        int f = x.cols()
        Array<Int32> res = Array<Int32>( n )
        for i = 0, i < n, i++
        {
            int best = 0
            Float32 bestScore = 0.0f - 1000000000.0f
            for k = 0, k < this.classCount, k++
            {
                Float32 logP = TensorMath.safeLog( this.prior.get( 0, k ) )
                for j = 0, j < f, j++
                {
                    Float32 m = this.mean.get( k, j )
                    Float32 v = this.var.get( k, j )
                    Float32 d = x.get( i, j ) - m
                    logP = logP - 0.5f * TensorMath.safeLog( 6.283185307f * v ) - ( d * d ) / ( 2.0f * v )
                }
                if logP > bestScore
                {
                    bestScore = logP
                    best = k
                }
            }
            res[i] = best
        }
        ret res
    }
}

# ── 决策树桩（单层决策树）───────────────────────────────
public class DecisionStump
{
    public Int32 feature = 0
    public Float32 threshold = 0.0f
    public Int32 leftClass = 0
    public Int32 rightClass = 1

    public void fit( Tensor x, Array<Int32> y, Int32 feature )
    {
        this.feature = feature
        int n = x.rows()
        Float32 lo = x.get( 0, feature )
        Float32 hi = lo
        for i = 1, i < n, i++
        {
            Float32 v = x.get( i, feature )
            if v < lo
            {
                lo = v
            }
            if v > hi
            {
                hi = v
            }
        }
        int bestErr = n + 1
        for s = 1, s < 20, s++
        {
            Float32 th = lo + ( hi - lo ) * SystemConvertFloat32( s ) / 20.0f
            int err = 0
            for i = 0, i < n, i++
            {
                Int32 pred = this.rightClass
                if x.get( i, feature ) <= th
                {
                    pred = this.leftClass
                }
                if pred != y[i]
                {
                    err++
                }
            }
            if err < bestErr
            {
                bestErr = err
                this.threshold = th
            }
        }
    }

    public Array<Int32> predict( Tensor x )
    {
        Array<Int32> res = Array<Int32>( x.rows() )
        for i = 0, i < x.rows(), i++
        {
            if x.get( i, this.feature ) <= this.threshold
            {
                res[i] = this.leftClass
            }
            else
            {
                res[i] = this.rightClass
            }
        }
        ret res
    }
}

# ── PCA（幂迭代求第一主成分）────────────────────────────
public class PCA
{
    public Tensor mean = null
    public Tensor component = null

    public void fit( Tensor x, Int32 iters )
    {
        this.mean = x.sumAlong( 0 ).scale( 1.0f / SystemConvertFloat32( x.rows() ) )
        Tensor xc = x.sub( this.mean )
        int f = x.cols()
        this.component = Tensor.randn( Shape.matrix( f, 1 ) )
        for it = 0, it < iters, it++
        {
            Tensor proj = xc.matmul( this.component )
            Tensor g = xc.transpose().matmul( proj )
            this.component = Ops.normalize( g )
        }
    }

    public Tensor transform( Tensor x )
    {
        ret x.sub( this.mean ).matmul( this.component )
    }
}

ClassicMlTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[ClassicMlTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[ClassicMlTest] " + name + " : FAIL" )
        }
    }

    static Float32 accuracy( Array<Int32> pred, Array<Int32> label )
    {
        int hit = 0
        for i = 0, i < pred.length, i++
        {
            if pred[i] == label[i]
            {
                hit++
            }
        }
        ret SystemConvertFloat32( hit ) / SystemConvertFloat32( pred.length )
    }

    # 造可分数据：第 c 类围绕 c*3 分布
    static Tensor makeClassData( Array<Int32> labels, Int32 features )
    {
        Rng rng = Rng( 7 )
        int n = labels.length
        Tensor x = Tensor( Shape.matrix( n, features ) )
        for i = 0, i < n, i++
        {
            Float32 center = SystemConvertFloat32( labels[i] ) * 3.0f
            for j = 0, j < features, j++
            {
                x.set( i, j, center + rng.normal( 0.0f, 0.5f ) )
            }
        }
        ret x
    }

    # ── 1. 线性回归：拟合 y = 2x + 1 ─────────────────────
    static linearRegressionTest()
    {
        global.println( "===== linearRegressionTest =====" )
        Tensor x = Tensor( Shape.matrix( 6, 1 ) )
        Tensor y = Tensor( Shape.matrix( 6, 1 ) )
        for i = 0, i < 6, i++
        {
            Float32 xv = SystemConvertFloat32( i ) * 0.5f
            x.set( i, 0, xv )
            y.set( i, 0, 2.0f * xv + 1.0f )
        }
        LinearRegression lr = LinearRegression()
        lr.lr = 0.05f
        lr.epochs = 300
        lr.fit( x, y )
        global.println( "w ~= 2 -> " + SystemConvertString( lr.weight.get( 0, 0 ) ) )
        global.println( "b ~= 1 -> " + SystemConvertString( lr.bias ) )
        global.println( "r2 = " + SystemConvertString( Metrics.r2( lr.predict( x ), y ) ) )
        check( "w near 2", TensorMath.abs( lr.weight.get( 0, 0 ) - 2.0f ) < 0.2f )
    }

    # ── 2. 逻辑回归：x0 + x1 > 1 ─────────────────────────
    static logisticRegressionTest()
    {
        global.println( "===== logisticRegressionTest =====" )
        Rng rng = Rng( 11 )
        int n = 20
        Tensor x = Tensor( Shape.matrix( n, 2 ) )
        Tensor y = Tensor( Shape.matrix( n, 1 ) )
        Array<Int32> labels = Array<Int32>( n )
        for i = 0, i < n, i++
        {
            Float32 a = rng.uniform( 0.0f, 1.0f )
            Float32 b = rng.uniform( 0.0f, 1.0f )
            x.set( i, 0, a )
            x.set( i, 1, b )
            if a + b > 1.0f
            {
                y.set( i, 0, 1.0f )
                labels[i] = 1
            }
            else
            {
                y.set( i, 0, 0.0f )
                labels[i] = 0
            }
        }
        LogisticRegression lg = LogisticRegression()
        lg.lr = 0.5f
        lg.epochs = 500
        lg.fit( x, y )
        Array<Int32> pred = lg.predict( x )
        global.println( "acc = " + SystemConvertString( ClassicMlTest.accuracy( pred, labels ) ) )
        check( "logreg acc > 0.7", ClassicMlTest.accuracy( pred, labels ) > 0.7f )
    }

    # ── 3. KNN ───────────────────────────────────────────
    static knnTest()
    {
        global.println( "===== knnTest =====" )
        Array<Int32> labels = [ 0, 0, 0, 0, 1, 1, 1, 1 ]
        Tensor x = ClassicMlTest.makeClassData( labels, 2 )
        KNN knn = KNN( 3 )
        knn.fit( x, labels )
        Array<Int32> pred = knn.predict( x )
        global.println( "acc = " + SystemConvertString( ClassicMlTest.accuracy( pred, labels ) ) )
        check( "knn acc == 1", ClassicMlTest.accuracy( pred, labels ) == 1.0f )
    }

    # ── 4. KMeans ───────────────────────────────────────
    static kmeansTest()
    {
        global.println( "===== kmeansTest =====" )
        Array<Int32> labels = [ 0, 0, 0, 0, 1, 1, 1, 1 ]
        Tensor x = ClassicMlTest.makeClassData( labels, 2 )
        KMeans km = KMeans( 2 )
        km.fit( x, 20 )
        Array<Int32> assign = km.predict( x )
        global.println( "cluster0 center = " + km.centers.row( 0 ).toString() )
        global.println( "cluster1 center = " + km.centers.row( 1 ).toString() )
        check( "two clusters", assign[0] == assign[1] )
    }

    # ── 5. 朴素贝叶斯 ───────────────────────────────────
    static naiveBayesTest()
    {
        global.println( "===== naiveBayesTest =====" )
        Array<Int32> labels = [ 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2 ]
        Tensor x = ClassicMlTest.makeClassData( labels, 3 )
        GaussianNB nb = GaussianNB( 3 )
        nb.fit( x, labels )
        Array<Int32> pred = nb.predict( x )
        global.println( "acc = " + SystemConvertString( ClassicMlTest.accuracy( pred, labels ) ) )
        check( "nb acc == 1", ClassicMlTest.accuracy( pred, labels ) == 1.0f )
    }

    # ── 6. 决策树桩 ─────────────────────────────────────
    static decisionStumpTest()
    {
        global.println( "===== decisionStumpTest =====" )
        Array<Int32> labels = [ 0, 0, 0, 0, 1, 1, 1, 1 ]
        Tensor x = ClassicMlTest.makeClassData( labels, 2 )
        DecisionStump stump = DecisionStump()
        stump.fit( x, labels, 0 )
        Array<Int32> pred = stump.predict( x )
        global.println( "threshold = " + SystemConvertString( stump.threshold ) )
        global.println( "acc = " + SystemConvertString( ClassicMlTest.accuracy( pred, labels ) ) )
        check( "stump acc == 1", ClassicMlTest.accuracy( pred, labels ) == 1.0f )
    }

    # ── 7. PCA ──────────────────────────────────────────
    static pcaTest()
    {
        global.println( "===== pcaTest =====" )
        # 一条近似直线的二维数据，主方向约为 (1,1)
        Tensor x = Tensor( Shape.matrix( 6, 2 ) )
        for i = 0, i < 6, i++
        {
            Float32 t = SystemConvertFloat32( i ) * 0.5f
            x.set( i, 0, t )
            x.set( i, 1, t + 0.05f )
        }
        PCA pca = PCA()
        pca.fit( x, 50 )
        Tensor z = pca.transform( x )
        global.println( "component = " + pca.component.toString() )
        global.println( "projection = " + z.toString() )
        check( "projection shape == (6,1)", z.rows() == 6 && z.cols() == 1 )
    }

    static fun()
    {
        global.println( "========== ClassicMlTest (start) ==========" )
        linearRegressionTest()
        logisticRegressionTest()
        knnTest()
        kmeansTest()
        naiveBayesTest()
        decisionStumpTest()
        pcaTest()
        global.println( "========== ClassicMlTest (end) ==========" )
    }
}
