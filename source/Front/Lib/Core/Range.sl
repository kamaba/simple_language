public class Range<T:Num> interface IIterable<T>, IIterator<T>
{
    T _start = null
    T _end = null
    T _step = null
    T _iteratorValue = 0
    T _current = null
    Array<T> _toArrayCache = null


    _init_( T _end )
    {
        this._init_(0, _end, 1)
    }
    _init_( T _start, T _end )
    {
        this._init_(_start, _end, 1)
    }
    _init_( T _start, T _end, T _step )
    {
        this._start = _start
        this._end = _end
        this._step = _step
        this._toArrayCache = null
        this.reset()
    }
    #接口层
    override void reset()
    {
        this._iteratorValue = 0
        this._current = null
    }
    override bool moveNext()
    {
        if this._step == 0
        {
            this._current = null
            ret false
        }

        T nextValue = this._start + this._iteratorValue
        bool hasNext_var = false
        if this._step > 0
        {
            hasNext_var = nextValue < this._end
        }
        else
        {
            hasNext_var = nextValue > this._end
        }

        if hasNext_var
        {
            this._current = nextValue
            this._iteratorValue += this._step
        }
        else
        {
            this._current = null
        }
        ret hasNext_var
    }
    bool isContain( T t )
    {
        if( t > this._start )
        {
            if( t < this._end )
            {
                ret true
            }
        }
        else
        {
            if t > this._end 
            {
                ret true
            }
            ret false
        }
    }
    override T current()
    {
        ret this._current
    }
    override IIterator<T> iterator()
    {
        ret this
    }

    # 将区间内所有值物化为 Array<T>：首次遍历写入缓存并复用，避免重复分配与迭代。
    Array<T> toArray()
    {
        if this._toArrayCache != null
        {
            ret this._toArrayCache
        }

        this.reset()
        int cnt = 0
        while this.moveNext()
        {
            cnt++
        }

        this.reset()
        Array<T> arr = Array<T>(cnt)
        int idx = 0
        while this.moveNext()
        {
            arr.setValue(idx, this.current())
            idx++
        }
        this._toArrayCache = arr
        ret arr
    }

    override string toString()
    {
        ret "Range(" + this._start + "," + this._end + "," + this._step + ")"
    }
}
