
using SimpleLanguage.IR;
using SimpleLanguage.Parse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SimpleLanguage.VM.Runtime
{
    public class InnerCLRRuntimeVM
    {
        public static bool isPrint { get; set; } = false;
        public static RuntimeVM currentCLRRuntime = null;
        public static RuntimeVM topCLRRuntime = null;

        private static SValue[] m_GlobalVariableValueArray = null;
        private static Dictionary<int, int> m_GlobalVariableId2IndexDict = new Dictionary<int, int>();
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
        public static RuntimeVM CreateExeSplite(List<RuntimeType> irmtList, List<IRData> irlist )
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
            var staticArray = IRManager.instance.globalStaticVariableList;
            m_GlobalVariableValueArray = new SValue[staticArray.Count];

            List<IRData> execIRList = new List<IRData>();
            for (int i = 0; i < staticArray.Count; i++)
            {
                m_GlobalVariableId2IndexDict.Add(staticArray[i].id, i );

                var rt = RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(staticArray[i].irMetaType);
                IRMetaClass owirmc = IRManager.instance.GetIRMetaClassById(staticArray[i].irMetaType.irOwnerMetaClass.id);

                var obj = ObjectManager.CreateObjectByRuntimeType(rt, true);
                m_GlobalVariableValueArray[i].SetSObject(obj);

                IRExpress irexpress = new IRExpress(IRManager.instance, staticArray[i].express);

                IRStoreVariable irsv = new IRStoreVariable(staticArray[i].irMetaType, null, staticArray[i].id, IRMetaVariableFrom.Global);

                execIRList.AddRange(irexpress.IRDataList);
                execIRList.AddRange(irsv.IRDataList);

            }
            //InnverCLRRuntimeVM.RootInnerCLRRuntime 
            RuntimeVM clrRuntime = new RuntimeVM(execIRList);
            clrRuntime.isPersistent = true;
            clrRuntime.id = "InnverCLRRuntimeVM.CLRRuntime.EntryMethod()";
            InnerCLRRuntimeVM.PushCLRRuntime(clrRuntime);
            clrRuntime.Run();
        }
        public static void StoreGlobalVariable( int id, ref SValue savl )
        {
            if(m_GlobalVariableId2IndexDict.ContainsKey( id ) )
            {
                m_GlobalVariableValueArray[m_GlobalVariableId2IndexDict[id]] = savl;
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
                case EType.Boolean:
                case EType.Byte: sStore.SetInt8Value(sValue.int8Value); break;
                case EType.SByte: sStore.SetSInt8Value(sValue.sint8Value); break;
                case EType.Int16: sStore.SetInt16Value(sValue.int16Value); break;
                case EType.UInt16: sStore.SetUInt16Value(sValue.uint16Value); break;
                case EType.Int32: sStore.SetInt32Value(sValue.int32Value); break;
                case EType.UInt32: sStore.SetUInt32Value(sValue.uint32Value); break;
                case EType.Int64: sStore.SetInt64Value(sValue.int64Value); break;
                case EType.UInt64: sStore.SetUInt64Value(sValue.uint64Value); break;
                case EType.Float32: sStore.SetFloatValue(sValue.floatValue); break;
                case EType.Float64: sStore.SetDoubleValue(sValue.doubleValue); break;
                case EType.String: sStore.SetStringValue(sValue.stringValue); break;
                case EType.Null:
                    {
                        sStore.SetNull();
                    }
                    break;
                case EType.Class:
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
                sval = m_GlobalVariableValueArray[m_GlobalVariableId2IndexDict[id]];
            }
            else
            {
                Log.AddVM(EError.None, "没有找到全局变量的映射关系!");
            }
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
        public static void RunIRNewMethod( List<RuntimeType> irmtList, List<IRData> irlist )
        {
            topCLRRuntime = m_ClrRuntimeStack.Peek();
            RuntimeVM clrRuntime = InnerCLRRuntimeVM.CreateExeSplite(irmtList, irlist );
            clrRuntime.SetNewObject();
            clrRuntime.Run();
            clrRuntime.ClearNewObject();
            InnerCLRRuntimeVM.PopCLRRuntime();
            
        }
    }
}
