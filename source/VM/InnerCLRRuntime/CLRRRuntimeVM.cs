
using SimpleLanguage.Logging;
using System;
using System.Runtime.InteropServices;

namespace SimpleLanguage.VM.Runtime
{
    public class CLRVM
    {
        private sealed class GlobalVariableMeta
        {
            public string typeName = string.Empty;
            public int ownerClassId;
            public int index;
        }

        public static bool isPrint { get; set; } = false;
        public static RuntimeVM currentCLRRuntime = null;
        public static RuntimeVM topCLRRuntime = null;

        private static List<SValue> m_GlobalVariableValueList = new List<SValue>();
        private static Dictionary<int, int> m_GlobalVariableId2IndexDict = new Dictionary<int, int>();
        private static Dictionary<int, GlobalVariableMeta> m_GlobalVariableMetaDict = new Dictionary<int, GlobalVariableMeta>();
        private static List<Instruction> m_GlobalInitInstructionList = new List<Instruction>();
        public static Stack<RuntimeVM> clrRuntimeStack => m_ClrRuntimeStack;

        private static Stack<RuntimeVM> m_ClrRuntimeStack = new Stack<RuntimeVM>();
        public CLRVM()
        {

        }
        public static RuntimeVM GetCLRRuntimeById( string id )
        {
            foreach( var v in m_ClrRuntimeStack )
            {
                if( v.id == id )
                {
                    return v;
                }
            }
            return null;
        }
        public static RuntimeVM CreateCLRRuntime( List<RuntimeType> irmtList, RuntimeMethod method )
        {
            var getrt = GetCLRRuntimeById(method.id);
            //if( getrt != null )
            //{
            //    return getrt;
            //}
            //else
            {
                RuntimeVM clrRuntime = new RuntimeVM( irmtList, method);
                clrRuntime.id = method.id;
                m_ClrRuntimeStack.Push(clrRuntime);
                return clrRuntime;
            }
        }
        public static RuntimeVM CreateExeSplite(List<RuntimeType> irmtList, List<Instruction> irlist )
        {
            RuntimeVM clrRuntime = new RuntimeVM( irmtList, irlist );
            m_ClrRuntimeStack.Push(clrRuntime);
            return clrRuntime;
        }

        public static void PushCLRRuntime(RuntimeVM clrRuntime )
        {
            m_ClrRuntimeStack.Push(clrRuntime);
        }
        public static RuntimeVM PopCLRRuntime()
        {
            return m_ClrRuntimeStack.Pop();
        }
        //public static void GetStaticVariable( RuntimeType rt, int index, ref SValue val)
        //{
        //    if(staticClassObjectDict.ContainsKey(irmc.id) == false )
        //    {
        //        Log.AddVM(EError.None, "GetStaticVariable 没有找到相当的静态类");
        //        return;
        //    }
        //    ClassObject sobj = staticClassObjectDict[irmc.id];

        //    sobj.GetMemberVariableSValue(index, ref val); 
        //}
        //public static void SetStaticVariable( IRMetaClass irmc, int index, ref SValue svalue)
        //{
        //    if (staticClassObjectDict.ContainsKey(irmc.id) == false)
        //    {
        //        Log.AddVM(EError.None, "SetStaticVariable 没有找到相当的静态类");
        //        return;
        //    }
        //    ClassObject sobj = staticClassObjectDict[irmc.id];

        //    sobj.SetMemberVariableSValue(index, svalue );
        //}
        public static void Init()
        {
            // VM init only; global mapping/execution is triggered explicitly
            // after IR class/type load and global registration.
        }
        public static void LoadGlobalVariableMapping()
        {
            for (int i = 0; i < m_GlobalVariableValueList.Count; i++)
            {
                int globalId = -1;
                foreach (var kv in m_GlobalVariableId2IndexDict)
                {
                    if (kv.Value == i)
                    {
                        globalId = kv.Key;
                        break;
                    }
                }

                var v = default(SValue);
                if (!TryCreateGlobalDefaultValue(globalId, ref v))
                {
                    v.SetNull();
                }
                m_GlobalVariableValueList[i] = v;
            }

            if (m_GlobalInitInstructionList == null || m_GlobalInitInstructionList.Count == 0)
            {
                return;
            }

            bool pushedRoot = false;
            if (m_ClrRuntimeStack.Count == 0)
            {
                var root = new RuntimeVM(new List<Instruction>());
                root.id = "__global_init_root__";
                PushCLRRuntime(root);
                pushedRoot = true;
            }

            try
            {
                var clrRuntime = CreateExeSplite(new List<RuntimeType>(), new List<Instruction>(m_GlobalInitInstructionList));
                clrRuntime.id = "__global_init__";
                clrRuntime.isPersistent = true;
                clrRuntime.Run(true);
                PopCLRRuntime();
            }
            finally
            {
                if (pushedRoot && m_ClrRuntimeStack.Count > 0)
                {
                    PopCLRRuntime();
                }
            }
        }

