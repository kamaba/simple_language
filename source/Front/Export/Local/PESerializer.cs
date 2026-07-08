
using System.IO;
using System.Text;
using SimpleLanguage.IR;
using SimpleLanguage.Wrapper;

namespace SimpleLanguage.Export
{
    public static class PESerializer
    {
        public static void WriteModule( WrapperModule module, string path, SimpleLanguage.Project.ProjectConfig.ExportSection cfg = null)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs, Encoding.UTF8))
            {
                // header
                WriteHeader(bw, module, cfg);
                // string pool
                WriteStringPool(bw, fs, module, cfg);
                WriteConstPool(bw,fs, module, cfg);
                // write CLR-like definition tables
                WriteTypeDefs(bw, module);
                WriteMethodDefs(bw, module);

                // globals
                WriteGlobals(bw, module);
                // types / irClass
                WriteTypes(bw, module);

                // write namespaces
                WriteNamespaces(bw, module);

                // write method implementations
                WriteMethods(bw, module);
            }
        }

        static void WriteTypeDefs(BinaryWriter bw, WrapperModule module)
        {
            /*
            bw.Write(module.typeDefs.Count);
            foreach (var td in module.typeDefs)
            {
                bw.Write(td.namespaceName ?? "");
                bw.Write(td.name ?? "");
                bw.Write(td.fieldList.Count);
                foreach (var fidx in td.fieldList)
                {
                    if (fidx >= 0 && fidx < module.fieldDefs.Count)
                    {
                        var fd = module.fieldDefs[fidx];
                        bw.Write(fd.name ?? "");
                        bw.Write(fd.signatureOffset);
                    }
                    else
                    {
                        bw.Write(""); bw.Write(-1);
                    }
                }
            }
            */
        }

        static void WriteMethodDefs(BinaryWriter bw, WrapperModule module)
        {
            /*
            bw.Write(module.methodTable.Count);
            foreach (var md in module.methodTable)
            {
                bw.Write(md.name ?? "");
                bw.Write(md.flags);
                bw.Write(md.implFlags);
                bw.Write(md.signatureOffset);
                bw.Write(md.paramListStart);
            }
            */
        }

        static void WriteHeader(BinaryWriter bw, WrapperModule module, SimpleLanguage.Project.ProjectConfig.ExportSection cfg)
        {
            bw.Write((int)0x534C564D); // 'SLVM' magic
            bw.Write((int)(cfg?.VersionMain ?? 0)); // version
            bw.Write((int)(cfg?.VersionSub ?? 0)); // version
            bw.Write((int)(cfg?.VersionPatch ?? 0)); // version
            bw.Write(module.name ?? "");
            bw.Write(module.guid ?? "");
        }

        static void WriteStringPool(BinaryWriter bw, FileStream fs, WrapperModule module, SimpleLanguage.Project.ProjectConfig.ExportSection cfg)
        {
            /*
            bw.Write(module.stringPool.Count);
            if (module.stringPool.Count == 0) return;

            bool packAsBlob = cfg == null || cfg.StringPoolAsBlob;
            if (!packAsBlob)
            {
                foreach (var s in module.stringPool) bw.Write(s ?? "");
            }
            else
            {
                for (int i = 0; i < module.stringPool.Count; i++)
                {
                    bw.Write(module.stringPool[i] ?? "");
                }
                int offsets = 0;
                for (int i = 0; i < module.stringPool.Count; i++)
                {
                    bw.Write(offsets);
                    bw.Write(module.stringPool[i].Length);
                    offsets += module.stringPool[i].Length; 
                }
            }
            */
        }

        static void WriteConstPool(BinaryWriter bw, FileStream fs, WrapperModule module, SimpleLanguage.Project.ProjectConfig.ExportSection cfg)
        {
            /*
            bw.Write(module.blobHeap.Count);
            if (module.blobHeap.Count == 0) return;
            bw.Write(module.blobHeap.ToArray());
            */
        }

        static void WriteGlobals(BinaryWriter bw, WrapperModule module)
        {
            /*
            bw.Write(module.globals.Count);
            foreach (var g in module.globals)
            {
                bw.Write(g.name ?? "");
                bw.Write(g.metaId);
                bw.Write(g.isStatic);
                bw.Write(g.isConst);
                bw.Write(g.initValueIndex);
                bw.Write(g.initValue ?? "");
            }
            */
        }

        static void WriteNamespaces(BinaryWriter bw, WrapperModule module)
        {
            bw.Write(module.nodeTree.Count);
            foreach (var ns in module.nodeTree)
            {
                bw.Write(ns.name ?? "");
                bw.Write(ns.children.Count);
                foreach (var c in ns.children) bw.Write(c.name ?? "");
            }
        }

        static void WriteTypes(BinaryWriter bw, WrapperModule module)
        {
            /*
            // write typeDefs
            bw.Write(module.typeDefs.Count);
            foreach (var td in module.typeDefs)
            {
                bw.Write(td.namespaceName ?? "");
                bw.Write(td.name ?? "");
                bw.Write(td.fieldList.Count);
                foreach (var fidx in td.fieldList)
                {
                    if (fidx >= 0 && fidx < module.fieldDefs.Count)
                    {
                        var fd = module.fieldDefs[fidx];
                        bw.Write(fd.name ?? "");
                        bw.Write(fd.signatureOffset);
                    }
                    else
                    {
                        bw.Write(""); bw.Write(-1);
                    }
                }
            }

            // write IR meta classes
            bw.Write(module.irMetaClasses.Count);
            foreach (var ic in module.irMetaClasses)
            {
                bw.Write(ic.id);
                bw.Write(ic.name ?? "");
                bw.Write(ic.byteCount);
                bw.Write(ic.templateCount);
                bw.Write(ic.needInitMemberVariable);
                bw.Write(ic.localVariables.Count);
                foreach (var lv in ic.localVariables)
                {
                    bw.Write(lv.id);
                    bw.Write(lv.name ?? "");
                    bw.Write(lv.index);
                    bw.Write(lv.from);
                    bw.Write(lv.irMetaType ?? "");
                }
                bw.Write(ic.staticVariables.Count);
                foreach (var sv in ic.staticVariables)
                {
                    bw.Write(sv.id);
                    bw.Write(sv.name ?? "");
                    bw.Write(sv.index);
                    bw.Write(sv.from);
                    bw.Write(sv.irMetaType ?? "");
                }
            }
            */
        }

        static void WriteMethods(BinaryWriter bw, WrapperModule module)
        {
            /*
            foreach (var m in module.methods)
            {
                bw.Write(m.id ?? "");
                bw.Write(m.onlyFunctionName ?? "");
                // write owner info if available 
                bw.Write(m.isPublic);
                bw.Write(m.isStatic);
                bw.Write(m.argumentCount);
                bw.Write(m.localCount);
                bw.Write(m.instructions.Count);
                foreach (var ins in m.instructions)
                {
                    bw.Write(ins.opcode ?? "");
                    bw.Write(ins.index);
                    bw.Write(ins.opValueIndex);
                    bw.Write(ins.opValue ?? "");
                    // write typed payload: length + bytes
                    if (ins.payload != null && ins.payload.Length > 0)
                    {
                        bw.Write((int)ins.payload.Length);
                        bw.Write(ins.payload);
                        bw.Write((byte)ins.payloadType);
                    }
                    else
                    {
                        bw.Write((int)0);
                    }
                }
            }
            */
        }
        public static WrapperModule ReadPEModule(string path, SimpleLanguage.Project.ProjectConfig.ExportSection cfg = null)
        {
            /*
            var module = new WrapperModule();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs, Encoding.UTF8))
            {
                module.name = br.ReadString();
                int spcount = br.ReadInt32();
                module.stringPool = new System.Collections.Generic.List<string>();
                for (int i = 0; i < spcount; i++) module.stringPool.Add(br.ReadString());

                // read typeDefs
                int tdcount = br.ReadInt32();
                for (int tdi = 0; tdi < tdcount; tdi++)
                {
                    var td = new PETypeDef();
                    td.namespaceName = br.ReadString();
                    td.name = br.ReadString();
                    int fcount = br.ReadInt32();
                    for (int fi = 0; fi < fcount; fi++)
                    {
                        var fname = br.ReadString(); var fsign = br.ReadInt32();
                        var fd = new PEFieldDef() { name = fname, flags = 0, signatureOffset = fsign };
                        int fidx = module.fieldDefs.Count; module.fieldDefs.Add(fd); td.fieldList.Add(fidx);
                    }
                    module.typeDefs.Add(td);
                }

                // read methodDefs
                int mdcount = br.ReadInt32();
                for (int mdi = 0; mdi < mdcount; mdi++)
                {
                    var md = new PEMethodDef();
                    md.name = br.ReadString();
                    md.flags = br.ReadInt32();
                    md.implFlags = br.ReadInt32();
                    md.signatureOffset = br.ReadInt32();
                    module.methodTable.Add(md);
                }

                // read globals
                int gcount = br.ReadInt32();
                for (int gi = 0; gi < gcount; gi++)
                {
                    var g = new PEGlobal();
                    g.name = br.ReadString();
                    try { g.metaId = br.ReadInt32(); } catch { g.metaId = -1; }
                    g.isStatic = br.ReadBoolean();
                    g.isConst = br.ReadBoolean();
                    g.initValueIndex = br.ReadInt32();
                    g.initValue = br.ReadString();
                    module.globals.Add(g);
                }

                // read IR meta classes
                int irCount = br.ReadInt32();
                for (int ii = 0; ii < irCount; ii++)
                {
                    var ic = new PEClass();
                    ic.id = br.ReadInt32();
                    ic.name = br.ReadString();
                    ic.byteCount = br.ReadInt32();
                    ic.templateCount = br.ReadInt32();
                    ic.needInitMemberVariable = br.ReadBoolean();
                    int lcount = br.ReadInt32();
                    for (int li = 0; li < lcount; li++)
                    {
                        var lv = new PEVariable();
                        lv.id = br.ReadInt32(); lv.name = br.ReadString(); lv.index = br.ReadInt32(); lv.from = br.ReadInt32(); lv.irMetaType = br.ReadString();
                        ic.localVariables.Add(lv);
                    }
                    int scount = br.ReadInt32();
                    for (int si = 0; si < scount; si++)
                    {
                        var sv = new PEVariable();
                        sv.id = br.ReadInt32(); sv.name = br.ReadString(); sv.index = br.ReadInt32(); sv.from = br.ReadInt32(); sv.irMetaType = br.ReadString();
                        ic.staticVariables.Add(sv);
                    }
                    module.irMetaClasses.Add(ic);
                }

                // read namespaces
                int nsCount = br.ReadInt32();
                for (int ni = 0; ni < nsCount; ni++)
                {
                    var ns = new PEPathNode();
                    ns.name = br.ReadString();
                    int ccount = br.ReadInt32();
                    for (int ci = 0; ci < ccount; ci++) ns.children.Add(new PEPathNode() { name = br.ReadString() });
                    module.nodeTree.Add(ns);
                }

                // read method implementations
                int mcount = br.ReadInt32();
                for (int i = 0; i < mcount; i++)
                {
                    var m = new PEMethod();
                    m.id = br.ReadString();
                    m.onlyFunctionName = br.ReadString();
                    m.argumentCount = br.ReadInt32();
                    m.localCount = br.ReadInt32();
                    m.isPublic = br.ReadBoolean();
                    m.isStatic = br.ReadBoolean();
                    int icount = br.ReadInt32();
                    for (int j = 0; j < icount; j++)
                    {
                        var ins = new PEInstruction();
                        ins.opcode = br.ReadString();
                        ins.index = br.ReadInt32();
                        ins.opValueIndex = br.ReadInt32();
                        ins.opValue = br.ReadString();
                        int payloadLen = br.ReadInt32();
                        if (payloadLen > 0)
                        {
                            ins.payload = br.ReadBytes(payloadLen);
                            try { ins.payloadType = (PEPayloadType)br.ReadByte(); } catch { ins.payloadType = PEPayloadType.None; }
                        }
                        m.instructions.Add(ins);
                    }
                    module.methods.Add(m);
                }
            }
            return module;
            */
            return null;
        }

        // New: export IR data into PEModule structures
        public static WrapperModule FromIRToPEModule(IRMethod[] methods, string moduleName)
        {
            /*
            var mod = new WrapperModule();
            mod.name = moduleName;

            // export IR string table
            foreach (var kv in IRManager.instance.IRStringDict)
            {
                mod.AddString(kv.Value);
            }

            // export globals (static meta member variables)
            var clsList = ClassManager.instance.runtimeClassList;
            for (int ci = 0; ci < clsList.Count; ci++)
            {
                var mc = clsList[ci];
                var gvars = mc.GetMetaMemberVariableListByFlag(true);
                for (int gi = 0; gi < gvars.Count; gi++)
                {
                    var mv = gvars[gi];
                    var pg = new PEGlobal();
                    pg.name = mv.name;
                    pg.metaId = mv.GetHashCode();
                    pg.isStatic = true;
                    pg.isConst = mv.isConst;
                    if (mv.express is MetaConstExpressNode)
                    {
                        try { var mcen = (MetaConstExpressNode)mv.express; var sval = mcen.value?.ToString(); pg.initValue = sval; if (!string.IsNullOrEmpty(sval)) pg.initValueIndex = mod.AddString(sval); } catch { }
                    }
                    mod.globals.Add(pg);
                }
            }

            // types -> typeDefs and fieldDefs
            foreach (var rc in ClassManager.instance.runtimeClassList)
            {
                var td = new PETypeDef();
                var fullname = rc.allClassName ?? "";
                int idx = fullname.LastIndexOf('.');
                if (idx > 0)
                {
                    td.namespaceName = fullname.Substring(0, idx);
                    td.name = fullname.Substring(idx + 1);
                }
                else
                {
                    td.namespaceName = "";
                    td.name = fullname;
                }
                foreach (var mv in rc.GetMetaMemberVariableListByFlag(false))
                {
                    var fd = new PEFieldDef();
                    fd.name = mv.name;
                    fd.flags = 0;
                    fd.signatureOffset = -1;
                    int fidx = mod.fieldDefs.Count;
                    mod.fieldDefs.Add(fd);
                    td.fieldList.Add(fidx);
                }
                mod.typeDefs.Add(td);
            }

            // IR meta classes -> PEClass
            foreach (var irmc in IRManager.instance.irMetaClassList)
            {
                var pc = new PEClass();
                pc.id = irmc.id;
                pc.name = irmc.irName;
                pc.byteCount = irmc.byteCount;
                pc.templateCount = irmc.templateCount;
                pc.needInitMemberVariable = irmc.needInitMemberVariable;
                foreach (var lv in irmc.localIRMetaVariableList)
                {
                    var pv = new PEVariable();
                    pv.id = lv.id;
                    pv.name = lv.name;
                    pv.index = lv.index;
                    pv.from = (int)lv.irMetaVariableFrom;
                    pv.irMetaType = lv.irMetaType?.ToString();
                    pc.localVariables.Add(pv);
                }
                foreach (var sv in irmc.staticIRMetaVariableList)
                {
                    var pv = new PEVariable();
                    pv.id = sv.id;
                    pv.name = sv.name;
                    pv.index = sv.index;
                    pv.from = (int)sv.irMetaVariableFrom;
                    pv.irMetaType = sv.irMetaType?.ToString();
                    pc.staticVariables.Add(pv);
                }
                mod.irMetaClasses.Add(pc);
            }

            // methods -> PEMethod + PEMethodDef
            foreach (var m in methods)
            {
                var pm = new PEMethod();
                pm.id = m.id;
                pm.onlyFunctionName = m.onlyFunctionName;
                pm.argumentCount = m.methodArgumentList?.Count ?? 0;
                pm.localCount = m.methodLocalVariableList?.Count ?? 0;
                pm.isPublic = true;
                pm.isStatic = (m.irOwnerMetaClass == null);

                foreach (var d in m.IRDataList)
                {
                    var ins = new PEInstruction();
                    ins.opcode = d.opCode.ToString();
                    ins.index = d.index;
                    if (d.Payload != null && d.Payload.Length > 0)
                    {
                        ins.payload = d.Payload;
                        // note: no precise payloadType mapping here, use blob
                    }
                    if (d.opValue != null)
                    {
                        var sval = d.opValue.ToString();
                        if (!string.IsNullOrEmpty(sval)) ins.opValueIndex = mod.AddString(sval);
                        ins.opValue = sval;
                    }
                    pm.instructions.Add(ins);
                }
                mod.methods.Add(pm);

                var md = new PEMethodDef();
                md.name = pm.id;
                md.flags = 0;
                md.implFlags = 0;
                md.signatureOffset = -1;
                mod.methodTable.Add(md);
            }

            return mod;
            */
            return null;
        }
    }
}
