public class DllStaticImport extends Attribute
{
    # 运行前（Preload）生效: Front 只导出 attribute 数据（lib, symbol, sig），cvm 装配期
    #   注册表按名+stage 发现并绑定，把命中调用点改写为 CallFFIStatic(77)+绑定索引
    #   （与 DllImport 同款机制，ATTRIBUTE_DESIGN P10）；绑定失败运行期回退 SL 函数体
    # 与 DllImport 的区别: 库只按 jsonc dllImports "static" 静态注册名预载解析（不走路径/dllImports 兜底链）
    # 用法: @DllStaticImport("CLangdll", "sl_mul2", "i64->i64")（sig 可省略，导出层从函数签名补全）

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
