using System;
using System.Collections.Generic;

namespace SimpleLanguage.Project
{
    // 平台定义注册表的 Front 侧镜像（PLATFORM_CAPABILITY_DESIGN.md §13.8 / §13.9）。
    // 表内容与 C 侧 csimple_lang/src/vm/platform/sl_platform_def.c 逐字对称
    // （成员名 key + 别名 aliases；数值不入 Front——jsonc 校验/归一只用名字）。
    // 匹配语义与 sl_def_lookup 一致：先比成员名、再比别名，均大小写不敏感；
    // 命中别名时归一为规范成员名，随 module.json 导出（§13.8 别名归一）。
    public static class PlatformDefTables
    {
        // ── kind 序号（与 SLDefKind / Environment.Platform.DefKind 一致）──
        public const int KindOs = 0;
        public const int KindOsVersion = 1;
        public const int KindArch = 2;
        public const int KindCpu = 3;
        public const int KindIsa = 4;
        public const int KindDevice = 5;
        public const int KindAi = 6;
        public const int KindGfx = 7;
        public const int KindShaderModel = 8;
        public const int KindNetwork = 9;
        public const int KindLink = 10;
        public const int KindMcuFamily = 11;
        public const int KindRtos = 12;
        public const int KindRuntime = 13;
        public const int KindBuildMode = 14;
        public const int KindEndian = 15;
        public const int KindCount = 16;

        public sealed class DefEntry
        {
            public string Key = string.Empty;
            public string[] Aliases = Array.Empty<string>();
        }

        // key + 空格分隔的别名串 → 条目
        static DefEntry Of(string key, string aliases)
        {
            var list = new List<string>();
            foreach (var a in aliases.Split(' '))
            {
                if (a.Length > 0)
                {
                    list.Add(a);
                }
            }
            return new DefEntry { Key = key, Aliases = list.ToArray() };
        }

        // ── 16 张表（镜像 sl_platform_def.c；成员名与别名逐字照搬）──

        static readonly DefEntry[] sOs = new DefEntry[]
        {
            Of("unknown", ""),
            Of("window", "windows win win32 win64"),
            Of("linux", ""),
            Of("mac", "macos osx darwin"),
            Of("unix", ""),
            Of("freeBSD", "freebsd bsd"),
            Of("android", ""),
            Of("ios", "iphone ipad"),
            Of("ps4", "playstation4"),
            Of("ps5", "playstation5"),
            Of("xboxOne", ""),
            Of("xboxSeries", "xbox"),
            Of("nintendoSwitch", "switch nintendo nx"),
            Of("browser", "wasm-browser"),
            Of("wasi", ""),
            Of("bareMetal", "baremetal bare noos"),
            Of("rtos", ""),
        };

        static readonly DefEntry[] sOsVersion = new DefEntry[]
        {
            Of("unknown", ""),
            Of("window7", ""),
            Of("window8", ""),
            Of("window10", ""),
            Of("window11", ""),
            Of("windowServer2019", ""),
            Of("windowServer2022", ""),
            Of("linuxGeneric", ""),
            Of("linuxDebian", "debian"),
            Of("linuxUbuntu", "ubuntu"),
            Of("linuxArch", "arch"),
            Of("linuxAlpine", "alpine"),
            Of("linuxCentos", "centos"),
            Of("linuxRhel", "rhel"),
            Of("linuxFedora", "fedora"),
            Of("linuxOpenSuse", "opensuse"),
            Of("linuxGentoo", "gentoo"),
            Of("linuxKali", "kali"),
            Of("linuxKeil", ""),
            Of("linuxYocto", ""),
            Of("linuxBuildroot", ""),
            Of("linuxOpenWrt", ""),
            Of("macos13", ""),
            Of("macos14", ""),
            Of("macos15", ""),
            Of("ios16", ""),
            Of("ios17", ""),
            Of("ios18", ""),
            Of("androidApi31", ""),
            Of("androidApi33", ""),
            Of("androidApi34", ""),
            Of("ps4Sdk", ""),
            Of("ps5Sdk", ""),
            Of("xboxGdk", ""),
            Of("switchSdk", ""),
            Of("wasiPreview1", ""),
            Of("wasiP2", ""),
        };

