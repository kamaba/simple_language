using SimpleLanguage.IR;
using SimpleLanguage.Parse;
using SimpleLanguage.VM.InnerCLRRuntime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace SimpleLanguage.VM.Runtime
{
    public class InnerCLRRuntimeVM
    {
        public static bool isPrint { get; set; } = false;
        public static RuntimeMethod currentCLRRuntime = null;
        public static RuntimeMethod topCLRRuntime = null;

        public static Dictionary<int, ClassObject> staticClassObjectDict = new Dictionary<int, ClassObject>();
        public static Stack<RuntimeMethod> clrRuntimeStack => m_ClrRuntimeStack;

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
        public static RuntimeMethod CreateCLRRuntime( IRMetaClass irmc, IRMethod _irMethod )
        {
            var getrt = GetCLRRuntimeById(_irMethod.id);
            //if( getrt != null )
            //{
            //    return getrt;
            //}
            //else
            {
                RuntimeMethod clrRuntime = new RuntimeMethod(irmc, _irMethod);
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
        public static void GetStaticVariable(IRMetaClass irmc, int index, ref SValue val)
        {
            if(staticClassObjectDict.ContainsKey(irmc.id) == false )
            {
                Log.AddVM(EError.None, "GetStaticVariable 没有找到相当的静态类");
                return;
            }
            ClassObject sobj = staticClassObjectDict[irmc.id];

            sobj.GetMemberVariableSValue(index, ref val); 
        }
        public static void SetStaticVariable( IRMetaClass irmc, int index, ref SValue svalue)
        {
            if (staticClassObjectDict.ContainsKey(irmc.id) == false)
            {
                Log.AddVM(EError.None, "SetStaticVariable 没有找到相当的静态类");
                return;
            }
            ClassObject sobj = staticClassObjectDict[irmc.id];

            sobj.SetMemberVariableSValue(index, svalue );
        }
        public static void Init()
        {
            //var staticArray = IRManager.instance.staticVariableList;
            //m_StaticVariableValueArray = new SValue[staticArray.Count];
            //for (int i = 0; i < staticArray.Count; i++)
            //{
            //    m_StaticVariableValueArray[i] = ObjectManager.CreateValueByDefineType( staticArray[i].irMetaClass );
            //}
            //InnverCLRRuntimeVM.RootInnerCLRRuntime 

            for( int i = 0; i < IRManager.instance.irMetaClassList.Count; i++ )
            {
                var irmc = IRManager.instance.irMetaClassList[i];
                ClassObject co = new ClassObject(irmc, true);
                staticClassObjectDict.Add(irmc.id, co);
            }
            foreach( var v in staticClassObjectDict )
            {
                v.Value.Create();
            }

            RuntimeMethod clrRuntime = new RuntimeMethod(IRManager.instance.irDataList);
            clrRuntime.isPersistent = true;
            clrRuntime.id = "InnverCLRRuntimeVM.CLRRuntime.EntryMethod()";
            InnerCLRRuntimeVM.PushCLRRuntime(clrRuntime);
            clrRuntime.Run();
        }
        public static void RunIRMethod( IRMetaClass irmc, IRMethod _irMethod )
        {
            topCLRRuntime = m_ClrRuntimeStack.Peek();
            RuntimeMethod clrRuntime = InnerCLRRuntimeVM.CreateCLRRuntime(irmc, _irMethod);
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
