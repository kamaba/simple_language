

    public interface IArray
    {
    }

    public class Array<T> interface IIterable<T>, IIterator<T>
    {
        int _length = 0
        Type _type = null;
        _index = 0;
        T _current = null
        long _ptr = 0
           
        public static Array<T> createInstance(int length)
        {
            var arr = Array<T>(length)
            ret arr
        }
        public static Array<CT> CreateInstance<CT>( int length1 )
        {            
            var arr = Array<CT>(length1)
            ret arr
        }

        _init_( int __len )
        {
            #uint allSize = __len * 4            
            this._length = __len
            #this._ptr = Lib.ArrayClass.CreateArray( length, 4 )
        }
        get int length(){ ret this._length }

        #接口层
        override void reset()
        {
            this._index = 0;
        }
        override bool moveNext()
        {            
            bool hasNext_var = this._index < this._length 
            if hasNext_var
            {
                this._current = SimpleLanguage.Lib.ArrayClass.GetArrayValueThis( this, this._index ) as T
            }
            else
            {
                this._current = null
            }
            this._index++;
            #System.Console.WriteLine(" Array.moveNext-----" + this._index )
            ret hasNext_var
        }
        override T current()
        {
            ret this._current;
        }
        override set void current( T val )
        {
            SimpleLanguage.Lib.ArrayClass.SetArrayValueThis( this, this._index, val )
            this._current = val
        }
        override void release()
        {
        }
        override IIterator<T> iterator()
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
            var retobj = SimpleLanguage.Lib.ArrayClass.GetArrayValueThis( this, ind )
            this._current = retobj;
        }
        set setValue( int __index, T val )
        {
            #Lib.Array.SetArrayValue( this._ptr, 5,  index, val )
            SimpleLanguage.Lib.ArrayClass.SetArrayValueThis( this, __index, val )
        }
        get T getValue( int __index )
        {
            #ret Lib.ArrayClass.GetArrayValue( this._ptr, 5,  index )
            ret SimpleLanguage.Lib.ArrayClass.GetArrayValueThis( this, __index )
        }
        setValues( Int64 valPtr, int len )
        {
            #Lib.ArrayClass.SetArrayValue( this._ptr, 1,  valPtr, len )
        }
        override string toString()
        {            
            string showstr = "["
            for i = 0, i < this._length, i++
            {
                var cur = SimpleLanguage.Lib.ArrayClass.GetArrayValueThis( this, i )
                showstr = showstr + cur.toString()
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