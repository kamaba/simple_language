#ifndef MATH_VM_OBJECT_BRIDGE_H
#define MATH_VM_OBJECT_BRIDGE_H

/*
 * math_vm_object_bridge.h —— cvm_math_lib 矩阵/向量 VM 对象桥接层。
 *
 * sl_vm_ext_api.h 只提供标量 push/pop；矩阵 systemCall 需要按对象指针读写
 * VM 对象成员，本头文件补齐：
 *   1. VMObject 布局镜像偏移（与 csimple_lang src/vm/vm_object.h 的
 *      struct _VMObject 严格一致，x64）；
 *   2. mathvm_pop_object —— 弹出栈顶对象槽（PTR / NULL）；
 *   3. mathvm_obj_is_valid —— 指针防御检查（地址 / 对齐 / 成员数据 / 尺寸）；
 *   4. mathvm_half_to_float / mathvm_float_to_half —— Float16 位模式转换
 *      （Float16 矩阵用；Float16 在 VM 内以 uint16 位模式存储于成员数据）。
 *
 * VMObject 布局（x64，偏移与 vm_object.h 一致）：
 *   偏移  0 : header（uint64 联合）
 *   偏移  8 : member_data（uint8*）
 *   偏移 16 : member_data_size（uint32）
 *   偏移 24 : member_runtime_objects（指针）
 *   偏移 32 : member_runtime_object_count（int32）
 *   偏移 40 : runtime_type_ref（指针）
 *
 * 成员数据按字段声明顺序紧凑排布、无对齐填充
 * （vm_object_init_member_layout: cursor += field_len）：
 * Float32=4B / Float64=8B / Float16=2B（位模式）/ 引用=8B。
 * Float32_3x3 的 m00..m22 即 member_data[0..36) 的 float[9]。
 */

#include "sl_vm_ext_api.h"

SLVM_EC_BEGIN

/* ------------------------------------------------------------------ */
/* VMObject 布局偏移                                                    */
/* ------------------------------------------------------------------ */
#define MATHVM_OBJ_MEMBER_DATA_OFFSET       8   /* uint8*  member_data */
#define MATHVM_OBJ_MEMBER_DATA_SIZE_OFFSET  16  /* uint32  member_data_size */

#define MATHVM_OBJ_MEMBER_DATA(obj) \
    (*(uint8**)((uint8*)(obj) + MATHVM_OBJ_MEMBER_DATA_OFFSET))
#define MATHVM_OBJ_MEMBER_DATA_SIZE(obj) \
    (*(uint32*)((uint8*)(obj) + MATHVM_OBJ_MEMBER_DATA_SIZE_OFFSET))

/* ------------------------------------------------------------------ */
/* 对象槽弹出                                                          */
/* ------------------------------------------------------------------ */
/* 弹出栈顶对象槽：
 *   - PTR 槽（8 字节）：输出 VMObject*；
 *   - NULL 槽（0 字节）：输出 NULL（视为成功，由调用方按需校验）；
 *   - 其余槽类型（含未标记遗留 4 字节路径）：返回 FALSE。
 * 与 slvm_pop_f64 的防御语义一致（sp / stack_slot_depth 同步递减）。 */
