import Std;
import Core;

# ============================================================================
# CSharpTest2 — @csharp_mono(){} 内联块移植示例（SampleCompute.cs 端到端）
#
# 移植源：根目录 SampleCompute.cs —— .NET Standard 2.0 计算示例库
# SampleCompute.Calculator.Compute(a, b) 的六步计算链（归一化校验 /
# 几何幂运算 / 位运算进制 / 字符串编码 / 集合聚合 / 格式化扰动）。
#
# 块整编契约下的移植改造（SLLabelParser / SLLabelBuilder）：
#   1) 代码段被包进 Main 方法体 —— 不能定义 C# 类，六个子函数内联展开；
#   2) csc 为 v4.0.30319（C#5 兼容）—— $"" 插值改 string.Format；
#   3) 无 System.Core 引用 —— Linq 的 Max/Average/Sum 改手写循环；
#   4) 出通道仅支持 int/string —— double 结果以 "F6" 定点字符串回传；
#   5) csc 源文件按 UTF-8 BOM 写出 —— 中文异常消息原样保留；
#   6) SL data/class 跨界镜像 —— Front 导出期收集字段布局下发插件，块外
#      生成镜像 C# class（SLAtSign.<SL类型名>），块内按 C# 对象直接点访问
#      （见 testAtSignDataMirror / testAtSignDataNested / testAtSignClassMirror）。
#
# 理论对拍（a=3, b=4，3-4-5 直角三角形）：geo = 5.48 精确（sin=0.8 cos=0.6）；
# 位段 3^4=7 → rotl3=56 → hex 38000000 → %1000003 = 521279；聚合值
# (sum+max)/(avg+1) ≈ 5.999891，加 fingerprint 长度扰动后 result ≈ 6.000x
# ——精确尾数依赖 mono 的 CompareInfo.GetHashCode 实现，断言取 ±0.01 宽窗。
# 注意：本文件 SL 层禁用行内注释（Lexer 单行 # 注释会吞换行，导致成员丢失）。
# ============================================================================

# ---------------- 结构体跨界镜像用例的类型定义（h6） ----------------
# SL data/class 经 Front 导出期布局收集（AtSignLabelBuildManager）下发插件，
# 生成镜像 C# class（SLAtSign 命名空间），块内按 C# 对象直接点访问。
# data 成员为推导形态（显式类型仅匿名 data 支持）：0 -> Int32、0.0d ->
# Float64、"" -> String、false -> Boolean、AtSignInner() -> 嵌套 data。

data AtSignMetrics
{
    width = 0
    height = 0
    ratio = 0.0d
    tag = ""
    active = false
}

data AtSignInner
{
    level = 0
    weight = 0.0d
}

data AtSignOuter
{
    id = 0
    inner = AtSignInner()
}

class AtSignCounter
{
    int step = 0
    int total = 0
}

class CSharpTest2
{
    # 断言计数（供 fun 尾部汇总）
    static int passed = 0
    static int failed = 0

    # 简单断言辅助：打印 PASS / FAIL
    static check( string name, bool cond )
    {
        if ( cond )
        {
            passed = passed + 1
            Console.println( "  [PASS] " + name )
        }
        else
        {
            failed = failed + 1
            Console.println( "  [FAIL] " + name )
        }
    }

    # ---------------- GeometryStep 单步移植（double 入通道 + F6 string 出通道） ----------------
    # 3-4-5 三角形：hyp=5，sin(atan2(4,3))=0.8，cos=0.6 → wave=0.48 → geo=5.48；
    # 浮点误差 ~1e-16 远离 F6 的 1e-6 舍入边界，"5.480000" 可精确断言。
    static testComputeGeometry()
    {
        Console.println( "===== CSharpTest2.testComputeGeometry =====" )
        double a = 3.0
        double b = 4.0
        string r = ""
        @csharp_mono()
        {
            Double a <- $a
            Double b <- $b
            using System;
            using System.Globalization;
            double hyp = Math.Sqrt(a * a + b * b);
            double angle = Math.Atan2(b, a);
            double wave = Math.Sin(angle) * Math.Cos(angle);
            double geo = hyp + wave;
            string $r <- geo.ToString("F6", CultureInfo.InvariantCulture);
        }
        Console.println( "GeometryStep(3,4) = " + r )
        check( "geometry(3,4): 3-4-5 triangle -> geo == 5.480000", r == "5.480000" )
    }

