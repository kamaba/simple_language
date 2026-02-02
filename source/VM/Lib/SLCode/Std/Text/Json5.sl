
namespace Std.Data
{
    public class Node
    {
        
    }
    public class Json extends Tree
    {
        _init_( string str )
        {

        }
        _init_( data _data )
        {
            
        }
    }
}

JsonTest
{
    static fun()
    {
        string js = '{"name":"okg", "age":15, "info":{
            "book":["love","mf", "fck"],
            "address":
            {
                "nation":"zh",
                "priv":"beijin",
                "area":"sjm",
                "code":100012
            }
        } }'

        j = Json(js)

        zh = j.@info.@address.@native

        dzh = j.data.info.address.native

        bookarr = j.data.info.book

        bookarr.add( "ok" )

        js.@info.@address.add( "bingo", "map" );

        bookarv = j.data.info.book.@2

        for v in j.data.info.book
        {
            Debug.WriteLine(" book=" + v )
        }
    }
}