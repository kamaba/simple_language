
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
        private static Dictionary<uint, RuntimeVariable> m_GlobalVariableDict = new Dictionary<uint, RuntimeVariable>();
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
        public static RuntimeVM CreateCLRRuntime(RuntimeClass rc, List<RuntimeType> irmtList, RuntimeMethod method)
        {
            var getrt = GetCLRRuntimeById(method.id);
            //if( getrt != null )
            //{
            //    return getrt;
            //}
            //else
            {
                RuntimeVM clrRuntime = new RuntimeVM(rc, irmtList, method);
                m_ClrRuntimeStack.Push(clrRuntime);
                return clrRuntime;
            }
        }
        public static RuntimeVM CreateCLRRuntime(RuntimeType rt, List<RuntimeType> irmtList, RuntimeMethod method)
        {
            var getrt = GetCLRRuntimeById(method.id);
            //if( getrt != null )
            //{
            //    return getrt;
            //}
            //else
            {
                RuntimeVM clrRuntime = new RuntimeVM(rt, irmtList, method);
                m_ClrRuntimeStack.Push(clrRuntime);
                return clrRuntime;
            }
        }
        public static RuntimeVM CreateExeSplite( string id, List<RuntimeType> irmtList, List<Instruction> irlist )
        {
            RuntimeVM clrRuntime = new RuntimeVM( id, irmtList, irlist );
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
        //public static void GetStaticVariable( RuntimeType rt, int index, ref RuntimeValue val)
        //{
        //    if(staticClassObjectDict.ContainsKey(irmc.id) == false )
        //    {
        //        Log.AddVM(LID.Unknown, "GetStaticVariable 娌℃湁鎵惧埌鐩稿綋鐨勯潤鎬佺�?);
        //        return;
        //    }
        //    ClassObject sobj = staticClassObjectDict[irmc.id];

        //    sobj.GetMemberVariableSValue(index, ref val); 
        //}
        //public static void SetStaticVariable( IRMetaClass irmc, int index, ref RuntimeValue RuntimeValue)
        //{
        //    if (staticClassObjectDict.ContainsKey(irmc.id) == false)
        //    {
        //        Log.AddVM(LID.Unknown, "SetStaticVariable 娌℃湁鎵惧埌鐩稿綋鐨勯潤鎬佺�?);
        //        return;
        //    }
        //    ClassObject sobj = staticClassObjectDict[irmc.id];

        //    sobj.SetMemberVariableSValue(index, RuntimeValue );
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
                var root = new RuntimeVM("__global_init_root__",new List<Instruction>());
                PushCLRRuntime(root);
                pushedRoot = true;
            }

            try
            {
                var clrRuntime = CreateExeSplite("__global_init__", new List<RuntimeType>(), new List<Instruction>(m_GlobalInitInstructionList));               
                //clrRuntime.isPersistent = true;
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
        public static void RegisterGlobalVariable(uint id, RuntimeVariable rv )
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
        public static void StoreGlobalVariable( uint id, ref RuntimeValue savl )
        {
            if (m_GlobalVariableDict.ContainsKey(id))
            {
                var slot = m_GlobalVariableDict[id];
                var rt = RuntimeTypeManager.GetRuntimeTypeByDefTypeAndAdd(slot.runtimeDefType);
                if (rt != null)
                {
                    rt.SetStaticMemberVariableSValue((int)id, ref savl);
                }
                else
                {
                    Log.AddRuntimeLog(LID.ShowMessageAssert, $"global is null globalId={id}");
                }
            }
            else
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"global is nullglobalId={id}");
            }

        }
        public static void LoadGlobalVariable( uint id, ref RuntimeValue sval )
        {
            if (m_GlobalVariableDict.ContainsKey(id))
            {
                var slot = m_GlobalVariableDict[id];
                var rt = RuntimeTypeManager.GetRuntimeTypeByDefTypeAndAdd(slot.runtimeDefType);
                if (rt != null)
                {
                    rt.GetStaticMemberVariableSValue((int)id, ref sval);
                }
                else
                {
                    sval.SetNull();
                    Log.AddRuntimeLog(LID.ShowMessageAssert, $"global is null globalId={id} ");
                }
            }
            else
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"global is nullglobalId={id} ");
            }
        }
        public static void RunIRMethod( RuntimeClass rc, List<RuntimeType> irmtList, RuntimeMethod _irMethod, bool isDisCountStackCount = true )
        {
            topCLRRuntime = m_ClrRuntimeStack.Peek();
            RuntimeVM clrRuntime = CreateCLRRuntime( rc, irmtList, _irMethod );
            clrRuntime.Run(isDisCountStackCount);
            PopCLRRuntime();
            var topt2 = m_ClrRuntimeStack.Peek();
            topt2.AddReturnObjectArray(clrRuntime.returnRuntimeObjectArray);
            //if (!clrRuntime.isPersistent)
            {
            }
        }
        public static void RunIRMethodByRuntimeType( RuntimeType rt, List<RuntimeType> rtList, RuntimeMethod rm )
        {
            topCLRRuntime = m_ClrRuntimeStack.Peek();
            RuntimeVM clrRuntime = CreateCLRRuntime(rt, rtList, rm );
            clrRuntime.Run(true);
            PopCLRRuntime();
            var topt2 = m_ClrRuntimeStack.Peek();
            topt2.AddReturnObjectArray(clrRuntime.returnRuntimeObjectArray);
            //if (!clrRuntime.isPersistent)
            {
            }
        }
        public static void RunIRNewMethod( string id, List<RuntimeType> irmtList, List<Instruction> irlist )
        {
            topCLRRuntime = m_ClrRuntimeStack.Peek();
            RuntimeVM clrRuntime = CreateExeSplite(id, irmtList, irlist );
            clrRuntime.SetNewObject();
            clrRuntime.Run(true);
            clrRuntime.ClearNewObject();
            PopCLRRuntime();
            
        }
    }
}
