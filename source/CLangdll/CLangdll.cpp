// CLangdll.cpp - SimpleLanguage C VM FFI 互操作测试库（第三方库样例）。
//
// 本 DLL 由 test/SpecialTest/FFITest.sl 通过 SystemFFI* 系列系统方法加载并调用，
// 覆盖以下特殊场景：
//   1. 标量多宽度混合（u8/i16/u32/i64/f32/f64 同一函数）
//   2. Float8（E4M3/E5M2）data 数据 → struct 位域分解
//   3. 混合标量 → struct 打包 + struct 指针读回
//   4. 函数指针：返回函数指针（get_adder / get_multiplier）
//   5. 函数指针：回调（C 调用 SL 侧注册的 trampoline）
//   6. Utf8 字符串进出
//   7. sl_exports_json 导出清单（ffi-design.md 0.1 节约定）
//
// 调用约定说明（与 csimple_lang/src/lib/ffi/sl_ffi_call.c 对齐）：
//   INT 类实参统一 int64 经 GR 传递 → C 侧窄整型形参只读寄存器低位即可；
//   DBL 类实参统一 double 经 XMM 传递 → C 侧 float 形参读 XMM 低 32 位位模式，
//   double 形参读全 64 位；Float8/Float16 实参由 VM 侧换算为等值 float 传递。
//   因此本文件形参声明与自然 C 写法完全一致，无需任何特殊标注。

#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>

// ---------------------------------------------------------------------------
// 0. 基础用例（保留原有入口）
// ---------------------------------------------------------------------------

extern "C" __declspec(dllexport)
int simplelanguage_addtest(int a, int b)
{
    return a + b;
}

// ---------------------------------------------------------------------------
// 1. 标量多宽度混合
//    FFI 签名: "u8,i16,u32,i64,f32,f64->f64"
// ---------------------------------------------------------------------------

extern "C" __declspec(dllexport)
double sl_mix_all(unsigned char a, short b, unsigned int c, long long d, float e, double f)
{
    return (double)a * 1.0
         + (double)b * 2.0
         + (double)c * 3.0
         + (double)d * 4.0
         + (double)e * 5.0
         +          f * 6.0;
}

// ---------------------------------------------------------------------------
// 2. Float8 data 数据 → struct
//    SLFloat8Parts 布局（自然对齐，sizeof = 32）：
//      offset  0: int      format    （0 = E4M3, 1 = E5M2）
//      offset  4: int      bits      （float8 原始 1 字节位模式）
//      offset  8: int      sign      （符号位）
//      offset 12: int      exponent  （指数位域原始值）
//      offset 16: int      mantissa  （尾数位域原始值）
//      offset 24: double   value     （按对应格式解码后的数值）
//    SL 侧用 SystemPtrReadInt32/ReadFloat64 按偏移读取。
// ---------------------------------------------------------------------------

typedef struct SLFloat8Parts
{
    int      format;
    int      bits;
    int      sign;
    int      exponent;
    int      mantissa;
    double   value;
} SLFloat8Parts;

/* 位模式构造 NaN（避免编译期 0.0f/0.0f 常量除零告警） */
static float sl_nan_value(void)
{
    uint32_t bits = 0x7FC00000u;
    float    f;
    memcpy(&f, &bits, sizeof(f));
    return f;
}

/* E4M3（1 符号 + 4 指数 bias 7 + 3 尾数）解码，无 Inf，0x7F 为 NaN */
static float e4m3_bits_to_float(uint8_t b)
{
    int      sign     = (b >> 7) & 0x1;
    int      exponent = (b >> 3) & 0xF;
    int      mantissa =  b       & 0x7;
    float    v;

    if (exponent == 0)
    {
        v = (float)mantissa * (1.0f / 8.0f) * (1.0f / 64.0f);  /* 2^-6 * m/8 */
    }
    else if (exponent == 0xF && mantissa == 0x7)
    {
        return sl_nan_value();                                    /* NaN */
    }
    else
    {
        v = (1.0f + (float)mantissa * (1.0f / 8.0f))
          * (float)(1u << exponent)
          * (1.0f / 128.0f);                                      /* 2^(e-7) */
    }
    return sign ? -v : v;
}

