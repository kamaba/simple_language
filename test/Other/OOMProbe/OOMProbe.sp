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
Project
{
    static Int32 allocArray() throws
    {
        ObjectArray objs = object[9999999]
        ret 0
    }

    static Int32 holdBatch( Int32 count ) throws
    {
        Object o = new()
        for Int32 i = 0, i < count, i = i + 1
        {
            o = new()
        }
        ret count
    }

    _main_()
    {
        SystemPrintln( "===== OOMProbe start =====" )
        Memory.setMode( 1 )
        Memory.setGcThreshold( 1000000 )

        SystemPrintln( "-- phase A: array over arena --" )
        label ArrBlock
        {
            try allocArray()
        }
        catch MemoryError e
        {
            SystemPrintln( "A catch hit" )
            SystemPrintln( e.toString() )
        }
        finally
        {
            SystemPrintln( "A finally ran" )
        }

        SystemPrintln( "-- phase B: 300-slot hold batch --" )
        label Hold300
        {
            try holdBatch( 300 )
        }
        catch MemoryError e
        {
            SystemPrintln( "B catch hit" )
            SystemPrintln( e.toString() )
        }

        SystemPrintln( "-- phase C: 3000-slot hold batch --" )
        label Hold3000
        {
            try holdBatch( 3000 )
        }
        catch MemoryError e
        {
            SystemPrintln( "C catch hit" )
            SystemPrintln( e.toString() )
        }

        SystemPrintln( "-- phases recovered, forcing gc --" )
        Int32 freed = Memory.collect()
        SystemPrintln( "collect freed=" + freed.toString() + " objects=" + Memory.objectCount().toString() )

        # UNCAUGHT forms - must run last: the run stops here on profile tiers.
        ObjectArray d1 = object[64]
        Object o1 = new()
        for Int32 i = 0, i < 64, i = i + 1
        {
            o1 = new()
            d1[i] = o1
        }
        SystemPrintln( "D1 survived (pool holds 64 objects)" )
        ObjectArray d2 = object[9999999]
        SystemPrintln( "unreachable on profile tiers" )
        SystemPrintln( "===== OOMProbe end =====" )
    }
    CompileBefore()
    {
    }
    CompileAfter()
    {
    }
}
