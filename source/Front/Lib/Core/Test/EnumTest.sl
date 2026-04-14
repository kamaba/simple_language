ForTest
{
    printBridgeKind( BridgeKind v )
    {

            if v == BridgeKind.SELF 
            {
                println( "BridgeKind--------------SELF " )
            }
            elif v == BridgeKind.JVM
            {
                println( "BridgeKind--------------JVM " )
            }
            else
            {
                println( "BridgeKind--------------NATIVE " )
            }
    }
    static fun()
    {        
    }
}