    # ---------------- Compute 全链移植（六子函数内联，块内打印中间量） ----------------
    static testComputeFullChain()
    {
        Console.println( "===== CSharpTest2.testComputeFullChain =====" )
        double a = 3.0
        double b = 4.0
        string result = ""
        @csharp_mono()
        {
            Double a <- $a
            Double b <- $b
            using System;
            using System.Collections.Generic;
            using System.Globalization;
            using System.Text;
            // Step 1: normalize + validate (Math.Abs)
            a = Math.Abs(a);
            b = Math.Abs(b);
            if (a < 1e-9 || b < 1e-9)
            {
                throw new ArgumentException("两个数都必须是非零正数（已取绝对值）。");
            }
            // Step 2: geometry + power (System.Math)
            double hyp = Math.Sqrt(a * a + b * b);
            double angle = Math.Atan2(b, a);
            double wave = Math.Sin(angle) * Math.Cos(angle);
            double geo = hyp + wave;
            double p = Math.Pow(a, 1.0 / 3.0) + Math.Pow(b, 1.0 / 2.0);
            double lg = Math.Log(p + 1.0, 2.0);
            double pw = p * lg;
            // Step 3: bit ops + base conversion (BitConverter / Convert)
            int ia = (int)Math.Round(a);
            int ib = (int)Math.Round(b);
            int xored = ia ^ ib;
            int rotated = (xored << 3) | (xored >> (32 - 3));
            byte[] bytes = BitConverter.GetBytes(rotated);
            string hex = BitConverter.ToString(bytes).Replace("-", "");
            long back = Convert.ToInt64(hex, 16);
            long bit = back % 1000003;
            // Step 4: string + encoding (Encoding / CultureInfo)
            string raw = string.Format(CultureInfo.InvariantCulture, "{0:0.000}|{1:0.000}", a, b);
            byte[] utf8 = Encoding.UTF8.GetBytes(raw);
            string b64 = Convert.ToBase64String(utf8);
            CultureInfo ci = CultureInfo.InvariantCulture;
            int hash = ci.CompareInfo.GetHashCode(b64, CompareOptions.IgnoreCase);
            string fingerprint = hash.ToString(ci);
            // Step 5: aggregate (List + manual max/avg/sum, no Linq on C#5)
            List<double> items = new List<double> { geo, pw, (double)bit };
            double max = items[0];
            double sum = 0.0;
            for (int i = 0; i < items.Count; i++)
            {
                sum += items[i];
                if (items[i] > max) { max = items[i]; }
            }
            double avg = sum / items.Count;
            double aggregated = (sum + max) / (avg + 1.0);
            // Step 6: format + parse + perturb (FinalizeStep)
            string formatted = aggregated.ToString("F6", CultureInfo.InvariantCulture);
            double cleaned = double.Parse(formatted, CultureInfo.InvariantCulture);
            int perturb = fingerprint.Length;
            double v = cleaned + perturb * 0.0001;
            Console.WriteLine("[CSharpTest2] geo=" + geo.ToString("F6", ci) + " pow=" + pw.ToString("F6", ci) + " bit=" + bit.ToString());
            Console.WriteLine("[CSharpTest2] raw='" + raw + "' b64=" + b64 + " fingerprint=" + fingerprint);
            Console.WriteLine("[CSharpTest2] aggregated=" + formatted + " perturb=" + perturb.ToString());
            string $result <- v.ToString("F6", CultureInfo.InvariantCulture);
        }
        Console.println( "Compute(3,4) result = " + result )
        check( "fullchain: Compute(3,4) result F6 string non-empty", result != "" )

        # 块仅一个出通道 -> result 经 string 入通道回传块内 double.Parse 复核
        int ok = 0
        @csharp_mono()
        {
            String s <- $result
            using System;
            using System.Globalization;
            double v = double.Parse(s, CultureInfo.InvariantCulture);
            double d = v - 6.0;
            if (d < 0.0) { d = -d; }
            int flag = 0;
            if (d < 0.01) { flag = 1; }
            $ok <- flag;
        }
        check( "fullchain: result ~ 6.0 (|err| < 0.01)", ok == 1 )
    }

    # ---------------- 参数校验负向（Step 1 throw -> MANAGED_EXC -95 -> try-catch） ----------------
    # a=0 触发移植代码原生的 ArgumentException -> mono_runtime_invoke 抛 ->
    # 插件侧 throw_error(-95) -> VM 异常 -> SL label{}try{}catch{} 捕获
    #（同 CSharpTest.testDegradation 形态，此处异常源是移植的 SampleCompute 逻辑）。
    static int computeThrow( double a, double b )
    {
        @csharp_mono()
        {
            Double a <- $a
            Double b <- $b
            using System;
            a = Math.Abs(a);
            b = Math.Abs(b);
            if (a < 1e-9 || b < 1e-9)
            {
                throw new ArgumentException("两个数都必须是非零正数（已取绝对值）。");
            }
        }
        ret 0
    }

    static testComputeThrow()
    {
        Console.println( "===== CSharpTest2.testComputeThrow =====" )
        int caught = 0
        label computeThrowBlock
        {
            try computeThrow( 0.0, 4.0 )
        }
        catch
        {
            caught = 1
        }
        check( "compute(0,4): validation throws -> caught (MANAGED_EXC -95)", caught == 1 )
    }