/* E5M2（1 符号 + 5 指数 bias 15 + 2 尾数）解码，含 Inf/NaN */
static float e5m2_bits_to_float(uint8_t b)
{
    int      sign     = (b >> 7) & 0x1;
    int      exponent = (b >> 2) & 0x1F;
    int      mantissa =  b       & 0x3;
    float    v;

    if (exponent == 0)
    {
        v = (float)mantissa * (1.0f / 4.0f) * (1.0f / 16384.0f); /* 2^-14 * m/4 */
    }
    else if (exponent == 0x1F)
    {
        return sl_nan_value();                                    /* Inf/NaN 统一按 NaN 处理 */
    }
    else
    {
        v = (1.0f + (float)mantissa * (1.0f / 4.0f))
          * (float)(1u << (exponent - 1))
          * (1.0f / 16384.0f);                                   /* 2^(e-15) */
    }
    return sign ? -v : v;
}

/* 暴力最近邻编码：遍历 256 个位模式取最近值（测试库用，语义清晰且无舍入 bug） */
static uint8_t float_to_e4m3_bits(float v)
{
    uint8_t best = 0;
    float   bestd;
    int     i;

    if (v != v)
    {
        return 0x7F;                                             /* NaN */
    }
    bestd = e4m3_bits_to_float(0) - v;
    if (bestd < 0.0f) bestd = -bestd;
    for (i = 1; i < 256; ++i)
    {
        float d = e4m3_bits_to_float((uint8_t)i) - v;
        if (d < 0.0f) d = -d;
        if (d < bestd)
        {
            bestd = d;
            best  = (uint8_t)i;
        }
    }
    return best;
}

static uint8_t float_to_e5m2_bits(float v)
{
    uint8_t best = 0;
    float   bestd;
    int     i;

    if (v != v)
    {
        return 0x7F;                                             /* NaN */
    }
    bestd = e5m2_bits_to_float(0) - v;
    if (bestd < 0.0f) bestd = -bestd;
    for (i = 1; i < 256; ++i)
    {
        float d = e5m2_bits_to_float((uint8_t)i) - v;
        if (d < 0.0f) d = -d;
        if (d < bestd)
        {
            bestd = d;
            best  = (uint8_t)i;
        }
    }
    return best;
}

static void float8_decode_into(uint8_t b, int is_e5m2, SLFloat8Parts* out)
{
    out->format   = is_e5m2 ? 1 : 0;
    out->bits     = (int)b;
    out->sign     = (b >> 7) & 0x1;
    if (is_e5m2)
    {
        out->exponent = (b >> 2) & 0x1F;
        out->mantissa =  b       & 0x3;
        out->value    = (double)e5m2_bits_to_float(b);
    }
    else
    {
        out->exponent = (b >> 3) & 0xF;
        out->mantissa =  b       & 0x7;
        out->value    = (double)e4m3_bits_to_float(b);
    }
}

/* Float8（E4M3）值 → struct：FFI 签名 "f8e4m3,ptr->void"。
   SL 侧传 Float8 值（VM 换算为等值 float 进 XMM），C 侧量化回位模式再分解。 */
extern "C" __declspec(dllexport)
void sl_float8_e4m3_to_struct(float value, SLFloat8Parts* out)
{
    uint8_t b = float_to_e4m3_bits(value);
    if (out == NULL) return;
    float8_decode_into(b, 0, out);
}

/* Float8（E5M2）值 → struct：FFI 签名 "f8e5m2,ptr->void" */
extern "C" __declspec(dllexport)
void sl_float8_e5m2_to_struct(float value, SLFloat8Parts* out)
{
    uint8_t b = float_to_e5m2_bits(value);
    if (out == NULL) return;
    float8_decode_into(b, 1, out);
}

/* Float8 原始 1 字节 data（u8）→ struct：FFI 签名 "u8,ptr->void"。
   SL 侧直接传位模式数据（例如把 0x3C 当 u8 传），C 侧按 E4M3 解码。 */
