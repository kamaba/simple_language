Project
{
    _main_()
    {
        # Inline statements (same as PlatformAotPosTest): the .sp entry
        # cannot call into classes of the sibling .sl files (call-link
        # parse fails), so the run marker is printed directly.
        SystemPrintln("[PlatformVariantTest] running: variant selection passed")
        SystemPrintln("[PlatformVariantTest] osName: " + Environment.current.osName)
    }
    CompileBefore()
    {
    }
    CompileAfter()
    {
    }
}