        static readonly DefEntry[] sArch = new DefEntry[]
        {
            Of("unknown", ""),
            Of("x86", "i386 i686"),
            Of("x64", "x86_64 amd64"),
            Of("arm32", "arm armv7 armv7l"),
            Of("arm64", "aarch64"),
            Of("riscv64", ""),
            Of("loongArch64", "loongarch64 la64"),
            Of("mips64", ""),
            Of("sw64", ""),
            Of("ppc64", "powerpc64"),
            Of("sparc64", ""),
            Of("wasm32", ""),
        };

        static readonly DefEntry[] sCpu = new DefEntry[]
        {
            Of("unknown", ""),
            Of("single", ""),
            Of("smp", ""),
            Of("bigLittle", ""),
            Of("hybrid", ""),
            Of("numa", ""),
        };

        static readonly DefEntry[] sIsa = new DefEntry[]
        {
            Of("none", ""),
            Of("sse", ""),
            Of("sse2", ""),
            Of("sse42", "sse4.2 sse4_2"),
            Of("avx", ""),
            Of("avx2", ""),
            Of("avx512", "avx512f"),
            Of("fma", ""),
            Of("neon", ""),
            Of("sve", ""),
            Of("sve2", ""),
            Of("rvv", ""),
            Of("lsx", ""),
            Of("lasx", ""),
            Of("mmi", ""),
            Of("altivec", ""),
            Of("vsx", ""),
        };

        static readonly DefEntry[] sDevice = new DefEntry[]
        {
            Of("none", ""),
            Of("cpu", ""),
            Of("gpu", ""),
            Of("npu", ""),
            Of("dsp", ""),
            Of("fpga", ""),
        };

        static readonly DefEntry[] sAi = new DefEntry[]
        {
            Of("none", ""),
            Of("onnxruntime", ""),
            Of("tensorRT", ""),
            Of("openVINO", ""),
            Of("tflite", ""),
            Of("coreML", ""),
            Of("cann", ""),
            Of("rknn", ""),
            Of("cambricon", ""),
            Of("qnn", ""),
            Of("snpe", ""),
            Of("horizonBpu", ""),
            Of("sophon", ""),
            Of("ane", ""),
            Of("torch", ""),
            Of("tensorFlow", ""),
        };

        static readonly DefEntry[] sGfx = new DefEntry[]
        {
            Of("none", ""),
            Of("d3d11", ""),
            Of("d3d12", ""),
            Of("vulkan", ""),
            Of("metal", ""),
            Of("openGL", "opengl"),
            Of("openGLES", "opengles"),
            Of("webGPU", "webgpu"),
            Of("software", ""),
        };

        static readonly DefEntry[] sShaderModel = new DefEntry[]
        {
            Of("unknown", ""),
            Of("sm_5_0", ""),
            Of("sm_6_0", ""),
            Of("sm_6_5", ""),
            Of("sm_6_6", ""),
            Of("sm_6_7", ""),
        };

        static readonly DefEntry[] sNetwork = new DefEntry[]
        {
            Of("none", ""),
            Of("loopback", ""),
            Of("ethernet", ""),
            Of("wifi", "wi-fi"),
            Of("cellular", "lte"),
            Of("unknownNet", ""),
        };

        static readonly DefEntry[] sLink = new DefEntry[]
        {
            Of("down", ""),
            Of("lan", "local"),
            Of("online", ""),
        };

        static readonly DefEntry[] sMcuFamily = new DefEntry[]
        {
            Of("none", ""),
            Of("cortexM", "cortex-m"),
            Of("cortexA", "cortex-a"),
            Of("cortexR", "cortex-r"),
            Of("xtensa", ""),
            Of("riscvMcu", "riscv-mcu"),
            Of("mcs51", "8051"),
            Of("avr", ""),
            Of("pic", ""),
            Of("msp430", ""),
            Of("rl78", ""),
            Of("rx", ""),
            Of("triCore", "tricore"),
            Of("hc08", ""),
        };

