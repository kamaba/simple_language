EnvironmentTest
{
    static fun()
    {
        SystemPrintln("========== EnvironmentTest (start) ==========")

        # 1. getVariable / setVariable
        SystemPrintln("--- Environment variable get/set ---")
        Environment.setVariable("SL_TEST_ENV", "hello_sl")
        val = Environment.getVariable("SL_TEST_ENV")
        SystemPrintln("getVariable(SL_TEST_ENV) = " + val)

        path = Environment.getVariable("PATH")
        if (path != null && SystemStringLength(path) > 0)
        {
            SystemPrintln("getVariable(PATH) has value, length = " + SystemConvertString(SystemStringLength(path)))
        }
        else
        {
            SystemPrintln("getVariable(PATH) is empty")
        }

        notFound = Environment.getVariable("SL_NO_SUCH_VAR_12345")
        SystemPrintln("getVariable(nonexistent) = '" + notFound + "'")

        # 2. currentDirectory
        SystemPrintln("--- Current directory ---")
        cwd = Environment.currentDirectory()
        SystemPrintln("currentDirectory() = " + cwd)

        # 3. tickCount / nowMillis
        SystemPrintln("--- Timing ---")
        tick = Environment.tickCount()
        SystemPrintln("tickCount() = " + SystemConvertString(tick))

        nowMs = Environment.nowMillis()
        SystemPrintln("nowMillis() = " + SystemConvertString(nowMs))

        # 4. toString
        SystemPrintln("Environment.toString() = " + nowMs.toString())

        SystemPrintln("========== EnvironmentTest (end) ==========")
    }
}
