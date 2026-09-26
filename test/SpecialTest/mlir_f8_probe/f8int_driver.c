/*
 * f8int_driver.c - differential test driver for f8int_probe.obj
 *
 * Reference: verbatim copy of lp_float32_to_bits / lp_bits_to_float32 from
 * csimple_lang/src/vm/runtime/runtime_value/runtime_value_convert.c
 * (the semantic baseline the MLIR integer implementation must reproduce).
 *
 * Probe entry points (from f8int_probe.mlir via llc, Win x64, C naming):
 *   int        e4m3_encode(float x);
 *   float      e4m3_decode(int lp);
 *   int        e5m2_encode(float x);
 *   float      e5m2_decode(int lp);
 *   int        f16_encode(float x);    // integer implementation (no F16C):
 *   float      f16_decode(int lp);     // native truncf/extf lowers to
 *   int        bf16_encode(float x);   // compiler-rt soft-float calls that
 *   float      bf16_decode(int lp);    // the AOT dll link cannot resolve.
 *   long long  e4m3_rt(double d);   // i64 = f64 bits
 *   long long  e5m2_rt(double d);
 *   long long  f16_rt(double d);
 *   long long  bf16_rt(double d);
 *
 * All four formats use the integer encode/decode and are compared BIT-EXACTLY
 * against the reference (including NaN payloads: the integer path reproduces
 * the reference's canonical 0x7FC00000 handling).
 *
 * Build: cl /nologo /O2 f8int_driver.c f8int_probe.obj /Fe:f8int_probe.exe
 */
#include <stdio.h>
#include <stdint.h>
#include <math.h>

typedef float float32;
typedef double float64;
typedef unsigned int uint32;
typedef int boolean;
#define TRUE 1
#define FALSE 0

/* ================= reference implementation (verbatim) ================= */
typedef union { float32 f; uint32 u; } lp_f32_bits;

static float32 lp_f32_from_bits(uint32 u)
{
    lp_f32_bits cvt;
    cvt.u = u;
    return cvt.f;
}

static uint32 lp_f32_to_u32(float32 f)
{
    lp_f32_bits cvt;
    cvt.f = f;
    return cvt.u;
}