        static readonly DefEntry[] sRtos = new DefEntry[]
        {
            Of("none", ""),
            Of("freeRTOS", "freertos"),
            Of("rtThread", "rtthread"),
            Of("zephyr", ""),
            Of("threadX", "threadx"),
            Of("ucos", ""),
            Of("mbed", ""),
            Of("liteOS", "liteos"),
            Of("aliosThings", "alios"),
        };

        static readonly DefEntry[] sRuntime = new DefEntry[]
        {
            Of("slvm", ""),
            Of("clr", ""),
            Of("jvm", ""),
            Of("aot", ""),
            Of("wasm", ""),
        };

        static readonly DefEntry[] sBuildMode = new DefEntry[]
        {
            Of("debug", ""),
            Of("release", ""),
        };

        static readonly DefEntry[] sEndian = new DefEntry[]
        {
            Of("unknown", ""),
            Of("little", ""),
            Of("big", ""),
        };

        static readonly DefEntry[][] sTables = new DefEntry[][]
        {
            sOs, sOsVersion, sArch, sCpu, sIsa, sDevice, sAi, sGfx,
            sShaderModel, sNetwork, sLink, sMcuFamily, sRtos, sRuntime, sBuildMode, sEndian,
        };

        // kind → SL 侧枚举名（Environment.Platform.<name>，错误提示引用；gfx→render、mcuFamily→embedded 与 SL 枚举名对齐）
        static readonly string[] sSlEnumNames = new string[]
        {
            "os", "osVersion", "arch", "cpu", "isa", "device", "ai", "render",
            "shaderModel", "network", "link", "embedded", "rtos", "runtime", "build", "endian",
        };

        // ── override key 前缀表（§8.5.3，镜像 sl_runtime_override.c 的 g_ov_prefixes）──
        // DefKind/DefKind2 → 该前缀成员受定义表约束（-1 = 自由文本成员不校验）；
        // network 联查 link 表（设计官方示例 "network.online"：online 是 link 表成员）。
        public sealed class OvPrefixEntry
        {
            public string Name = string.Empty;
            public int DefKind = -1;
            public int DefKind2 = -1;
        }

        public static readonly OvPrefixEntry[] OvPrefixes = new OvPrefixEntry[]
        {
            new OvPrefixEntry { Name = "os",           DefKind = KindOs },
            new OvPrefixEntry { Name = "osversion", },
            new OvPrefixEntry { Name = "arch",         DefKind = KindArch },
            new OvPrefixEntry { Name = "cpu",          DefKind = KindIsa },
            new OvPrefixEntry { Name = "isa",          DefKind = KindIsa },
            new OvPrefixEntry { Name = "cpucount", },
            new OvPrefixEntry { Name = "memory", },
            new OvPrefixEntry { Name = "lib", },
            new OvPrefixEntry { Name = "sdk", },
            new OvPrefixEntry { Name = "env", },
            new OvPrefixEntry { Name = "runtime", },
            new OvPrefixEntry { Name = "device",       DefKind = KindDevice },
            new OvPrefixEntry { Name = "environment", },
            new OvPrefixEntry { Name = "topology",     DefKind = KindCpu },
            new OvPrefixEntry { Name = "cputopology",  DefKind = KindCpu },
            new OvPrefixEntry { Name = "ai",           DefKind = KindAi },
            new OvPrefixEntry { Name = "render",       DefKind = KindGfx },
            new OvPrefixEntry { Name = "network",      DefKind = KindNetwork, DefKind2 = KindLink },
            new OvPrefixEntry { Name = "link",         DefKind = KindLink },
            new OvPrefixEntry { Name = "script", },
            new OvPrefixEntry { Name = "embedded",     DefKind = KindMcuFamily },
            new OvPrefixEntry { Name = "custom", },
        };

