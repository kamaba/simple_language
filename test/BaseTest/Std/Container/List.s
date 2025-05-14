

public class List<T>
{
    Array<T> arraycontent = Array<T>(4);

    public int hasCount;

    public void add( T t )
    {
        if( this.arraycontent.count < this.hasCount )
        {
            this.arraycontent.[this.hasCount] = t
            this.hasCount++
        }
    }
    public void set captity( int cap )
    {
        //SL.Core.ClassManager.instance.SetMetaClass( this, )
    }
}