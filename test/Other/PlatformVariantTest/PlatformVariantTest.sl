# PlatformVariantTest - P3 platform.variants positive case (design
# PLATFORM_CAPABILITY_DESIGN.md §14; jsonc in PlatformVariantTest.jsonc).
#
# Variant table (ordered, first match wins):
#   1. linux-native : os == linux
#   2. win-native   : os == window, environment docker OPTIONAL
#   3. any-interp   : no require -> interpreter fallback, always matches
#
# Expected on this host (Windows):
#   - variant 1 fails the os check -> skipped (no per-variant noise)
#   - variant 2 matches: os == window passes, docker is optional so a
#     non-container host only emits one downgrade line and the variant
#     still wins -> selectedVariant == 1
#   - variant 3 never needs to be consulted
#
# Observed output (manual verification, Windows host):
#   [PlatformCheck] 模块 "PlatformVariantTest" 选用变体 2/3 "win-native"（1 项可选要求未满足，降级运行）
#   [PlatformVariantTest] running: variant selection passed
#   [PlatformVariantTest] osName: windows
#   exit code 0
#
# Reaching _main_ at all proves the loader accepted a variant (a module
# whose variants all fail is rejected with exit code != 0 before any
# SL code runs). The _main_ in PlatformVariantTest.sp prints the marker
# inline (the .sp entry cannot call classes of sibling .sl files).

PlatformVariantTest
{
    static fun()
    {
        SystemPrintln( "[PlatformVariantTest] running: variant selection passed" )
        SystemPrintln( "[PlatformVariantTest] osName: " + Environment.current.osName )
    }
}
