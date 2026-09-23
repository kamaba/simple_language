# SBAProbe v2 - P3 evidence probe for vm_sba_reclaim (design SMALL_BLOCK_ALLOCATOR_DESIGN.md 3.5).
# Strategy: hold a big batch of objects live AT ONCE (forces SBA to carve many
# 64KB chunks after module load), then drop the whole batch and force a full GC
# cycle -> every block of the batch chunks goes back to the free list ->
# GC Phase 4.5 (vm_sba_reclaim) returns the fully-idle chunks to the OS.
# Evidence lives in the VM log:  [SBA] reclaimed N chunks to the OS
Project
{
    _main_()
    {
        ObjectArray objs = object[4]
        Object o = new()
        Int32 freed = 0
        SystemPrintln( "===== SBAProbe v2 start =====" )
        Memory.setMode( 1 )
        Memory.setGcThreshold( 100000 )
        SystemPrintln( "gcThreshold=" + Memory.gcThreshold().toString() )

        # batch 1: hold 20000 objects live at once (far above the ~34 pages of
        # pre-existing chunks, so SBA carves fresh 64KB chunks for the batch),
        # then drop them all -> those fresh chunks become fully idle -> reclaim.
        objs = object[20000]
        for Int32 i = 0, i < 20000, i = i + 1
        {
            o = new()
            objs[i] = o
        }
        SystemPrintln( "batch1 filled: objects=" + Memory.objectCount().toString() + " totalAlloc=" + Memory.totalAllocated().toString() )
        objs = object[4]
        freed = Memory.collect()
        SystemPrintln( "batch1 collectFreed=" + freed.toString() + " objectsAfter=" + Memory.objectCount().toString() )

        # batch 2: repeat with the same oversized batch to prove reclaim is repeatable
        objs = object[20000]
        for Int32 i = 0, i < 20000, i = i + 1
        {
            o = new()
            objs[i] = o
        }
        SystemPrintln( "batch2 filled: objects=" + Memory.objectCount().toString() + " totalAlloc=" + Memory.totalAllocated().toString() )
        objs = object[4]
        freed = Memory.collect()
        SystemPrintln( "batch2 collectFreed=" + freed.toString() + " objectsAfter=" + Memory.objectCount().toString() )

        SystemPrintln( "cycles=" + Memory.GcCycleCount().toString() )
        SystemPrintln( "===== SBAProbe v2 end =====" )
    }
    CompileBefore()
    {
    }
    CompileAfter()
    {
    }
}
