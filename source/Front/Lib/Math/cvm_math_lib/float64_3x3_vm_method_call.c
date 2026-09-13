/*
 * float64_3x3_vm_method_call.c —— Float64_3x3 的 systemCall 原生实现。
 *
 * 与 float32_3x3_vm_method_call.c 同模式：jsonc 声明
 *
 *     { "name": "SystemMathMat3dMul", "returnType": "void",
 *       "params": [ "object", "object", "object" ],
 *       "isVariadic": false,
 *       "cvmFunction": "math_lib.dll!mathvm_mat3d_mul" }
 *
 * 由 cvm 的 OpCode_CallSystemMethod 直接派发（函数签名与内部 VMSystemFunc
 * 一致：int32 f(VM* vm, int32 param_count)），Float64_3x3.sl 侧以
 * `SystemMathMat3dMul( this, b, r )` 调用。同一个 math_lib.dll 既供 FFI
 * （dllImports 别名）也供 vmDll（systemCall 派发）使用。
 *
 * 参数约定：最后一个参数在栈顶（IRCall.ParseSystemCall 按参数顺序压栈），
 * C 侧按逆序 pop；返回值由实现自己压回（determinant -> Float64，
 * eq -> bool，其余写 out 对象返回 void）。
 *
 * 布局：Float64_3x3 的 9 个 Float64 字段按声明顺序紧凑排布，
 * member_data[0..72) 即 double m[9]（行主序，m[row*3+col]，
 * m00..m22 对应索引 0..8）；Float64_3 的 x/y/z 即 member_data[0..24)。
 */

#include <math.h>

#include "math_vm_object_bridge.h"

#define MATHVM_MAT3_DOUBLE_COUNT 9
#define MATHVM_MAT3_BYTE_SIZE    72  /* 9 * sizeof(double) */
#define MATHVM_VEC3_BYTE_SIZE    24  /* 3 * sizeof(double) */

/* ------------------------------------------------------------------ */
/* 内部帮助                                                            */
/* ------------------------------------------------------------------ */

/* 弹出一个矩阵对象并校验（member_data_size == 72）。 */
static int32 mathvm_mat3d_pop(VM* vm, double** out_mat)
{
    void* obj = NULL;

    if (!mathvm_pop_object(vm, &obj))
    {
        return FALSE;
    }
    if (!mathvm_obj_is_valid(obj, MATHVM_MAT3_BYTE_SIZE))
    {
        return FALSE;
    }
    *out_mat = (double*)MATHVM_OBJ_MEMBER_DATA(obj);
    return TRUE;
}

