using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using SimpleLanguage.IR;
using SimpleLanguage.Core;

namespace SimpleLanguage.Export.SLVM
{
    public static class SLVMSerializer
    {
        public static void WriteModule(SLVMModule module, string path, SimpleLanguage.Project.ProjectConfig.ExportSection cfg = null)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs, Encoding.UTF8))
            {
                // header
                bw.Write((int)0x534C564D); // 'SLVM' magic
                bw.Write((int)1); // version
                bw.Write(module.name ?? "");
                // write string pool
                bw.Write(module.stringPool.Count);
                if (module.stringPool.Count > 0)
                {
                    bool packAsBlob = cfg == null || cfg.StringPoolAsBlob;
                    if (!packAsBlob)
                    {
                        // simple write strings
                        foreach (var s in module.stringPool) bw.Write(s ?? "");
                    }
                    else
                    {
                        // pack strings with offsets (blob)
                        long basePos = fs.Position;
                        for (int i = 0; i < module.stringPool.Count; i++) bw.Write((int)0);
                        var offsets = new List<int>();
                        for (int i = 0; i < module.stringPool.Count; i++)
                        {
                            offsets.Add((int)fs.Position);
                            bw.Write(module.stringPool[i] ?? "");
                        }
                        long cur = fs.Position;
                        fs.Position = basePos;
                        foreach (var off in offsets) bw.Write(off);
                        fs.Position = cur;
                    }
                }

                bw.Write(module.methods.Count);
                // write globals
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
                // write types
                bw.Write(module.types.Count);
                foreach (var t in module.types)
                {
                    bw.Write(t.name ?? "");
                    bw.Write(t.fields.Count);
                    foreach (var f in t.fields) { bw.Write(f.fieldName ?? ""); bw.Write(f.fieldType ?? ""); }
                }
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
            }
        }

        public static SLVMModule ReadModule(string path, SimpleLanguage.Project.ProjectConfig.ExportSection cfg = null)
        {
            var module = new SLVMModule();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs, Encoding.UTF8))
            {
                module.name = br.ReadString();
                int spcount = br.ReadInt32();
                module.stringPool = new System.Collections.Generic.List<string>();
                if (spcount > 0)
                {
                    bool packAsBlob = cfg == null || cfg.StringPoolAsBlob;
                    if (!packAsBlob)
                    {
                        for (int i = 0; i < spcount; i++) module.stringPool.Add(br.ReadString());
                    }
                    else
                    {
                        var offsets = new int[spcount];
                        for (int oi = 0; oi < spcount; oi++) offsets[oi] = br.ReadInt32();
                        long curpos = fs.Position;
                        for (int oi = 0; oi < spcount; oi++)
                        {
                            fs.Position = offsets[oi];
                            module.stringPool.Add(br.ReadString());
                        }
                        fs.Position = curpos;
                    }
                }

                int mcount = br.ReadInt32();
                // read globals
                int gcount = br.ReadInt32();
                for (int gi = 0; gi < gcount; gi++)
                {
                    var g = new SLVMGlobal();
                    g.name = br.ReadString();
                    try { g.metaId = br.ReadInt32(); } catch { g.metaId = -1; }
                    g.isStatic = br.ReadBoolean();
                    g.isConst = br.ReadBoolean();
                    g.initValueIndex = br.ReadInt32();
                    g.initValue = br.ReadString();
                    module.globals.Add(g);
                }
                // read types
                int tcount = br.ReadInt32();
                for (int ti = 0; ti < tcount; ti++)
                {
                    var t = new SLVMType();
                    t.name = br.ReadString();
                    int fcount = br.ReadInt32();
                    for (int fi = 0; fi < fcount; fi++)
                    {
                        var fname = br.ReadString(); var ftype = br.ReadString();
                        t.fields.Add((fname, ftype));
                    }
                    module.types.Add(t);
                }
                for (int i = 0; i < mcount; i++)
                {
                    var m = new SLVMMethod();
                    m.id = br.ReadString();
                    m.onlyFunctionName = br.ReadString();
                    m.argumentCount = br.ReadInt32();
                    m.localCount = br.ReadInt32();
                    m.isPublic = br.ReadBoolean();
                    m.isStatic = br.ReadBoolean();
                    int icount = br.ReadInt32();
                    for (int j = 0; j < icount; j++)
                    {
                        var ins = new SLVMInstruction();
                        ins.opcode = br.ReadString();
                        ins.index = br.ReadInt32();
                        ins.opValueIndex = br.ReadInt32();
                        ins.opValue = br.ReadString();
                        int payloadLen = br.ReadInt32();
                        if (payloadLen > 0)
                        {
                            ins.payload = br.ReadBytes(payloadLen);
                            try { ins.payloadType = (SLVMPayloadType)br.ReadByte(); } catch { ins.payloadType = SLVMPayloadType.None; }
                        }
                        m.instructions.Add(ins);
                    }
                    module.methods.Add(m);
                }
            }
            return module;
        }

        public static SLVMModule FromIRMethods(IRMethod[] methods, string moduleName)
        {
            var mod = new SLVMModule();
            mod.name = moduleName;
            // prefill common strings
            mod.AddString(moduleName);
            // export IR string table
            foreach (var kv in IRManager.instance.IRStringDict)
            {
                mod.AddString(kv.Value);
            }
            // export global/static variables - collect from ClassManager runtime classes and MetaData
            var clsList = ClassManager.instance.runtimeClassList;
            for (int ci = 0; ci < clsList.Count; ci++)
            {
                var mc = clsList[ci];
                var gvars = mc.GetMetaMemberVariableListByFlag(true);
                for (int gi = 0; gi < gvars.Count; gi++)
                {
                    var mv = gvars[gi];
                    var sg = new SLVMGlobal();
                    sg.name = mv.name;
                    sg.metaId = mv.GetHashCode();
                    sg.isStatic = true;
                    sg.isConst = mv.isConst;
                    if (mv.express is MetaConstExpressNode)
                    {
                        try { var mcen = (MetaConstExpressNode)mv.express; var sval = mcen.value?.ToString(); sg.initValue = sval; if (!string.IsNullOrEmpty(sval)) sg.initValueIndex = mod.AddString(sval); } catch { }
                    }
                    mod.globals.Add(sg);
                }
            }
            // export types (structures) from ClassManager runtime classes
            foreach (var rc in ClassManager.instance.runtimeClassList)
            {
                var st = new SLVMType();
                st.name = rc.allClassName;
                foreach (var mv in rc.GetMetaMemberVariableListByFlag(false))
                {
                    st.fields.Add((mv.name, mv.realMetaType?.ToString() ?? "object"));
                }
                mod.types.Add(st);
            }
            foreach (var m in methods)
            {
                var sm = new SLVMMethod();
                sm.id = m.id;
                sm.onlyFunctionName = m.onlyFunctionName;
                sm.argumentCount = m.methodArgumentList?.Count ?? 0;
                sm.localCount = m.methodLocalVariableList?.Count ?? 0;
                // visibility heuristics: public if not a private member function
                sm.isPublic = true;
                sm.isStatic = (m.irOwnerMetaClass == null);
                foreach (var d in m.IRDataList)
                {
                    var ins = new SLVMInstruction();
                    ins.opcode = d.opCode.ToString();
                    ins.index = d.index;
                    // use IRData.Payload if present for typed constant
                    if (d.Payload != null && d.Payload.Length > 0)
                    {
                        ins.payload = d.Payload;
                        // infer payload type from opcode
                        switch (d.opCode)
                        {
                            case EIROpCode.LoadConstInt32: ins.payloadType = SLVMPayloadType.Int32; break;
                            case EIROpCode.LoadConstInt64: ins.payloadType = SLVMPayloadType.Int64; break;
                            case EIROpCode.LoadConstUInt32: ins.payloadType = SLVMPayloadType.UInt32; break;
                            case EIROpCode.LoadConstUInt64: ins.payloadType = SLVMPayloadType.UInt64; break;
                            case EIROpCode.LoadConstDouble: ins.payloadType = SLVMPayloadType.Float64; break;
                            case EIROpCode.LoadConstFloat: ins.payloadType = SLVMPayloadType.Float32; break;
                            case EIROpCode.LoadConstString: ins.payloadType = SLVMPayloadType.String; break;
                            case EIROpCode.LoadConstBoolean: ins.payloadType = SLVMPayloadType.Boolean; break;
                            case EIROpCode.LoadConstByte: ins.payloadType = SLVMPayloadType.Byte; break;
                            case EIROpCode.LoadConstSByte: ins.payloadType = SLVMPayloadType.SByte; break;
                            case EIROpCode.LoadConstInt16: ins.payloadType = SLVMPayloadType.Int16; break;
                            case EIROpCode.LoadConstUInt16: ins.payloadType = SLVMPayloadType.UInt16; break;
                            default: ins.payloadType = SLVMPayloadType.None; break;
                        }
                        // for string constant, also add to pool
                        if (d.opCode == EIROpCode.LoadConstString)
                        {
                            string sval = d.opValue != null ? d.opValue.ToString() : null;
                            if (!string.IsNullOrEmpty(sval)) ins.opValueIndex = mod.AddString(sval);
                            ins.opValue = sval;
                        }
                    }
                    else
                    {
                        ins.opValue = d.opValue != null ? d.opValue.ToString() : "";
                        if (!string.IsNullOrEmpty(ins.opValue)) ins.opValueIndex = mod.AddString(ins.opValue);
                    }
                    sm.instructions.Add(ins);
                }
                mod.methods.Add(sm);
            }
            return mod;
        }
    }
}
