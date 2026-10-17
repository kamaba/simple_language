/*
 * float16_4x4_vm_method_call.c —— Float16_4x4 的 systemCall 原生实现。
 *
 * 与 float64_4x4_vm_method_call.c 同模式：jsonc 声明
 *
 *     { "name": "SystemMathMat4hMul", "returnType": "void",
 *       "params": [ "object", "object", "object" ],
 *       "isVariadic": false,
 *       "cvmFunction": "math_lib.dll!mathvm_mat4h_mul" }
 *
 * 由 cvm 的 OpCode_CallSystemMethod 直接派发（签名 int32 f(VM* vm,
 * int32 param_count)），Float16_4x4.sl 侧以 `SystemMathMat4hMul( this, b, r )`
 * 调用。同一个 math_lib.dll 既供 FFI 也供 vmDll 使用。
 *
 * 参数约定：最后一个参数在栈顶（IRCall.ParseSystemCall 按参数顺序压栈），
 * C 侧按逆序 pop；返回值由实现自己压回（determinant -> Float32，
 * eq -> bool，其余写 out 对象返回 void）。
 *
 * Float16 栈/成员表示：VM 中 Float16 以 IEEE 754 binary16 位模式存储
 * （矩阵成员为 uint16；栈上标量无 f16 pop/push），因此标量形参在 jsonc
 * 声明为 "Float32"（前端在调用点自动插入 IRConvert F16->R4），C 侧照常
 * slvm_pop_f32；determinant 的 returnType 同为 "Float32"（返回处前端
 * 自动 R4->F16 收敛回 Float16）。
 *
 * 计算语义：与 SL 版（toFloat32() 中转走 Mathf.dot3 FFI）一致——half 转
 * float32 无损，计算在 f32 中间精度进行，仅写回成员时经
 * mathvm_float_to_half 舍入；transpose/copy 为纯位操作不经转换。
 *
 * 布局：Float16_4x4 的 16 个 Float16 字段按声明顺序紧凑排布，
 * member_data[0..32) 即 uint16 m[16]（行主序，m[row*4+col]，
 * m00..m33 对应索引 0..15）；Float16_3 的 x/y/z 即 member_data[0..6)。
 * 弹栈校验必须匹配实际对象类型（mat4h=32B / vec3h=6B）。
 */

#include <math.h>

#include "math_vm_object_bridge.h"

#define MATHVM_MAT4_HALF_COUNT 16
#define MATHVM_MAT4H_BYTE_SIZE 32  /* 16 * sizeof(uint16) */
#define MATHVM_VEC3H_BYTE_SIZE 6   /* 3 * sizeof(uint16) */

/* ------------------------------------------------------------------ */
/* 内部帮助                                                            */
/* ------------------------------------------------------------------ */

/* 弹出一个矩阵对象并校验（member_data_size == 32）。 */
static int32 mathvm_mat4h_pop(VM* vm, uint16** out_mat)
{
    void* obj = NULL;

    if (!mathvm_pop_object(vm, &obj))
    {
        return FALSE;
    }
    if (!mathvm_obj_is_valid(obj, MATHVM_MAT4H_BYTE_SIZE))
    {
        return FALSE;
    }
    *out_mat = (uint16*)MATHVM_OBJ_MEMBER_DATA(obj);
    return TRUE;
}

/* 弹出一个向量对象并校验（member_data_size == 6）。 */
static int32 mathvm_vec3h_pop(VM* vm, uint16** out_vec)
{
    void* obj = NULL;

    if (!mathvm_pop_object(vm, &obj))
    {
        return FALSE;
    }
    if (!mathvm_obj_is_valid(obj, MATHVM_VEC3H_BYTE_SIZE))
    {
        return FALSE;
    }
    *out_vec = (uint16*)MATHVM_OBJ_MEMBER_DATA(obj);
    return TRUE;
}

