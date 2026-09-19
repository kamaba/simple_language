# Debug 调试系统门面（DEBUG_SYSTEM_DESIGN.md v4）
# 链路：Debug.* → Front 特译 Debug IR（compile.debug 才生成，§7.2）
#       → *.module.json 携带 optimizeLevel → C VM 分派 → 落帧 / 查询
# 采样类调用点由 Front 特译为 opcode 119/120/121；compile.debug 关闭时整句消除。
# 查询类为普通系统调用：compile.debug 关闭或 optimizeLevel>=2 时返回空/0，不报错。
public class Debug extends Object
{
    # 单值 / data 整表监视（T1/T2）——特译 opcode 121 mode=0
    public static void watch( object value, string tag )
    {
        SystemDebugWatch( value, tag )
    }

    # data/class 单成员监视（T3/T4）——特译 opcode 121 mode=1
    public static void watch( object target, string member, string tag )
    {
        SystemDebugWatchMember( target, member, tag )
    }

    # 有效帧总数（o2 快速路径 / debug 关闭时为 0）
    public static Int32 frameCount()
    {
        ret SystemDebugGetFrameCount()
    }

    # 单帧文本（越界返回空串）
    public static string getFrame( Int32 index )
    {
        ret SystemDebugGetFrame( index )
    }

    # 最新帧文本（监听回调配套快取）
    public static string lastFrame()
    {
        ret SystemDebugLastFrame()
    }

    # 帧文本截断上限（超长标注 ...(len=N)，<=0 恢复默认 256）
    public static void setMaxTextLength( Int32 maxLength )
    {
        SystemDebugSetMaxTextLength( maxLength )
    }

    # ---- P2：区间（DEBUG_SYSTEM_DESIGN §5.2）----

    # 区间开始（进入 scope_chain）——特译 opcode 119
    public static void begin( string tag )
    {
        SystemDebugBegin( tag )
    }

    # 区间结束（计入耗时统计）——特译 opcode 120
    public static void end( string tag )
    {
        SystemDebugEnd( tag )
    }

    # 行边界标记（frameTable 行轴，P6 启用）
    public static void mark( string tag )
    {
        SystemDebugMark( tag )
    }

    # ---- P2：采样控制（§5.3）----

    # 全局：前 n 次命中不采样（预热跳过）
    public static void setSkip( Int32 n )
    {
        SystemDebugSetSkip( n )
    }

    # 全局软暂停采样（查询类不受影响）
    public static void pause()
    {
        SystemDebugPause()
    }

    # 恢复采样
    public static void resume()
    {
        SystemDebugResume()
    }

    # 单监视点启用
    public static void enable( string tag )
    {
        SystemDebugEnableWatch( tag )
    }

    # 单监视点停用
    public static void disable( string tag )
    {
        SystemDebugDisableWatch( tag )
    }

    # ---- P2：查询（§5.4）----

    # 第 index 帧与前帧对比（仅 data/class 目标，无前帧返回原文本）
    public static string diff( Int32 index )
    {
        ret SystemDebugDiffFrame( index )
    }

    # 按标签（+可选区间链前缀）找帧索引列表
    public static Array<Int32> find( string tag, string scope = "" )
    {
        Array<Int32> r = null
        r = SystemDebugFindFrame( tag, scope )
        ret r
    }

    # 某标签的值序列（画帧表用）
    public static Array<string> series( string tag )
    {
        Array<string> r = null
        r = SystemDebugSeries( tag )
        ret r
    }

    # 区间耗时统计 → JSON
    public static string stats( string tag )
    {
        ret SystemDebugGetIntervalStats( tag )
    }

    # 当前区间链，如 "main > loop"
    public static string scopeChain()
    {
        ret SystemDebugGetScopeChain()
    }

    # ---- P3：条件监视（§5.1）----

    # 断言监视：cond==false 时计入违规并落违规帧（kind=3）——特译 opcode 121 mode=2
    public static void watchAssert( object value, string tag, bool cond )
    {
        SystemDebugWatchAssert( value, tag, cond )
    }

    # 调用链过滤：仅在含 caller 的调用链上采样——特译 opcode 121 mode=3
    public static void watchIn( object value, string tag, string caller )
    {
        SystemDebugWatchIn( value, tag, caller )
    }

    # 违规累计计数（§5.4，watchAssert cond==false 次数）
    public static Int32 assertViolations()
    {
        ret SystemDebugGetAssertViolations()
    }

    # ---- P4：监听回调（§5.7/§10）----

    # 订阅落帧通知（全帧）：每次落帧（watch/成员 watch/违规帧/mark）后调用
    # callback()（无参）。返回订阅号（1 起，用于 unlisten）；表满返回 -1。
    public static Int32 listen( object callback )
    {
        ret SystemDebugListen( callback )
    }

    # 订阅落帧通知（单标签）：仅 tag 匹配的帧触发。返回订阅号；表满 -1。
    public static Int32 listen( string tag, object callback )
    {
        ret SystemDebugListenLabel( tag, callback )
    }

    # 退订成功 true；id 不存在 / 已退订 false（重复 unlisten 必然 false）
    public static bool unlisten( Int32 id )
    {
        ret SystemDebugUnlisten( id )
    }

    # ---- P5：栈查看（§5.6/§9.3）----

    # 当前操作数栈快照：每槽 {index,type,value} 的 JSON 数组；无栈 "[]"
    # （命名 stackView：stack 为容器构造糖保留名，不可作成员名）
    public static string stackView()
    {
        ret SystemDebugGetOperandStack()
    }

    # 调用帧列表 [{index,method,line}]（栈顶→栈底）；无帧 "[]"
    public static string frames()
    {
        ret SystemDebugGetFrameInfo()
    }

    # 第 depth 层调用帧（0=当前执行帧）locals+args；越界 "[]"
    public static string frameData( Int32 depth )
    {
        ret SystemDebugGetFrameLocals( depth )
    }

    # 某帧上指定变量/参数值文本（先 locals 后 args）；未找到 ""
    public static string frameVar( Int32 depth, string name )
    {
        ret SystemDebugGetFrameVar( depth, name )
    }

    # ---- P6：Trace 与帧表（§5.4/§5.5/§13.6）----

    # 记一条日志：自动附用户帧 method/line 与调用链摘要（"a > b > c"，
    # 最外层在前）。采样类语义：pause/skip 期间静默丢弃。
    public static void log( string msg )
    {
        SystemDebugLog( msg )
    }

    # 日志流文本：每条 log 一行 "#<行> <method>:<line> | <链> | <msg>"，
    # 顺序与落点一致；无日志返回 ""
    public static string getTraceLog()
    {
        ret SystemDebugGetTraceLog()
    }

    # 帧表：行=mark 行边界（升序）、列=labels、单元格=该行处该 label
    # 最近一帧的值文本（无匹配 "-"）。首行表头 "row | <label>..."；
    # labels 空 / 无帧返回 ""
    public static string frameTable( Array<string> labels )
    {
        ret SystemDebugGetFrameTable( labels )
    }
}
