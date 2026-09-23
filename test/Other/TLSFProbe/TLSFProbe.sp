# TLSFProbe v1 - P4 evidence probe for the large-object TLSF arena
# (design SMALL_BLOCK_ALLOCATOR_DESIGN.md 3.6/3.7).
# Variant A (host default, VM_TLSF_DISABLE=1): tlsfEnabled() must report
#   disabled, all tlsf* getters return 0, and Memory.nativeAlloc/Free keep
#   working through the base_malloc fallback.
# Variant C1 (VM_TLSF_ENABLE=ON): tlsfEnabled() reports enabled; used/free
#   bytes move as native blocks come and go; freeing the middle block and
#   re-allocating a same-size block exposes external fragmentation; a 2 MiB
#   request (larger than the default 1 MiB arena) exercises the arena
#   exhaustion fallback to the libc heap and must still succeed.
Project
{
    _main_()
    {
        Int64 a = 0
        Int64 b = 0
        Int64 c = 0
        Int64 d = 0
        Int64 big = 0
        SystemPrintln( "===== TLSFProbe v1 start =====" )
        if Memory.tlsfEnabled()
        {
            SystemPrintln( "tlsf: enabled" )
            SystemPrintln( "init: used=" + Memory.tlsfUsedBytes().toString() + " free=" + Memory.tlsfFreeBytes().toString() + " frag=" + Memory.tlsfFragmentation().toString() )
            a = Memory.alloc( 65536 )
            b = Memory.alloc( 262144 )
            c = Memory.alloc( 131072 )
            SystemPrintln( "after abc: used=" + Memory.tlsfUsedBytes().toString() + " free=" + Memory.tlsfFreeBytes().toString() )
            Memory.freeNative( b )
            SystemPrintln( "after free b: used=" + Memory.tlsfUsedBytes().toString() + " free=" + Memory.tlsfFreeBytes().toString() + " frag=" + Memory.tlsfFragmentation().toString() )
            d = Memory.alloc( 131072 )
            SystemPrintln( "after d: used=" + Memory.tlsfUsedBytes().toString() + " free=" + Memory.tlsfFreeBytes().toString() )
            big = Memory.alloc( 2097152 )
            if big != 0
            {
                SystemPrintln( "big(2M) over-arena fallback alloc: ok" )
            }
            else
            {
                SystemPrintln( "big(2M) over-arena fallback alloc: FAILED" )
            }
            SystemPrintln( "after big: used=" + Memory.tlsfUsedBytes().toString() + " free=" + Memory.tlsfFreeBytes().toString() )
            Memory.freeNative( a )
            Memory.freeNative( c )
            Memory.freeNative( d )
            Memory.freeNative( big )
            SystemPrintln( "final: used=" + Memory.tlsfUsedBytes().toString() + " free=" + Memory.tlsfFreeBytes().toString() + " frag=" + Memory.tlsfFragmentation().toString() )
        }
        else
        {
            SystemPrintln( "tlsf: disabled (host default)" )
            SystemPrintln( "zeroed: used=" + Memory.tlsfUsedBytes().toString() + " free=" + Memory.tlsfFreeBytes().toString() + " frag=" + Memory.tlsfFragmentation().toString() )
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
            SystemPrintln( "still zeroed: used=" + Memory.tlsfUsedBytes().toString() + " free=" + Memory.tlsfFreeBytes().toString() + " frag=" + Memory.tlsfFragmentation().toString() )
        }
        SystemPrintln( "===== TLSFProbe v1 end =====" )
    }
    CompileBefore()
    {
    }
    CompileAfter()
    {
    }
}
