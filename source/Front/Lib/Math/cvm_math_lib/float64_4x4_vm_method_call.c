/*
 * float64_4x4_vm_method_call.c —— Float64_4x4 的 systemCall 原生实现。
 *
 * 与 float32_4x4_vm_method_call.c 同模式：jsonc 声明
 *
 *     { "name": "SystemMathMat4dMul", "returnType": "void",
 *       "params": [ "object", "object", "object" ],
 *       "isVariadic": false,
 *       "cvmFunction": "math_lib.dll!mathvm_mat4d_mul" }
 *
 * 由 cvm 的 OpCode_CallSystemMethod 直接派发（签名 int32 f(VM* vm,
 * int32 param_count)），Float64_4x4.sl 侧以 `SystemMathMat4dMul( this, b, r )`
 * 调用。同一个 math_lib.dll 既供 FFI 也供 vmDll 使用。
 *
 * 参数约定：最后一个参数在栈顶（IRCall.ParseSystemCall 按参数顺序压栈），
 * C 侧按逆序 pop；返回值由实现自己压回（determinant -> Float64，
 * eq -> bool，其余写 out 对象返回 void）。
 *
 * 布局：Float64_4x4 的 16 个 Float64 字段按声明顺序紧凑排布，
 * member_data[0..128) 即 double m[16]（行主序，m[row*4+col]，
 * m00..m33 对应索引 0..15）；Float64_3 的 x/y/z 即 member_data[0..24)。
 * 弹栈校验必须匹配实际对象类型（mat4=128B / vec3=24B）。
 */

#include <math.h>

#include "math_vm_object_bridge.h"

#define MATHVM_MAT4_DOUBLE_COUNT 16
#define MATHVM_MAT4_BYTE_SIZE    128 /* 16 * sizeof(double) */
#define MATHVM_VEC3_BYTE_SIZE    24  /* 3 * sizeof(double) */

/* ------------------------------------------------------------------ */
/* 内部帮助                                                            */
/* ------------------------------------------------------------------ */