/* half 向量加载到 f32（rotationAxis / lookAt 内部使用）。 */
static void mathvm_vec3h_load(const uint16* v, float32* o)
{
    o[0] = mathvm_half_to_float(v[0]);
    o[1] = mathvm_half_to_float(v[1]);
    o[2] = mathvm_half_to_float(v[2]);
}

/* 三维向量点积 / 叉积 / 归一化（f32 中间精度）。 */
static float32 mathvm_vec3f_dot(const float32* a, const float32* b)
{
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
}

static void mathvm_vec3f_cross(const float32* a, const float32* b, float32* o)
{
    o[0] = a[1] * b[2] - a[2] * b[1];
    o[1] = a[2] * b[0] - a[0] * b[2];
    o[2] = a[0] * b[1] - a[1] * b[0];
}

static void mathvm_vec3f_normalize(const float32* v, float32* o)
{
    float32 len = sqrtf(mathvm_vec3f_dot(v, v));

    if (len > 0.0f)
    {
        float32 inv = 1.0f / len;
        o[0] = v[0] * inv;
        o[1] = v[1] * inv;
        o[2] = v[2] * inv;
    }
    else
    {
        o[0] = 0.0f;
        o[1] = 0.0f;
        o[2] = 0.0f;
    }
}

/* ------------------------------------------------------------------ */
/* 矩阵乘                                                              */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4h_mul(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    uint16* b = NULL;
    uint16* o = NULL;
    float32 af[MATHVM_MAT4_HALF_COUNT];
    float32 bf[MATHVM_MAT4_HALF_COUNT];
    int32 r = 0;
    int32 c = 0;
    int32 k = 0;
    int32 i = 0;
    float32 sum = 0.0f;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) || /* out：栈顶 */
        !mathvm_mat4h_pop(vm, &b) ||
        !mathvm_mat4h_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT4_HALF_COUNT; i++)
    {
        af[i] = mathvm_half_to_float(a[i]);
        bf[i] = mathvm_half_to_float(b[i]);
    }

    for (r = 0; r < 4; r++)
    {
        for (c = 0; c < 4; c++)
        {
            sum = 0.0f;
            for (k = 0; k < 4; k++)
            {
                sum += af[r * 4 + k] * bf[k * 4 + c];
            }
            o[r * 4 + c] = mathvm_float_to_half(sum);
        }
    }
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 变换点（w 补 1，带平移）/ 变换方向（w 补 0，忽略平移）               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4h_transform_point(VM* vm, int32 param_count)
{
    uint16* m = NULL;
    uint16* v = NULL;
    uint16* o = NULL;
    float32 mf[MATHVM_MAT4_HALF_COUNT];
    float32 x = 0.0f;
    float32 y = 0.0f;
    float32 z = 0.0f;
    int32 i = 0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_vec3h_pop(vm, &o) ||                  /* out：栈顶，Float16_3 */
        !mathvm_vec3h_pop(vm, &v) ||                  /* v：Float16_3 */
        !mathvm_mat4h_pop(vm, &m))                    /* m：Float16_4x4 */
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT4_HALF_COUNT; i++)
    {
        mf[i] = mathvm_half_to_float(m[i]);
    }
    x = mathvm_half_to_float(v[0]);
    y = mathvm_half_to_float(v[1]);
    z = mathvm_half_to_float(v[2]);

    o[0] = mathvm_float_to_half(mf[0] * x + mf[1] * y + mf[2] * z + mf[3]);
    o[1] = mathvm_float_to_half(mf[4] * x + mf[5] * y + mf[6] * z + mf[7]);
    o[2] = mathvm_float_to_half(mf[8] * x + mf[9] * y + mf[10] * z + mf[11]);
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat4h_transform_direction(VM* vm, int32 param_count)
{
    uint16* m = NULL;
    uint16* v = NULL;
    uint16* o = NULL;
    float32 mf[MATHVM_MAT4_HALF_COUNT];
    float32 x = 0.0f;
    float32 y = 0.0f;
    float32 z = 0.0f;
    int32 i = 0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_vec3h_pop(vm, &o) ||
        !mathvm_vec3h_pop(vm, &v) ||
        !mathvm_mat4h_pop(vm, &m))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT4_HALF_COUNT; i++)
    {
        mf[i] = mathvm_half_to_float(m[i]);
    }
    x = mathvm_half_to_float(v[0]);
    y = mathvm_half_to_float(v[1]);
    z = mathvm_half_to_float(v[2]);

    o[0] = mathvm_float_to_half(mf[0] * x + mf[1] * y + mf[2] * z);
    o[1] = mathvm_float_to_half(mf[4] * x + mf[5] * y + mf[6] * z);
    o[2] = mathvm_float_to_half(mf[8] * x + mf[9] * y + mf[10] * z);
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 转置（纯位模式搬移，不经 half 转换）                                 */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4h_transpose(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    uint16* o = NULL;
    int32 r = 0;
    int32 c = 0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) ||
        !mathvm_mat4h_pop(vm, &a))
    {
        return FALSE;
    }

    for (r = 0; r < 4; r++)
    {
        for (c = 0; c < 4; c++)
        {
            o[r * 4 + c] = a[c * 4 + r];
        }
    }
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 行列式（与 SL 版伴随矩阵法一致；f32 中转，压回 Float32 栈槽）        */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4h_determinant(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    float32 af[MATHVM_MAT4_HALF_COUNT];
    float32 b00 = 0.0f;
    float32 b01 = 0.0f;
    float32 b02 = 0.0f;
    float32 b03 = 0.0f;
    float32 b04 = 0.0f;
    float32 b05 = 0.0f;
    float32 b06 = 0.0f;
    float32 b07 = 0.0f;
    float32 b08 = 0.0f;
    float32 b09 = 0.0f;
    float32 b10 = 0.0f;
    float32 b11 = 0.0f;
    int32 i = 0;

    if (param_count != 1)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT4_HALF_COUNT; i++)
    {
        af[i] = mathvm_half_to_float(a[i]);
    }

    b00 = af[0] * af[5] - af[1] * af[4];
    b01 = af[0] * af[6] - af[2] * af[4];
    b02 = af[0] * af[7] - af[3] * af[4];
    b03 = af[1] * af[6] - af[2] * af[5];
    b04 = af[1] * af[7] - af[3] * af[5];
    b05 = af[2] * af[7] - af[3] * af[6];
    b06 = af[8] * af[13] - af[9] * af[12];
    b07 = af[8] * af[14] - af[10] * af[12];
    b08 = af[8] * af[15] - af[11] * af[12];
    b09 = af[9] * af[14] - af[10] * af[13];
    b10 = af[9] * af[15] - af[11] * af[13];
    b11 = af[10] * af[15] - af[11] * af[14];

    return slvm_push_f32(vm, b00 * b11 - b01 * b10 + b02 * b09
                               + b03 * b08 - b04 * b07 + b05 * b06)
        ? TRUE : FALSE;
}

