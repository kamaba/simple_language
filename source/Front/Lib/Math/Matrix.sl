@Nickname("Mat")
public class Matrix
{
    # 行主序存储：_data[row * cols + col]
    Array<Float32> _data = null
    public Int32 rows = 0
    public Int32 cols = 0

    # ── 构造 ─────────────────────────────────────────────
    public void _init_( Int32 _rows, Int32 _cols )
    {
        this.rows = _rows
        this.cols = _cols
        this._data = Array<Float32>( _rows * _cols )
        # 系统级批量清零（一次调用替代逐元素循环）
        SystemArrayFillValue( this._data, 0, _rows * _cols, 0.0f )
    }

    public void _init_( Int32 _rows, Int32 _cols, Array<Float32> values )
    {
        this.rows = _rows
        this.cols = _cols
        # 系统级拷贝（保留 Float32 元素类型，生成新数组不与源共享存储）
        this._data = SystemArrayCopy( values, _rows * _cols )
    }

    # ── 索引访问 ─────────────────────────────────────────
    override Float32 _getItem_( int index )
    {
        ret this._data[index]
    }

    override void _setItem_( int index, Float32 value )
    {
        this._data[index] = value
    }

    public Float32 getValue( int row, int col )
    {
        ret this._data[ row * this.cols + col ]
    }

    public void setValue( int row, int col, Float32 value )
    {
        this._data[ row * this.cols + col ] = value
    }

    public Int32 count()
    {
        ret this.rows * this.cols
    }

    public bool isSquare()
    {
        ret this.rows == this.cols
    }

    # ── 运算符重载 ───────────────────────────────────────
    override Matrix _add_( Matrix b )
    {
        if this.rows != b.rows || this.cols != b.cols
        {
            ret this
        }
        Matrix r = Matrix( this.rows, this.cols )
        # 系统级批量加：r[i] = a[i] + b[i]
        SystemArrayAddFloat32( this._data, b._data, r._data, this.count() )
        ret r
    }

    override Matrix _sub_( Matrix b )
    {
        if this.rows != b.rows || this.cols != b.cols
        {
            ret this
        }
        Matrix r = Matrix( this.rows, this.cols )
        # 系统级批量减：r[i] = a[i] - b[i]
        SystemArraySubFloat32( this._data, b._data, r._data, this.count() )
        ret r
    }

    override Matrix _mul_( Object obj1 )
    {
        if obj1 is Matrix b
        {
            if this.cols != b.rows
            {
                ret this
            }
            Matrix r = Matrix( this.rows, b.cols )
            # 系统级矩阵乘：r[i*cols+j] = Σ_t a[i*k+t] * b[t*cols+j]
            SystemMathMatMulFloat32( this._data, b._data, r._data, this.rows, b.cols, this.cols )
            ret r
        }
        if obj1 is Float32 s
        {
            Matrix r = Matrix( this.rows, this.cols )
            # 系统级批量标量乘：r[i] = a[i] * s
            SystemArrayMulFloat32( this._data, s, r._data, this.count() )
            ret r
        }
        ret this
    }

    override bool _eq_( Matrix b )
    {
        if this.rows != b.rows || this.cols != b.cols
        {
            ret false
        }
        # 系统级逐元素精确比较
        ret SystemArrayEqualsFloat32( this._data, b._data, this.count() )
    }

    override bool _ne_( Matrix b )
    {
        ret !this._eq_( b )
    }

    # ── 矩阵运算 ─────────────────────────────────────────
    public Matrix transpose()
    {
        Matrix r = Matrix( this.cols, this.rows )
        # 系统级转置：r[j*rows+i] = a[i*cols+j]
        SystemMathTransposeFloat32( this._data, r._data, this.rows, this.cols )
        ret r
    }

    public Matrix clone()
    {
        Matrix r = Matrix( this.rows, this.cols )
        # 系统级拷贝（保留 Float32 元素类型，不与源共享存储）
        r._data = SystemArrayCopy( this._data, this.count() )
        ret r
    }

    # 方阵：取子式（去掉 row / col）
    Matrix minorMatrix( int row, int col )
    {
        Matrix r = Matrix( this.rows - 1, this.cols - 1 )
        int ri = 0
        int i = 0
        while i < this.rows
        {
            if i == row
            {
                i++
                continue
            }
            int rj = 0
            int j = 0
            while j < this.cols
            {
                if j == col
                {
                    j++
                    continue
                }
                r.setValue( ri, rj, this.getValue( i, j ) )
                rj++
                j++
            }
            ri++
            i++
        }
        ret r
    }

    # 方阵行列式（递归展开，仅适合小规模）
    public Float32 determinant()
    {
        if !this.isSquare()
        {
            ret 0.0f
        }
        if this.rows == 1
        {
            ret this.getValue( 0, 0 )
        }
        if this.rows == 2
        {
            ret this.getValue( 0, 0 ) * this.getValue( 1, 1 ) - this.getValue( 0, 1 ) * this.getValue( 1, 0 )
        }

        Float32 det = 0.0f
        int col = 0
        while col < this.cols
        {
            Float32 signValue = 1.0f
            if col % 2 == 1
            {
                signValue = -1.0f
            }
            # this 调用结果不能直接链式 .method()，拆成局部变量
            Matrix minor = this.minorMatrix( 0, col )
            det = det + signValue * this.getValue( 0, col ) * minor.determinant()
            col++
        }
        ret det
    }

    public bool isInvertible()
    {
        ret this.isSquare() && this.determinant() != 0.0f
    }

    # ── 静态工厂 ─────────────────────────────────────────
    public static Matrix identity( Int32 n )
    {
        Matrix r = Matrix( n, n )
        int i = 0
        while i < n
        {
            r.setValue( i, i, 1.0f )
            i++
        }
        ret r
    }

    public static Matrix zero( Int32 rows, Int32 cols )
    {
        ret Matrix( rows, cols )
    }

    override string toString()
    {
        # SystemArrayToString 输出 "[1,2,3]"（逗号分隔无空格，与 Array.toString 格式一致）
        ret "Matrix(" + this.rows.toString() + "x" + this.cols.toString() + ")" + SystemArrayToString( this._data )
    }
}