/* 弹出一个矩阵对象并校验（member_data_size == 128）。 */
static int32 mathvm_mat4d_pop(VM* vm, double** out_mat)
{
    void* obj = NULL;

    if (!mathvm_pop_object(vm, &obj))
    {
        return FALSE;
    }
    if (!mathvm_obj_is_valid(obj, MATHVM_MAT4_BYTE_SIZE))
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

/* 三维向量点积 / 叉积 / 归一化（lookAt / rotationAxis 内部使用）。 */
static double mathvm_vec3d_dot(const double* a, const double* b)
{
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
}

static void mathvm_vec3d_cross(const double* a, const double* b, double* o)
{
    o[0] = a[1] * b[2] - a[2] * b[1];
    o[1] = a[2] * b[0] - a[0] * b[2];
    o[2] = a[0] * b[1] - a[1] * b[0];
}

static void mathvm_vec3d_normalize(const double* v, double* o)
{
    double len = sqrt(mathvm_vec3d_dot(v, v));

    if (len > 0.0)
    {
        double inv = 1.0 / len;
        o[0] = v[0] * inv;
        o[1] = v[1] * inv;
        o[2] = v[2] * inv;
    }
    else
    {
        o[0] = 0.0;
        o[1] = 0.0;
        o[2] = 0.0;
    }
}

/* ------------------------------------------------------------------ */
/* 矩阵乘                                                              */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4d_mul(VM* vm, int32 param_count)
{
    double* a = NULL;
    double* b = NULL;
    double* o = NULL;
    int32 r = 0;
    int32 c = 0;
    int32 k = 0;
    double sum = 0.0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) || /* out：栈顶 */
        !mathvm_mat4d_pop(vm, &b) ||
        !mathvm_mat4d_pop(vm, &a))
    {
        return FALSE;
    }

    for (r = 0; r < 4; r++)
    {
        for (c = 0; c < 4; c++)
        {
            sum = 0.0;
            for (k = 0; k < 4; k++)
            {
                sum += a[r * 4 + k] * b[k * 4 + c];
            }
            o[r * 4 + c] = sum;
        }
    }
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 变换点（w 补 1，带平移）/ 变换方向（w 补 0，忽略平移）               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4d_transform_point(VM* vm, int32 param_count)
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
    if (!mathvm_vec3d_pop(vm, &o) ||                  /* out：栈顶，Float64_3 */
        !mathvm_vec3d_pop(vm, &v) ||                  /* v：Float64_3 */
        !mathvm_mat4d_pop(vm, &m))                    /* m：Float64_4x4 */
    {
        return FALSE;
    }

    x = v[0];
    y = v[1];
    z = v[2];
    o[0] = m[0] * x + m[1] * y + m[2] * z + m[3];
    o[1] = m[4] * x + m[5] * y + m[6] * z + m[7];
    o[2] = m[8] * x + m[9] * y + m[10] * z + m[11];
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat4d_transform_direction(VM* vm, int32 param_count)
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
    if (!mathvm_vec3d_pop(vm, &o) ||
        !mathvm_vec3d_pop(vm, &v) ||
        !mathvm_mat4d_pop(vm, &m))
    {
        return FALSE;
    }

    x = v[0];
    y = v[1];
    z = v[2];
    o[0] = m[0] * x + m[1] * y + m[2] * z;
    o[1] = m[4] * x + m[5] * y + m[6] * z;
    o[2] = m[8] * x + m[9] * y + m[10] * z;
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 转置                                                                 */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4d_transpose(VM* vm, int32 param_count)
{
    double* a = NULL;
    double* o = NULL;
    int32 r = 0;
    int32 c = 0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) ||
        !mathvm_mat4d_pop(vm, &a))
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
/* 行列式（与 SL 版伴随矩阵法一致）                                     */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4d_determinant(VM* vm, int32 param_count)
{
    double* a = NULL;
    double b00 = 0.0;
    double b01 = 0.0;
    double b02 = 0.0;
    double b03 = 0.0;
    double b04 = 0.0;
    double b05 = 0.0;
    double b06 = 0.0;
    double b07 = 0.0;
    double b08 = 0.0;
    double b09 = 0.0;
    double b10 = 0.0;
    double b11 = 0.0;

    if (param_count != 1)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &a))
    {
        return FALSE;
    }

    b00 = a[0] * a[5] - a[1] * a[4];
    b01 = a[0] * a[6] - a[2] * a[4];
    b02 = a[0] * a[7] - a[3] * a[4];
    b03 = a[1] * a[6] - a[2] * a[5];
    b04 = a[1] * a[7] - a[3] * a[5];
    b05 = a[2] * a[7] - a[3] * a[6];
    b06 = a[8] * a[13] - a[9] * a[12];
    b07 = a[8] * a[14] - a[10] * a[12];
    b08 = a[8] * a[15] - a[11] * a[12];
    b09 = a[9] * a[14] - a[10] * a[13];
    b10 = a[9] * a[15] - a[11] * a[13];
    b11 = a[10] * a[15] - a[11] * a[14];

    return slvm_push_f64(vm, b00 * b11 - b01 * b10 + b02 * b09
                               + b03 * b08 - b04 * b07 + b05 * b06)
        ? TRUE : FALSE;
}

