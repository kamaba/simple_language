
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

    bool static Call( BridgeKind kind, string dllName, string className, string method,  BridgeObject retObj, Array<BridgeObject> arrParams )
    {
        if kind == BridgeKind.CLR
        {
            CallCLRMethod( dllName,  className, method, retObj, arrParams);
        }
        elif kind == BridgeKind.NATIVE
        {
            CallNativeMethod( dllName,  className, method, retObj, arrParams );
        }
        elif kind == BridgeKind.JVM
        {
            CallJVMMethod( dllName,  className, method, retObj, arrParams );
        }
        ret true
    }
}