/* float32 -> specified low-precision bit pattern (round-to-nearest-even) */
static uint32 lp_float32_to_bits(float32 v, int ebits, int mbits, int bias, boolean has_inf)
{
    uint32 fb, sign, exp, mant, sign_shifted, m, rem, half, significand, mm, rem_bits, half_bits;
    int max_exp_field, et, drop, shift;

    fb = lp_f32_to_u32(v);
    sign = (fb >> 31) & 1u;
    exp = (fb >> 23) & 0xFFu;
    mant = fb & 0x7FFFFFu;

    sign_shifted = sign << (ebits + mbits);
    max_exp_field = (1 << ebits) - 1;      /* e4m3: 15, e5m2/f16: 31, bf16: 255 */

    if (exp == 255u)
    {
        /* inf / nan */
        if (mant == 0u && has_inf)
        {
            return sign_shifted | ((uint32)max_exp_field << mbits); /* inf */
        }
        return sign_shifted | ((uint32)max_exp_field << mbits) | ((1u << mbits) - 1u); /* NaN */
    }

    et = (int)exp - 127 + bias;            /* target exponent field */

    /* Overflow:
     *  - formats with inf (e5m2/f16/bf16): exponent field all-ones -> inf
     *  - e4m3: exp field 15 with mant 111 is NaN; mant 0..6 still finite (max 448) */
    if (et >= max_exp_field && (has_inf || et > max_exp_field))
    {
        if (has_inf)
        {
            return sign_shifted | ((uint32)max_exp_field << mbits);
        }
        return sign_shifted | ((uint32)max_exp_field << mbits) | ((1u << mbits) - 1u);
    }

    if (et > 0)
    {
        /* normal number */
        drop = 23 - mbits;
        m = mant >> drop;
        rem = mant & ((1u << drop) - 1u);
        half = 1u << (drop - 1);
        if (rem > half || (rem == half && (m & 1u) != 0u))
        {
            m++;
            if (m == (1u << mbits))
            {
                m = 0;
                et++;
                if (et >= max_exp_field && (has_inf || et > max_exp_field))
                {
                    if (has_inf)
                    {
                        return sign_shifted | ((uint32)max_exp_field << mbits);
                    }
                    return sign_shifted | ((uint32)max_exp_field << mbits) | ((1u << mbits) - 1u);
                }
            }
        }
        return sign_shifted | ((uint32)et << mbits) | m;
    }

    /* Subnormal or smaller: M = (2^23 + mant) * 2^(et - 24 + mbits) only holds
     * when the input is a normal float32 (exp >= 1, implicit 2^23 bit exists).
     * bf16 shares the 8-bit exponent with float32 (bias=127): its subnormal
     * range [2^-133, 2^-126) falls entirely inside the float32 subnormal
     * region (exp == 0, no implicit bit), where value = mant * 2^-149,
     * i.e. mm = mant >> (23 - mbits - et). */
    {
        boolean input_normal = (exp >= 1u);
        significand = input_normal ? (mant | 0x800000u) : mant;
        shift = (input_normal ? 24 : 23) - mbits - et;  /* et <= 0 -> shift >= 1 */
    }
    if (shift >= 32)
    {
        /* far below half of the smallest subnormal -> round to 0 */
        return sign_shifted;
    }
    mm = significand >> shift;
    rem_bits = significand & ((1u << shift) - 1u);
    half_bits = 1u << (shift - 1);
    if (rem_bits > half_bits || (rem_bits == half_bits && (mm & 1u) != 0u))
    {
        mm++;
        if (mm == (1u << mbits))
        {
            /* carry into the smallest normal number */
            return sign_shifted | (1u << mbits);
        }
    }
    return sign_shifted | mm;
}

/* specified low-precision bit pattern -> float32 */
static float32 lp_bits_to_float32(int bits, int ebits, int mbits, int bias, boolean has_inf)
{
    int max_exp_field = (1 << ebits) - 1;
    int mant_mask = (1 << mbits) - 1;
    int sign = (bits >> (ebits + mbits)) & 1;
    int exp = (bits >> mbits) & max_exp_field;
    int mant = bits & mant_mask;
    int result_sign = sign << 31;
    int e, m, shift, f32_exp, f32_bits;

    if (exp == 0)
    {
        if (mant == 0)
        {
            return lp_f32_from_bits((uint32)result_sign); /* +-0 */
        }
        /* subnormal: value = mant * 2^(1 - bias - mbits), normalize by left shift */
        e = 1 - bias - mbits;
        m = mant;
        shift = 0;
        while ((m & (1 << mbits)) == 0)
        {
            m <<= 1;
            shift++;
        }
        m &= mant_mask;
        f32_exp = e + mbits - shift + 127;
        if (f32_exp > 0)
        {
            f32_bits = result_sign | (f32_exp << 23) | (m << (23 - mbits));
            return lp_f32_from_bits((uint32)f32_bits);
        }
        /* The result is subnormal in float32 too (only bf16 reaches here:
         * its subnormal gap 2^-133 is finer than float32's 2^-149 grid... in
         * fact value = mant * 2^e maps to f32 subnormal bits mant << (e+149)). */
        {
            int fs = e + 149;
            if (fs < 0)
            {
                fs = 0; /* defensive: cannot happen for supported formats */
            }
            f32_bits = result_sign | ((uint32)mant << fs);
            return lp_f32_from_bits((uint32)f32_bits);
        }
    }

    if (exp == max_exp_field)
    {
        if (!has_inf)
        {
            /* e4m3: exp==15 && mant==7 is NaN, others finite (max 448) */
            if (mant == mant_mask)
            {
                return lp_f32_from_bits(0x7FC00000u); /* NaN */
            }
            f32_bits = result_sign | ((exp - bias + 127) << 23) | (mant << (23 - mbits));
            return lp_f32_from_bits((uint32)f32_bits);
        }
        if (mant != 0)
        {
            return lp_f32_from_bits(0x7FC00000u); /* NaN */
        }
        return lp_f32_from_bits((uint32)(result_sign | (0xFF << 23))); /* inf */
    }

    /* normal number */
    f32_bits = result_sign | ((exp - bias + 127) << 23) | (mant << (23 - mbits));
    return lp_f32_from_bits((uint32)f32_bits);
}
/* ====================== end reference implementation ==================== */

