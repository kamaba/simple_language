# Autograd —— 轻量自动微分（反向模式）
#
# 设计：
#   1. Variable 包装 Tensor，记录产生它的算子 op 与该算子的输入 parents
#   2. forward 时不做任何额外计算，只在反向时按 op 名字反推梯度
#   3. backward() 先对计算图做拓扑排序，再逆序调用每个节点的 propagate()
#   4. 广播产生的梯度用 Ops.reduceTo 还原到输入形状
#
# 说明：这是"够用就行"的教学级实现，不求覆盖所有算子与高阶导数

@Nickname("Variable")
public class Variable
{
    public Tensor data = null
    public Tensor grad = null
    public bool requiresGrad = false

    # 计算图：本节点由哪个算子、由哪些输入产生
    public Array<Variable> parents = null
    public string op = ""
    public Float32 param = 0.0f

    bool _visited = false

    # 全局开关：推理 / 参数冻结时关闭构图
    static bool _noGrad = false

    void _init_()
    {
        this.data = null
        this.requiresGrad = false
        this.op = ""
        this.parents = null
    }

    void _init_( Tensor data, bool requiresGrad )
    {
        this.data = data
        this.requiresGrad = requiresGrad
        this.op = ""
        this.parents = null
    }

    # ── 构造 ─────────────────────────────────────────────
    public static Variable of( Tensor data )
    {
        ret Variable( data, false )
    }

    public static Variable parameter( Tensor data )
    {
        ret Variable( data, true )
    }

    public static Variable constant( Float32 value )
    {
        ret Variable( Tensor.scalar( value ), false )
    }

    public static bool noGrad()
    {
        ret Variable._noGrad
    }

    public static void setNoGrad( bool value )
    {
        Variable._noGrad = value
    }

    # 生成一个新节点并挂到图上
    Variable emit( Tensor value, string opName, Array<Variable> inputs, bool needGrad )
    {
        Variable r = Variable( value, needGrad )
        if Variable._noGrad
        {
            ret r
        }
        r.op = opName
        r.parents = inputs
        ret r
    }

    # ── 前向算子 ─────────────────────────────────────────
    Variable add( Variable o )
    {
        Array<Variable> ins = Array<Variable>( 2 )
        ins[0] = this
        ins[1] = o
        bool need = this.requiresGrad || o.requiresGrad
        ret this.emit( this.data.add( o.data ), "add", ins, need )
    }

    Variable sub( Variable o )
    {
        Array<Variable> ins = Array<Variable>( 2 )
        ins[0] = this
        ins[1] = o
        bool need = this.requiresGrad || o.requiresGrad
        ret this.emit( this.data.sub( o.data ), "sub", ins, need )
    }

    Variable mul( Variable o )
    {
        Array<Variable> ins = Array<Variable>( 2 )
        ins[0] = this
        ins[1] = o
        bool need = this.requiresGrad || o.requiresGrad
        ret this.emit( this.data.mul( o.data ), "mul", ins, need )
    }

    Variable div( Variable o )
    {
        Array<Variable> ins = Array<Variable>( 2 )
        ins[0] = this
        ins[1] = o
        bool need = this.requiresGrad || o.requiresGrad
        ret this.emit( this.data.div( o.data ), "div", ins, need )
    }

    Variable matmul( Variable o )
    {
        Array<Variable> ins = Array<Variable>( 2 )
        ins[0] = this
        ins[1] = o
        bool need = this.requiresGrad || o.requiresGrad
        ret this.emit( this.data.matmul( o.data ), "matmul", ins, need )
    }

    Variable scale( Float32 k )
    {
        Array<Variable> ins = Array<Variable>( 1 )
        ins[0] = this
        Variable r = this.emit( this.data.scale( k ), "scale", ins, this.requiresGrad )
        r.param = k
        ret r
    }

    Variable relu()
    {
        Array<Variable> ins = Array<Variable>( 1 )
        ins[0] = this
        ret this.emit( this.data.relu(), "relu", ins, this.requiresGrad )
    }

    Variable sigmoid()
    {
        Array<Variable> ins = Array<Variable>( 1 )
        ins[0] = this
        ret this.emit( this.data.sigmoid(), "sigmoid", ins, this.requiresGrad )
    }

