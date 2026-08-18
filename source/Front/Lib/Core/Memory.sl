# =========================================================================
# Memory - Memory management and garbage collection API.
#
# Inspired by:
#   CLR  : System.GC (Collect, KeepAlive, GetTotalMemory, GetGeneration)
#   Dart : WeakReference, Finalizer, implicit generational GC
#   Go   : runtime.GC, SetGCPercent, tri-color mark-sweep
#
# The VM supports two per-object management modes:
#   Manual – the caller controls lifetime via Retain/Release/Free.
#            The GC will NOT sweep objects marked as manual.
#   Auto   – the tri-color GC traces and sweeps the object automatically.
#
# Free() and Release() require the object to be in Manual mode first
# (via Memory.Manual(obj)).  Calling them on an Auto-managed object
# is a no-op and returns false.
#
# Global mode (Memory.SetMode) controls whether the GC runs at all.
#   SetMode(false) – GC disabled (pure manual management).
#   SetMode(true)  – GC enabled, auto-collects when pool exceeds threshold.
# =========================================================================
public class Memory
{
    # ---------------------------------------------------------------
    # Mode constants (mirror VM_MEM_MODE_* in C).
    # ---------------------------------------------------------------
    public const Int32 MODE_MANUAL = 0
    public const Int32 MODE_GC     = 1

    # ---------------------------------------------------------------
    # Per-object mode control.
    # ---------------------------------------------------------------

    # Switch an object to manual management.  After this call the GC
    # will not trace or sweep the object; the caller is responsible for
    # calling Release() or Free() when done.
    # Returns 1 on success.
    public static Int32 manual( object obj )
    {
        ret SystemMemoryManual( obj )
    }

    # Restore an object to automatic (GC) management.  The GC will
    # resume tracing and may sweep the object when it becomes unreachable.
    # Returns 1 on success.
    public static Int32 auto( object obj )
    {
        ret SystemMemoryAuto( obj )
    }

    # Check whether an object is currently in manual management mode.
    # Returns true if manual, false if auto-managed.
    public static bool isManual( object obj )
    {
        ret SystemMemoryIsManual( obj ) != 0
    }

    # ---------------------------------------------------------------
    # Reference counting (manual management).
    # ---------------------------------------------------------------

    # Get the current reference count of an object.
    public static Int32 refCount( object obj )
    {
        ret SystemMemoryRefCount( obj )
    }

    # Increment the reference count (like CLR WeakReference.TrackResurrection
    # or Objective-C retain).  Returns 1 on success.
    public static Int32 retain( object obj )
    {
        ret SystemMemoryRetain( obj )
    }

    # Decrement the reference count; when it reaches 0 the object is freed.
    # Requires Manual mode.  Returns 1 on success, 0 if rejected (auto mode).
    public static Int32 release( object obj )
    {
        ret SystemMemoryRelease( obj )
    }

    # Unconditionally free the object immediately.
    # Requires Manual mode.  Returns 1 on success, 0 if rejected (auto mode).
    public static Int32 free( object obj )
    {
        ret SystemMemoryFree( obj )
    }

    # ---------------------------------------------------------------
    # GC control (CLR-inspired: GC.Collect, GC.GetTotalMemory).
    # ---------------------------------------------------------------

    # Force a full GC cycle (stop-the-world tri-color mark-sweep).
    # Returns the number of objects freed.
    public static Int32 collect()
    {
        ret SystemMemoryCollect()
    }

    # Force a GC cycle only if the object pool size is >= threshold.
    # Returns the number of objects freed (0 if not triggered).
    public static Int32 collect( Int32 threshold )
    {
        ret SystemMemoryCollectThreshold( threshold )
    }

    # Set the GC auto-trigger threshold.  When the object pool grows
    # past this size, a collection is automatically triggered on the
    # next allocation (only in GC mode).
    # Returns 1 on success.
    public static Int32 setGcThreshold( Int32 threshold )
    {
        ret SystemMemorySetGcThreshold( threshold )
    }

    # Get the current GC auto-trigger threshold.
    public static Int32 gcThreshold()
    {
        ret SystemMemoryGetGcThreshold()
    }

    # Set the global memory mode.
    #   Memory.MODE_MANUAL (0) – GC disabled.
    #   Memory.MODE_GC     (1) – GC enabled.
    # Returns 1 on success.
    public static Int32 setMode( Int32 mode )
    {
        ret SystemMemorySetMode( mode )
    }

    # ---------------------------------------------------------------
    # Statistics (CLR-inspired: GC.CollectionCount, GC.GetTotalMemory).
    # ---------------------------------------------------------------

    # Total number of objects currently in the object pool.
    public static Int32 objectCount()
    {
        ret SystemMemoryGetObjectCount()
    }

    # Total number of GC cycles performed.
    public static Int32 GcCycleCount()
    {
        ret SystemMemoryGetGcCycleCount()
    }

    # Number of objects freed in the most recent GC cycle.
    public static Int32 gcFreedCount()
    {
        ret SystemMemoryGetGcFreedCount()
    }

    # Total objects ever allocated (cumulative).
    public static Int32 totalAllocated()
    {
        ret SystemMemoryGetTotalAllocated()
    }

    # Total objects ever freed, including manual free/release and GC sweep.
    public static Int32 totalFreed()
    {
        ret SystemMemoryGetTotalFreed()
    }

    # ---------------------------------------------------------------
    # Strong / weak references (moved from Object.sl).
    # ---------------------------------------------------------------

    # Strong reference: increments the refcount and returns the object
    # identity pointer.  Pairs with Release() for manual lifetime control.
    public static object ref( object obj )
    {
        ret SystemObjectRef( obj )
    }

    # ---------------------------------------------------------------
    # Weak references (Dart-inspired: WeakReference, Finalizer).
    # ---------------------------------------------------------------

    # Register a weak reference to obj.  The returned handle is the
    # object pointer itself; use IsWeakRefValid to check if it is
    # still alive.  When the object is freed, the weak ref is
    # automatically invalidated.
    public static object weakRef( object obj )
    {
        ret SystemMemoryWeakRef( obj )
    }

    # Check whether a weak reference is still valid (the target object
    # has not been freed).  Returns true if valid.
    public static bool isWeakRefValid( object obj )
    {
        ret SystemMemoryIsWeakRefValid( obj ) != 0
    }

    # ---------------------------------------------------------------
    # CLR-inspired: GC.KeepAlive.
    # Keeps an object reachable past the call site, preventing the GC
    # from collecting it before this point.  Increments the refcount
    # so the object survives even in manual mode until explicitly released.
    # ---------------------------------------------------------------
    public static void keepAlive( object obj )
    {
        SystemMemoryKeepAlive( obj )
    }

    # ---------------------------------------------------------------
    # Object cloning (CLR-inspired: ICloneable, MemberwiseClone).
    # ---------------------------------------------------------------

    # Creates a shallow copy of the object.  The clone has the same
    # runtime type and member values as the original.  Reference-type
    # members share the same targets (shallow copy, not deep).
    # The clone is added to the GC pool and returned.
    public static object clone( object obj )
    {
        ret SystemMemoryClone( obj )
    }
}
