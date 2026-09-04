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

    # ---------------------------------------------------------------
    # Native memory (moved from FFI.NativeMemory).
    # Build C native content (arrays / scalars / structs) dynamically.
    # cvm: memory_system_method.c (SystemMemoryNative* system calls).
    #
    # Conventions:
    #   - Addresses are passed as Int64 (0 = null / invalid).
    #   - Type names accept SL names ("Int32"/"string"...) and FFI short
    #     names ("i32"/"utf8"...).
    #   - Writes: i8==u8, i16==u16, i32==u32, i64==u64==ptr (bit-pattern
    #     aliases); reads must pick the sign/zero-extending variant.
    #   - Structs use C natural alignment (offset=round_up(cur,align),
    #     tail padded to the largest field alignment), e.g. fields
    #     "i32,i64,f32,f64" (max 64 fields).
    #   - writeUtf8 stores the SL string's data pointer (the string must
    #     stay alive; native side is read-only); copyUtf8 copies bytes
    #     (self-owned, no lifetime constraint).
    # ---------------------------------------------------------------

    # ---------------------------------------------------------------
    # Allocation / free / type size.
    # ---------------------------------------------------------------

    # Allocate byteCount bytes of zeroed native memory, return the
    # address (0 = failure).
    public static Int64 alloc( Int32 byteCount )
    {
        ret SystemMemoryNativeAlloc( byteCount )
    }

    # Free a block allocated by alloc (double-free is undefined).
    # Named freeNative to avoid clashing with free(object) above.
    public static bool freeNative( Int64 addr )
    {
        ret SystemMemoryNativeFree( addr )
    }

    # FFI type storage size in bytes: bool/i8/u8/f8=1, i16/u16/f16=2,
    # i32/u32/f32=4, i64/u64/f64/ptr/utf8=8. Unknown type returns 0.
    public static Int32 sizeOf( string typeName )
    {
        ret SystemMemoryNativeSizeOf( typeName )
    }

    # Allocate a C array of count elements (laid out by element type
    # size, zeroed), return the base address.  Access elements with the
    # read/write series + index*sizeOf offsets.
    public static Int64 allocArray( Int32 count, string elemType )
    {
        Int32 sz = Memory.sizeOf( elemType )
        if ( count <= 0 || sz <= 0 )
        {
            ret 0
        }
        ret Memory.alloc( count * sz )
    }

    # ---------------------------------------------------------------
    # Scalar builders (allocate + write, return the address).
    # ---------------------------------------------------------------

    # Build a native Int8 (i8/u8 share the bit pattern).
    public static Int64 newInt8( Int32 value )
    {
        Int64 p = Memory.alloc( 1 )
        if ( p != 0 )
        {
            Memory.writeI8( p, 0, value )
        }
        ret p
    }

    # Build a native Int16 (i16/u16 share the bit pattern).
    public static Int64 newInt16( Int32 value )
    {
        Int64 p = Memory.alloc( 2 )
        if ( p != 0 )
        {
            Memory.writeI16( p, 0, value )
        }
        ret p
    }

    # Build a native Int32 (i32/u32 share the bit pattern).
    public static Int64 newInt32( Int32 value )
    {
        Int64 p = Memory.alloc( 4 )
        if ( p != 0 )
        {
            Memory.writeI32( p, 0, value )
        }
        ret p
    }

    # Build a native Int64 (i64/u64/ptr share the bit pattern).
    public static Int64 newInt64( Int64 value )
    {
        Int64 p = Memory.alloc( 8 )
        if ( p != 0 )
        {
            Memory.writeI64( p, 0, value )
        }
        ret p
    }

    # Build a native Float32.
    public static Int64 newFloat32( Float32 value )
    {
        Int64 p = Memory.alloc( 4 )
        if ( p != 0 )
        {
            Memory.writeF32( p, 0, value )
        }
        ret p
    }

    # Build a native Float64.
    public static Int64 newFloat64( Float64 value )
    {
        Int64 p = Memory.alloc( 8 )
        if ( p != 0 )
        {
            Memory.writeF64( p, 0, value )
        }
        ret p
    }

    # Build a native bool (1 byte).
    public static Int64 newBool( bool value )
    {
        Int64 p = Memory.alloc( 1 )
        if ( p != 0 )
        {
            Memory.writeBool( p, 0, value ? 1 : 0 )
        }
        ret p
    }

    # Build a utf8 string slot (8-byte address slot holding a pointer,
    # not bytes).  Equivalent to newInt64 + writeUtf8.
    public static Int64 newUtf8( string value )
    {
        Int64 p = Memory.alloc( 8 )
        if ( p != 0 )
        {
            Memory.writeUtf8( p, 0, value )
        }
        ret p
    }

    # ---------------------------------------------------------------
    # Reads (addr + byte offset).
    # ---------------------------------------------------------------

    # Read a 1-byte bool.
    public static bool readBool( Int64 addr, Int32 offset )
    {
        ret SystemMemoryNativeReadBool( addr, offset )
    }

    # Read 1 byte signed (sign-extended to Int32).
    public static Int32 readInt8( Int64 addr, Int32 offset )
    {
        ret SystemMemoryNativeReadI8( addr, offset )
    }

    # Read 1 byte unsigned (zero-extended to Int32).
    public static Int32 readUInt8( Int64 addr, Int32 offset )
    {
        ret SystemMemoryNativeReadU8( addr, offset )
    }

    # Read 2 bytes signed (sign-extended to Int32).
    public static Int32 readInt16( Int64 addr, Int32 offset )
    {
        ret SystemMemoryNativeReadI16( addr, offset )
    }

    # Read 2 bytes unsigned (zero-extended to Int32).
    public static Int32 readUInt16( Int64 addr, Int32 offset )
    {
        ret SystemMemoryNativeReadU16( addr, offset )
    }

    # Read 4 bytes signed.
    public static Int32 readInt32( Int64 addr, Int32 offset )
    {
        ret SystemMemoryNativeReadI32( addr, offset )
    }

    # Read 4 bytes unsigned (zero-extended to Int64, keeps > 2^31-1).
    public static Int64 readUInt32( Int64 addr, Int32 offset )
    {
        ret SystemMemoryNativeReadU32( addr, offset )
    }

    # Read 8 bytes signed (u64/ptr bit-pattern alias).
    public static Int64 readInt64( Int64 addr, Int32 offset )
    {
        ret SystemMemoryNativeReadI64( addr, offset )
    }

    # Read 8 bytes unsigned / a native pointer (same as readInt64).
    public static Int64 readUInt64( Int64 addr, Int32 offset )
    {
        ret SystemMemoryNativeReadI64( addr, offset )
    }

    # Read 8 bytes as a native pointer (same as readInt64).
    public static Int64 readPtr( Int64 addr, Int32 offset )
    {
        ret SystemMemoryNativeReadI64( addr, offset )
    }

    # Read a 4-byte single-precision float.
    public static Float32 readFloat32( Int64 addr, Int32 offset )
    {
        ret SystemMemoryNativeReadF32( addr, offset )
    }

    # Read an 8-byte double-precision float.
    public static Float64 readFloat64( Int64 addr, Int32 offset )
    {
        ret SystemMemoryNativeReadF64( addr, offset )
    }

    # Read the char* stored in an 8-byte address slot, copied into an
    # SL string.
    public static string readUtf8( Int64 addr, Int32 offset )
    {
        ret SystemMemoryNativeReadUtf8( addr, offset )
    }

    # ---------------------------------------------------------------
    # Writes (addr + byte offset).
    # ---------------------------------------------------------------

    # Write a 1-byte bool.
    public static bool writeBool( Int64 addr, Int32 offset, Int32 value )
    {
        ret SystemMemoryNativeWriteBool( addr, offset, value )
    }

    # Write 1 byte (i8/u8 share the bit pattern).
    public static bool writeI8( Int64 addr, Int32 offset, Int32 value )
    {
        ret SystemMemoryNativeWriteI8( addr, offset, value )
    }

    # Write 2 bytes (i16/u16 share the bit pattern).
    public static bool writeI16( Int64 addr, Int32 offset, Int32 value )
    {
        ret SystemMemoryNativeWriteI16( addr, offset, value )
    }

    # Write 4 bytes (i32/u32 share the bit pattern).
    public static bool writeI32( Int64 addr, Int32 offset, Int32 value )
    {
        ret SystemMemoryNativeWriteI32( addr, offset, value )
    }

    # Write 8 bytes (i64/u64/ptr share the bit pattern).
    public static bool writeI64( Int64 addr, Int32 offset, Int64 value )
    {
        ret SystemMemoryNativeWriteI64( addr, offset, value )
    }

    # Write a 4-byte single-precision float.
    public static bool writeF32( Int64 addr, Int32 offset, Float32 value )
    {
        ret SystemMemoryNativeWriteF32( addr, offset, value )
    }

    # Write an 8-byte double-precision float.
    public static bool writeF64( Int64 addr, Int32 offset, Float64 value )
    {
        ret SystemMemoryNativeWriteF64( addr, offset, value )
    }

    # Write the SL string's data pointer into an 8-byte address slot
    # (the string must stay alive; native side is read-only; use
    # copyUtf8 when a self-owned copy is needed).
    public static bool writeUtf8( Int64 addr, Int32 offset, string value )
    {
        ret SystemMemoryNativeWriteUtf8( addr, offset, value )
    }

    # Copy string bytes to addr+offset (NUL-terminated, at most
    # maxBytes-1 payload bytes); returns copied bytes (without NUL).
    public static Int32 copyUtf8( Int64 addr, Int32 offset, string value, Int32 maxBytes )
    {
        ret SystemMemoryNativeCopyUtf8( addr, offset, value, maxBytes )
    }

    # ---------------------------------------------------------------
    # Struct layout (C natural alignment).
    # ---------------------------------------------------------------

    # Total struct size (tail padded to the largest field alignment).
    # fields like "i32,i64,f32,f64" (SL or FFI short names, max 64).
    public static Int32 structSize( string fields )
    {
        ret SystemMemoryNativeStructSize( fields )
    }

    # Byte offset of field index (0-based); -1 on bad fields / index.
    public static Int32 structFieldOffset( string fields, Int32 index )
    {
        ret SystemMemoryNativeStructFieldOffset( fields, index )
    }

    # Dynamically build a struct instance (zeroed allocation, no
    # initialization), return the address.  Fill fields with the
    # write/read series + structFieldOffset.
    public static Int64 newStruct( string fields )
    {
        Int32 sz = Memory.structSize( fields )
        if ( sz <= 0 )
        {
            ret 0
        }
        ret Memory.alloc( sz )
    }

    # ---------------------------------------------------------------
    # data <-> Array<object> conversion.
    #
    # Purpose: structured data at the cvm layer can be converted into
    # C struct data via dataToArray (then written to native memory /
    # passed to special functions), and converted back via arrayToData
    # given the per-node type layout.
    # ---------------------------------------------------------------

    # Convert a data object into a new Array<object>, one slot per
    # member node (in declaration order).  Returns null on failure.
    public static Array<object> dataToArray( object obj )
    {
        ret SystemMemoryDataToArray( obj )
    }

    # Build a data object (meta_kind = DATA) from values, laid out by
    # the fields type string (comma-separated node types, e.g.
    # "i32,f64,string,object"; SL or FFI short names, max 64 fields).
    # Array elements are written to the members in order; fields past
    # the array length stay null/zero.  Returns null on failure.
    public static object arrayToData( Array<object> values, string fields )
    {
        ret SystemMemoryArrayToData( values, fields )
    }

    # ---------------------------------------------------------------
    # Named data (data DataName{...}) <-> C struct conversion.
    #
    # Layout: C natural alignment, matching structSize/structFieldOffset
    # rules.  Member slot mapping:
    #   - scalar members (bool/i8..i64/f16..f64): inline, slot width
    #   - string member: 8-byte char* slot (native side read-only)
    #   - nested data member: recursively inlined (C nested struct)
    #   - enum member: 4-byte Int32 slot (underlying constant)
    #   - class member: 8-byte object address slot (SystemPtrFromObject
    #     semantics; read back via SystemPtrToObject-like restore)
    #
    # The returned native address is NOT managed by SL memory
    # management; the caller owns it (free it when done).
    # ---------------------------------------------------------------

    # Serialize a named data instance into a freshly allocated C
    # struct block (zero-filled then filled).  structName is the
    # corresponding C struct definition name, used for logging only --
    # the layout is fully driven by the SL data definition, so both
    # sides must agree on it.  Returns the native address, 0 on
    # failure (non-data object / null nested data member).
    public static Int64 dataToNativeStruct( string structName, object obj )
    {
        ret SystemMemoryDataToNativeStruct( structName, obj )
    }

    # Build a new named data instance from native memory, resolving
    # the data class by typeName (full or short name).  The sugar form
    #     var dn = Memory.nativeStructToData<DataName>( addr )
    # injects "DataName" as typeName at the front end, i.e. it calls
    #     Memory.nativeStructToData( addr, "DataName" )
    # Strings are copied into SL-owned objects; class members are
    # restored by address (object must still be alive).  Returns null
    # on failure (unknown type / bad address / layout mismatch).
    public static object nativeStructToData( Int64 addr, string typeName )
    {
        ret SystemMemoryNativeStructToData( addr, typeName )
    }
}