/* ------------------------------------------------------------------ */
/* 逆矩阵（det == 0 时 out 清零，与 SL 版返回零矩阵一致）               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4d_inverse(VM* vm, int32 param_count)
{
    double* a = NULL;
    double* o = NULL;
    double b00 = 0.0;
    double b01 = 0.0;
    double b02 = 0.0;
    double b03 = 0.0;
    double b04 = 0.0;
    double b05 = 0.0;
    double b06 = 0.0;
    double b07 = 0.0;
    double b08 = 0.0;
    double b09 = 0.0;
    double b10 = 0.0;
    double b11 = 0.0;
    double det = 0.0;
    double inv = 0.0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) ||
        !mathvm_mat4d_pop(vm, &a))
    {
        return FALSE;
    }

    b00 = a[0] * a[5] - a[1] * a[4];
    b01 = a[0] * a[6] - a[2] * a[4];
    b02 = a[0] * a[7] - a[3] * a[4];
    b03 = a[1] * a[6] - a[2] * a[5];
    b04 = a[1] * a[7] - a[3] * a[5];
    b05 = a[2] * a[7] - a[3] * a[6];
    b06 = a[8] * a[13] - a[9] * a[12];
    b07 = a[8] * a[14] - a[10] * a[12];
    b08 = a[8] * a[15] - a[11] * a[12];
    b09 = a[9] * a[14] - a[10] * a[13];
    b10 = a[9] * a[15] - a[11] * a[13];
    b11 = a[10] * a[15] - a[11] * a[14];

    det = b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06;

    if (det == 0.0)
    {
        int32 i = 0;
        for (i = 0; i < MATHVM_MAT4_DOUBLE_COUNT; i++)
        {
            o[i] = 0.0;
        }
        return TRUE;
    }
    inv = 1.0 / det;

    o[0]  = (a[5] * b11 - a[6] * b10 + a[7] * b09) * inv;
    o[1]  = (a[2] * b10 - a[1] * b11 - a[3] * b09) * inv;
    o[2]  = (a[13] * b05 - a[14] * b04 + a[15] * b03) * inv;
    o[3]  = (a[10] * b04 - a[11] * b05 - a[9] * b03) * inv;
    o[4]  = (a[6] * b08 - a[4] * b11 - a[7] * b07) * inv;
    o[5]  = (a[0] * b11 - a[2] * b08 + a[3] * b07) * inv;
    o[6]  = (a[14] * b02 - a[12] * b05 - a[15] * b01) * inv;
    o[7]  = (a[8] * b05 - a[10] * b02 + a[11] * b01) * inv;
    o[8]  = (a[4] * b10 - a[5] * b08 + a[7] * b06) * inv;
    o[9]  = (a[1] * b08 - a[0] * b10 - a[3] * b06) * inv;
    o[10] = (a[12] * b04 - a[13] * b02 + a[15] * b00) * inv;
    o[11] = (a[9] * b02 - a[8] * b04 - a[11] * b00) * inv;
    o[12] = (a[5] * b07 - a[4] * b09 - a[6] * b06) * inv;
    o[13] = (a[0] * b09 - a[1] * b07 + a[2] * b06) * inv;
    o[14] = (a[13] * b01 - a[12] * b03 - a[14] * b00) * inv;
    o[15] = (a[8] * b03 - a[9] * b01 + a[10] * b00) * inv;
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 矩阵加                                                               */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4d_add(VM* vm, int32 param_count)
{
    double* a = NULL;
    double* b = NULL;
    double* o = NULL;
    int32 i = 0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) ||
        !mathvm_mat4d_pop(vm, &b) ||
        !mathvm_mat4d_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT4_DOUBLE_COUNT; i++)
    {
        o[i] = a[i] + b[i];
    }
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 逐元素相等（返回 bool）                                              */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4d_eq(VM* vm, int32 param_count)
{
    double* a = NULL;
    double* b = NULL;
    int32 i = 0;
    int32 eq = TRUE;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &b) ||
        !mathvm_mat4d_pop(vm, &a))
    {
        return FALSE;
    }

    for (i = 0; i < MATHVM_MAT4_DOUBLE_COUNT; i++)
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

SLVM_FUNC_EXPORT int32 mathvm_mat4d_copy(VM* vm, int32 param_count)
{
    double* a = NULL;
    double* o = NULL;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) ||
        !mathvm_mat4d_pop(vm, &a))
    {
        return FALSE;
    }

    memcpy(o, a, MATHVM_MAT4_BYTE_SIZE);
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 常量矩阵 / 工厂                                                      */
/* ------------------------------------------------------------------ */

