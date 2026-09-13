# Scheduler —— 学习率调度
#
# 用法：
#   LRScheduler sch = CosineLR(opt, totalSteps)
#   每个 epoch / step 之后调用 sch.step()，内部会把新学习率写回 optimizer

@Nickname("LRScheduler")
public class LRScheduler
{
    public Optimizer optimizer = null
    public Float32 baseLr = 0.001f
    Int32 _step = 0

    public void _init_( Optimizer optimizer )
    {
        this.optimizer = optimizer
        this.baseLr = optimizer.lr
        this._step = 0
    }

    public get int stepCount()
    {
        ret this._step
    }

    public virtual Float32 getLr()
    {
        ret this.baseLr
    }

    # 推进一步并把学习率写回优化器
    public virtual void step()
    {
        this._step = this._step + 1
        this.optimizer.setLr( this.getLr() )
    }
}

# 每隔 stepSize 步乘以 gamma
@Nickname("StepLR")
public class StepLR extends LRScheduler
{
    public Int32 stepSize = 10
    public Float32 gamma = 0.1f

    public void _init_( Optimizer optimizer, Int32 stepSize, Float32 gamma )
    {
        this.optimizer = optimizer
        this.baseLr = optimizer.lr
        this.stepSize = stepSize
        this.gamma = gamma
        this._step = 0
    }

    override Float32 getLr()
    {
        int k = this._step / this.stepSize
        ret this.baseLr * Mathf.pow( this.gamma, SystemConvertFloat32( k ) )
    }
}

# 每步乘以 gamma
@Nickname("ExpLR")
public class ExponentialLR extends LRScheduler
{
    public Float32 gamma = 0.99f

    public void _init_( Optimizer optimizer, Float32 gamma )
    {
        this.optimizer = optimizer
        this.baseLr = optimizer.lr
        this.gamma = gamma
        this._step = 0
    }

    override Float32 getLr()
    {
        ret this.baseLr * Mathf.pow( this.gamma, SystemConvertFloat32( this._step ) )
    }
}

# 余弦退火：lr = min + (base - min) * (1 + cos(pi * t / T)) / 2
@Nickname("CosineLR")
public class CosineLR extends LRScheduler
{
    public Int32 totalSteps = 100
    public Float32 minLr = 0.0f

    public void _init_( Optimizer optimizer, Int32 totalSteps )
    {
        this.optimizer = optimizer
        this.baseLr = optimizer.lr
        this.totalSteps = totalSteps
        this._step = 0
    }

    override Float32 getLr()
    {
        Float32 t = SystemConvertFloat32( this._step )
        Float32 T = SystemConvertFloat32( this.totalSteps )
        Float32 cosv = Mathf.cos( 3.141592653589793f * t / T )
        ret this.minLr + ( this.baseLr - this.minLr ) * ( 1.0f + cosv ) * 0.5f
    }
}

# 线性 warmup：前 warmupSteps 步从 0 线性升到 baseLr，之后保持不变
@Nickname("WarmupLR")
public class WarmupLR extends LRScheduler
{
    public Int32 warmupSteps = 1000

    public void _init_( Optimizer optimizer, Int32 warmupSteps )
    {
        this.optimizer = optimizer
        this.baseLr = optimizer.lr
        this.warmupSteps = warmupSteps
        this._step = 0
    }

    override Float32 getLr()
    {
        if this._step >= this.warmupSteps
        {
            ret this.baseLr
        }
        Float32 t = SystemConvertFloat32( this._step )
        Float32 w = SystemConvertFloat32( this.warmupSteps )
        ret this.baseLr * t / w
    }
}

# 指标停滞时降低学习率
@Nickname("ReduceLROnPlateau")
public class ReduceLROnPlateau extends LRScheduler
{
    public Float32 factor = 0.5f
    public Int32 patience = 5
    public Float32 best = 0.0f
    Int32 _wait = 0
    public Float32 current = 0.0f

    public void _init_( Optimizer optimizer, Int32 patience, Float32 factor )
    {
        this.optimizer = optimizer
        this.baseLr = optimizer.lr
        this.patience = patience
        this.factor = factor
        this.best = 1000000000.0f
        this._wait = 0
        this.current = this.baseLr
        this._step = 0
    }

    # 传入本轮的验证指标（如 loss），越小越好
    public void report( Float32 metric )
    {
        this._step = this._step + 1
        if metric < this.best - 0.000001f
        {
            this.best = metric
            this._wait = 0
            ret
        }
        this._wait = this._wait + 1
        if this._wait >= this.patience
        {
            this._wait = 0
            this.current = this.current * this.factor
            this.optimizer.setLr( this.current )
        }
    }

    override Float32 getLr()
    {
        ret this.current
    }
}
