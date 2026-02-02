
namespace Http
{
    public enum EMethod
    {
        Get,
        Post,
        Update
    }
    public class Response
    {

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
                Console.Write( response.content )
            }

            c2 = Client(Uri.parse(https://www.qq.com" ) )
            res = c2.send()
            if res.codeState == 200 
            {
                
            }
        }
    }
}