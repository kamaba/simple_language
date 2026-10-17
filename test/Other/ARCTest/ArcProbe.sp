Project
{
    println( txt )
    {
        SystemPrintln( txt )
    }

    static ArcProbeT makeObj()
    {
        ArcProbeT tmp = new();
        tmp.x = 7;
        ret tmp;
    }

    static int makeClaimed()
    {
        ArcProbeNS.ArcNoCtor tmp = new();
        tmp.x = 99;
        ret Memory.refCount(tmp);
    }

    # P3-A: return transfer (StoreReturn hold)
    static int returnTransferCase()
    {
        ArcProbeT o0 = makeObj();
        int rc = Memory.refCount(o0);
        int x = o0.x;
        ret rc * 10 + x;
    }

    # P3-B: container transfer (vm_array_try_store_value hold)
    static int listTransferCase()
    {
        List<ArcProbeT> xs = List<ArcProbeT>();
        ArcProbeT o1 = makeObj();
        int rcBefore = Memory.refCount(o1);
        xs.add(o1);
        int rcAfter = Memory.refCount(o1);
        int len = xs.length;
        ret rcAfter * 100 + rcBefore * 10 + len;
    }

    # P3-C: array slot transfer (vm_array_try_store_slot_bytes hold)
    static int arrayTransferCase()
    {
        Array<ArcProbeT> arr = Array<ArcProbeT>(1);
        ArcProbeT o2 = makeObj();
        int rcBefore = Memory.refCount(o2);
        arr[0] = o2;
        int rcAfter = Memory.refCount(o2);
        int x2 = arr[0].x;
        ret rcAfter * 100 + rcBefore * 10 + x2;
    }

    # P3-D: static field transfer (StoreStaticField hold)
    static int staticFieldTransferCase()
    {
        ArcProbeT o3 = makeObj();
        int rcBefore = Memory.refCount(o3);
        ArcProbeNS.ArcHolder.slot = o3;
        int rcAfter = Memory.refCount(o3);
        ret rcAfter * 10 + rcBefore;
    }

    # P3-E: instance field transfer (StoreNotStaticField hold)
    static int instanceFieldTransferCase()
    {
        ArcProbeNS.ArcBox box = new();
        ArcProbeT o4 = makeObj();
        int rcBefore = Memory.refCount(o4);
        box.item = o4;
        int rcAfter = Memory.refCount(o4);
        ret rcAfter * 10 + rcBefore;
    }

    # P3-F: global/Project field transfer (StoreGlobal hold)
    ArcProbeT gslot = null;
    static int globalTransferCase()
    {
        ArcProbeT o5 = makeObj();
        int rcBefore = Memory.refCount(o5);
        global.gslot = o5;
        int rcAfter = Memory.refCount(o5);
        ret rcAfter * 10 + rcBefore;
    }

    # P3-G: closure capture transfer (context slot hold)
    static int closureTransferCase()
    {
        ArcProbeT o6 = makeObj();
        int rcBefore = Memory.refCount(o6);
        function touchCaptured()
        {
            o6.x = 55;
        }
        int rcAfter = Memory.refCount(o6);
        touchCaptured();
        int x6 = o6.x;
        ret rcAfter * 1000 + rcBefore * 100 + x6;
    }

    _main_()
    {
        global.println("--- ARC P2/P3 transfer probe ---");
        int c0 = Memory.objectCount();

        int rA = returnTransferCase();
        int rB = listTransferCase();
        int rC = arrayTransferCase();
        int rD = staticFieldTransferCase();
        int rE = instanceFieldTransferCase();
        int rF = globalTransferCase();
        int rG = closureTransferCase();
        global.println("A return transfer   : " + rA.toString() + " (want 17)");
        global.println("B list.add transfer : " + rB.toString() + " (want 211)");
        global.println("C array slot xfer   : " + rC.toString() + " (want 217)");
        global.println("D static field xfer : " + rD.toString() + " (want 21)");
        global.println("E field transfer    : " + rE.toString() + " (want 21)");
        global.println("F global field xfer : " + rF.toString() + " (want 21)");
        global.println("G closure capture   : " + rG.toString() + " (want 2155)");

        int rcSlot = Memory.refCount(ArcProbeNS.ArcHolder.slot);
        int rcG = Memory.refCount(global.gslot);
        global.println("D static slot rc    : " + rcSlot.toString() + " (want 1)");
        global.println("F global slot rc    : " + rcG.toString() + " (want 1)");

        int rc = makeClaimed();
        global.println("P2 refCount(claimed) = " + rc.toString());

        Memory.release(ArcProbeNS.ArcHolder.slot);
        Memory.release(global.gslot);

        int c1 = Memory.objectCount();
        global.println("objectCount before = " + c0.toString());
        global.println("objectCount after  = " + c1.toString());
        global.println("ARC transfer no-leak: (c1 <= c0) -> " + (c1 <= c0).toString());
    }
}
