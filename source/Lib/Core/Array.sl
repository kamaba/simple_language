
public class MetaClass
{

}

public class Type
{
    public MetaClass _metaClass = null
}

public class Array
{
    int _index = -1;

    
    static int maxLenght()
    {
        ret 0
    }

    _init_<T>( int count = 0 )
    {

    }
    _init_( Type type, int count )
    {
        
    }
    public T _index_<T>( int index )
    {

    }
    public Array<T> join( Array<T> con )
    {
        ret null
    }
    public void resize( int len )
    {

    }
    public set void legnth( int val )
    {
        this.resize( val )
    }
    public get int length()
    {
        ret 0;
    }
    public set void index( int _ind )
    {
        this._index = _ind;
    }
    public get int index()
    {
        ret this._index
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
    public set int _value_<T>( T t )
    {        
        if( this._index < 0 || this._index > this._length )
        {
            ret -1
        }

        Type t = T.type

        var obj = ArrayMetaClass.Get( t, this._index )

        obj.SetValue<T>( t )
    }
}

#!
ArrayTest
{
    static fun()
    {
        array a = new( int.type, 20);
        a2 = array()
        a3 = [1,2,3,4]
        a4 = Array( float32.type, 20 ){ 1.2f, 2.2f }

        
    }
}

!#