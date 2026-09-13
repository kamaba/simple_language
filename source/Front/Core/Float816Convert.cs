//****************************************************************************
//  File:      Float816Convert.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/8/22 12:00:00
//  Description: float8(e4m3/e5m2)/float16/bfloat16 与 float32 的位级转换。
//               底层存储约定：float8 用 byte 保存位模式，float16/bfloat16 用 ushort。
//****************************************************************************

using System;

namespace SimpleLanguage
{
    /// <summary>
    /// 低精度浮点格式的位级编解码工具。
    /// e4m3: 1s+4e+3m, bias=7, 无 inf, NaN=0x7F/0xFF, 最大有限值 ±448
    /// e5m2: 1s+5e+2m, bias=15, 有 inf/NaN
    /// f16 : 1s+5e+10m, bias=15 (IEEE half)
    /// bf16: 1s+8e+7m, bias=127 (bfloat16/brain float16)
    /// 舍入方式：round-to-nearest-even；e4m3 溢出进 NaN 槽位，其余溢出进 inf。
    /// </summary>
    public static class Float816Convert
    {
        // ===================== 编码：float32 -> 位模式 =====================

        public static byte Float32ToFloat8E4M3Bits(float v)
        {
            return (byte)Float32ToBits(v, 4, 3, 7, hasInf: false);
        }

        public static byte Float32ToFloat8E5M2Bits(float v)
        {
            return (byte)Float32ToBits(v, 5, 2, 15, hasInf: true);
        }

        public static ushort Float32ToFloat16Bits(float v)
        {
            return (ushort)Float32ToBits(v, 5, 10, 15, hasInf: true);
        }

        public static ushort Float32ToBFloat16Bits(float v)
        {
            return (ushort)Float32ToBits(v, 8, 7, 127, hasInf: true);
        }

        // ===================== 解码：位模式 -> float32 =====================

        public static float Float8E4M3BitsToFloat32(int bits)
        {
            return BitsToFloat32(bits, 4, 3, 7, hasInf: false);
        }

        public static float Float8E5M2BitsToFloat32(int bits)
        {
            return BitsToFloat32(bits, 5, 2, 15, hasInf: true);
        }

        public static float Float16BitsToFloat32(int bits)
        {
            return BitsToFloat32(bits, 5, 10, 15, hasInf: true);
        }

        public static float BFloat16BitsToFloat32(int bits)
        {
            return BitsToFloat32(bits, 8, 7, 127, hasInf: true);
        }

        // ===================== 按 EType 的对象级辅助 =====================

        /// <summary>把任意数值转为对应低精度类型的位模式存储值（byte/ushort）。</summary>
        public static object ToBitsByEType(EType etype, object value)
        {
            float f = Convert.ToSingle(value, System.Globalization.CultureInfo.InvariantCulture);
            switch (etype)
            {
                case EType.Float8: return Float32ToFloat8E4M3Bits(f);
                case EType.Float8_E5M2: return Float32ToFloat8E5M2Bits(f);
                case EType.Float16: return Float32ToFloat16Bits(f);
                case EType.Float16_Brain: return Float32ToBFloat16Bits(f);
            }
            return value;
        }

