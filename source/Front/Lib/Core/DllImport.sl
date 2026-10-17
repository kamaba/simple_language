public class DllImport extends Attribute
{
    # 运行前（Preload）生效: 装配期由 VM 原生 handler 将外部函数映射为 CVM 可调用入口
    # 标注在 static 函数声明上, 带 fallback 函数体（绑定失败时回退执行 SL 实现）
    # 用法:
    #   @DllImport("path.dll", "func")
    #   @DllImport("CLangdll", "sl_mul2", "i64->i64")   别名+签名

    # 库路径或已注册库名（如 "CLangdll" 或相对路径）
    private string _library = ""

    # 导出函数名
    private string _entryName = ""

    # 签名串（如 "i64->i64"）, 空 = 按 fallback 体推导
    private string _signature = ""

    _init_( string library, string entryName )
    {
        this._library = library
        this._entryName = entryName
        this._attributeStage = EAttributeStage.Preload
        this._attributeTargets = EAttributeTarget.Method
    }

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
