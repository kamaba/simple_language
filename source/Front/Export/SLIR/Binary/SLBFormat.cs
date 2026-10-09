#nullable enable
using System;

namespace SimpleLanguage.Export.SLIR.Binary
{
    /// <summary>
    /// SLB 二进制模块包格式常量（v1.0）。
    /// 规范真源：csimple_lang/md/design/SLB_DESIGN.md §4；
    /// C VM 侧镜像：csimple_lang/src/vm/load/slir_package_format.h（P1 落地）。
    /// 容器布局：FileHeader(48B 明文) + DataArea([段表][段数据]，可整体 zlib 压缩)。
    /// 头部与段表字段一律小端。
    /// </summary>
    internal static class SLBFormat
    {
        // ---- 容器布局 ----
        /// <summary>魔数 7F 'S' 'L' 'B'（首字节不可打印，防误判文本文件）。</summary>
        public static readonly byte[] Magic = new byte[] { 0x7F, 0x53, 0x4C, 0x42 };
        /// <summary>FileHeader 固定长度（字节）。</summary>
        public const int FileHeaderSize = 48;
        /// <summary>headerCrc32 覆盖范围：头部前 44 字节（0..43）。</summary>
        public const int HeaderCrcCoverLength = 44;
        /// <summary>格式主版本。读侧 major != 1 拒载。</summary>
        public const ushort FormatMajor = 1;
        /// <summary>格式次版本。读侧 minor &gt; 当前值拒载（含未识别的向前演进）。</summary>
        public const ushort FormatMinor = 0;
        /// <summary>moduleUuid 字节数（uuid 为 32 位 hex 字符串 → 16 字节）。</summary>
        public const int UuidLength = 16;
        /// <summary>SectionTable 单条目长度（字节）。</summary>
        public const int SectionTableEntrySize = 24;

        // ---- headerFlags 位定义 ----
        /// <summary>bit0：字节序（0=小端；1=大端——v1 读侧恒拒载）。</summary>
        public const uint FlagEndianBig = 0x01;
        /// <summary>bit1：DataArea 已整体 zlib 压缩（头部恒明文）。</summary>
        public const uint FlagCompressed = 0x02;
        /// <summary>bit2：含 DEBUG 段，METHOD/CODE 与 express 内的 debugInfoId 字段生效。</summary>
        public const uint FlagDebugInfo = 0x04;
        /// <summary>bit3：含 SHA-256 摘要（P5 保留；读侧遇 1 拒载）。</summary>
        public const uint FlagStrictDigest = 0x08;
        /// <summary>bit4：META 段含 AOT 信息（tag 0x0A）。</summary>
        public const uint FlagMetaAot = 0x10;
        /// <summary>读侧已知的 headerFlags 位掩码（未知位 = 超前演进，拒载）。</summary>
        public const uint KnownHeaderFlags = FlagEndianBig | FlagCompressed | FlagDebugInfo | FlagStrictDigest | FlagMetaAot;

        // ---- 段类型（每类型至多一段；必需段恒写（空内容也写），可选段仅有数据时写） ----
        /// <summary>必需：字符串池。</summary>
        public const uint SectionPool = 0x0001;
        /// <summary>必需：模块元信息（tag-TLV）。</summary>
        public const uint SectionMeta = 0x0002;
        /// <summary>可选：命名空间/类型表。</summary>
        public const uint SectionNamespace = 0x0003;
        /// <summary>必需：类表。</summary>
        public const uint SectionClass = 0x0004;
        /// <summary>可选：全局静态变量表。</summary>
        public const uint SectionGlobal = 0x0005;
        /// <summary>必需：方法表（含 instrStart/instrCount 指向 CODE 全局指令序）。</summary>
        public const uint SectionMethod = 0x0006;
        /// <summary>必需：指令流（无 count 前缀，条数由 METHOD 段决定）。</summary>
        public const uint SectionCode = 0x0007;
        /// <summary>可选：IR 字符串常量表。</summary>
        public const uint SectionIRString = 0x0008;
        /// <summary>可选：调试信息表（debugInfoId 按引用相等去重）。</summary>
        public const uint SectionDebug = 0x0009;

