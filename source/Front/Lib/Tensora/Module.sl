# Module / Sequential —— 网络容器
#
# 负责：层堆叠、前向串联、反向串联、参数与梯度的收集、统一更新与清零

@Nickname("Module")
public class Module
{
    public string name = "Module"
    public bool training = true

    Array<Layer> _layers = null

    public void _init_()
    {
        this.name = "Module"
        this.training = true
        this._layers = Array<Layer>( 0 )
    }

    public void _init_( string name )
    {
        this.name = name
        this.training = true
        this._layers = Array<Layer>( 0 )
    }

    # ── 结构 ─────────────────────────────────────────────
    # 子类自定义 _init_ 时基类 _init_ 未必被调用，这里兜底初始化
    public void ensureLayers()
    {
        if this._layers == null
        {
            this._layers = Array<Layer>( 0 )
        }
    }

    public void add( Layer l )
    {
        this.ensureLayers()
        Array<Layer> nl = Array<Layer>( this._layers.length + 1 )
        for i = 0, i < this._layers.length, i++
        {
            nl[i] = this._layers[i]
        }
        nl[ this._layers.length ] = l
        this._layers = nl
    }

    public Array<Layer> layers()
    {
        this.ensureLayers()
        ret this._layers
    }

    public get int layerCount()
    {
        this.ensureLayers()
        ret this._layers.length
    }

    # ── 前向 / 反向 ──────────────────────────────────────
    public virtual Tensor forward( Tensor x )
    {
        this.ensureLayers()
        Tensor cur = x
        for i = 0, i < this._layers.length, i++
        {
            cur = this._layers[i].forward( cur )
        }
        ret cur
    }

    public virtual Tensor backward( Tensor gradOutput )
    {
        this.ensureLayers()
        Tensor g = gradOutput
        for i = this._layers.length - 1, i >= 0, i--
        {
            g = this._layers[i].backward( g )
        }
        ret g
    }

    # ── 参数 ─────────────────────────────────────────────
    public Array<Tensor> parameters()
    {
        this.ensureLayers()
        List<Tensor> all = List<Tensor>()
        for i = 0, i < this._layers.length, i++
        {
            Array<Tensor> ps = this._layers[i].parameters()
            for j = 0, j < ps.length, j++
            {
                all.add( ps[j] )
            }
        }
        Array<Tensor> arr = Array<Tensor>( all.length )
        for i = 0, i < all.length, i++
        {
            arr[i] = all[i]
        }
        ret arr
    }

    public Array<Tensor> gradients()
    {
        this.ensureLayers()
        List<Tensor> all = List<Tensor>()
        for i = 0, i < this._layers.length, i++
        {
            Array<Tensor> gs = this._layers[i].gradients()
            for j = 0, j < gs.length, j++
            {
                all.add( gs[j] )
            }
        }
        Array<Tensor> arr = Array<Tensor>( all.length )
        for i = 0, i < all.length, i++
        {
            arr[i] = all[i]
        }
        ret arr
    }

    public get int paramCount()
    {
        this.ensureLayers()
        int n = 0
        for i = 0, i < this._layers.length, i++
        {
            n = n + this._layers[i].paramCount()
        }
        ret n
    }

    public void zeroGrad()
    {
        this.ensureLayers()
        for i = 0, i < this._layers.length, i++
        {
            this._layers[i].zeroGrad()
        }
    }

    # 默认 SGD 更新（接 Optimizer 时不用它）
    public void update( Float32 lr )
    {
        this.ensureLayers()
        for i = 0, i < this._layers.length, i++
        {
            this._layers[i].update( lr )
        }
    }

    # ── 模式 ─────────────────────────────────────────────
    public void train( bool mode )
    {
        this.ensureLayers()
        this.training = mode
        for i = 0, i < this._layers.length, i++
        {
            this._layers[i].train( mode )
        }
    }

    public void eval()
    {
        this.train( false )
    }

    public string summary()
    {
        this.ensureLayers()
        string s = this.name + " ["
        for i = 0, i < this._layers.length, i++
        {
            if i > 0
            {
                s = s + " -> "
            }
            s = s + this._layers[i].summary()
        }
        s = s + "] total=" + SystemConvertString( this.paramCount() )
        ret s
    }
}

# Sequential —— 严格顺序执行的容器（与 Module 同构，语义更明确）
@Nickname("Seq")
public class Sequential extends Module
{
    public void _init_()
    {
        this.name = "Sequential"
        this.training = true
        this._layers = Array<Layer>( 0 )
    }

    public void append( Layer l )
    {
        this.add( l )
    }
}
