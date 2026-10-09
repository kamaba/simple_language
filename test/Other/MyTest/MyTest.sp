# OOMProbe - P5-2 + P5-3 joint acceptance probe (design SMALL_BLOCK_ALLOCATOR_DESIGN.md).
# Phase A/B/C: batch allocations inside label{}catch{} with `try` prefixes; when
# the SBA small-block pool / TLSF arena is exhausted the C VM throws the
# pre-created Core.MemoryError.OOM singleton, the catch prints its message and
# the run CONTINUES (the old NULL path stopped silently).
#   tier budgets (CMake VM_SBA_PROFILE): SBA pool + TLSF arena
#     L0  4096B + 4KB    F4  31552B + 48KB    H7  126208B + 256KB
#   A: object[9999999] -> NewArray over arena on every profile tier -> catch
#   B: 300 new() batch -> L0 NewObject OOM; F4 borderline; H7 passes
#   C: 3000 new() batch -> NewObject OOM on L0/F4/H7 pool limit -> catch
# Note: SL array lengths must be compile-time constants (Front asserts on
# variable lengths), so the array lives in allocArray() with a fixed size and
# holdBatch() applies pure NewObject pressure (gcThreshold is raised first, so
# unreferenced objects still consume the pool until Memory.collect()).
# Phase D: UNCAUGHT forms (no label/try) - the run must stop and the CLI must
# report error_code (-31 NewArray at D2; L0 may hit -4 NewObject at D1 first).

import Std;

Project
{
    _main_()
    {
        Console.println( "===== Mytest start =====" )
        MyTest1.fun();
        Console.println( "===== Mytest end =====" )
    }
    _before_()
    {
    }
    _after_()
    {
    }
}
