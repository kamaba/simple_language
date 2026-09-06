#!
Tuple.sl  元组类型，参照 C# Tuple<T1,...> / Python tuple 两种形式：
1. 模板形式 Tuple<T1> ~ Tuple<T1,...,T8>：C# 风格的强类型定长元组，
   公有字段 item1..itemN，支持下标读写（_getItem_/_setItem_，即 t.$0 语法）。
2. 无模板形式 Tuple：Python 风格的动态类型元组，内部基于 Array<object> 实现，
   可无限扩展（Tuple.create(params) / add），支持下标读写。
前期不支持匿名元组字面量 ()，所有元组必须显式使用 Tuple 标记。
!#

# ============================================================
# 模板形式：Tuple<T1>（单元素，强类型定长）
# ============================================================

public class Tuple<T1> extends Object
{
    public T1 item1 = null

    _init_( T1 v1 )
    {
        this.item1 = v1
    }

    public static Tuple<T1> create( T1 v1 )
    {
        var t = Tuple<T1>( v1 )
        ret t
    }

    get int length()
    {
        ret 1
    }

    #下标读取：index 超出 [0, length) 返回 null
    public object _getItem_( int index )
    {
        if index == 0
        {
            ret this.item1
        }
        ret null
    }

    #下标写入：index 超出 [0, length) 静默忽略
    public void _setItem_( int index, object value )
    {
        if index == 0
        {
            this.item1 = value as T1
        }
    }

    override string toString()
    {
        ret "(" + this._valueToString( this.item1 ) + ")"
    }

    string _valueToString( object v )
    {
        if v == null
        {
            ret "null"
        }
        ret v.toString()
    }
}

# ============================================================
# 模板形式：Tuple<T1,T2>
# ============================================================

public class Tuple<T1,T2> extends Object
{
    public T1 item1 = null
    public T2 item2 = null

    _init_( T1 v1, T2 v2 )
    {
        this.item1 = v1
        this.item2 = v2
    }

    public static Tuple<T1,T2> create( T1 v1, T2 v2 )
    {
        var t = Tuple<T1,T2>( v1, v2 )
        ret t
    }

    get int length()
    {
        ret 2
    }

    public object _getItem_( int index )
    {
        if index == 0
        {
            ret this.item1
        }
        elif index == 1
        {
            ret this.item2
        }
        ret null
    }

    public void _setItem_( int index, object value )
    {
        if index == 0
        {
            this.item1 = value as T1
        }
        elif index == 1
        {
            this.item2 = value as T2
        }
    }

    override string toString()
    {
        ret "(" + this._valueToString( this.item1 ) + ", " + this._valueToString( this.item2 ) + ")"
    }

    string _valueToString( object v )
    {
        if v == null
        {
            ret "null"
        }
        ret v.toString()
    }
}

# ============================================================
# 模板形式：Tuple<T1,T2,T3>
# ============================================================

public class Tuple<T1,T2,T3> extends Object
{
    public T1 item1 = null
    public T2 item2 = null
    public T3 item3 = null

    _init_( T1 v1, T2 v2, T3 v3 )
    {
        this.item1 = v1
        this.item2 = v2
        this.item3 = v3
    }

    public static Tuple<T1,T2,T3> create( T1 v1, T2 v2, T3 v3 )
    {
        var t = Tuple<T1,T2,T3>( v1, v2, v3 )
        ret t
    }

    get int length()
    {
        ret 3
    }

    public object _getItem_( int index )
    {
        if index == 0
        {
            ret this.item1
        }
        elif index == 1
        {
            ret this.item2
        }
        elif index == 2
        {
            ret this.item3
        }
        ret null
    }

    public void _setItem_( int index, object value )
    {
        if index == 0
        {
            this.item1 = value as T1
        }
        elif index == 1
        {
            this.item2 = value as T2
        }
        elif index == 2
        {
            this.item3 = value as T3
        }
    }

