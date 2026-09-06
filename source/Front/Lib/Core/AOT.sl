public class AOT extends Attribute
{
    # AOT（Ahead-Of-Time）预编译属性
    # 用法: @AOT() 或 @AOT( 2, "x64", "static", true, false, false ) 标注在类/成员上
    # 当前仅注册到编译时处理器，暂不关联其它导出逻辑
    #
    # 参数参考其它语言的 AOT 实现：
    #   .NET NativeAOT: PublishAot / TrimMode / RID(windows-x64, linux-arm64)
    #   GraalVM Native Image: -O / --target / --static / --shared / -g
    #                         / --initialize-at-build-time / --no-fallback
    #   GCC/Clang: -O0..-O3 / -g / -static / -shared / target triple

    # 优化级别: 0=无优化 1=基础优化(默认) 2=激进优化
    # 参考 GraalVM -O、GCC -O0~-O3
    private Int32 _optimizeLevel = 1

    # 目标平台: 如 "x64" / "arm64" / "win-x64" / "linux-amd64"，空为当前平台
    # 参考 .NET RID、GraalVM --target、GCC target triple
    private string _target = ""

    # 链接模式: ""=可执行文件(默认) / "static"=静态链接 / "shared"=共享库
    # 参考 GraalVM --static / --shared、GCC -static / -shared
    private string _linkMode = ""

    # 是否生成调试信息
    # 参考 GraalVM -g、GCC -g
    private bool _isDebugInfo = false

    # 是否启用裁剪（移除未使用的代码/元数据）
    # 参考 .NET PublishTrimmed / TrimMode、Mono linker
    private bool _isTrimming = false

    # 是否在构建期完成初始化（静态数据提前到编译期）
    # 参考 GraalVM --initialize-at-build-time
    private bool _isInitializeAtBuildTime = false

    # 无参构造: @AOT() 使用全部默认值
    override _init_()
    {
        this._attributeHandleType = 0
    }

    # 全参构造: @AOT( optimizeLevel, target, linkMode, isDebugInfo, isTrimming, isInitializeAtBuildTime )
    _init_( Int32 optimizeLevel, string target, string linkMode, bool isDebugInfo, bool isTrimming, bool isInitializeAtBuildTime )
    {
        this._optimizeLevel = optimizeLevel
        this._target = target
        this._linkMode = linkMode
        this._isDebugInfo = isDebugInfo
        this._isTrimming = isTrimming
        this._isInitializeAtBuildTime = isInitializeAtBuildTime
        this._attributeHandleType = 0
    }

    public get Int32 optimizeLevel()
    {
        ret this._optimizeLevel
    }

    public get string target()
    {
        ret this._target
    }

    public get string linkMode()
    {
        ret this._linkMode
    }

    public get bool isDebugInfo()
    {
        ret this._isDebugInfo
    }

    public get bool isTrimming()
    {
        ret this._isTrimming
    }

    public get bool isInitializeAtBuildTime()
    {
        ret this._isInitializeAtBuildTime
    }

    # 编译时回调 - 预留，暂无逻辑
    # 后续由 C# 侧 AttributeManager / ExportAot / LLVMEmitter 读取参数决定导出行为
    override void OnCompile()
    {
    }
}