        /// <summary>把低精度类型的位模式存储值（byte/ushort）解码为 double 数值。</summary>
        public static double BitsToDoubleByEType(EType etype, object value)
        {
            switch (etype)
            {
                case EType.Float8: return Float8E4M3BitsToFloat32(Convert.ToInt32(value));
                case EType.Float8_E5M2: return Float8E5M2BitsToFloat32(Convert.ToInt32(value));
                case EType.Float16: return Float16BitsToFloat32(Convert.ToInt32(value));
                case EType.Float16_Brain: return BFloat16BitsToFloat32(Convert.ToInt32(value));
            }
            return Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>判断编码->解码后是否与原值完全一致（隐式转换的精度无损判定）。</summary>
        public static bool IsExactlyRepresentable(EType etype, object value)
        {
            double d = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            double roundTrip = BitsToDoubleByEType(etype, ToBitsByEType(etype, value));
            if (double.IsNaN(d) || double.IsInfinity(d))
            {
                return double.IsNaN(roundTrip) || double.IsInfinity(roundTrip);
            }
            return d == roundTrip;
        }

        /// <summary>
        /// 判断值是否落在目标低精度格式的可表示范围内（允许舍入精度损失，仅拒绝溢出）：
        /// e5m2/f16/bf16 溢出表现为 inf，e4m3 溢出表现为 NaN；下溢舍入到 0/次正规视为可接受。
        /// </summary>
        public static bool IsWithinRange(EType etype, object value)
        {
            double d = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            if (double.IsNaN(d))
            {
                return true;
            }
            double roundTrip = BitsToDoubleByEType(etype, ToBitsByEType(etype, value));
            if (double.IsInfinity(roundTrip) || double.IsNaN(roundTrip))
            {
                return false;
            }
            return true;
        }

        // ===================== 通用实现 =====================

        /// <summary>float32 -> 指定低精度格式位模式（round-to-nearest-even）。</summary>
        private static uint Float32ToBits(float v, int ebits, int mbits, int bias, bool hasInf)
        {
            uint fb = unchecked((uint)BitConverter.SingleToInt32Bits(v));
            uint sign = (fb >> 31) & 1u;
            uint exp = (fb >> 23) & 0xFFu;
            uint mant = fb & 0x7FFFFFu;

            uint signShifted = sign << (ebits + mbits);
            int maxExpField = (1 << ebits) - 1;      // e4m3: 15, e5m2/f16: 31, bf16: 255

            if (exp == 255)
            {
                // inf / nan
                if (mant == 0 && hasInf)
                {
                    return signShifted | (uint)(maxExpField << mbits); // inf
                }
                return signShifted | (uint)(maxExpField << mbits) | ((1u << mbits) - 1u); // NaN
            }

            int et = (int)exp - 127 + bias;          // 目标指数域

            // 溢出判断：
            //  - 有 inf 的格式（e5m2/f16/bf16）：指数域达到全 1 即溢出 -> inf
            //  - e4m3：指数域全 1 且尾数 111 才是 NaN，指数域 15 时尾数 0~6 仍是有限数（最大 448）
            if (et >= maxExpField && (hasInf || et > maxExpField))
            {
                if (hasInf)
                {
                    return signShifted | (uint)(maxExpField << mbits);
                }
                return signShifted | (uint)(maxExpField << mbits) | ((1u << mbits) - 1u);
            }

            if (et > 0)
            {
                // 常规数
                int drop = 23 - mbits;
                uint m = mant >> drop;
                uint rem = mant & ((1u << drop) - 1u);
                uint half = 1u << (drop - 1);
                if (rem > half || (rem == half && (m & 1u) != 0))
                {
                    m++;
                    if (m == (1u << mbits))
                    {
                        m = 0;
                        et++;
                        if (et >= maxExpField && (hasInf || et > maxExpField))
                        {
                            if (hasInf)
                            {
                                return signShifted | (uint)(maxExpField << mbits);
                            }
                            return signShifted | (uint)(maxExpField << mbits) | ((1u << mbits) - 1u);
                        }
                    }
                }
                return signShifted | ((uint)et << mbits) | m;
            }

            // 次正规数或更小。
            // 公式 M = (2^23 + mant) * 2^(et - 24 + mbits) 仅当输入是 float32 常规数
            // （exp >= 1，隐含位 2^23 真实存在）时成立。
            // bf16 与 float32 共享 8 位指数（bias=127），bf16 的次正规值域
            // [2^(-bias-mbits), 2^(1-bias)) = [2^-133, 2^-126) 整体落在 float32
            // 次正规区间（exp == 0，无隐含位）之内，此时真实值 = mant * 2^-149，
            // 对应 mm = mant >> (23 - mbits - et)。
            bool inputNormal = exp >= 1;
            uint significand = inputNormal ? (mant | 0x800000u) : mant;
            int shift = (inputNormal ? 24 : 23) - mbits - et;   // et <= 0 时 shift >= 1
            if (shift >= 32)
            {
                // 远小于最小次正规数的一半（significand < 2^24），round-to-nearest -> 0
                return signShifted;
            }
            uint mm = significand >> shift;
            uint remBits = significand & ((1u << shift) - 1u);
            uint halfBits = 1u << (shift - 1);
            if (remBits > halfBits || (remBits == halfBits && (mm & 1u) != 0))
            {
                mm++;
                if (mm == (1u << mbits))
                {
                    // 进位成最小常规数
                    return signShifted | (1u << mbits);
                }
            }
            return signShifted | mm;
        }

        /// <summary>指定低精度格式位模式 -> float32。</summary>
        private static float BitsToFloat32(int bits, int ebits, int mbits, int bias, bool hasInf)
        {
            int maxExpField = (1 << ebits) - 1;
            int mantMask = (1 << mbits) - 1;
            int sign = (bits >> (ebits + mbits)) & 1;
            int exp = (bits >> mbits) & maxExpField;
            int mant = bits & mantMask;

            int resultSign = sign << 31;

            if (exp == 0)
            {
                if (mant == 0)
                {
                    return Int32BitsToFloat(resultSign); // ±0
                }
                // 次正规数: value = mant * 2^(1 - bias - mbits)
                // 左移规格化后: value = (1 + m/2^mbits) * 2^(1 - bias - mbits + shift - ... )
                // 即 f32 指数域 = (1 - bias) - (mbits - shift) + 127
                int e = 1 - bias - mbits;
                int m = mant;
                // 左移直到最高有效位对齐到第 mbits 位
                int shift = 0;
                while ((m & (1 << mbits)) == 0)
                {
                    m <<= 1;
                    shift++;
                }
                m &= mantMask;
                int f32Exp = e + mbits - shift + 127;
                if (f32Exp > 0)
                {
                    int f32Bits = resultSign | (f32Exp << 23) | (m << (23 - mbits));
                    return Int32BitsToFloat(f32Bits);
                }
                // 结果在 float32 中同样是次正规数（仅 bf16 会到达：其最小步长 2^-133
                // 落在 float32 次正规栅格 2^-149 之上，value = mant * 2^e
                // 对应 f32 次正规位 = mant << (e + 149)）。
                {
                    int fs = e + 149;
                    if (fs < 0) fs = 0; // 防御：当前格式不会触发
                    int subBits = resultSign | (mant << fs);
                    return Int32BitsToFloat(subBits);
                }
            }

            if (exp == maxExpField)
            {
                bool isNanSlot = mant != 0 || !hasInf; // e4m3: 最大指数字段全 1 且尾数 111 才是 NaN；此处对 e4m3 需要细分
                if (!hasInf)
                {
                    // e4m3: exp==15 且 mant==7 为 NaN，其余为有限数（最大 448）
                    if (mant == mantMask)
                    {
                        return float.NaN;
                    }
                    // 有限数
                    int f32Bits = resultSign | ((exp - bias + 127) << 23) | (mant << (23 - mbits));
                    return Int32BitsToFloat(f32Bits);
                }
                if (isNanSlot)
                {
                    return float.NaN;
                }
                return Int32BitsToFloat(resultSign | (0xFF << 23)); // inf
            }

            // 常规数
            int bits32 = resultSign | ((exp - bias + 127) << 23) | (mant << (23 - mbits));
            return Int32BitsToFloat(bits32);
        }

        private static float Int32BitsToFloat(int bits)
        {
            return BitConverter.Int32BitsToSingle(bits);
        }
    }
}
