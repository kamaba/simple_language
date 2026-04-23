
using SimpleLanguage.Logging;

namespace SimpleLanguage.VM.Runtime
{
    public class CLRVM
    {
        public static bool isPrint { get; set; } = false;
        public static RuntimeVM currentCLRRuntime = null;
        public static RuntimeVM topCLRRuntime = null;

        // Fast lookup list for global ids. It only stores slot mapping metadata,
        // actual values are always stored on RuntimeType static fields.
        private static List<Instruction> m_GlobalInitInstructionList = new List<Instruction>();
        private static Dictionary<int, RuntimeVariable> m_GlobalVariableDict = new Dictionary<int, RuntimeVariable>();
        private static bool m_IsGlobalInitApplied = false;
        private static bool m_IsGlobalInitApplying = false;
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
        //        Log.AddVM(LID.Unknown, "GetStaticVariable 娌℃湁鎵惧埌鐩稿綋鐨勯潤鎬佺被");
        //        return;
        //    }
        //    ClassObject sobj = staticClassObjectDict[irmc.id];

        //    sobj.GetMemberVariableSValue(index, ref val); 
        //}
        //public static void SetStaticVariable( IRMetaClass irmc, int index, ref SValue svalue)
        //{
        //    if (staticClassObjectDict.ContainsKey(irmc.id) == false)
        //    {
        //        Log.AddVM(LID.Unknown, "SetStaticVariable 娌℃湁鎵惧埌鐩稿綋鐨勯潤鎬佺被");
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
            if (m_IsGlobalInitApplied || m_IsGlobalInitApplying)
            {
                return;
            }
            if (m_GlobalInitInstructionList == null || m_GlobalInitInstructionList.Count == 0)
            {
                m_IsGlobalInitApplied = true;
                return;
            }

            bool pushedRoot = false;
            m_IsGlobalInitApplying = true;
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
                m_IsGlobalInitApplied = true;
            }
            finally
            {
                m_IsGlobalInitApplying = false;
                if (pushedRoot && m_ClrRuntimeStack.Count > 0)
                {
                    PopCLRRuntime();
                }
            }
        }
        public static void EnsureGlobalVariableMappingInitialized()
        {
            LoadGlobalVariableMapping();
        }

        public static void ResetGlobalVariableMapping()
        {
            m_GlobalVariableDict.Clear();
            m_GlobalInitInstructionList.Clear();
            m_IsGlobalInitApplied = false;
            m_IsGlobalInitApplying = false;
        }
        public static void RegisterGlobalVariable(int id, RuntimeVariable rv )
        {
            if (m_GlobalVariableDict.ContainsKey(id))
            {
                return;
            }
            m_GlobalVariableDict[id] = rv;  
        }

        public static void SetGlobalInitInstructions(List<Instruction> instructionList)
        {
            m_GlobalInitInstructionList = instructionList ?? new List<Instruction>();
            m_IsGlobalInitApplied = false;
        }
        public static void StoreGlobalVariable( int id, ref SValue savl )
        {
            if (m_GlobalVariableDict.ContainsKey(id))
            {
                var slot = m_GlobalVariableDict[id];
                var rt = RuntimeTypeManager.GetRuntimeTypeByDefTypeAndAdd(slot.runtimeDefType);
                if (rt != null)
                {
                    rt.SetStaticMemberVariableSValue(id, savl);
                }
                else
                {
                    Log.AddProjectLog(LID.AutoCLRRRuntimeVML171, $"娌℃湁鎵惧埌鍏ㄥ眬鍙橀噺鎵€灞炵殑RuntimeType鏄犲皠! globalId={id}");
                }
            }
            else
            {
                Log.AddProjectLog(LID.AutoCLRRRuntimeVML176, "娌℃湁鎵惧埌鍏ㄥ眬鍙橀噺鐨勬槧灏勫叧绯?");
            }

        }
        public static void SetValue(ref SValue sValue, ref SValue sStore )
        {
            switch (sStore.eType)
            {
                case EVMType.Boolean:
                case EVMType.UInt8: sStore.SetUInt8Value(sValue.int8Value); break;
                case EVMType.Int8: sStore.SetInt8Value(sValue.sint8Value); break;
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
                        Log.AddProjectLog(LID.AutoCLRRRuntimeVML208, "Error StoreNotStaticField Path:" );
                    }
                    break;
            }
        }
        public static void LoadGlobalVariable( int id, ref SValue sval )
        {
            if (m_GlobalVariableDict.ContainsKey(id))
            {
                var slot = m_GlobalVariableDict[id];
                var rt = RuntimeTypeManager.GetRuntimeTypeByDefTypeAndAdd(slot.runtimeDefType);
                if (rt != null)
                {
                    rt.GetStaticMemberVariableSValue( id, ref sval);
                }
                else
                {
                    sval.SetNull();
                    Log.AddProjectLog(LID.AutoCLRRRuntimeVML226, $"娌℃湁鎵惧埌鍏ㄥ眬鍙橀噺鎵€灞炵殑RuntimeType鏄犲皠! globalId={id} ");
                }
            }
            else
            {
                Log.AddProjectLog(LID.AutoCLRRRuntimeVML231, "娌℃湁鎵惧埌鍏ㄥ眬鍙橀噺鐨勬槧灏勫叧绯?");
            }
        }
        public static void RunIRMethod( List<RuntimeType> irmtList, RuntimeMethod _irMethod, bool isDisCountStackCount = true )
        {
            topCLRRuntime = m_ClrRuntimeStack.Peek();
            RuntimeVM clrRuntime = CreateCLRRuntime( irmtList, _irMethod );
            clrRuntime.Run(isDisCountStackCount);
            PopCLRRuntime();
            var topt2 = m_ClrRuntimeStack.Peek();
            topt2.AddReturnObjectArray(clrRuntime.returnRuntimeObjectArray);
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
    }
}
