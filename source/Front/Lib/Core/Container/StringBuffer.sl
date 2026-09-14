# ============================================================================
# Core/Container/StringBuffer.sl — 可变字符串缓冲（Java 兼容命名变体）
#
# 与 StringBuilder 非重复关系：Java 语义中 Buffer 为线程安全变体、
# Builder 为快速变体；本语言 C VM 为协作式单线程协程（无抢占），
# 方法内无挂起点即天然安全，无需锁——两者安全性等价。
# 故本类为 StringBuilder 的纯继承别名（构造器与方法全部物理继承自
# 父类，参见 Front/Core/MetaClass.cs HandleExtendMemberFunction），
# 仅保留类名以兼容 Java 风格调用习惯。
#
# 用法与能力完全等同 StringBuilder：
#   var sb = StringBuffer( "a" )
#   sb.append( 1 ).append( true ).appendLine( "x" )
#   sb.appendFormat( "{0}-{1}", "a", 2 )
#   global.println( sb.toString() )
#
# 生命周期注意（继承自 StringBuilder）：底层 ByteBuf 注册表无 GC
# 自动回收，长生命周期程序建议用毕显式 release()。
# ============================================================================

public class StringBuffer extends StringBuilder
{
}