        public static void ResetGlobalVariableMapping()
        {
            m_GlobalVariableId2IndexDict.Clear();
            m_GlobalVariableValueList.Clear();
            m_GlobalVariableMetaDict.Clear();
            m_GlobalInitInstructionList.Clear();
        }

        public static void RegisterGlobalVariable(int id)
        {
            RegisterGlobalVariable(id, string.Empty, 0, -1);
        }

        public static void RegisterGlobalVariable(int id, string typeName, int ownerClassId, int index)
        {
            if (m_GlobalVariableId2IndexDict.ContainsKey(id))
            {
                m_GlobalVariableMetaDict[id] = new GlobalVariableMeta
                {
                    typeName = typeName ?? string.Empty,
                    ownerClassId = ownerClassId,
                    index = index,
                };
                return;
            }

            int mapIndex = m_GlobalVariableValueList.Count;
            m_GlobalVariableId2IndexDict[id] = mapIndex;
            var v = default(SValue);
            v.SetNull();
            m_GlobalVariableValueList.Add(v);

            m_GlobalVariableMetaDict[id] = new GlobalVariableMeta
            {
                typeName = typeName ?? string.Empty,
                ownerClassId = ownerClassId,
                index = index,
            };
        }

        public static void SetGlobalInitInstructions(List<Instruction> instructionList)
        {
            m_GlobalInitInstructionList = instructionList ?? new List<Instruction>();
        }

