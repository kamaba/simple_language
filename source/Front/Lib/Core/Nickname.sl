public class Nickname extends Attribute
{   
    private string nickname = "";
    _init_( string _nickname )
    {        
        this.nickname = _nickname;
    }
    override String toString()
    {        
        ret SystemConvertString( this )
    }
}