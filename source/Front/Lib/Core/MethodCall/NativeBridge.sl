
enum BridgeKind extends Byte
{
   SELF = 0
   CLR
   JVM
   NATIVE
}

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
    static BridgeKind _kind = BridgeKind.SELF

    bool static Call( BridgeKind kind, string dllName, string namespaceName, string className, string method,  BridgeObject retObj, Array<BridgeObject> arrParams )
    {
        if kind == BridgeKind.CLR
        {
            CallCLRMethod( dllName, namespaceName, className, method, retObj, arrParams);
        }
        elif kind == BridgeKind.NATIVE
        {
            CallNativeMethod( dllName, namespaceName, className, method, retObj, arrParams );
        }
        elif kind == BridgeKind.JVM
        {
            CallJVMMethod( dllName, namespaceName, className, method, retObj, arrParams );
        }
        #!
        for v in BridgeKind
        {
            #int index = v.index;
            #name = v.name
            if v == BridgeKind.SELF
            {

            }
        }
        for v in BridgeKind.values
        {

        }
        
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
        !#
        ret true
    }
}