#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SimpleLanguage.Export.SLIR.Types;

namespace SimpleLanguage.Export.SLIR.Binary
{
    /// <summary>
    /// SLB 读取上下文：字符串池 + 调试信息表，供 poolRef / debugRef 反解。
    /// 与写侧 WriteContext 对称（SLBWriter.cs）。
    /// </summary>
    internal sealed class ReadContext
    {
        private string[] pool = Array.Empty<string>();
        private SLInstructionDebugInfo[] debugTable = Array.Empty<SLInstructionDebugInfo>();

        /// <summary>是否含调试信息（FileHeader bit2，决定 DebugRef 字段是否存在）。</summary>
        public bool HasDebug { get; }

        public ReadContext(bool hasDebug)
        {
            HasDebug = hasDebug;
        }

        public void SetPool(string[] items)
        {
            pool = items ?? Array.Empty<string>();
        }

        public void SetDebugTable(SLInstructionDebugInfo[] items)
        {
            debugTable = items ?? Array.Empty<SLInstructionDebugInfo>();
        }

        public string GetStr(long index)
        {
            if (index < 0 || index >= pool.Length)
            {
                throw new SLBFormatException("字符串池引用越界: index=" + index + ", poolSize=" + pool.Length);
            }
            return pool[index];
        }

        public SLInstructionDebugInfo? GetDebug(long id)
        {
            if (!HasDebug)
            {
                return null;
            }
            if (id == 0)
            {
                return null;
            }
            if (id < 0 || id > debugTable.Length)
            {
                throw new SLBFormatException("调试信息引用越界: id=" + id + ", debugCount=" + debugTable.Length);
            }
            return debugTable[id - 1];
        }
    }

    /// <summary>
    /// SLB 限界解码器：在 data[start, start+length) 窗口上顺序读取。
    /// 读原语与写侧 SLBEncoder 一一对应（uvarint 用 ulong 累积，与 BCL
    /// Write7BitEncodedLong 位模式对称）。
    /// </summary>
    internal sealed class SLBDecoder
    {
        private readonly byte[] data;
        private readonly int end;
        private readonly ReadContext ctx;
        private int pos;

        public SLBDecoder(byte[] data, int start, int length, ReadContext ctx)
        {
            if (start < 0 || length < 0 || (long)start + (long)length > data.Length)
            {
                throw new SLBFormatException("解码窗口越界: start=" + start + ", length=" + length);
            }
            this.data = data;
            this.pos = start;
            this.end = start + length;
            this.ctx = ctx;
        }

        /// <summary>窗口内剩余未读字节数。</summary>
        public int Remaining
        {
            get { return end - pos; }
        }

        private void Ensure(int count)
        {
            if (count < 0 || (long)pos + (long)count > (long)end)
            {
                throw new SLBFormatException("数据不足: 需要 " + count + " 字节, 剩余 " + (end - pos));
            }
        }

        public byte U8()
        {
            Ensure(1);
            return data[pos++];
        }

        public bool Bool()
        {
            byte v = U8();
            if (v > 1)
            {
                throw new SLBFormatException("bool 值非法: " + v);
            }
            return v == 1;
        }

        /// <summary>读 uvarint（LEB128）；最多 10 字节，值域不得超过 long.MaxValue。</summary>
        public long UVar()
        {
            ulong result = 0UL;
            int shift = 0;
            while (true)
            {
                Ensure(1);
                byte b = data[pos++];
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    break;
                }
                shift += 7;
                if (shift >= 70)
                {
                    throw new SLBFormatException("uvarint 超过 10 字节");
                }
            }
            if (result > (ulong)long.MaxValue)
            {
                throw new SLBFormatException("uvarint 值超出 long 范围");
            }
            return (long)result;
        }

        /// <summary>读 uvarint 并还原为 int（写侧按 uint 位模式编码，负数无损往返）。</summary>
        public int UVarInt()
        {
            long v = UVar();
            if ((ulong)v > 0xFFFFFFFFUL)
            {
                throw new SLBFormatException("uvarint 值超出 uint 范围");
            }
            return (int)(uint)v;
        }

        public byte[] Raw(int count)
        {
            if (count < 0)
            {
                throw new SLBFormatException("raw 长度非法: " + count);
            }
            Ensure(count);
            byte[] result = new byte[count];
            Buffer.BlockCopy(data, pos, result, 0, count);
            pos += count;
            return result;
        }

        /// <summary>读 uvarint 长度前缀 + 字节块。</summary>
        public byte[] Raw()
        {
            long len = UVar();
            if (len < 0 || len > int.MaxValue)
            {
                throw new SLBFormatException("raw 长度非法: " + len);
            }
            return Raw((int)len);
        }

        /// <summary>读 uvarint 字节长度前缀 + UTF-8 字符串（不经池，与写侧 Utf8 对称）。</summary>
        public string Utf8()
        {
            long len = UVar();
            if (len < 0 || len > int.MaxValue)
            {
                throw new SLBFormatException("utf8 长度非法: " + len);
            }
            byte[] bytes = Raw((int)len);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>读池引用字符串（写侧 Str 对称）。</summary>
        public string Str()
        {
            return ctx.GetStr(UVar());
        }

        public string? OptStr()
        {
            byte presence = U8();
            if (presence == 0)
            {
                return null;
            }
            if (presence != 1)
            {
                throw new SLBFormatException("可选字符串 presence 非法: " + presence);
            }
            return Str();
        }

        public bool? OptBool()
        {
            byte presence = U8();
            if (presence == 0)
            {
                return null;
            }
            if (presence != 1)
            {
                throw new SLBFormatException("可选 bool presence 非法: " + presence);
            }
            return Bool();
        }

        /// <summary>读调试信息引用；无调试信息时写侧不落任何字节（对称：读侧不消费字节）。</summary>
        public SLInstructionDebugInfo? DebugRef()
        {
            if (!ctx.HasDebug)
            {
                return null;
            }
            return ctx.GetDebug(UVar());
        }
    }

