
public interface IMap<T1, T2>
{
    _init_( int capacity );
    get int capacity();
    set void capacity( int value );
    void ensureCapacity( int min );
    public void add( T1 t1, T2 t2 );
    public void remove( T1 key );
    public void removeAt( int index );
    public void clear();
}