SLVM_FUNC_EXPORT int32 mathvm_mat4d_identity(VM* vm, int32 param_count)
{
    double* o = NULL;

    if (param_count != 1)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o))
    {
        return FALSE;
    }

    memset(o, 0, MATHVM_MAT4_BYTE_SIZE);
    o[0] = 1.0;
    o[5] = 1.0;
    o[10] = 1.0;
    o[15] = 1.0;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat4d_translation(VM* vm, int32 param_count)
{
    double* o = NULL;
    float64 tz = 0.0;
    float64 ty = 0.0;
    float64 tx = 0.0;

    if (param_count != 4)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) ||      /* out：栈顶 */
        !slvm_pop_f64(vm, &tz) ||
        !slvm_pop_f64(vm, &ty) ||
        !slvm_pop_f64(vm, &tx))
    {
        return FALSE;
    }

    memset(o, 0, MATHVM_MAT4_BYTE_SIZE);
    o[0] = 1.0;
    o[5] = 1.0;
    o[10] = 1.0;
    o[3] = tx;   /* m03：行主序，平移在第 4 列 */
    o[7] = ty;   /* m13 */
    o[11] = tz;  /* m23 */
    o[15] = 1.0;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat4d_scale(VM* vm, int32 param_count)
{
    double* o = NULL;
    float64 sz = 0.0;
    float64 sy = 0.0;
    float64 sx = 0.0;

    if (param_count != 4)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) ||
        !slvm_pop_f64(vm, &sz) ||
        !slvm_pop_f64(vm, &sy) ||
        !slvm_pop_f64(vm, &sx))
    {
        return FALSE;
    }

    memset(o, 0, MATHVM_MAT4_BYTE_SIZE);
    o[0] = sx;
    o[5] = sy;
    o[10] = sz;
    o[15] = 1.0;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat4d_rotation_x(VM* vm, int32 param_count)
{
    double* o = NULL;
    float64 radians = 0.0;
    double c = 0.0;
    double s = 0.0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) ||
        !slvm_pop_f64(vm, &radians))
    {
        return FALSE;
    }

    c = cos(radians);
    s = sin(radians);
    memset(o, 0, MATHVM_MAT4_BYTE_SIZE);
    o[0] = 1.0;
    o[5] = c;
    o[6] = -s;
    o[9] = s;
    o[10] = c;
    o[15] = 1.0;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat4d_rotation_y(VM* vm, int32 param_count)
{
    double* o = NULL;
    float64 radians = 0.0;
    double c = 0.0;
    double s = 0.0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) ||
        !slvm_pop_f64(vm, &radians))
    {
        return FALSE;
    }

    c = cos(radians);
    s = sin(radians);
    memset(o, 0, MATHVM_MAT4_BYTE_SIZE);
    o[0] = c;
    o[2] = s;
    o[5] = 1.0;
    o[8] = -s;
    o[10] = c;
    o[15] = 1.0;
    return TRUE;
}

SLVM_FUNC_EXPORT int32 mathvm_mat4d_rotation_z(VM* vm, int32 param_count)
{
    double* o = NULL;
    float64 radians = 0.0;
    double c = 0.0;
    double s = 0.0;

    if (param_count != 2)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) ||
        !slvm_pop_f64(vm, &radians))
    {
        return FALSE;
    }

    c = cos(radians);
    s = sin(radians);
    memset(o, 0, MATHVM_MAT4_BYTE_SIZE);
    o[0] = c;
    o[1] = -s;
    o[4] = s;
    o[5] = c;
    o[10] = 1.0;
    o[15] = 1.0;
    return TRUE;
}

/* 绕任意轴旋转（axis 先归一化，与 SL 版一致） */
SLVM_FUNC_EXPORT int32 mathvm_mat4d_rotation_axis(VM* vm, int32 param_count)
{
    double* o = NULL;
    float64 radians = 0.0;
    double* axis = NULL;
    double a[3] = { 0.0, 0.0, 0.0 };
    double x = 0.0;
    double y = 0.0;
    double z = 0.0;
    double c = 0.0;
    double s = 0.0;
    double t = 0.0;

    if (param_count != 3)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) ||              /* out：栈顶，Float64_4x4 */
        !slvm_pop_f64(vm, &radians) ||
        !mathvm_vec3d_pop(vm, &axis))             /* axis：Float64_3 */
    {
        return FALSE;
    }

    mathvm_vec3d_normalize(axis, a);
    x = a[0];
    y = a[1];
    z = a[2];
    c = cos(radians);
    s = sin(radians);
    t = 1.0 - c;

    o[0] = t * x * x + c;
    o[1] = t * x * y - s * z;
    o[2] = t * x * z + s * y;
    o[3] = 0.0;
    o[4] = t * x * y + s * z;
    o[5] = t * y * y + c;
    o[6] = t * y * z - s * x;
    o[7] = 0.0;
    o[8] = t * x * z - s * y;
    o[9] = t * y * z + s * x;
    o[10] = t * z * z + c;
    o[11] = 0.0;
    o[12] = 0.0;
    o[13] = 0.0;
    o[14] = 0.0;
    o[15] = 1.0;
    return TRUE;
}

