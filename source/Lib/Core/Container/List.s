

public class List<T>
{
    Array arraycontent = null

    public int hasCount;

    public void add( T t )
    {
        if( this.arraycontent.count < this.hasCount )
        {
            this.arraycontent.[this.hasCount] = t
            this.hasCount++
        }
    }
    void insert( int index, T t )
    {

    }
    public void clear()
    {

    }
    T find( Action( t ) action ){
        ret null
    }
    public bool remove( Object obj )
    {
        ret false
    }
    public bool contains( object obj )
    {
        ret false
    }
    public bool removeAt( int index )
    {
        ret false
    }
    T get _value_( int index )
    {
        ret null
    }
    T set _value_( int index )
    {
        ret null
    }
    T at( int index )
    {

    }
    public void set captity( int cap )
    {
        //SL.Core.ClassManager.instance.SetMetaClass( this, )
    }

    Set<T> toSet()
    {
        Set<T> sett = new()
        for v in this
        {
            sett.add(v)
        }
        ret sett
    }
    Map<int,T> toMap()
    {
        Map<int,T> map = new()        
        for i = 0, i < this.count(), i++
        {
            map.add(i, _value[i] )
        }
        ret map
    }
    T[] toArray()
    {
        T[] arrt = new(this.count())

        ret arrt
    }
    string override string toString()
    {
        ret ""
    }
}