        // ---- META 段 tag（tag-TLV；未知 tag 读侧跳过 = 前向兼容） ----
        /// <summary>moduleName（poolRef）。</summary>
        public const byte MetaTagModuleName = 0x01;
        /// <summary>versionMain（uvarint）。</summary>
        public const byte MetaTagVersionMain = 0x02;
        /// <summary>versionSub（uvarint）。</summary>
        public const byte MetaTagVersionSub = 0x03;
        /// <summary>versionPatch（uvarint）。</summary>
        public const byte MetaTagVersionPatch = 0x04;
        /// <summary>entryMethodId（uvarint；缺 tag = null，0 值也写 tag）。</summary>
        public const byte MetaTagEntryMethodId = 0x05;
        /// <summary>buildMode（poolRef，恒写 tag 保真空串）。</summary>
        public const byte MetaTagBuildMode = 0x06;
        /// <summary>optimizeLevel（uvarint）。</summary>
        public const byte MetaTagOptimizeLevel = 0x07;
        /// <summary>nativeDll（poolRef，恒写 tag 保真空串）。</summary>
        public const byte MetaTagNativeDll = 0x08;
        /// <summary>platform（SLPlatformPackage 声明序紧凑编码；缺 tag = null）。</summary>
        public const byte MetaTagPlatform = 0x09;
        /// <summary>aot（SLAotPackage 声明序紧凑编码；缺 tag = null）。</summary>
        public const byte MetaTagAot = 0x0A;
        /// <summary>systemCalls（列表，恒写 tag，空列表 count=0）。</summary>
        public const byte MetaTagSystemCalls = 0x10;
        /// <summary>dllImports（列表，恒写 tag）。</summary>
        public const byte MetaTagDllImports = 0x11;
        /// <summary>vmDllImports（列表，恒写 tag）。</summary>
        public const byte MetaTagVmDllImports = 0x12;
        /// <summary>plugins（列表，恒写 tag）。</summary>
        public const byte MetaTagPlugins = 0x13;
        /// <summary>atSignLabel（列表，恒写 tag）。</summary>
        public const byte MetaTagAtSignLabel = 0x14;
        /// <summary>moduleReferences（列表，恒写 tag，空列表 count=0）。</summary>
        public const byte MetaTagModuleReferences = 0x15;
    }

    /// <summary>SLB 读写过程中的格式错误（写侧数据非法 / 读侧校验不过）。</summary>
    public sealed class SLBFormatException : Exception
    {
        public SLBFormatException(string message) : base(message) { }
        public SLBFormatException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// CRC-32（IEEE 802.3 反射多项式 0xEDB88320，表驱动）。
    /// 与 zlib crc32() 结果一致，供 FileHeader.headerCrc32 与 SectionTable.rawCrc32；
    /// C VM 侧读装载器按同算法镜像。
    /// </summary>
    internal static class SLBCrc32
    {
        private static readonly uint[] table = BuildTable();

        private static uint[] BuildTable()
        {
            var t = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++)
                {
                    c = ((c & 1u) != 0u) ? (0xEDB88320u ^ (c >> 1)) : (c >> 1);
                }
                t[i] = c;
            }
            return t;
        }

        public static uint Compute(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return Compute(data, 0, data.Length);
        }

        public static uint Compute(byte[] data, int offset, int length)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0 || length < 0 || offset + length > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            uint c = 0xFFFFFFFFu;
            for (int i = 0; i < length; i++)
            {
                c = table[(c ^ data[offset + i]) & 0xFFu] ^ (c >> 8);
            }
            return c ^ 0xFFFFFFFFu;
        }
    }
}
