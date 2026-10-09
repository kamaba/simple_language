ArcProbeNS.ArcCtor
{
    int x = 0
    _init_()
    {
        this.x = 1
    }
}

ArcProbeNS.ArcNoCtor
{
    int x = 0
}

ArcProbeNS.ArcFieldInit
{
    int x = 0
    int y = 42
}

ArcProbeNS.ArcHolder
{
    static ArcProbeT slot = null
}

ArcProbeNS.ArcBox
{
    ArcProbeT item = null
}

ArcProbeT
{
    int x = 0
}