        private static bool TryCreateGlobalDefaultValue(int globalId, ref SValue value)
        {
            if (globalId == -1) return false;

            if (m_GlobalVariableMetaDict.TryGetValue(globalId, out var meta))
            {
                var typeName = meta.typeName ?? string.Empty;

                if (typeName.EndsWith("Boolean", StringComparison.OrdinalIgnoreCase) || typeName.EndsWith("Bool", StringComparison.OrdinalIgnoreCase))
                {
                    value.SetBoolValue(false);
                    return true;
                }
                if (typeName.EndsWith("String", StringComparison.OrdinalIgnoreCase))
                {
                    value.SetStringValue(string.Empty);
                    return true;
                }
                if (typeName.EndsWith("Float32", StringComparison.OrdinalIgnoreCase))
                {
                    value.SetFloatValue(0f);
                    return true;
                }
                if (typeName.EndsWith("Float64", StringComparison.OrdinalIgnoreCase))
                {
                    value.SetDoubleValue(0d);
                    return true;
                }
                if (typeName.EndsWith("UInt64", StringComparison.OrdinalIgnoreCase))
                {
                    value.SetUInt64Value(0UL);
                    return true;
                }
                if (typeName.EndsWith("Int64", StringComparison.OrdinalIgnoreCase))
                {
                    value.SetInt64Value(0L);
                    return true;
                }
                if (typeName.EndsWith("UInt32", StringComparison.OrdinalIgnoreCase))
                {
                    value.SetUInt32Value(0U);
                    return true;
                }
                if (typeName.EndsWith("Int32", StringComparison.OrdinalIgnoreCase))
                {
                    value.SetInt32Value(0);
                    return true;
                }
                if (typeName.EndsWith("UInt16", StringComparison.OrdinalIgnoreCase))
                {
                    value.SetUInt16Value(0);
                    return true;
                }
                if (typeName.EndsWith("Int16", StringComparison.OrdinalIgnoreCase))
                {
                    value.SetInt16Value(0);
                    return true;
                }
                if (typeName.EndsWith("Byte", StringComparison.OrdinalIgnoreCase))
                {
                    value.SetInt8Value(0);
                    return true;
                }

                RuntimeClass rc = null;
                if (meta.ownerClassId != 0)
                {
                    rc = RuntimeClassManager.instance.GetRuntimeClassById(meta.ownerClassId);
                }
                if (rc == null && !string.IsNullOrEmpty(typeName))
                {
                    rc = RuntimeClassManager.instance.GetRuntimeClassByName(typeName);
                }

                if (rc != null)
                {
                    var rt = RuntimeTypeManager.GetRuntimeTypeByClassId(rc.id) ?? RuntimeTypeManager.AddRuntimeTypeByClass(rc);
                    if (rt != null)
                    {
                        var sobj = ObjectManager.CreateObjectByRuntimeType(rt, true);
                        value.SetSObject(sobj);
                        return true;
                    }
                }
            }

            return false;
        }
        public static void StoreGlobalVariable( int id, ref SValue savl )
        {
            if(m_GlobalVariableId2IndexDict.ContainsKey( id ) )
            {
                m_GlobalVariableValueList[m_GlobalVariableId2IndexDict[id]] = savl;
            }
            else
            {
                Log.AddVM(EError.None, "没有找到全局变量的映射关系!");
            }

        }
        public static void SetValue(ref SValue sValue, ref SValue sStore )
        {
            switch (sStore.eType)
            {
                case EVMType.Boolean:
                case EVMType.Byte: sStore.SetInt8Value(sValue.int8Value); break;
                case EVMType.SByte: sStore.SetSInt8Value(sValue.sint8Value); break;
                case EVMType.Int16: sStore.SetInt16Value(sValue.int16Value); break;
                case EVMType.UInt16: sStore.SetUInt16Value(sValue.uint16Value); break;
                case EVMType.Int32: sStore.SetInt32Value(sValue.int32Value); break;
                case EVMType.UInt32: sStore.SetUInt32Value(sValue.uint32Value); break;
                case EVMType.Int64: sStore.SetInt64Value(sValue.int64Value); break;
                case EVMType.UInt64: sStore.SetUInt64Value(sValue.uint64Value); break;
                case EVMType.Float32: sStore.SetFloatValue(sValue.floatValue); break;
                case EVMType.Float64: sStore.SetDoubleValue(sValue.doubleValue); break;
                case EVMType.String: sStore.SetStringValue(sValue.stringValue); break;
                case EVMType.Null:
                    {
                        sStore.SetNull();
                    }
                    break;
                case EVMType.Class:
                    {
                        sStore.SetSObject(sValue.sobject);
                    }
                    break;
                default:
                    {
                        Log.AddVM(EError.None, "Error StoreNotStaticField Path:" );
                    }
                    break;
            }
        }
        public static void LoadGlobalVariable( int id, ref SValue sval )
        {
            if (m_GlobalVariableId2IndexDict.ContainsKey(id))
            {
                sval = m_GlobalVariableValueList[m_GlobalVariableId2IndexDict[id]];
            }
            else
            {
                Log.AddVM(EError.None, "没有找到全局变量的映射关系!");
            }
        }
        public static void RunIRMethod( List<RuntimeType> irmtList, RuntimeMethod _irMethod, bool isDisCountStackCount = true )
        {
            topCLRRuntime = m_ClrRuntimeStack.Peek();
            RuntimeVM clrRuntime = CreateCLRRuntime( irmtList, _irMethod );
            clrRuntime.Run(isDisCountStackCount);
            PopCLRRuntime();
            var topt2 = m_ClrRuntimeStack.Peek();
            topt2.AddReturnObjectArray(clrRuntime.returnObjectArray);
            //if (!clrRuntime.isPersistent)
            {
            }
        }
        public static void RunIRNewMethod( List<RuntimeType> irmtList, List<Instruction> irlist )
        {
            topCLRRuntime = m_ClrRuntimeStack.Peek();
            RuntimeVM clrRuntime = CreateExeSplite(irmtList, irlist );
            clrRuntime.SetNewObject();
            clrRuntime.Run(true);
            clrRuntime.ClearNewObject();
            PopCLRRuntime();
            
        }

