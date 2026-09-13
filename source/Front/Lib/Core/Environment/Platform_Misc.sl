# =========================================================================
# Platform_Misc — network / link / embedded / rtos / runtime / build / endian
# definition enums (§13.6).
#
# Part of `namespace Environment { namespace Platform { ... } }`
# (design doc PLATFORM_CAPABILITY_DESIGN.md §13.2).
# =========================================================================

namespace Environment
{
    namespace Platform
    {
        # Network connectivity kinds (§13.6). Bit set.
        public enum network extends int
        {
            none       = 0
            loopback   = 1
            ethernet   = 2
            wifi       = 3
            cellular   = 4
            unknownNet = 5
        }

        # Link state (§13.6).
        # 注：设计文档原名 local，但 local 是 SL 保留字（LexerParseToToken.cs
        # ETokenType.Local），不可作枚举成员名，故改名为 lan；
        # jsonc 字符串值 "local" 由 C 侧 sl_def_link 别名表兼容。
        public enum link extends int
        {
            down   = 0
            lan    = 1
            online = 2
        }

        # MCU families for firmware targets (§13.6).
        public enum embedded extends int
        {
            none     = 0
            cortexM  = 1
            cortexA  = 2
            cortexR  = 3
            xtensa   = 4
            riscvMcu = 5
            mcs51    = 6
            avr      = 7
            pic      = 8
            msp430   = 9
            rl78     = 10
            rx       = 11
            triCore   = 12
            hc08     = 13
        }

        # Real-time operating systems (§13.6).
        public enum rtos extends int
        {
            none         = 0
            freeRTOS     = 50
            rtThread     = 51
            zephyr       = 52
            threadX      = 53
            ucos         = 54
            mbed         = 55
            liteOS       = 56
            aliosThings  = 57
        }

        # Host runtime kind (§13.6). CVM always reports slvm.
        public enum runtime extends int
        {
            slvm = 0
            clr  = 1
            jvm  = 2
            aot  = 3
            wasm = 4
        }

        # Build mode (§12.9): comes from the entry module's jsonc
        # compile.optimize.
        public enum build extends int
        {
            debug   = 0
            release = 1
        }

        # Byte order (§13.6).
        public enum endian extends int
        {
            unknown = 0
            little  = 1
            big     = 2
        }
    }
}
