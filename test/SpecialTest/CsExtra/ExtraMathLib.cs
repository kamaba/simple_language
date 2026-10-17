// ExtraMathLib.cs —— jsonc plugins.csharp_mono.references 示例源（C#5 兼容）：
// 预编译为同目录 ExtraMathLib.dll（Framework csc /target:library），jsonc
// plugins.csharp_mono.references 段引入后由 Front 拷入插件 libDir——
// 编译期 csc /r: 引用（块内 using ExtraMathLib; 类型可解析）、运行期 mono
// assemblies_path 按裸名从插件 dll 同目录解析加载（与 SLAtSign.dll 同机制）。
// 与 sources 的区别：references 引入**预编译程序集**（不参与同批编译，
// 改动需重编 dll）；sources 是**源码**（每趟导出与块体同批重编）。
// 注意：命名空间勿与内部类同名（ExtraMath.Twice 会被 C# 解析为
// 命名空间成员查找而报 CS0234——实测踩坑）。
using System;

namespace ExtraMathLib
{
    public static class ExtraMath
    {
        public static int Twice( int x )
        {
            return x * 2;
        }

        public static double Scale( double v, double k )
        {
            return v * k;
        }
    }
}
