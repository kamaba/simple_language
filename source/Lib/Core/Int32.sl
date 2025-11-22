import CSharp.SimpleLanguage

Core.Int32 extends Object
{
    const int MaxValue = 0x7fffffff;
    const int MinValue = unchecked((int)0x80000000);

    #private Int32 _value = 0i;

    _init_()
    {
    }
    _init_( Int32 _val )
    {
        SimpleLanguage.Lib.Int32Class.SetInt32Value( this, _val )
        #this._value = _val        
    }
    override String toString()
    {
        ret SimpleLanguage.Lib.Int32Class.ConvertToString( this )
    }
    static String Int32ToString( Int32 value )
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToString( value )
    }
    #!
    cast( Type t )
    {
        if t == Int16.type 
        {
            ret Convert.Int32ConvertToInt16( m_Value )
        }
    }

    public int compareTo(object value)
    {
        if (value == null)
        {
            ret 1;
        }

        ret 0
    }

    public static int parse( string s )
    {
        ret 0
    }
    !#
}