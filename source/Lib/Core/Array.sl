
public class Core.Array
{
    int _index = -1;
    int _length = 0;
    Type _type = object.type();
    
    _init_()
    {
        this._length = 0
    }
    _init_( int len )
    {
        this._length = len;
    }
    _init_( int len, Type type = object.type )
    {
        this._length = len;
        this._type = type;
    }
    #该方法是系统方法，可以直接 通过 [], $x 方法    
    public object _indexValue_( int index )
    {
        obj = Lib.ArrayClass.GetArrayValueByIndex( this, index )
        ret obj;
    }

    public T indexValue<T>( int index )
    {
        obj = Lib.ArrayClass.GetArrayValueByIndex( this, index )
        ret obj as T
    }

    #该方法是 当前游标的植，进行替换
    public get object _value_()
    {
        if( this._index < 0 || this._index > this._length )
        {
            ret null
        }
        ret ArrayMetaClass.Get( this._index )
    }
    public get T _value_<T>()
    {
        if( this._index < 0 || this._index > this._length )
        {
            ret null
        }

        Type t = T.type

        var obj = ArrayMetaClass.Get( t, this._index )

        ret obj as T
    }
    #该方法是 当前游标的植，进行替换
    public set void _value( object t )
    {   
        if( this._index < 0 || this._index > this._length )
        {
            ret -1
        }

        Type t = T.type

        ArrayMetaClass.SetValue( t, this._index )
    }

    public void resize( int len, bool isSetZero = false )
    {
        #ArrayMetaClass.SetValue( t, this._index )
    }
    public set void legnth( int val )
    {
        this.resize( val )
    }
    public get int length()
    {
        ret this._length
    }
    public set void index( int _ind )
    {
        this._index = _ind;
    }
    public get int index()
    {
        ret this._index
    }
}