/* probe entry points */
extern int       e4m3_encode(float x);
extern float     e4m3_decode(int lp);
extern int       e5m2_encode(float x);
extern float     e5m2_decode(int lp);
extern int       f16_encode(float x);
extern float     f16_decode(int lp);
extern int       bf16_encode(float x);
extern float     bf16_decode(int lp);
extern long long e4m3_rt(double d);
extern long long e5m2_rt(double d);
extern long long f16_rt(double d);
extern long long bf16_rt(double d);

static uint64_t g_fail = 0;

static void fail4(const char* what, uint64_t input, uint64_t got, uint64_t want)
{
    if (g_fail < 16)
    {
        printf("FAIL %-12s input=0x%016llX got=0x%016llX want=0x%016llX\n",
               what, input, got, want);
    }
    g_fail++;
}

typedef union { float64 d; uint64_t u; } f64_bits;
static uint64_t f64u(double d) { f64_bits c; c.d = d; return c.u; }

/* ------------------------------------------------------------------ */
/* 1) encode exhaustive: all 2^32 f32 patterns, both formats           */
/* ------------------------------------------------------------------ */
static void test_encode_exhaustive(void)
{
    uint64_t i;
    for (i = 0; i <= 0xFFFFFFFFULL; i++)
    {
        float x = lp_f32_from_bits((uint32)i);
        uint32 p4 = (uint32)e4m3_encode(x);
        uint32 r4 = lp_float32_to_bits(x, 4, 3, 7, FALSE);
        uint32 p5 = (uint32)e5m2_encode(x);
        uint32 r5 = lp_float32_to_bits(x, 5, 2, 15, TRUE);
        uint32 p16 = (uint32)f16_encode(x);
        uint32 r16 = lp_float32_to_bits(x, 5, 10, 15, TRUE);
        uint32 pbf = (uint32)bf16_encode(x);
        uint32 rbf = lp_float32_to_bits(x, 8, 7, 127, TRUE);
        if (p4 != r4) fail4("e4m3_encode", i, p4, r4);
        if (p5 != r5) fail4("e5m2_encode", i, p5, r5);
        if (p16 != r16) fail4("f16_encode", i, p16, r16);
        if (pbf != rbf) fail4("bf16_encode", i, pbf, rbf);
        if ((i & 0x3FFFFFFFULL) == 0x3FFFFFFFULL)
            printf("  encode sweep at 0x%08llX\n", (unsigned long long)i);
    }
    printf("encode exhaustive: done (fails so far %llu)\n", (unsigned long long)g_fail);
}

