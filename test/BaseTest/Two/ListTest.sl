
namespace Core
{
    class Object
    {
        public void _init_()
        {

        }

        public string toString()
        {
            ret ""
        }
    }
    class Byte extends Object
    {
    }
    class Boolean
    {

    }
    class SByte
    {
        
    }
    class Int16
    {
        
    }
    class UInt16
    {
        
    }
    class Int32
    {
        _init_(Int32 val )
        {
            
        }        
    }
    class UInt32
    {
        
    }
    class Int64
    {
        
    }
    class UInt64
    {
        
    }
    class Float32
    {
        
    }
    class Float64
    {
        _init_(Float64 f)
        {

        }
    }
    class String
    {
        _init_( String str )
        {

        }
    }
    public class Type
    {
        public int length = 4
    }    
    public class Array
    {
        int _length = 0
        int _rank = 1

        int _listPtr = 0

        _init_(){
            this._listPtr = 0
        }
        _init_( int length )
        {
            uint allSize = length * 4

            this._listPtr = Lib.Array.CreateArray( length, 4 )

        }
        #!
        public static Array CreateInstance(Type elementType, int length);
        public static Array CreateInstance(Type elementType, int length1, int length2 );
        public static Array CreateInstance(Type elementType, int length1, int length2, int lenght3 );;
        !#
        #!
        _init_( uint length, Type type )
        {        
            uint allSize = length * type.length
            this._listPtr = ArrayMetaClass.SetArrayLength( allSize )
        }
        _init_( uint length, Type type, int rank )
        {
            uint unitLength = type.length
            this.length = length
            this.rank = rank
            uint allSize = length * type.length

            this._listPtr = ArrayMetaClass.SetArrayLength( allSize )
        }
        !#
    }

    List extends Object
    {

    }

    List<T> extends Object
    {
        T[] _items = new()

        _init_( int capity )
        {
            this._items.setLength( capity )
        }
        add( object obj )
        {

        }
        add( T t )
        {

        }
        remove( T t )
        {

        }
        removeIndex( int index )
        {

        }
        clear()
        {

        }
        set capity( int cap )
        {
        }
    }
}

ListTest
{
    static fun()
    {
        List<int> aalist = new(10)
    }
}