/* ------------------------------------------------------------------ */
/* 逆矩阵（det == 0 时 out 清零，与 SL 版返回零矩阵一致）               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4h_inverse(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    uint16* o = NULL;
    float32 af[MATHVM_MAT4_HALF_COUNT];
    float32 b00 = 0.0f;
    float32 b01 = 0.0f;
    float32 b02 = 0.0f;
    float32 b03 = 0.0f;
    float32 b04 = 0.0f;
    float32 b05 = 0.0f;
    float32 b06 = 0.0f;
    float32 b07 = 0.0f;
    float32 b08 = 0.0f;
    float32 b09 = 0.0f;
    float32 b10 = 0.0f;
    float32 b11 = 0.0f;
    float32 det = 0.0f;
    float32 inv = 0.0f;
    int32 i = 0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) ||
        !mathvm_mat4h_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT4_HALF_COUNT; i++)
    {
        af[i] = mathvm_half_to_float(a[i]);
    }

    b00 = af[0] * af[5] - af[1] * af[4];
    b01 = af[0] * af[6] - af[2] * af[4];
    b02 = af[0] * af[7] - af[3] * af[4];
    b03 = af[1] * af[6] - af[2] * af[5];
    b04 = af[1] * af[7] - af[3] * af[5];
    b05 = af[2] * af[7] - af[3] * af[6];
    b06 = af[8] * af[13] - af[9] * af[12];
    b07 = af[8] * af[14] - af[10] * af[12];
    b08 = af[8] * af[15] - af[11] * af[12];
    b09 = af[9] * af[14] - af[10] * af[13];
    b10 = af[9] * af[15] - af[11] * af[13];
    b11 = af[10] * af[15] - af[11] * af[14];

    det = b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06;

    if (det == 0.0f)
    {
        memset(o, 0, MATHVM_MAT4H_BYTE_SIZE);
        return TRUE;
    }
    inv = 1.0f / det;

    o[0]  = mathvm_float_to_half((af[5] * b11 - af[6] * b10 + af[7] * b09) * inv);
    o[1]  = mathvm_float_to_half((af[2] * b10 - af[1] * b11 - af[3] * b09) * inv);
    o[2]  = mathvm_float_to_half((af[13] * b05 - af[14] * b04 + af[15] * b03) * inv);
    o[3]  = mathvm_float_to_half((af[10] * b04 - af[11] * b05 - af[9] * b03) * inv);
    o[4]  = mathvm_float_to_half((af[6] * b08 - af[4] * b11 - af[7] * b07) * inv);
    o[5]  = mathvm_float_to_half((af[0] * b11 - af[2] * b08 + af[3] * b07) * inv);
    o[6]  = mathvm_float_to_half((af[14] * b02 - af[12] * b05 - af[15] * b01) * inv);
    o[7]  = mathvm_float_to_half((af[8] * b05 - af[10] * b02 + af[11] * b01) * inv);
    o[8]  = mathvm_float_to_half((af[4] * b10 - af[5] * b08 + af[7] * b06) * inv);
    o[9]  = mathvm_float_to_half((af[1] * b08 - af[0] * b10 - af[3] * b06) * inv);
    o[10] = mathvm_float_to_half((af[12] * b04 - af[13] * b02 + af[15] * b00) * inv);
    o[11] = mathvm_float_to_half((af[9] * b02 - af[8] * b04 - af[11] * b00) * inv);
    o[12] = mathvm_float_to_half((af[5] * b07 - af[4] * b09 - af[6] * b06) * inv);
    o[13] = mathvm_float_to_half((af[0] * b09 - af[1] * b07 + af[2] * b06) * inv);
    o[14] = mathvm_float_to_half((af[13] * b01 - af[12] * b03 - af[14] * b00) * inv);
    o[15] = mathvm_float_to_half((af[8] * b03 - af[9] * b01 + af[10] * b00) * inv);
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 矩阵加                                                               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4h_add(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    uint16* b = NULL;
    uint16* o = NULL;
    int32 i = 0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) ||
        !mathvm_mat4h_pop(vm, &b) ||
        !mathvm_mat4h_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT4_HALF_COUNT; i++)
    {
        o[i] = mathvm_float_to_half(mathvm_half_to_float(a[i])
                                  + mathvm_half_to_float(b[i]));
    }
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 逐元素相等（返回 bool；经 f32 数值比较，正确处理 +-0）               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4h_eq(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    uint16* b = NULL;
    int32 i = 0;
    int32 eq = TRUE;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &b) ||
        !mathvm_mat4h_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT4_HALF_COUNT; i++)
    {
        if (mathvm_half_to_float(a[i]) != mathvm_half_to_float(b[i]))
        {
            eq = FALSE;
            break;
        }
    }
    return slvm_push_bool(vm, eq) ? TRUE : FALSE;
}

/* ------------------------------------------------------------------ */
/* 拷贝（纯位模式搬移）                                                 */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4h_copy(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    uint16* o = NULL;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) ||
        !mathvm_mat4h_pop(vm, &a))
    {
        return FALSE;
    }

    memcpy(o, a, MATHVM_MAT4H_BYTE_SIZE);
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 常量矩阵 / 工厂                                                      */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4h_identity(VM* vm, int32 param_count)
{
    uint16* o = NULL;

    if (param_count != 1)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o))
    {
        return FALSE;
    }

    memset(o, 0, MATHVM_MAT4H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(1.0f);
    o[5] = mathvm_float_to_half(1.0f);
    o[10] = mathvm_float_to_half(1.0f);
    o[15] = mathvm_float_to_half(1.0f);
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat4h_translation(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    float32 tz = 0.0f;
    float32 ty = 0.0f;
    float32 tx = 0.0f;

    if (param_count != 4)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) ||      /* out：栈顶 */
        !slvm_pop_f32(vm, &tz) ||
        !slvm_pop_f32(vm, &ty) ||
        !slvm_pop_f32(vm, &tx))
    {
        return FALSE;
    }

    memset(o, 0, MATHVM_MAT4H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(1.0f);
    o[5] = mathvm_float_to_half(1.0f);
    o[10] = mathvm_float_to_half(1.0f);
    o[3] = mathvm_float_to_half(tx);   /* m03：行主序，平移在第 4 列 */
    o[7] = mathvm_float_to_half(ty);   /* m13 */
    o[11] = mathvm_float_to_half(tz);  /* m23 */
    o[15] = mathvm_float_to_half(1.0f);
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat4h_scale(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    float32 sz = 0.0f;
    float32 sy = 0.0f;
    float32 sx = 0.0f;

    if (param_count != 4)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) ||
        !slvm_pop_f32(vm, &sz) ||
        !slvm_pop_f32(vm, &sy) ||
        !slvm_pop_f32(vm, &sx))
    {
        return FALSE;
    }

    memset(o, 0, MATHVM_MAT4H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(sx);
    o[5] = mathvm_float_to_half(sy);
    o[10] = mathvm_float_to_half(sz);
    o[15] = mathvm_float_to_half(1.0f);
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat4h_rotation_x(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    float32 radians = 0.0f;
    float32 c = 0.0f;
    float32 s = 0.0f;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) ||
        !slvm_pop_f32(vm, &radians))
    {
        return FALSE;
    }

    c = cosf(radians);
    s = sinf(radians);
    memset(o, 0, MATHVM_MAT4H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(1.0f);
    o[5] = mathvm_float_to_half(c);
    o[6] = mathvm_float_to_half(-s);
    o[9] = mathvm_float_to_half(s);
    o[10] = mathvm_float_to_half(c);
    o[15] = mathvm_float_to_half(1.0f);
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat4h_rotation_y(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    float32 radians = 0.0f;
    float32 c = 0.0f;
    float32 s = 0.0f;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) ||
        !slvm_pop_f32(vm, &radians))
    {
        return FALSE;
    }

    c = cosf(radians);
    s = sinf(radians);
    memset(o, 0, MATHVM_MAT4H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(c);
    o[2] = mathvm_float_to_half(s);
    o[5] = mathvm_float_to_half(1.0f);
    o[8] = mathvm_float_to_half(-s);
    o[10] = mathvm_float_to_half(c);
    o[15] = mathvm_float_to_half(1.0f);
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat4h_rotation_z(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    float32 radians = 0.0f;
    float32 c = 0.0f;
    float32 s = 0.0f;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) ||
        !slvm_pop_f32(vm, &radians))
    {
        return FALSE;
    }

    c = cosf(radians);
    s = sinf(radians);
    memset(o, 0, MATHVM_MAT4H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(c);
    o[1] = mathvm_float_to_half(-s);
    o[4] = mathvm_float_to_half(s);
    o[5] = mathvm_float_to_half(c);
    o[10] = mathvm_float_to_half(1.0f);
    o[15] = mathvm_float_to_half(1.0f);
    return TRUE;
}