    override string toString()
    {
        ret "(" + this._valueToString( this.item1 ) + ", " + this._valueToString( this.item2 ) + ", " + this._valueToString( this.item3 ) + ")"
    }

    string _valueToString( object v )
    {
        if v == null
        {
            ret "null"
        }
        ret v.toString()
    }
}

# ============================================================
# 模板形式：Tuple<T1,T2,T3,T4>
# ============================================================

public class Tuple<T1,T2,T3,T4> extends Object
{
    public T1 item1 = null
    public T2 item2 = null
    public T3 item3 = null
    public T4 item4 = null

    _init_( T1 v1, T2 v2, T3 v3, T4 v4 )
    {
        this.item1 = v1
        this.item2 = v2
        this.item3 = v3
        this.item4 = v4
    }

    public static Tuple<T1,T2,T3,T4> create( T1 v1, T2 v2, T3 v3, T4 v4 )
    {
        var t = Tuple<T1,T2,T3,T4>( v1, v2, v3, v4 )
        ret t
    }

    get int length()
    {
        ret 4
    }

    public object _getItem_( int index )
    {
        if index == 0
        {
            ret this.item1
        }
        elif index == 1
        {
            ret this.item2
        }
        elif index == 2
        {
            ret this.item3
        }
        elif index == 3
        {
            ret this.item4
        }
        ret null
    }

    public void _setItem_( int index, object value )
    {
        if index == 0
        {
            this.item1 = value as T1
        }
        elif index == 1
        {
            this.item2 = value as T2
        }
        elif index == 2
        {
            this.item3 = value as T3
        }
        elif index == 3
        {
            this.item4 = value as T4
        }
    }

    override string toString()
    {
        ret "(" + this._valueToString( this.item1 ) + ", " + this._valueToString( this.item2 ) + ", " + this._valueToString( this.item3 ) + ", " + this._valueToString( this.item4 ) + ")"
    }

    string _valueToString( object v )
    {
        if v == null
        {
            ret "null"
        }
        ret v.toString()
    }
}

# ============================================================
# 模板形式：Tuple<T1,T2,T3,T4,T5>
# ============================================================

public class Tuple<T1,T2,T3,T4,T5> extends Object
{
    public T1 item1 = null
    public T2 item2 = null
    public T3 item3 = null
    public T4 item4 = null
    public T5 item5 = null

    _init_( T1 v1, T2 v2, T3 v3, T4 v4, T5 v5 )
    {
        this.item1 = v1
        this.item2 = v2
        this.item3 = v3
        this.item4 = v4
        this.item5 = v5
    }

    public static Tuple<T1,T2,T3,T4,T5> create( T1 v1, T2 v2, T3 v3, T4 v4, T5 v5 )
    {
        var t = Tuple<T1,T2,T3,T4,T5>( v1, v2, v3, v4, v5 )
        ret t
    }

    get int length()
    {
        ret 5
    }

    public object _getItem_( int index )
    {
        if index == 0
        {
            ret this.item1
        }
        elif index == 1
        {
            ret this.item2
        }
        elif index == 2
        {
            ret this.item3
        }
        elif index == 3
        {
            ret this.item4
        }
        elif index == 4
        {
            ret this.item5
        }
        ret null
    }

    public void _setItem_( int index, object value )
    {
        if index == 0
        {
            this.item1 = value as T1
        }
        elif index == 1
        {
            this.item2 = value as T2
        }
        elif index == 2
        {
            this.item3 = value as T3
        }
        elif index == 3
        {
            this.item4 = value as T4
        }
        elif index == 4
        {
            this.item5 = value as T5
        }
    }

    override string toString()
    {
        ret "(" + this._valueToString( this.item1 ) + ", " + this._valueToString( this.item2 ) + ", " + this._valueToString( this.item3 ) + ", " + this._valueToString( this.item4 ) + ", " + this._valueToString( this.item5 ) + ")"
    }

    string _valueToString( object v )
    {
        if v == null
        {
            ret "null"
        }
        ret v.toString()
    }
}