    Variable tanh()
    {
        Array<Variable> ins = Array<Variable>( 1 )
        ins[0] = this
        ret this.emit( this.data.tanh(), "tanh", ins, this.requiresGrad )
    }

    Variable exp()
    {
        Array<Variable> ins = Array<Variable>( 1 )
        ins[0] = this
        ret this.emit( this.data.exp(), "exp", ins, this.requiresGrad )
    }

    Variable log()
    {
        Array<Variable> ins = Array<Variable>( 1 )
        ins[0] = this
        ret this.emit( this.data.log(), "log", ins, this.requiresGrad )
    }

    Variable pow( Float32 p )
    {
        Array<Variable> ins = Array<Variable>( 1 )
        ins[0] = this
        Variable r = this.emit( this.data.pow( p ), "pow", ins, this.requiresGrad )
        r.param = p
        ret r
    }

    Variable sum()
    {
        Array<Variable> ins = Array<Variable>( 1 )
        ins[0] = this
        ret this.emit( Tensor.scalar( this.data.sum() ), "sum", ins, this.requiresGrad )
    }

    Variable mean()
    {
        Array<Variable> ins = Array<Variable>( 1 )
        ins[0] = this
        ret this.emit( Tensor.scalar( this.data.mean() ), "mean", ins, this.requiresGrad )
    }

    Variable softmax()
    {
        Array<Variable> ins = Array<Variable>( 1 )
        ins[0] = this
        ret this.emit( this.data.softmax(), "softmax", ins, this.requiresGrad )
    }

    Variable reshape( Shape s )
    {
        Array<Variable> ins = Array<Variable>( 1 )
        ins[0] = this
        ret this.emit( this.data.reshape( s ), "reshape", ins, this.requiresGrad )
    }

    Variable transpose()
    {
        Array<Variable> ins = Array<Variable>( 1 )
        ins[0] = this
        ret this.emit( this.data.transpose(), "transpose", ins, this.requiresGrad )
    }

    # ── 反向 ─────────────────────────────────────────────
    void backward()
    {
        this.backward( Tensor.onesLike( this.data ) )
    }

    void backward( Tensor seed )
    {
        this.grad = seed
        Array<Variable> order = Graph.topoSort( this )
        for i = order.length - 1, i >= 0, i--
        {
            order[i].propagate()
        }
    }

    # 梯度累加（同一节点被多次使用时）
    void accumulate( Tensor g )
    {
        if this.grad == null
        {
            this.grad = g.clone()
        }
        else
        {
            this.grad.addInPlace( g )
        }
    }

    void zeroGrad()
    {
        this.grad = null
    }

