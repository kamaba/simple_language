# CoroGCProbe - coroutine GC root-set gap probe (GC_DESIGN.md 3.4.1).
# vm_gc_mark_coroutine_roots must keep alive, across a forced Memory.collect():
#   S1  a SUSPENDED worker coroutine's frame-held object (frame local rows +
#       private stack snapshot)
#   S2  objects held by a coroutine suspended inside a NESTED call: both the
#       leaf frame (callee rows) and the ancestor frame snapshot (caller rows)
#       are off the vm-level runtime rows while the worker is parked
#   S3  a DEAD worker's ret_values payload, fetched by a late await AFTER the
#       collect (ret_values is the only remaining reference)
# Before the fix the sweep freed those objects (WHITE) and the resume / late
# await read freed memory (UAF). Strings are built by runtime concatenation
# (variable + toString) so they are fresh heap objects, not interned literals.
Project
{
    static check( string name, bool cond )
    {
        if cond
        {
            SystemPrintln( "[CoroGCProbe] " + name + " : OK" )
        }
        else
        {
            SystemPrintln( "[CoroGCProbe] " + name + " : FAIL" )
        }
    }

    # S2 leaf: suspends INSIDE a nested method call, holding its own local
    # string; the caller (worker frame) holds sAnc at the same time, so the
    # parked chain covers leaf callee rows AND ancestor caller rows.
    static string s2Leaf()
    {
        Int32 v = 22
        string sLeaf = "S2-leaf-" + v.toString()
        yield;
        ret sLeaf
    }

    _main_()
    {
        SystemPrintln( "===== CoroGCProbe start =====" )
        Memory.setMode( 1 )
        Memory.setGcThreshold( 100000 )
        SystemPrintln( "gcThreshold=" + Memory.gcThreshold().toString() )

        # ---------- S1: suspended coroutine frame local ----------
        function s1WorkerFn = function()
        {
            Int32 v = 111
            string s = "S1-" + v.toString()
            yield;
            ret s == "S1-111"
        }
        Task t1 = spawn s1WorkerFn()
        Coroutine.yieldNow()
        SystemPrintln( "S1 worker parked, objects=" + Memory.objectCount().toString() )
        Int32 freed1 = Memory.collect()
        SystemPrintln( "S1 collectFreed=" + freed1.toString() + " objectsAfter=" + Memory.objectCount().toString() )
        bool r1 = Coroutine.awaitTask( t1 ) as bool
        check( "S1 suspended frame local survives GC", r1 )

        # ---------- S2: nested-call suspend (leaf callee rows + ancestor caller rows) ----------
        function s2WorkerFn = function()
        {
            Int32 v = 33
            string sAnc = "S2-anc-" + v.toString()
            string sLeaf = s2Leaf()
            ret ( sAnc == "S2-anc-33" ) && ( sLeaf == "S2-leaf-22" )
        }
        Task t2 = spawn s2WorkerFn()
        Coroutine.yieldNow()
        SystemPrintln( "S2 worker parked in nested call, objects=" + Memory.objectCount().toString() )
        Int32 freed2 = Memory.collect()
        SystemPrintln( "S2 collectFreed=" + freed2.toString() + " objectsAfter=" + Memory.objectCount().toString() )
        bool r2 = Coroutine.awaitTask( t2 ) as bool
        check( "S2 nested-frame rows survive GC", r2 )

        # ---------- S3: dead coroutine ret_values (late await after collect) ----------
        function s3WorkerFn = function()
        {
            Int32 v = 444
            string s = "S3-" + v.toString()
            ret s
        }
        Task t3 = spawn s3WorkerFn()
        Coroutine.yieldNow()
        SystemPrintln( "S3 worker dead, ret_values held, objects=" + Memory.objectCount().toString() )
        Int32 freed3 = Memory.collect()
        SystemPrintln( "S3 collectFreed=" + freed3.toString() + " objectsAfter=" + Memory.objectCount().toString() )
        string r3 = Coroutine.awaitTask( t3 ) as string
        check( "S3 dead-coroutine ret_values survive GC", r3 == "S3-444" )

        SystemPrintln( "cycles=" + Memory.GcCycleCount().toString() )
        SystemPrintln( "===== CoroGCProbe end =====" )
    }
    CompileBefore()
    {
    }
    CompileAfter()
    {
    }
}
