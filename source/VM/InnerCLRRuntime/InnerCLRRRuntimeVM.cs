using SimpleLanguage.Core;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.VM.Runtime
{
    public class InnerCLRRuntimeVM
    {
        public static bool isPrint { get; set; } = false;
        public static RuntimeMethod currentCLRRuntime = null;
        public static RuntimeMethod topCLRRuntime = null;
        public static Stack<RuntimeMethod> clrRuntimeStack => m_ClrRuntimeStack;

        private static SValue[] m_StaticVariableValueArray = null;
        private static Stack<RuntimeMethod> m_ClrRuntimeStack = new Stack<RuntimeMethod>();
        public InnerCLRRuntimeVM()
        {

        }
        public static RuntimeMethod GetCLRRuntimeById( string id )
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
        public static RuntimeMethod CreateCLRRuntime( IRMethod _irMethod )
        {
            var getrt = GetCLRRuntimeById(_irMethod.id);
            if( getrt != null )
            {
                return getrt;
            }
            else
            {
                RuntimeMethod clrRuntime = new RuntimeMethod(_irMethod);
                clrRuntime.id = _irMethod.id;
                m_ClrRuntimeStack.Push(clrRuntime);
                return clrRuntime;
            }
        }
        public static void PushCLRRuntime(RuntimeMethod clrRuntime )
        {
            m_ClrRuntimeStack.Push(clrRuntime);
        }
        public static RuntimeMethod PopCLRRuntime()
        {
            return m_ClrRuntimeStack.Pop();
        }
        public static void GetStaticVariable(int index, ref SValue val)
        {
            if (index > m_StaticVariableValueArray.Length)
            {
                Console.WriteLine("执行的参数超出范围!!");
                return;
            }
            ObjectManager.SetValueByValue(ref val, ref m_StaticVariableValueArray[index] );
        }
        public static void SetStaticVariable(int index, SValue svalue)
        {
            if (index > m_StaticVariableValueArray.Length)
            {
                Console.WriteLine("执行的栈超出范围!!");
                return;
            }
            ObjectManager.SetValueByValue( ref m_StaticVariableValueArray[index], ref svalue );
        }
        public static void Init()
        {            
            var staticArray = IRManager.instance.staticVariableList;
            m_StaticVariableValueArray = new SValue[staticArray.Count];
            for (int i = 0; i < staticArray.Count; i++)
            {
                m_StaticVariableValueArray[i] = ObjectManager.CreateValueByDefineType(staticArray[i].metaVariable.metaDefineType);
            }
            //InnverCLRRuntimeVM.RootInnerCLRRuntime 
            RuntimeMethod clrRuntime = new RuntimeMethod(IRManager.instance.irDataList);
            clrRuntime.isPersistent = true;
            clrRuntime.id = "InnverCLRRuntimeVM.CLRRuntime.EntryMethod()";
            InnerCLRRuntimeVM.PushCLRRuntime(clrRuntime);
            clrRuntime.Run();
        }
        public static void RunIRMethod( IRMethod _irMethod )
        {
            topCLRRuntime = m_ClrRuntimeStack.Peek();
            RuntimeMethod clrRuntime = InnerCLRRuntimeVM.CreateCLRRuntime(_irMethod);
            clrRuntime.Run();
            topCLRRuntime.AddReturnObjectArray(clrRuntime.returnObjectArray);
            if (!clrRuntime.isPersistent)
            {
                currentCLRRuntime = InnerCLRRuntimeVM.PopCLRRuntime();
            }
        }
    }
}