/* 弹出一个向量对象并校验（member_data_size == 24）。 */
static int32 mathvm_vec3d_pop(VM* vm, double** out_vec)
{
    void* obj = NULL;

    if (!mathvm_pop_object(vm, &obj))
    {
        return FALSE;
    }
    if (!mathvm_obj_is_valid(obj, MATHVM_VEC3_BYTE_SIZE))
    {
        return FALSE;
    }
    *out_vec = (double*)MATHVM_OBJ_MEMBER_DATA(obj);
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 矩阵乘                                                              */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3d_mul(VM* vm, int32 param_count)
{
    double* a = NULL;
    double* b = NULL;
    double* o = NULL;
    int32 r = 0;
    int32 c = 0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat3d_pop(vm, &o) || /* out：栈顶 */
        !mathvm_mat3d_pop(vm, &b) ||
        !mathvm_mat3d_pop(vm, &a))
    {
        return FALSE;
    }

    for (r = 0; r < 3; r++)
    {
        for (c = 0; c < 3; c++)
        {
            o[r * 3 + c] = a[r * 3 + 0] * b[0 * 3 + c]
                         + a[r * 3 + 1] * b[1 * 3 + c]
                         + a[r * 3 + 2] * b[2 * 3 + c];
        }
    }
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 矩阵 x 向量（transform）                                            */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3d_mul_vec(VM* vm, int32 param_count)
{
    double* m = NULL;
    double* v = NULL;
    double* o = NULL;
    double x = 0.0;
    double y = 0.0;
    double z = 0.0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_vec3d_pop(vm, &o) ||                      /* out：栈顶，Float64_3 */
        !mathvm_vec3d_pop(vm, &v) ||                      /* v：Float64_3 */
        !mathvm_mat3d_pop(vm, &m))                        /* m：Float64_3x3 */
    {
        return FALSE;
    }

    x = v[0];
    y = v[1];
    z = v[2];
    o[0] = m[0] * x + m[1] * y + m[2] * z;
    o[1] = m[3] * x + m[4] * y + m[5] * z;
    o[2] = m[6] * x + m[7] * y + m[8] * z;
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 转置                                                                 */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3d_transpose(VM* vm, int32 param_count)
{
    double* a = NULL;
    double* o = NULL;
    int32 r = 0;
    int32 c = 0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3d_pop(vm, &o) ||
        !mathvm_mat3d_pop(vm, &a))
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
/* 行列式                                                               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3d_determinant(VM* vm, int32 param_count)
{
    double* a = NULL;
    double det = 0.0;

    if (param_count != 1)
    {
        return FALSE;
    }
    if (!mathvm_mat3d_pop(vm, &a))
    {
        return FALSE;
    }

    det = a[0] * (a[4] * a[8] - a[5] * a[7])
        - a[1] * (a[3] * a[8] - a[5] * a[6])
        + a[2] * (a[3] * a[7] - a[4] * a[6]);

    return slvm_push_f64(vm, det) ? TRUE : FALSE;
}

/* ------------------------------------------------------------------ */
/* 逆矩阵（det == 0 时 out 清零，与 SL 版返回零矩阵一致）               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3d_inverse(VM* vm, int32 param_count)
{
    double* a = NULL;
    double* o = NULL;
    double det = 0.0;
    double inv = 0.0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3d_pop(vm, &o) ||
        !mathvm_mat3d_pop(vm, &a))
    {
        return FALSE;
    }

    det = a[0] * (a[4] * a[8] - a[5] * a[7])
        - a[1] * (a[3] * a[8] - a[5] * a[6])
        + a[2] * (a[3] * a[7] - a[4] * a[6]);

    if (det == 0.0)
    {
        int32 i = 0;
        for (i = 0; i < MATHVM_MAT3_DOUBLE_COUNT; i++)
        {
            o[i] = 0.0;
        }
        return TRUE;
    }
    inv = 1.0 / det;

    o[0] = (a[4] * a[8] - a[5] * a[7]) * inv;
    o[1] = (a[2] * a[7] - a[1] * a[8]) * inv;
    o[2] = (a[1] * a[5] - a[2] * a[4]) * inv;
    o[3] = (a[5] * a[6] - a[3] * a[8]) * inv;
    o[4] = (a[0] * a[8] - a[2] * a[6]) * inv;
    o[5] = (a[2] * a[3] - a[0] * a[5]) * inv;
    o[6] = (a[3] * a[7] - a[4] * a[6]) * inv;
    o[7] = (a[1] * a[6] - a[0] * a[7]) * inv;
    o[8] = (a[0] * a[4] - a[1] * a[3]) * inv;
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 矩阵加                                                               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3d_add(VM* vm, int32 param_count)
{
    double* a = NULL;
    double* b = NULL;
    double* o = NULL;
    int32 i = 0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat3d_pop(vm, &o) ||
        !mathvm_mat3d_pop(vm, &b) ||
        !mathvm_mat3d_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT3_DOUBLE_COUNT; i++)
    {
        o[i] = a[i] + b[i];
    }
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 逐元素相等（返回 bool）                                              */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3d_eq(VM* vm, int32 param_count)
{
    double* a = NULL;
    double* b = NULL;
    int32 i = 0;
    int32 eq = TRUE;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3d_pop(vm, &b) ||
        !mathvm_mat3d_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT3_DOUBLE_COUNT; i++)
    {
        if (a[i] != b[i])
        {
            eq = FALSE;
            break;
        }
    }
    return slvm_push_bool(vm, eq) ? TRUE : FALSE;
}

