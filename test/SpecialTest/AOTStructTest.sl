# ============================================================
# AOTStructTest - data/class 跨 AOT ABI 编组测试
# （MLIR_AOT_LLVM_STRUCT_DESIGN.md §5 实现Validation）
#
# 验证点（AOT ABI kind：0=i64、1=f64、2=struct 原生缓冲、3=objref）：
#   1. kind=2 data 参数过界：C 侧 marshal 到原生缓冲 + 调用后回写源对象
#   2. AOT 体内 data 成员访问：GEP 直读原生缓冲（Load/StoreNotStaticField）
#   3. retSlot=2 struct 返回：ret 预置协议（§5.6）+ C 侧 skeleton 物化
#   4. 桥 kind=2（§5.7）：AOT 内 CallStatic 传 data 给解释器，
#      身份缓存命中 + 双向同步（进桥 buf→obj，出桥 obj→buf）
#   5. kind=3 class 引用透传：AOT 体内不访问成员，转传给桥
#   6. 嵌套 data 成员（ptr-32 trick）：链式读 + 链式写
#
# 每个 @AOT 函数配一个同名 Vm 后缀的解释器对照版本。
# ============================================================

# 二维向量：全标量 data（fastPath 布局）
data AotVec2
{
    x = 0.0
    y = 0.0
}

# 质点：嵌套 data 成员（layout slot=3 嵌套内联，ptr-32 trick 覆盖）
data AotParticle
{
    mass = 1.0
    pos = AotVec2(){ x = 0.0, y = 0.0 }
    vel = AotVec2(){ x = 0.0, y = 0.0 }
}

# 引用类型：kind=3 objref 透传用
class AotBox
{
    v = 0.0
}

class AOTStructMath
{
    # ── 1) kind=2 参数 + 成员读写 + 调用后回写（void 返回）──
    @AOT()
    static VecScale( AotVec2 v, double s )
    {
        v.x = v.x * s
        v.y = v.y * s
    }

    # 非 AOT 对照（CVM 解释执行，data 按引用传递）
    static VecScaleVm( AotVec2 v, double s )
    {
        v.x = v.x * s
        v.y = v.y * s
    }

    # ── 2) struct 返回（ret 预置协议 §5.6：epilogue 展平拷贝到
    #        ret->data，C 侧 skeleton_new + unmarshal 物化新对象）──
    @AOT()
    static AotVec2 VecScaled( AotVec2 v, double s )
    {
        v.x = v.x * s
        v.y = v.y * s
        ret v
    }

    static AotVec2 VecScaledVm( AotVec2 v, double s )
    {
        v.x = v.x * s
        v.y = v.y * s
        ret v
    }

    # ── 3) 双 struct 参数纯读 + 标量返回（GEP 直读，无写回）──
    @AOT()
    static double VecDot( AotVec2 a, AotVec2 b )
    {
        ret a.x * b.x + a.y * b.y
    }

    static double VecDotVm( AotVec2 a, AotVec2 b )
    {
        ret a.x * b.x + a.y * b.y
    }

    # ── 4) 桥 kind=2：AOT 反调解释器（CallStatic），data 参数
    #        经身份缓存还原为源对象，返回值走 kind=1 ──
    static double VecDotBridge( AotVec2 a, AotVec2 b )
    {
        ret a.x * b.x + a.y * b.y
    }

    @AOT()
    static double VecDotViaVm( AotVec2 a, AotVec2 b )
    {
        ret VecDotBridge( a, b )
    }

    # ── 5) 桥 kind=2 双向同步：解释器突变 data 后 AOT 再读回
    #        （进桥 native→obj 同步，出桥 obj→native 同步）──
    static BumpVm( AotVec2 v )
    {
        v.x = v.x + 1.0
    }

    @AOT()
    static double BumpAndRead( AotVec2 v )
    {
        BumpVm( v )
        ret v.x
    }

    # ── 6) 嵌套 data 成员链式读（ptr-32 trick）──
    @AOT()
    static double ParticleEnergy( AotParticle p )
    {
        ret 0.5 * p.mass * ( p.vel.x * p.vel.x + p.vel.y * p.vel.y )
    }

    static double ParticleEnergyVm( AotParticle p )
    {
        ret 0.5 * p.mass * ( p.vel.x * p.vel.x + p.vel.y * p.vel.y )
    }

    # ── 7) 嵌套 data 成员链式写（StoreNotStaticField 接收
    #        ptr-32 值：GEP[32+innerOffset] 落在 buf+memberOffset）──
    @AOT()
    static ParticleStep( AotParticle p, double dt )
    {
        p.pos.x = p.pos.x + p.vel.x * dt
        p.pos.y = p.pos.y + p.vel.y * dt
    }

    static ParticleStepVm( AotParticle p, double dt )
    {
        p.pos.x = p.pos.x + p.vel.x * dt
        p.pos.y = p.pos.y + p.vel.y * dt
    }

    # ── 8) kind=3 objref 透传：AOT 体内不访问 class 成员，
    #        仅把引用转传给桥内解释器方法 ──
    static double ReadBoxV( AotBox b )
    {
        ret b.v
    }

    @AOT()
    static double PassBoxThrough( AotBox b )
    {
        ret ReadBoxV( b )
    }
}