# ============================================================
# 模板形式：Tuple<T1,T2,T3,T4,T5,T6>
# ============================================================

public class Tuple<T1,T2,T3,T4,T5,T6> extends Object
{
    public T1 item1 = null
    public T2 item2 = null
    public T3 item3 = null
    public T4 item4 = null
    public T5 item5 = null
    public T6 item6 = null

    _init_( T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6 )
    {
        this.item1 = v1
        this.item2 = v2
        this.item3 = v3
        this.item4 = v4
        this.item5 = v5
        this.item6 = v6
    }

    public static Tuple<T1,T2,T3,T4,T5,T6> create( T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6 )
    {
        var t = Tuple<T1,T2,T3,T4,T5,T6>( v1, v2, v3, v4, v5, v6 )
        ret t
    }

    get int length()
    {
        ret 6
    }

    public object _getItem_( int index )
    {
        if index == 0
        {
            ret this.item1
        }
        elif index == 1
        {
            ret this.item2
        }
        elif index == 2
        {
            ret this.item3
        }
        elif index == 3
        {
            ret this.item4
        }
        elif index == 4
        {
            ret this.item5
        }
        elif index == 5
        {
            ret this.item6
        }
        ret null
    }

    public void _setItem_( int index, object value )
    {
        if index == 0
        {
            this.item1 = value as T1
        }
        elif index == 1
        {
            this.item2 = value as T2
        }
        elif index == 2
        {
            this.item3 = value as T3
        }
        elif index == 3
        {
            this.item4 = value as T4
        }
        elif index == 4
        {
            this.item5 = value as T5
        }
        elif index == 5
        {
            this.item6 = value as T6
        }
    }

    override string toString()
    {
        ret "(" + this._valueToString( this.item1 ) + ", " + this._valueToString( this.item2 ) + ", " + this._valueToString( this.item3 ) + ", " + this._valueToString( this.item4 ) + ", " + this._valueToString( this.item5 ) + ", " + this._valueToString( this.item6 ) + ")"
    }

    string _valueToString( object v )
    {
        if v == null
        {
            ret "null"
        }
        ret v.toString()
    }
}

# ============================================================
# 模板形式：Tuple<T1,T2,T3,T4,T5,T6,T7>
# ============================================================

public class Tuple<T1,T2,T3,T4,T5,T6,T7> extends Object
{
    public T1 item1 = null
    public T2 item2 = null
    public T3 item3 = null
    public T4 item4 = null
    public T5 item5 = null
    public T6 item6 = null
    public T7 item7 = null

    _init_( T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7 )
    {
        this.item1 = v1
        this.item2 = v2
        this.item3 = v3
        this.item4 = v4
        this.item5 = v5
        this.item6 = v6
        this.item7 = v7
    }

    public static Tuple<T1,T2,T3,T4,T5,T6,T7> create( T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7 )
    {
        var t = Tuple<T1,T2,T3,T4,T5,T6,T7>( v1, v2, v3, v4, v5, v6, v7 )
        ret t
    }

    get int length()
    {
        ret 7
    }

    public object _getItem_( int index )
    {
        if index == 0
        {
            ret this.item1
        }
        elif index == 1
        {
            ret this.item2
        }
        elif index == 2
        {
            ret this.item3
        }
        elif index == 3
        {
            ret this.item4
        }
        elif index == 4
        {
            ret this.item5
        }
        elif index == 5
        {
            ret this.item6
        }
        elif index == 6
        {
            ret this.item7
        }
        ret null
    }

    public void _setItem_( int index, object value )
    {
        if index == 0
        {
            this.item1 = value as T1
        }
        elif index == 1
        {
            this.item2 = value as T2
        }
        elif index == 2
        {
            this.item3 = value as T3
        }
        elif index == 3
        {
            this.item4 = value as T4
        }
        elif index == 4
        {
            this.item5 = value as T5
        }
        elif index == 5
        {
            this.item6 = value as T6
        }
        elif index == 6
        {
            this.item7 = value as T7
        }
    }