/* ------------------------------------------------------------------ */
/* 拷贝                                                                 */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3d_copy(VM* vm, int32 param_count)
{
    double* a = NULL;
    double* o = NULL;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3d_pop(vm, &o) ||
        !mathvm_mat3d_pop(vm, &a))
    {
        return FALSE;
    }

    memcpy(o, a, MATHVM_MAT3_BYTE_SIZE);
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 常量矩阵 / 工厂                                                      */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3d_identity(VM* vm, int32 param_count)
{
    double* o = NULL;

    if (param_count != 1)
    {
        return FALSE;
    }
    if (!mathvm_mat3d_pop(vm, &o))
    {
        return FALSE;
    }

    o[0] = 1.0; o[1] = 0.0; o[2] = 0.0;
    o[3] = 0.0; o[4] = 1.0; o[5] = 0.0;
    o[6] = 0.0; o[7] = 0.0; o[8] = 1.0;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3d_rotation_x(VM* vm, int32 param_count)
{
    double* o = NULL;
    float64 radians = 0.0;
    double c = 0.0;
    double s = 0.0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3d_pop(vm, &o) ||      /* out：栈顶 */
        !slvm_pop_f64(vm, &radians))
    {
        return FALSE;
    }

    c = cos(radians);
    s = sin(radians);
    o[0] = 1.0; o[1] = 0.0; o[2] = 0.0;
    o[3] = 0.0; o[4] = c;     o[5] = -s;
    o[6] = 0.0; o[7] = s;     o[8] = c;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3d_rotation_y(VM* vm, int32 param_count)
{
    double* o = NULL;
    float64 radians = 0.0;
    double c = 0.0;
    double s = 0.0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3d_pop(vm, &o) ||
        !slvm_pop_f64(vm, &radians))
    {
        return FALSE;
    }

    c = cos(radians);
    s = sin(radians);
    o[0] = c;     o[1] = 0.0; o[2] = s;
    o[3] = 0.0; o[4] = 1.0; o[5] = 0.0;
    o[6] = -s;    o[7] = 0.0; o[8] = c;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3d_rotation_z(VM* vm, int32 param_count)
{
    double* o = NULL;
    float64 radians = 0.0;
    double c = 0.0;
    double s = 0.0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3d_pop(vm, &o) ||
        !slvm_pop_f64(vm, &radians))
    {
        return FALSE;
    }

    c = cos(radians);
    s = sin(radians);
    o[0] = c;     o[1] = -s;    o[2] = 0.0;
    o[3] = s;     o[4] = c;     o[5] = 0.0;
    o[6] = 0.0; o[7] = 0.0; o[8] = 1.0;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3d_scale(VM* vm, int32 param_count)
{
    double* o = NULL;
    float64 sy = 0.0;
    float64 sx = 0.0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat3d_pop(vm, &o) ||      /* out：栈顶 */
        !slvm_pop_f64(vm, &sy) ||
        !slvm_pop_f64(vm, &sx))
    {
        return FALSE;
    }

    o[0] = sx;    o[1] = 0.0; o[2] = 0.0;
    o[3] = 0.0; o[4] = sy;    o[5] = 0.0;
    o[6] = 0.0; o[7] = 0.0; o[8] = 1.0;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3d_translation(VM* vm, int32 param_count)
{
    double* o = NULL;
    float64 ty = 0.0;
    float64 tx = 0.0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat3d_pop(vm, &o) ||
        !slvm_pop_f64(vm, &ty) ||
        !slvm_pop_f64(vm, &tx))
    {
        return FALSE;
    }

    o[0] = 1.0; o[1] = 0.0; o[2] = tx;   /* m02：行主序，平移在第 3 列 */
    o[3] = 0.0; o[4] = 1.0; o[5] = ty;   /* m12 */
    o[6] = 0.0; o[7] = 0.0; o[8] = 1.0;
    return TRUE;
}