    # 按 op 把梯度推给 parents
    void propagate()
    {
        if this.grad == null
        {
            ret
        }
        if this.parents == null
        {
            ret
        }
        if this.op == "add"
        {
            this.parents[0].accumulate( Ops.reduceTo( this.grad, this.parents[0].data.shape() ) )
            this.parents[1].accumulate( Ops.reduceTo( this.grad, this.parents[1].data.shape() ) )
        }
        elif this.op == "sub"
        {
            this.parents[0].accumulate( Ops.reduceTo( this.grad, this.parents[0].data.shape() ) )
            this.parents[1].accumulate( Ops.reduceTo( this.grad.neg(), this.parents[1].data.shape() ) )
        }
        elif this.op == "mul"
        {
            Tensor a = this.parents[0].data
            Tensor b = this.parents[1].data
            this.parents[0].accumulate( Ops.reduceTo( this.grad.mul( b ), a.shape() ) )
            this.parents[1].accumulate( Ops.reduceTo( this.grad.mul( a ), b.shape() ) )
        }
        elif this.op == "div"
        {
            Tensor a2 = this.parents[0].data
            Tensor b2 = this.parents[1].data
            this.parents[0].accumulate( Ops.reduceTo( this.grad.div( b2 ), a2.shape() ) )
            Tensor gb = this.grad.mul( a2 ).div( b2.mul( b2 ) ).neg()
            this.parents[1].accumulate( Ops.reduceTo( gb, b2.shape() ) )
        }
        elif this.op == "matmul"
        {
            Tensor a3 = this.parents[0].data
            Tensor b3 = this.parents[1].data
            this.parents[0].accumulate( this.grad.matmul( b3.transpose() ) )
            this.parents[1].accumulate( a3.transpose().matmul( this.grad ) )
        }
        elif this.op == "scale"
        {
            this.parents[0].accumulate( this.grad.scale( this.param ) )
        }
        elif this.op == "relu"
        {
            Tensor a4 = this.parents[0].data
            Tensor mask = Tensor( a4.shape() )
            for i = 0, i < a4.size(), i++
            {
                if a4.at( i ) > 0.0f
                {
                    mask.setAt( i, 1.0f )
                }
                else
                {
                    mask.setAt( i, 0.0f )
                }
            }
            this.parents[0].accumulate( this.grad.mul( mask ) )
        }
        elif this.op == "sigmoid"
        {
            Tensor s = this.data
            this.parents[0].accumulate( this.grad.mul( s ).mul( Tensor.onesLike( s ).sub( s ) ) )
        }
        elif this.op == "tanh"
        {
            Tensor t = this.data
            this.parents[0].accumulate( this.grad.mul( Tensor.onesLike( t ).sub( t.mul( t ) ) ) )
        }
        elif this.op == "exp"
        {
            this.parents[0].accumulate( this.grad.mul( this.data ) )
        }
        elif this.op == "log"
        {
            this.parents[0].accumulate( this.grad.div( this.parents[0].data ) )
        }
        elif this.op == "pow"
        {
            Tensor a5 = this.parents[0].data
            Tensor gp = this.grad.mul( a5.pow( this.param - 1.0f ) ).scale( this.param )
            this.parents[0].accumulate( gp )
        }
        elif this.op == "sum"
        {
            Tensor a6 = this.parents[0].data
            this.parents[0].accumulate( Tensor.full( a6.shape(), this.grad.sum() ) )
        }
        elif this.op == "mean"
        {
            Tensor a7 = this.parents[0].data
            Float32 v = this.grad.sum() / SystemConvertFloat32( a7.size() )
            this.parents[0].accumulate( Tensor.full( a7.shape(), v ) )
        }
        elif this.op == "softmax"
        {
            # J = diag(s) - s·sᵀ  →  gx = s * (g - Σ(g*s))
            Tensor s2 = this.data
            Tensor gx = Tensor( s2.shape() )
            for i = 0, i < s2.rows(), i++
            {
                Float32 dot = 0.0f
                for j = 0, j < s2.cols(), j++
                {
                    dot = dot + this.grad.get( i, j ) * s2.get( i, j )
                }
                for j = 0, j < s2.cols(), j++
                {
                    gx.set( i, j, s2.get( i, j ) * ( this.grad.get( i, j ) - dot ) )
                }
            }
            this.parents[0].accumulate( gx )
        }
        elif this.op == "reshape"
        {
            this.parents[0].accumulate( this.grad.reshape( this.parents[0].data.shape() ) )
        }
        elif this.op == "transpose"
        {
            this.parents[0].accumulate( this.grad.transpose() )
        }
    }

    override string toString()
    {
        string s = "Variable(op=" + this.op + ", grad="
        if this.grad == null
        {
            s = s + "none"
        }
        else
        {
            s = s + "yes"
        }
        ret s + ")"
    }
}

# Graph —— 拓扑排序（DFS 后序）
public class Graph
{
    static List<Variable> _order = null
    static List<Variable> _marks = null

    public static Array<Variable> topoSort( Variable root )
    {
        Graph._order = List<Variable>()
        Graph._marks = List<Variable>()
        Graph.dfs( root )
        for i = 0, i < Graph._marks.length, i++
        {
            Graph._marks[i]._visited = false
        }
        Array<Variable> arr = Array<Variable>( Graph._order.length )
        for i = 0, i < Graph._order.length, i++
        {
            arr[i] = Graph._order[i]
        }
        ret arr
    }

    static void dfs( Variable node )
    {
        if node == null
        {
            ret
        }
        if node._visited
        {
            ret
        }
        node._visited = true
        Graph._marks.add( node )
        if node.parents != null
        {
            for i = 0, i < node.parents.length, i++
            {
                Graph.dfs( node.parents[i] )
            }
        }
        Graph._order.add( node )
    }
}
