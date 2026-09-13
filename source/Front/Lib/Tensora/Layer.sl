# Layer —— 所有网络层的基类
#
# 约定：
#   forward(x)      前向，子类必须缓存反传需要的中间量（inputCache / outputCache）
#   backward(g)     反向，返回传给下一层的梯度，同时把本层参数梯度累加进 _grads
#   update(lr)      默认 SGD 原地更新；接入 Optimizer 时由优化器接管
#   _params/_grads  一一对应：_grads[i] 是 _params[i] 的梯度

@Nickname("Layer")
public class Layer
{
    public string name = ""
    public bool training = true

    Array<Tensor> _params = null
    Array<Tensor> _grads = null

    public Tensor inputCache = null
    public Tensor outputCache = null

    public void _init_()
    {
        this.name = "Layer"
        this.training = true
        this._params = Array<Tensor>( 0 )
        this._grads = Array<Tensor>( 0 )
    }

    public void _init_( string name )
    {
        this.name = name
        this.training = true
        this._params = Array<Tensor>( 0 )
        this._grads = Array<Tensor>( 0 )
    }

    # ── 前向 / 反向 ──────────────────────────────────────
    public virtual Tensor forward( Tensor x )
    {
        this.inputCache = x
        this.outputCache = x
        ret x
    }

    public virtual Tensor backward( Tensor gradOutput )
    {
        ret gradOutput
    }

    # 默认 SGD 更新
    public virtual void update( Float32 lr )
    {
        this.ensureParams()
        for i = 0, i < this._params.length, i++
        {
            this._params[i].subInPlace( this._grads[i].scale( lr ) )
        }
    }

    # ── 参数管理 ─────────────────────────────────────────
    # 子类自定义 _init_ 时基类 _init_ 未必被调用，这里兜底初始化
    public void ensureParams()
    {
        if this._params == null
        {
            this._params = Array<Tensor>( 0 )
            this._grads = Array<Tensor>( 0 )
        }
    }

    public void addParam( Tensor p )
    {
        this.ensureParams()
        Array<Tensor> np = Array<Tensor>( this._params.length + 1 )
        Array<Tensor> ng = Array<Tensor>( this._grads.length + 1 )
        for i = 0, i < this._params.length, i++
        {
            np[i] = this._params[i]
            ng[i] = this._grads[i]
        }
        np[ this._params.length ] = p
        ng[ this._grads.length ] = Tensor.zerosLike( p )
        this._params = np
        this._grads = ng
    }

    # 复合层（MHA / TransformerBlock）通过重写这两个方法聚合子层参数
    public virtual Array<Tensor> parameters()
    {
        this.ensureParams()
        ret this._params
    }

    public virtual Array<Tensor> gradients()
    {
        this.ensureParams()
        ret this._grads
    }

    public virtual void zeroGrad()
    {
        this.ensureParams()
        for i = 0, i < this._grads.length, i++
        {
            this._grads[i].zero()
        }
    }

    public get int paramCount()
    {
        this.ensureParams()
        int n = 0
        for i = 0, i < this._params.length, i++
        {
            n = n + this._params[i].size()
        }
        ret n
    }

    # ── 训练 / 推理模式 ──────────────────────────────────
    public void train( bool mode )
    {
        this.training = mode
    }

    public void eval()
    {
        this.training = false
    }

    public virtual string summary()
    {
        ret this.name + " params=" + SystemConvertString( this.paramCount() )
    }
}
