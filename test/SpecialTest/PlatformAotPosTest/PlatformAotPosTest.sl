# ============================================================
# PlatformAotPosTest - AOT target triple positive test (P2.5, S11.4)
#
# export.aot.features = "+sse4.2,+avx2" matches
# platform.require.cpu all = ["sse4.2"] (both sides normalize the ISA
# alias "sse4.2" -> "sse42" before comparison) -> no Error 20037,
# compile completes, and llc receives -mattr=+sse4.2,+avx2
# (Info 22115 "AOT: llc target options:" in Front.txt).
#
# AOTMath.Add is an @AOT() candidate (static non-template), so the
# stage 1-3 pipeline also emits aot.mlir and builds aot.dll via the
# real tools\llvm toolchain.
# ============================================================

class AOTMath
{
    # AOT 候选（非模板静态函数）
    @AOT()
    static int Add( int a, int b )
    {
        ret a + b
    }
}
