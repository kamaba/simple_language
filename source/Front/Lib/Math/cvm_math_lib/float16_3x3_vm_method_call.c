/*
 * float16_3x3_vm_method_call.c —— Float16_3x3 的 systemCall 原生实现。
 *
 * 与 float64_3x3_vm_method_call.c 同模式：jsonc 声明
 *
 *     { "name": "SystemMathMat3hMul", "returnType": "void",
 *       "params": [ "object", "object", "object" ],
 *       "isVariadic": false,
 *       "cvmFunction": "math_lib.dll!mathvm_mat3h_mul" }
 *
 * 由 cvm 的 OpCode_CallSystemMethod 直接派发（签名 int32 f(VM* vm,
 * int32 param_count)），Float16_3x3.sl 侧以 `SystemMathMat3hMul( this, b, r )`
 * 调用。同一个 math_lib.dll 既供 FFI（dllImports 别名）也供 vmDll
 * （systemCall 派发）使用。
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
 * 布局：Float16_3x3 的 9 个 Float16 字段按声明顺序紧凑排布，
 * member_data[0..18) 即 uint16 m[9]（行主序，m[row*3+col]，
 * m00..m22 对应索引 0..8）；Float16_3 的 x/y/z 即 member_data[0..6)。
 */

#include <math.h>

#include "math_vm_object_bridge.h"

#define MATHVM_MAT3_HALF_COUNT 9
#define MATHVM_MAT3H_BYTE_SIZE 18  /* 9 * sizeof(uint16) */
#define MATHVM_VEC3H_BYTE_SIZE 6   /* 3 * sizeof(uint16) */

/* ------------------------------------------------------------------ */
/* 内部帮助                                                            */
/* ------------------------------------------------------------------ */

/* 弹出一个矩阵对象并校验（member_data_size == 18）。 */
static int32 mathvm_mat3h_pop(VM* vm, uint16** out_mat)
{
    void* obj = NULL;

    if (!mathvm_pop_object(vm, &obj))
    {
        return FALSE;
    }
    if (!mathvm_obj_is_valid(obj, MATHVM_MAT3H_BYTE_SIZE))
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

/* ------------------------------------------------------------------ */
/* 矩阵乘                                                              */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3h_mul(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    uint16* b = NULL;
    uint16* o = NULL;
    float32 af[MATHVM_MAT3_HALF_COUNT];
    float32 bf[MATHVM_MAT3_HALF_COUNT];
    int32 r = 0;
    int32 c = 0;
    int32 i = 0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat3h_pop(vm, &o) || /* out：栈顶 */
        !mathvm_mat3h_pop(vm, &b) ||
        !mathvm_mat3h_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT3_HALF_COUNT; i++)
    {
        af[i] = mathvm_half_to_float(a[i]);
        bf[i] = mathvm_half_to_float(b[i]);
    }

    for (r = 0; r < 3; r++)
    {
        for (c = 0; c < 3; c++)
        {
            o[r * 3 + c] = mathvm_float_to_half(af[r * 3 + 0] * bf[0 * 3 + c]
                                              + af[r * 3 + 1] * bf[1 * 3 + c]
                                              + af[r * 3 + 2] * bf[2 * 3 + c]);
        }
    }
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 矩阵 x 向量（transform）                                            */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3h_mul_vec(VM* vm, int32 param_count)
{
    uint16* m = NULL;
    uint16* v = NULL;
    uint16* o = NULL;
    float32 mf[MATHVM_MAT3_HALF_COUNT];
    float32 x = 0.0f;
    float32 y = 0.0f;
    float32 z = 0.0f;
    int32 i = 0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_vec3h_pop(vm, &o) ||                      /* out：栈顶，Float16_3 */
        !mathvm_vec3h_pop(vm, &v) ||                      /* v：Float16_3 */
        !mathvm_mat3h_pop(vm, &m))                        /* m：Float16_3x3 */
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT3_HALF_COUNT; i++)
    {
        mf[i] = mathvm_half_to_float(m[i]);
    }
    x = mathvm_half_to_float(v[0]);
    y = mathvm_half_to_float(v[1]);
    z = mathvm_half_to_float(v[2]);

    o[0] = mathvm_float_to_half(mf[0] * x + mf[1] * y + mf[2] * z);
    o[1] = mathvm_float_to_half(mf[3] * x + mf[4] * y + mf[5] * z);
    o[2] = mathvm_float_to_half(mf[6] * x + mf[7] * y + mf[8] * z);
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 转置（纯位模式搬移，不经 half 转换）                                 */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3h_transpose(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    uint16* o = NULL;
    int32 r = 0;
    int32 c = 0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3h_pop(vm, &o) ||
        !mathvm_mat3h_pop(vm, &a))
    {
        return FALSE;
    }

    for (r = 0; r < 3; r++)
    {
        for (c = 0; c < 3; c++)
        {
            o[r * 3 + c] = a[c * 3 + r];
        }
    }
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 行列式（f32 中转计算，压回 Float32 栈槽）                            */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3h_determinant(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    float32 af[MATHVM_MAT3_HALF_COUNT];
    float32 det = 0.0f;
    int32 i = 0;

    if (param_count != 1)
    {
        return FALSE;
    }
    if (!mathvm_mat3h_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT3_HALF_COUNT; i++)
    {
        af[i] = mathvm_half_to_float(a[i]);
    }

    det = af[0] * (af[4] * af[8] - af[5] * af[7])
        - af[1] * (af[3] * af[8] - af[5] * af[6])
        + af[2] * (af[3] * af[7] - af[4] * af[6]);

    return slvm_push_f32(vm, det) ? TRUE : FALSE;
}

