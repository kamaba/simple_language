# Normalization —— 归一化层
#
# BatchNorm：沿 batch 维（列）归一化，训练用 batch 统计 + 滑动平均，推理用滑动统计
# LayerNorm ：沿特征维（行内）归一化，与 batch 无关，RNN / Transformer 常用

@Nickname("BatchNorm")
public class BatchNorm extends Layer
{
    Int32 _features = 0
    Float32 _momentum = 0.1f
    Float32 _eps = 0.00001f

    public Tensor gamma = null
    public Tensor beta = null
    public Tensor runningMean = null
    public Tensor runningVar = null

    Tensor _normalized = null
    Tensor _batchMean = null
    Tensor _batchVar = null

    public void _init_( Int32 features )
    {
        this.name = "BatchNorm"
        this._features = features
        this.gamma = Tensor.ones( Shape.matrix( 1, features ) )
        this.beta = Tensor( Shape.matrix( 1, features ) )
        this.runningMean = Tensor( Shape.matrix( 1, features ) )
        this.runningVar = Tensor.ones( Shape.matrix( 1, features ) )
        this.addParam( this.gamma )
        this.addParam( this.beta )
    }

    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        int n = x.rows()
        int d = x.cols()

        if this.training
        {
            Tensor mean = x.sumAlong( 0 ).scale( 1.0f / SystemConvertFloat32( n ) )
            Tensor diff = x.sub( mean )
            Tensor var = diff.mul( diff ).sumAlong( 0 ).scale( 1.0f / SystemConvertFloat32( n ) )
            Tensor std = Tensor( var.shape() )
            for i = 0, i < var.size(), i++
            {
                std.setAt( i, Mathf.sqrt( var.at( i ) + this._eps ) )
            }
            Tensor norm = diff.div( std )
            this._normalized = norm
            this._batchMean = mean
            this._batchVar = var

            # 滑动平均更新
            for i = 0, i < d, i++
            {
                Float32 m = this.runningMean.get( 0, i )
                Float32 v = this.runningVar.get( 0, i )
                this.runningMean.set( 0, i, m + ( mean.get( 0, i ) - m ) * this._momentum )
                this.runningVar.set( 0, i, v + ( var.get( 0, i ) - v ) * this._momentum )
            }
            this.outputCache = norm.mul( this.gamma ).add( this.beta )
            ret this.outputCache
        }

        Tensor diff2 = x.sub( this.runningMean )
        Tensor std2 = Tensor( this.runningVar.shape() )
        for i = 0, i < this.runningVar.size(), i++
        {
            std2.setAt( i, Mathf.sqrt( this.runningVar.at( i ) + this._eps ) )
        }
        this.outputCache = diff2.div( std2 ).mul( this.gamma ).add( this.beta )
        ret this.outputCache
    }

    override Tensor backward( Tensor gradOutput )
    {
        int n = gradOutput.rows()
        Float32 invN = 1.0f / SystemConvertFloat32( n )
        Array<Tensor> gs = this.gradients()

        # dgamma / dbeta
        for j = 0, j < gradOutput.cols(), j++
        {
            Float32 dg = 0.0f
            Float32 db = 0.0f
            for i = 0, i < n, i++
            {
                dg = dg + gradOutput.get( i, j ) * this._normalized.get( i, j )
                db = db + gradOutput.get( i, j )
            }
            gs[0].set( 0, j, gs[0].get( 0, j ) + dg )
            gs[1].set( 0, j, gs[1].get( 0, j ) + db )
        }

        # dx
        Tensor dx = Tensor( gradOutput.shape() )
        for j = 0, j < gradOutput.cols(), j++
        {
            Float32 sumG = 0.0f
            Float32 sumGN = 0.0f
            for i = 0, i < n, i++
            {
                sumG = sumG + gradOutput.get( i, j )
                sumGN = sumGN + gradOutput.get( i, j ) * this._normalized.get( i, j )
            }
            Float32 std = Mathf.sqrt( this._batchVar.get( 0, j ) + this._eps )
            for i = 0, i < n, i++
            {
                Float32 g = gradOutput.get( i, j )
                Float32 v = ( g - sumG * invN - this._normalized.get( i, j ) * sumGN * invN ) / std
                dx.set( i, j, v * this.gamma.get( 0, j ) )
            }
        }
        ret dx
    }

    override string summary()
    {
        ret "BatchNorm(" + SystemConvertString( this._features ) + ")"
    }
}

@Nickname("LayerNorm")
public class LayerNorm extends Layer
{
    Int32 _features = 0
    Float32 _eps = 0.00001f

    public Tensor gamma = null
    public Tensor beta = null
    Tensor _normalized = null

    public void _init_( Int32 features )
    {
        this.name = "LayerNorm"
        this._features = features
        this.gamma = Tensor.ones( Shape.matrix( 1, features ) )
        this.beta = Tensor( Shape.matrix( 1, features ) )
        this.addParam( this.gamma )
        this.addParam( this.beta )
    }

    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        int n = x.rows()
        int d = x.cols()
        Tensor out = Tensor( x.shape() )
        Tensor norm = Tensor( x.shape() )
        for i = 0, i < n, i++
        {
            Float32 mean = 0.0f
            for j = 0, j < d, j++
            {
                mean = mean + x.get( i, j )
            }
            mean = mean / SystemConvertFloat32( d )
            Float32 var = 0.0f
            for j = 0, j < d, j++
            {
                Float32 v = x.get( i, j ) - mean
                var = var + v * v
            }
            var = var / SystemConvertFloat32( d )
            Float32 std = Mathf.sqrt( var + this._eps )
            for j = 0, j < d, j++
            {
                Float32 nv = ( x.get( i, j ) - mean ) / std
                norm.set( i, j, nv )
                out.set( i, j, nv * this.gamma.get( 0, j ) + this.beta.get( 0, j ) )
            }
        }
        this._normalized = norm
        this.outputCache = out
        ret out
    }

    override Tensor backward( Tensor gradOutput )
    {
        int n = gradOutput.rows()
        int d = gradOutput.cols()
        Array<Tensor> gs = this.gradients()
        for j = 0, j < d, j++
        {
            Float32 dg = 0.0f
            Float32 db = 0.0f
            for i = 0, i < n, i++
            {
                dg = dg + gradOutput.get( i, j ) * this._normalized.get( i, j )
                db = db + gradOutput.get( i, j )
            }
            gs[0].set( 0, j, gs[0].get( 0, j ) + dg )
            gs[1].set( 0, j, gs[1].get( 0, j ) + db )
        }
        Tensor dx = Tensor( gradOutput.shape() )
        for i = 0, i < n, i++
        {
            Float32 sumG = 0.0f
            Float32 sumGN = 0.0f
            for j = 0, j < d, j++
            {
                sumG = sumG + gradOutput.get( i, j )
                sumGN = sumGN + gradOutput.get( i, j ) * this._normalized.get( i, j )
            }
            for j = 0, j < d, j++
            {
                Float32 v = gradOutput.get( i, j ) - sumG / SystemConvertFloat32( d ) - this._normalized.get( i, j ) * sumGN / SystemConvertFloat32( d )
                dx.set( i, j, v * this.gamma.get( 0, j ) )
            }
        }
        ret dx
    }

    override string summary()
    {
        ret "LayerNorm(" + SystemConvertString( this._features ) + ")"
    }
}