/* 绕任意轴旋转（axis 先归一化，与 SL 版一致） */
SLVM_FUNC_EXPORT int32 mathvm_mat4h_rotation_axis(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    float32 radians = 0.0f;
    uint16* axis = NULL;
    float32 a[3] = { 0.0f, 0.0f, 0.0f };
    float32 x = 0.0f;
    float32 y = 0.0f;
    float32 z = 0.0f;
    float32 c = 0.0f;
    float32 s = 0.0f;
    float32 t = 0.0f;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) ||              /* out：栈顶，Float16_4x4 */
        !slvm_pop_f32(vm, &radians) ||
        !mathvm_vec3h_pop(vm, &axis))             /* axis：Float16_3 */
    {
        return FALSE;
    }

    mathvm_vec3h_load(axis, a);
    mathvm_vec3f_normalize(a, a);
    x = a[0];
    y = a[1];
    z = a[2];
    c = cosf(radians);
    s = sinf(radians);
    t = 1.0f - c;

    memset(o, 0, MATHVM_MAT4H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(t * x * x + c);
    o[1] = mathvm_float_to_half(t * x * y - s * z);
    o[2] = mathvm_float_to_half(t * x * z + s * y);
    o[4] = mathvm_float_to_half(t * x * y + s * z);
    o[5] = mathvm_float_to_half(t * y * y + c);
    o[6] = mathvm_float_to_half(t * y * z - s * x);
    o[8] = mathvm_float_to_half(t * x * z - s * y);
    o[9] = mathvm_float_to_half(t * y * z + s * x);
    o[10] = mathvm_float_to_half(t * z * z + c);
    o[15] = mathvm_float_to_half(1.0f);
    return TRUE;
}

