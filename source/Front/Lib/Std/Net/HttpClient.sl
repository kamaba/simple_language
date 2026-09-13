
namespace Http
{
    enum EMethod
    {
        Get = 0
        Post
        Update
    }
    public class Response
    {
        codeState = 0
        content = ""
    }
    public class Client
    {
        _init_( string url )
        {
            
        }

        public Response send()
        {
            ret Response()
        }
    }

    HttpTest
    {
        static fun()
        {
            Client c = new("http://www.baidu.com" )

            response = c.send()

            if response.codeState == 200
            {
                Console.write( response.content )
            }

            c2 = Client( "https://www.qq.com" ) 
            res = c2.send()
            if res.codeState == 200 
            {
                
            }
        }
    }
}