extern "C" __declspec(dllexport)
void sl_float8_bits_to_struct(unsigned char bits, SLFloat8Parts* out)
{
    if (out == NULL) return;
    float8_decode_into(bits, 0, out);
}

/* Float8 双格式对照：同一值分别按 E4M3 / E5M2 编码，写入两个 struct。
   FFI 签名 "f32,ptr,ptr->void"，用于验证同值在不同浮点格式下的位域差异。 */
extern "C" __declspec(dllexport)
void sl_float8_dual_to_struct(float value, SLFloat8Parts* out_e4m3, SLFloat8Parts* out_e5m2)
{
    if (out_e4m3 != NULL)
    {
        float8_decode_into(float_to_e4m3_bits(value), 0, out_e4m3);
    }
    if (out_e5m2 != NULL)
    {
        float8_decode_into(float_to_e5m2_bits(value), 1, out_e5m2);
    }
}

/* struct → Float8（E4M3）位模式：反向组合。
   FFI 签名 "ptr->u8"：读 struct 的 sign/exponent/mantissa 位域并重组 1 字节。 */
extern "C" __declspec(dllexport)
unsigned char sl_struct_to_float8_bits(const SLFloat8Parts* s)
{
    if (s == NULL) return 0;
    return (unsigned char)(((s->sign & 0x1) << 7)
                         | ((s->exponent & 0xF) << 3)
                         |  (s->mantissa & 0x7));
}

/* Float8 值进、Float8 值出（E4M3 往返）：FFI 签名 "f8e4m3->f8e4m3"，
   用于验证 VM 侧 f8 参数装载与 f8 返回压槽。 */
extern "C" __declspec(dllexport)
float sl_float8_e4m3_roundtrip(float value)
{
    uint8_t b = float_to_e4m3_bits(value);
    return e4m3_bits_to_float(b);
}

/* Float8 值进、Float8 值出（E5M2 往返）：FFI 签名 "f8e5m2->f8e5m2"，
   用于验证 VM 侧 E5M2 参数装载与 E5M2 返回压槽。 */
extern "C" __declspec(dllexport)
float sl_float8_e5m2_roundtrip(float value)
{
    uint8_t b = float_to_e5m2_bits(value);
    return e5m2_bits_to_float(b);
}

/* Float16 值进、Float16 值出：FFI 签名 "f16->f16" */
extern "C" __declspec(dllexport)
float sl_float16_roundtrip(float value)
{
    /* float → half → float（简单 RNE 量化） */
    uint16_t h;
    uint32_t f;
    memcpy(&f, &value, sizeof(f));
    uint16_t sign = (uint16_t)((f >> 16) & 0x8000u);
    int32_t  exp  = (int32_t)((f >> 23) & 0xFFu) - 127 + 15;
    uint32_t man  = f & 0x7FFFFFu;

    if (((f >> 23) & 0xFFu) == 0xFF)
    {
        h = (uint16_t)(sign | 0x7C00u | (man ? 0x200u : 0u));   /* Inf/NaN */
    }
    else if (exp >= 0x1F)
    {
        h = (uint16_t)(sign | 0x7C00u);                          /* 上溢 → Inf */
    }
    else if (exp <= 0)
    {
        /* 次正规/零：补隐含位后右移到 10 位次正规尾数 m10 = m23 >> (14 - exp) */
        if (exp < -10)
        {
            h = sign;                                            /* 下溢 → 零 */
        }
        else
        {
            man |= 0x800000u;
            h = (uint16_t)(sign | (man >> (14 - exp)));
        }
    }
    else
    {
        /* 正常数：RNE 舍入到 10 位尾数 */
        uint32_t m10 = man >> 13;
        uint32_t rem = man & 0x1FFFu;
        if (rem > 0x1000u || (rem == 0x1000u && (m10 & 1u)))
        {
            ++m10;
            if (m10 == 0x400u) { m10 = 0u; ++exp; }
        }
        h = (uint16_t)(sign | ((uint32_t)exp << 10) | m10);
    }

    /* half → float 还原 */
    {
        uint32_t hs = (uint32_t)(h >> 15) & 1u;
        uint32_t he = (uint32_t)(h >> 10) & 0x1Fu;
        uint32_t hm = (uint32_t)(h       & 0x3FFu);
        uint32_t out;
        if (he == 0)
        {
            out = (hs << 31) | (hm << 13);                       /* 次正规/零 */
        }
        else if (he == 0x1F)
        {
            out = (hs << 31) | 0x7F800000u | (hm << 13);         /* Inf/NaN */
        }
        else
        {
            out = (hs << 31) | ((he - 15 + 127) << 23) | (hm << 13);
        }
        float r;
        memcpy(&r, &out, sizeof(r));
        return r;
    }
}

