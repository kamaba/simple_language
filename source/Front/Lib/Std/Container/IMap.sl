
public interface IMap<T1, T2>
{
    _init_( int capacity );
    get int capacity();
    set void capacity( int value );
    void ensureCapacity( int min );
    public bool add( T1 key, T2 value );
    public T2 remove( T1 key );
    public void removeAt( int index );
    public void clear();

    T2 _getItem_( T1 key );
    void _setItem_( T1 key, T2 value );
}
