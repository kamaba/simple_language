# NestedGCProbe - ancestor-frame GC root-set probe (companion to CoroGCProbe).
# vm_gc_mark_runtime_slots scans ONLY the current frame's rows; whether the
# ANCESTOR rows (caller frame arg/local/ret snapshots parked in the child
# frame's caller_* rows) are roots is what this probe measures. No coroutines:
# a plain synchronous chain _main_ -> gcMid -> gcLeaf, with Memory.collect()
# fired at the deepest frame.
# After the collect the leaf floods again so any wrongly swept ancestor string
# has its bytes overwritten by fresh objects - a stale read would NOT compare
# equal (no UAF false positive).
Project
{
    static check( string name, bool cond )
    {
        if cond
        {
            SystemPrintln( "[NestedGCProbe] " + name + " : OK" )
        }
        else
        {
            SystemPrintln( "[NestedGCProbe] " + name + " : FAIL" )
        }
    }

    # Deepest frame: flood -> collect -> flood again (overwrite freed blocks).
    static bool gcLeaf( string inherited )
    {
        ObjectArray junk = object[20000]
        for Int32 i = 0, i < 20000, i = i + 1
        {
            junk[i] = new()
        }
        junk = object[4]
        Int32 freed = Memory.collect()
        SystemPrintln( "leaf collect freed=" + freed.toString() + " objectsAfter=" + Memory.objectCount().toString() )
        ObjectArray junk2 = object[20000]
        for Int32 i = 0, i < 20000, i = i + 1
        {
            junk2[i] = new()
        }
        junk2 = object[4]
        ret ( freed > 0 ) && ( inherited == "MID-KEEP-marker-1234567890" )
    }

    # Middle frame: holds its own local string; its arg row is a caller-row
    # snapshot from gcMid's perspective (and a callee row of gcMid itself).
    static bool gcMid( string inherited )
    {
        string keepMid = "MID-KEEP-marker-1234567890"
        bool leafOk = gcLeaf( keepMid )
        SystemPrintln( "mid keepMid=" + keepMid )
        ret leafOk && ( keepMid == "MID-KEEP-marker-1234567890" )
    }

    _main_()
    {
        SystemPrintln( "===== NestedGCProbe start =====" )
        Memory.setMode( 1 )
        Memory.setGcThreshold( 100000 )
        SystemPrintln( "gcThreshold=" + Memory.gcThreshold().toString() )

        # Outermost frame state that must survive a GC two levels deeper.
        string keepRoot = "ROOT-KEEP-marker-abcdefghij"
        Int32 rootTag = 777
        bool midOk = gcMid( keepRoot )

        SystemPrintln( "root keepRoot=" + keepRoot )
        bool rootOk = midOk && ( keepRoot == "ROOT-KEEP-marker-abcdefghij" ) && ( rootTag == 777 )
        check( "nested GC keeps mid frame rows", midOk )
        check( "nested GC keeps root frame rows", rootOk )
        SystemPrintln( "cycles=" + Memory.GcCycleCount().toString() )
        SystemPrintln( "===== NestedGCProbe end =====" )
    }
    CompileBefore()
    {
    }
    CompileAfter()
    {
    }
}
