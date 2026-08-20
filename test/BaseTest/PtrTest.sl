PtrTest
{
    static fun()
    {
        SystemPrintln("========== PtrTest (start) ==========")

        # 1. Ptr.size() - pointer size on current platform (4 or 8)
        SystemPrintln("--- Ptr.size() ---")
        sz = Ptr.size()
        SystemPrintln("Ptr.size() = " + SystemConvertString(sz))
        if (sz == 4 || sz == 8)
        {
            SystemPrintln("Ptr.size() is 4 or 8: OK")
        }
        else
        {
            SystemPrintln("ERROR: Ptr.size() unexpected value!")
        }

        # 2. Ptr.zero() - zero/null pointer
        SystemPrintln("--- Ptr.zero() ---")
        z = Ptr.zero()
        SystemPrintln("Ptr.zero() = " + SystemConvertString(z.toInt64()))
        if (z.toInt64() == 0)
        {
            SystemPrintln("Ptr.zero() is 0: OK")
        }
        else
        {
            SystemPrintln("ERROR: Ptr.zero() is not 0!")
        }

        # 3. Default constructor
        SystemPrintln("--- Ptr() default constructor ---")
        p0 = Ptr()
        SystemPrintln("Ptr() = " + SystemConvertString(p0.toInt64()))
        if (p0.toInt64() == 0)
        {
            SystemPrintln("Ptr() address is 0: OK")
        }
        else
        {
            SystemPrintln("ERROR: Ptr() address is not 0!")
        }

        # 4. Alloc, write, read, free - Int32 round trip
        SystemPrintln("--- alloc/writeInt32/readInt32 ---")
        p1 = Ptr.alloc(64)
        addr1 = p1.toInt64()
        SystemPrintln("alloc(64) address = " + SystemConvertString(addr1))
        if (addr1 != 0)
        {
            SystemPrintln("alloc returned non-zero: OK")
        }
        else
        {
            SystemPrintln("ERROR: alloc returned 0!")
        }

        p1.writeInt32(0, 42)
        v1 = p1.readInt32(0)
        SystemPrintln("writeInt32(0, 42) -> readInt32(0) = " + SystemConvertString(v1))
        if (v1 == 42)
        {
            SystemPrintln("Int32 round trip 42: OK")
        }
        else
        {
            SystemPrintln("ERROR: Int32 round trip failed!")
        }

        # Write at different offset
        p1.writeInt32(4, 100)
        v2 = p1.readInt32(4)
        SystemPrintln("writeInt32(4, 100) -> readInt32(4) = " + SystemConvertString(v2))
        if (v2 == 100)
        {
            SystemPrintln("Int32 round trip 100 at offset 4: OK")
        }
        else
        {
            SystemPrintln("ERROR: Int32 round trip at offset 4 failed!")
        }

        # 5. Byte read/write
        SystemPrintln("--- writeByte/readByte ---")
        p1.writeByte(8, 255)
        b1 = p1.readByte(8)
        SystemPrintln("writeByte(8, 255) -> readByte(8) = " + SystemConvertString(b1))
        if (b1 == 255)
        {
            SystemPrintln("Byte round trip 255: OK")
        }
        else
        {
            SystemPrintln("ERROR: Byte round trip failed!")
        }

        p1.writeByte(9, 0)
        b2 = p1.readByte(9)
        SystemPrintln("writeByte(9, 0) -> readByte(9) = " + SystemConvertString(b2))
        if (b2 == 0)
        {
            SystemPrintln("Byte round trip 0: OK")
        }
        else
        {
            SystemPrintln("ERROR: Byte round trip 0 failed!")
        }

        # 6. Int64 read/write
        SystemPrintln("--- writeInt64/readInt64 ---")
        p1.writeInt64(16, 9876543210)
        l1 = p1.readInt64(16)
        SystemPrintln("writeInt64(16, 9876543210) -> readInt64(16) = " + SystemConvertString(l1))
        if (l1 == 9876543210)
        {
            SystemPrintln("Int64 round trip 9876543210: OK")
        }
        else
        {
            SystemPrintln("ERROR: Int64 round trip failed!")
        }

        # 7. Float64 read/write
        SystemPrintln("--- writeFloat64/readFloat64 ---")
        p1.writeFloat64(24, 3.14159)
        f1 = p1.readFloat64(24)
        SystemPrintln("writeFloat64(24, 3.14159) -> readFloat64(24) = " + SystemConvertString(f1))
        if (f1 == 3.14159)
        {
            SystemPrintln("Float64 round trip 3.14159: OK")
        }
        else
        {
            SystemPrintln("ERROR: Float64 round trip failed!")
        }

        # 8. Pointer arithmetic: add / subtract
        SystemPrintln("--- add/subtract ---")
        p2 = p1.add(8)
        SystemPrintln("p1.add(8) = " + SystemConvertString(p2.toInt64()))
        if (p2.toInt64() == addr1 + 8)
        {
            SystemPrintln("add(8) correct: OK")
        }
        else
        {
            SystemPrintln("ERROR: add(8) incorrect!")
        }

        p3 = p2.subtract(4)
        SystemPrintln("p2.subtract(4) = " + SystemConvertString(p3.toInt64()))
        if (p3.toInt64() == addr1 + 4)
        {
            SystemPrintln("subtract(4) correct: OK")
        }
        else
        {
            SystemPrintln("ERROR: subtract(4) incorrect!")
        }

        # 9. equals
        SystemPrintln("--- equals ---")
        p4 = Ptr(addr1)
        if (p1.equals(p4))
        {
            SystemPrintln("p1.equals(Ptr(addr1)): OK")
        }
        else
        {
            SystemPrintln("ERROR: equals failed!")
        }

        if (!p1.equals(p2))
        {
            SystemPrintln("p1 not equals p2: OK")
        }
        else
        {
            SystemPrintln("ERROR: different pointers should not be equal!")
        }

        # 10. toString
        SystemPrintln("--- toString ---")
        ts = p1.toString()
        SystemPrintln("p1.toString() = " + ts)
        if (SystemStringLength(ts) > 4)
        {
            SystemPrintln("toString has content: OK")
        }
        else
        {
            SystemPrintln("ERROR: toString too short!")
        }

        # 11. Free memory
        SystemPrintln("--- free ---")
        ok = Ptr.free(p1)
        SystemPrintln("Ptr.free(p1) = " + SystemConvertString(ok))
        if (ok)
        {
            SystemPrintln("free returned true: OK")
        }
        else
        {
            SystemPrintln("ERROR: free failed!")
        }

        SystemPrintln("========== Ptr (raw) end ==========")

        # ===============================================================
        # Ptr<T> typed object pointer tests
        # ===============================================================
        SystemPrintln("========== Ptr<T> (typed) start ==========")

        # 12. Create typed pointer from object
        SystemPrintln("--- Ptr<PtrData>(obj) ---")
        pdata = PtrData()
        pdata.a = 1
        pdata.b = 2
        pdata.c = 3
        ptp = Ptr<PtrData>( pdata )
        objAddr = ptp.toInt64()
        SystemPrintln("Ptr<PtrData>(pdata) address = " + SystemConvertString(objAddr))
        if (objAddr != 0)
        {
            SystemPrintln("Typed pointer non-zero: OK")
        }
        else
        {
            SystemPrintln("ERROR: typed pointer is zero!")
        }

        # 13. Read fields through typed pointer
        SystemPrintln("--- readInt32 via Ptr<T> ---")
        ra = ptp.readInt32(0)
        SystemPrintln("readInt32(0) = " + SystemConvertString(ra))
        if (ra == 1)
        {
            SystemPrintln("read field a = 1: OK")
        }
        else
        {
            SystemPrintln("ERROR: field a mismatch! got " + SystemConvertString(ra))
        }

        rb = ptp.readInt32(4)
        SystemPrintln("readInt32(4) = " + SystemConvertString(rb))
        if (rb == 2)
        {
            SystemPrintln("read field b = 2: OK")
        }
        else
        {
            SystemPrintln("ERROR: field b mismatch! got " + SystemConvertString(rb))
        }

        rc = ptp.readInt64(8)
        SystemPrintln("readInt64(8) = " + SystemConvertString(rc))
        if (rc == 3)
        {
            SystemPrintln("read field c = 3: OK")
        }
        else
        {
            SystemPrintln("ERROR: field c mismatch! got " + SystemConvertString(rc))
        }

        # 14. Write fields through typed pointer
        SystemPrintln("--- writeInt32 via Ptr<T> ---")
        ptp.writeInt32(0, 42)
        ptp.writeInt32(4, 99)
        ptp.writeInt64(8, 7777)

        wa = ptp.readInt32(0)
        wb = ptp.readInt32(4)
        wc = ptp.readInt64(8)
        SystemPrintln("after write: a=" + SystemConvertString(wa) + " b=" + SystemConvertString(wb) + " c=" + SystemConvertString(wc))
        if (wa == 42 && wb == 99 && wc == 7777)
        {
            SystemPrintln("write fields via Ptr<T>: OK")
        }
        else
        {
            SystemPrintln("ERROR: write fields failed!")
        }

        # 15. Verify the original object reflects changes
        SystemPrintln("--- verify object reflects changes ---")
        if (pdata.a == 42 && pdata.b == 99 && pdata.c == 7777)
        {
            SystemPrintln("Object reflects pointer writes: OK")
        }
        else
        {
            SystemPrintln("ERROR: object does not reflect changes! a=" + SystemConvertString(data.a) + " b=" + SystemConvertString(data.b))
        }

        # 16. Recover object from typed pointer
        SystemPrintln("--- get() recover object ---")
        recovered = ptp.get()
        SystemPrintln("recovered.a = " + SystemConvertString(recovered.a))
        if (recovered.a == 42)
        {
            SystemPrintln("get() recovered object: OK")
        }
        else
        {
            SystemPrintln("ERROR: get() failed!")
        }

        # 17. toString
        SystemPrintln("--- toString ---")
        ts2 = ptp.toString()
        SystemPrintln("Ptr<T>.toString() = " + ts2)
        if (SystemStringLength(ts2) > 6)
        {
            SystemPrintln("Ptr<T> toString has content: OK")
        }
        else
        {
            SystemPrintln("ERROR: Ptr<T> toString too short!")
        }

        SystemPrintln("========== Ptr<T> (typed) end ==========")
    }
}

# Helper class for Ptr<T> testing.
# Field layout in member_data:
#   offset 0: Int32 a (4 bytes)
#   offset 4: Int32 b (4 bytes)
#   offset 8: Int64 c (8 bytes)
PtrData
{
    Int32 a = 0
    Int32 b = 0
    Int64 c = 0
}
