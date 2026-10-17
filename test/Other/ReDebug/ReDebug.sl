import Std

ReDebug
{
    static fun()
    {
        var re = Text.RegExp( "([a-z]+)([0-9]+)" )
        Console.println( "gc=" + re.groupCount.toString() )
        var m = re.search( "mail: bob@exa.com end" )
        if m != null
        {
            Console.println( "search ok: " + m.value() )
            Console.println( "g1=" + m.group( 1 ) + " g2=" + m.group( 2 ) )
        }
        else
        {
            Console.println( "search null" )
        }
        var all = re.matchAll( "ab12 cd34" )
        Console.println( "matchAll n=" + all.length.toString() )
        var rep = Text.RegExp( "[0-9]+" ).replace( "a1b2c3", "#" )
        Console.println( "replace: " + rep )
        Console.println( "ReDebug end" )
    }
}