    # ---------------- SL data -> C# 镜像 class（h6 结构体跨界映射） ----------------
    # AtSignMetrics 五标量字段（int/double/string/bool）经 CVM 编组成
    # SLLabelObject 树 -> 插件按镜像类布局递归装配 mono 实例 -> 块内当
    # 普通 C# 对象点访问。double 字段走 R8 直传；bool 走 INT 0/1 同构
    # （BOOLEAN 字段拷 1 字节）；string 走 MonoString 装箱。
    static testAtSignDataMirror()
    {
        Console.println( "===== CSharpTest2.testAtSignDataMirror =====" )
        AtSignMetrics m = new()
        m.width = 6
        m.height = 4
        m.ratio = 1.5d
        m.tag = "mirror"
        m.active = true
        int area = 0
        @csharp_mono()
        {
            AtSignMetrics m <- $m
            using System;
            int area = m.width * m.height;
            $area <- area;
        }
        Console.println( "DataMirror area = " + area.toString() )
        check( "dataMirror: mirror fields width*height == 24", area == 24 )

        int flag = 0
        @csharp_mono()
        {
            AtSignMetrics m <- $m
            using System;
            int flag = 0;
            if (m.ratio == 1.5 && m.tag == "mirror" && m.active)
            {
                flag = 1;
            }
            $flag <- flag;
        }
        check( "dataMirror: double/string/bool fields crossed intact", flag == 1 )
    }

    # ---------------- 嵌套 data 递归装配 ----------------
    # AtSignOuter.inner 经 CVM 递归编组成子 SLLabelObject -> 插件递归装配
    # 子镜像实例（深度限 32）。具名 data 成员的花括号覆盖不生效，构造后
    # 显式赋值（md/syntax/data.md 已知限制）。
    static testAtSignDataNested()
    {
        Console.println( "===== CSharpTest2.testAtSignDataNested =====" )
        AtSignOuter o = new()
        o.id = 7
        o.inner.level = 3
        o.inner.weight = 2.5d
        int n = 0
        @csharp_mono()
        {
            AtSignOuter o <- $o
            using System;
            int n = o.inner.level * 100 + (int)(o.inner.weight * 10.0);
            $n <- n;
        }
        Console.println( "DataNested n = " + n.toString() )
        check( "dataNested: recursive mirror o.inner (level=3, weight=2.5) -> 325", n == 325 )
    }

    # ---------------- SL class -> C# 镜像 class ----------------
    # AtSignCounter 实例（REGULAR 元类型 + 类信息）经同一编组/装配链跨界，
    # CollectSlType 走 GetClassByName 路径收集布局（data 走 exportMetaDataList）。
    static testAtSignClassMirror()
    {
        Console.println( "===== CSharpTest2.testAtSignClassMirror =====" )
        AtSignCounter c = AtSignCounter()
        c.step = 3
        c.total = 60
        int n = 0
        @csharp_mono()
        {
            AtSignCounter c <- $c
            using System;
            int n = c.total / c.step;
            $n <- n;
        }
        Console.println( "ClassMirror n = " + n.toString() )
        check( "classMirror: SL class assembled as C# mirror total/step == 20", n == 20 )
    }

    # ---------------- jsonc references / sources（额外引用与同编源码） ----------------
    # references = 预编译 ExtraMathLib.dll（导出期 Front 拷入插件 libDir：编译期
    # csc /r: 引用 + 运行期 assemblies_path 裸名加载）；sources = SlExtraUtil.cs
    # 与块体同批合并编译进 SLAtSign.dll。块内 using 两命名空间混用两侧类型。
    # 注意：引用库命名空间（ExtraMathLib）勿与内部类同名——C# 会把
    # "类名.方法" 解析成 "命名空间.成员" 查找而报 CS0234（实测踩坑）。
    static testAtSignCsExtra()
    {
        Console.println( "===== CSharpTest2.testAtSignCsExtra =====" )
        int n = 0
        @csharp_mono()
        {
            using SLExtra;
            using ExtraMathLib;
            SlPoint p = new SlPoint();
            p.X = 3;
            p.Y = -4;
            int n = SlExtraUtil.Sum( 4, ExtraMath.Twice( 5 ) ) + p.Manhattan();
            $n <- n;
        }
        Console.println( "CsExtra n = " + n.toString() )
        check( "csExtra: sources(SLExtra)+references(ExtraMathLib) merge -> Sum(4,Twice(5))+|(3,-4)| == 21", n == 21 )
    }

    static fun()
    {
        Console.println( "========== CSharpTest2 start ==========" )
        testComputeGeometry()
        testComputeFullChain()
        testComputeThrow()
        testAtSignDataMirror()
        testAtSignDataNested()
        testAtSignClassMirror()
        testAtSignCsExtra()
        Console.println( "========== CSharpTest2 end: passed=" + passed.toString() + " failed=" + failed.toString() + " ==========" )
    }
}
