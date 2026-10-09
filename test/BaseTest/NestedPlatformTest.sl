# NestedPlatformTest - smoke test for the P1.7 Environment.Platform form:
# nested namespace + top-level enum + class-in-namespace static getter,
# full chain (Front compile + CVM run).
#
# Verifies the building blocks of design doc §13.2 / §13.7:
#   namespace Environment { namespace Platform { enum os } class current }

namespace NestedNS
{
    namespace Inner
    {
        public enum kind extends int
        {
            unknown = 0
            one     = 1
            two     = 2
        }
    }

    public class cur extends Object
    {
        public static get Int32 os()
        {
            ret 2
        }

        public static get string note()
        {
            ret "nested-ns"
        }
    }
}

NestedPlatformTest
{
    static fun()
    {
        SystemPrintln("========== NestedPlatformTest (start) ==========")

        # 1. nested-namespace enum member access (4-segment chain)
        SystemPrintln("kind.one.value = " + NestedNS.Inner.kind.one.value.toString())
        SystemPrintln("kind.two.value = " + NestedNS.Inner.kind.two.value.toString())

        # 2. class-in-namespace static getter (no parens, 3-segment chain)
        SystemPrintln("cur.os = " + NestedNS.cur.os.toString())
        SystemPrintln("cur.note = " + NestedNS.cur.note)

        # 3. Int32 == enum Member compare (P1.6 bug2 path, nested-ns form)
        if (NestedNS.cur.os == NestedNS.Inner.kind.two)
        {
            SystemPrintln("compare: cur.os == kind.two OK")
        }
        else
        {
            SystemPrintln("compare: cur.os != kind.two FAIL")
        }

        # 4. enum Member as method arg (P1.6 bug3 path, nested-ns form)
        NestedPlatformTest.echo( NestedNS.Inner.kind.one )

        # 5. enum Member .value as arg (explicit unwrap, via local var)
        Int32 twoValue = NestedNS.Inner.kind.two.value
        NestedPlatformTest.echo( twoValue )

        SystemPrintln("========== NestedPlatformTest (end) ==========")
    }

    static echo( Int32 v )
    {
        SystemPrintln("echo v = " + v.toString())
    }
}
