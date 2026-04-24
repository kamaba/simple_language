CallOtherLanguage
{
    static fun()
    {
        @cs( 20, 0xff,  a2, out bb )
        {
            using System;

            int a = 0xff
            string b = a2.ToString()

            var ab = Convert.ToInt16(a)

            public class OK
            {
                public int a {get;set;} = 0
            }
            System.Console.WriteLine( b )
        }
        @js( 20, 10, out var addvar  )
        {
            int a = int.parseInt( args[0] )
            int b = int.parseInt( args[1] )

            var addfun = async function( p1, p2 )
            {
                return p1 + p2;
            }
            addvar = await addfun( a, b )
        }

        @java( out var add )
        {

        }

        @py
        {
            
        }
    }
}