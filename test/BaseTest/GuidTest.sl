GuidTest
{
    static fun()
    {
        SystemPrintln("========== GuidTest (start) ==========")

        # 1. static newGuid()
        SystemPrintln("--- Guid.newGuid() ---")
        g1 = Guid.newGuid()
        SystemPrintln("newGuid() = " + g1)

        g2 = Guid.newGuid()
        SystemPrintln("newGuid() = " + g2)

        if (g1 != g2)
        {
            SystemPrintln("Two GUIDs are different: OK")
        }
        else
        {
            SystemPrintln("ERROR: Two GUIDs are the same!")
        }

        # 2. Guid length check (standard format: 36 chars)
        len = SystemStringLength(g1)
        SystemPrintln("GUID length = " + SystemConvertString(len))
        if (len == 36)
        {
            SystemPrintln("GUID length is 36: OK")
        }
        else
        {
            SystemPrintln("WARNING: GUID length is not 36")
        }

        # 3. Default constructor
        SystemPrintln("--- Guid() constructor ---")
        guidObj = Guid()
        SystemPrintln("Guid().value = " + guidObj.value)
        SystemPrintln("Guid().toString() = " + guidObj.toString())

        # 4. Constructor with string
        SystemPrintln("--- Guid(string) constructor ---")
        fixedGuid = Guid("12345678-1234-1234-1234-123456789abc")
        SystemPrintln("Guid(fixed).value = " + fixedGuid.value)
        SystemPrintln("Guid(fixed).toString() = " + fixedGuid.toString())

        if (fixedGuid.value == "12345678-1234-1234-1234-123456789abc")
        {
            SystemPrintln("Fixed GUID value matches: OK")
        }
        else
        {
            SystemPrintln("ERROR: Fixed GUID value mismatch!")
        }

        SystemPrintln("========== GuidTest (end) ==========")
    }
}