/* ------------------------------------------------------------------ */
/* 2) decode exhaustive: all 256 lp values, both formats               */
/* ------------------------------------------------------------------ */
static void test_decode_exhaustive(void)
{
    int lp;
    for (lp = 0; lp < 256; lp++)
    {
        float p4 = e4m3_decode(lp);
        float r4 = lp_bits_to_float32(lp, 4, 3, 7, FALSE);
        float p5 = e5m2_decode(lp);
        float r5 = lp_bits_to_float32(lp, 5, 2, 15, TRUE);
        if (lp_f32_to_u32(p4) != lp_f32_to_u32(r4))
            fail4("e4m3_decode", (uint64_t)lp, lp_f32_to_u32(p4), lp_f32_to_u32(r4));
        if (lp_f32_to_u32(p5) != lp_f32_to_u32(r5))
            fail4("e5m2_decode", (uint64_t)lp, lp_f32_to_u32(p5), lp_f32_to_u32(r5));
    }
    /* f16 / bf16 are 16-bit formats: full 65536-value sweep */
    for (lp = 0; lp < 65536; lp++)
    {
        float p16 = f16_decode(lp);
        float r16 = lp_bits_to_float32(lp, 5, 10, 15, TRUE);
        float pbf = bf16_decode(lp);
        float rbf = lp_bits_to_float32(lp, 8, 7, 127, TRUE);
        if (lp_f32_to_u32(p16) != lp_f32_to_u32(r16))
            fail4("f16_decode", (uint64_t)lp, lp_f32_to_u32(p16), lp_f32_to_u32(r16));
        if (lp_f32_to_u32(pbf) != lp_f32_to_u32(rbf))
            fail4("bf16_decode", (uint64_t)lp, lp_f32_to_u32(pbf), lp_f32_to_u32(rbf));
    }
    printf("decode exhaustive: done (fails so far %llu)\n", (unsigned long long)g_fail);
}

/* ------------------------------------------------------------------ */
/* 3) full round trips (VM Convert semantics)                          */
/*    ref = bits( (double) lp_bits_to_float32(                         */
/*                    lp_float32_to_bits((float)d, fmt), fmt) )        */
/* ------------------------------------------------------------------ */
static void rt_e4m3(double d)
{
    uint64_t got = (uint64_t)e4m3_rt(d);
    uint32 lp = lp_float32_to_bits((float32)d, 4, 3, 7, FALSE);
    double w = (double)lp_bits_to_float32((int)lp, 4, 3, 7, FALSE);
    uint64_t want = f64u(w);
    if (got != want) fail4("e4m3_rt", f64u(d), got, want);
}

static void rt_e5m2(double d)
{
    uint64_t got = (uint64_t)e5m2_rt(d);
    uint32 lp = lp_float32_to_bits((float32)d, 5, 2, 15, TRUE);
    double w = (double)lp_bits_to_float32((int)lp, 5, 2, 15, TRUE);
    uint64_t want = f64u(w);
    if (got != want) fail4("e5m2_rt", f64u(d), got, want);
}

static void rt_f16(double d)
{
    uint64_t got = (uint64_t)f16_rt(d);
    uint32 lp = lp_float32_to_bits((float32)d, 5, 10, 15, TRUE);
    double w = (double)lp_bits_to_float32((int)lp, 5, 10, 15, TRUE);
    uint64_t want = f64u(w);
    if (got != want) fail4("f16_rt", f64u(d), got, want);
}

static void rt_bf16(double d)
{
    uint64_t got = (uint64_t)bf16_rt(d);
    uint32 lp = lp_float32_to_bits((float32)d, 8, 7, 127, TRUE);
    double w = (double)lp_bits_to_float32((int)lp, 8, 7, 127, TRUE);
    uint64_t want = f64u(w);
    if (got != want) fail4("bf16_rt", f64u(d), got, want);
}

static void rt_all(double d)
{
    rt_e4m3(d); rt_e5m2(d); rt_f16(d); rt_bf16(d);
}

