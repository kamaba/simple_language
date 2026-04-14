public class Range<T:Num> interface IIterable<T>, IIterator<T>
{
    T _start = null
    T _end = null
    T _step = null
    T _iteratorValue = 0
    T _current = null


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
    override string toString()
    {
        ret "Range(" + this._start + "," + this._end + "," + this._step + ")"
    }
}
