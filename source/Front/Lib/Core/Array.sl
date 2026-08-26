
public class Array<T> interface IIterable<T>, IIterator<T>
{
    int _length = 0
    Type _type = null;
    int _index = 0;
    T _current = null
       
    public static Array<T> create( int length )
    {
        var arr = Array<T>(length)
        ret arr
    }

    _init_( int __len )
    {         
        this._length = __len
    }
    get int length(){ ret this._length }

    public void fill(T value, int startIndex = 0, int count = -1 )
    {
        if startIndex < 0 || startIndex >= this._length
        {
            ret
        }
        #count==0：默认从 startIndex 填到 _length 末尾；
        #count>0：精确填 count 个（超过 capacity 剩余槽位则截断），
        #此前 elif 把 count 覆盖成 capacity-startIndex，导致 fill(33,2,3) 填到 capacity 末尾。
        if( count == 0 )
        {
            count = this._length - startIndex
        }
        elif count > 0
        {
            if count > this._length - startIndex
            {
                count = this._length - startIndex
            }
        }
        else
        {
            SystemPrint("Array.fill: index out of range")
            ret
        }
        SystemArrayFillValue(this, startIndex, count, value )        
    }
    #接口层
    override void reset()
    {
        this._index = -1;
        this._current = null
    }
    override bool moveNext()
    {          
        this._index++;  
        bool hasNext_var = this._index < this._length 
        if hasNext_var
        {
            this._current = SystemArrayGetValueThis(this, this._index) as T
        }
        else
        {
            this._current = null
        }
        #global.println(" Array.moveNext-----" + this._index + " length: " + this._length  )
        ret hasNext_var
    }
    override get T current()
    {
        ret this._current;
    }
    override set void current( T val )
    {
        SystemArraySetValueThis(this, this._index, val)
        this._current = val
    }
    override get IIterator<T> iterator()
    {
        ret this
    }
    get int index()
    {
        ret this._index;
    }
    set void index( int ind )
    {
        if( ind < 0 )
        {
            #throw error("");
            ret
        }
        if( ind >= this._length )
        {
            #throw error("超出了范围")
            ret 
        }
        this._index = ind;
        this._current = SystemArrayGetValueThis(this, ind) as T
    }
    _setItem_( int __index, T val )
    {
        SystemArraySetValueThis(this, __index, val)
    }
    T _getItem_( int __index )
    {
        ret SystemArrayGetValueThis(this, __index) as T
    }
    public void forEach( Function callback )
    {
        for i = 0, i < this._length, i++
        {
            var item = SystemArrayGetValueThis(this, i) as T
            callback( item )
        }
    }
    override string toString()
    {            
        string showstr = "["
        for i = 0, i < this._length, i++
        {
            var cur = SystemArrayGetValueThis(this, i)
            if cur == null
            {
                showstr = showstr + "null"
            }
            else
            {
                showstr = showstr + cur.toString()
            }
            if( i < this._length - 1 )
            {
                showstr += ","
            }
        }
        ret showstr + "]"
    }
}

#!
var iter = a1.iterator()  使用a1.type 变成T
bool f = false
v = null
label start
if f = iter.moveNext()
{
    v = iter.current()
    then_statement
    goto start
}
v = null
!#        