

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
    public void set captity( int cap )
    {
        //SL.Core.ClassManager.instance.SetMetaClass( this, )
    }
}