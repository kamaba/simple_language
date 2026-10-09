#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using SimpleLanguage.Export.SLIR.Types;
using SimpleLanguage.Logging;

namespace SimpleLanguage.Export.SLIR.Binary
{
    /// <summary>
    /// SLB 字符串池：全段共享，首次出现序编号。
    /// 编码侧 Str/OptStr 一律经 Intern 取池索引（poolRef = uvarint 索引）；
    /// POOL 段在其余段编码完成后最后生成（保证表完备）。
    /// </summary>
    internal sealed class SLBStringPool
    {
        private readonly List<string> items = new List<string>();
        private readonly Dictionary<string, int> index = new Dictionary<string, int>(StringComparer.Ordinal);

        public int Count { get { return items.Count; } }

        public string Get(int i) { return items[i]; }

        public int Intern(string value)
        {
            if (index.TryGetValue(value, out var idx)) return idx;
            idx = items.Count;
            items.Add(value);
            index[value] = idx;
            return idx;
        }
    }

    /// <summary>
    /// 一次导出的共享上下文：字符串池 + 调试信息表 + hasDebug 预扫描结果。
    /// Debug 表按引用相等去重（SLInstructionDebugInfo 未重写 Equals），
    /// InternDebug 返回 index+1（0 保留给 null）；DEBUG 段最后生成。
    /// </summary>
    internal sealed class WriteContext
    {
        public readonly SLBStringPool Pool = new SLBStringPool();

        private readonly List<SLInstructionDebugInfo> debugTable = new List<SLInstructionDebugInfo>();
        private readonly Dictionary<SLInstructionDebugInfo, int> debugIndex =
            new Dictionary<SLInstructionDebugInfo, int>(ReferenceEqualityComparer.Instance);

        /// <summary>预扫描结果（ScanHasDebug）：决定 headerFlags bit2 与 DebugRef 是否写字段。</summary>
        public bool HasDebug { get; }

        public WriteContext(bool hasDebug)
        {
            HasDebug = hasDebug;
        }

        public int DebugCount { get { return debugTable.Count; } }

        public SLInstructionDebugInfo GetDebug(int i) { return debugTable[i]; }

        public int InternDebug(SLInstructionDebugInfo dbg)
        {
            if (debugIndex.TryGetValue(dbg, out var idx)) return idx + 1;
            idx = debugTable.Count;
            debugTable.Add(dbg);
            debugIndex[dbg] = idx;
            return idx + 1;
        }
    }

    /// <summary>
    /// SLB 编码原语：包装 MemoryStream + BinaryWriter。
    /// uvarint = 位模式 LEB128（手写编码（低位在前、续位 0x80），
    /// int 字段统一按 uint 位模式编码，负数如 -1 → 0xFFFFFFFF 5 字节，无损 round-trip）。
    /// </summary>
    internal sealed class SLBEncoder
    {
        private readonly MemoryStream stream;
        private readonly BinaryWriter writer;
        private readonly WriteContext ctx;

        public SLBEncoder(WriteContext ctx)
        {
            this.ctx = ctx;
            stream = new MemoryStream();
            writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        }

        public byte[] ToArray()
        {
            writer.Flush();
            return stream.ToArray();
        }

        /// <summary>手写 LEB128（BCL 的 Write7BitEncodedLong 是 protected 不可外部调用）：低位在前、续位 0x80。</summary>
        private static void PutUVar(BinaryWriter w, long value)
        {
            ulong v = (ulong)value;
            while (v >= 0x80)
            {
                w.Write((byte)(v | 0x80));
                v >>= 7;
            }
            w.Write((byte)v);
        }

        /// <summary>uvarint（原始 long 位模式）。</summary>
        public void UVar(long value) { PutUVar(writer, value); }

        /// <summary>int 字段的 uvarint：按 uint 位模式编码（-1 → 0xFFFFFFFF）。</summary>
        public void UVarInt(int value) { PutUVar(writer, (long)(uint)value); }

        public void U8(byte value) { writer.Write(value); }

        public void Bool(bool value) { writer.Write((byte)(value ? 1 : 0)); }

        public void Raw(byte[] data) { writer.Write(data); }

        /// <summary>poolRef：Intern 取索引后写 uvarint。</summary>
        public void Str(string value) { PutUVar(writer, ctx.Pool.Intern(value ?? string.Empty)); }