// ---------------------------------------------------------------------------
// 3. 混合标量 → struct 打包 + struct 指针读回
//    SLMixStruct 布局（自然对齐，sizeof = 32）：
//      offset  0: int32_t  i32v
//      offset  8: int64_t  i64v
//      offset 16: float    f32v
//      offset 24: double   f64v
// ---------------------------------------------------------------------------

typedef struct SLMixStruct
{
    int32_t  i32v;
    int64_t  i64v;
    float    f32v;
    double   f64v;
} SLMixStruct;

/* FFI 签名 "i32,i64,f32,f64,ptr->void"：四路标量打包进 struct */
extern "C" __declspec(dllexport)
void sl_mix_to_struct(int i32v, long long i64v, float f32v, double f64v, SLMixStruct* out)
{
    if (out == NULL) return;
    out->i32v = i32v;
    out->i64v = i64v;
    out->f32v = f32v;
    out->f64v = f64v;
}

/* FFI 签名 "ptr->f64"：struct 四字段求和 */
extern "C" __declspec(dllexport)
double sl_struct_sum(const SLMixStruct* s)
{
    if (s == NULL) return 0.0;
    return (double)s->i32v + (double)s->i64v + (double)s->f32v + s->f64v;
}

/* FFI 签名 "ptr,i64->i64"：struct.i64v 累加后返回（原地修改） */
extern "C" __declspec(dllexport)
long long sl_struct_add_i64(SLMixStruct* s, long long delta)
{
    if (s == NULL) return 0;
    s->i64v += delta;
    return s->i64v;
}

// ---------------------------------------------------------------------------
// 4. 函数指针：返回函数指针
// ---------------------------------------------------------------------------

/* 加法目标（get_adder 返回它的地址） */
extern "C" __declspec(dllexport)
long long sl_add(long long a, long long b)
{
    return a + b;
}

/* 2 倍乘目标 */
extern "C" __declspec(dllexport)
long long sl_mul2(long long x)
{
    return x * 2;
}

/* 10 倍乘目标 */
extern "C" __declspec(dllexport)
long long sl_mul10(long long x)
{
    return x * 10;
}

/* FFI 签名 "->ptr"：返回 sl_add 的函数指针。
   SL 侧拿到的 Int64 句柄可直接再通过 Call 系列调用。 */
extern "C" __declspec(dllexport)
void* sl_get_adder(void)
{
    return (void*)&sl_add;
}

/* FFI 签名 "i64->ptr"：按倍数返回乘法函数指针（工厂） */
extern "C" __declspec(dllexport)
void* sl_get_multiplier(long long k)
{
    if (k == 10)
    {
        return (void*)&sl_mul10;
    }
    return (void*)&sl_mul2;
}

/* FFI 签名 "ptr,i64,i64->i64"：C 侧直接调用传入的函数指针
   （该指针可以来自 get_adder，也可来自 SL 侧 CreateCallback 的 trampoline）。 */
extern "C" __declspec(dllexport)
long long sl_call_fn_ptr(long long (*fn)(long long, long long), long long a, long long b)
{
    if (fn == NULL) return -1;
    return fn(a, b);
}

// ---------------------------------------------------------------------------
// 5. 函数指针：回调（C 调用 SL 注册的 trampoline）
//    trampoline 静态形状为 int64(int64,int64)，故回调形参全用 long long。
// ---------------------------------------------------------------------------

