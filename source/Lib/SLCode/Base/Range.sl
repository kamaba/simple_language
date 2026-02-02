

    public class Range<T:Number> interface IIterable<T>, IIterator<T>
    {
        T _start = null
        T _end = null
        T _step = null
        T _current = null;
        _init_( T _start, T _end, T _step )
        {
            this._start = _start
            this._end = _end
            this._step = _step
        }
        #接口层
        override void reset()
        {
            this._current = this._start
        }
        override bool moveNext()
        {            
            bool hasNext_var = this._current < this._end 
            if hasNext_var
            {
                this._current = SimpleLanguage.Lib.ArrayClass.GetArrayValueThis( this, this._index ) as T
            }
            else
            {
                this._current = null
            }
            this._current += this._step;
            #System.Console.WriteLine(" Array.moveNext-----" + this._index )
            ret hasNext_var
        }
        override T current()
        {
            ret this._current;
        }
        override void release()
        {
        }
        override IIterator<T> iterator()
        {
            ret this
        }
        override string toString()
        {            
            string showstr = "["
            for i = _start, i < this._end
            {
                var cur = i + this._step
                showstr = showstr + cur.toString()
                if( i < this._length - 1 )
                {
                    showstr += ","
                }
            }
            ret showstr + "]"
        }
    }

    public class Range extends Range<int>
    { 
    }