    override string toString()
    {
        ret "(" + this._valueToString( this.item1 ) + ", " + this._valueToString( this.item2 ) + ", " + this._valueToString( this.item3 ) + ", " + this._valueToString( this.item4 ) + ", " + this._valueToString( this.item5 ) + ", " + this._valueToString( this.item6 ) + ", " + this._valueToString( this.item7 ) + ")"
    }

    string _valueToString( object v )
    {
        if v == null
        {
            ret "null"
        }
        ret v.toString()
    }
}

# ============================================================
# 模板形式：Tuple<T1,T2,T3,T4,T5,T6,T7,T8>
# ============================================================

public class Tuple<T1,T2,T3,T4,T5,T6,T7,T8> extends Object
{
    public T1 item1 = null
    public T2 item2 = null
    public T3 item3 = null
    public T4 item4 = null
    public T5 item5 = null
    public T6 item6 = null
    public T7 item7 = null
    public T8 item8 = null

    _init_( T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8 )
    {
        this.item1 = v1
        this.item2 = v2
        this.item3 = v3
        this.item4 = v4
        this.item5 = v5
        this.item6 = v6
        this.item7 = v7
        this.item8 = v8
    }

    public static Tuple<T1,T2,T3,T4,T5,T6,T7,T8> create( T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8 )
    {
        var t = Tuple<T1,T2,T3,T4,T5,T6,T7,T8>( v1, v2, v3, v4, v5, v6, v7, v8 )
        ret t
    }

    get int length()
    {
        ret 8
    }

    public object _getItem_( int index )
    {
        if index == 0
        {
            ret this.item1
        }
        elif index == 1
        {
            ret this.item2
        }
        elif index == 2
        {
            ret this.item3
        }
        elif index == 3
        {
            ret this.item4
        }
        elif index == 4
        {
            ret this.item5
        }
        elif index == 5
        {
            ret this.item6
        }
        elif index == 6
        {
            ret this.item7
        }
        elif index == 7
        {
            ret this.item8
        }
        ret null
    }

    public void _setItem_( int index, object value )
    {
        if index == 0
        {
            this.item1 = value as T1
        }
        elif index == 1
        {
            this.item2 = value as T2
        }
        elif index == 2
        {
            this.item3 = value as T3
        }
        elif index == 3
        {
            this.item4 = value as T4
        }
        elif index == 4
        {
            this.item5 = value as T5
        }
        elif index == 5
        {
            this.item6 = value as T6
        }
        elif index == 6
        {
            this.item7 = value as T7
        }
        elif index == 7
        {
            this.item8 = value as T8
        }
    }

    override string toString()
    {
        ret "(" + this._valueToString( this.item1 ) + ", " + this._valueToString( this.item2 ) + ", " + this._valueToString( this.item3 ) + ", " + this._valueToString( this.item4 ) + ", " + this._valueToString( this.item5 ) + ", " + this._valueToString( this.item6 ) + ", " + this._valueToString( this.item7 ) + ", " + this._valueToString( this.item8 ) + ")"
    }

    string _valueToString( object v )
    {
        if v == null
        {
            ret "null"
        }
        ret v.toString()
    }
}

# ============================================================
# 无模板形式：Tuple（Python 风格，动态类型，基于数组可无限扩展）
# ============================================================

public class Tuple extends Object
{
    Array<object> _values = null
    int _length = 0
    int _capacity = 0

    #空元组：Tuple()，长度 0
    override _init_()
    {
        this._values = Array<object>( 4 )
        this._capacity = 4
    }

    _init_( val1 )
    {
        this._appendValue( val1 )
    }

    _init_( val1, val2 )
    {
        this._appendValue( val1 )
        this._appendValue( val2 )
    }

    _init_( val1, val2, val3 )
    {
        this._appendValue( val1 )
        this._appendValue( val2 )
        this._appendValue( val3 )
    }

