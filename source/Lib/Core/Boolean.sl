import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage

public class Boolean extends Object
{
    bool _value = false

    _init_( bool b )
    {
        this._value = b
    }
    override string toString()
    {
        ret "True" ? this._value : "False"
    }
}