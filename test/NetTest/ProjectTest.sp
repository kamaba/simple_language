Project
{
    _main_()
    {
        # 端口独立分配（避免组间干扰 / 重复运行的 TIME_WAIT 残留）：
        #   A=19301/19302/19399  C=19306-19308  D=19311-19313
        #   G=19314/19315        E=19321        F=19322-19324
        #   B=19303-19305        H=19325        T=19341-19344
        TcpBasicTest.fun()
        NetFrameTest.fun()
        UdpEchoTest.fun()
        NetStreamComposeTest.fun()
        NetConcurrentTest.fun()
        NetCloseTest.fun()
        NetIsolateTest.fun()
        NetTimeoutTest.fun()
        # NetSuspendTest 必须最后：B3 守卫用例依赖 root 返回后
        # vm_net_outstanding_count()>0 维持 VM 存活（NET_DESIGN.md ADR-7），
        # 需此前各组协程均已结束，避免在途协程干扰守卫判定
        NetSuspendTest.fun()
    }
    CompileBefore()
    {
    }
    CompileAfter()
    {
    }
}
