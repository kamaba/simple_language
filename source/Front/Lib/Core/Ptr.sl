# =========================================================================
# Ptr - Pointer abstraction for native memory access.
#
# Inspired by:
#   CLR  : System.IntPtr (Zero, Size, Add, Subtract, ToInt32, ToInt64)
#   Rust : raw pointers (*const T, *mut T)
#   Go   : unsafe.Pointer
#
# Provides a safe wrapper around native memory addresses with typed
# read/write operations.  Memory must be allocated via Ptr.alloc() and
# freed via Ptr.free() to avoid leaks.
#
# Usage:
#   p = Ptr.alloc(64)       # allocate 64 bytes
#   p.writeInt32(0, 42)     # write Int32 at offset 0
#   v = p.readInt32(0)      # read Int32 at offset 0 -> 42
#   Ptr.free(p)             # release memory
# =========================================================================
public class Ptr extends Object
{
    # ---------------------------------------------------------------
    # Fields
    # ---------------------------------------------------------------

    # The native memory address (64-bit to support both 32/64-bit platforms).
    Int64 _address = 0

    # ---------------------------------------------------------------
    # Constructors
    # ---------------------------------------------------------------

    # Default constructor: zero address (null pointer).
    override void _init_()
    {
        this._address = 0
    }

    # Construct from a 64-bit address.
    void _init_( Int64 address )
    {
        this._address = address
    }

    # Construct from a 32-bit address.
    void _init_( Int32 address )
    {
        this._address = SystemConvertInt64( address )
    }

    # ---------------------------------------------------------------
    # Static factory and utility methods
    # ---------------------------------------------------------------

    # Returns a Ptr with value zero (null pointer).
    public static Ptr zero()
    {
        ret Ptr( 0 )
    }

    # Size of a pointer on the current platform (4 or 8 bytes).
    public static Int32 size()
    {
        ret SystemPtrSize()
    }

    # Allocate a block of native memory of the given size.
    # Returns a Ptr pointing to the allocated memory.
    public static Ptr alloc( Int32 byteCount )
    {
        addr = SystemPtrAlloc( byteCount )
        ret Ptr( addr )
    }

    # Free native memory previously allocated by alloc().
    # Returns true on success.
    public static bool free( Ptr p )
    {
        ret SystemPtrFree( p.toInt64() ) != 0
    }

    # ---------------------------------------------------------------
    # Conversion methods
    # ---------------------------------------------------------------

    # Returns the address as a 64-bit integer.
    public Int64 toInt64()
    {
        ret this._address
    }

    # Returns the address as a 32-bit integer (may truncate on 64-bit).
    public Int32 toInt32()
    {
        ret SystemConvertInt32( this._address )
    }

    # ---------------------------------------------------------------
    # Equality
    # ---------------------------------------------------------------

    # Check if this pointer equals another.
    public bool equals( Ptr other )
    {
        ret this._address == other.toInt64()
    }

    # ---------------------------------------------------------------
    # Pointer arithmetic
    # ---------------------------------------------------------------

    # Returns a new Ptr offset by the given number of bytes.
    public Ptr add( Int32 offset )
    {
        ret Ptr( this._address + SystemConvertInt64( offset ) )
    }

    # Returns a new Ptr offset backwards by the given number of bytes.
    public Ptr subtract( Int32 offset )
    {
        ret Ptr( this._address - SystemConvertInt64( offset ) )
    }

    # ---------------------------------------------------------------
    # Typed read operations
    # ---------------------------------------------------------------

    # Read a single byte at the given offset.
    public Int32 readByte( Int32 offset )
    {
        ret SystemPtrReadByte( this._address, offset )
    }

    # Read a 32-bit signed integer at the given offset.
    public Int32 readInt32( Int32 offset )
    {
        ret SystemPtrReadInt32( this._address, offset )
    }

    # Read a 64-bit signed integer at the given offset.
    public Int64 readInt64( Int32 offset )
    {
        ret SystemPtrReadInt64( this._address, offset )
    }

    # Read a 64-bit floating-point number at the given offset.
    public Num readFloat64( Int32 offset )
    {
        ret SystemPtrReadFloat64( this._address, offset )
    }

    # ---------------------------------------------------------------
    # Typed write operations
    # ---------------------------------------------------------------

    # Write a single byte at the given offset. Returns 1 on success.
    public Int32 writeByte( Int32 offset, Int32 value )
    {
        ret SystemPtrWriteByte( this._address, offset, value )
    }

    # Write a 32-bit signed integer at the given offset. Returns 1 on success.
    public Int32 writeInt32( Int32 offset, Int32 value )
    {
        ret SystemPtrWriteInt32( this._address, offset, value )
    }

    # Write a 64-bit signed integer at the given offset. Returns 1 on success.
    public Int32 writeInt64( Int32 offset, Int64 value )
    {
        ret SystemPtrWriteInt64( this._address, offset, value )
    }