        // Execute a single method from a .slvm file
        public static void RunSLVMMethodFile(string slvmPath, string methodId)
        {
            /*
            var irlist = SimpleLanguage.Export.PELoader.ConvertSLVMMethodToIRDataList(slvmPath, methodId);
            if (irlist == null) return;

            // ensure there is a root runtime on stack
            bool pushedRoot = false;
            if (m_ClrRuntimeStack.Count == 0)
            {
                var root = new RuntimeVM(new List<Instruction>());
                root.id = "__slvm_root__";
                m_ClrRuntimeStack.Push(root);
                pushedRoot = true;
            }

            topCLRRuntime = m_ClrRuntimeStack.Peek();
            var exe = CreateExeSplite(new List<RuntimeType>(), new List<Instruction>(irlist));
            exe.Run(true);
            PopCLRRuntime();

            if (pushedRoot)
            {
                // pop the temporary root
                m_ClrRuntimeStack.Pop();
            }
            */
        }

        // Load an entire .slvm module and register its string pool into IRManager
        public static void LoadSLVMModule(string slvmPath)
        {
            /*
            var module = SLVMSerializer.ReadModule(slvmPath);
            if (module == null) return;
            // register strings
            foreach (var s in module.stringPool)
            {
                IRManager.instance.AddStringIRStack(s);
            }
            // register globals
            foreach (var g in module.globals)
            {
                // create simple SValue from initValue or string pool
                SValue sval = default;
                if (!string.IsNullOrEmpty(g.initValue))
                {
                    // try parse numeric, otherwise treat as string
                    if (int.TryParse(g.initValue, out int vi)) sval.SetInt32Value(vi);
                    else if (long.TryParse(g.initValue, out long vl)) sval.SetInt64Value(vl);
                    else if (double.TryParse(g.initValue, out double vd)) sval.SetDoubleValue(vd);
                    else sval.SetStringValue(g.initValue);
                }
                else if (g.initValueIndex >= 0 && g.initValueIndex < module.stringPool.Count)
                {
                    sval.SetStringValue(module.stringPool[g.initValueIndex]);
                }
                else
                {
                    sval.SetNull();
                }
                // prefer exported meta id when available, fallback to name hash
                int id = g.metaId != -1 ? g.metaId : (g.name?.GetHashCode() ?? 0);
                // if we have a corresponding IR meta variable, try to initialize runtime static member
                if (g.metaId != -1)
                {
                    int gvIndex = IRManager.instance.GetGlobalStaticMetaVariableById(g.metaId);
                    if (gvIndex >= 0)
                    {
                        var irmv = IRManager.instance.globalStaticVariableList[gvIndex];
                        try
                        {
                            // find owner class runtime type and set its static member
                            var owner = irmv.irMetaType.irOwnerMetaClass;
                            var ownerRt = RuntimeTypeManager.GetRuntimeTypeByMTAndIRMetaClass(owner);
                            if (ownerRt == null)
                            {
                                ownerRt = RuntimeTypeManager.AddRuntimeTypeByClass(owner);
                            }
                            if (ownerRt != null)
                            {
                                ownerRt.SetMemberVariableSValue(irmv.index, sval);
                            }
                        }
                        catch { }
                    }
                }

                if (!m_GlobalVariableId2IndexDict.ContainsKey(id))
                {
                    int idx = m_GlobalVariableValueList.Count;
                    m_GlobalVariableId2IndexDict[id] = idx;
                    m_GlobalVariableValueList.Add(sval);
                }
            }

            // register types: create placeholders in RuntimeTypeManager if necessary
            foreach (var t in module.types)
            {
                // ensure IRMetaClass exists in IRManager
                var irmc = IRManager.instance.GetIRMetaClassByName(t.name);
                if (irmc == null)
                {
                    // skip unknown types for now
                    continue;
                }
                // create runtime type if not exists
                var rt = RuntimeTypeManager.GetRuntimeTypeByMTAndIRMetaClass(irmc);
                if (rt == null)
                {
                    RuntimeTypeManager.AddRuntimeTypeByClass(irmc);
                }
            }
            */
        }
    }
}
