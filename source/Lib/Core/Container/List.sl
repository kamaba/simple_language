
    public class Core.List interface Core.IIterable, Core.IIterator
    {
        int _length = 0
        int _index = 0;
        Object _current = null
        long _ptr = 0

           
        public static List createInstance(int length)
        {
            var list = List(length)
            ret list
        }

        _init_( int __len )
        {
            #uint allSize = __len * 4            
            this._length = __len
            #this._ptr = Lib.Array.CreateArray( length, 4 )
        }
        override void reset()
        {
            this._index = 0;
        }
        override bool moveNext()
        {            
            bool hasNext_var = this._index < this._length 
            if hasNext_var
            {
                this._current = SimpleLanguage.Lib.Array.GetArrayValueThis( this, this._index )
            }
            else
            {
                this._current = null
            }
            this._index++;
            System.Console.WriteLine("index=============== " + this._index )
            ret hasNext_var
            ret true
        }
        override object current()
        {
            ret this._current;
        }
        override void release()
        {
        }
        override IIterator iterator()
        {
            ret this
        }
        get T current<T>()
        {
            ret this._current as T;
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
            var retobj = SimpleLanguage.Lib.Array.GetArrayValueThis( this, ind )
            this._current = retobj;
        }
        set setValue( int __index, object val )
        {
            #Lib.Array.SetArrayValue( this._ptr, 5,  index, val )
            SimpleLanguage.Lib.Array.SetArrayValueThis( this, __index, val )
        }
        get object getValue( int __index )
        {
            #ret Lib.Array.GetArrayValue( this._ptr, 5,  index )
            ret SimpleLanguage.Lib.Array.GetArrayValueThis( this, __index )
        }
        setValues( Int64 valPtr, int len )
        {
            #Lib.Array.SetArrayValue( this._ptr, 1,  valPtr, len )
        }     
    }


    
    public class Core.List<T> interface Core.IIterable<T>, Core.IIterator<T>
    {
        int _length = 0
        int _capitaly = 0
        Type _type = null;
        _index = 0;
        T _current = null
        long _ptr = 0


        _init_( int __capitaly )
        {
            #uint allSize = __len * 4            
            this._length = __len
            #this._ptr = Lib.Array.CreateArray( length, 4 )
        } 
        override void reset()
        {
            this._index = 0;
        }
        override bool moveNext()
        {            
            bool hasNext_var = this._index < this._length 
            if hasNext_var
            {
                this._current = SimpleLanguage.Lib.Array.GetArrayValueThis( this, this._index )
            }
            else
            {
                this._current = null
            }
            this._index++;
            System.Console.WriteLine("index=============== " + this._index )
            ret hasNext_var
            ret true
        }
        override T current()
        {
            ret this._current;
        }
        override void release()
        {
        }
        override IIterator iterator()
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
            var retobj = SimpleLanguage.Lib.Array.GetArrayValueThis( this, ind )
            this._current = retobj;
        }
        set setValue( int __index, object val )
        {
            #Lib.Array.SetArrayValue( this._ptr, 5,  index, val )
            SimpleLanguage.Lib.Array.SetArrayValueThis( this, __index, val )
        }
        get object getValue( int __index )
        {
            #ret Lib.Array.GetArrayValue( this._ptr, 5,  index )
            ret SimpleLanguage.Lib.Array.GetArrayValueThis( this, __index )
        }
        setValues( Int64 valPtr, int len )
        {
            #Lib.Array.SetArrayValue( this._ptr, 1,  valPtr, len )
        }     
        public void add(T t )
        {
            if this._length < this._capitaly
            {
                SimpleLanguage.Lib.Array.SetArrayValueThis( this, this._length, val )
                this._length++
            }
            else
            {
                
            }
        }
        public void remove( T t )
        {

        }
    }