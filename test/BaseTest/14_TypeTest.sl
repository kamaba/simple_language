import Std
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
        _init_(Int32 val )
        {
            
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
        public Type[] typelist = new()

        override string toString()
        {
            if( this._metaClass == null )
                ret "no_meta_class"

            ret this._metaClass.className;
        }
    }    
}
TypeTest
{
    ArrClass
    {
        int i = 0;
    }
    Level<T> 
    {
        static fun()
        {
            type1 = Level<T>.type
            type2 = Level<int>.type
            Console.WriteLine("levelT.type" + type1.toString() );
        }
    }
    static bool IsNType( Type t )
    {
        return t == int.type;
    }
    static fun()
    {  
        t = int.type()
        int i2 = 20
        t2 = i2.type

        if t == t2 
        {
            System.Console.WriteLine("22222222= " t2.toString() )
        }

        #!
        bool a = IsNType( ArrClass.type )
        var t = List<int>.type
        ArrayClass.type()
        var mcname mmi = t.metaClass.name

        System.Console.WriteLine("22222222= " t2.toString() )
        !#
    }
}