
enum BridgeKind extends Byte
{
   SELF = 0
   CLR
   JVM
   NATIVE
}

public class BridgeObject extends Object
{
    public bool boolvalue = false;
    public Int32 int32value = 0;
    public Int64 int64value = 0L;
    public Float64 float64value = 0.0d;
    public string stringvalue = "";

    public int valuetype = 0;

    
    _init_( bool _value )
    {
        this.boolvalue = _value
        this.valuetype = 0
    }
    _init_( Int32 _value )
    {
        this.int32value = _value
        this.valuetype = 1
    }
    _init_( Int64 _value )
    {
        this.int64value = _value
        this.valuetype = 2
    }
    _init_( Float64 _value )
    {
        this.float64value = _value
        this.valuetype = 3
    }
    _init_( string _value )
    {
        this.stringvalue = _value
        this.valuetype = 4
    }
}

public class NativeBridge extends Object
{
    static BridgeKind _kind = BridgeKind.SELF

    bool static Call( BridgeKind kind, string dllName, string className, string method,  BridgeObject retObj, Array<BridgeObject> arrParams )
    {
        if kind == BridgeKind.CLR
        {
            SystemCallCLRMethod( dllName,  className, method, retObj, arrParams);
        }
        elif kind == BridgeKind.NATIVE
        {
            SystemCallNativeMethod( dllName,  className, method, retObj, arrParams );
        }
        elif kind == BridgeKind.JVM
        {
            SystemCallJVMMethod( dllName,  className, method, retObj, arrParams );
        }
        ret true
    }
}