/* 透视投影（右手系，depth 映射到 [-1,1]） */
SLVM_FUNC_EXPORT int32 mathvm_mat4h_perspective(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    float32 far_z = 0.0f;
    float32 near_z = 0.0f;
    float32 aspect = 0.0f;
    float32 fov_y = 0.0f;
    float32 f = 0.0f;

    if (param_count != 5)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) ||
        !slvm_pop_f32(vm, &far_z) ||
        !slvm_pop_f32(vm, &near_z) ||
        !slvm_pop_f32(vm, &aspect) ||
        !slvm_pop_f32(vm, &fov_y))
    {
        return FALSE;
    }

    f = 1.0f / tanf(fov_y * 0.5f);
    memset(o, 0, MATHVM_MAT4H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(f / aspect);
    o[5] = mathvm_float_to_half(f);
    o[10] = mathvm_float_to_half((far_z + near_z) / (near_z - far_z));
    o[11] = mathvm_float_to_half((2.0f * far_z * near_z) / (near_z - far_z));
    o[14] = mathvm_float_to_half(-1.0f);
    return TRUE;
}

/* 正交投影 */
SLVM_FUNC_EXPORT int32 mathvm_mat4h_ortho(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    float32 far_z = 0.0f;
    float32 near_z = 0.0f;
    float32 top = 0.0f;
    float32 bottom = 0.0f;
    float32 right = 0.0f;
    float32 left = 0.0f;

    if (param_count != 7)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) ||
        !slvm_pop_f32(vm, &far_z) ||
        !slvm_pop_f32(vm, &near_z) ||
        !slvm_pop_f32(vm, &top) ||
        !slvm_pop_f32(vm, &bottom) ||
        !slvm_pop_f32(vm, &right) ||
        !slvm_pop_f32(vm, &left))
    {
        return FALSE;
    }

    memset(o, 0, MATHVM_MAT4H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(2.0f / (right - left));
    o[3] = mathvm_float_to_half(0.0f - (right + left) / (right - left));
    o[5] = mathvm_float_to_half(2.0f / (top - bottom));
    o[7] = mathvm_float_to_half(0.0f - (top + bottom) / (top - bottom));
    o[10] = mathvm_float_to_half(0.0f - 2.0f / (far_z - near_z));
    o[11] = mathvm_float_to_half(0.0f - (far_z + near_z) / (far_z - near_z));
    o[15] = mathvm_float_to_half(1.0f);
    return TRUE;
}

