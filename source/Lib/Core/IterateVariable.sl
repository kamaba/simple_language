    public class IterateVariable interface IIterator
    {
        _start = 0
        _index = 0
        _value = null
        IIterator _iterator = null
        _isDone = false;


        _init_( IIterable __it )
        {
            this._iterator = __it.iterator();
            this._iterator.reset()
        }
        get int index(){ ret this._index }
        get object value(){ ret this._value }

        override void reset()
        {
            this._isDone = false
            this._start = 0
            this._index = 0
            this._value = null
        }
        override bool moveNext()
        {
            if this._isDone
            {
                ret false
            }
            this._isDone = this._iterator.moveNext()
            ret this._isDone
        }
        override get object current()
        {
            this._value = this._iterator.current()
            ret this._value
        }
        override void release()
        {
            
        }
    }