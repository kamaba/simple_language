
public interface IList<T>
{
    _init_( int capacity );
    get int capacity();
    set void capacity( int value );
    void ensureCapacity( int min );
    public void add( T item );
    public void remove( T item );
    public void removeAt( int index );
    public void clear();
    Array<T> toArray()
}