/* 视图矩阵（右手系 lookAt） */
SLVM_FUNC_EXPORT int32 mathvm_mat4h_look_at(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    uint16* up = NULL;
    uint16* target = NULL;
    uint16* eye = NULL;
    float32 up_f[3] = { 0.0f, 0.0f, 0.0f };
    float32 target_f[3] = { 0.0f, 0.0f, 0.0f };
    float32 eye_f[3] = { 0.0f, 0.0f, 0.0f };
    float32 z_axis[3] = { 0.0f, 0.0f, 0.0f };
    float32 x_axis[3] = { 0.0f, 0.0f, 0.0f };
    float32 y_axis[3] = { 0.0f, 0.0f, 0.0f };
    float32 tmp[3] = { 0.0f, 0.0f, 0.0f };

    if (param_count != 4)
    {
        return FALSE;
    }
    if (!mathvm_mat4h_pop(vm, &o) ||              /* out：栈顶，Float16_4x4 */
        !mathvm_vec3h_pop(vm, &up) ||             /* upHint：Float16_3 */
        !mathvm_vec3h_pop(vm, &target) ||         /* target：Float16_3 */
        !mathvm_vec3h_pop(vm, &eye))              /* eye：Float16_3 */
    {
        return FALSE;
    }

    mathvm_vec3h_load(up, up_f);
    mathvm_vec3h_load(target, target_f);
    mathvm_vec3h_load(eye, eye_f);

    /* zAxis = (eye - target).normalize() */
    tmp[0] = eye_f[0] - target_f[0];
    tmp[1] = eye_f[1] - target_f[1];
    tmp[2] = eye_f[2] - target_f[2];
    mathvm_vec3f_normalize(tmp, z_axis);

    /* xAxis = cross(up, zAxis).normalize() */
    mathvm_vec3f_cross(up_f, z_axis, tmp);
    mathvm_vec3f_normalize(tmp, x_axis);

    /* yAxis = cross(zAxis, xAxis) */
    mathvm_vec3f_cross(z_axis, x_axis, y_axis);

    o[0] = mathvm_float_to_half(x_axis[0]);
    o[1] = mathvm_float_to_half(x_axis[1]);
    o[2] = mathvm_float_to_half(x_axis[2]);
    o[3] = mathvm_float_to_half(0.0f - mathvm_vec3f_dot(x_axis, eye_f));
    o[4] = mathvm_float_to_half(y_axis[0]);
    o[5] = mathvm_float_to_half(y_axis[1]);
    o[6] = mathvm_float_to_half(y_axis[2]);
    o[7] = mathvm_float_to_half(0.0f - mathvm_vec3f_dot(y_axis, eye_f));
    o[8] = mathvm_float_to_half(z_axis[0]);
    o[9] = mathvm_float_to_half(z_axis[1]);
    o[10] = mathvm_float_to_half(z_axis[2]);
    o[11] = mathvm_float_to_half(0.0f - mathvm_vec3f_dot(z_axis, eye_f));
    o[12] = 0;
    o[13] = 0;
    o[14] = 0;
    o[15] = mathvm_float_to_half(1.0f);
    return TRUE;
}
