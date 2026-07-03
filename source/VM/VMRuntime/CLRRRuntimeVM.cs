
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
        public static void Init()
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
        public static void RunIRNewMethod(string id, RuntimeType rt, List<Instruction> irlist)
        {
            try
            {
                topCLRRuntime = m_ClrRuntimeStack.Count > 0  ? m_ClrRuntimeStack.Peek() : null;
                Log.AddVM(LID.ShowMessageInfo, $"RunIRNewMethod id={id} rt={rt} irlist.count={irlist?.Count}");
                RuntimeVM clrRuntime = new RuntimeVM(id, rt, rt?.runtimeTemplateList, irlist);
                m_ClrRuntimeStack.Push(clrRuntime);
                clrRuntime.SetNewObject();
                clrRuntime.Run(true);
                clrRuntime.ClearNewObject();
                PopCLRRuntime();
            }
            catch (Exception ex)
            {
                Log.AddVM(LID.ShowMessageError, $"RunIRNewMethod id={id} rt={rt} irlist.count={irlist?.Count} exception={ex}");
            }
        }
        public static void RunIRMethodByRuntimeType(RuntimeType rt, List<RuntimeType> rtList, RuntimeMethod method, bool isDisCountStackCount = true)
        {
            topCLRRuntime = m_ClrRuntimeStack.Peek();
            RuntimeVM clrRuntime = null;

            var getrt = GetCLRRuntimeById(method.id);
            //if( getrt != null )
            //{
            //    return getrt;
            //}
            //else
            {
                clrRuntime = new RuntimeVM(rt, rtList, method);
                m_ClrRuntimeStack.Push(clrRuntime);
            }
            clrRuntime.Run(isDisCountStackCount);
            PopCLRRuntime();
            var topt2 = m_ClrRuntimeStack.Peek();
            topt2.AddReturnObjectArray(clrRuntime.returnRuntimeObjectArray);
            //if (!clrRuntime.isPersistent)
            {
            }
        }
        public static void PushCLRRuntime(RuntimeVM clrRuntime )
        {
            m_ClrRuntimeStack.Push(clrRuntime);
        }
        public static RuntimeVM PopCLRRuntime()
        {
            return m_ClrRuntimeStack.Pop();
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
            m_IsGlobalInitApplying = true;
            RunIRNewMethod("__global_init__", null, new List<Instruction>(m_GlobalInitInstructionList));           
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
    }
}
