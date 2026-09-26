# MiTest - mimalloc host 堆接管回归用例（P6，证据探针见 test/MiProbe）。
# 同一份用例在两种构建变体下都必须全 PASS，按 mimallocEnabled() 自动分派：
#   host 默认（VM_MIMALLOC_DISABLE=1）：探针全部 0/false 编译剔除语义，
#       Memory.alloc/freeNative 走 libc 兜底。
#   VM_MIMALLOC_ENABLE=ON：mimalloc 全局接管 base_malloc（SBA 超块、LOS、
#       运行时结构全部落其上），native 分配/释放精确体现在 current 字节上。
# 统计口径注意（上游 mimalloc v3.5 源码语义）：
#   malloc_requested 只累计非 huge 块、free 递减被上游注释（free.c），
#   即 requested 是累计口径且不随 free 回落 —— 本用例只断言其不倒退；
#   current（malloc_normal + malloc_huge）才是存活字节的可靠口径。
import Std;

MiTest
{
    static check( string name, bool cond )
    {
        if ( cond )
        {
            Console.println( "  [PASS] " + name )
        }
        else
        {
            Console.println( "  [FAIL] " + name )
        }
    }

    static fun()
    {
        Console.println( "===== MiTest.fun (mimalloc host allocator) =====" )
        if ( Memory.mimallocEnabled() )
        {
            testTakeover()
        }
        else
        {
            testDisabled()
        }
        Console.println( "===== MiTest end =====" )
    }

    # ---- host 默认变体：探针编译剔除，全部 0/false ----
    static testDisabled()
    {
        Console.println( "--- variant: disabled (libc malloc) ---" )
        check( "enabled() == false", Memory.mimallocEnabled() == false )
        check( "version() == 0", Memory.mimallocVersion() == 0 )
        check( "currentBytes() == 0", Memory.mimallocCurrentBytes() == 0 )
        check( "peakBytes() == 0", Memory.mimallocPeakBytes() == 0 )
        check( "requestedBytes() == 0", Memory.mimallocRequestedBytes() == 0 )
        check( "committedBytes() == 0", Memory.mimallocCommittedBytes() == 0 )
        check( "reservedBytes() == 0", Memory.mimallocReservedBytes() == 0 )
        # native 路径走 libc 兜底照常可用
        Int64 p = Memory.alloc( 4096 )
        check( "alloc(4096) != 0", p != 0 )
        Memory.writeI64( p, 0, 9000000000 )
        check( "i64 roundtrip on libc heap", Memory.readInt64( p, 0 ) == 9000000000 )
        check( "freeNative(p)", Memory.freeNative( p ) )
        # collect 在禁用态是安全 no-op，统计保持全 0
        Memory.mimallocCollect( true )
        check( "collect() no-op, still 0", Memory.mimallocCurrentBytes() == 0 )
    }

    # ---- mimalloc 接管变体：统计真实，alloc/free 精确反映 ----
    static testTakeover()
    {
        Console.println( "--- variant: enabled (mimalloc takeover) ---" )
        check( "enabled() == true", Memory.mimallocEnabled() )
        check( "version() >= 30500 (v3.5)", Memory.mimallocVersion() >= 30500 )
        Int64 baseCur = Memory.mimallocCurrentBytes()
        Int64 basePeak = Memory.mimallocPeakBytes()
        Int64 baseReq = Memory.mimallocRequestedBytes()
        # VM 自身运行时结构全部落在 mimalloc 堆上（全局接管的直接证据）
        check( "baseline current > 0 (VM itself on mimalloc)", baseCur > 0 )
        check( "baseline peak >= current", basePeak >= baseCur )
        check( "committed > 0", Memory.mimallocCommittedBytes() > 0 )
        check( "reserved >= committed", Memory.mimallocReservedBytes() >= Memory.mimallocCommittedBytes() )
        # native 分配走 mi_zalloc：current 精确上涨（1M + 4M）
        Int64 p1 = Memory.alloc( 1048576 )
        Int64 p2 = Memory.alloc( 4194304 )
        check( "alloc 1M+4M != 0", p1 != 0 && p2 != 0 )
        Memory.writeI64( p2, 0, 1234567890123 )
        check( "i64 roundtrip on mimalloc block", Memory.readInt64( p2, 0 ) == 1234567890123 )
        Int64 midCur = Memory.mimallocCurrentBytes()
        check( "current rises >= 5MiB after 1M+4M", midCur >= baseCur + 5242880 )
        check( "peak >= current", Memory.mimallocPeakBytes() >= midCur )
        # requested 为累计口径（free 不递减、huge 块不计入），只断言不倒退
        check( "requested not decreased", Memory.mimallocRequestedBytes() >= baseReq )
        # 释放 + 归还 OS：current 回落（留 1MiB 余量吸收 check 打印的零星分配）
        Memory.freeNative( p1 )
        Memory.freeNative( p2 )
        Memory.mimallocCollect( true )
        Int64 endCur = Memory.mimallocCurrentBytes()
        check( "current falls >= 4MiB after free+collect", endCur + 4194304 <= midCur )
        # collect(false) 轻量路径同样安全
        Memory.mimallocCollect( false )
        check( "collect(false) safe", Memory.mimallocCurrentBytes() <= midCur )
    }
}