/* 透视投影（右手系，depth 映射到 [-1,1]） */
SLVM_FUNC_EXPORT int32 mathvm_mat4d_perspective(VM* vm, int32 param_count)
{
    double* o = NULL;
    float64 far_z = 0.0;
    float64 near_z = 0.0;
    float64 aspect = 0.0;
    float64 fov_y = 0.0;
    double f = 0.0;

    if (param_count != 5)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) ||
        !slvm_pop_f64(vm, &far_z) ||
        !slvm_pop_f64(vm, &near_z) ||
        !slvm_pop_f64(vm, &aspect) ||
        !slvm_pop_f64(vm, &fov_y))
    {
        return FALSE;
    }

    f = 1.0 / tan(fov_y * 0.5);
    memset(o, 0, MATHVM_MAT4_BYTE_SIZE);
    o[0] = f / aspect;
    o[5] = f;
    o[10] = (far_z + near_z) / (near_z - far_z);
    o[11] = (2.0 * far_z * near_z) / (near_z - far_z);
    o[14] = -1.0;
    return TRUE;
}

/* 正交投影 */
SLVM_FUNC_EXPORT int32 mathvm_mat4d_ortho(VM* vm, int32 param_count)
{
    double* o = NULL;
    float64 far_z = 0.0;
    float64 near_z = 0.0;
    float64 top = 0.0;
    float64 bottom = 0.0;
    float64 right = 0.0;
    float64 left = 0.0;

    if (param_count != 7)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) ||
        !slvm_pop_f64(vm, &far_z) ||
        !slvm_pop_f64(vm, &near_z) ||
        !slvm_pop_f64(vm, &top) ||
        !slvm_pop_f64(vm, &bottom) ||
        !slvm_pop_f64(vm, &right) ||
        !slvm_pop_f64(vm, &left))
    {
        return FALSE;
    }

    memset(o, 0, MATHVM_MAT4_BYTE_SIZE);
    o[0] = 2.0 / (right - left);
    o[3] = 0.0 - (right + left) / (right - left);
    o[5] = 2.0 / (top - bottom);
    o[7] = 0.0 - (top + bottom) / (top - bottom);
    o[10] = 0.0 - 2.0 / (far_z - near_z);
    o[11] = 0.0 - (far_z + near_z) / (far_z - near_z);
    o[15] = 1.0;
    return TRUE;
}

/* 视图矩阵（右手系 lookAt） */
SLVM_FUNC_EXPORT int32 mathvm_mat4d_look_at(VM* vm, int32 param_count)
{
    double* o = NULL;
    double* up = NULL;
    double* target = NULL;
    double* eye = NULL;
    double z_axis[3] = { 0.0, 0.0, 0.0 };
    double x_axis[3] = { 0.0, 0.0, 0.0 };
    double y_axis[3] = { 0.0, 0.0, 0.0 };
    double tmp[3] = { 0.0, 0.0, 0.0 };

    if (param_count != 4)
    {
        return FALSE;
    }
    if (!mathvm_mat4d_pop(vm, &o) ||              /* out：栈顶，Float64_4x4 */
        !mathvm_vec3d_pop(vm, &up) ||             /* upHint：Float64_3 */
        !mathvm_vec3d_pop(vm, &target) ||         /* target：Float64_3 */
        !mathvm_vec3d_pop(vm, &eye))              /* eye：Float64_3 */
    {
        return FALSE;
    }

    /* zAxis = (eye - target).normalize() */
    tmp[0] = eye[0] - target[0];
    tmp[1] = eye[1] - target[1];
    tmp[2] = eye[2] - target[2];
    mathvm_vec3d_normalize(tmp, z_axis);

    /* xAxis = cross(up, zAxis).normalize() */
    mathvm_vec3d_cross(up, z_axis, tmp);
    mathvm_vec3d_normalize(tmp, x_axis);

    /* yAxis = cross(zAxis, xAxis) */
    mathvm_vec3d_cross(z_axis, x_axis, y_axis);

    o[0] = x_axis[0];
    o[1] = x_axis[1];
    o[2] = x_axis[2];
    o[3] = 0.0 - mathvm_vec3d_dot(x_axis, eye);
    o[4] = y_axis[0];
    o[5] = y_axis[1];
    o[6] = y_axis[2];
    o[7] = 0.0 - mathvm_vec3d_dot(y_axis, eye);
    o[8] = z_axis[0];
    o[9] = z_axis[1];
    o[10] = z_axis[2];
    o[11] = 0.0 - mathvm_vec3d_dot(z_axis, eye);
    o[12] = 0.0;
    o[13] = 0.0;
    o[14] = 0.0;
    o[15] = 1.0;
    return TRUE;
}
