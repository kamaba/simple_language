import Std;

# 函数类型别名（typealias X = Ret Function( P... ) 形态）：
# 与 Func<Ret, P...> 等价, FFI 取函数时可从别名签名推导 FFI sig
typealias AddFuncTA = int Function( int, int )
typealias EchoFuncTA = string Function( string )

# ============================================================
# 具名 data <-> C struct 互转测试类型：
# 覆盖内置标量/string/嵌套 data/enum/class 引用成员
# （对应 C struct 布局见 testDataToStruct 注释）
# ============================================================
class FFIHoldClass
{
    value = 0
}

enum FFIKind
{
    A = 1
    B = 2
}

data FFIMetaInfo
{
    level = 1
    flag = true
}

data FFIStructSample
{
    id = 0
    score = 0.0
    name = "x"
    meta = FFIMetaInfo(){ level = 3, flag = true }
    kind = FFIKind.B
    hold = FFIHoldClass(){ value = 222 }
}

# ============================================================
# FFICallbacks - FFI 回调目标类
# C 侧经 trampoline 重入 VM 调用这里的静态方法（签名约束：
# INT 类参数 <= 2，返回 INT 类；形参全用 Int64 对齐 trampoline 形状）。
# 方法 id：SpecialTest.FFICallbacks.OnAdd_2_Core.Int64_Core.Int64
# ============================================================
class FFICallbacks
{
    static Int64 OnAdd( Int64 a, Int64 b )
    {
        ret a + b
    }

    static Int64 OnMul( Int64 a, Int64 b )
    {
        ret a * b
    }
}

