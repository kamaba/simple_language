# MiProbe v1 - P6 evidence probe for the mimalloc host heap takeover.
# Variant host-default (VM_MIMALLOC_DISABLE=1): mimallocEnabled() must report
#   false, version and every byte counter return 0, and Memory.alloc /
#   freeNative keep working through the libc fallback.
# Variant VM_MIMALLOC_ENABLE=ON: mimallocEnabled() reports true, version is
#   30500 (MI_MALLOC_VERSION, v3.5), the byte counters move as native blocks
#   come and go, and after free + mimallocCollect(true) the committed
#   footprint drops as free pages are returned to the OS (the edge TLSF's
#   static arena lacks).
Project
{
    _main_()
    {
        Int64 a = 0
        Int64 b = 0
        Int64 committedBefore = 0
        Int64 committedAfter = 0
        SystemPrintln( "===== MiProbe v1 start =====" )
        if Memory.mimallocEnabled()
        {
            SystemPrintln( "mimalloc: enabled (host takeover)" )
            SystemPrintln( "version: " + Memory.mimallocVersion().toString() )
            committedBefore = Memory.mimallocCommittedBytes()
            SystemPrintln( "baseline: current=" + Memory.mimallocCurrentBytes().toString() + " requested=" + Memory.mimallocRequestedBytes().toString() + " committed=" + committedBefore.toString() + " reserved=" + Memory.mimallocReservedBytes().toString() )
            a = Memory.alloc( 1048576 )
            b = Memory.alloc( 4194304 )
            SystemPrintln( "after a(1M)+b(4M): current=" + Memory.mimallocCurrentBytes().toString() + " requested=" + Memory.mimallocRequestedBytes().toString() + " committed=" + Memory.mimallocCommittedBytes().toString() )
            Memory.freeNative( a )
            Memory.freeNative( b )
            Memory.mimallocCollect( true )
            committedAfter = Memory.mimallocCommittedBytes()
            SystemPrintln( "after free+collect: current=" + Memory.mimallocCurrentBytes().toString() + " peak=" + Memory.mimallocPeakBytes().toString() + " committed=" + committedAfter.toString() )
            if committedAfter < committedBefore + 5242880
            {
                SystemPrintln( "committed released back to OS: ok" )
            }
            else
            {
                SystemPrintln( "committed released back to OS: WEAK (pages may be cached)" )
            }
        }
        else
        {
            SystemPrintln( "mimalloc: disabled (host default keeps libc malloc)" )
            SystemPrintln( "version: " + Memory.mimallocVersion().toString() )
            SystemPrintln( "zeroed: current=" + Memory.mimallocCurrentBytes().toString() + " peak=" + Memory.mimallocPeakBytes().toString() + " requested=" + Memory.mimallocRequestedBytes().toString() + " committed=" + Memory.mimallocCommittedBytes().toString() + " reserved=" + Memory.mimallocReservedBytes().toString() )
            a = Memory.alloc( 65536 )
            if a != 0
            {
                SystemPrintln( "native alloc fallback: ok" )
            }
            else
            {
                SystemPrintln( "native alloc fallback: FAILED" )
            }
            if Memory.freeNative( a )
            {
                SystemPrintln( "native free fallback: ok" )
            }
            else
            {
                SystemPrintln( "native free fallback: FAILED" )
            }
            Memory.mimallocCollect( true )
            SystemPrintln( "still zeroed: current=" + Memory.mimallocCurrentBytes().toString() + " committed=" + Memory.mimallocCommittedBytes().toString() )
        }
        SystemPrintln( "===== MiProbe v1 end =====" )
    }
    CompileBefore()
    {
    }
    CompileAfter()
    {
    }
}