/* ------------------------------------------------------------------ */
/* 逆矩阵（det == 0 时 out 清零，与 SL 版返回零矩阵一致）               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3h_inverse(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    uint16* o = NULL;
    float32 af[MATHVM_MAT3_HALF_COUNT];
    float32 det = 0.0f;
    float32 inv = 0.0f;
    int32 i = 0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3h_pop(vm, &o) ||
        !mathvm_mat3h_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT3_HALF_COUNT; i++)
    {
        af[i] = mathvm_half_to_float(a[i]);
    }

    det = af[0] * (af[4] * af[8] - af[5] * af[7])
        - af[1] * (af[3] * af[8] - af[5] * af[6])
        + af[2] * (af[3] * af[7] - af[4] * af[6]);

    if (det == 0.0f)
    {
        memset(o, 0, MATHVM_MAT3H_BYTE_SIZE);
        return TRUE;
    }
    inv = 1.0f / det;

    o[0] = mathvm_float_to_half((af[4] * af[8] - af[5] * af[7]) * inv);
    o[1] = mathvm_float_to_half((af[2] * af[7] - af[1] * af[8]) * inv);
    o[2] = mathvm_float_to_half((af[1] * af[5] - af[2] * af[4]) * inv);
    o[3] = mathvm_float_to_half((af[5] * af[6] - af[3] * af[8]) * inv);
    o[4] = mathvm_float_to_half((af[0] * af[8] - af[2] * af[6]) * inv);
    o[5] = mathvm_float_to_half((af[2] * af[3] - af[0] * af[5]) * inv);
    o[6] = mathvm_float_to_half((af[3] * af[7] - af[4] * af[6]) * inv);
    o[7] = mathvm_float_to_half((af[1] * af[6] - af[0] * af[7]) * inv);
    o[8] = mathvm_float_to_half((af[0] * af[4] - af[1] * af[3]) * inv);
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 矩阵加                                                               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3h_add(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    uint16* b = NULL;
    uint16* o = NULL;
    int32 i = 0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat3h_pop(vm, &o) ||
        !mathvm_mat3h_pop(vm, &b) ||
        !mathvm_mat3h_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT3_HALF_COUNT; i++)
    {
        o[i] = mathvm_float_to_half(mathvm_half_to_float(a[i])
                                  + mathvm_half_to_float(b[i]));
    }
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 逐元素相等（返回 bool；经 f32 数值比较，正确处理 +-0）               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3h_eq(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    uint16* b = NULL;
    int32 i = 0;
    int32 eq = TRUE;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3h_pop(vm, &b) ||
        !mathvm_mat3h_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT3_HALF_COUNT; i++)
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

SLVM_FUNC_EXPORT int32 mathvm_mat3h_copy(VM* vm, int32 param_count)
{
    uint16* a = NULL;
    uint16* o = NULL;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3h_pop(vm, &o) ||
        !mathvm_mat3h_pop(vm, &a))
    {
        return FALSE;
    }

    memcpy(o, a, MATHVM_MAT3H_BYTE_SIZE);
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 常量矩阵 / 工厂                                                      */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3h_identity(VM* vm, int32 param_count)
{
    uint16* o = NULL;

    if (param_count != 1)
    {
        return FALSE;
    }
    if (!mathvm_mat3h_pop(vm, &o))
    {
        return FALSE;
    }

    memset(o, 0, MATHVM_MAT3H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(1.0f);
    o[4] = mathvm_float_to_half(1.0f);
    o[8] = mathvm_float_to_half(1.0f);
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3h_rotation_x(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    float32 radians = 0.0f;
    float32 c = 0.0f;
    float32 s = 0.0f;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3h_pop(vm, &o) ||      /* out：栈顶 */
        !slvm_pop_f32(vm, &radians))
    {
        return FALSE;
    }

    c = cosf(radians);
    s = sinf(radians);
    memset(o, 0, MATHVM_MAT3H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(1.0f);
    o[4] = mathvm_float_to_half(c);
    o[5] = mathvm_float_to_half(-s);
    o[7] = mathvm_float_to_half(s);
    o[8] = mathvm_float_to_half(c);
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3h_rotation_y(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    float32 radians = 0.0f;
    float32 c = 0.0f;
    float32 s = 0.0f;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3h_pop(vm, &o) ||
        !slvm_pop_f32(vm, &radians))
    {
        return FALSE;
    }

    c = cosf(radians);
    s = sinf(radians);
    memset(o, 0, MATHVM_MAT3H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(c);
    o[2] = mathvm_float_to_half(s);
    o[4] = mathvm_float_to_half(1.0f);
    o[6] = mathvm_float_to_half(-s);
    o[8] = mathvm_float_to_half(c);
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3h_rotation_z(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    float32 radians = 0.0f;
    float32 c = 0.0f;
    float32 s = 0.0f;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3h_pop(vm, &o) ||
        !slvm_pop_f32(vm, &radians))
    {
        return FALSE;
    }

    c = cosf(radians);
    s = sinf(radians);
    memset(o, 0, MATHVM_MAT3H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(c);
    o[1] = mathvm_float_to_half(-s);
    o[4] = mathvm_float_to_half(c);
    o[3] = mathvm_float_to_half(s);
    o[8] = mathvm_float_to_half(1.0f);
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3h_scale(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    float32 sy = 0.0f;
    float32 sx = 0.0f;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat3h_pop(vm, &o) ||      /* out：栈顶 */
        !slvm_pop_f32(vm, &sy) ||
        !slvm_pop_f32(vm, &sx))
    {
        return FALSE;
    }

    memset(o, 0, MATHVM_MAT3H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(sx);
    o[4] = mathvm_float_to_half(sy);
    o[8] = mathvm_float_to_half(1.0f);
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3h_translation(VM* vm, int32 param_count)
{
    uint16* o = NULL;
    float32 ty = 0.0f;
    float32 tx = 0.0f;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat3h_pop(vm, &o) ||
        !slvm_pop_f32(vm, &ty) ||
        !slvm_pop_f32(vm, &tx))
    {
        return FALSE;
    }

    memset(o, 0, MATHVM_MAT3H_BYTE_SIZE);
    o[0] = mathvm_float_to_half(1.0f);
    o[4] = mathvm_float_to_half(1.0f);
    o[8] = mathvm_float_to_half(1.0f);
    o[2] = mathvm_float_to_half(tx);   /* m02：行主序，平移在第 3 列 */
    o[5] = mathvm_float_to_half(ty);   /* m12 */
    return TRUE;
}