    /// <summary>
    /// SLB 二进制模块包读取器（.slb / 字节流 → SLModulePackage），与 SLBWriter 逐字段对称。
    /// 四道校验门：①FileHeader（magic/版本/flags/CRC）②解压（长度互校）
    /// ③段表（类型/去重/边界）④逐段 rawCrc32 + 池引用越界。
    /// 规范真源：csimple_lang/md/design/SLB_DESIGN.md。
    /// </summary>
    public static class SLBReader
    {
        /// <summary>从磁盘读取 .slb 文件并解析为模块包。</summary>
        public static SLModulePackage Read(string inputPath)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentNullException(nameof(inputPath));
            }
            return FromBytes(File.ReadAllBytes(inputPath));
        }

        /// <summary>从内存字节流解析模块包（供内存直传链路与测试使用）。</summary>
        public static SLModulePackage FromBytes(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            /* ---- 门 1：FileHeader ---- */
            if (data.Length < SLBFormat.FileHeaderSize)
            {
                throw new SLBFormatException("文件长度不足 FileHeader: " + data.Length);
            }
            for (int i = 0; i < SLBFormat.Magic.Length; i++)
            {
                if (data[i] != SLBFormat.Magic[i])
                {
                    throw new SLBFormatException("magic 不匹配: 不是 SLB 文件");
                }
            }
            int formatMajor = GetU16(data, 4);
            int formatMinor = GetU16(data, 6);
            if (formatMajor != SLBFormat.FormatMajor)
            {
                throw new SLBFormatException("formatMajor 不支持: " + formatMajor + "（期望 " + SLBFormat.FormatMajor + "）");
            }
            if (formatMinor > SLBFormat.FormatMinor)
            {
                throw new SLBFormatException("formatMinor 不支持: " + formatMinor + "（期望 <= " + SLBFormat.FormatMinor + "）");
            }
            uint headerFlags = GetU32(data, 8);
            if ((headerFlags & SLBFormat.FlagEndianBig) != 0)
            {
                throw new SLBFormatException("大端字节序不支持（v1 仅小端）");
            }
            if ((headerFlags & SLBFormat.FlagStrictDigest) != 0)
            {
                throw new SLBFormatException("SHA-256 摘要（strictDigest）为 P5 预留，读侧暂不支持");
            }
            if ((headerFlags & ~SLBFormat.KnownHeaderFlags) != 0)
            {
                throw new SLBFormatException("未知的 headerFlags 位: 0x" + headerFlags.ToString("X"));
            }
            if (GetU16(data, 42) != 0)
            {
                throw new SLBFormatException("header.reserved 非 0");
            }
            if (SLBCrc32.Compute(data, 0, SLBFormat.HeaderCrcCoverLength) != GetU32(data, 44))
            {
                throw new SLBFormatException("headerCrc32 校验失败");
            }

            bool compressed = (headerFlags & SLBFormat.FlagCompressed) != 0;
            bool hasDebug = (headerFlags & SLBFormat.FlagDebugInfo) != 0;
            uint dataRawTotal = GetU32(data, 36);
            int sectionCount = GetU16(data, 40);
            byte[] uuid = new byte[SLBFormat.UuidLength];
            Array.Copy(data, 12, uuid, 0, SLBFormat.UuidLength);

            /* ---- 门 2：解压（压缩时 [4B dataRawTotal 前缀][zlib stream]；非压缩零拷贝） ---- */
            byte[] area;
            int areaBase;
            if (compressed)
            {
                long payloadLen = (long)data.Length - SLBFormat.FileHeaderSize;
                if (payloadLen < 4)
                {
                    throw new SLBFormatException("压缩数据缺少长度前缀");
                }
                uint prefixTotal = GetU32(data, SLBFormat.FileHeaderSize);
                if (prefixTotal != dataRawTotal)
                {
                    throw new SLBFormatException("压缩前缀长度与 dataRawTotal 不一致: " + prefixTotal + " vs " + dataRawTotal);
                }
                area = ZLibDecompress(data, SLBFormat.FileHeaderSize + 4, (int)(payloadLen - 4));
                if ((uint)area.Length != dataRawTotal)
                {
                    throw new SLBFormatException("解压后长度与 dataRawTotal 不一致: " + area.Length + " vs " + dataRawTotal);
                }
                areaBase = 0;
            }
            else
            {
                if ((long)data.Length - SLBFormat.FileHeaderSize != (long)dataRawTotal)
                {
                    throw new SLBFormatException("非压缩文件长度与 dataRawTotal 不一致: " + (data.Length - SLBFormat.FileHeaderSize) + " vs " + dataRawTotal);
                }
                area = data;
                areaBase = SLBFormat.FileHeaderSize;
            }

            /* ---- 门 3 + 门 4：段表（类型/去重/边界/逐段 CRC） ---- */
            if (sectionCount > 9)
            {
                throw new SLBFormatException("段数量非法: " + sectionCount);
            }
            long tableEnd = (long)sectionCount * SLBFormat.SectionTableEntrySize;
            if (tableEnd > (long)dataRawTotal)
            {
                throw new SLBFormatException("段表越界: " + tableEnd + " vs " + dataRawTotal);
            }
            var sections = new Dictionary<uint, (int offset, int length)>();
            var seenTypes = new HashSet<uint>();
            for (int i = 0; i < sectionCount; i++)
            {
                int entryBase = areaBase + i * SLBFormat.SectionTableEntrySize;
                uint type = GetU32(area, entryBase);
                uint off = GetU32(area, entryBase + 4);
                uint len = GetU32(area, entryBase + 8);
                uint crc = GetU32(area, entryBase + 12);
                if (type == 0 || type > SLBFormat.SectionDebug)
                {
                    throw new SLBFormatException("段类型非法: 0x" + type.ToString("X"));
                }
                if (!seenTypes.Add(type))
                {
                    throw new SLBFormatException("段类型重复: 0x" + type.ToString("X"));
                }
                if (off < (uint)tableEnd)
                {
                    throw new SLBFormatException("段 offset 落在段表内: type=0x" + type.ToString("X"));
                }
                if ((ulong)off + (ulong)len > (ulong)dataRawTotal)
                {
                    throw new SLBFormatException("段数据越界: type=0x" + type.ToString("X"));
                }
                if (SLBCrc32.Compute(area, areaBase + (int)off, (int)len) != crc)
                {
                    throw new SLBFormatException("段 rawCrc32 校验失败: type=0x" + type.ToString("X"));
                }
                sections[type] = ((int)off, (int)len);
            }

            var poolSection = RequireSection(sections, SLBFormat.SectionPool, "POOL");
            var metaSection = RequireSection(sections, SLBFormat.SectionMeta, "META");
            var classSection = RequireSection(sections, SLBFormat.SectionClass, "CLASS");
            var methodSection = RequireSection(sections, SLBFormat.SectionMethod, "METHOD");
            var codeSection = RequireSection(sections, SLBFormat.SectionCode, "CODE");
            if (hasDebug != sections.ContainsKey(SLBFormat.SectionDebug))
            {
                throw new SLBFormatException("hasDebug 标志与 DEBUG 段不一致");
            }

            /* ---- 解码（POOL 建池 → DEBUG 建表 → META → NS → CLASS → GLOBAL → METHOD → CODE → IRSTRING） ---- */
            var ctx = new ReadContext(hasDebug);

            var poolDec = new SLBDecoder(area, areaBase + poolSection.offset, poolSection.length, ctx);
            var poolItems = new string[Count(poolDec, "POOL")];
            for (int i = 0; i < poolItems.Length; i++)
            {
                poolItems[i] = poolDec.Utf8();
            }
            RequireDrained(poolDec, "POOL");
            ctx.SetPool(poolItems);

            if (hasDebug)
            {
                var debugSection = sections[SLBFormat.SectionDebug];
                var debugDec = new SLBDecoder(area, areaBase + debugSection.offset, debugSection.length, ctx);
                var debugItems = new SLInstructionDebugInfo[Count(debugDec, "DEBUG")];
                for (int i = 0; i < debugItems.Length; i++)
                {
                    var info = new SLInstructionDebugInfo();
                    info.path = debugDec.Str();
                    info.name = debugDec.Str();
                    info.info = debugDec.Str();
                    info.beginLine = debugDec.UVarInt();
                    info.beginChar = debugDec.UVarInt();
                    info.endLine = debugDec.UVarInt();
                    info.endChar = debugDec.UVarInt();
                    debugItems[i] = info;
                }
                RequireDrained(debugDec, "DEBUG");
                ctx.SetDebugTable(debugItems);
            }

            var pkg = new SLModulePackage();
            pkg.uuid = Convert.ToHexString(uuid).ToLowerInvariant();

            var metaDec = new SLBDecoder(area, areaBase + metaSection.offset, metaSection.length, ctx);
            DecodeMetaSection(metaDec, ctx, pkg);
            if (((headerFlags & SLBFormat.FlagMetaAot) != 0) != (pkg.aot != null))
            {
                throw new SLBFormatException("FlagMetaAot 与 META aot tag 不一致");
            }

            if (sections.TryGetValue(SLBFormat.SectionNamespace, out var nsSection))
            {
                var nsDec = new SLBDecoder(area, areaBase + nsSection.offset, nsSection.length, ctx);
                DecodeNamespaceSection(nsDec, pkg);
            }

            var classDec = new SLBDecoder(area, areaBase + classSection.offset, classSection.length, ctx);
            DecodeClassSection(classDec, pkg);

            if (sections.TryGetValue(SLBFormat.SectionGlobal, out var globalSection))
            {
                var globalDec = new SLBDecoder(area, areaBase + globalSection.offset, globalSection.length, ctx);
                DecodeGlobalSection(globalDec, pkg);
            }

            var methodDec = new SLBDecoder(area, areaBase + methodSection.offset, methodSection.length, ctx);
            List<(SLMethodPackage method, int instrStart, int instrCount)> methodRecords = DecodeMethodSection(methodDec);

            /* CODE 段无 count 前缀：整段即全局指令表，读到尾为止。 */
            var codeDec = new SLBDecoder(area, areaBase + codeSection.offset, codeSection.length, ctx);
            var allInstructions = new List<SLIRInstructionPackage>();
            while (codeDec.Remaining > 0)
            {
                allInstructions.Add(DecodeInstruction(codeDec));
            }

            if (sections.TryGetValue(SLBFormat.SectionIRString, out var irSection))
            {
                var irDec = new SLBDecoder(area, areaBase + irSection.offset, irSection.length, ctx);
                DecodeIRStringSection(irDec, pkg);
            }

            /* instructionList 组装：METHOD 的 instrStart/instrCount 必须连续平铺覆盖 CODE 全表。 */
            int expectedStart = 0;
            foreach (var rec in methodRecords)
            {
                if (rec.instrStart != expectedStart)
                {
                    throw new SLBFormatException("方法指令区间不连续: method=" + rec.method.id + ", instrStart=" + rec.instrStart + ", 期望 " + expectedStart);
                }
                rec.method.instructionList = allInstructions.GetRange(rec.instrStart, rec.instrCount);
                expectedStart += rec.instrCount;
                pkg.methodList.Add(rec.method);
            }
            if (expectedStart != allInstructions.Count)
            {
                throw new SLBFormatException("CODE 段与 METHOD 段指令总数不一致: " + allInstructions.Count + " vs " + expectedStart);
            }

            return pkg;
        }

        // ---- META 段（tag-TLV；未知 tag 跳过 = 前向兼容） ----

        private static void DecodeMetaSection(SLBDecoder dec, ReadContext ctx, SLModulePackage pkg)
        {
            var seen = new HashSet<byte>();
            while (dec.Remaining > 0)
            {
                byte tag = dec.U8();
                long len = dec.UVar();
                if (len < 0 || len > int.MaxValue || len > dec.Remaining)
                {
                    throw new SLBFormatException("META tag 值长度非法: tag=0x" + tag.ToString("X"));
                }
                if (!seen.Add(tag))
                {
                    throw new SLBFormatException("META tag 重复: 0x" + tag.ToString("X"));
                }
                byte[] value = dec.Raw((int)len);
                var vdec = new SLBDecoder(value, 0, value.Length, ctx);
                switch (tag)
                {
                    case SLBFormat.MetaTagModuleName:
                    {
                        pkg.moduleName = vdec.Str();
                        break;
                    }
                    case SLBFormat.MetaTagVersionMain:
                    {
                        pkg.versionMain = vdec.UVarInt();
                        break;
                    }
                    case SLBFormat.MetaTagVersionSub:
                    {
                        pkg.versionSub = vdec.UVarInt();
                        break;
                    }
                    case SLBFormat.MetaTagVersionPatch:
                    {
                        pkg.versionPatch = vdec.UVarInt();
                        break;
                    }
                    case SLBFormat.MetaTagEntryMethodId:
                    {
                        pkg.entryMethodId = vdec.UVarInt();
                        break;
                    }
                    case SLBFormat.MetaTagBuildMode:
                    {
                        pkg.buildMode = vdec.Str();
                        break;
                    }
                    case SLBFormat.MetaTagOptimizeLevel:
                    {
                        pkg.optimizeLevel = vdec.UVarInt();
                        break;
                    }
                    case SLBFormat.MetaTagNativeDll:
                    {
                        pkg.nativeDll = vdec.Str();
                        break;
                    }
                    case SLBFormat.MetaTagPlatform:
                    {
                        pkg.platform = DecodePlatform(vdec);
                        break;
                    }
                    case SLBFormat.MetaTagAot:
                    {
                        pkg.aot = DecodeAot(vdec);
                        break;
                    }
                    case SLBFormat.MetaTagSystemCalls:
                    {
                        int count = Count(vdec, "META.systemCalls");
                        for (int i = 0; i < count; i++)
                        {
                            pkg.systemCalls.Add(DecodeSystemCall(vdec));
                        }
                        break;
                    }
                    case SLBFormat.MetaTagDllImports:
                    {
                        int count = Count(vdec, "META.dllImports");
                        for (int i = 0; i < count; i++)
                        {
                            pkg.dllImports.Add(DecodeDllImport(vdec));
                        }
                        break;
                    }
                    case SLBFormat.MetaTagVmDllImports:
                    {
                        int count = Count(vdec, "META.vmDllImports");
                        for (int i = 0; i < count; i++)
                        {
                            pkg.vmDllImports.Add(DecodeVmDllImport(vdec));
                        }
                        break;
                    }
                    case SLBFormat.MetaTagPlugins:
                    {
                        int count = Count(vdec, "META.plugins");
                        for (int i = 0; i < count; i++)
                        {
                            pkg.plugins.Add(DecodePlugin(vdec));
                        }
                        break;
                    }
                    case SLBFormat.MetaTagAtSignLabel:
                    {
                        int count = Count(vdec, "META.atSignLabel");
                        for (int i = 0; i < count; i++)
                        {
                            pkg.atSignLabel.Add(DecodeAtSignLabelEntry(vdec));
                        }
                        break;
                    }
                    case SLBFormat.MetaTagModuleReferences:
                    {
                        int count = Count(vdec, "META.moduleReferences");
                        for (int i = 0; i < count; i++)
                        {
                            pkg.moduleReferences.Add(DecodeModuleReference(vdec));
                        }
                        break;
                    }
                    default:
                    {
                        /* 未知 tag：跳过（前向兼容） */
                        break;
                    }
                }
                if (vdec.Remaining != 0)
                {
                    throw new SLBFormatException("META tag 值有尾部冗余字节: tag=0x" + tag.ToString("X"));
                }
            }
            RequireDrained(dec, "META");
        }

        // ---- NS 段 ----

        private static void DecodeNamespaceSection(SLBDecoder dec, SLModulePackage pkg)
        {
            int count = Count(dec, "NS");
            for (int i = 0; i < count; i++)
            {
                var ns = new SLNamespacePackage();
                ns.fullName = dec.Str();
                int typeCount = Count(dec, "NS.typeList");
                for (int t = 0; t < typeCount; t++)
                {
                    var type = new SLTypePackage();
                    type.fullName = dec.Str();
                    type.name = dec.Str();
                    int methodCount = Count(dec, "NS.typeList.methodList");
                    for (int m = 0; m < methodCount; m++)
                    {
                        type.methodList.Add(DecodeMethodMeta(dec));
                    }
                    type.templateParameterCount = dec.UVarInt();
                    ns.typeList.Add(type);
                }
                pkg.namespaceList.Add(ns);
            }
            RequireDrained(dec, "NS");
        }

        // ---- CLASS 段 ----

        private static void DecodeClassSection(SLBDecoder dec, SLModulePackage pkg)
        {
            int count = Count(dec, "CLASS");
            for (int i = 0; i < count; i++)
            {
                var cls = new SLClassPackage();
                cls.id = dec.UVarInt();
                cls.name = dec.Str();
                cls.fullName = dec.Str();
                cls.exportNames = dec.OptStr();
                cls.sourcePath = dec.Str();
                cls.metaClassKind = dec.UVarInt();
                cls.permission = dec.UVarInt();
                cls.isDynamic = dec.Bool();
                cls.isFinal = dec.Bool();
                cls.baseClassId = dec.UVarInt();
                int ifaceCount = Count(dec, "CLASS.implementsInterfaceIdList");
                for (int k = 0; k < ifaceCount; k++)
                {
                    cls.implementsInterfaceIdList.Add(dec.UVarInt());
                }
                int fieldCount = Count(dec, "CLASS.fieldList");
                for (int k = 0; k < fieldCount; k++)
                {
                    cls.fieldList.Add(DecodeField(dec));
                }
                int nonStaticCount = Count(dec, "CLASS.nonStaticMethodList");
                for (int k = 0; k < nonStaticCount; k++)
                {
                    cls.nonStaticMethodList.Add(DecodeMethodMeta(dec));
                }
                int operatorCount = Count(dec, "CLASS.operatorMethodList");
                for (int k = 0; k < operatorCount; k++)
                {
                    cls.operatorMethodList.Add(DecodeMethodMeta(dec));
                }
                int staticCount = Count(dec, "CLASS.staticMethodList");
                for (int k = 0; k < staticCount; k++)
                {
                    cls.staticMethodList.Add(DecodeMethodMeta(dec));
                }
                cls.templateCount = dec.UVarInt();
                cls.templateParameterCount = dec.UVarInt();
                int tpNameCount = Count(dec, "CLASS.templateParameterNames");
                for (int k = 0; k < tpNameCount; k++)
                {
                    cls.templateParameterNames.Add(dec.Str());
                }
                int tpTypeCount = Count(dec, "CLASS.templateTypeList");
                for (int k = 0; k < tpTypeCount; k++)
                {
                    cls.templateTypeList.Add(DecodeRuntimeDefType(dec));
                }
                int relCount = Count(dec, "CLASS.templateRelationList");
                for (int k = 0; k < relCount; k++)
                {
                    cls.templateRelationList.Add(DecodeTemplateRelation(dec));
                }
                int attrCount = Count(dec, "CLASS.attributeList");
                for (int k = 0; k < attrCount; k++)
                {
                    cls.attributeList.Add(DecodeAttribute(dec));
                }
                pkg.classList.Add(cls);
            }
            RequireDrained(dec, "CLASS");
        }

        // ---- GLOBAL 段 ----

        private static void DecodeGlobalSection(SLBDecoder dec, SLModulePackage pkg)
        {
            int count = Count(dec, "GLOBAL");
            for (int i = 0; i < count; i++)
            {
                var g = new SLGlobalStaticVariablePackage();
                g.id = dec.UVarInt();
                g.name = dec.Str();
                g.ownerClassId = dec.UVarInt();
                g.index = dec.UVarInt();
                g.typeDef = DecodeOptRuntimeDefType(dec);
                int exprCount = Count(dec, "GLOBAL.express");
                for (int k = 0; k < exprCount; k++)
                {
                    g.express.Add(DecodeInstruction(dec));
                }
                pkg.globalStaticVariableList.Add(g);
            }
            RequireDrained(dec, "GLOBAL");
        }

        // ---- METHOD 段（返回记录列表；instructionList 由 CODE 段统一组装） ----

        private static List<(SLMethodPackage method, int instrStart, int instrCount)> DecodeMethodSection(SLBDecoder dec)
        {
            var records = new List<(SLMethodPackage method, int instrStart, int instrCount)>();
            int count = Count(dec, "METHOD");
            for (int i = 0; i < count; i++)
            {
                var m = new SLMethodPackage();
                m.id = dec.Str();
                m.methodId = dec.UVarInt();
                m.name = dec.Str();
                m.exportNames = dec.OptStr();
                m.declaringTypeFullName = dec.Str();
                m.declaringClassId = dec.UVarInt();
                m.interfaceMethod = dec.Bool();
                m.flags = dec.UVarInt();
                m.isTemplateFunction = dec.Bool();
                int tpNameCount = Count(dec, "METHOD.templateParameterNames");
                for (int k = 0; k < tpNameCount; k++)
                {
                    m.templateParameterNames.Add(dec.Str());
                }
                int returnCount = Count(dec, "METHOD.returnList");
                for (int k = 0; k < returnCount; k++)
                {
                    m.returnList.Add(DecodeVariable(dec));
                }
                int argCount = Count(dec, "METHOD.argumentList");
                for (int k = 0; k < argCount; k++)
                {
                    m.argumentList.Add(DecodeVariable(dec));
                }
                int localCount = Count(dec, "METHOD.localList");
                for (int k = 0; k < localCount; k++)
                {
                    m.localList.Add(DecodeVariable(dec));
                }
                int instrStart = dec.UVarInt();
                int instrCount = dec.UVarInt();
                int attrCount = Count(dec, "METHOD.attributeList");
                for (int k = 0; k < attrCount; k++)
                {
                    m.attributeList.Add(DecodeAttribute(dec));
                }
                records.Add((m, instrStart, instrCount));
            }
            RequireDrained(dec, "METHOD");
            return records;
        }

        // ---- IRSTRING 段 ----

        private static void DecodeIRStringSection(SLBDecoder dec, SLModulePackage pkg)
        {
            int count = Count(dec, "IRSTRING");
            for (int i = 0; i < count; i++)
            {
                var item = new IRStringItem();
                item.id = dec.UVarInt();
                item.value = dec.Utf8();
                pkg.irStringDict.Add(item);
            }
            RequireDrained(dec, "IRSTRING");
        }

        // ---- 共享子结构 ----

        private static SLIRInstructionPackage DecodeInstruction(SLBDecoder dec)
        {
            var ins = new SLIRInstructionPackage();
            ins.id = dec.UVarInt();
            ins.opCode = dec.U8();
            ins.byteLength = dec.UVarInt();
            byte payloadPresence = dec.U8();
            if (payloadPresence == 1)
            {
                ins.payload = dec.Raw();
            }
            else if (payloadPresence != 0)
            {
                throw new SLBFormatException("payload presence 非法: " + payloadPresence);
            }
            ins.debugInfo = dec.DebugRef();
            return ins;
        }

        private static SLVariablePackage DecodeVariable(SLBDecoder dec)
        {
            var v = new SLVariablePackage();
            v.id = dec.UVarInt();
            v.index = dec.UVarInt();
            v.name = dec.Str();
            v.typeDef = DecodeOptRuntimeDefType(dec);
            v.debugInfo = dec.DebugRef();
            v.hasExpress = dec.Bool();
            v.defaultConstValue = dec.OptStr();
            v.defaultConstEType = dec.UVarInt();
            v.isConst = dec.Bool();
            return v;
        }

        private static SLFieldPackage DecodeField(SLBDecoder dec)
        {
            var f = new SLFieldPackage();
            f.name = dec.Str();
            f.exportNames = dec.OptStr();
            f.typeDef = DecodeOptRuntimeDefType(dec);
            f.flags = dec.UVarInt();
            f.index = dec.UVarInt();
            f.order = dec.UVarInt();
            int exprCount = Count(dec, "FIELD.express");
            for (int k = 0; k < exprCount; k++)
            {
                f.express.Add(DecodeInstruction(dec));
            }
            int attrCount = Count(dec, "FIELD.attributeList");
            for (int k = 0; k < attrCount; k++)
            {
                f.attributeList.Add(DecodeAttribute(dec));
            }
            return f;
        }

        private static SLMethodMeta DecodeMethodMeta(SLBDecoder dec)
        {
            var mm = new SLMethodMeta();
            mm.id = dec.Str();
            mm.name = dec.Str();
            mm.index = dec.UVarInt();
            return mm;
        }

        private static SLAttributePackage DecodeAttribute(SLBDecoder dec)
        {
            var attr = new SLAttributePackage();
            attr.name = dec.Str();
            int argCount = Count(dec, "Attribute.args");
            for (int k = 0; k < argCount; k++)
            {
                attr.args.Add(dec.Str());
            }
            attr.stage = dec.UVarInt();
            attr.targets = dec.UVarInt();
            return attr;
        }

        private static SLRuntimeDefTypePackage? DecodeOptRuntimeDefType(SLBDecoder dec)
        {
            byte presence = dec.U8();
            if (presence == 0)
            {
                return null;
            }
            if (presence != 1)
            {
                throw new SLBFormatException("可选类型 presence 非法: " + presence);
            }
            return DecodeRuntimeDefType(dec);
        }

        private static SLRuntimeDefTypePackage DecodeRuntimeDefType(SLBDecoder dec)
        {
            var t = new SLRuntimeDefTypePackage();
            t.classId = dec.UVarInt();
            t.className = dec.Str();
            t.ownerClassId = dec.UVarInt();
            t.ownerClassName = dec.Str();
            t.templateIndex = dec.UVarInt();
            t.isTemplate = dec.Bool();
            int childCount = Count(dec, "RuntimeDefType.runtimeDefTypeList");
            for (int k = 0; k < childCount; k++)
            {
                t.runtimeDefTypeList.Add(DecodeRuntimeDefType(dec));
            }
            return t;
        }

        private static SLTemplateRelationPackage DecodeTemplateRelation(SLBDecoder dec)
        {
            var rel = new SLTemplateRelationPackage();
            rel.relatedClassId = dec.UVarInt();
            int mapCount = Count(dec, "TemplateRelation.mapping");
            for (int k = 0; k < mapCount; k++)
            {
                var entry = new SLTemplateRelationEntry();
                entry.index = dec.UVarInt();
                entry.type = DecodeOptRuntimeDefType(dec);
                rel.mapping.Add(entry);
            }
            return rel;
        }

        private static SLSystemCallPackage DecodeSystemCall(SLBDecoder dec)
        {
            var call = new SLSystemCallPackage();
            call.name = dec.Str();
            call.returnType = dec.Str();
            int paramCount = Count(dec, "SystemCall.params");
            for (int k = 0; k < paramCount; k++)
            {
                call.@params.Add(dec.Str());
            }
            call.isVariadic = dec.Bool();
            call.id = dec.UVarInt();
            call.cvmFunction = dec.Str();
            call.className = dec.Str();
            return call;
        }

        private static SLDllImportPackage DecodeDllImport(SLBDecoder dec)
        {
            var imp = new SLDllImportPackage();
            imp.alias = dec.Str();
            imp.name = dec.Str();
            imp.path = dec.Str();
            imp.@static = dec.Str();
            return imp;
        }

        private static SLVmDllImportPackage DecodeVmDllImport(SLBDecoder dec)
        {
            var imp = new SLVmDllImportPackage();
            imp.name = dec.Str();
            return imp;
        }

        private static SLPluginPackage DecodePlugin(SLBDecoder dec)
        {
            var p = new SLPluginPackage();
            p.id = dec.Str();
            p.lib = dec.Str();
            int libsCount = Count(dec, "Plugin.libs");
            for (int k = 0; k < libsCount; k++)
            {
                p.libs.Add(dec.Str());
            }
            p.prefix = dec.Str();
            p.enabled = dec.Bool();
            p.abi = dec.UVarInt();
            byte platformPresence = dec.U8();
            if (platformPresence == 1)
            {
                p.platform = DecodePlatformNode(dec);
            }
            else if (platformPresence != 0)
            {
                throw new SLBFormatException("Plugin.platform presence 非法: " + platformPresence);
            }
            p.onUnavailable = dec.Str();
            int capCount = Count(dec, "Plugin.capabilities");
            for (int k = 0; k < capCount; k++)
            {
                var cap = new SLCapabilityPackage();
                cap.type = dec.Str();
                cap.name = dec.Str();
                p.capabilities.Add(cap);
            }
            return p;
        }

        private static SLAtSignLabelEntryPackage DecodeAtSignLabelEntry(SLBDecoder dec)
        {
            var e = new SLAtSignLabelEntryPackage();
            e.entryIndex = dec.UVarInt();
            e.pluginId = dec.Str();
            e.tag = dec.Str();
            e.entry = dec.Str();
            e.entryMethod = dec.Str();
            e.lib = dec.Str();
            int channelCount = Count(dec, "AtSignLabel.channels");
            for (int k = 0; k < channelCount; k++)
            {
                var ch = new SLAtSignLabelChannelPackage();
                ch.dir = dec.Str();
                ch.slVar = dec.Str();
                ch.slType = dec.Str();
                ch.target = dec.Str();
                e.channels.Add(ch);
            }
            return e;
        }

        private static SLModuleReferencePackage DecodeModuleReference(SLBDecoder dec)
        {
            var r = new SLModuleReferencePackage();
            r.name = dec.Str();
            r.uuid = dec.Str();
            r.path = dec.Str();
            r.versionMain = dec.UVarInt();
            r.versionSub = dec.UVarInt();
            r.versionPatch = dec.UVarInt();
            return r;
        }

        private static SLPlatformPackage DecodePlatform(SLBDecoder dec)
        {
            var p = new SLPlatformPackage();
            p.v = dec.UVarInt();
            int targetCount = Count(dec, "Platform.targets");
            for (int k = 0; k < targetCount; k++)
            {
                p.targets.Add(dec.Str());
            }
            byte rootPresence = dec.U8();
            if (rootPresence == 1)
            {
                p.root = DecodePlatformNode(dec);
            }
            else if (rootPresence != 0)
            {
                throw new SLBFormatException("Platform.root presence 非法: " + rootPresence);
            }
            p.fallbackHint = dec.OptStr();
            p.networkProbe = dec.OptBool();
            byte overridePresence = dec.U8();
            if (overridePresence == 1)
            {
                string json = dec.Utf8();
                try
                {
                    p.@override = JsonNode.Parse(json) as JsonObject;
                }
                catch (JsonException ex)
                {
                    throw new SLBFormatException("Platform.override JSON 解析失败", ex);
                }
                if (p.@override == null)
                {
                    throw new SLBFormatException("Platform.override 不是 JSON 对象");
                }
            }
            else if (overridePresence != 0)
            {
                throw new SLBFormatException("Platform.override presence 非法: " + overridePresence);
            }
            byte variantsPresence = dec.U8();
            if (variantsPresence == 1)
            {
                int variantCount = Count(dec, "Platform.variants");
                p.variants = new List<SLPlatformVariantPackage>();
                for (int k = 0; k < variantCount; k++)
                {
                    p.variants.Add(DecodePlatformVariant(dec));
                }
            }
            else if (variantsPresence != 0)
            {
                throw new SLBFormatException("Platform.variants presence 非法: " + variantsPresence);
            }
            return p;
        }

        private static SLPlatformVariantPackage DecodePlatformVariant(SLBDecoder dec)
        {
            var v = new SLPlatformVariantPackage();
            v.target = dec.OptStr();
            v.aot = dec.OptStr();
            byte rootPresence = dec.U8();
            if (rootPresence == 1)
            {
                v.root = DecodePlatformNode(dec);
            }
            else if (rootPresence != 0)
            {
                throw new SLBFormatException("PlatformVariant.root presence 非法: " + rootPresence);
            }
            return v;
        }

        private static SLPlatformNodePackage DecodePlatformNode(SLBDecoder dec)
        {
            var n = new SLPlatformNodePackage();
            n.op = dec.Str();
            n.kind = dec.OptStr();
            n.cmp = dec.OptStr();
            n.key = dec.OptStr();
            n.value = dec.OptStr();
            byte setPresence = dec.U8();
            if (setPresence == 1)
            {
                int setCount = Count(dec, "PlatformNode.set");
                n.set = new List<string>();
                for (int k = 0; k < setCount; k++)
                {
                    n.set.Add(dec.Str());
                }
            }
            else if (setPresence != 0)
            {
                throw new SLBFormatException("PlatformNode.set presence 非法: " + setPresence);
            }
            n.optional = dec.OptBool();
            byte childrenPresence = dec.U8();
            if (childrenPresence == 1)
            {
                int childCount = Count(dec, "PlatformNode.children");
                n.children = new List<SLPlatformNodePackage>();
                for (int k = 0; k < childCount; k++)
                {
                    n.children.Add(DecodePlatformNode(dec));
                }
            }
            else if (childrenPresence != 0)
            {
                throw new SLBFormatException("PlatformNode.children presence 非法: " + childrenPresence);
            }
            return n;
        }

        private static SLAotPackage DecodeAot(SLBDecoder dec)
        {
            var a = new SLAotPackage();
            a.enabled = dec.Bool();
            a.mlir = dec.OptStr();
            a.dll = dec.Str();
            int methodCount = Count(dec, "Aot.methods");
            for (int k = 0; k < methodCount; k++)
            {
                var m = new SLAotMethodPackage();
                m.id = dec.UVarInt();
                m.symbol = dec.Str();
                m.status = dec.Str();
                m.reason = dec.OptStr();
                int paramCount = Count(dec, "Aot.method.paramList");
                for (int j = 0; j < paramCount; j++)
                {
                    var param = new SLAotParamPackage();
                    param.slot = dec.UVarInt();
                    param.typeId = dec.UVarInt();
                    param.typeName = dec.Str();
                    m.paramList.Add(param);
                }
                m.retSlot = dec.UVarInt();
                m.retTypeId = dec.UVarInt();
                a.methods.Add(m);
            }
            int typeCount = Count(dec, "Aot.typeList");
            for (int k = 0; k < typeCount; k++)
            {
                var t = new SLAotTypePackage();
                t.classId = dec.UVarInt();
                t.fullName = dec.Str();
                t.metaClassKind = dec.UVarInt();
                t.baseClassId = dec.UVarInt();
                t.templateParameterCount = dec.UVarInt();
                t.nativeSize = dec.UVarInt();
                t.fastPath = dec.Bool();
                int layoutCount = Count(dec, "Aot.type.layout");
                for (int j = 0; j < layoutCount; j++)
                {
                    var layout = new SLAotLayoutEntryPackage();
                    layout.index = dec.UVarInt();
                    layout.offset = dec.UVarInt();
                    layout.size = dec.UVarInt();
                    layout.slot = dec.UVarInt();
                    layout.name = dec.Str();
                    layout.vmOffset = dec.UVarInt();
                    layout.nestedTypeId = dec.UVarInt();
                    t.layout.Add(layout);
                }
                a.typeList.Add(t);
            }
            return a;
        }

        // ---- 辅助 ----

        /// <summary>读列表元素数并做防恶意大 count 上界校验（每元素至少 1 字节）。</summary>
        private static int Count(SLBDecoder dec, string sectionName)
        {
            long c = dec.UVar();
            if (c < 0 || c > int.MaxValue || c > dec.Remaining)
            {
                throw new SLBFormatException(sectionName + " 数量非法: " + c);
            }
            return (int)c;
        }

        private static (int offset, int length) RequireSection(Dictionary<uint, (int offset, int length)> sections, uint type, string name)
        {
            if (sections.TryGetValue(type, out var section))
            {
                return section;
            }
            throw new SLBFormatException("缺少必需段 " + name);
        }

        private static void RequireDrained(SLBDecoder dec, string sectionName)
        {
            if (dec.Remaining != 0)
            {
                throw new SLBFormatException(sectionName + " 段有尾部冗余字节: " + dec.Remaining);
            }
        }

        private static byte[] ZLibDecompress(byte[] input, int offset, int count)
        {
            try
            {
                using var source = new MemoryStream(input, offset, count, writable: false);
                using var zlib = new ZLibStream(source, CompressionMode.Decompress);
                using var output = new MemoryStream();
                zlib.CopyTo(output);
                return output.ToArray();
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException)
            {
                throw new SLBFormatException("zlib 解压失败", ex);
            }
        }

        private static int GetU16(byte[] data, int offset)
        {
            return (int)(ushort)(data[offset] | (data[offset + 1] << 8));
        }

        private static uint GetU32(byte[] data, int offset)
        {
            return (uint)data[offset] | ((uint)data[offset + 1] << 8) | ((uint)data[offset + 2] << 16) | ((uint)data[offset + 3] << 24);
        }
    }
}
