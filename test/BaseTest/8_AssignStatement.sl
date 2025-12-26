import Std
import CSharp.SimpleLanguage
import CSharp.System

namespace Core
{
    class Object
    {
        public void _init_()
        {

        }
        public string toString()
        {
            ret ""
        }
    }
    class Byte extends Object
    {
    }
    class Boolean
    {

    }
    class SByte
    {
        
    }
    class Int16
    {
        
    }
    class UInt16
    {
        
    }
    class Int32
    {
        Int32 _value = 0i
        _init_( Int32 val )
        {
            this._value = val
        }
        override string toString()
        {
            ret SimpleLanguage.Lib.StringClass.Int32ToString(this._value)
        }
    }
    class UInt32
    {
        
    }
    class Int64
    {
        
    }
    class UInt64
    {
        
    }
    class Float32
    {
        
    }
    class Float64
    {
        _init_(Float64 f)
        {

        }
    }
    class String
    {
        String _value = "";
        _init_( Int32 _val )
        {
            
        }
        _init_( String str )
        {

        }
    }
    public class MetaClass
    {
        _namespaceName = "";
        _className = "";

        get string className(){ ret this._className; }
    }
    public class Type
    {
        int _hashCode = 0
        MetaClass _metaClass = null
        public Type[] typelist = null

        override string toString()
        {
            if( this._metaClass == null )
            {
                ret "no_meta_class"
            }

            ret this._metaClass.className;
        }
    }
    public interface IIterator
    {
        void reset()
        bool moveNext()
        get object current()
        void release()
    }
    public interface IIterable
    {
        IIterator iterator()
    }
    public interface IIterator<T>
    {
        void reset()
        bool moveNext()
        get T current()
        set void current( T t )
        void release()
    }
    public interface IIterable<T>
    {
        IIterator<T> iterator()
    }
    public class Array<T> interface IIterable<T>, IIterator<T>
    {
        int _length = 0
        Type _type = null;
        _index = 0;
        T _current = null
        long _ptr = 0

        _init_( int __len )
        {
            #uint allSize = __len * 4            
            this._length = __len
            #this._ptr = Lib.ArrayClass.CreateArray( length, 4 )
        }
        get int length(){ ret this._length }
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
}


AssignStatement
{
    ArrClass
    {
        int i1 = 0;
        i2 = "aaa"

        set i1set( int i111 )
        {
            this.i1 = i111;
        }
    }
    Level<T>
    {
        T t = new()
        _init_( obj )
        {
            this.t = obj as T
        }
        override string toString()
        {
            ret this.t.toString()
        }
    }
    static fun()
    { 
        ArrClass ac = new(){ i2 = "bbb" }
        ac.i1 = 10
        ac.i1 += 20
        ac.i1++
        ac.i1 = ac.a1++;    #不允许单独使用++，只能在单条表达式中使用
        ac.i1 = (20/3).toInt32() + 104
        ac.i1set = 250
    }
}