# Optimizer —— 优化器
#
# 用法：
#   Optimizer opt = Adam(0.001f)
#   opt.step( model.parameters(), model.gradients() )
#
# 说明：状态（动量 / 二阶矩）按参数下标一一对应，首次 step 时惰性分配

@Nickname("Optimizer")
public class Optimizer
{
    public Float32 lr = 0.001f
    public Float32 weightDecay = 0.0f

    Int32 _step = 0
    Array<Tensor> _m = null
    Array<Tensor> _v = null

    public void _init_()
    {
        this.lr = 0.001f
        this._step = 0
        this._m = null
        this._v = null
    }

    public void _init_( Float32 lr )
    {
        this.lr = lr
        this._step = 0
        this._m = null
        this._v = null
    }

    public get int stepCount()
    {
        ret this._step
    }

    public void setLr( Float32 lr )
    {
        this.lr = lr
    }

    # 惰性分配状态张量（与参数同形状，初始为 0）
    void ensureState( Array<Tensor> parameters, Int32 slotCount )
    {
        if this._m == null
        {
            this._m = Array<Tensor>( parameters.length )
            for i = 0, i < parameters.length, i++
            {
                this._m[i] = Tensor.zerosLike( parameters[i] )
            }
        }
        if slotCount >= 2
        {
            if this._v == null
            {
                this._v = Array<Tensor>( parameters.length )
                for i = 0, i < parameters.length, i++
                {
                    this._v[i] = Tensor.zerosLike( parameters[i] )
                }
            }
        }
    }

    public Array<Tensor> state1()
    {
        ret this._m
    }

    public Array<Tensor> state2()
    {
        ret this._v
    }

    # 默认实现：朴素 SGD
    public virtual void step( Array<Tensor> parameters, Array<Tensor> gradients )
    {
        this._step = this._step + 1
        for i = 0, i < parameters.length, i++
        {
            parameters[i].subInPlace( gradients[i].scale( this.lr ) )
        }
    }

    public void zeroGrad( Array<Tensor> gradients )
    {
        for i = 0, i < gradients.length, i++
        {
            gradients[i].zero()
        }
    }

    override string toString()
    {
        ret "Optimizer(lr=" + SystemConvertString( this.lr ) + ")"
    }
}

@Nickname("SGD")
public class SGD extends Optimizer
{
    public Float32 momentum = 0.0f
    public bool nesterov = false

    public void _init_( Float32 lr )
    {
        this.lr = lr
        this.momentum = 0.0f
        this._m = null
        this._v = null
    }

    public void _init_( Float32 lr, Float32 momentum )
    {
        this.lr = lr
        this.momentum = momentum
        this._m = null
        this._v = null
    }

    override void step( Array<Tensor> parameters, Array<Tensor> gradients )
    {
        this._step = this._step + 1
        this.ensureState( parameters, 1 )
        for i = 0, i < parameters.length, i++
        {
            Tensor p = parameters[i]
            Tensor g = gradients[i]
            if this.momentum > 0.0f
            {
                this._m[i].scaleInPlace( this.momentum )
                this._m[i].addInPlace( g )
                Tensor upd = this._m[i]
                if this.nesterov
                {
                    upd = g.add( this._m[i].scale( this.momentum ) )
                }
                p.subInPlace( upd.scale( this.lr ) )
            }
            else
            {
                p.subInPlace( g.scale( this.lr ) )
            }
        }
    }
}

@Nickname("Adam")
public class Adam extends Optimizer
{
    public Float32 beta1 = 0.9f
    public Float32 beta2 = 0.999f
    public Float32 eps = 0.00000001f

    public void _init_( Float32 lr )
    {
        this.lr = lr
        this._m = null
        this._v = null
    }

    public void _init_( Float32 lr, Float32 beta1, Float32 beta2 )
    {
        this.lr = lr
        this.beta1 = beta1
        this.beta2 = beta2
        this._m = null
        this._v = null
    }

    override void step( Array<Tensor> parameters, Array<Tensor> gradients )
    {
        this._step = this._step + 1
        this.ensureState( parameters, 2 )
        Float32 bc1 = 1.0f - Mathf.pow( this.beta1, SystemConvertFloat32( this._step ) )
        Float32 bc2 = 1.0f - Mathf.pow( this.beta2, SystemConvertFloat32( this._step ) )
        if bc1 <= 0.0f
        {
            bc1 = 0.0000001f
        }
        if bc2 <= 0.0f
        {
            bc2 = 0.0000001f
        }
        for i = 0, i < parameters.length, i++
        {
            Tensor p = parameters[i]
            Tensor g = gradients[i]

            this._m[i].scaleInPlace( this.beta1 )
            this._m[i].addInPlace( g.scale( 1.0f - this.beta1 ) )

            Tensor gg = g.mul( g )
            this._v[i].scaleInPlace( this.beta2 )
            this._v[i].addInPlace( gg.scale( 1.0f - this.beta2 ) )

            for k = 0, k < p.size(), k++
            {
                Float32 mh = this._m[i].at( k ) / bc1
                Float32 vh = this._v[i].at( k ) / bc2
                Float32 upd = this.lr * mh / ( Mathf.sqrt( vh ) + this.eps )
                if this.weightDecay > 0.0f
                {
                    upd = upd + this.lr * this.weightDecay * p.at( k )
                }
                p.setAt( k, p.at( k ) - upd )
            }
        }
    }
}

@Nickname("AdamW")
public class AdamW extends Adam
{
    public void _init_( Float32 lr )
    {
        this.lr = lr
        this.weightDecay = 0.01f
        this._m = null
        this._v = null
    }

    public void _init_( Float32 lr, Float32 weightDecay )
    {
        this.lr = lr
        this.weightDecay = weightDecay
        this._m = null
        this._v = null
    }
}

@Nickname("RMSProp")
public class RMSProp extends Optimizer
{
    public Float32 alpha = 0.99f
    public Float32 eps = 0.00000001f

    public void _init_( Float32 lr )
    {
        this.lr = lr
        this._m = null
        this._v = null
    }

    override void step( Array<Tensor> parameters, Array<Tensor> gradients )
    {
        this._step = this._step + 1
        this.ensureState( parameters, 1 )
        for i = 0, i < parameters.length, i++
        {
            Tensor p = parameters[i]
            Tensor g = gradients[i]
            Tensor gg = g.mul( g )
            this._m[i].scaleInPlace( this.alpha )
            this._m[i].addInPlace( gg.scale( 1.0f - this.alpha ) )
            for k = 0, k < p.size(), k++
            {
                Float32 upd = this.lr * g.at( k ) / ( Mathf.sqrt( this._m[i].at( k ) ) + this.eps )
                p.setAt( k, p.at( k ) - upd )
            }
        }
    }
}

@Nickname("Adagrad")
public class Adagrad extends Optimizer
{
    public Float32 eps = 0.00000001f

    public void _init_( Float32 lr )
    {
        this.lr = lr
        this._m = null
        this._v = null
    }

    override void step( Array<Tensor> parameters, Array<Tensor> gradients )
    {
        this._step = this._step + 1
        this.ensureState( parameters, 1 )
        for i = 0, i < parameters.length, i++
        {
            Tensor p = parameters[i]
            Tensor g = gradients[i]
            this._m[i].addInPlace( g.mul( g ) )
            for k = 0, k < p.size(), k++
            {
                Float32 upd = this.lr * g.at( k ) / ( Mathf.sqrt( this._m[i].at( k ) ) + this.eps )
                p.setAt( k, p.at( k ) - upd )
            }
        }
    }
}
