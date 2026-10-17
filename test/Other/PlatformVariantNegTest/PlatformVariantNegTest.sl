# PlatformVariantNegTest - P3 platform.variants negative case (design
# PLATFORM_CAPABILITY_DESIGN.md §14; jsonc in PlatformVariantNegTest.jsonc).
#
# Variant table: a single linux-only variant (os == linux). The value is
# a legal os member name, so Front compilation succeeds and the check
# happens at CVM load time.
#
# Expected on Windows at run time (manual verification, not automated):
#   csimple_lang.exe run <out>/export/PlatformVariantNegTest/PlatformVariantNegTest.module.json
#   1. [PlatformCheck] prints a per-variant diagnostic (each variant
#      marked with why it failed, e.g. os: expect linux, actual window)
#      plus the fallback hint
#   2. module load is refused, exit code != 0, no SL code executes
#      (the println below is unreachable)
#   3. adding --force-run bypasses the refusal: module runs, the
#      selectedVariant stays -1 and AOT loading falls back to the
#      module-level behaviour (vm_aot_registry.c variant guard)
#
# fun() exists only so the file compiles into the module; it is never
# expected to run under a plain `run`.

PlatformVariantNegTest
{
    static fun()
    {
        SystemPrintln( "unreachable: no variant matches, CVM rejects the module before execution" )
    }
}