    # Write a 64-bit floating-point number at the given offset. Returns 1 on success.
    public Int32 writeFloat64( Int32 offset, Num value )
    {
        ret SystemPtrWriteFloat64( this._address, offset, value )
    }

    # ---------------------------------------------------------------
    # Object overrides
    # ---------------------------------------------------------------

    # Returns the address as a decimal string.
    override string toString()
    {
        ret "Ptr(" + SystemConvertString( this._address ) + ")"
    }
}

# =========================================================================
# Ptr<T> - Typed pointer for accessing object internals.
#
# Inspired by:
#   C++  : T* (typed raw pointer to object)
#   CLR  : System.Runtime.InteropServices.Marshal (PtrToStructure, StructureToPtr)
#   Rust : *const T (typed raw pointer with field access)
#
# Wraps a VM object's internal address, allowing typed read/write access
# to the object's member_data buffer at specific offsets.  This enables
# direct field manipulation without going through the regular method
# dispatch path.
#
# Usage:
#   class Vec3 { Int32 x = 0; Int32 y = 0; Int32 z = 0 }
#   v = Vec3()
#   p = Ptr<Vec3>( v )       # typed pointer from object
#   p.writeInt32( 0, 10 )    # set x = 10 (offset 0)
#   p.writeInt32( 4, 20 )    # set y = 20 (offset 4)
#   p.writeInt32( 8, 30 )    # set z = 30 (offset 8)
#   obj = p.get()            # recover the original object
# =========================================================================
public class Ptr<T> extends Object
{
    # ---------------------------------------------------------------
    # Fields
    # ---------------------------------------------------------------

    # The internal VMObject address.
    Int64 _objAddr = 0

    # ---------------------------------------------------------------
    # Constructors
    # ---------------------------------------------------------------

    # Construct from an object instance: stores the internal address.
    void _init_( T obj )
    {
        this._objAddr = SystemPtrFromObject( obj )
    }

    # Construct from a raw address (UInt32).
    void _init_( UInt32 address )
    {
        this._objAddr = SystemConvertInt64( address )
    }

    # Construct from a raw address (Int64).
    void _init_( Int64 address )
    {
        this._objAddr = address
    }

    # ---------------------------------------------------------------
    # Object recovery
    # ---------------------------------------------------------------

    # Recover the original typed object from the pointer.
    public T get()
    {
        ret SystemPtrToObject( this._objAddr )
    }

    # ---------------------------------------------------------------
    # Conversion
    # ---------------------------------------------------------------

    # Returns the internal object address as Int64.
    public Int64 toInt64()
    {
        ret this._objAddr
    }

    # ---------------------------------------------------------------
    # Typed field read operations (read from member_data + offset)
    # ---------------------------------------------------------------

    # Read a single byte at the given field offset.
    public Int32 readByte( Int32 offset )
    {
        ret SystemPtrObjReadByte( this._objAddr, offset )
    }

    # Read a 32-bit integer at the given field offset.
    public Int32 readInt32( Int32 offset )
    {
        ret SystemPtrObjReadInt32( this._objAddr, offset )
    }

    # Read a 64-bit integer at the given field offset.
    public Int64 readInt64( Int32 offset )
    {
        ret SystemPtrObjReadInt64( this._objAddr, offset )
    }

    # Read a 64-bit float at the given field offset.
    public Num readFloat64( Int32 offset )
    {
        ret SystemPtrObjReadFloat64( this._objAddr, offset )
    }

    # ---------------------------------------------------------------
    # Typed field write operations (write to member_data + offset)
    # ---------------------------------------------------------------

    # Write a single byte at the given field offset. Returns 1 on success.
    public Int32 writeByte( Int32 offset, Int32 value )
    {
        ret SystemPtrObjWriteByte( this._objAddr, offset, value )
    }

    # Write a 32-bit integer at the given field offset. Returns 1 on success.
    public Int32 writeInt32( Int32 offset, Int32 value )
    {
        ret SystemPtrObjWriteInt32( this._objAddr, offset, value )
    }

    # Write a 64-bit integer at the given field offset. Returns 1 on success.
    public Int32 writeInt64( Int32 offset, Int64 value )
    {
        ret SystemPtrObjWriteInt64( this._objAddr, offset, value )
    }

    # Write a 64-bit float at the given field offset. Returns 1 on success.
    public Int32 writeFloat64( Int32 offset, Num value )
    {
        ret SystemPtrObjWriteFloat64( this._objAddr, offset, value )
    }

    # ---------------------------------------------------------------
    # Object overrides
    # ---------------------------------------------------------------

    override string toString()
    {
        ret "Ptr<T>(" + SystemConvertString( this._objAddr ) + ")"
    }
}