        // 前缀名（大小写不敏感）→ 前缀条目；未命中返回 null
        public static OvPrefixEntry FindOvPrefix(string name)
        {
            foreach (var p in OvPrefixes)
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }
            return null;
        }

        // 前缀成员是否在定义表中（主表 + 第二表；自由文本前缀返回 false 由调用方放行）
        public static bool OvPrefixMemberKnown(OvPrefixEntry p, string member)
        {
            return (p.DefKind >= 0 && TryNormalize(p.DefKind, member, out _))
                || (p.DefKind2 >= 0 && TryNormalize(p.DefKind2, member, out _));
        }

        // 前缀名纠错：21 个大类名里找编辑距离最近（≤2）的候选；找不到返回空
        public static string SuggestOvPrefix(string raw)
        {
            var best = string.Empty;
            var bestDist = 3;
            foreach (var p in OvPrefixes)
            {
                var d = Levenshtein(raw, p.Name);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = p.Name;
                }
            }
            return best;
        }

        // 名字（成员名或别名，大小写不敏感）→ 规范成员名；未命中返回 false（canonical=null）
        public static bool TryNormalize(int kind, string raw, out string canonical)
        {
            canonical = null;
            if (kind < 0 || kind >= KindCount || string.IsNullOrEmpty(raw))
            {
                return false;
            }
            foreach (var e in sTables[kind])
            {
                if (string.Equals(e.Key, raw, StringComparison.OrdinalIgnoreCase))
                {
                    canonical = e.Key;
                    return true;
                }
                foreach (var a in e.Aliases)
                {
                    if (string.Equals(a, raw, StringComparison.OrdinalIgnoreCase))
                    {
                        canonical = e.Key;
                        return true;
                    }
                }
            }
            return false;
        }

        // 全部成员名（空格分隔，按表序）
        public static string MembersText(int kind)
        {
            if (kind < 0 || kind >= KindCount)
            {
                return string.Empty;
            }
            var keys = new List<string>();
            foreach (var e in sTables[kind])
            {
                keys.Add(e.Key);
            }
            return string.Join(" ", keys);
        }

        // 全部别名平铺（空格分隔；无别名返回空串）
        public static string AliasesText(int kind)
        {
            if (kind < 0 || kind >= KindCount)
            {
                return string.Empty;
            }
            var list = new List<string>();
            foreach (var e in sTables[kind])
            {
                foreach (var a in e.Aliases)
                {
                    list.Add(a);
                }
            }
            return string.Join(" ", list);
        }

        public static string SlEnumName(int kind)
        {
            if (kind < 0 || kind >= KindCount)
            {
                return string.Empty;
            }
            return sSlEnumNames[kind];
        }

        // 纠错建议：在成员名 + 别名里找编辑距离最近（≤2）的候选，返回其规范成员名；找不到返回空
        public static string Suggest(int kind, string raw)
        {
            if (kind < 0 || kind >= KindCount || string.IsNullOrEmpty(raw))
            {
                return string.Empty;
            }
            var best = string.Empty;
            var bestDist = 3;
            foreach (var e in sTables[kind])
            {
                Consider(e.Key, e.Key);
                foreach (var a in e.Aliases)
                {
                    Consider(a, e.Key);
                }
            }
            return best;

            void Consider(string candidate, string canonical)
            {
                var d = Levenshtein(raw, candidate);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = canonical;
                }
            }
        }

        // 经典编辑距离（jsonc 值均为短串，无需滚动数组以外的优化）
        static int Levenshtein(string a, string b)
        {
            var n = a.Length;
            var m = b.Length;
            if (n == 0)
            {
                return m;
            }
            if (m == 0)
            {
                return n;
            }
            var prev = new int[m + 1];
            var curr = new int[m + 1];
            for (var j = 0; j <= m; j++)
            {
                prev[j] = j;
            }
            for (var i = 1; i <= n; i++)
            {
                curr[0] = i;
                for (var j = 1; j <= m; j++)
                {
                    var cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                    curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                var t = prev;
                prev = curr;
                curr = t;
            }
            return prev[m];
        }
    }
}
