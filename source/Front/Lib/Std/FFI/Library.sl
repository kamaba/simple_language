#!
 * Std/FFI/Library.sl
 * FFI 动态库（FFI.Library，加载/符号解析/函数值）与静态辅助
 * （FFI.StaticLibrary，全局统计/回调注册）的 SL 封装（Dart 风格，
 * 见 md/design/ffi-design.md）。
 * 分层：cvm src/lib/ffi/（平台加载/引用计数/卸载）+
 * src/vm/system_method_call/ffi_system_method.c（VM 适配层，弹参压结果）。
 *
 * 说明：变参调用（SystemFFICallVoid / CallI32 / ... / CallF8E5M2）是
 * isVariadic 系统方法——实参须在调用点直接展开（SL 无法把变参标量
 * 打包后转发），因此不在本类提供包装。
!#

#! FFI 动态库句柄封装：加载 / 符号解析 / 引用计数 / 释放。 !#
public class FFI.Library extends Object
{
    #! 库句柄（0 = 无效 / 未加载）。 !#
    Int64 _handle = 0

    public override void _init_()
    {
    }

    #!
     * 按路径加载动态库。同路径共享同一份底层句柄（去重缓存），
     * 每次加载引用计数 +1。加载失败时句柄为 0。
    !#
    public void _init_( string path )
    {
        this._handle = SystemFFILoadLibrary( path )
    }

    #! 句柄（诊断用途）。 !#
    get Int64 handle()
    {
        ret this._handle
    }

    #! 是否已加载有效句柄。 !#
    get bool isValid()
    {
        ret this._handle != 0
    }

    #! 取导出符号地址（0 = 未找到）。 !#
    public Int64 getSymbol( string name )
    {
        ret SystemFFIGetSymbol( this._handle, name )
    }

    #!
     * 取导出函数并包装成可调用的 Function 值（Dart-FFI
     * lookupFunction + asFunction 风格）。sig 为 FFI 签名字符串，
     * 如 "i32,i32->i32"；类型名也接受 SL 名（"Int32"/"string"...）。
     * 失败（符号未找到 / 签名非法）时返回 null。
     *
     * 声明目标为 Func<Ret, P1, P2...> 或函数 typealias 且调用点只传
     * name 一个参数时，前端会从 Func 定义推导 sig 自动补全第二个实参。
    !#
    public Function getFunction( string name, string sig )
    {
        Int64 fn = this.getSymbol( name )
        if ( fn == 0 )
        {
            ret null
        }
        ret SystemFFIMakeFunction( fn, sig )
    }

    #! 引用计数 +1，返回新计数。 !#
    public Int32 addRef()
    {
        ret SystemFFILibraryRef( this._handle )
    }

    #! 当前引用计数（-1 = 句柄无效）。 !#
    public Int32 refcount()
    {
        ret SystemFFILibraryRefcount( this._handle )
    }

    #!
     * 引用计数 -1；返回 false 表示句柄无效。仅当本次是最后一个
     * 引用（归零并真正卸载）时才把句柄清 0；否则句柄仍然有效。
    !#
    public bool release()
    {
        if ( this._handle == 0 )
        {
            ret false
        }
        if ( SystemFFILibraryRefcount( this._handle ) <= 1 )
        {
            #! 最后一个引用：本次 release 将真正卸载。 !#
            bool ok = SystemFFIFreeLibrary( this._handle )
            if ( ok )
            {
                this._handle = 0
            }
            ret ok
        }
        ret SystemFFIFreeLibrary( this._handle )
    }
}

#! FFI 静态辅助：全局统计与回调注册。 !#
public class FFI.StaticLibrary extends Object
{
    public override void _init_()
    {
    }

    #! 当前已加载（未卸载）的库个数。 !#
    public static Int32 libraryCount()
    {
        ret SystemFFILibraryCount()
    }

    #!
     * 把 SL 静态方法注册为 native 可调用回调，返回 trampoline 地址
     * （0 = 失败）。签名约束：INT 类参数 <=2，返回 void 或 INT 类。
     * method 可传完整 method id 或短方法名（按参数个数匹配）。
    !#
    public static Int64 createCallback( string method, string sig )
    {
        ret SystemFFICreateCallback( method, sig )
    }

    #! 释放回调槽。 !#
    public static bool freeCallback( Int64 addr )
    {
        ret SystemFFIFreeCallback( addr )
    }

    #!
     * @DllImport 声明式绑定辅助：加载库并取符号包装为 Function 值。
     * 编译期 @DllImport 注入此单层静态调用，内部即链式
     * FFI.Library( path ).getFunction( symbol, sig )。库按 C#
     * DllImport 语义进程级常驻（去重缓存共享句柄，不 release）。
    !#
    public static Function bindFunction( string path, string symbol, string sig )
    {
        ret FFI.Library( path ).getFunction( symbol, sig )
    }
}
