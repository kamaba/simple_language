/*
 * float32_3x3_vm_method_call.c —— Float32_3x3 的 systemCall 原生实现。
 *
 * 与 math_lib.c 的 FFI 普通 C ABI（@DllImport，无 VM 栈依赖）不同，这里是
 * systemCalls / cvmFunction 形式：jsonc 声明
 *
 *     { "name": "SystemMathMat3Mul", "returnType": "void",
 *       "params": [ "object", "object", "object" ],
 *       "isVariadic": false,
 *       "cvmFunction": "math_lib.dll!mathvm_mat3f_mul" }
 *
 * 由 cvm 的 OpCode_CallSystemMethod 直接派发（函数签名与内部 VMSystemFunc
 * 一致：int32 f(VM* vm, int32 param_count)），Float32_3x3.sl 侧以
 * `SystemMathMat3Mul( this, b, r )` 调用。同一个 math_lib.dll 既供 FFI
 * （dllImports 别名）也供 vmDll（systemCall 派发）使用。
 *
 * 参数约定：最后一个参数在栈顶（IRCall.ParseSystemCall 按参数顺序压栈），
 * C 侧按逆序 pop；返回值由实现自己压回（determinant -> Float32，
 * eq -> bool，其余写 out 对象返回 void）。
 *
 * 输出参数模式：外部 DLL 无法分配 VM 对象（需 vm->obj_pool + RuntimeClass），
 * 矩阵结果统一走 out 参数——SL 侧先构造对象再传入，C 侧直接改写其
 * member_data（即字段内存）。
 *
 * 布局：Float32_3x3 的 9 个 Float32 字段按声明顺序紧凑排布，
 * member_data[0..36) 即 float m[9]（行主序，m[row*3+col]，
 * m00..m22 对应索引 0..8）；Float32_3 的 x/y/z 即 member_data[0..12)。
 */

#include <math.h>

#include "math_vm_object_bridge.h"

#define MATHVM_MAT3_FLOAT_COUNT 9
#define MATHVM_MAT3_BYTE_SIZE   36  /* 9 * sizeof(float) */
#define MATHVM_VEC3_BYTE_SIZE   12  /* 3 * sizeof(float) */

/* ------------------------------------------------------------------ */
/* 内部帮助                                                            */
/* ------------------------------------------------------------------ */

/* 弹出一个矩阵对象并校验（member_data_size == 36）。 */
static int32 mathvm_mat3f_pop(VM* vm, float** out_mat)
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
    *out_mat = (float*)MATHVM_OBJ_MEMBER_DATA(obj);
    return TRUE;
}

/* 弹出一个向量对象并校验（member_data_size == 12）。 */
static int32 mathvm_vec3f_pop(VM* vm, float** out_vec)
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
    *out_vec = (float*)MATHVM_OBJ_MEMBER_DATA(obj);
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 矩阵乘                                                              */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3f_mul(VM* vm, int32 param_count)
{
    float* a = NULL;
    float* b = NULL;
    float* o = NULL;
    int32 r = 0;
    int32 c = 0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat3f_pop(vm, &o) || /* out：栈顶 */
        !mathvm_mat3f_pop(vm, &b) ||
        !mathvm_mat3f_pop(vm, &a))
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
/* 矩阵 x 向量                                                         */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3f_mul_vec(VM* vm, int32 param_count)
{
    float* m = NULL;
    float* v = NULL;
    float* o = NULL;
    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_vec3f_pop(vm, &o) ||                      /* out：栈顶，Float32_3 */
        !mathvm_vec3f_pop(vm, &v) ||                      /* v：Float32_3 */
        !mathvm_mat3f_pop(vm, &m))                        /* m：Float32_3x3 */
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

SLVM_FUNC_EXPORT int32 mathvm_mat3f_transpose(VM* vm, int32 param_count)
{
    float* a = NULL;
    float* o = NULL;
    int32 r = 0;
    int32 c = 0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3f_pop(vm, &o) ||
        !mathvm_mat3f_pop(vm, &a))
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

SLVM_FUNC_EXPORT int32 mathvm_mat3f_determinant(VM* vm, int32 param_count)
{
    float* a = NULL;
    float det = 0.0f;

    if (param_count != 1)
    {
        return FALSE;
    }
    if (!mathvm_mat3f_pop(vm, &a))
    {
        return FALSE;
    }

    det = a[0] * (a[4] * a[8] - a[5] * a[7])
        - a[1] * (a[3] * a[8] - a[5] * a[6])
        + a[2] * (a[3] * a[7] - a[4] * a[6]);

    return slvm_push_f32(vm, det) ? TRUE : FALSE;
}