        /// <summary>可空字符串：presence u8 + poolRef（null 只写 0）。</summary>
        public void OptStr(string? value)
        {
            if (value == null)
            {
                writer.Write((byte)0);
            }
            else
            {
                writer.Write((byte)1);
                PutUVar(writer, ctx.Pool.Intern(value));
            }
        }

        /// <summary>可空 bool：presence u8 + bool u8。</summary>
        public void OptBool(bool? value)
        {
            if (value == null)
            {
                writer.Write((byte)0);
            }
            else
            {
                writer.Write((byte)1);
                writer.Write((byte)(value.Value ? 1 : 0));
            }
        }

        /// <summary>独立 UTF-8 字符串（不经池）：uvarint byteLen + bytes。</summary>
        public void Utf8(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            PutUVar(writer, bytes.Length);
            writer.Write(bytes);
        }

        /// <summary>
        /// debugInfoId：仅当 HasDebug（headerFlags bit2）时写字段，uvarint(InternDebug)；
        /// null 写 0。HasDebug=false 时该字段整个不存在（读侧同开关跳过）。
        /// </summary>
        public void DebugRef(SLInstructionDebugInfo? dbg)
        {
            if (!ctx.HasDebug) return;
            PutUVar(writer, dbg == null ? 0L : ctx.InternDebug(dbg));
        }
    }

    /// <summary>
    /// SLB 写入器：SLModulePackage → .slb 二进制模块包（v1.0，规范见 SLB_DESIGN.md §4）。
    /// 与 JSON 通道共用同一份 SLModulePackage 中间数据（Build 一次、双通道投影）。
    /// </summary>
    public static class SLBWriter
    {
        /// <summary>编码并写文件（含目录创建与成功日志）。</summary>
        public static void Write(SLModulePackage pkg, string outputPath, bool compressed)
        {
            if (pkg == null) throw new ArgumentNullException(nameof(pkg));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentNullException(nameof(outputPath));

            var bytes = WriteToBytes(pkg, compressed);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
            File.WriteAllBytes(outputPath, bytes);

            Log.AddIRLog(LID.ExportSLModulePackageExportModuleSuccess, "export module success (binary): " + outputPath);
        }