# ============================================================
# FFITest - FFI 动态库加载/调用端到端测试
# 测试对象：source/CLangdll/x64/Debug/CLangdll.dll（CVM 相对 CWD 解析）
# 覆盖：
#   1. 库生命周期：加载 / addRef / 同路径去重共享 / 释放 / 引用计数归零卸载
#   2. 基础标量调用（i32 / i64）
#   3. 多宽度标量混合（u8,i16,u32,i64,f32,f64 -> f64）
#   4. Float8 data 数据 -> struct 位域分解（u8 位模式 / e4m3 值 / e5m2 值 / 双格式对照）
#   5. Float8（E4M3/E5M2）/ Float16 roundtrip（只取精确可表示值）
#   6. 混合标量打包进 struct + SystemPtr 读回/改写 + native 读回
#   7. 函数指针：native 返回函数指针（get_adder/get_multiplier）、ptr 形参调用
#   8. 回调：SL 静态方法注册为 native 可调用（含嵌套重入/双回调）
#   9. Utf8 字符串进出（echo/concat/strlen）
#  10. sl_exports_json 导出清单（ffi-design.md 0.1 节约定）
#  11. 函数值：Func<Ret,P...>/typealias 声明 native 函数变量（Dart lookupFunction 风格）
#      getFunction/getSymbol 只传 name 时编译期从函数类型定义推导 sig 注入
#  12. Memory native（原 FFI.NativeMemory 迁入 Core/Memory）：动态构建
#      C 数组/标量/struct（alloc/读写/布局/native 校验，free->freeNative）
#  12b. Memory data <-> Array<object> 互转（arrayToData/dataToArray，
#      fields 布局串；cvm 层数据 <-> C struct 数据格式转换）
#  13. lookupFunction<Ret,P...>(name)：调用点模板实参注入 sig（内部仍走 getFunction）
#  14. @DllImport(路径,符号[,sig])：C# DllImport 风格 attribute + 函数定义
#      （static 返回类型 函数名( 参数类型 参数名, ... ){ ...fallback 体... }，
#      必须带函数体；dll 绑定可用时转发 dll 导入函数，不可用（平台不符/
#      加载失败/符号不存在）时执行函数体（fallback 本体）；sig 缺省从签名推导）
#  15. dllImports 配置：project.jsonc "dllImports" 段（路径/名称/别名），
#      global.dllImport.<alias> 直接访问库对象；@DllImport 第 1 实参可用别名
# 16. @DllImport C# P/Invoke 风格函数声明（与 14 等价）：
#      static 返回类型 函数名( 参数类型 参数名, ... ) -> 编译期合成
#      隐藏 Func 字段 + 同名静态函数 wrapper，直接调用（内部仍 LoadLibrary）
# 17. C# 风格声明 + 独立别名（别名 != 库名，dllImports 查表按 alias 命中）
# 18. 调用点常量隐式窄化（C# 隐式常量转换：int 字面量 -> byte/short 形参，
#      含一元负折叠与边界值 255/-32768/32767）
#  19. @DllImport fallback：库不存在（绑定失败 -> 隐藏字段 null）时执行
#      函数本体（SL 实现的等价逻辑）
#  20. dllImports functions 段：project.jsonc 项内 "functions" 列表注入
#      Project 静态库函数变量，global.<funcName>(...) 直接调用
#      （C# DllImport 语义；cvm 层数据经 arrayToData 转 C struct 供特殊调用）
# ============================================================
FFITest
{
    static string s_dllPath = "../../source/CLangdll/x64/Debug/CLangdll.dll"

    # ── @DllImport：C# [DllImport("libdemo.so")] static extern Int32 add(...) 风格 ──
    # 统一为函数定义形式（旧 static Func<...> 字段形式已废弃）；
    # 函数体为必填：dll 绑定可用时调用转发 dll 导入函数，不可用
    #（运行平台不符 / 库加载失败 / 符号不存在）时执行函数体（fallback
    # 本体，此处为与 dll 等价的 SL 实现）；sig 缺省从签名推导（i32,i32->i32）
    @DllImport( "../../source/CLangdll/x64/Debug/CLangdll.dll", "simplelanguage_addtest" )
    static int s_dllAdd( int a, int b )
    {
        ret a + b
    }

    # 第 3 实参手写 sig 覆盖推导（Ptr 等不可推导类型用）
    @DllImport( "../../source/CLangdll/x64/Debug/CLangdll.dll", "sl_mul2", "i64->i64" )
    static Int64 s_dllMul2( Int64 v )
    {
        ret v * 2
    }

    # utf8 进出：utf8->i64
    @DllImport( "../../source/CLangdll/x64/Debug/CLangdll.dll", "sl_strlen_utf8" )
    static Int64 s_dllStrlen( string s )
    {
        ret s.length
    }

    # 零参：->utf8
    @DllImport( "../../source/CLangdll/x64/Debug/CLangdll.dll", "sl_exports_json" )
    static string s_dllJson()
    {
        ret "{}"
    }

    # 6 参全宽度混合：u8,i16,u32,i64,f32,f64->f64
    @DllImport( "../../source/CLangdll/x64/Debug/CLangdll.dll", "sl_mix_all" )
    static double s_dllMix( byte a, short b, uint c, long d, float e, double f )
    {
        # fallback：与 sl_mix_all 等价（a*1 + b*2 + c*3 + d*4 + e*5 + f*6）
        double da = a
        double db = b
        double dc = c
        double dd = d
        double de = e
        ret da * 1.0 + db * 2.0 + dc * 3.0 + dd * 4.0 + de * 5.0 + f * 6.0
    }

    # ── @DllImport 别名形态：第 1 实参用 project.jsonc "dllImports" 配置的别名 ──
    #（编译期查表替换为配置路径，免在代码里写长路径）
    @DllImport( "CLangdll", "sl_mul2", "i64->i64" )
    static Int64 s_dllMul2Alias( Int64 v )
    {
        ret v * 2
    }

    @DllImport( "CLangdll", "sl_exports_json" )
    static string s_dllJsonAlias()
    {
        ret "{}"
    }

    # ── @DllImport C# P/Invoke 风格函数声明（与上方形态等价，均须带 fallback 体）──
    # @DllImport( "库", "符号"[, "sig"] ) static 返回类型 函数名( 参数类型 参数名, ... ) { ... }
    # 改写为隐藏 Func 字段 + 同名静态 wrapper（内部仍走 LoadLibrary 体系），
    # 类内可直接按普通函数调用；dll 不可用时执行函数体（fallback 本体）
    @DllImport( "CLangdll", "sl_add" )
    static Int64 addCs( Int64 a, Int64 b )
    {
        ret a + b
    }

    @DllImport( "CLangdll", "simplelanguage_addtest" )
    static int addtestCs( int a, int b )
    {
        ret a + b
    }

    # 第 3 实参手写 sig
    @DllImport( "CLangdll", "sl_mul2", "i64->i64" )
    static Int64 mul2Cs( Int64 v )
    {
        ret v * 2
    }

    @DllImport( "CLangdll", "sl_strlen_utf8" )
    static Int64 strlenCs( string s )
    {
        ret s.length
    }

    # 零参
    @DllImport( "CLangdll", "sl_exports_json" )
    static string exportsJsonCs()
    {
        ret "{}"
    }

    # void 返回 + ptr 参数（sig 不可推导，手写 "u8,ptr->void"）
    # fallback：void 空体即可（dll 可用时由 dll 完成结构体写入）
    @DllImport( "CLangdll", "sl_float8_bits_to_struct", "u8,ptr->void" )
    static void bitsToStructCs( byte bits, Int64 p )
    {
    }

    # 6 参全宽度混合（sig 从参数类型推导）
    @DllImport( "CLangdll", "sl_mix_all" )
    static double mixCs( byte a, short b, uint c, long d, float e, double f )
    {
        # fallback：与 sl_mix_all 等价（a*1 + b*2 + c*3 + d*4 + e*5 + f*6）
        double da = a
        double db = b
        double dc = c
        double dd = d
        double de = e
        ret da * 1.0 + db * 2.0 + dc * 3.0 + dd * 4.0 + de * 5.0 + f * 6.0
    }

    # ── C# 风格 + 独立别名（别名 != 库名，验证查表按 alias 命中）──
    @DllImport( "cl", "sl_add" )
    static Int64 addCsAlias( Int64 a, Int64 b )
    {
        ret a + b
    }

    @DllImport( "cl", "sl_mix_all" )
    static double mixCsAlias( byte a, short b, uint c, long d, float e, double f )
    {
        # fallback：与 sl_mix_all 等价（a*1 + b*2 + c*3 + d*4 + e*5 + f*6）
        double da = a
        double db = b
        double dc = c
        double dd = d
        double de = e
        ret da * 1.0 + db * 2.0 + dc * 3.0 + dd * 4.0 + de * 5.0 + f * 6.0
    }

    # ── @DllImport fallback：库不存在（绑定失败 -> 隐藏字段 null）时走函数体 ──
    @DllImport( "no_such_library_xyz.dll", "no_such_symbol" )
    static int s_fbAdd( int a, int b )
    {
        ret a + b
    }

    # 简单断言辅助：打印 PASS / FAIL
    static check( string name, bool cond )
    {
        if ( cond )
        {
            Console.println( "  [PASS] " + name )
        }
        else
        {
            Console.println( "  [FAIL] " + name )
        }
    }

    # ── 1. 库生命周期（相对基准断言）──
    # 注意：本类 @DllImport / getFunction 静态初始化器在类加载时已加载
    # s_dllPath（进程常驻引用，按 C# DllImport 语义不 release），故本测试
    # 不假设全局引用从 0 开始，也不能把库 release 到卸载（静态字段仍
    # 持有引用，卸载会使静态 Func 的 native 指针悬空）。引用计数的
    # 加/减/去重/句柄有效性语义以 load 后的基准 r0 相对验证；
    # 归零真正卸载属 C 层 sl_ffi_lib_release 的职责。
    static testLibraryLifecycle()
    {
        Console.println( "===== FFITest.testLibraryLifecycle =====" )
        int baseCount = FFI.StaticLibrary.libraryCount()
        var lib = FFI.Library( s_dllPath )
        int r0 = lib.refcount()
        check( "load: isValid", lib.isValid )
        check( "load: refcount>=1 (static fields hold base refs)", r0 >= 1 )
        check( "load: libraryCount unchanged (dedup)", FFI.StaticLibrary.libraryCount() == baseCount )

        # 同一对象引用计数 +1
        check( "addRef -> r0+1", lib.addRef() == r0 + 1 )

        # 同路径再加载：共享底层句柄（去重缓存），refcount 累加
        var lib2 = FFI.Library( s_dllPath )
        check( "reload same path: refcount==r0+2", lib.refcount() == r0 + 2 )
        check( "reload same path: libraryCount unchanged", FFI.StaticLibrary.libraryCount() == baseCount )

        # 非最终 release：引用递减但句柄仍有效
        check( "lib2.release() ok (r0+2->r0+1)", lib2.release() )
        check( "non-final release: lib2 still valid", lib2.isValid )
        check( "non-final release: refcount==r0+1", lib2.refcount() == r0 + 1 )

        # 递减回基准：静态常驻引用仍在，库不卸载、句柄保持有效
        check( "lib.release() ok (r0+1->r0)", lib.release() )
        check( "base refs held: lib still valid", lib.isValid )
        check( "base refs held: libraryCount unchanged", FFI.StaticLibrary.libraryCount() == baseCount )
        check( "refcount back to r0", lib.refcount() == r0 )

        # 加载失败：句柄为 0，!isValid 且 release 返回 false（死句柄语义）
        var bad = FFI.Library( "no_such_library_zzz.dll" )
        check( "load failed: !isValid", bad.isValid == false )
        check( "release on dead handle -> false", bad.release() == false )
        check( "libraryCount unchanged after failed load", FFI.StaticLibrary.libraryCount() == baseCount )
    }

    # ── 2. 基础标量调用 ──
    static testBasicCall()
    {
        Console.println( "===== FFITest.testBasicCall =====" )
        var lib = FFI.Library( s_dllPath )
        Int64 addtest = lib.getSymbol( "simplelanguage_addtest" )
        Int64 addi64 = lib.getSymbol( "sl_add" )
        check( "resolve simplelanguage_addtest", addtest != 0 )
        check( "resolve sl_add", addi64 != 0 )

        check( "addtest(20,22)==42", SystemFFICallI32( addtest, "i32,i32->i32", 20, 22 ) == 42 )
        check( "addtest(-5,7)==2", SystemFFICallI32( addtest, "i32,i32->i32", -5, 7 ) == 2 )
        check( "sl_add(11,31)==42", SystemFFICallI64( addi64, "i64,i64->i64", 11, 31 ) == 42 )
        lib.release()
    }

    # ── 3. 多宽度标量混合：u8/i16/u32/i64/f32/f64 同一函数 ──
    static testScalarMix()
    {
        Console.println( "===== FFITest.testScalarMix =====" )
        var lib = FFI.Library( s_dllPath )
        Int64 mixall = lib.getSymbol( "sl_mix_all" )
        Int64 u32v = 3000000000
        Int64 i64v = 5000000000
        # 200*1 + (-1000)*2 + 3000000000*3 + 5000000000*4 + 2.5*5 + 1.25*6
        Float64 r = SystemFFICallF64( mixall, "u8,i16,u32,i64,f32,f64->f64", 200, -1000, u32v, i64v, 2.5, 1.25 )
        check( "mix_all == 28999998220.0", r == 28999998220.0d )
        lib.release()
    }

    # ── 4. Float8 data 数据 -> struct 位域分解 ──
    # SLFloat8Parts 布局（自然对齐，sizeof=32）：
    #   offset  0: int32 format / 4: int32 bits / 8: int32 sign
    #   offset 12: int32 exponent / 16: int32 mantissa / 24: float64 value
    static testFloat8Struct()
    {
        Console.println( "===== FFITest.testFloat8Struct =====" )
        var lib = FFI.Library( s_dllPath )
        Int64 bitsToStruct = lib.getSymbol( "sl_float8_bits_to_struct" )
        Int64 e4ToStruct = lib.getSymbol( "sl_float8_e4m3_to_struct" )
        Int64 e5ToStruct = lib.getSymbol( "sl_float8_e5m2_to_struct" )
        Int64 dualToStruct = lib.getSymbol( "sl_float8_dual_to_struct" )
        Int64 structToBits = lib.getSymbol( "sl_struct_to_float8_bits" )
        Int64 p = SystemPtrAlloc( 64 )
        Int64 p2 = SystemPtrAlloc( 64 )

        # u8 原始位模式 60(=0x3C: sign0 exp7 mant4) -> struct
        SystemFFICallVoid( bitsToStruct, "u8,ptr->void", 60, p )
        check( "bits60 format==0(E4M3)", SystemPtrReadInt32( p, 0 ) == 0 )
        check( "bits60 bits==60", SystemPtrReadInt32( p, 4 ) == 60 )
        check( "bits60 sign==0", SystemPtrReadInt32( p, 8 ) == 0 )
        check( "bits60 exponent==7", SystemPtrReadInt32( p, 12 ) == 7 )
        check( "bits60 mantissa==4", SystemPtrReadInt32( p, 16 ) == 4 )
        check( "bits60 value==1.5", SystemPtrReadFloat64( p, 24 ) == 1.5 )

        # Float8(E4M3) 值 1.5fe4 -> struct（量化后同为位模式 60）
        SystemFFICallVoid( e4ToStruct, "f8e4m3,ptr->void", 1.5fe4, p )
        check( "e4m3 1.5 bits==60", SystemPtrReadInt32( p, 4 ) == 60 )
        check( "e4m3 1.5 value==1.5", SystemPtrReadFloat64( p, 24 ) == 1.5 )

        # Float8(E5M2) 值 2.0fe5 -> struct（位模式 64: exp16 mant0）
        SystemFFICallVoid( e5ToStruct, "f8e5m2,ptr->void", 2.0fe5, p )
        check( "e5m2 2.0 format==1", SystemPtrReadInt32( p, 0 ) == 1 )
        check( "e5m2 2.0 bits==64", SystemPtrReadInt32( p, 4 ) == 64 )
        check( "e5m2 2.0 exponent==16", SystemPtrReadInt32( p, 12 ) == 16 )
        check( "e5m2 2.0 mantissa==0", SystemPtrReadInt32( p, 16 ) == 0 )
        check( "e5m2 2.0 value==2.0", SystemPtrReadFloat64( p, 24 ) == 2.0 )

        # 同一 f32 值双格式对照：E4M3 exp=8 vs E5M2 exp=16
        SystemFFICallVoid( dualToStruct, "f32,ptr,ptr->void", 2.0f, p, p2 )
        check( "dual e4m3 exponent==8", SystemPtrReadInt32( p, 12 ) == 8 )
        check( "dual e4m3 value==2.0", SystemPtrReadFloat64( p, 24 ) == 2.0 )
        check( "dual e5m2 exponent==16", SystemPtrReadInt32( p2, 12 ) == 16 )
        check( "dual e5m2 value==2.0", SystemPtrReadFloat64( p2, 24 ) == 2.0 )

        # struct -> 位模式重组：sign=1 exp=8 mant=2 -> 194(0xC2) -> 值 -2.5
        SystemPtrWriteInt32( p, 8, 1 )
        SystemPtrWriteInt32( p, 12, 8 )
        SystemPtrWriteInt32( p, 16, 2 )
        Int32 recombined = SystemFFICallI32( structToBits, "ptr->i32", p )
        check( "struct->bits == 194", recombined == 194 )
        SystemFFICallVoid( bitsToStruct, "u8,ptr->void", recombined, p )
        check( "bits194 sign==1", SystemPtrReadInt32( p, 8 ) == 1 )
        check( "bits194 value==-2.5", SystemPtrReadFloat64( p, 24 ) == -2.5 )

        SystemPtrFree( p )
        SystemPtrFree( p2 )
        lib.release()
    }

    # ── 5. Float8 / Float16 roundtrip（只取精确可表示值，规避两侧舍入差异）──
    static testFloat8Roundtrip()
    {
        Console.println( "===== FFITest.testFloat8Roundtrip =====" )
        var lib = FFI.Library( s_dllPath )
        Int64 rtE4 = lib.getSymbol( "sl_float8_e4m3_roundtrip" )
        Int64 rtE5 = lib.getSymbol( "sl_float8_e5m2_roundtrip" )
        Int64 rtF16 = lib.getSymbol( "sl_float16_roundtrip" )

        # E4M3 往返：验证 VM 侧 f8 参数装载与 f8 返回压槽
        Float8 r1 = SystemFFICallF8E4M3( rtE4, "f8e4m3->f8e4m3", 1.5fe4 )
        check( "e4m3 roundtrip 1.5", r1 == 1.5fe4 )
        Float8 r2 = SystemFFICallF8E4M3( rtE4, "f8e4m3->f8e4m3", -2.5fe4 )
        check( "e4m3 roundtrip -2.5", r2 == -2.5fe4 )

        # E5M2 往返（返回声明 Float8/E4M3，实际槽 kind 为 E5M2，转换恒等保留位模式）
        Float8_E5M2 r3 = SystemFFICallF8E5M2( rtE5, "f8e5m2->f8e5m2", 2.0fe5 )
        check( "e5m2 roundtrip 2.0", r3 == 2.0fe5 )
        Float8_E5M2 r4 = SystemFFICallF8E5M2( rtE5, "f8e5m2->f8e5m2", 0.25fe5 )
        check( "e5m2 roundtrip 0.25", r4 == 0.25fe5 )

        # f16 往返（f32->f32，C 侧 RNE 量化）：1.5f 精确；0.3f -> 最近的 half 再回 f32
        Float32 f1 = SystemFFICallF32( rtF16, "f32->f32", 1.5f )
        check( "f16 roundtrip 1.5f exact", f1 == 1.5f )
        Float32 f2 = SystemFFICallF32( rtF16, "f32->f32", 0.3f )
        Float32 expect16 = 0.300048828125f
        check( "f16 roundtrip 0.3f -> 0.300048828125", f2 == expect16 )
        lib.release()
    }

    # ── 6. 混合标量打包进 struct + 指针读回/改写 ──
    # SLMixStruct 布局（自然对齐，sizeof=32）：
    #   offset 0: int32 / 8: int64 / 16: float32 / 24: float64
    static testMixStruct()
    {
        Console.println( "===== FFITest.testMixStruct =====" )
        var lib = FFI.Library( s_dllPath )
        Int64 mixToStruct = lib.getSymbol( "sl_mix_to_struct" )
        Int64 structSum = lib.getSymbol( "sl_struct_sum" )
        Int64 structAddI64 = lib.getSymbol( "sl_struct_add_i64" )
        Int64 p = SystemPtrAlloc( 64 )
        Int64 big = 9000000000

        # 四路标量打包进 struct
        SystemFFICallVoid( mixToStruct, "i32,i64,f32,f64,ptr->void", -100, big, 2.5f, 0.125, p )
        check( "mix i32v==-100", SystemPtrReadInt32( p, 0 ) == -100 )
        check( "mix i64v==9000000000", SystemPtrReadInt64( p, 8 ) == big )
        # f32 字段 2.5f 无 ReadFloat32，按 Int32 位模式读：0x40200000 = 1075838976
        check( "mix f32v bits(2.5f)", SystemPtrReadInt32( p, 16 ) == 1075838976 )
        check( "mix f64v==0.125", SystemPtrReadFloat64( p, 24 ) == 0.125 )

        # native 读 struct 求和：-100 + 9000000000 + 2.5 + 0.125
        Float64 sum = SystemFFICallF64( structSum, "ptr->f64", p )
        check( "struct_sum == 8999999902.625", sum == 8999999902.625d )

        # native 原地累加 i64v 后返回
        Int64 added = SystemFFICallI64( structAddI64, "ptr,i64->i64", p, 5 )
        check( "struct_add_i64 == 9000000005", added == 9000000005 )
        Float64 sum2 = SystemFFICallF64( structSum, "ptr->f64", p )
        check( "struct_sum after add == 8999999907.625", sum2 == 8999999907.625d )

        # SL 侧改写 struct 字段后 native 再读：7 + 3 + 2.5 + 4.0
        SystemPtrWriteInt32( p, 0, 7 )
        SystemPtrWriteInt64( p, 8, 3 )
        SystemPtrWriteFloat64( p, 24, 4.0 )
        Float64 sum3 = SystemFFICallF64( structSum, "ptr->f64", p )
        check( "struct_sum after SL write == 16.5", sum3 == 16.5 )

        SystemPtrFree( p )
        lib.release()
    }

    # ── 7. 函数指针：native 返回函数指针 / ptr 形参调用 ──
    static testFunctionPointer()
    {
        Console.println( "===== FFITest.testFunctionPointer =====" )
        var lib = FFI.Library( s_dllPath )
        Int64 getAdder = lib.getSymbol( "sl_get_adder" )
        Int64 getMultiplier = lib.getSymbol( "sl_get_multiplier" )
        Int64 callFnPtr = lib.getSymbol( "sl_call_fn_ptr" )

        # 取回 native 函数指针（"->ptr" 零实参变体）
        Int64 adder = SystemFFICallI64( getAdder, "->ptr" )
        check( "get_adder != 0", adder != 0 )
        check( "adder(19,23)==42", SystemFFICallI64( adder, "i64,i64->i64", 19, 23 ) == 42 )

        # 带参返回函数指针：mul10 / mul2
        Int64 mul10 = SystemFFICallI64( getMultiplier, "i64->ptr", 10 )
        Int64 mul2 = SystemFFICallI64( getMultiplier, "i64->ptr", 2 )
        check( "mul10(21)==210", SystemFFICallI64( mul10, "i64->i64", 21 ) == 210 )
        check( "mul2(21)==42", SystemFFICallI64( mul2, "i64->i64", 21 ) == 42 )

        # ptr 形参直接调用函数指针
        check( "call_fn_ptr(adder,7,35)==42", SystemFFICallI64( callFnPtr, "ptr,i64,i64->i64", adder, 7, 35 ) == 42 )
        lib.release()
    }

    # ── 8. 回调：SL 静态方法注册为 native 可调用 ──
    static testCallbacks()
    {
        Console.println( "===== FFITest.testCallbacks =====" )
        var lib = FFI.Library( s_dllPath )
        Int64 callCb = lib.getSymbol( "sl_call_with_callback" )
        Int64 reduceCb = lib.getSymbol( "sl_reduce_with_callback" )
        Int64 twoCb = lib.getSymbol( "sl_call_two_callbacks" )
        Int64 callFnPtr = lib.getSymbol( "sl_call_fn_ptr" )

        # 注册 SL 静态方法为 native 可调用回调
        Int64 cbAdd = FFI.StaticLibrary.createCallback( "SpecialTest.FFICallbacks.OnAdd_2_Core.Int64_Core.Int64", "i64,i64->i64" )
        Int64 cbMul = FFI.StaticLibrary.createCallback( "SpecialTest.FFICallbacks.OnMul_2_Core.Int64_Core.Int64", "i64,i64->i64" )
        check( "createCallback OnAdd", cbAdd != 0 )
        check( "createCallback OnMul", cbMul != 0 )

        # C 调 SL 回调：cb(x, x+1)
        check( "call_with_callback(OnAdd,20)==41", SystemFFICallI64( callCb, "ptr,i64->i64", cbAdd, 20 ) == 41 )
        check( "call_with_callback(OnMul,20)==420", SystemFFICallI64( callCb, "ptr,i64->i64", cbMul, 20 ) == 420 )

        # 嵌套回调（同一回调重入两次）：cb(cb(2,3),4)
        check( "reduce(OnAdd,2,3,4)==9", SystemFFICallI64( reduceCb, "ptr,i64,i64,i64->i64", cbAdd, 2, 3, 4 ) == 9 )
        check( "reduce(OnMul,2,3,4)==24", SystemFFICallI64( reduceCb, "ptr,i64,i64,i64->i64", cbMul, 2, 3, 4 ) == 24 )

        # 双回调各自执行后求和：13 + 42
        check( "two_callbacks(OnAdd,OnMul,6,7)==55", SystemFFICallI64( twoCb, "ptr,ptr,i64,i64->i64", cbAdd, cbMul, 6, 7 ) == 55 )

        # 回调地址作为普通函数指针传给 native
        check( "call_fn_ptr(OnAdd,30,12)==42", SystemFFICallI64( callFnPtr, "ptr,i64,i64->i64", cbAdd, 30, 12 ) == 42 )

        # 释放回调槽
        check( "freeCallback OnAdd", FFI.StaticLibrary.freeCallback( cbAdd ) )
        check( "freeCallback OnMul", FFI.StaticLibrary.freeCallback( cbMul ) )
        lib.release()
    }

    # ── 9. Utf8 字符串进出 ──
    static testUtf8()
    {
        Console.println( "===== FFITest.testUtf8 =====" )
        var lib = FFI.Library( s_dllPath )
        Int64 echoFn = lib.getSymbol( "sl_echo" )
        Int64 concatFn = lib.getSymbol( "sl_concat" )
        Int64 strlenFn = lib.getSymbol( "sl_strlen_utf8" )
        string echoed = SystemFFICallUtf8( echoFn, "utf8->utf8", "hello ffi" )
        check( "echo == 'echo:hello ffi'", echoed == "echo:hello ffi" )
        string joined = SystemFFICallUtf8( concatFn, "utf8,utf8->utf8", "foo", "bar" )
        check( "concat('foo','bar') == 'foobar'", joined == "foobar" )
        check( "strlen('abc123') == 6", SystemFFICallI64( strlenFn, "utf8->i64", "abc123" ) == 6 )
        lib.release()
    }

    # ── 10. sl_exports_json 导出清单（ffi-design.md 0.1 节约定）──
    static testExportsJson()
    {
        Console.println( "===== FFITest.testExportsJson =====" )
        var lib = FFI.Library( s_dllPath )
        Int64 exportsFn = lib.getSymbol( "sl_exports_json" )
        string json = SystemFFICallUtf8( exportsFn, "->utf8" )
        check( "sl_exports_json non-empty", json != null && json != "" )
        Console.println( "  exports json head: " + json )
        lib.release()
    }

    # ── 11. 函数值：Func<>/typealias 声明 + 编译期签名推导（Dart lookupFunction 风格）──
    # getFunction/getSymbol 只传 name 时, 编译期从左侧函数类型定义推导 FFI sig
    # 注入为第二实参; getSymbol 只返回裸地址, 赋给函数类型变量时改写为 getFunction
    static testFunctionValue()
    {
        Console.println( "===== FFITest.testFunctionValue =====" )
        var lib = FFI.Library( s_dllPath )

        # Func<int,int,int> = getFunction(name): 推导注入 "i32,i32->i32"
        Func<int,int,int> addf = lib.getFunction( "simplelanguage_addtest" )
        check( "Func<i,i,i> addf(20,22)==42", addf( 20, 22 ) == 42 )
        check( "Func<i,i,i> addf(-5,7)==2", addf( -5, 7 ) == 2 )

        # Func + getSymbol(name): 裸地址改写为 getFunction 并注入推导 sig
        Func<int,int,int> addg = lib.getSymbol( "simplelanguage_addtest" )
        check( "Func<i,i,i> addg(30,12)==42", addg( 30, 12 ) == 42 )

        # Int64 类型名形态: i64 native 函数
        Func<Int64,Int64,Int64> addl = lib.getFunction( "sl_add" )
        check( "Func<i64,i64,i64> addl(11,31)==42", addl( 11, 31 ) == 42 )

        # 显式 sig: 已传第二实参时不注入, 原样使用
        Func<Int64,Int64> mul2f = lib.getFunction( "sl_mul2", "i64->i64" )
        check( "explicit sig mul2f(21)==42", mul2f( 21 ) == 42 )

        # 函数类型 typealias: AddFuncTA = int Function( int, int )
        AddFuncTA addta = lib.getFunction( "simplelanguage_addtest" )
        check( "AddFuncTA addta(40,2)==42", addta( 40, 2 ) == 42 )

        # typealias + getSymbol 改写形态
        EchoFuncTA echota = lib.getSymbol( "sl_echo" )
        check( "EchoFuncTA echota('x')=='echo:x'", echota( "x" ) == "echo:x" )

        # utf8 参数/返回 Func 形态
        Func<string,string> echof = lib.getFunction( "sl_echo" )
        check( "Func<s,s> echof('abc')=='echo:abc'", echof( "abc" ) == "echo:abc" )
        Func<Int64,string> strlenf = lib.getFunction( "sl_strlen_utf8" )
        check( "Func<i64,s> strlenf('abc123')==6", strlenf( "abc123" ) == 6 )

        # 零参 sig: Func<string> -> "->utf8"
        Func<string> jsonf = lib.getFunction( "sl_exports_json" )
        string json = jsonf()
        check( "Func<s> jsonf() non-empty", json != null && json != "" )

        # f64 返回 + 全宽度参数（u8,i16,u32,i64,f32,f64 -> f64）
        Int64 u32v = 3000000000
        Int64 i64v = 5000000000
        Func<double,byte,short,uint,long,float,double> mixf = lib.getFunction( "sl_mix_all" )
        double r = mixf( 200, -1000, u32v, i64v, 2.5f, 1.25 )
        check( "Func f64 mixf == 28999998220.0", r == 28999998220.0d )

        lib.release()
    }

    # ── 12. Memory native（原 FFI.NativeMemory 迁入 Core/Memory）：动态构建 C 数组/标量/struct ──
    static testNativeMemory()
    {
        Console.println( "===== FFITest.testNativeMemory =====" )
        # 分配/释放 + 零清验证
        Int64 p = Memory.alloc( 64 )
        check( "alloc(64) != 0", p != 0 )
        check( "alloc zeroed", Memory.readInt64( p, 0 ) == 0 )
        # 类型尺寸
        check( "sizeOf(bool)==1", Memory.sizeOf( "bool" ) == 1 )
        check( "sizeOf(Int32)==4", Memory.sizeOf( "Int32" ) == 4 )
        check( "sizeOf(Int64)==8", Memory.sizeOf( "Int64" ) == 8 )
        check( "sizeOf(string)==8", Memory.sizeOf( "string" ) == 8 )
        # 标量写读 roundtrip
        Memory.writeI32( p, 0, -123 )
        check( "i32 roundtrip", Memory.readInt32( p, 0 ) == -123 )
        Memory.writeI64( p, 8, 9000000000 )
        check( "i64 roundtrip", Memory.readInt64( p, 8 ) == 9000000000 )
        Memory.writeF32( p, 16, 2.5f )
        check( "f32 roundtrip", Memory.readFloat32( p, 16 ) == 2.5f )
        Memory.writeF64( p, 24, 0.125 )
        check( "f64 roundtrip", Memory.readFloat64( p, 24 ) == 0.125 )
        Memory.writeBool( p, 32, 1 )
        check( "bool roundtrip", Memory.readBool( p, 32 ) )
        # 窄整型符号/零扩展
        Memory.writeI8( p, 36, -1 )
        check( "i8 sign-extend", Memory.readInt8( p, 36 ) == -1 )
        check( "u8 zero-extend", Memory.readUInt8( p, 36 ) == 255 )
        Memory.writeI16( p, 38, -2 )
        check( "i16 sign-extend", Memory.readInt16( p, 38 ) == -2 )
        check( "u16 zero-extend", Memory.readUInt16( p, 38 ) == 65534 )
        Memory.writeI32( p, 40, -1 )
        check( "u32 zero-extend to Int64", Memory.readUInt32( p, 40 ) == 4294967295 )
        # 数组：4 个 Int32 槽连排
        Int64 arr = Memory.allocArray( 4, "Int32" )
        check( "allocArray(4,Int32) != 0", arr != 0 )
        Memory.writeI32( arr, 0, 10 )
        Memory.writeI32( arr, 4, 20 )
        Memory.writeI32( arr, 8, 30 )
        Memory.writeI32( arr, 12, 40 )
        Int32 sum = Memory.readInt32( arr, 0 ) + Memory.readInt32( arr, 4 ) + Memory.readInt32( arr, 8 ) + Memory.readInt32( arr, 12 )
        check( "int32[4] sum == 100", sum == 100 )
        # 标量构建辅助
        Int64 ni = Memory.newInt32( 42 )
        check( "newInt32(42)", Memory.readInt32( ni, 0 ) == 42 )
        Int64 nd = Memory.newFloat64( 0.5 )
        check( "newFloat64(0.5)", Memory.readFloat64( nd, 0 ) == 0.5 )
        # utf8 槽：存指针 + 读回
        string s = "hello mem"
        Int64 us = Memory.newUtf8( s )
        check( "newUtf8/readUtf8 roundtrip", Memory.readUtf8( us, 0 ) == "hello mem" )
        # copyUtf8：字节拷贝（含 NUL）
        Int64 buf = Memory.alloc( 16 )
        Int32 copied = Memory.copyUtf8( buf, 0, "abc", 16 )
        check( "copyUtf8 returns 3", copied == 3 )
        check( "copyUtf8 bytes", Memory.readUInt8( buf, 0 ) == 97 && Memory.readUInt8( buf, 1 ) == 98 && Memory.readUInt8( buf, 2 ) == 99 && Memory.readUInt8( buf, 3 ) == 0 )
        # struct：布局 + 动态构建 + native 校验
        check( "structSize(i32,i64,f32,f64)==32", Memory.structSize( "i32,i64,f32,f64" ) == 32 )
        check( "fieldOffset[0]==0", Memory.structFieldOffset( "i32,i64,f32,f64", 0 ) == 0 )
        check( "fieldOffset[1]==8", Memory.structFieldOffset( "i32,i64,f32,f64", 1 ) == 8 )
        check( "fieldOffset[2]==16", Memory.structFieldOffset( "i32,i64,f32,f64", 2 ) == 16 )
        check( "fieldOffset[3]==24", Memory.structFieldOffset( "i32,i64,f32,f64", 3 ) == 24 )
        var lib = FFI.Library( s_dllPath )
        Int64 structSum = lib.getSymbol( "sl_struct_sum" )
        Int64 structAddI64 = lib.getSymbol( "sl_struct_add_i64" )
        Int64 st = Memory.newStruct( "i32,i64,f32,f64" )
        Memory.writeI32( st, 0, -100 )
        Memory.writeI64( st, 8, 9000000000 )
        Memory.writeF32( st, 16, 2.5f )
        Memory.writeF64( st, 24, 0.125 )
        Float64 total = SystemFFICallF64( structSum, "ptr->f64", st )
        check( "native struct_sum == 8999999902.625", total == 8999999902.625d )
        Int64 added = SystemFFICallI64( structAddI64, "ptr,i64->i64", st, 5 )
        check( "native struct_add_i64 == 9000000005", added == 9000000005 )
        # 释放（freeNative：避免与 Memory.free(object) 冲突的重命名）
        check( "free(p)", Memory.freeNative( p ) )
        check( "free(arr)", Memory.freeNative( arr ) )
        check( "free(ni)", Memory.freeNative( ni ) )
        check( "free(nd)", Memory.freeNative( nd ) )
        check( "free(us)", Memory.freeNative( us ) )
        check( "free(buf)", Memory.freeNative( buf ) )
        check( "free(st)", Memory.freeNative( st ) )
        check( "free(0) -> false", Memory.freeNative( 0 ) == false )
        lib.release()
    }

    # ── 12b. Memory data <-> Array<object> 互转（cvm 层数据 <-> C struct 数据格式转换）──
    # arrayToData(values, fields)：按 fields 类型串（i32,f64,string,object）
    # 把数组打包成 data 对象；dataToArray(data)：data 成员回读为值数组
    static testDataToArray()
    {
        Console.println( "===== FFITest.testDataToArray =====" )
        # arrayToData：Array<object> -> data（按 fields 布局）
        Array<object> values = Array<object>( 4 )
        values[0] = 11
        values[1] = 2.5
        values[2] = "mem"
        values[3] = null
        var d = Memory.arrayToData( values, "i32,f64,string,object" )
        check( "arrayToData != null", d != null )
        # dataToArray：data -> Array<object>，roundtrip 比对（object 元素 as 转型后比较）
        var back = Memory.dataToArray( d )
        check( "dataToArray != null", back != null )
        check( "roundtrip length == 4", back.length == 4 )
        check( "roundtrip [0]==11", ( back[0] as int ) == 11 )
        check( "roundtrip [1]==2.5", ( back[1] as double ) == 2.5 )
        check( "roundtrip [2]=='mem'", ( back[2] as string ) == "mem" )
        check( "roundtrip [3]==null", back[3] == null )
        # data -> native struct：dataToC 的设计目的——cvm 层数据转 C struct 内存
        #（fields 布局 "i32,i64,f32,f64" 同上 struct 校验：成员按 C 对齐打包）
        Array<object> sv = Array<object>( 4 )
        sv[0] = -100
        sv[1] = 9000000000
        sv[2] = 2.5f
        sv[3] = 0.125
        var sd = Memory.arrayToData( sv, "i32,i64,f32,f64" )
        check( "arrayToData struct != null", sd != null )
        var sb = Memory.dataToArray( sd )
        check( "struct roundtrip [0]==-100", ( sb[0] as int ) == -100 )
        check( "struct roundtrip [1]==9000000000", ( sb[1] as long ) == 9000000000 )
        check( "struct roundtrip [2]==2.5", ( sb[2] as float ) == 2.5f )
        check( "struct roundtrip [3]==0.125", ( sb[3] as double ) == 0.125 )
    }

    # ── 21. 具名 data DataName{} <-> C struct 互转 ──
    # dataToNativeStruct("C 结构体名", data)：data 实例 -> 新分配的 C struct
    #   内存地址（自然对齐；string/class 成员为指针槽，嵌套 data 内联展开）。
    #   structName 仅用于日志——布局完全由 SL 侧 data 定义驱动，两侧布局须一致。
    # nativeStructToData<类型名>( addr )：native 内存 -> 按类型名新建 data 实例
    #   （前端语法糖，等价于 nativeStructToData( addr, "类型名" )）。
    # 两边转换出的 native 内存均不受 SL 内存管理，由调用方负责释放。
    # FFIStructSample 布局（成员顺序，C 自然对齐）：
    #   id    Int32   @ 0  (4B)      score Float32 @ 4  (4B)
    #   name  char*   @ 8  (8B)      meta  嵌套 data @ 16（FFIMetaInfo:
    #                                level Int32 @0 / flag bool @4 -> size 8）
    #   kind  enum    @ 24 (4B)      hold  对象地址槽 @ 32 (8B)
    #   总计 40 字节（max align 8 尾部补齐）
    static testDataToStruct()
    {
        Console.println( "===== FFITest.testDataToStruct =====" )
        FFIStructSample s = FFIStructSample(){
            id = 77,
            score = 2.5,
            name = "sl",
            meta = FFIMetaInfo(){ level = 3, flag = true },
            kind = FFIKind.B,
            hold = FFIHoldClass(){ value = 222 }
        }
        # ── data -> native struct：native 偏移逐成员读回校验 ──
        Int64 addr = Memory.dataToNativeStruct( "FFIStructSample", s )
        check( "dataToNativeStruct != 0", addr != 0 )
        check( "id @0 == 77", Memory.readInt32( addr, 0 ) == 77 )
        check( "score @4 == 2.5", Memory.readFloat32( addr, 4 ) == 2.5f )
        check( "name @8 == 'sl'", Memory.readUtf8( addr, 8 ) == "sl" )
        check( "meta.level @16 == 3", Memory.readInt32( addr, 16 ) == 3 )
        check( "meta.flag @20 == true", Memory.readBool( addr, 20 ) == true )
        check( "kind @24 == 2 (FFIKind.B)", Memory.readInt32( addr, 24 ) == 2 )
        Int64 holdPtr = Memory.readPtr( addr, 32 )
        check( "hold @32 ptr != 0", holdPtr != 0 )

        # C 侧改写 enum 槽：2 -> 1（FFIKind.B -> A），回读应还原为新常量
        Memory.writeI32( addr, 24, 1 )

        # ── native struct -> data：<类型名> 语法糖回读，逐成员校验 ──
        var back = Memory.nativeStructToData<FFIStructSample>( addr )
        check( "nativeStructToData != null", back != null )
        FFIStructSample b = back as FFIStructSample
        check( "back as FFIStructSample != null", b != null )
        check( "back.id == 77", b.id == 77 )
        check( "back.score == 2.5", b.score == 2.5f )
        check( "back.name == 'sl'", b.name == "sl" )
        check( "back.meta.level == 3", b.meta.level == 3 )
        check( "back.meta.flag == true", b.meta.flag == true )
        check( "back.kind == FFIKind.A (改写后)", b.kind == FFIKind.A )
        check( "back.hold.value == 222 (引用槽还原)", b.hold.value == 222 )
        check( "back.hold == s.hold (同对象)", b.hold == s.hold )

        # ── 失败路径 ──
        FFIStructSample bad = FFIStructSample(){
            id = 1, score = 0.0, name = "y",
            meta = null, kind = FFIKind.A, hold = null
        }
        check( "dataToNativeStruct(嵌套 meta null) == 0",
            Memory.dataToNativeStruct( "FFIStructSample", bad ) == 0 )
        check( "nativeStructToData(0) == null",
            Memory.nativeStructToData<FFIStructSample>( 0 ) == null )
        check( "nativeStructToData(未知类型名) == null",
            Memory.nativeStructToData( addr, "NoSuchDataName" ) == null )

        Memory.freeNative( addr )
    }

    # ── 20. dllImports functions 段：global.<funcName>(...) 全局库函数变量直调 ──
    # project.jsonc dllImports 项的 "functions" 列表注入 Project 静态 Func 成员：
    #     { "path": "...", "alias": "CLangdll",
    #       "functions": [ { "name": "libaddfunc", "symbol": "simplelanguage_addtest",
    #                        "sig": "i32,i32->i32" } ] }
    # SL 侧 var res = global.libaddfunc( 1, 2 ) 直接调用（C# DllImport 语义）
    static testGlobalLibFunc()
    {
        Console.println( "===== FFITest.testGlobalLibFunc =====" )
        # global.libaddfunc：jsonc functions 段注入的库函数变量
        var res = global.libaddfunc( 1, 2 )
        check( "global.libaddfunc(1,2)==3", res == 3 )
        check( "global.libaddfunc(20,22)==42", global.libaddfunc( 20, 22 ) == 42 )
        check( "global.libaddfunc(-5,7)==2", global.libaddfunc( -5, 7 ) == 2 )
    }

    # ── 13. lookupFunction<Ret,P...>：模板实参注入 sig（内部改写为 getFunction）──
    static testLookupFunction()
    {
        Console.println( "===== FFITest.testLookupFunction =====" )
        var lib = FFI.Library( s_dllPath )
        # var 声明 + 模板 <int,int,int> -> sig "i32,i32->i32"
        var addf = lib.lookupFunction<int,int,int>( "simplelanguage_addtest" )
        check( "lookup<int,int,int> addf(20,22)==42", addf( 20, 22 ) == 42 )
        check( "lookup<int,int,int> addf(-5,7)==2", addf( -5, 7 ) == 2 )
        # 显式 Func 类型声明同样生效
        Func<int,int,int> addt = lib.lookupFunction<int,int,int>( "sl_add" )
        check( "lookup Func<int,int,int> addt(11,31)==42", addt( 11, 31 ) == 42 )
        # SL 类型名 <Int64,Int64,Int64> -> "i64,i64->i64"
        Func<long,long,long> addl = lib.lookupFunction<Int64,Int64,Int64>( "sl_add" )
        check( "lookup<Int64,Int64,Int64> addl(11,31)==42", addl( 11, 31 ) == 42 )
        # 单参 <Int64,string> -> "utf8->i64"
        Func<Int64,string> strlenf = lib.lookupFunction<Int64,string>( "sl_strlen_utf8" )
        check( "lookup<Int64,string> strlenf('abc123')==6", strlenf( "abc123" ) == 6 )
        # 零参 <string> -> "->utf8"
        Func<string> jsonf = lib.lookupFunction<string>( "sl_exports_json" )
        check( "lookup<string> jsonf() non-empty", jsonf() != null && jsonf() != "" )
        # 7 模板实参（ret + 6 参）全宽度混合 -> "u8,i16,u32,i64,f32,f64->f64"
        Int64 u32v = 3000000000
        Int64 i64v = 5000000000
        Func<double,byte,short,uint,long,float,double> mixf = lib.lookupFunction<double,byte,short,uint,long,float,double>( "sl_mix_all" )
        double r = mixf( 200, -1000, u32v, i64v, 2.5f, 1.25 )
        check( "lookup 7-args mixf == 28999998220.0", r == 28999998220.0d )
        lib.release()
    }

    # ── 14. @DllImport：attribute 声明式绑定（编译期合成 FFI.Library().getFunction()）──
    static testDllImport()
    {
        Console.println( "===== FFITest.testDllImport =====" )
        # 函数定义形式：类加载时静态初始化器（隐藏 __dll_ 字段）即完成绑定
        Console.println( "probe0: s_dllAdd(2,3) = " + s_dllAdd( 2, 3 ) )
        Console.println( "probe1: s_dllMul2(21) = " + s_dllMul2( 21 ) )
        Console.println( "probe2: s_dllStrlen('ab') = " + s_dllStrlen( "ab" ) )
        Console.println( "probe3: s_dllJson() non-empty -> " + (s_dllJson() != null && s_dllJson() != "") )
        Console.println( "probe4: s_dllMix(1,2,3,4,0.5f,0.25) = " + s_dllMix( 1, 2, 3, 4, 0.5f, 0.25 ) )
        int direct = s_dllAdd( 20, 22 )
        Console.println( "probe5: s_dllAdd(20,22) = " + direct )
        # sig 从签名 (int,int)->int 推导为 "i32,i32->i32"
        check( "dll add(20,22)==42", s_dllAdd( 20, 22 ) == 42 )
        check( "dll add(-5,7)==2", s_dllAdd( -5, 7 ) == 2 )
        # 手写 sig "i64->i64"
        check( "dll mul2(21)==42", s_dllMul2( 21 ) == 42 )
        check( "dll mul2(-7)==-14", s_dllMul2( -7 ) == -14 )
        # utf8->i64
        check( "dll strlen('abc123')==6", s_dllStrlen( "abc123" ) == 6 )
        # ->utf8
        check( "dll json() non-empty", s_dllJson() != null && s_dllJson() != "" )
        # 6 参全宽度混合（推导 sig）
        # u32v 用 uint 匹配第 3 参 u32（变量不做窄化，C# 同语义）
        uint u32v = 3000000000ui
        Int64 i64v = 5000000000
        double r = s_dllMix( 200, -1000, u32v, i64v, 2.5f, 1.25 )
        check( "dll mix == 28999998220.0", r == 28999998220.0d )
    }

    # ── 15. dllImports 配置：project.jsonc 别名 + global.dllImport.<alias> 注入访问 ──
    static testDllImportConfig()
    {
        Console.println( "===== FFITest.testDllImportConfig =====" )
        # global.dllImport.<alias>：jsonc 配置注入的库对象（免写长路径）
        var lib2 = global.dllImport.CLangdll
        check( "global.dllImport.CLangdll != null", lib2 != null )
        check( "global.dllImport.CLangdll.isValid", lib2.isValid )
        # 链式取函数（与手写 FFI.Library(路径) 等价，同路径共享句柄）
        var addc = lib2.lookupFunction<int,int,int>( "simplelanguage_addtest" )
        check( "config lib lookup add(20,22)==42", addc( 20, 22 ) == 42 )
        Func<Int64,string> strlenc = lib2.lookupFunction<Int64,string>( "sl_strlen_utf8" )
        check( "config lib strlen('abc123')==6", strlenc( "abc123" ) == 6 )
        # @DllImport 别名形态：第 1 实参用配置别名（编译期查表替换为路径）
        check( "alias dll mul2(21)==42", s_dllMul2Alias( 21 ) == 42 )
        check( "alias dll mul2(-7)==-14", s_dllMul2Alias( -7 ) == -14 )
        check( "alias dll json() non-empty", s_dllJsonAlias() != null && s_dllJsonAlias() != "" )
    }

    # ── 16. @DllImport C# 风格函数声明：编译期合成静态函数 wrapper 直调 ──
    static testDllImportCsStyle()
    {
        Console.println( "===== FFITest.testDllImportCsStyle =====" )
        # 直接按普通静态函数调用（wrapper 转发隐藏 Func 字段）
        check( "cs addCs(11,31)==42", addCs( 11, 31 ) == 42 )
        check( "cs addCs(-5,7)==2", addCs( -5, 7 ) == 2 )
        check( "cs addtestCs(20,22)==42", addtestCs( 20, 22 ) == 42 )
        # 手写 sig
        check( "cs mul2Cs(21)==42", mul2Cs( 21 ) == 42 )
        check( "cs mul2Cs(-7)==-14", mul2Cs( -7 ) == -14 )
        # utf8 参数
        check( "cs strlenCs('abc123')==6", strlenCs( "abc123" ) == 6 )
        # 零参调用
        check( "cs exportsJsonCs() non-empty", exportsJsonCs() != null && exportsJsonCs() != "" )
        # void 返回 + ptr 参数（手写 sig "u8,ptr->void"，位模式 60 -> E4M3 1.5）
        Int64 p = SystemPtrAlloc( 64 )
        bitsToStructCs( 60, p )
        check( "cs bitsToStructCs bits==60", SystemPtrReadInt32( p, 4 ) == 60 )
        check( "cs bitsToStructCs value==1.5", SystemPtrReadFloat64( p, 24 ) == 1.5 )
        SystemPtrFree( p )
        # 6 参全宽度混合（推导 sig）
        # u32v 用 uint 类型匹配 sl_mix_all 第 3 参 u32（C# 同样不允许 long 变量隐式转 uint）
        uint u32v = 3000000000ui
        Int64 i64v = 5000000000
        double r = mixCs( 200, -1000, u32v, i64v, 2.5f, 1.25 )
        check( "cs mixCs == 28999998220.0", r == 28999998220.0d )
    }

    # ── 17. C# 风格 + 独立别名（project.jsonc dllImports 别名 != 库名）──
    static testDllImportCsAlias()
    {
        Console.println( "===== FFITest.testDllImportCsAlias =====" )
        # 别名 "cl" 查表命中配置路径（与库名 CLangdll 不同的别名）
        check( "cs alias addCsAlias(11,31)==42", addCsAlias( 11, 31 ) == 42 )
        check( "cs alias addCsAlias(-5,7)==2", addCsAlias( -5, 7 ) == 2 )
        # 全后缀字面量直传（ui/L/f + 无后缀小数默认 f32 拓宽到 f64）
        check( "cs alias mixCsAlias == 28999998220.0",
            mixCsAlias( 200, -1000, 3000000000ui, 5000000000L, 2.5f, 1.25 ) == 28999998220.0d )
    }

    # ── 18. 调用点常量隐式窄化（C# 隐式常量转换语义，含边界值）──
    static testCallConstNarrowing()
    {
        Console.println( "===== FFITest.testCallConstNarrowing =====" )
        # byte/short 形参接收 int 字面量：值域检查后窄化（含一元负折叠 -32768）
        # 255*1 + (-32768)*2 = -65281
        check( "narrow byte 255 + short -32768 == -65281.0",
            mixCs( 255, -32768, 0ui, 0L, 0.0f, 0.0 ) == -65281.0d )
        # 0*1 + 32767*2 = 65534
        check( "narrow byte 0 + short 32767 == 65534.0",
            mixCs( 0, 32767, 0ui, 0L, 0.0f, 0.0 ) == 65534.0d )
        # 1*1 + (-1)*2 = -1
        check( "narrow byte 1 + short -1 == -1.0",
            mixCs( 1, -1, 0ui, 0L, 0.0f, 0.0 ) == -1.0d )
    }

    # ── 19. @DllImport fallback：dll 绑定不可用时执行函数本体 ──
    static testDllImportFallback()
    {
        Console.println( "===== FFITest.testDllImportFallback =====" )
        # s_fbAdd 绑定的库不存在：类加载时 bindFunction 返回 null
        #（隐藏 __dll_s_fbAdd 字段为 null）-> wrapper 走 else 分支执行
        # fallback 本体（SL 实现的 a+b），与 dll 可用时的语义一致
        check( "fallback fbAdd(20,22)==42", s_fbAdd( 20, 22 ) == 42 )
        check( "fallback fbAdd(-5,7)==2", s_fbAdd( -5, 7 ) == 2 )
        check( "fallback fbAdd(0,0)==0", s_fbAdd( 0, 0 ) == 0 )
    }

    # ── main entry ──
    static fun()
    {
        testLibraryLifecycle()
        testBasicCall()
        testScalarMix()
        testFloat8Struct()
        testFloat8Roundtrip()
        testMixStruct()
        testFunctionPointer()
        testCallbacks()
        testUtf8()
        testExportsJson()
        testFunctionValue()
        testNativeMemory()
        testDataToArray()
        testDataToStruct()
        testLookupFunction()
        testDllImport()
        testDllImportConfig()
        testGlobalLibFunc()
        testDllImportCsStyle()
        testDllImportCsAlias()
        testCallConstNarrowing()
        testDllImportFallback()
        Console.println( "===== all FFI tests done =====" )
    }
}
