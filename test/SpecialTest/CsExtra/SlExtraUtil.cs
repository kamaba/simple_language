// SlExtraUtil.cs —— jsonc plugins.csharp_mono.sources 示例（C#5 兼容）：
// 由 Front 读入文件内容随 Build 请求下发，csharp_mono 插件把它与全部
// @csharp_mono(){} 块体条目同批合并编译进 SLAtSign.dll——块内代码
// 头区写 using SLExtra; 即可调用（无需手工放进插件 lib 目录）。
// 约束：类名/命名空间勿与镜像 class（SLAtSign 命名空间）撞名——
// 同库合并编译，冲突由 csc 报错拒收整批。
namespace SLExtra
{
    // 静态工具类：块内直接调用静态方法
    public static class SlExtraUtil
    {
        public static int Sum( int a, int b )
        {
            return a + b;
        }

        public static double Weight( int a, double b )
        {
            return a * b;
        }
    }

    // 结构类型（class）同样可声明：块内可 new SlPoint 当普通 C# 对象用
    public class SlPoint
    {
        public int X;
        public int Y;

        public int Manhattan()
        {
            return ( X < 0 ? -X : X ) + ( Y < 0 ? -Y : Y );
        }
    }
}