static void test_roundtrip_boundary(void)
{
    static const double v[] = {
        0.0, -0.0, 1.0, -1.0, 0.5, -0.5,
        0.001953125, 0.0009765625,          /* 2^-9, 2^-10 (e4m3 grid)  */
        0.00006103515625, 0.000030517578125,/* 2^-14, 2^-15 (e5m2 grid) */
        0.0000152587890625, 5.960464477539063e-08, /* 2^-16, 2^-24 */
        240.0, 288.0, 416.0, 432.0, 440.0, 448.0, 449.0, 456.0, 464.0, 465.0,
        480.0, 512.0, 57344.0,
        65504.0, 65505.0, 65519.9, 65520.0, 65521.0, 65536.0,
        1e30, 1e300,
        1.0 / 3.0, 2.0 / 3.0, 1e-45, 1e-40, 1e-20,
        3.4028234663852886e38,   /* f32 max */
    };
    static const double sv[] = {
        0.0, -0.0, 1.0, -1.0, 0.5,
    };
    /* inf / NaN as raw bit patterns */
    static const uint64_t sp[] = {
        0x7FF0000000000000ULL, /* +inf */
        0xFFF0000000000000ULL, /* -inf */
        0x7FF8000000000000ULL, /* qNaN canonical */
        0xFFF8000000000001ULL, /* qNaN payload */
        0x7FF0000000000001ULL, /* sNaN */
    };
    size_t k;

    for (k = 0; k < sizeof(v) / sizeof(v[0]); k++)
    {
        double d = v[k];
        rt_all(d);  rt_all(-d);
        /* +- 1 f32-ulp neighbors around each boundary value */
        {
            float f = (float)d;
            float nf = nextafterf(f, 1e30f);
            float pf = nextafterf(f, -1e30f);
            rt_all((double)nf); rt_all((double)pf);
            rt_all((double)(-nf)); rt_all((double)(-pf));
        }
    }
    for (k = 0; k < sizeof(sv) / sizeof(sv[0]); k++)
    {
        rt_all(sv[k]);
    }
    for (k = 0; k < sizeof(sp) / sizeof(sp[0]); k++)
    {
        f64_bits c; c.u = sp[k];
        rt_all(c.d);
    }
    printf("roundtrip boundary: done (fails so far %llu)\n", (unsigned long long)g_fail);
}

static void test_roundtrip_random(void)
{
    uint64_t s = 88172645463325252ULL;
    int i;
    for (i = 0; i < 2000000; i++)
    {
        f64_bits c;
        s ^= s << 13; s ^= s >> 7; s ^= s << 17;
        c.u = s;
        rt_all(c.d);
    }
    printf("roundtrip random: done (fails so far %llu)\n", (unsigned long long)g_fail);
}

/* ------------------------------------------------------------------ */
/* 4) f16 / bf16 full round trips, exhaustive over all 2^32 f32        */
/*    inputs (integer implementation: bit-exact comparison).           */
/*    truncf f64->f32 of an f32 bit pattern is the identity, so this   */
/*    sweep covers every f64 that a VM Convert could produce.          */
/* ------------------------------------------------------------------ */
static void test_f16_bf16_exhaustive(void)
{
    uint64_t i;
    for (i = 0; i <= 0xFFFFFFFFULL; i++)
    {
        double d = (double)lp_f32_from_bits((uint32)i);
        rt_f16(d);
        rt_bf16(d);
        if ((i & 0x3FFFFFFFULL) == 0x3FFFFFFFULL)
            printf("  rt sweep at 0x%08llX\n", (unsigned long long)i);
    }
    printf("f16/bf16 exhaustive: done (fails so far %llu)\n", (unsigned long long)g_fail);
}

int main(int argc, char** argv)
{
    int stage = 0;
    setvbuf(stdout, NULL, _IONBF, 0);
    if (argc > 1) stage = atoi(argv[1]);

    if (stage == 0 || stage == 1) test_decode_exhaustive();
    if (stage == 0 || stage == 2) test_roundtrip_boundary();
    if (stage == 0 || stage == 3) test_roundtrip_random();
    if (stage == 0 || stage == 4) test_encode_exhaustive();
    if (stage == 0 || stage == 5) test_f16_bf16_exhaustive();

    if (g_fail)
    {
        printf("RESULT: FAIL (%llu mismatches)\n", (unsigned long long)g_fail);
        return 1;
    }
    printf("RESULT: PASS\n");
    return 0;
}
