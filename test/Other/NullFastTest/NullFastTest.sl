NullFastClass
{
    int val = 100

    _init_(int v)
    {
        this.val = v
    }

    int GetVal()
    {
        ret this.val
    }
}

NullFastTest
{
    static fun()
    {
        global.println("===== Null fast branch test =====")

        NullFastClass obj = NullFastClass(42)

        # 1. != null : non-null value, true path
        if obj != null
        {
            global.println("obj != null -> true")
        }
        else
        {
            global.println("obj != null -> false")
        }

        # 2. == null : non-null value, false path
        if obj == null
        {
            global.println("obj == null -> true")
        }
        else
        {
            global.println("obj == null -> false")
        }

        NullFastClass nullobj = null

        # 3. == null : null value, true path
        if nullobj == null
        {
            global.println("nullobj == null -> true")
        }
        else
        {
            global.println("nullobj == null -> false")
        }

        # 4. != null : null value, false path
        if nullobj != null
        {
            global.println("nullobj != null -> true")
        }
        else
        {
            global.println("nullobj != null -> false")
        }

        # 5. ?. null conditional field access (non-null receiver)
        int v1 = obj?.val
        global.println("obj?.val = " + v1)

        # 5b. ?. null conditional field access (null receiver)
        int v1n = nullobj?.val
        global.println("nullobj?.val = " + v1n)

        # 5c. ?. null conditional method call (non-null receiver)
        int v2 = obj?.GetVal()
        global.println("obj?.GetVal() = " + v2)

        # 5d. ?. null conditional method call (null receiver)
        int v2n = nullobj?.GetVal()
        global.println("nullobj?.GetVal() = " + v2n)

        # 6. ?? null coalescing (null left)
        string s1 = null
        string r1 = s1 ?? "default"
        global.println("s1 ?? default = " + r1)

        # 7. ?? null coalescing (non-null left)
        string s2 = "hello"
        string r2 = s2 ?? "default"
        global.println("s2 ?? default = " + r2)

        # 8. nested: method call guarded by != null
        if nullobj != null
        {
            global.println("nullobj.GetVal() = " + nullobj.GetVal())
        }
        else
        {
            global.println("nullobj is null, guard works")
        }

        global.println("===== Null fast branch test end =====")
    }
}