/* FFI 签名 "ptr,i64->i64"：回调一次 */
extern "C" __declspec(dllexport)
long long sl_call_with_callback(long long (*cb)(long long, long long), long long x)
{
    if (cb == NULL) return -1;
    return cb(x, x + 1);
}

/* FFI 签名 "ptr,i64,i64,i64->i64"：回调嵌套两次（验证重入） */
extern "C" __declspec(dllexport)
long long sl_reduce_with_callback(long long (*cb)(long long, long long),
                                  long long a, long long b, long long c)
{
    if (cb == NULL) return -1;
    return cb(cb(a, b), c);
}

/* FFI 签名 "ptr,ptr,i64,i64,i64->i64"：双回调各自执行后求和 */
extern "C" __declspec(dllexport)
long long sl_call_two_callbacks(long long (*cb1)(long long, long long),
                                long long (*cb2)(long long, long long),
                                long long a, long long b)
{
    if (cb1 == NULL || cb2 == NULL) return -1;
    return cb1(a, b) + cb2(a, b);
}

// ---------------------------------------------------------------------------
// 6. Utf8 字符串进出
// ---------------------------------------------------------------------------

/* FFI 签名 "utf8->utf8"：回显（静态缓冲区，单线程测试用） */
extern "C" __declspec(dllexport)
const char* sl_echo(const char* s)
{
    static char buf[256];
    if (s == NULL)
    {
        return "<null>";
    }
    snprintf(buf, sizeof(buf), "echo:%s", s);
    return buf;
}

/* FFI 签名 "utf8,utf8->utf8"：拼接（静态缓冲区，单线程测试用） */
extern "C" __declspec(dllexport)
const char* sl_concat(const char* a, const char* b)
{
    static char buf[256];
    if (a == NULL) a = "";
    if (b == NULL) b = "";
    snprintf(buf, sizeof(buf), "%s%s", a, b);
    return buf;
}

/* FFI 签名 "utf8->i64"：strlen */
extern "C" __declspec(dllexport)
long long sl_strlen_utf8(const char* s)
{
    return s ? (long long)strlen(s) : 0;
}

// ---------------------------------------------------------------------------
// 8. 端到端全链路：native 分配 -> cvm 读出/改值 -> 写回 -> native 打印
//    SLBookStruct 布局（自然对齐，x64 sizeof = 24）：
//      offset  0: int32_t  id
//      offset  4: float    price
//      offset  8: int32_t  pages
//      offset 12: (padding)
//      offset 16: char*    title   （SL 侧 string 成员 = char* 指针槽）
//    SL 侧对应 data FFICBook{ id, price, pages, title }（成员顺序须一致）。
// ---------------------------------------------------------------------------

typedef struct SLBookStruct
{
    int32_t  id;
    float    price;
    int32_t  pages;
    char*    title;
} SLBookStruct;

/* CRT strdup 自实现（避免 MSVC POSIX 告警），title 由 C 侧自持 */
static char* sl_strdup(const char* s)
{
    size_t n = (s != NULL) ? strlen(s) : 0;
    char*  p = (char*)malloc(n + 1);
    if (p != NULL)
    {
        if (n > 0) memcpy(p, s, n);
        p[n] = '\0';
    }
    return p;
}

/* FFI 签名 "i32,f32,i32,utf8->ptr"：native 侧 malloc 分配并填充，
   返回 struct 指针（SL 侧以 Int64 接住，可用
   Memory.nativeStructToData<FFICBook>(addr) 读回为 data 实例）。 */
extern "C" __declspec(dllexport)
void* sl_book_alloc(int id, float price, int pages, const char* title)
{
    SLBookStruct* s = (SLBookStruct*)malloc(sizeof(SLBookStruct));
    if (s == NULL) return NULL;
    s->id    = id;
    s->price = price;
    s->pages = pages;
    s->title = sl_strdup(title);
    return s;
}

/* FFI 签名 "ptr->i64"：打印 struct 全部字段并返回校验和
   id + pages + (int)price（NULL 指针返回 -1）。 */
