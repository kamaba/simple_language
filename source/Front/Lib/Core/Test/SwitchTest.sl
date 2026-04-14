import CSharp.System
import CSharpLang.SimpleLanguage

ForTest
{
    static fun()
    {
        a = 200;
        switch kind
        {
            case BridgeKind.SELF
            {
                a = 1
                next
            }
            case BridgeKind.CLR
            {

            }
            case BridgeKind.JVM
            {

            }
            case BridgeKind.NATIVE
            {

            }     
            default{
                a = 20;
            }       
        }        
    }
}