
namespace Std.Text
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
        Json a = Json('{"a":10, b = [1,2,3,4] }' )  #通过字符串转为Json

        Json a = Json( {a = 20, b = [1,2,3,5 ] } )  #匿名data 转为json

        a = Json();
        a["a"] = 20;
        a["b"] = [1,2,3,4];     #手动构建json内容

        a.delete("a")  

        a.$"b".$1 #访问里边某个元素
        
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

        zh = j.$"info".$"address".@"native"

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