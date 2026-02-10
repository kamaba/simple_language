
enum BridgeKind 
{
   SELF,
   CLR,
   JVM,
   NATIVE
};

public class BridgeObject extends Object
{
    const BridgeObject voidObject = new("void")
    const BridgeObject int32Object = new("Int32")
    const BridgeObject float32Object = new("Float32")
    const BridgeObject stringObject = new("string")

    _init_( string type )
    {
        
    }
}

public class NativeBridge extends Object
{
    BridgeKind kind = BridgeKind.SELF

    bool static Call( BridgeKind kind, string dllName, string namespaceName, string className, string method,  BridgeObject retObj, Array<BridgeObject> arrParams )
    {
        switch kind
        {
            case BridgeKind.SELF
            {
                
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
        }
        ret true
    }
}