extern "C" __declspec(dllexport)
long long sl_book_print(const SLBookStruct* s)
{
    if (s == NULL)
    {
        printf("[CLangdll] sl_book_print(NULL)\n");
        return -1;
    }
    printf("[CLangdll] SLBook id=%d price=%.2f pages=%d title='%s'\n",
           (int)s->id, (double)s->price, (int)s->pages,
           (s->title != NULL) ? s->title : "(null)");
    return (long long)s->id + (long long)s->pages + (long long)(int)s->price;
}

/* FFI 签名 "ptr->i64"：释放 sl_book_alloc 分配的块（title + struct）。
   仅用于释放 C 侧 malloc 的指针；dataToNativeStruct 产生的块由
   Memory.freeNative 释放，不可混用。 */
extern "C" __declspec(dllexport)
long long sl_book_free(SLBookStruct* s)
{
    if (s == NULL) return 0;
    free(s->title);
    free(s);
    return 1;
}

// ---------------------------------------------------------------------------
// 7. 导出清单（ffi-design.md 0.1：优先调用原生库导出的 sl_exports_json()）
// ---------------------------------------------------------------------------

extern "C" __declspec(dllexport)
const char* sl_exports_json(void)
{
    return "{"
        "\"baseNamespace\": \"Native\","
        "\"functionList\": ["
        "{\"publicName\":\"Native.add\",\"entryPoint\":\"simplelanguage_addtest\",\"callingConvention\":\"Cdecl\",\"returnType\":\"I32\",\"parameterTypeList\":[\"I32\",\"I32\"]},"
        "{\"publicName\":\"Native.mixAll\",\"entryPoint\":\"sl_mix_all\",\"callingConvention\":\"Cdecl\",\"returnType\":\"F64\",\"parameterTypeList\":[\"I32\",\"I32\",\"I32\",\"I64\",\"F32\",\"F64\"]},"
        "{\"publicName\":\"Native.float8E4M3ToStruct\",\"entryPoint\":\"sl_float8_e4m3_to_struct\",\"callingConvention\":\"Cdecl\",\"returnType\":\"Void\",\"parameterTypeList\":[\"F32\",\"Ptr\"]},"
        "{\"publicName\":\"Native.float8E5M2ToStruct\",\"entryPoint\":\"sl_float8_e5m2_to_struct\",\"callingConvention\":\"Cdecl\",\"returnType\":\"Void\",\"parameterTypeList\":[\"F32\",\"Ptr\"]},"
        "{\"publicName\":\"Native.float8BitsToStruct\",\"entryPoint\":\"sl_float8_bits_to_struct\",\"callingConvention\":\"Cdecl\",\"returnType\":\"Void\",\"parameterTypeList\":[\"I32\",\"Ptr\"]},"
        "{\"publicName\":\"Native.float8DualToStruct\",\"entryPoint\":\"sl_float8_dual_to_struct\",\"callingConvention\":\"Cdecl\",\"returnType\":\"Void\",\"parameterTypeList\":[\"F32\",\"Ptr\",\"Ptr\"]},"
        "{\"publicName\":\"Native.structToFloat8Bits\",\"entryPoint\":\"sl_struct_to_float8_bits\",\"callingConvention\":\"Cdecl\",\"returnType\":\"I32\",\"parameterTypeList\":[\"Ptr\"]},"
        "{\"publicName\":\"Native.float8Roundtrip\",\"entryPoint\":\"sl_float8_e4m3_roundtrip\",\"callingConvention\":\"Cdecl\",\"returnType\":\"F32\",\"parameterTypeList\":[\"F32\"]},"
        "{\"publicName\":\"Native.float8E5M2Roundtrip\",\"entryPoint\":\"sl_float8_e5m2_roundtrip\",\"callingConvention\":\"Cdecl\",\"returnType\":\"F32\",\"parameterTypeList\":[\"F32\"]},"
        "{\"publicName\":\"Native.float16Roundtrip\",\"entryPoint\":\"sl_float16_roundtrip\",\"callingConvention\":\"Cdecl\",\"returnType\":\"F32\",\"parameterTypeList\":[\"F32\"]},"
        "{\"publicName\":\"Native.mixToStruct\",\"entryPoint\":\"sl_mix_to_struct\",\"callingConvention\":\"Cdecl\",\"returnType\":\"Void\",\"parameterTypeList\":[\"I32\",\"I64\",\"F32\",\"F64\",\"Ptr\"]},"
        "{\"publicName\":\"Native.structSum\",\"entryPoint\":\"sl_struct_sum\",\"callingConvention\":\"Cdecl\",\"returnType\":\"F64\",\"parameterTypeList\":[\"Ptr\"]},"
        "{\"publicName\":\"Native.structAddI64\",\"entryPoint\":\"sl_struct_add_i64\",\"callingConvention\":\"Cdecl\",\"returnType\":\"I64\",\"parameterTypeList\":[\"Ptr\",\"I64\"]},"
        "{\"publicName\":\"Native.getAdder\",\"entryPoint\":\"sl_get_adder\",\"callingConvention\":\"Cdecl\",\"returnType\":\"Ptr\",\"parameterTypeList\":[]},"
        "{\"publicName\":\"Native.getMultiplier\",\"entryPoint\":\"sl_get_multiplier\",\"callingConvention\":\"Cdecl\",\"returnType\":\"Ptr\",\"parameterTypeList\":[\"I64\"]},"
        "{\"publicName\":\"Native.callFnPtr\",\"entryPoint\":\"sl_call_fn_ptr\",\"callingConvention\":\"Cdecl\",\"returnType\":\"I64\",\"parameterTypeList\":[\"Ptr\",\"I64\",\"I64\"]},"
        "{\"publicName\":\"Native.callWithCallback\",\"entryPoint\":\"sl_call_with_callback\",\"callingConvention\":\"Cdecl\",\"returnType\":\"I64\",\"parameterTypeList\":[\"Ptr\",\"I64\"]},"
        "{\"publicName\":\"Native.reduceWithCallback\",\"entryPoint\":\"sl_reduce_with_callback\",\"callingConvention\":\"Cdecl\",\"returnType\":\"I64\",\"parameterTypeList\":[\"Ptr\",\"I64\",\"I64\",\"I64\"]},"
        "{\"publicName\":\"Native.callTwoCallbacks\",\"entryPoint\":\"sl_call_two_callbacks\",\"callingConvention\":\"Cdecl\",\"returnType\":\"I64\",\"parameterTypeList\":[\"Ptr\",\"Ptr\",\"I64\",\"I64\"]},"
        "{\"publicName\":\"Native.echo\",\"entryPoint\":\"sl_echo\",\"callingConvention\":\"Cdecl\",\"returnType\":\"Utf8String\",\"parameterTypeList\":[\"Utf8String\"]},"
        "{\"publicName\":\"Native.concat\",\"entryPoint\":\"sl_concat\",\"callingConvention\":\"Cdecl\",\"returnType\":\"Utf8String\",\"parameterTypeList\":[\"Utf8String\",\"Utf8String\"]},"
        "{\"publicName\":\"Native.strlen\",\"entryPoint\":\"sl_strlen_utf8\",\"callingConvention\":\"Cdecl\",\"returnType\":\"I64\",\"parameterTypeList\":[\"Utf8String\"]},"
        "{\"publicName\":\"Native.bookAlloc\",\"entryPoint\":\"sl_book_alloc\",\"callingConvention\":\"Cdecl\",\"returnType\":\"Ptr\",\"parameterTypeList\":[\"I32\",\"F32\",\"I32\",\"Utf8String\"]},"
        "{\"publicName\":\"Native.bookPrint\",\"entryPoint\":\"sl_book_print\",\"callingConvention\":\"Cdecl\",\"returnType\":\"I64\",\"parameterTypeList\":[\"Ptr\"]},"
        "{\"publicName\":\"Native.bookFree\",\"entryPoint\":\"sl_book_free\",\"callingConvention\":\"Cdecl\",\"returnType\":\"I64\",\"parameterTypeList\":[\"Ptr\"]}"
        "]"
    "}";
}
