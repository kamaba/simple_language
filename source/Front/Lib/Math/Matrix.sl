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
        int i = 0
        while i < _rows * _cols
        {
            this._data[i] = 0.0f
            i++
        }
    }

    public void _init_( Int32 _rows, Int32 _cols, Array<Float32> values )
    {
        this.rows = _rows
        this.cols = _cols
        this._data = Array<Float32>( _rows * _cols )
        int i = 0
        while i < _rows * _cols
        {
            this._data[i] = values[i]
            i++
        }
    }

    # ── 索引访问 ─────────────────────────────────────────
    Float32 _getItem_( int index )
    {
        ret this._data[index]
    }

    void _setItem_( int index, Float32 value )
    {
        this._data[index] = value
    }

    public Float32 get( int row, int col )
    {
        ret this._data[ row * this.cols + col ]
    }

    public void set( int row, int col, Float32 value )
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
    override Matrix _add_( Object obj1 )
    {
        if obj1 is Matrix b
        {
            if this.rows != b.rows || this.cols != b.cols
            {
                ret this
            }
            Matrix r = Matrix( this.rows, this.cols )
            int i = 0
            while i < this.count()
            {
                r._data[i] = this._data[i] + b._data[i]
                i++
            }
            ret r
        }
        ret this
    }

    override Matrix _sub_( Object obj1 )
    {
        if obj1 is Matrix b
        {
            if this.rows != b.rows || this.cols != b.cols
            {
                ret this
            }
            Matrix r = Matrix( this.rows, this.cols )
            int i = 0
            while i < this.count()
            {
                r._data[i] = this._data[i] - b._data[i]
                i++
            }
            ret r
        }
        ret this
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
            int row = 0
            while row < this.rows
            {
                int col = 0
                while col < b.cols
                {
                    Float32 sum = 0.0f
                    int k = 0
                    while k < this.cols
                    {
                        sum = sum + this.get( row, k ) * b.get( k, col )
                        k++
                    }
                    r.set( row, col, sum )
                    col++
                }
                row++
            }
            ret r
        }
        if obj1 is Float32 s
        {
            Matrix r = Matrix( this.rows, this.cols )
            int i = 0
            while i < this.count()
            {
                r._data[i] = this._data[i] * s
                i++
            }
            ret r
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Matrix b
        {
            if this.rows != b.rows || this.cols != b.cols
            {
                ret false
            }
            int i = 0
            while i < this.count()
            {
                if this._data[i] != b._data[i]
                {
                    ret false
                }
                i++
            }
            ret true
        }
        ret false
    }

    override bool _ne_( Object obj1 )
    {
        ret !this._eq_( obj1 )
    }

    # ── 矩阵运算 ─────────────────────────────────────────
    public Matrix transpose()
    {
        Matrix r = Matrix( this.cols, this.rows )
        int row = 0
        while row < this.rows
        {
            int col = 0
            while col < this.cols
            {
                r.set( col, row, this.get( row, col ) )
                col++
            }
            row++
        }
        ret r
    }

    public Matrix clone()
    {
        Matrix r = Matrix( this.rows, this.cols )
        int i = 0
        while i < this.count()
        {
            r._data[i] = this._data[i]
            i++
        }
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
                r.set( ri, rj, this.get( i, j ) )
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
            ret this.get( 0, 0 )
        }
        if this.rows == 2
        {
            ret this.get( 0, 0 ) * this.get( 1, 1 ) - this.get( 0, 1 ) * this.get( 1, 0 )
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
            det = det + signValue * this.get( 0, col ) * this.minorMatrix( 0, col ).determinant()
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
            r.set( i, i, 1.0f )
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
        string s = "Matrix(" + this.rows.toString() + "x" + this.cols.toString() + ")["
        int i = 0
        while i < this.count()
        {
            if i > 0
            {
                s = s + ", "
            }
            s = s + this._data[i].toString()
            i++
        }
        s = s + "]"
        ret s
    }
}
