# =========================================================================
# Platform_Cpu — arch / cpu / isa definition enums (§13.6).
#
# Part of `namespace Environment { namespace Platform { ... } }`
# (design doc PLATFORM_CAPABILITY_DESIGN.md §13.2).
#
# Usage:
#   if Environment.current.arch == Environment.Platform.arch.x64 { ... }
#   if Environment.current.isaHas( Environment.Platform.isa.avx2 ) { ... }
# =========================================================================

namespace Environment
{
    namespace Platform
    {
        # CPU architecture (§13.6).
        public enum arch extends int
        {
            unknown     = 0
            x86         = 1
            # x64: x86_64 / amd64
            x64         = 2
            # arm32: arm / armv7
            arm32       = 3
            # arm64: aarch64 / arm64
            arm64       = 4
            riscv64     = 5
            loongArch64 = 6
            mips64      = 7
            sw64        = 8
            ppc64       = 9
            sparc64     = 10
            wasm32      = 11
        }

        # CPU topology kind (§13.6).
        public enum cpu extends int
        {
            unknown  = 0
            single   = 1
            smp      = 2
            bigLittle = 3
            hybrid   = 4
            numa     = 5
        }

        # Instruction set extensions (§13.6). ★ Bit set semantics: the value
        # is a BIT INDEX, not a bit value — query with
        # Environment.current.isaHas( Environment.Platform.isa.avx2 ).
        public enum isa extends int
        {
            none    = 0
            sse     = 1
            sse2    = 2
            sse42   = 3
            avx     = 4
            avx2    = 5
            avx512  = 6
            fma     = 7
            neon    = 10
            sve     = 11
            sve2    = 12
            rvv     = 13
            lsx     = 14
            lasx    = 15
            mmi     = 16
            altivec = 17
            vsx     = 18
        }
    }
}