static int32 mathvm_pop_object(VM* vm, void** out_obj)
{
    uint8 kind = 0;

    if (vm == NULL || out_obj == NULL)
    {
        return FALSE;
    }
    *out_obj = NULL;

    if (vm->stack_slot_depth > 0u)
    {
        kind = ((const uint8*)vm->stack_slot_kind)[vm->stack_slot_depth - 1u];
    }
    else
    {
        kind = (uint8)SLVM_STACK_SLOT_INT32; /* 遗留路径：未标记的 4 字节槽 */
    }

    if (kind == (uint8)SLVM_STACK_SLOT_NULL)
    {
        vm->stack_slot_depth--;
        return TRUE; /* *out_obj 已置 NULL */
    }
    if (kind != (uint8)SLVM_STACK_SLOT_PTR)
    {
        return FALSE;
    }

    if ((size_t)(vm->sp - (const uint8*)vm->stack) < sizeof(void*))
    {
        return FALSE;
    }
    vm->sp -= sizeof(void*);
    if (vm->stack_slot_depth > 0u)
    {
        vm->stack_slot_depth--;
    }
    memcpy(out_obj, vm->sp, sizeof(void*));
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* 对象指针防御检查                                                     */
/* ------------------------------------------------------------------ */
/* SL 侧类型系统已保证调用点参数类型正确，这里是运行期兜底：
 *   - 指针非空且地址 >= 0x10000（与 vm_object_is_valid 一致）；
 *   - VMObject 8 字节对齐；
 *   - member_data 非空；
 *   - member_data_size 与预期一致（Float32_3x3=36 / Float32_3=12 等）。 */
static int32 mathvm_obj_is_valid(void* obj, uint32 expect_member_size)
{
    uint8* member_data = NULL;
    uint32 member_data_size = 0;

    if (obj == NULL || (uintptr_t)obj < (uintptr_t)0x10000u)
    {
        return FALSE;
    }
    if (((uintptr_t)obj & 7u) != 0u)
    {
        return FALSE;
    }

    member_data = MATHVM_OBJ_MEMBER_DATA(obj);
    if (member_data == NULL)
    {
        return FALSE;
    }
    member_data_size = MATHVM_OBJ_MEMBER_DATA_SIZE(obj);
    if (member_data_size != expect_member_size)
    {
        return FALSE;
    }
    return TRUE;
}

/* ------------------------------------------------------------------ */
/* Float16 <-> Float32 位模式转换                                       */
/* ------------------------------------------------------------------ */
/* VM 中 Float16 以 IEEE 754 binary16 位模式存储于成员数据（uint16），
 * 计算时需转 float32。round-to-nearest-even。 */
static float32 mathvm_half_to_float(uint16 h)
{
    uint32 sign = 0u;
    uint32 exp = 0u;
    uint32 frac = 0u;
    uint32 bits = 0u;
    float32 out = 0.0f;

    sign = (uint32)((h >> 15) & 1u);
    exp = (uint32)((h >> 10) & 0x1Fu);
    frac = (uint32)(h & 0x3FFu);

    if (exp == 0u)
    {
        if (frac == 0u)
        {
            bits = sign << 31; /* +-0 */
        }
        else
        {
            /* 非规格化：half -> float32 规格化 */
            exp = 127u - 15u + 1u;
            while ((frac & 0x400u) == 0u)
            {
                frac <<= 1;
                exp--;
            }
            frac &= 0x3FFu;
            bits = (sign << 31) | (exp << 23) | (frac << 13);
        }
    }
    else if (exp == 0x1Fu)
    {
        /* Inf / NaN */
        bits = (sign << 31) | (0xFFu << 23) | (frac << 13);
    }
    else
    {
        bits = (sign << 31) | ((exp - 15u + 127u) << 23) | (frac << 13);
    }

    memcpy(&out, &bits, sizeof(out));
    return out;
}

static uint16 mathvm_float_to_half(float32 f)
{
    uint32 x = 0u;
    uint32 sign = 0u;
    uint32 exp = 0u;
    uint32 frac = 0u;
    int32 e = 0;

    memcpy(&x, &f, sizeof(x));

    sign = (x >> 31) & 1u;
    exp = (x >> 23) & 0xFFu;
    frac = x & 0x007FFFFFu;

    if (exp == 0xFFu)
    {
        /* Inf / NaN（NaN 保留粘滞位） */
        return (uint16)((sign << 15) | 0x7C00u | (frac != 0u ? 0x0200u : 0u));
    }

    e = (int32)exp - 127 + 15;

    if (e >= 0x1F)
    {
        /* 上溢 -> Inf */
        return (uint16)((sign << 15) | 0x7C00u);
    }

    if (e <= 0)
    {
        /* 下溢 -> 非规格化 / 0 */
        uint32 shifted = 0u;
        uint32 rem = 0u;
        uint32 half = 0u;
        uint32 shift = 0u;

        if (e < -10)
        {
            return (uint16)(sign << 15);
        }
        frac |= 0x00800000u; /* 恢复隐含 1 */
        shift = (uint32)(14 - e); /* e in [-10,0] -> shift in [14,24] */
        shifted = frac >> shift;
        rem = frac & ((shift >= 32u) ? 0xFFFFFFFFu : ((1u << shift) - 1u));
        half = 1u << (shift - 1u);
        if (rem > half || (rem == half && (shifted & 1u) != 0u))
        {
            shifted += 1u;
        }
        return (uint16)((sign << 15) | shifted);
    }

    {
        /* 规格化：23bit 尾数 -> 10bit */
        uint32 shifted = frac >> 13;
        uint32 rem = frac & 0x1FFFu;

        if (rem > 0x1000u || (rem == 0x1000u && (shifted & 1u) != 0u))
        {
            shifted += 1u;
            if (shifted == 0x400u)
            {
                /* 尾数进位 -> 指数 +1 */
                shifted = 0u;
                e += 1;
                if (e >= 0x1F)
                {
                    return (uint16)((sign << 15) | 0x7C00u);
                }
            }
        }
        return (uint16)((sign << 15) | ((uint32)e << 10) | shifted);
    }
}

SLVM_EC_END

#endif /* MATH_VM_OBJECT_BRIDGE_H */
