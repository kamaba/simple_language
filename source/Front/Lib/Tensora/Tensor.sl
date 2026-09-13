# Tensor —— 多维张量（Float32，行主序扁平存储）
#
# 存储：_data 一维数组 + _shape（维度）+ _strides（步长）
# 语义：
#   - 逐元素运算均支持广播（add / sub / mul / div）
#   - matmul 目前实现 2D × 2D
#   - 所有 *_InPlace / copyFrom 接口供优化器与反向传播原地更新使用

@Nickname("Tensor")
public class Tensor
{
    Array<Float32> _data = null
    Shape _shape = null
    Array<Int32> _strides = null

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this._shape = Shape.scalar()
        this._strides = this._shape.strides()
        this._data = Array<Float32>( 1 )
        this._data[0] = 0.0f
    }

    public void _init_( Shape shape )
    {
        this._shape = shape
        this._strides = shape.strides()
        this._data = Array<Float32>( shape.size() )
        this.zero()
    }

    public void _init_( Shape shape, Float32 fillValue )
    {
        this._shape = shape
        this._strides = shape.strides()
        this._data = Array<Float32>( shape.size() )
        this.fillValue( fillValue )
    }

    public void _init_( Array<Float32> values, Shape shape )
    {
        this._shape = shape
        this._strides = shape.strides()
        this._data = Array<Float32>( shape.size() )
        for i = 0, i < this._data.length, i++
        {
            this._data[i] = values[i]
        }
    }

    public void _init_( Int32 rows, Int32 cols )
    {
        this._shape = Shape.matrix( rows, cols )
        this._strides = this._shape.strides()
        this._data = Array<Float32>( rows * cols )
        this.zero()
    }

    # ── 属性 ─────────────────────────────────────────────
    get Shape shape()
    {
        ret this._shape
    }

    get int rank()
    {
        ret this._shape.rank()
    }

    get int size()
    {
        ret this._data.length
    }

    get int rows()
    {
        ret this._shape.dims[0]
    }

    get int cols()
    {
        ret this._shape.dims[ this._shape.rank() - 1 ]
    }

    get Array<Float32> data()
    {
        ret this._data
    }

    # ── 索引访问 ─────────────────────────────────────────
    int offset( Array<Int32> index )
    {
        ret this._shape.offset( index )
    }

    Float32 at( int flatIndex )
    {
        ret this._data[ flatIndex ]
    }

    void setAt( int flatIndex, Float32 value )
    {
        this._data[ flatIndex ] = value
    }

    Float32 get( Array<Int32> index )
    {
        ret this._data[ this._shape.offset( index ) ]
    }

    void set( Array<Int32> index, Float32 value )
    {
        this._data[ this._shape.offset( index ) ] = value
    }

    Float32 get( int i )
    {
        ret this._data[ i ]
    }

    void set( int i, Float32 value )
    {
        this._data[ i ] = value
    }

    Float32 get( int i, int j )
    {
        ret this._data[ i * this._strides[0] + j * this._strides[1] ]
    }

    void set( int i, int j, Float32 value )
    {
        this._data[ i * this._strides[0] + j * this._strides[1] ] = value
    }

    Float32 get( int i, int j, int k )
    {
        ret this._data[ i * this._strides[0] + j * this._strides[1] + k * this._strides[2] ]
    }

    void set( int i, int j, int k, Float32 value )
    {
        this._data[ i * this._strides[0] + j * this._strides[1] + k * this._strides[2] ] = value
    }

    Float32 get( int i, int j, int k, int l )
    {
        ret this._data[ i * this._strides[0] + j * this._strides[1] + k * this._strides[2] + l * this._strides[3] ]
    }

    void set( int i, int j, int k, int l, Float32 value )
    {
        this._data[ i * this._strides[0] + j * this._strides[1] + k * this._strides[2] + l * this._strides[3] ] = value
    }

    # 广播取值：把目标形状的索引映射到本张量
    Float32 pick( Array<Int32> index )
    {
        int r = this.rank()
        Array<Int32> own = Array<Int32>( r )
        int shift = index.length - r
        for i = 0, i < r, i++
        {
            int src = shift + i
            if src < 0
            {
                own[i] = 0
            }
            else
            {
                if this._shape.dims[i] == 1
                {
                    own[i] = 0
                }
                else
                {
                    own[i] = index[ src ]
                }
            }
        }
        ret this._data[ this._shape.offset( own ) ]
    }

    # ── 拷贝 / 填充 ──────────────────────────────────────
    Tensor clone()
    {
        Tensor r = Tensor( this._shape.clone() )
        for i = 0, i < this._data.length, i++
        {
            r._data[i] = this._data[i]
        }
        ret r
    }

    void copyFrom( Tensor src )
    {
        for i = 0, i < this._data.length, i++
        {
            this._data[i] = src._data[i]
        }
    }

    void fillValue( Float32 value )
    {
        for i = 0, i < this._data.length, i++
        {
            this._data[i] = value
        }
    }

    void zero()
    {
        this.fillValue( 0.0f )
    }

    bool sameShape( Tensor other )
    {
        ret this._shape.equals( other.shape() )
    }

    # ── 形状变换 ─────────────────────────────────────────
    Tensor reshape( Shape newShape )
    {
        Tensor r = Tensor( newShape )
        for i = 0, i < this._data.length, i++
        {
            r._data[i] = this._data[i]
        }
        ret r
    }

    Tensor flatten()
    {
        ret this.reshape( Shape.vector( this._data.length ) )
    }

    # 2D 转置
    Tensor transpose()
    {
        int m = this.rows()
        int n = this.cols()
        Tensor r = Tensor( Shape.matrix( n, m ) )
        for i = 0, i < m, i++
        {
            for j = 0, j < n, j++
            {
                r.set( j, i, this.get( i, j ) )
            }
        }
        ret r
    }

    # 通用轴置换：axes 为新的轴顺序
    Tensor transpose( Array<Int32> axes )
    {
        int r = this.rank()
        Array<Int32> newDims = Array<Int32>( r )
        for i = 0, i < r, i++
        {
            newDims[i] = this._shape.dims[ axes[i] ]
        }
        Shape outShape = Shape( newDims )
        Tensor res = Tensor( outShape )
        Array<Int32> src = Array<Int32>( r )
        for f = 0, f < this._data.length, f++
        {
            Array<Int32> idx = this._shape.unflatten( f )
            for a = 0, a < r, a++
            {
                src[ axes[a] ] = idx[a]
            }
            res.setAt( outShape.offset( src ), this._data[f] )
        }
        ret res
    }

    # 沿第 0 维切片 [start, start + count)
    Tensor slice( Int32 start, Int32 count )
    {
        int r = this.rank()
        Array<Int32> newDims = Array<Int32>( r )
        for i = 0, i < r, i++
        {
            newDims[i] = this._shape.dims[i]
        }
        newDims[0] = count
        Shape outShape = Shape( newDims )
        Tensor res = Tensor( outShape )
        int rowSize = this._data.length / this._shape.dims[0]
        int base = start * rowSize
        for i = 0, i < count * rowSize, i++
        {
            res._data[i] = this._data[ base + i ]
        }
        ret res
    }

    # 取第 r 行（保持二维：1 × cols）
    Tensor row( Int32 r )
    {
        int n = this.cols()
        Tensor res = Tensor( Shape.matrix( 1, n ) )
        for j = 0, j < n, j++
        {
            res.set( 0, j, this.get( r, j ) )
        }
        ret res
    }

    # 写回第 r 行
    void setRow( Int32 r, Tensor v )
    {
        for j = 0, j < this.cols(), j++
        {
            this.set( r, j, v.get( j ) )
        }
    }

    # ── 逐元素运算（支持广播）─────────────────────────────
    Tensor add( Tensor b )
    {
        Shape out = Shape.broadcastShape( this._shape, b.shape() )
        Tensor r = Tensor( out )
        for f = 0, f < out.size(), f++
        {
            Array<Int32> idx = out.unflatten( f )
            r.setAt( f, this.pick( idx ) + b.pick( idx ) )
        }
        ret r
    }

    Tensor sub( Tensor b )
    {
        Shape out = Shape.broadcastShape( this._shape, b.shape() )
        Tensor r = Tensor( out )
        for f = 0, f < out.size(), f++
        {
            Array<Int32> idx = out.unflatten( f )
            r.setAt( f, this.pick( idx ) - b.pick( idx ) )
        }
        ret r
    }

    Tensor mul( Tensor b )
    {
        Shape out = Shape.broadcastShape( this._shape, b.shape() )
        Tensor r = Tensor( out )
        for f = 0, f < out.size(), f++
        {
            Array<Int32> idx = out.unflatten( f )
            r.setAt( f, this.pick( idx ) * b.pick( idx ) )
        }
        ret r
    }

    Tensor div( Tensor b )
    {
        Shape out = Shape.broadcastShape( this._shape, b.shape() )
        Tensor r = Tensor( out )
        for f = 0, f < out.size(), f++
        {
            Array<Int32> idx = out.unflatten( f )
            r.setAt( f, this.pick( idx ) / b.pick( idx ) )
        }
        ret r
    }

    Tensor scale( Float32 k )
    {
        Tensor r = Tensor( this._shape.clone() )
        for i = 0, i < this._data.length, i++
        {
            r._data[i] = this._data[i] * k
        }
        ret r
    }

    Tensor addScalar( Float32 k )
    {
        Tensor r = Tensor( this._shape.clone() )
        for i = 0, i < this._data.length, i++
        {
            r._data[i] = this._data[i] + k
        }
        ret r
    }

    Tensor neg()
    {
        ret this.scale( -1.0f )
    }

    # ── 矩阵乘法（2D × 2D）──────────────────────────────
    Tensor matmul( Tensor b )
    {
        int m = this.rows()
        int k = this.cols()
        int n = b.cols()
        Tensor r = Tensor( Shape.matrix( m, n ) )
        for i = 0, i < m, i++
        {
            for j = 0, j < n, j++
            {
                Float32 sum = 0.0f
                for p = 0, p < k, p++
                {
                    sum = sum + this.get( i, p ) * b.get( p, j )
                }
                r.set( i, j, sum )
            }
        }
        ret r
    }

    # ── 原地运算（优化器 / 反传用）───────────────────────
    void addInPlace( Tensor b )
    {
        for i = 0, i < this._data.length, i++
        {
            this._data[i] = this._data[i] + b.pick( this._shape.unflatten( i ) )
        }
    }

    void subInPlace( Tensor b )
    {
        for i = 0, i < this._data.length, i++
        {
            this._data[i] = this._data[i] - b.pick( this._shape.unflatten( i ) )
        }
    }

    void mulInPlace( Tensor b )
    {
        for i = 0, i < this._data.length, i++
        {
            this._data[i] = this._data[i] * b.pick( this._shape.unflatten( i ) )
        }
    }

    void scaleInPlace( Float32 k )
    {
        for i = 0, i < this._data.length, i++
        {
            this._data[i] = this._data[i] * k
        }
    }

    # ── 归约 ─────────────────────────────────────────────
    Float32 sum()
    {
        Float32 s = 0.0f
        for i = 0, i < this._data.length, i++
        {
            s = s + this._data[i]
        }
        ret s
    }

    Float32 mean()
    {
        ret this.sum() / SystemConvertFloat32( this._data.length )
    }

    Float32 max()
    {
        Float32 m = this._data[0]
        for i = 1, i < this._data.length, i++
        {
            if this._data[i] > m
            {
                m = this._data[i]
            }
        }
        ret m
    }

    Float32 min()
    {
        Float32 m = this._data[0]
        for i = 1, i < this._data.length, i++
        {
            if this._data[i] < m
            {
                m = this._data[i]
            }
        }
        ret m
    }

    int argmax()
    {
        int best = 0
        Float32 m = this._data[0]
        for i = 1, i < this._data.length, i++
        {
            if this._data[i] > m
            {
                m = this._data[i]
                best = i
            }
        }
        ret best
    }

    int argmin()
    {
        int best = 0
        Float32 m = this._data[0]
        for i = 1, i < this._data.length, i++
        {
            if this._data[i] < m
            {
                m = this._data[i]
                best = i
            }
        }
        ret best
    }

    # 沿指定轴求和（axis=0 压缩行，axis=1 压缩列）
    Tensor sumAlong( Int32 axis )
    {
        ret this.reduceAlong( axis, 0 )
    }

    Tensor meanAlong( Int32 axis )
    {
        ret this.reduceAlong( axis, 1 )
    }

    Tensor maxAlong( Int32 axis )
    {
        ret this.reduceAlong( axis, 2 )
    }

    # mode: 0=sum 1=mean 2=max
    Tensor reduceAlong( Int32 axis, Int32 mode )
    {
        int m = this._shape.dims[0]
        int n = this._shape.dims[1]
        if axis == 0
        {
            Tensor r = Tensor( Shape.matrix( 1, n ) )
            for j = 0, j < n, j++
            {
                Float32 acc = 0.0f
                for i = 0, i < m, i++
                {
                    if mode == 2
                    {
                        if i == 0
                        {
                            acc = this.get( i, j )
                        }
                        elif this.get( i, j ) > acc
                        {
                            acc = this.get( i, j )
                        }
                    }
                    else
                    {
                        acc = acc + this.get( i, j )
                    }
                }
                if mode == 1
                {
                    acc = acc / SystemConvertFloat32( m )
                }
                r.set( 0, j, acc )
            }
            ret r
        }
        Tensor r2 = Tensor( Shape.matrix( m, 1 ) )
        for i = 0, i < m, i++
        {
            Float32 acc2 = 0.0f
            for j = 0, j < n, j++
            {
                if mode == 2
                {
                    if j == 0
                    {
                        acc2 = this.get( i, j )
                    }
                    elif this.get( i, j ) > acc2
                    {
                        acc2 = this.get( i, j )
                    }
                }
                else
                {
                    acc2 = acc2 + this.get( i, j )
                }
            }
            if mode == 1
            {
                acc2 = acc2 / SystemConvertFloat32( n )
            }
            r2.set( i, 0, acc2 )
        }
        ret r2
    }

    # ── 数学逐元素（AI 常用）─────────────────────────────
    Tensor exp()
    {
        Tensor r = Tensor( this._shape.clone() )
        for i = 0, i < this._data.length, i++
        {
            r._data[i] = TensorMath.exp( this._data[i] )
        }
        ret r
    }

    Tensor log()
    {
        Tensor r = Tensor( this._shape.clone() )
        for i = 0, i < this._data.length, i++
        {
            r._data[i] = TensorMath.safeLog( this._data[i] )
        }
        ret r
    }

    Tensor sqrt()
    {
        Tensor r = Tensor( this._shape.clone() )
        for i = 0, i < this._data.length, i++
        {
            r._data[i] = Mathf.sqrt( this._data[i] )
        }
        ret r
    }

    Tensor abs()
    {
        Tensor r = Tensor( this._shape.clone() )
        for i = 0, i < this._data.length, i++
        {
            r._data[i] = TensorMath.abs( this._data[i] )
        }
        ret r
    }

    Tensor pow( Float32 p )
    {
        Tensor r = Tensor( this._shape.clone() )
        for i = 0, i < this._data.length, i++
        {
            r._data[i] = Mathf.pow( this._data[i], p )
        }
        ret r
    }

    Tensor clamp( Float32 lo, Float32 hi )
    {
        Tensor r = Tensor( this._shape.clone() )
        for i = 0, i < this._data.length, i++
        {
            r._data[i] = TensorMath.clamp( this._data[i], lo, hi )
        }
        ret r
    }

    Tensor relu()
    {
        Tensor r = Tensor( this._shape.clone() )
        for i = 0, i < this._data.length, i++
        {
            r._data[i] = TensorMath.relu( this._data[i] )
        }
        ret r
    }

    Tensor sigmoid()
    {
        Tensor r = Tensor( this._shape.clone() )
        for i = 0, i < this._data.length, i++
        {
            r._data[i] = TensorMath.sigmoid( this._data[i] )
        }
        ret r
    }

    Tensor tanh()
    {
        Tensor r = Tensor( this._shape.clone() )
        for i = 0, i < this._data.length, i++
        {
            r._data[i] = TensorMath.tanh( this._data[i] )
        }
        ret r
    }

    # 对最后一维做 softmax（逐行）
    Tensor softmax()
    {
        Tensor r = Tensor( this._shape.clone() )
        int m = this._shape.dims[0]
        int n = this.cols()
        for i = 0, i < m, i++
        {
            Float32 mx = this.get( i, 0 )
            for j = 1, j < n, j++
            {
                if this.get( i, j ) > mx
                {
                    mx = this.get( i, j )
                }
            }
            Float32 denom = 0.0f
            for j = 0, j < n, j++
            {
                denom = denom + TensorMath.exp( this.get( i, j ) - mx )
            }
            for j = 0, j < n, j++
            {
                r.set( i, j, TensorMath.exp( this.get( i, j ) - mx ) / denom )
            }
        }
        ret r
    }

    Tensor logSoftmax()
    {
        Tensor r = Tensor( this._shape.clone() )
        int m = this._shape.dims[0]
        int n = this.cols()
        for i = 0, i < m, i++
        {
            Float32 mx = this.get( i, 0 )
            Float32 denom = 0.0f
            for j = 0, j < n, j++
            {
                if this.get( i, j ) > mx
                {
                    mx = this.get( i, j )
                }
            }
            for j = 0, j < n, j++
            {
                denom = denom + TensorMath.exp( this.get( i, j ) - mx )
            }
            Float32 logSum = mx + TensorMath.safeLog( denom )
            for j = 0, j < n, j++
            {
                r.set( i, j, this.get( i, j ) - logSum )
            }
        }
        ret r
    }

    # ── 静态工厂 ─────────────────────────────────────────
    public static Tensor zeros( Shape shape )
    {
        ret Tensor( shape, 0.0f )
    }

    public static Tensor ones( Shape shape )
    {
        ret Tensor( shape, 1.0f )
    }

    public static Tensor full( Shape shape, Float32 value )
    {
        ret Tensor( shape, value )
    }

    public static Tensor scalar( Float32 value )
    {
        Tensor t = Tensor( Shape.scalar() )
        t.setAt( 0, value )
        ret t
    }

    public static Tensor vector( Array<Float32> values )
    {
        ret Tensor( values, Shape.vector( values.length ) )
    }

    public static Tensor matrix( Array<Float32> values, Int32 rows, Int32 cols )
    {
        ret Tensor( values, Shape.matrix( rows, cols ) )
    }

    public static Tensor identity( Int32 n )
    {
        Tensor t = Tensor( Shape.matrix( n, n ) )
        for i = 0, i < n, i++
        {
            t.set( i, i, 1.0f )
        }
        ret t
    }

    # 均匀分布 [0,1)
    public static Tensor random( Shape shape )
    {
        Tensor t = Tensor( shape )
        for i = 0, i < t._data.length, i++
        {
            t._data[i] = Rng.global().nextFloat()
        }
        ret t
    }

    # 标准正态分布
    public static Tensor randn( Shape shape )
    {
        Tensor t = Tensor( shape )
        for i = 0, i < t._data.length, i++
        {
            t._data[i] = Rng.global().nextNormal()
        }
        ret t
    }

    public static Tensor zerosLike( Tensor x )
    {
        ret Tensor.zeros( x.shape() )
    }

    public static Tensor onesLike( Tensor x )
    {
        ret Tensor.ones( x.shape() )
    }

    override string toString()
    {
        string s = "Tensor" + this._shape.toString() + "["
        int n = this._data.length
        if n > 8
        {
            n = 8
        }
        for i = 0, i < n, i++
        {
            if i > 0
            {
                s = s + ", "
            }
            s = s + SystemConvertString( this._data[i] )
        }
        if this._data.length > n
        {
            s = s + ", ..."
        }
        s = s + "]"
        ret s
    }
}