    _init_( val1, val2, val3, val4 )
    {
        this._appendValue( val1 )
        this._appendValue( val2 )
        this._appendValue( val3 )
        this._appendValue( val4 )
    }

    _init_( val1, val2, val3, val4, val5 )
    {
        this._appendValue( val1 )
        this._appendValue( val2 )
        this._appendValue( val3 )
        this._appendValue( val4 )
        this._appendValue( val5 )
    }

    _init_( val1, val2, val3, val4, val5, val6 )
    {
        this._appendValue( val1 )
        this._appendValue( val2 )
        this._appendValue( val3 )
        this._appendValue( val4 )
        this._appendValue( val5 )
        this._appendValue( val6 )
    }

    _init_( val1, val2, val3, val4, val5, val6, val7 )
    {
        this._appendValue( val1 )
        this._appendValue( val2 )
        this._appendValue( val3 )
        this._appendValue( val4 )
        this._appendValue( val5 )
        this._appendValue( val6 )
        this._appendValue( val7 )
    }

    _init_( val1, val2, val3, val4, val5, val6, val7, val8 )
    {
        this._appendValue( val1 )
        this._appendValue( val2 )
        this._appendValue( val3 )
        this._appendValue( val4 )
        this._appendValue( val5 )
        this._appendValue( val6 )
        this._appendValue( val7 )
        this._appendValue( val8 )
    }

    #不限长静态工厂：可传任意个元素（内部按数组存储）
    public static Tuple create( params object[] values )
    {
        var t = Tuple()
        if values != null
        {
            int count = values.length
            for i = 0, i < count, i++
            {
                t._appendValue( SystemArrayGetValueThis( values, i ) )
            }
        }
        ret t
    }

    #追加一个元素（容量不足自动扩容），返回 this 便于链式调用：t.add(1).add(2)
    public Tuple add( object value )
    {
        this._appendValue( value )
        ret this
    }

    get int length()
    {
        ret this._length
    }

    get bool isEmpty()
    {
        if this._length <= 0
        {
            ret true
        }
        ret false
    }

    #下标读取：index 超出 [0, length) 返回 null
    public object _getItem_( int index )
    {
        if index < 0 || index >= this._length
        {
            ret null
        }
        ret SystemArrayGetValueThis( this._values, index )
    }

    #下标写入：index 超出 [0, length) 静默忽略
    public void _setItem_( int index, object value )
    {
        if index < 0 || index >= this._length
        {
            ret
        }
        SystemArraySetValueThis( this._values, index, value )
    }

    #元素首次出现下标，未找到返回 -1
    public int indexOf( object value )
    {
        for i = 0, i < this._length, i++
        {
            if SystemArrayGetValueThis( this._values, i ) == value
            {
                ret i
            }
        }
        ret -1
    }

    public bool contains( object value )
    {
        if this.indexOf( value ) >= 0
        {
            ret true
        }
        ret false
    }

    public void clear()
    {
        this._length = 0
    }

    #内部：追加一个元素（首次使用时初始化内部数组，容量不足时扩容）
    void _appendValue( object val )
    {
        if this._values == null
        {
            this._values = Array<object>( 4 )
            this._capacity = 4
        }
        if this._length >= this._capacity
        {
            this._grow()
        }
        SystemArraySetValueThis( this._values, this._length, val )
        this._length++
    }

    #内部：容量扩展 0->4，之后倍增 4->8->16...
    void _grow()
    {
        int newCapacity = 4
        if this._capacity > 0
        {
            newCapacity = this._capacity * 2
        }
        this._values = SystemArrayResize( this._values, newCapacity )
        this._capacity = newCapacity
    }

    override string toString()
    {
        string s = "("
        for i = 0, i < this._length, i++
        {
            var cur = SystemArrayGetValueThis( this._values, i )
            if cur == null
            {
                s = s + "null"
            }
            else
            {
                s = s + cur.toString()
            }
            if i < this._length - 1
            {
                s = s + ", "
            }
        }
        s = s + ")"
        ret s
    }
}