/* ------------------------------------------------------------------ */
/* 逆矩阵（det == 0 时 out 清零，与 SL 版返回零矩阵一致）               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3f_inverse(VM* vm, int32 param_count)
{
    float* a = NULL;
    float* o = NULL;
    float det = 0.0f;
    float inv = 0.0f;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3f_pop(vm, &o) ||
        !mathvm_mat3f_pop(vm, &a))
    {
        return FALSE;
    }

    det = a[0] * (a[4] * a[8] - a[5] * a[7])
        - a[1] * (a[3] * a[8] - a[5] * a[6])
        + a[2] * (a[3] * a[7] - a[4] * a[6]);

    if (det == 0.0f)
    {
        int32 i = 0;
        for (i = 0; i < MATHVM_MAT3_FLOAT_COUNT; i++)
        {
            o[i] = 0.0f;
        }
        return TRUE;
    }
    inv = 1.0f / det;

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

SLVM_FUNC_EXPORT int32 mathvm_mat3f_add(VM* vm, int32 param_count)
{
    float* a = NULL;
    float* b = NULL;
    float* o = NULL;
    int32 i = 0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat3f_pop(vm, &o) ||
        !mathvm_mat3f_pop(vm, &b) ||
        !mathvm_mat3f_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT3_FLOAT_COUNT; i++)
    {
        o[i] = a[i] + b[i];
    }
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 逐元素相等（返回 bool）                                              */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3f_eq(VM* vm, int32 param_count)
{
    float* a = NULL;
    float* b = NULL;
    int32 i = 0;
    int32 eq = TRUE;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3f_pop(vm, &b) ||
        !mathvm_mat3f_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT3_FLOAT_COUNT; i++)
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

SLVM_FUNC_EXPORT int32 mathvm_mat3f_copy(VM* vm, int32 param_count)
{
    float* a = NULL;
    float* o = NULL;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3f_pop(vm, &o) ||
        !mathvm_mat3f_pop(vm, &a))
    {
        return FALSE;
    }

    memcpy(o, a, MATHVM_MAT3_BYTE_SIZE);
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 常量矩阵 / 工厂                                                      */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat3f_identity(VM* vm, int32 param_count)
{
    float* o = NULL;

    if (param_count != 1)
    {
        return FALSE;
    }
    if (!mathvm_mat3f_pop(vm, &o))
    {
        return FALSE;
    }

    o[0] = 1.0f; o[1] = 0.0f; o[2] = 0.0f;
    o[3] = 0.0f; o[4] = 1.0f; o[5] = 0.0f;
    o[6] = 0.0f; o[7] = 0.0f; o[8] = 1.0f;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3f_rotation_x(VM* vm, int32 param_count)
{
    float* o = NULL;
    float32 radians = 0.0f;
    float c = 0.0f;
    float s = 0.0f;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3f_pop(vm, &o) ||      /* out：栈顶 */
        !slvm_pop_f32(vm, &radians))
    {
        return FALSE;
    }

    c = cosf(radians);
    s = sinf(radians);
    o[0] = 1.0f; o[1] = 0.0f; o[2] = 0.0f;
    o[3] = 0.0f; o[4] = c;     o[5] = -s;
    o[6] = 0.0f; o[7] = s;     o[8] = c;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3f_rotation_y(VM* vm, int32 param_count)
{
    float* o = NULL;
    float32 radians = 0.0f;
    float c = 0.0f;
    float s = 0.0f;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3f_pop(vm, &o) ||
        !slvm_pop_f32(vm, &radians))
    {
        return FALSE;
    }

    c = cosf(radians);
    s = sinf(radians);
    o[0] = c;     o[1] = 0.0f; o[2] = s;
    o[3] = 0.0f; o[4] = 1.0f; o[5] = 0.0f;
    o[6] = -s;    o[7] = 0.0f; o[8] = c;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3f_rotation_z(VM* vm, int32 param_count)
{
    float* o = NULL;
    float32 radians = 0.0f;
    float c = 0.0f;
    float s = 0.0f;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat3f_pop(vm, &o) ||
        !slvm_pop_f32(vm, &radians))
    {
        return FALSE;
    }

    c = cosf(radians);
    s = sinf(radians);
    o[0] = c;     o[1] = -s;    o[2] = 0.0f;
    o[3] = s;     o[4] = c;     o[5] = 0.0f;
    o[6] = 0.0f; o[7] = 0.0f; o[8] = 1.0f;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3f_scale(VM* vm, int32 param_count)
{
    float* o = NULL;
    float32 sy = 0.0f;
    float32 sx = 0.0f;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat3f_pop(vm, &o) ||      /* out：栈顶 */
        !slvm_pop_f32(vm, &sy) ||
        !slvm_pop_f32(vm, &sx))
    {
        return FALSE;
    }

    o[0] = sx;    o[1] = 0.0f; o[2] = 0.0f;
    o[3] = 0.0f; o[4] = sy;    o[5] = 0.0f;
    o[6] = 0.0f; o[7] = 0.0f; o[8] = 1.0f;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat3f_translation(VM* vm, int32 param_count)
{
    float* o = NULL;
    float32 ty = 0.0f;
    float32 tx = 0.0f;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat3f_pop(vm, &o) ||
        !slvm_pop_f32(vm, &ty) ||
        !slvm_pop_f32(vm, &tx))
    {
        return FALSE;
    }

    o[0] = 1.0f; o[1] = 0.0f; o[2] = tx;
    o[3] = 0.0f; o[4] = 1.0f; o[5] = ty;
    o[6] = 0.0f; o[7] = 0.0f; o[8] = 1.0f;
    return TRUE;
}
