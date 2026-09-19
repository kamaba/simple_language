/*
 * math_lib.c —— Math 模块 FFI 原生实现（产物 math_lib.dll）。
 *
 * 供 Mathf.sl / Mathd.sl / 矩阵类的 @DllImport 声明直接绑定
 * （FFI.StaticLibrary.bindFunction，普通 C ABI，无 VM 栈依赖，
 * 见 md/design/ffi-design.md）：
 *
 *     @DllImport( "math_lib", "mathf_sin", "Float32->Float32" )
 *     public static Float32 sin( Float32 value ) { ...SL fallback... }
 *
 * 导出命名约定：
 *   mathf_* —— Float32（单精度），C 形参/返回 float，sig 用 SL 名 "Float32"；
 *   mathd_* —— Float64（双精度），C 形参/返回 double，sig 用 SL 名 "Float64"；
 *   *dot3   —— 三元素内积（矩阵乘法/变换按行、列元素直接传参，
 *              6 实参恰为 FFI 调度上限，避开 SL 数组寻址开销）。
 *
 * 调用约定：x64 唯一（Microsoft x64），与 sl_ffi_call 的 INT/DBL
 * 分类调度对齐（float/double 实参与返回均走 XMM 寄存器）。
 */

#include <math.h>

#define MATH_LIB_API __declspec(dllexport)

/* ===================== Float32（mathf_*） ===================== */

MATH_LIB_API float mathf_sin(float x) { return sinf(x); }
MATH_LIB_API float mathf_cos(float x) { return cosf(x); }
MATH_LIB_API float mathf_tan(float x) { return tanf(x); }
MATH_LIB_API float mathf_asin(float x) { return asinf(x); }
MATH_LIB_API float mathf_acos(float x) { return acosf(x); }
MATH_LIB_API float mathf_atan(float x) { return atanf(x); }
MATH_LIB_API float mathf_atan2(float y, float x) { return atan2f(y, x); }
MATH_LIB_API float mathf_sinh(float x) { return sinhf(x); }
MATH_LIB_API float mathf_cosh(float x) { return coshf(x); }
MATH_LIB_API float mathf_tanh(float x) { return tanhf(x); }
MATH_LIB_API float mathf_pow(float x, float y) { return powf(x, y); }
MATH_LIB_API float mathf_sqrt(float x) { return sqrtf(x); }
MATH_LIB_API float mathf_exp(float x) { return expf(x); }
MATH_LIB_API float mathf_log(float x) { return logf(x); }
MATH_LIB_API float mathf_log10(float x) { return log10f(x); }
MATH_LIB_API float mathf_ceil(float x) { return ceilf(x); }
MATH_LIB_API float mathf_floor(float x) { return floorf(x); }
MATH_LIB_API float mathf_round(float x) { return roundf(x); }
MATH_LIB_API int   mathf_truncate(float x) { return (int)x; }

MATH_LIB_API float mathf_dot3(float a0, float a1, float a2,
                               float b0, float b1, float b2)
{
    return a0 * b0 + a1 * b1 + a2 * b2;
}

/* ===================== Float64（mathd_*） ===================== */

MATH_LIB_API double mathd_sin(double x) { return sin(x); }
MATH_LIB_API double mathd_cos(double x) { return cos(x); }
MATH_LIB_API double mathd_tan(double x) { return tan(x); }
MATH_LIB_API double mathd_asin(double x) { return asin(x); }
MATH_LIB_API double mathd_acos(double x) { return acos(x); }
MATH_LIB_API double mathd_atan(double x) { return atan(x); }
MATH_LIB_API double mathd_atan2(double y, double x) { return atan2(y, x); }
MATH_LIB_API double mathd_sinh(double x) { return sinh(x); }
MATH_LIB_API double mathd_cosh(double x) { return cosh(x); }
MATH_LIB_API double mathd_tanh(double x) { return tanh(x); }
MATH_LIB_API double mathd_pow(double x, double y) { return pow(x, y); }
MATH_LIB_API double mathd_sqrt(double x) { return sqrt(x); }
MATH_LIB_API double mathd_exp(double x) { return exp(x); }
MATH_LIB_API double mathd_log(double x) { return log(x); }
MATH_LIB_API double mathd_log10(double x) { return log10(x); }
MATH_LIB_API double mathd_ceil(double x) { return ceil(x); }
MATH_LIB_API double mathd_floor(double x) { return floor(x); }
MATH_LIB_API double mathd_round(double x) { return round(x); }
MATH_LIB_API int    mathd_truncate(double x) { return (int)x; }

MATH_LIB_API double mathd_dot3(double a0, double a1, double a2,
                               double b0, double b1, double b2)
{
    return a0 * b0 + a1 * b1 + a2 * b2;
}