        /// <summary>编码到内存字节流：FileHeader(48B) + DataArea（[段表][段数据]，可选整体 zlib 压缩）。</summary>
        public static byte[] WriteToBytes(SLModulePackage pkg, bool compressed)
        {
            if (pkg == null) throw new ArgumentNullException(nameof(pkg));

            var ctx = new WriteContext(ScanHasDebug(pkg));

            // 各段编码（共享 ctx 收集字符串池/调试表；POOL/DEBUG 依赖表完备须最后编码）
            var metaBytes = EncodeMetaSection(pkg, ctx);
            var nsBytes = EncodeNamespaceSection(pkg, ctx);
            var classBytes = EncodeClassSection(pkg, ctx);
            var globalBytes = EncodeGlobalSection(pkg, ctx);
            var methodBytes = EncodeMethodSection(pkg, ctx);
            var codeBytes = EncodeCodeSection(pkg, ctx);
            var irStringBytes = EncodeIRStringSection(pkg, ctx);
            var debugBytes = EncodeDebugSection(ctx);
            var poolBytes = EncodePoolSection(ctx);

            // 段清单（物理顺序 POOL→META→NS→CLASS→GLOBAL→METHOD→CODE→IRSTRING→DEBUG；
            // 必需段恒写（空内容也写），可选段仅有数据时写）
            var sections = new List<(uint type, byte[] data)>
            {
                (SLBFormat.SectionPool, poolBytes),
                (SLBFormat.SectionMeta, metaBytes),
            };
            if (nsBytes != null) sections.Add((SLBFormat.SectionNamespace, nsBytes));
            sections.Add((SLBFormat.SectionClass, classBytes));
            if (globalBytes != null) sections.Add((SLBFormat.SectionGlobal, globalBytes));
            sections.Add((SLBFormat.SectionMethod, methodBytes));
            sections.Add((SLBFormat.SectionCode, codeBytes));
            if (irStringBytes != null) sections.Add((SLBFormat.SectionIRString, irStringBytes));
            if (debugBytes != null) sections.Add((SLBFormat.SectionDebug, debugBytes));

            // DataArea = 段表 + 段数据（offset 相对 DataArea 起点，第一段 offset = 段表长度）
            var dataArea = new MemoryStream();
            int tableEnd = sections.Count * SLBFormat.SectionTableEntrySize;
            var offsets = new int[sections.Count];
            int cur = tableEnd;
            for (int i = 0; i < sections.Count; i++)
            {
                offsets[i] = cur;
                cur += sections[i].data.Length;
            }
            for (int i = 0; i < sections.Count; i++)
            {
                PutU32(dataArea, sections[i].type);
                PutU32(dataArea, (uint)offsets[i]);
                PutU32(dataArea, (uint)sections[i].data.Length);
                PutU32(dataArea, SLBCrc32.Compute(sections[i].data));
                PutU32(dataArea, 0); // reserved u8[8]
                PutU32(dataArea, 0);
            }
            for (int i = 0; i < sections.Count; i++)
            {
                dataArea.Write(sections[i].data, 0, sections[i].data.Length);
            }
            var dataAreaBytes = dataArea.ToArray();

            // FileHeader 48B（小端）
            uint headerFlags = 0;
            if (compressed) headerFlags |= SLBFormat.FlagCompressed;
            if (ctx.HasDebug) headerFlags |= SLBFormat.FlagDebugInfo;
            if (pkg.aot != null) headerFlags |= SLBFormat.FlagMetaAot;

            var uuidBytes = ParseUuid(pkg.uuid);

            var header = new byte[SLBFormat.FileHeaderSize];
            Array.Copy(SLBFormat.Magic, 0, header, 0, SLBFormat.Magic.Length);
            PutU16(header, 4, SLBFormat.FormatMajor);
            PutU16(header, 6, SLBFormat.FormatMinor);
            PutU32(header, 8, headerFlags);
            Array.Copy(uuidBytes, 0, header, 12, SLBFormat.UuidLength);
            PutU64(header, 28, (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            PutU32(header, 36, (uint)dataAreaBytes.Length);
            PutU16(header, 40, (ushort)sections.Count);
            PutU16(header, 42, 0); // reserved
            PutU32(header, 44, SLBCrc32.Compute(header, 0, SLBFormat.HeaderCrcCoverLength));

            // 压缩分叉（唯一处）：DataArea 整体一次 zlib，容器 [u32 LE dataRawTotal][zlib stream]；头部恒明文
            byte[] payload;
            if (compressed)
            {
                using var raw = new MemoryStream();
                var prefix = new byte[4];
                PutU32(prefix, 0, (uint)dataAreaBytes.Length);
                raw.Write(prefix, 0, prefix.Length);
                using (var zlib = new ZLibStream(raw, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlib.Write(dataAreaBytes, 0, dataAreaBytes.Length);
                }
                payload = raw.ToArray();
            }
            else
            {
                payload = dataAreaBytes;
            }

            var result = new byte[SLBFormat.FileHeaderSize + payload.Length];
            Buffer.BlockCopy(header, 0, result, 0, header.Length);
            Buffer.BlockCopy(payload, 0, result, header.Length, payload.Length);
            return result;
        }

        // ------------------------------------------------------------------
        // 段编码
        // ------------------------------------------------------------------

        /// <summary>预扫描 hasDebug：决定 headerFlags bit2 与 debugInfoId 字段的有无（一次编码）。</summary>
        private static bool ScanHasDebug(SLModulePackage pkg)
        {
            foreach (var m in pkg.methodList)
            {
                if (m == null) continue;
                foreach (var v in m.returnList) if (v?.debugInfo != null) return true;
                foreach (var v in m.argumentList) if (v?.debugInfo != null) return true;
                foreach (var v in m.localList) if (v?.debugInfo != null) return true;
                foreach (var ins in m.instructionList) if (ins?.debugInfo != null) return true;
            }
            foreach (var c in pkg.classList)
            {
                if (c == null) continue;
                foreach (var f in c.fieldList)
                {
                    if (f == null) continue;
                    foreach (var ins in f.express) if (ins?.debugInfo != null) return true;
                }
            }
            foreach (var g in pkg.globalStaticVariableList)
            {
                if (g == null) continue;
                foreach (var ins in g.express) if (ins?.debugInfo != null) return true;
            }
            return false;
        }

        private static byte[] EncodePoolSection(WriteContext ctx)
        {
            var enc = new SLBEncoder(ctx);
            enc.UVar(ctx.Pool.Count);
            for (int i = 0; i < ctx.Pool.Count; i++)
            {
                enc.Utf8(ctx.Pool.Get(i));
            }
            return enc.ToArray();
        }

        /// <summary>META 段：tag-TLV（u8 tag + uvarint valueLen + value bytes）。
        /// 列表类 tag 恒写（空列表 count=0）；可空标量缺 tag = null。</summary>
        private static byte[] EncodeMetaSection(SLModulePackage pkg, WriteContext ctx)
        {
            var enc = new SLBEncoder(ctx);

            void Tag(byte tag, Action<SLBEncoder> value)
            {
                var tmp = new SLBEncoder(ctx);
                value(tmp);
                var bytes = tmp.ToArray();
                enc.U8(tag);
                enc.UVar(bytes.Length);
                enc.Raw(bytes);
            }

            Tag(SLBFormat.MetaTagModuleName, v => v.Str(pkg.moduleName));
            Tag(SLBFormat.MetaTagVersionMain, v => v.UVarInt(pkg.versionMain));
            Tag(SLBFormat.MetaTagVersionSub, v => v.UVarInt(pkg.versionSub));
            Tag(SLBFormat.MetaTagVersionPatch, v => v.UVarInt(pkg.versionPatch));
            if (pkg.entryMethodId.HasValue)
            {
                var entry = pkg.entryMethodId.Value;
                Tag(SLBFormat.MetaTagEntryMethodId, v => v.UVarInt(entry));
            }
            Tag(SLBFormat.MetaTagBuildMode, v => v.Str(pkg.buildMode));
            Tag(SLBFormat.MetaTagOptimizeLevel, v => v.UVarInt(pkg.optimizeLevel));
            Tag(SLBFormat.MetaTagNativeDll, v => v.Str(pkg.nativeDll));
            if (pkg.platform != null)
            {
                var platform = pkg.platform;
                Tag(SLBFormat.MetaTagPlatform, v => EncodePlatform(v, platform));
            }
            if (pkg.aot != null)
            {
                var aot = pkg.aot;
                Tag(SLBFormat.MetaTagAot, v => EncodeAot(v, aot));
            }
            Tag(SLBFormat.MetaTagSystemCalls, v =>
            {
                v.UVar(pkg.systemCalls.Count);
                foreach (var s in pkg.systemCalls) EncodeSystemCall(v, s);
            });
            Tag(SLBFormat.MetaTagDllImports, v =>
            {
                v.UVar(pkg.dllImports.Count);
                foreach (var d in pkg.dllImports) EncodeDllImport(v, d);
            });
            Tag(SLBFormat.MetaTagVmDllImports, v =>
            {
                v.UVar(pkg.vmDllImports.Count);
                foreach (var d in pkg.vmDllImports) EncodeVmDllImport(v, d);
            });
            Tag(SLBFormat.MetaTagPlugins, v =>
            {
                v.UVar(pkg.plugins.Count);
                foreach (var p in pkg.plugins) EncodePlugin(v, p);
            });
            Tag(SLBFormat.MetaTagAtSignLabel, v =>
            {
                v.UVar(pkg.atSignLabel.Count);
                foreach (var a in pkg.atSignLabel) EncodeAtSignLabelEntry(v, a);
            });
            Tag(SLBFormat.MetaTagModuleReferences, v =>
            {
                v.UVar(pkg.moduleReferences.Count);
                foreach (var r in pkg.moduleReferences) EncodeModuleReference(v, r);
            });

            return enc.ToArray();
        }

        private static byte[]? EncodeNamespaceSection(SLModulePackage pkg, WriteContext ctx)
        {
            if (pkg.namespaceList.Count == 0) return null;
            var enc = new SLBEncoder(ctx);
            enc.UVar(pkg.namespaceList.Count);
            foreach (var ns in pkg.namespaceList)
            {
                enc.Str(ns.fullName);
                enc.UVar(ns.typeList.Count);
                foreach (var t in ns.typeList)
                {
                    enc.Str(t.fullName);
                    enc.Str(t.name);
                    enc.UVar(t.methodList.Count);
                    foreach (var mm in t.methodList) EncodeMethodMeta(enc, mm);
                    enc.UVarInt(t.templateParameterCount);
                }
            }
            return enc.ToArray();
        }

        private static byte[] EncodeClassSection(SLModulePackage pkg, WriteContext ctx)
        {
            var enc = new SLBEncoder(ctx);
            enc.UVar(pkg.classList.Count);
            foreach (var c in pkg.classList)
            {
                enc.UVarInt(c.id);
                enc.Str(c.name);
                enc.Str(c.fullName);
                enc.OptStr(c.exportNames);
                enc.Str(c.sourcePath);
                enc.UVarInt(c.metaClassKind);
                enc.UVarInt(c.permission);
                enc.Bool(c.isDynamic);
                enc.Bool(c.isFinal);
                enc.UVarInt(c.baseClassId);
                enc.UVar(c.implementsInterfaceIdList.Count);
                foreach (var iid in c.implementsInterfaceIdList) enc.UVarInt(iid);
                enc.UVar(c.fieldList.Count);
                foreach (var f in c.fieldList) EncodeField(enc, f);
                enc.UVar(c.nonStaticMethodList.Count);
                foreach (var mm in c.nonStaticMethodList) EncodeMethodMeta(enc, mm);
                enc.UVar(c.operatorMethodList.Count);
                foreach (var mm in c.operatorMethodList) EncodeMethodMeta(enc, mm);
                enc.UVar(c.staticMethodList.Count);
                foreach (var mm in c.staticMethodList) EncodeMethodMeta(enc, mm);
                enc.UVarInt(c.templateCount);
                enc.UVarInt(c.templateParameterCount);
                enc.UVar(c.templateParameterNames.Count);
                foreach (var tn in c.templateParameterNames) enc.Str(tn);
                enc.UVar(c.templateTypeList.Count);
                foreach (var tt in c.templateTypeList) EncodeRuntimeDefType(enc, tt);
                enc.UVar(c.templateRelationList.Count);
                foreach (var tr in c.templateRelationList) EncodeTemplateRelation(enc, tr);
                enc.UVar(c.attributeList.Count);
                foreach (var a in c.attributeList) EncodeAttribute(enc, a);
            }
            return enc.ToArray();
        }

        private static byte[]? EncodeGlobalSection(SLModulePackage pkg, WriteContext ctx)
        {
            if (pkg.globalStaticVariableList.Count == 0) return null;
            var enc = new SLBEncoder(ctx);
            enc.UVar(pkg.globalStaticVariableList.Count);
            foreach (var g in pkg.globalStaticVariableList)
            {
                enc.UVarInt(g.id);
                enc.Str(g.name);
                enc.UVarInt(g.ownerClassId);
                enc.UVarInt(g.index);
                EncodeOptRuntimeDefType(enc, g.typeDef);
                enc.UVar(g.express.Count);
                foreach (var ins in g.express) EncodeInstruction(enc, ins);
            }
            return enc.ToArray();
        }

        private static byte[] EncodeMethodSection(SLModulePackage pkg, WriteContext ctx)
        {
            var enc = new SLBEncoder(ctx);
            int instrStart = 0;
            enc.UVar(pkg.methodList.Count);
            foreach (var m in pkg.methodList)
            {
                enc.Str(m.id);
                enc.UVarInt(m.methodId);
                enc.Str(m.name);
                enc.OptStr(m.exportNames);
                enc.Str(m.declaringTypeFullName);
                enc.UVarInt(m.declaringClassId);
                enc.Bool(m.interfaceMethod);
                enc.UVarInt(m.flags);
                enc.Bool(m.isTemplateFunction);
                enc.UVar(m.templateParameterNames.Count);
                foreach (var tn in m.templateParameterNames) enc.Str(tn);
                enc.UVar(m.returnList.Count);
                foreach (var v in m.returnList) EncodeVariable(enc, v);
                enc.UVar(m.argumentList.Count);
                foreach (var v in m.argumentList) EncodeVariable(enc, v);
                enc.UVar(m.localList.Count);
                foreach (var v in m.localList) EncodeVariable(enc, v);
                enc.UVarInt(instrStart);
                enc.UVarInt(m.instructionList.Count);
                instrStart += m.instructionList.Count;
                enc.UVar(m.attributeList.Count);
                foreach (var a in m.attributeList) EncodeAttribute(enc, a);
            }
            return enc.ToArray();
        }

        /// <summary>CODE 段：全部方法指令按 methodList 顺序拼接（无 count 前缀，
        /// 条数由 METHOD 段 instrStart/instrCount 决定，指向本段全局指令序）。</summary>
        private static byte[] EncodeCodeSection(SLModulePackage pkg, WriteContext ctx)
        {
            var enc = new SLBEncoder(ctx);
            foreach (var m in pkg.methodList)
            {
                foreach (var ins in m.instructionList)
                {
                    EncodeInstruction(enc, ins);
                }
            }
            return enc.ToArray();
        }

        private static byte[]? EncodeIRStringSection(SLModulePackage pkg, WriteContext ctx)
        {
            if (pkg.irStringDict.Count == 0) return null;
            var enc = new SLBEncoder(ctx);
            enc.UVar(pkg.irStringDict.Count);
            foreach (var item in pkg.irStringDict)
            {
                enc.UVarInt(item.id);
                enc.Utf8(item.value);
            }
            return enc.ToArray();
        }

        private static byte[]? EncodeDebugSection(WriteContext ctx)
        {
            if (!ctx.HasDebug) return null;
            var enc = new SLBEncoder(ctx);
            enc.UVar(ctx.DebugCount);
            for (int i = 0; i < ctx.DebugCount; i++)
            {
                var d = ctx.GetDebug(i);
                enc.Str(d.path);
                enc.Str(d.name);
                enc.Str(d.info);
                enc.UVarInt(d.beginLine);
                enc.UVarInt(d.beginChar);
                enc.UVarInt(d.endLine);
                enc.UVarInt(d.endChar);
            }
            return enc.ToArray();
        }

        // ------------------------------------------------------------------
        // 子结构编码（声明序紧凑编码，无 tag）
        // ------------------------------------------------------------------

        /// <summary>单条指令（CODE 段与 field/global express 内嵌共用）：
        /// uvarint id + u8 opCode + uvarint byteLength + u8 hasPayload +
        /// [uvarint payloadLen + bytes] + [HasDebug] uvarint debugId+1。
        /// hasPayload 区分 null 与 byte[0] 保真。</summary>
        private static void EncodeInstruction(SLBEncoder enc, SLIRInstructionPackage ins)
        {
            enc.UVarInt(ins.id);
            enc.U8(ins.opCode);
            enc.UVarInt(ins.byteLength);
            if (ins.payload == null)
            {
                enc.U8(0);
            }
            else
            {
                enc.U8(1);
                enc.UVar(ins.payload.Length);
                enc.Raw(ins.payload);
            }
            enc.DebugRef(ins.debugInfo);
        }

        private static void EncodeVariable(SLBEncoder enc, SLVariablePackage v)
        {
            enc.UVarInt(v.id);
            enc.UVarInt(v.index);
            enc.Str(v.name);
            EncodeOptRuntimeDefType(enc, v.typeDef);
            enc.DebugRef(v.debugInfo);
            enc.Bool(v.hasExpress);
            enc.OptStr(v.defaultConstValue);
            enc.UVarInt(v.defaultConstEType);
            enc.Bool(v.isConst);
        }

        private static void EncodeField(SLBEncoder enc, SLFieldPackage f)
        {
            enc.Str(f.name);
            enc.OptStr(f.exportNames);
            EncodeOptRuntimeDefType(enc, f.typeDef);
            enc.UVarInt(f.flags);
            enc.UVarInt(f.index);
            enc.UVarInt(f.order);
            enc.UVar(f.express.Count);
            foreach (var ins in f.express) EncodeInstruction(enc, ins);
            enc.UVar(f.attributeList.Count);
            foreach (var a in f.attributeList) EncodeAttribute(enc, a);
        }

        private static void EncodeMethodMeta(SLBEncoder enc, SLMethodMeta mm)
        {
            enc.Str(mm.id);
            enc.Str(mm.name);
            enc.UVarInt(mm.index);
        }

        private static void EncodeAttribute(SLBEncoder enc, SLAttributePackage a)
        {
            enc.Str(a.name);
            enc.UVar(a.args.Count);
            foreach (var arg in a.args) enc.Str(arg);
            enc.UVarInt(a.stage);
            enc.UVarInt(a.targets);
        }

        private static void EncodeOptRuntimeDefType(SLBEncoder enc, SLRuntimeDefTypePackage? t)
        {
            if (t == null)
            {
                enc.U8(0);
            }
            else
            {
                enc.U8(1);
                EncodeRuntimeDefType(enc, t);
            }
        }

        private static void EncodeRuntimeDefType(SLBEncoder enc, SLRuntimeDefTypePackage t)
        {
            enc.UVarInt(t.classId);
            enc.Str(t.className);
            enc.UVarInt(t.ownerClassId);
            enc.Str(t.ownerClassName);
            enc.UVarInt(t.templateIndex);
            enc.Bool(t.isTemplate);
            enc.UVar(t.runtimeDefTypeList.Count);
            foreach (var child in t.runtimeDefTypeList) EncodeRuntimeDefType(enc, child);
        }

        private static void EncodeTemplateRelation(SLBEncoder enc, SLTemplateRelationPackage tr)
        {
            enc.UVarInt(tr.relatedClassId);
            enc.UVar(tr.mapping.Count);
            foreach (var e in tr.mapping)
            {
                enc.UVarInt(e.index);
                EncodeOptRuntimeDefType(enc, e.type);
            }
        }

        private static void EncodeSystemCall(SLBEncoder enc, SLSystemCallPackage s)
        {
            enc.Str(s.name);
            enc.Str(s.returnType);
            enc.UVar(s.@params.Count);
            foreach (var p in s.@params) enc.Str(p);
            enc.Bool(s.isVariadic);
            enc.UVarInt(s.id);
            enc.Str(s.cvmFunction);
            enc.Str(s.className);
        }

        private static void EncodeDllImport(SLBEncoder enc, SLDllImportPackage d)
        {
            enc.Str(d.alias);
            enc.Str(d.name);
            enc.Str(d.path);
            enc.Str(d.@static);
        }

        private static void EncodeVmDllImport(SLBEncoder enc, SLVmDllImportPackage d)
        {
            enc.Str(d.name);
        }

        private static void EncodePlugin(SLBEncoder enc, SLPluginPackage p)
        {
            enc.Str(p.id);
            enc.Str(p.lib);
            enc.UVar(p.libs.Count);
            foreach (var l in p.libs) enc.Str(l);
            enc.Str(p.prefix);
            enc.Bool(p.enabled);
            enc.UVarInt(p.abi);
            if (p.platform != null)
            {
                enc.U8(1);
                EncodePlatformNode(enc, p.platform);
            }
            else
            {
                enc.U8(0);
            }
            enc.Str(p.onUnavailable);
            enc.UVar(p.capabilities.Count);
            foreach (var c in p.capabilities)
            {
                enc.Str(c.type);
                enc.Str(c.name);
            }
        }

        private static void EncodeAtSignLabelEntry(SLBEncoder enc, SLAtSignLabelEntryPackage a)
        {
            enc.UVarInt(a.entryIndex);
            enc.Str(a.pluginId);
            enc.Str(a.tag);
            enc.Str(a.entry);
            enc.Str(a.entryMethod);
            enc.Str(a.lib);
            enc.UVar(a.channels.Count);
            foreach (var ch in a.channels)
            {
                enc.Str(ch.dir);
                enc.Str(ch.slVar);
                enc.Str(ch.slType);
                enc.Str(ch.target);
            }
        }

        private static void EncodeModuleReference(SLBEncoder enc, SLModuleReferencePackage r)
        {
            enc.Str(r.name);
            enc.Str(r.uuid);
            enc.Str(r.path);
            enc.UVarInt(r.versionMain);
            enc.UVarInt(r.versionSub);
            enc.UVarInt(r.versionPatch);
        }

        private static void EncodePlatform(SLBEncoder enc, SLPlatformPackage p)
        {
            enc.UVarInt(p.v);
            enc.UVar(p.targets.Count);
            foreach (var t in p.targets) enc.Str(t);
            if (p.root != null)
            {
                enc.U8(1);
                EncodePlatformNode(enc, p.root);
            }
            else
            {
                enc.U8(0);
            }
            enc.OptStr(p.fallbackHint);
            enc.OptBool(p.networkProbe);
            if (p.@override != null)
            {
                enc.U8(1);
                enc.Utf8(p.@override.ToJsonString());
            }
            else
            {
                enc.U8(0);
            }
            if (p.variants != null)
            {
                enc.U8(1);
                enc.UVar(p.variants.Count);
                foreach (var v in p.variants) EncodePlatformVariant(enc, v);
            }
            else
            {
                enc.U8(0);
            }
        }

        private static void EncodePlatformVariant(SLBEncoder enc, SLPlatformVariantPackage v)
        {
            enc.OptStr(v.target);
            enc.OptStr(v.aot);
            if (v.root != null)
            {
                enc.U8(1);
                EncodePlatformNode(enc, v.root);
            }
            else
            {
                enc.U8(0);
            }
        }

        private static void EncodePlatformNode(SLBEncoder enc, SLPlatformNodePackage n)
        {
            enc.Str(n.op);
            enc.OptStr(n.kind);
            enc.OptStr(n.cmp);
            enc.OptStr(n.key);
            enc.OptStr(n.value);
            if (n.set != null)
            {
                enc.U8(1);
                enc.UVar(n.set.Count);
                foreach (var s in n.set) enc.Str(s);
            }
            else
            {
                enc.U8(0);
            }
            enc.OptBool(n.optional);
            if (n.children != null)
            {
                enc.U8(1);
                enc.UVar(n.children.Count);
                foreach (var c in n.children) EncodePlatformNode(enc, c);
            }
            else
            {
                enc.U8(0);
            }
        }

        private static void EncodeAot(SLBEncoder enc, SLAotPackage a)
        {
            enc.Bool(a.enabled);
            enc.OptStr(a.mlir);
            enc.Str(a.dll);
            enc.UVar(a.methods.Count);
            foreach (var m in a.methods)
            {
                enc.UVarInt(m.id);
                enc.Str(m.symbol);
                enc.Str(m.status);
                enc.OptStr(m.reason);
                enc.UVar(m.paramList.Count);
                foreach (var p in m.paramList)
                {
                    enc.UVarInt(p.slot);
                    enc.UVarInt(p.typeId);
                    enc.Str(p.typeName);
                }
                enc.UVarInt(m.retSlot);
                enc.UVarInt(m.retTypeId);
            }
            enc.UVar(a.typeList.Count);
            foreach (var t in a.typeList)
            {
                enc.UVarInt(t.classId);
                enc.Str(t.fullName);
                enc.UVarInt(t.metaClassKind);
                enc.UVarInt(t.baseClassId);
                enc.UVarInt(t.templateParameterCount);
                enc.UVarInt(t.nativeSize);
                enc.Bool(t.fastPath);
                enc.UVar(t.layout.Count);
                foreach (var l in t.layout)
                {
                    enc.UVarInt(l.index);
                    enc.UVarInt(l.offset);
                    enc.UVarInt(l.size);
                    enc.UVarInt(l.slot);
                    enc.Str(l.name);
                    enc.UVarInt(l.vmOffset);
                    enc.UVarInt(l.nestedTypeId);
                }
            }
        }

        // ------------------------------------------------------------------
        // 小端写原语与工具
        // ------------------------------------------------------------------

        private static void PutU16(byte[] buf, int offset, ushort value)
        {
            buf[offset] = (byte)(value & 0xFF);
            buf[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        private static void PutU32(byte[] buf, int offset, uint value)
        {
            buf[offset] = (byte)(value & 0xFF);
            buf[offset + 1] = (byte)((value >> 8) & 0xFF);
            buf[offset + 2] = (byte)((value >> 16) & 0xFF);
            buf[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        private static void PutU64(byte[] buf, int offset, ulong value)
        {
            for (int i = 0; i < 8; i++)
            {
                buf[offset + i] = (byte)((value >> (8 * i)) & 0xFF);
            }
        }

        private static void PutU32(MemoryStream stream, uint value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 24) & 0xFF));
        }

        /// <summary>uuid（32 位 hex 字符串）→ 16 字节；非法长度/非 hex 抛 SLBFormatException。</summary>
        private static byte[] ParseUuid(string uuid)
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromHexString(uuid ?? string.Empty);
            }
            catch (FormatException ex)
            {
                throw new SLBFormatException("SLB: module uuid is not valid hex: " + (uuid ?? string.Empty), ex);
            }
            if (bytes.Length != SLBFormat.UuidLength)
            {
                throw new SLBFormatException("SLB: module uuid must be 32 hex chars (16 bytes), got " + (bytes.Length * 2) + " hex chars");
            }
            return bytes;
        }
    }
}
