
using SimpleLanguage.IR;
using SimpleLanguage.Parse;
using System.Collections.Generic;

namespace SimpleLanguage.VM.Runtime
{
    public class InnerCLRRuntimeVM
    {
        public static bool isPrint { get; set; } = false;
        public static RuntimeVM currentCLRRuntime = null;
        public static RuntimeVM topCLRRuntime = null;
        public static Stack<RuntimeVM> clrRuntimeStack => m_ClrRuntimeStack;

        private static Stack<RuntimeVM> m_ClrRuntimeStack = new Stack<RuntimeVM>();
        public InnerCLRRuntimeVM()
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
        public static RuntimeVM CreateCLRRuntime( List<RuntimeType> irmtList, IRMethod _irMethod )
        {
            var getrt = GetCLRRuntimeById(_irMethod.id);
            //if( getrt != null )
            //{
            //    return getrt;
            //}
            //else
            {
                RuntimeVM clrRuntime = new RuntimeVM( irmtList, _irMethod);
                clrRuntime.id = _irMethod.id;
                m_ClrRuntimeStack.Push(clrRuntime);
                return clrRuntime;
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
            //var staticArray = IRManager.instance.staticVariableList;
            //m_StaticVariableValueArray = new SValue[staticArray.Count];
            //for (int i = 0; i < staticArray.Count; i++)
            //{
            //    m_StaticVariableValueArray[i] = ObjectManager.CreateValueByDefineType( staticArray[i].irMetaClass );
            //}
            //InnverCLRRuntimeVM.RootInnerCLRRuntime 
            RuntimeVM clrRuntime = new RuntimeVM(IRManager.instance.irDataList);
            clrRuntime.isPersistent = true;
            clrRuntime.id = "InnverCLRRuntimeVM.CLRRuntime.EntryMethod()";
            InnerCLRRuntimeVM.PushCLRRuntime(clrRuntime);
            clrRuntime.Run();
        }
        public static void RunIRMethod( List<RuntimeType> irmtList, IRMethod _irMethod )
        {
            topCLRRuntime = m_ClrRuntimeStack.Peek();
            RuntimeVM clrRuntime = InnerCLRRuntimeVM.CreateCLRRuntime( irmtList, _irMethod );
            clrRuntime.Run();
            InnerCLRRuntimeVM.PopCLRRuntime();
            var topt2 = m_ClrRuntimeStack.Peek();
            topt2.AddReturnObjectArray(clrRuntime.returnObjectArray);
            //if (!clrRuntime.isPersistent)
            {
            }
        }
    }
}
