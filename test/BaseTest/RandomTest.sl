RandomTest
{
    static fun()
    {
        SystemPrintln("========== RandomTest (start) ==========")

        # 1. Seeded constructor - deterministic sequence
        SystemPrintln("--- Seeded constructor (deterministic) ---")
        Random r1 = new(42)
        Int32 a1 = r1.nextInt(100)
        Int32 a2 = r1.nextInt(100)
        Int32 a3 = r1.nextInt(100)
        SystemPrintln("seed=42: " + a1.toString() + ", " + a2.toString() + ", " + a3.toString())

        # Same seed produces same sequence
        Random r2 = new(42)
        Int32 b1 = r2.nextInt(100)
        Int32 b2 = r2.nextInt(100)
        Int32 b3 = r2.nextInt(100)
        if (a1 == b1 && a2 == b2 && a3 == b3)
        {
            SystemPrintln("Same seed -> same sequence: OK")
        }
        else
        {
            SystemPrintln("Same seed -> same sequence: FAIL")
        }

        # Different seed produces different sequence
        Random r3 = new(999)
        Int32 c1 = r3.nextInt(100)
        if (a1 != c1)
        {
            SystemPrintln("Different seed -> different value: OK")
        }
        else
        {
            SystemPrintln("Different seed -> different value: FAIL (unlikely but possible)")
        }

        # 2. nextInt(max) - range check [0, max)
        SystemPrintln("--- nextInt(max) range [0, 100) ---")
        Random rng = new(12345)
        bool rangeOk = true
        int i = 0
        while (i < 10)
        {
            Int32 v = rng.nextInt(100)
            if (v < 0 || v >= 100)
            {
                rangeOk = false
            }
            i = i + 1
        }
        if (rangeOk)
        {
            SystemPrintln("nextInt(100) range [0, 100): OK")
        }
        else
        {
            SystemPrintln("nextInt(100) range: FAIL")
        }

        # 3. nextInt(min, max) - range check [10, 20)
        SystemPrintln("--- nextInt(min, max) range [10, 20) ---")
        rangeOk = true
        i = 0
        while (i < 10)
        {
            Int32 v = rng.nextInt(10, 20)
            if (v < 10 || v >= 20)
            {
                rangeOk = false
            }
            i = i + 1
        }
        if (rangeOk)
        {
            SystemPrintln("nextInt(10, 20) range [10, 20): OK")
        }
        else
        {
            SystemPrintln("nextInt(10, 20) range: FAIL")
        }

        # 4. nextInt edge cases
        SystemPrintln("--- nextInt edge cases ---")
        Random er = new(7)
        Int32 z = er.nextInt(1)
        SystemPrintln("nextInt(1) = " + z.toString() + " (always 0)")
        if (z == 0)
        {
            SystemPrintln("nextInt(1) == 0: OK")
        }
        else
        {
            SystemPrintln("nextInt(1) == 0: FAIL")
        }
        Int32 zz = er.nextInt(0)
        SystemPrintln("nextInt(0) = " + zz.toString() + " (guard returns 0)")
        if (zz == 0)
        {
            SystemPrintln("nextInt(0) == 0: OK")
        }
        else
        {
            SystemPrintln("nextInt(0) == 0: FAIL")
        }

        # 5. nextFloat() - range check [0.0, 1.0)
        SystemPrintln("--- nextFloat() range [0.0, 1.0) ---")
        rangeOk = true
        i = 0
        while (i < 10)
        {
            Num f = rng.nextFloat()
            if (f < 0.0 || f >= 1.0)
            {
                rangeOk = false
            }
            i = i + 1
        }
        if (rangeOk)
        {
            SystemPrintln("nextFloat() range [0.0, 1.0): OK")
        }
        else
        {
            SystemPrintln("nextFloat() range: FAIL")
        }

        # 6. nextFloat(min, max) - range check [5.0, 10.0)
        SystemPrintln("--- nextFloat(min, max) range [5.0, 10.0) ---")
        rangeOk = true
        i = 0
        while (i < 10)
        {
            Num f = rng.nextFloat(5.0, 10.0)
            if (f < 5.0 || f >= 10.0)
            {
                rangeOk = false
            }
            i = i + 1
        }
        if (rangeOk)
        {
            SystemPrintln("nextFloat(5.0, 10.0) range [5.0, 10.0): OK")
        }
        else
        {
            SystemPrintln("nextFloat(5.0, 10.0) range: FAIL")
        }

        # 7. nextBool() - returns true or false
        SystemPrintln("--- nextBool() ---")
        Random br = new(555)
        bool sawTrue = false
        bool sawFalse = false
        i = 0
        while (i < 20)
        {
            if (br.nextBool())
            {
                sawTrue = true
            }
            else
            {
                sawFalse = true
            }
            i = i + 1
        }
        if (sawTrue && sawFalse)
        {
            SystemPrintln("nextBool() saw both true and false: OK")
        }
        else
        {
            SystemPrintln("nextBool() did not see both values: FAIL")
        }

        # 8. Static methods
        SystemPrintln("--- Static methods ---")
        Int32 si = Random.randomInt(1000)
        SystemPrintln("Random.randomInt(1000) = " + si.toString())
        if (si >= 0 && si < 1000)
        {
            SystemPrintln("randomInt(1000) range: OK")
        }
        else
        {
            SystemPrintln("randomInt(1000) range: FAIL")
        }
        Num sf = Random.randomFloat()
        SystemPrintln("Random.randomFloat() = " + sf.toString())
        if (sf >= 0.0 && sf < 1.0)
        {
            SystemPrintln("randomFloat() range: OK")
        }
        else
        {
            SystemPrintln("randomFloat() range: FAIL")
        }

        # 9. Distribution sanity (count values in buckets)
        SystemPrintln("--- Distribution sanity (nextInt(3), 100 draws) ---")
        Random dr = new(31415)
        int buckets = 0
        int count0 = 0
        int count1 = 0
        int count2 = 0
        int countOther = 0
        i = 0
        while (i < 100)
        {
            Int32 v = dr.nextInt(3)
            if (v == 0) { count0 = count0 + 1 }
            elif (v == 1) { count1 = count1 + 1 }
            elif (v == 2) { count2 = count2 + 1 }
            else { countOther = countOther + 1 }
            i = i + 1
        }
        SystemPrintln("count[0]=" + count0.toString() + " count[1]=" + count1.toString() + " count[2]=" + count2.toString())
        if (countOther == 0 && count0 > 10 && count1 > 10 && count2 > 10)
        {
            SystemPrintln("Distribution roughly uniform: OK")
        }
        else
        {
            SystemPrintln("Distribution check: FAIL")
        }

        SystemPrintln("========== RandomTest (end) ==========")
    }
}
