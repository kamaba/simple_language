public class DllStaticImport extends Attribute
{
    # 运行前（Preload）生效: 编译期发射 CallFFIStatic(77) + 装配期原生静态绑定（双轨, 机制不变）
    # 与 DllImport 的区别: 调用点直接编译为 FFI 静态调用, 不走 SL fallback
    # 用法: @DllStaticImport("CLangdll", "sl_mul2", "i64->i64")

    private string _library = ""

    private string _entryName = ""

    private string _signature = ""

    _init_( string library, string entryName, string signature )
    {
        this._library = library
        this._entryName = entryName
        this._signature = signature
        this._attributeStage = EAttributeStage.Preload
        this._attributeTargets = EAttributeTarget.Method
    }

    public get string library()
    {
        ret this._library
    }

    public get string entryName()
    {
        ret this._entryName
    }

    public get string signature()
    {
        ret this._signature
    }
}
