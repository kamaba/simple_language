//****************************************************************************
//  File:      RuntimeVM.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Logging;
using System.Runtime.CompilerServices;
using System.Reflection;
using SimpleLanuageVM.Load;
using SimpleLanguage.Parse;
using System.Globalization;
using SimpleLanguage.VM.MemoryManagement;

namespace SimpleLanguage.VM.Runtime
{
    public unsafe class RuntimeVM
    {
        public RuntimeObject[] returnRuntimeObjectArray { get => m_ReturnRuntimeObjectArray; }
        public ushort valueIndex => m_ValueIndex;
        public string id => m_Id;
        public int level => m_Level;
        public bool isPersistent => m_IsPersistent;



        private List<RuntimeType> m_InputTemplateRuntimeTypeList;
        private RuntimeObject[] m_LocalVariableRuntimeObjectArray;
        private RuntimeObject[] m_ArgumentRuntimeObjectArray;
        private RuntimeObject[] m_ReturnRuntimeObjectArray;

        private RuntimeMethod m_Method;
        private Instruction[] m_InstructionList;
        private ushort m_ExecuteIndex;
        private ushort m_ExecuteCount;
        private RuntimeClass m_CurrentRuntimeClass;
        private SValue[] m_ValueStack;
        private ushort m_ValueIndex;
        //public IntPtr m_RawBuffer;
        //public RawSValue* m_RawPtr;
        //public int m_RawCapacity;


        private string m_Id = "";
        private int m_Level = 0;
        private bool m_IsPersistent = false;
        public RuntimeVM( string id, List<Instruction> irlist)
        {
            m_Id = id;
            m_InstructionList = irlist?.ToArray();
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            //m_RawCapacity = 1024;

            Init();
        }
        public RuntimeVM( List<RuntimeType> rtList, RuntimeMethod rm )
        {
            m_InputTemplateRuntimeTypeList = rtList ?? new List<RuntimeType>();
            m_Method = rm;
            m_Id = rm.id;
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            //m_RawCapacity = 1024;
            m_InstructionList = rm.InstructionList.ToArray();
            Init();
        }
        public RuntimeVM( string id, List<RuntimeType> rtList, List<Instruction> irlist)
        {
            m_Id = id;
            m_InputTemplateRuntimeTypeList = rtList ?? new List<RuntimeType>();
            m_InstructionList = irlist?.ToArray();
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            //m_RawCapacity = 1024;

            Init();
        }

        public void Init()
        {
            //鍙傛暟鍒楄〃 argument variable table
            if (m_Method != null)
            {
                m_ReturnRuntimeObjectArray = new RuntimeObject[m_Method.methodReturnVariableList.Count];
                for (int i = 0; i < m_Method.methodReturnVariableList.Count; i++)
                {
                    RuntimeDefType imt = m_Method.methodReturnVariableList[i].runtimeDefType;
                    //SObject sobj = imt != null
                    //    ? CreateObjectByIRMetaType(imt, imt.ownerRuntimeClass, true)
                    //    : new SObject(EVMType.Object);
                    RuntimeType rt = ResolveRuntimeTypeForInit(imt);
                    m_ReturnRuntimeObjectArray[i] = new RuntimeObject(rt, m_Method.methodReturnVariableList[i], null);
                }

                m_ArgumentRuntimeObjectArray = new RuntimeObject[m_Method.methodArgumentList.Count];
                for (int i = 0; i < m_Method.methodArgumentList.Count; i++)
                {
                    RuntimeDefType imt = m_Method.methodArgumentList[i].runtimeDefType;
                    //// Enum-typed parameters use a generic any-object slot (not ClassObject for the enum type).
                    //SObject sobj = null;
                    //if (IsEnumDeclaredParameterType(imt))
                    //{
                    //    sobj = new SObject(EVMType.Object);
                    //}
                    //else
                    //{

                    //    sobj = imt != null
                    //        ? CreateObjectByIRMetaType(imt, imt.ownerRuntimeClass, true)
                    //        : new SObject(EVMType.Object);
                    //}
                    RuntimeType rt = ResolveRuntimeTypeForInit(imt);
                    m_ArgumentRuntimeObjectArray[i] = new RuntimeObject(rt, m_Method.methodArgumentList[i], null);
                }
                for (int i = 0; i < m_ArgumentRuntimeObjectArray.Length; i++)
                {
                    Log.AddRuntimeLog(LID.ShowMessageInfo, "Argu_" + i.ToString() + "_Value: [" + m_ArgumentRuntimeObjectArray[i]?.ToString() + "]");
                }

                //灞€閮ㄥ彉閲忓垪琛?local variable table
                m_LocalVariableRuntimeObjectArray = new RuntimeObject[m_Method.methodLocalVariableList.Count];
                for (int i = 0; i < m_Method.methodLocalVariableList.Count; i++)
                {
                    var mev = m_Method.methodLocalVariableList[i];
                    RuntimeDefType imt = mev.runtimeDefType;
                    //杩欏潡锛岄渶瑕侊紝濡傛灉鏄ā鏉跨被锛屽厛妫€鏌ユ槸鍚︽湁杈撳叆鐨勬ā鏉跨被鍨嬪垪琛紝濡傛灉鏈夛紝鐩存帴鐢ㄨ緭鍏ョ殑妯℃澘绫诲瀷鍒楄〃鍒涘缓瀵硅薄锛屽鏋滄病鏈夛紝鍐嶇敤imt鍒涘缓瀵硅薄
                    //SObject sobj = imt != null
                    //    ? CreateObjectByIRMetaType(imt, m_Method.ownerMetaClass, true)
                    //    : new SObject(EVMType.Object);
                    RuntimeType rt = ResolveRuntimeTypeForInit(imt);
                    m_LocalVariableRuntimeObjectArray[i] = new RuntimeObject(rt, m_Method.methodLocalVariableList[i], null); ;
                }
                for (int i = 0; i < m_LocalVariableRuntimeObjectArray.Length; i++)
                {
                    Log.AddRuntimeLog(LID.ShowMessageInfo, "Variable_" + i.ToString() + m_LocalVariableRuntimeObjectArray[i].ToString());
                }
            }

            else
            {
                m_ReturnRuntimeObjectArray = new RuntimeObject[0];
                m_ArgumentRuntimeObjectArray = new RuntimeObject[0];
                m_LocalVariableRuntimeObjectArray = new RuntimeObject[0];
            }
            var count = m_InstructionList.Length;
            if (count < 48)
            {
                m_ValueStack = new SValue[128];
            }
            else if (count >= 48 && count < 150)
            {
                m_ValueStack = new SValue[160];
            }
            else if (count >= 150 && count < 300)
            {
                m_ValueStack = new SValue[200];
            }
            else if (count >= 300 && count < 500)
            {
                m_ValueStack = new SValue[300];
            }
            else if (count >= 500 && count < 800)
            {
                m_ValueStack = new SValue[400];
            }
            else
            {
                m_ValueStack = new SValue[500];
            }

            SlMemoryManager.Instance.RegisterVmForRootCollection(this);
        }

        private RuntimeType ResolveRuntimeTypeForInit(RuntimeDefType? defType)
        {
            if (defType == null)
            {
                return RuntimeTypeManager.objectRuntimeType;
            }

            var ownerClass = m_Method?.ownerMetaClass ?? defType.ownerRuntimeClass;
            if (ownerClass != null && m_InputTemplateRuntimeTypeList != null && m_InputTemplateRuntimeTypeList.Count > 0)
            {
                var templateRt = GetRuntimeTypeByDefType(defType, ownerClass, m_InputTemplateRuntimeTypeList, true);
                if (templateRt != null)
                {
                    return templateRt;
                }
            }

            return RuntimeTypeManager.GetRuntimeTypeByDefTypeAndAdd(defType);
        }

        public void SetValueIndex( int vindex ) => m_ValueIndex = (ushort)vindex;
        /// <summary>GC roots: value stack and argument/local/return runtime object slots.</summary>
        internal void AppendSlMemoryRoots(HashSet<SObject> roots)
        {
            if (roots == null) return;
            if (m_ValueStack != null)
            {
                int n = m_ValueIndex;
                for (int i = 0; i < n; i++)
                {
                    var v = m_ValueStack[i];
                    if (!v.isNull && v.sobject != null)
                        roots.Add(v.sobject);
                }
            }
            AppendRuntimeObjectRoots(roots, m_ArgumentRuntimeObjectArray);
            AppendRuntimeObjectRoots(roots, m_LocalVariableRuntimeObjectArray);
            AppendRuntimeObjectRoots(roots, m_ReturnRuntimeObjectArray);
        }

        private static void AppendRuntimeObjectRoots(HashSet<SObject> roots, RuntimeObject[]? arr)
        {
            if (arr == null) return;
            foreach (var ro in arr)
            {
                if (ro?.sobject != null)
                    roots.Add(ro.sobject);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushSValueSynced(in SValue v)
        {
            if (m_ValueStack == null) m_ValueStack = new SValue[1024];
            if (m_ValueIndex >= m_ValueStack.Length) return;
            m_ValueStack[m_ValueIndex++] = v;
#if DEBUG
            Log.AddVM(LID.ShowMessageInfo, "push svalue " + v.ToString() );
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryPushStackSlot(out int slotIndex)
        {
            if (m_ValueStack == null) m_ValueStack = new SValue[1024];
            if (m_ValueIndex >= m_ValueStack.Length)
            {
                slotIndex = -1;
                //this block can auto 
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"SVM Error: Value stack overflow, current index={m_ValueIndex}, stack length={m_ValueStack.Length}");
                return false;
            }
            slotIndex = m_ValueIndex++;
            return true;
        }
        public static RuntimeType GetRuntimeTypeByDefType(RuntimeDefType irmt, RuntimeClass curIRMc, List<RuntimeType> __rtList, bool isAdd = false)
        {
            if (irmt.templateIndex != -1)
            {
                if (irmt.ownerRuntimeClass == curIRMc || curIRMc.name == "Core.Object")
                {
                    return __rtList[irmt.templateIndex];
                }
                else
                {
                    var mt = curIRMc.GetRuntimeDefTypeByTemplateAndClassRelation(irmt.ownerRuntimeClass, irmt.templateIndex);
                    if (mt == null) return null;

                    return GetRuntimeTypeByDefType(mt, curIRMc, __rtList, isAdd);
                }
            }
            else
            {
                List<RuntimeType> rtList = new List<RuntimeType>();
                if (irmt.runtimeDefTypeList.Count > 0)
                {
                    for (int i = 0; i < irmt.runtimeDefTypeList.Count; i++)
                    {
                        var crt = GetRuntimeTypeByDefType(irmt.runtimeDefTypeList[i], curIRMc, __rtList, isAdd);
                        rtList.Add(crt);
                    }
                }
                RuntimeType rt = RuntimeTypeManager.GetRuntimeTypeByRuntimeClassAndRuntimeTypeList(irmt.runtimeClass, rtList);
                if (rt == null && isAdd)
                {
                    rt = RuntimeTypeManager.AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(irmt.runtimeClass, rtList);
                }
                return rt;
            }
        }
        public SObject CreateObjectByIRMetaType(RuntimeDefType irmt, RuntimeClass curIrMc, bool isAdd = false)
        {
            if (irmt == null) return new SObject(EVMType.Object);
            var rtbd = GetRuntimeTypeByDefType( irmt, curIrMc, m_InputTemplateRuntimeTypeList, isAdd );
            return ObjectManager.CreateObjectByRuntimeType(rtbd, false);
        }
        public void AddReturnObjectArray(RuntimeObject[] sobjs)
        {
            //m_ReturnObjectArray = sobjs;

            for (int i = 0; i < sobjs.Length; i++)
            {
                if (sobjs[i].runtimeType != RuntimeTypeManager.voidRuntimeType)
                {
                    //GetObjectByValue(4, i, sobjs, ref m_ValueStack[m_ValueIndex++] );
                    var obj = sobjs[i];
                    if( obj == null )
                    {
                        Log.AddRuntimeLog(LID.ShowMessageAssert, "object is null");
                        return;
                    }
                    if (obj.eType == EVMType.Null )
                    {
                        m_ValueStack[m_ValueIndex++].SetNull();
                        return;
                    }
                    obj.SetSValueByRuntimeObjct(ref m_ValueStack[m_ValueIndex]);
                    m_ValueIndex++;
                }
            }
        }
        public SValue GetCurrentIndexValue(int index)
        {
            return m_ValueStack[index];
        }
        public void SetNewObject()
        {
            SValue sval = CLRVM.topCLRRuntime.GetCurrentIndexValue(CLRVM.topCLRRuntime.m_ValueIndex - 1);
            m_ValueStack[m_ValueIndex++] = sval;
            m_CurrentRuntimeClass = sval.sobject?.runtimeClass;
        }
        public void ClearNewObject()
        {
            m_CurrentRuntimeClass = null;
        }
        public void Run(bool disStackCount)
        {
            if (m_InstructionList == null || m_InstructionList.Length == 0) return;

            string funName = id;

            string pushChar = "";
            for (int i = 0; i < level; i++)
            {
                pushChar = '\t' + pushChar;
            }
            Log.AddProjectLog(LID.ShowMessageInfo, pushChar + "[VMRuntime] [Push] Method: [" + funName + "]");
            m_Level++;

            var topClrRuntime = CLRVM.topCLRRuntime;
            for (int i = 0; i < m_ArgumentRuntimeObjectArray.Length; i++)
            {
                SValue sval;
                if (disStackCount)
                {
                    topClrRuntime.m_ValueIndex--;
                    sval = topClrRuntime.GetCurrentIndexValue(topClrRuntime.m_ValueIndex);
                }
                else
                {
                    sval = topClrRuntime.GetCurrentIndexValue(topClrRuntime.m_ValueIndex - 1 - i);
                }
                uint index = (uint)(m_ArgumentRuntimeObjectArray.Length - i - 1);
                m_ArgumentRuntimeObjectArray[index].SetSObjectBySValue(ref sval);
            }

            m_ExecuteIndex = 0;
            m_ExecuteCount = (ushort)m_InstructionList.Length;
            while (m_ExecuteIndex < m_ExecuteCount)
            {
                var iri = m_InstructionList[m_ExecuteIndex];
                try
                {
                    RunInstruction(iri);
                }
                catch (Exception ex)
                {
                    // SvmNullNumericArithmeticException：LID.VMOperatorNotShouldHaveNull 已在 SValue（比较/算术）中输出，这里不再打日志
                    if (ex is SvmNullNumericArithmeticException) throw;
                    // CompilationAbortException：由 Log 系统统一决定“阻断/取消执行”，此处不重复包装日志。
                    if (ex is CompilationAbortException) throw;
                    var loc2 = iri?.debugInfo?.FormatDiagnosticLine();
                    var detail = string.IsNullOrEmpty(loc2)
                        ? $"VM instruction fault: op={iri?.opCode} ip={m_ExecuteIndex} id={iri?.id} index={iri?.index}"
                        : $"VM instruction fault: op={iri?.opCode} ip={m_ExecuteIndex} id={iri?.id} index={iri?.index} at {loc2}";
                    if (iri?.debugInfo != null)
                        Log.AddRuntimeLog(LID.ShowMessageError, iri.debugInfo, detail + " — " + ex.Message);
                    else
                        Log.AddRuntimeLog(LID.ShowMessageError, detail + " — " + ex);
                    throw;
                }
                m_ExecuteIndex++;
            }


            m_Level--;
            pushChar = "";
            for (int i = 0; i < m_Level; i++)
            {
                pushChar = '\t' + pushChar;
            }
            Log.AddRuntimeLog(LID.ShowMessageInfo, pushChar + "[VMRuntime] [Pop] Method: [" + funName + "]");
        }
        private static object? ConvertInvokeArg(object? source, Type targetType)
        {
            if (source == null) return null;

            // BridgeObject 鍙傛暟钀藉湴锛坙egacy bridge 璺緞涔熼渶瑕侊級
            if (source is ClassObject co && IsBridgeObjectRuntime(co.runtimeClass))
            {
                if (TryExtractBridgeObjectPayload(co, out var payloadObj))
                {
                    if (payloadObj == null) return null;
                    if (targetType == typeof(object)) return payloadObj;
                    if (targetType == typeof(string))
                        return payloadObj is string s ? s : payloadObj.ToString();
                    if (targetType.IsInstanceOfType(payloadObj)) return payloadObj;

                    if (payloadObj is string payloadStr)
                    {
                        if (targetType == typeof(int) && int.TryParse(payloadStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                            return i;
                        if (targetType == typeof(long) && long.TryParse(payloadStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                            return l;
                        if (targetType == typeof(float) && float.TryParse(payloadStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var f))
                            return f;
                        if (targetType == typeof(double) && double.TryParse(payloadStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d))
                            return d;
                        if (targetType == typeof(bool))
                        {
                            if (bool.TryParse(payloadStr, out var b)) return b;
                            if (int.TryParse(payloadStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bi)) return bi != 0;
                        }
                    }

                    if (targetType.IsEnum)
                    {
                        try { return Enum.ToObject(targetType, payloadObj); } catch { /* ignore */ }
                    }
                    try { return Convert.ChangeType(payloadObj, targetType); } catch { /* ignore */ }
                }
            }

            if (targetType == typeof(object) || targetType.IsInstanceOfType(source)) return source;
            if (targetType.IsEnum) return Enum.ToObject(targetType, source);
            return Convert.ChangeType(source, targetType);
        }

        private static bool IsBridgeObjectRuntime(RuntimeClass? runtimeClass)
        {
            if (runtimeClass == null) return false;
            var n = runtimeClass.name ?? string.Empty;
            return n.EndsWith("BridgeObject", StringComparison.Ordinal) || n.Contains(".BridgeObject", StringComparison.Ordinal);
        }

        private static bool TryExtractBridgeObjectPayload(ClassObject co, out object? payloadObj)
        {
            payloadObj = null;
            var rc = co.runtimeClass;
            if (rc == null) return false;

            var vars = rc.nonStaticIRMetaVariableList;
            if (vars == null || vars.Count == 0) return false;

            int index = -1;
            for (int i = 0; i < vars.Count; i++)
            {
                var vn = vars[i]?.name ?? string.Empty;
                if (string.Equals(vn, "valuetype", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(vn, "_valuetype", StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
            if (index < 0) index = 0;

            var sv = default(SValue);
            co.GetMemberVariableSValue(index, ref sv);
            if(sv.int32Value >= 0 && sv.int32Value <= 5 )
            {
                var realval = default(SValue);
                co.GetMemberVariableSValue(sv.int32Value, ref realval);
                payloadObj = realval.GetValueObject();
                return true;
            }
            else
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "array is index < 5");
                return false;
            }
        }

        private static object? NormalizeLegacyBridgeArg(ref SValue sv)
        {
            var raw = sv.GetValueObject();
            // Use object-target conversion so BridgeObject payload is extracted to CLR-friendly value.
            return ConvertInvokeArg(raw, typeof(object));
        }

        private static int FindBridgeMemberIndexByName(RuntimeClass? rc, string memberName)
        {
            var list = rc?.nonStaticIRMetaVariableList;
            if (list == null) return -1;
            for (int i = 0; i < list.Count; i++)
            {
                var n = list[i]?.name ?? string.Empty;
                if (string.Equals(n, memberName, StringComparison.OrdinalIgnoreCase)
                    || n.EndsWith("." + memberName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool TryStoreLegacyReturnToBridgeObject(SValue[] values, object? retObjValue)
        {
            if (values == null || values.Length < 4 || retObjValue == null) return false;

            var retObjSv = values[3];
            if (retObjSv.eType != EVMType.Class || retObjSv.sobject is not ClassObject retBridge) return false;
            if (!IsBridgeObjectRuntime(retBridge.runtimeClass)) return false;

            int idxBool = FindBridgeMemberIndexByName(retBridge.runtimeClass, "boolvalue");
            int idxI32 = FindBridgeMemberIndexByName(retBridge.runtimeClass, "int32value");
            int idxI64 = FindBridgeMemberIndexByName(retBridge.runtimeClass, "int64value");
            int idxF64 = FindBridgeMemberIndexByName(retBridge.runtimeClass, "float64value");
            int idxStr = FindBridgeMemberIndexByName(retBridge.runtimeClass, "stringvalue");
            int idxType = FindBridgeMemberIndexByName(retBridge.runtimeClass, "valuetype");

            var outSv = default(SValue);
            int outTypeCode = -1;
            switch (retObjValue)
            {
                case bool b:
                    outSv.SetBoolValue(b);
                    outTypeCode = 0;
                    if (idxBool >= 0) retBridge.SetMemberVariableSValue(idxBool, outSv);
                    break;
                case byte v:
                    outSv.SetInt32Value(v);
                    outTypeCode = 1;
                    if (idxI32 >= 0) retBridge.SetMemberVariableSValue(idxI32, outSv);
                    break;
                case sbyte v:
                    outSv.SetInt32Value(v);
                    outTypeCode = 1;
                    if (idxI32 >= 0) retBridge.SetMemberVariableSValue(idxI32, outSv);
                    break;
                case short v:
                    outSv.SetInt32Value(v);
                    outTypeCode = 1;
                    if (idxI32 >= 0) retBridge.SetMemberVariableSValue(idxI32, outSv);
                    break;
                case ushort v:
                    outSv.SetInt32Value(v);
                    outTypeCode = 1;
                    if (idxI32 >= 0) retBridge.SetMemberVariableSValue(idxI32, outSv);
                    break;
                case int v:
                    outSv.SetInt32Value(v);
                    outTypeCode = 1;
                    if (idxI32 >= 0) retBridge.SetMemberVariableSValue(idxI32, outSv);
                    break;
                case uint v:
                    outSv.SetInt64Value(v);
                    outTypeCode = 1;
                    if (idxI64 >= 0) retBridge.SetMemberVariableSValue(idxI64, outSv);
                    break;
                case long v:
                    outSv.SetInt64Value(v);
                    outTypeCode = 1;
                    if (idxI64 >= 0) retBridge.SetMemberVariableSValue(idxI64, outSv);
                    break;
                case ulong v:
                    outSv.SetInt64Value(unchecked((long)v));
                    outTypeCode = 1;
                    if (idxI64 >= 0) retBridge.SetMemberVariableSValue(idxI64, outSv);
                    break;
                case float v:
                    outSv.SetDoubleValue(v);
                    outTypeCode = 2;
                    if (idxF64 >= 0) retBridge.SetMemberVariableSValue(idxF64, outSv);
                    break;
                case double v:
                    outSv.SetDoubleValue(v);
                    outTypeCode = 2;
                    if (idxF64 >= 0) retBridge.SetMemberVariableSValue(idxF64, outSv);
                    break;
                case string s:
                    outSv.SetStringValue(s);
                    outTypeCode = 3;
                    if (idxStr >= 0) retBridge.SetMemberVariableSValue(idxStr, outSv);
                    break;
                default:
                    outSv.SetStringValue(retObjValue.ToString() ?? string.Empty);
                    outTypeCode = 3;
                    if (idxStr >= 0) retBridge.SetMemberVariableSValue(idxStr, outSv);
                    break;
            }

            if (idxType >= 0 && outTypeCode >= 0)
            {
                var typeSv = default(SValue);
                typeSv.SetInt32Value(outTypeCode);
                retBridge.SetMemberVariableSValue(idxType, typeSv);
            }

            return true;
        }

        private static bool TryBuildInvokeArgsForMethod(MethodInfo mi, object[] argsClr, out object?[] invokeArgs, out int score)
        {
            score = 0;
            var pars = mi.GetParameters();
            invokeArgs = new object?[pars.Length];
            if (pars.Length != argsClr.Length) return false;

            for (int i = 0; i < pars.Length; i++)
            {
                var pType = pars[i].ParameterType;
                var raw = argsClr[i];

                if (raw == null)
                {
                    bool canNull = !pType.IsValueType || Nullable.GetUnderlyingType(pType) != null;
                    if (!canNull) return false;
                    invokeArgs[i] = null;
                    score += 1;
                    continue;
                }

                var rawType = raw.GetType();
                if (rawType == pType)
                {
                    invokeArgs[i] = raw;
                    score += 4;
                    continue;
                }
                if (pType.IsAssignableFrom(rawType))
                {
                    invokeArgs[i] = raw;
                    score += 3;
                    continue;
                }

                try
                {
                    var converted = ConvertInvokeArg(raw, pType);
                    if (converted == null)
                    {
                        bool canNull = !pType.IsValueType || Nullable.GetUnderlyingType(pType) != null;
                        if (!canNull) return false;
                        invokeArgs[i] = null;
                        score += 1;
                        continue;
                    }

                    if (pType.IsInstanceOfType(converted) || converted.GetType() == pType)
                    {
                        invokeArgs[i] = converted;
                        score += 2;
                        continue;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        private static RuntimeDefType? TryGetInstructionRuntimeDefType(Instruction iri)
        {
            if (iri == null) return null;
            //if (iri.opValue is RuntimeDefType direct) return direct;

            var resolved = SLRuntimeModuleRegistry.TryResolveRuntimeDefTypeFromInstruction(iri.opValue, iri.Payload);
            if (resolved != null)
            {
                iri.opValue = resolved;
            }
            return resolved;
        }

        internal bool TrySystemCallPopArgs(int paramCount, out SValue[] args)
        {
            args = null!;
            if (paramCount < 0) return false;
            if (m_ValueIndex < paramCount) return false;
            args = new SValue[paramCount];
            for (int pi = paramCount - 1; pi >= 0; pi--)
                args[pi] = m_ValueStack[--m_ValueIndex];
            return true;
        }

        internal bool TrySystemCallPopDiscard(int discardCount)
        {
            if (discardCount < 0) return false;
            if (m_ValueIndex < discardCount) return false;
            for (int pi = discardCount - 1; pi >= 0; pi--)
                _ = m_ValueStack[--m_ValueIndex];
            return true;
        }


        internal bool TryInvokeRegisteredBridgeByIndex(Instruction iri)
        {
            int bridgeIndex;
            if (iri.TryGetInt32(out var payloadIndex)) bridgeIndex = payloadIndex;
            else bridgeIndex = iri.index;

            if (!CSharpBridgeRegistry.TryResolve(bridgeIndex, out var model)) return false;
            if (!CSharpBridgeRegistry.TryBindMethod(model, out var methodInfo))
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"Bridge method bind failed, index={bridgeIndex}");
                return true;
            }

            var pars = methodInfo.GetParameters();
            if (m_ValueIndex < pars.Length)
            {
                Log.AddRuntimeLog( LID.ShowMessageAssert, $"Bridge stack underflow, need={pars.Length}, has={m_ValueIndex}");
                return true;
            }

            var invokeArgs = new object?[pars.Length];
            for (int i = pars.Length - 1; i >= 0; i--)
            {
                var sv = m_ValueStack[--m_ValueIndex];
                var raw = sv.ToClrObject(pars[i].ParameterType);
                invokeArgs[i] = ConvertInvokeArg(raw, pars[i].ParameterType);
            }

            if (!methodInfo.IsStatic)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "Bridge instance methods are not supported");
                return true;
            }

            var ret = methodInfo.Invoke(null, invokeArgs);
            if (methodInfo.ReturnType != typeof(void))
            {
                var sv = SValue.FromClrObject(ret);
                PushSValueSynced(sv);
            }

            return true;
        }

        internal bool TryInvokeLegacyBridgeSignature(Instruction iri, string callName)
        {
            int paramCountLocal = iri.index;
            if (paramCountLocal <= 0) return false;

            var values = new SValue[paramCountLocal];
            for (int i = paramCountLocal - 1; i >= 0; i--)
            {
                if (m_ValueIndex == 0)
                {
                    Log.AddRuntimeLog(LID.ShowMessageAssert, $"{callName} stack underflow");
                    return true;
                }
                values[i] = m_ValueStack[--m_ValueIndex];
            }

            string namespaceName = values.Length > 1 ? values[0].GetValueObject()?.ToString() ?? string.Empty : string.Empty;
            string className = values.Length > 2 ? values[1].GetValueObject()?.ToString() ?? string.Empty : string.Empty;
            string methodName = values.Length > 3 ? values[2].GetValueObject()?.ToString() ?? string.Empty : string.Empty;

            object[] argsClr = Array.Empty<object>();
            SValue paramArr ;
            if (values.Length >= 5)
            {
                var arr = values[4];
                if (arr.eType == EVMType.Array && arr.sobject is ArrayObject aobj)
                {
                    int len = aobj.length;
                    argsClr = new object[len];
                    for (int j = 0; j < len; j++)
                    {
                        var temp = default(SValue);
                        aobj.LoadValue(j, ref temp);
                        argsClr[j] = NormalizeLegacyBridgeArg(ref temp);
                    }
                }
                else
                {
                    var single = values[4];
                    argsClr = new object[] { NormalizeLegacyBridgeArg(ref single) };
                }
            }

            MethodInfo? miFound = null;
            var methodId = CSharpBridgeRegistry.BuildMethodId(namespaceName, className, methodName);
            if (CSharpBridgeRegistry.TryResolve(methodId, out var model)
                && CSharpBridgeRegistry.TryBindMethod(model, out var regMi))
            {
                miFound = regMi;
            }

            if (miFound == null)
            {
                string typeFull = string.IsNullOrEmpty(namespaceName) ? className : (namespaceName + "." + className);
                Type t = Type.GetType(typeFull, throwOnError: false);
                if (t == null)
                {
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        t = a.GetType(typeFull, throwOnError: false);
                        if (t != null) break;
                    }
                }
                if (t == null)
                {
                    Log.AddRuntimeLog(LID.ShowMessageAssert, $"{callName}: type not found " + typeFull);
                    return true;
                }

                int bestScore = int.MinValue;
                object?[]? bestInvokeArgs = null;
                foreach (var mi2 in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    if (mi2.Name != methodName) continue;
                    if (TryBuildInvokeArgsForMethod(mi2, argsClr, out var candidateArgs, out var score))
                    {
                        if (score > bestScore)
                        {
                            bestScore = score;
                            miFound = mi2;
                            bestInvokeArgs = candidateArgs;
                        }
                    }
                }

                if (miFound != null && bestInvokeArgs != null)
                {                    
                    if (!miFound.IsStatic)
                    {
                        Log.AddRuntimeLog(LID.ShowMessageAssert, $"{callName}: instance methods not supported in bridge");
                        return true;
                    }
                    var ret2 = miFound.Invoke(null, bestInvokeArgs);
                    _ = TryStoreLegacyReturnToBridgeObject(values, ret2);
                    if (miFound.ReturnType != typeof(void))
                    {
                        var sv2 = SValue.FromClrObject(ret2);
                        //PushSValueSynced(sv2);
                    }
                    return true;
                }
            }

            if (miFound == null)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"{callName}: method not found " + methodName);
                return true;
            }

            if (!TryBuildInvokeArgsForMethod(miFound, argsClr, out var invokeArgs, out _))
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"{callName}: method signature mismatch " + methodName);
                return true;
            }

            if (!miFound.IsStatic)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, $"{callName}: instance methods not supported in bridge");
                return true;
            }

            var ret = miFound.Invoke(null, invokeArgs);
            _ = TryStoreLegacyReturnToBridgeObject(values, ret);
            if (miFound.ReturnType != typeof(void))
            {
                var sv = SValue.FromClrObject(ret);
                PushSValueSynced(sv);
            }

            return true;
        }

        public void RunInstruction(Instruction iri)
        {
            if (iri == null) return;
#if DEBUG
            int opcode = (int)iri.opCode;
            var idd = this.id;
#endif
            switch (iri.opCode)
            {
                case EIROpCode.Nop: break;
                case EIROpCode.LoadConstNull:
                    {
                        TryPushStackSlot(out int slot);
                        m_ValueStack[slot].SetNull();
                    }
                    break;
                case EIROpCode.LoadConstBoolean:
                    {
                        if (iri.TryGetBoolean(out bool b) && TryPushStackSlot(out int slot))
                            m_ValueStack[slot].SetBoolValue(b);
                    }
                    break;
                case EIROpCode.LoadConstUInt8:
                    {
                        if (iri.TryGetUInt8(out byte cb) && TryPushStackSlot(out int slot))
                            m_ValueStack[slot].SetUInt8Value(cb);
                    }
                    break;
                case EIROpCode.LoadConstInt8:
                    {
                        if (iri.TryGetInt8(out sbyte sb) && TryPushStackSlot(out int slot))
                            m_ValueStack[slot].SetInt8Value(sb);
                    }
                    break;
                case EIROpCode.LoadConstInt16:
                    {
                        if (iri.TryGetInt16(out short sv) && TryPushStackSlot(out int slot))
                            m_ValueStack[slot].SetInt16Value(sv);
                    }
                    break;
                case EIROpCode.LoadConstUInt16:
                    {
                        if (iri.TryGetUInt16(out ushort usv) && TryPushStackSlot(out int slot))
                            m_ValueStack[slot].SetUInt16Value(usv);
                    }
                    break;
                case EIROpCode.LoadConstInt32:
                    {
                        if (iri.TryGetInt32(out int i32) && TryPushStackSlot(out int slot))
                            m_ValueStack[slot].SetInt32Value(i32);
                    }
                    break;
                case EIROpCode.LoadConstUInt32:
                    {
                        if (iri.TryGetUInt32(out uint ui32) && TryPushStackSlot(out int slot))
                            m_ValueStack[slot].SetUInt32Value(ui32);
                    }
                    break;
                case EIROpCode.LoadConstInt64:
                    {
                        if (iri.TryGetInt64(out long l) && TryPushStackSlot(out int slot))
                            m_ValueStack[slot].SetInt64Value(l);
                    }
                    break;
                case EIROpCode.LoadConstUInt64:
                    {
                        if (iri.TryGetUInt64(out ulong ul) && TryPushStackSlot(out int slot))
                            m_ValueStack[slot].SetUInt64Value(ul);
                    }
                    break;
                case EIROpCode.LoadConstFloat32 :
                    {
                        if (iri.TryGetFloat32(out float f) && TryPushStackSlot(out int slot))
                            m_ValueStack[slot].SetFloatValue(f);
                    }
                    break;
                case EIROpCode.LoadConstFloat64:
                    {
                        if (iri.TryGetFloat64(out double d) && TryPushStackSlot(out int slot))
                            m_ValueStack[slot].SetDoubleValue(d);
                    }
                    break;
                case EIROpCode.LoadConstString:
                    {
                        var resolved = SLAssembly.TryGetConstString(iri.index) ?? string.Empty;
                        if (TryPushStackSlot(out int slot))
                            m_ValueStack[slot].SetStringValue(resolved);
                    }
                    break;
                case EIROpCode.LoadConstType:
                    {
                        var mdt = TryGetInstructionRuntimeDefType(iri);
                        if (mdt != null)
                        {
                            var rt = GetRuntimeTypeByDefType(mdt, m_CurrentRuntimeClass != null ? m_CurrentRuntimeClass : mdt.ownerRuntimeClass, m_InputTemplateRuntimeTypeList, true);
                            var sobj = new TypeObject(rt);
                            sobj.CreateObject();
                            if (TryPushStackSlot(out int slot))
                                m_ValueStack[slot].SetValueBySObject(sobj);
                        }
                    }
                    break;
                case EIROpCode.Convert_I8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.UInt8);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to UInt8");
#endif
                    }
                    break;
                case EIROpCode.Convert_SI8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Int8);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to Int8");
#endif
                    }
                    break;
                case EIROpCode.Convert_I16:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Int16);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to Int16");
#endif
                    }
                    break;
                case EIROpCode.Convert_UI16:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.UInt16);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to UInt16");
#endif
                    }
                    break;
                case EIROpCode.Convert_I32:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Int32);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to Int32");
#endif
                    }
                    break;
                case EIROpCode.Convert_UI32:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.UInt32);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to UInt32");
#endif
                    }
                    break;
                case EIROpCode.Convert_I64:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Int64);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to Int64");
#endif
                    }
                    break;
                case EIROpCode.Convert_UI64:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.UInt64);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to UInt64");
#endif
                    }
                    break;
                case EIROpCode.Convert_R4:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Float32);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to Float32");
#endif
                    }
                    break;
                case EIROpCode.Convert_R8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Float64);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to Float64");
#endif
                    }
                    break;
                case EIROpCode.Convert_ToString:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.String);
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "Convert to String");
#endif
                    }
                    break;
                case EIROpCode.LoadArgument:
                    {
                        if (TryPushStackSlot(out int slot))
                        {
                            if ((uint)iri.index > m_ArgumentRuntimeObjectArray.Length)
                            {
                                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "LoadArgument", iri.index);
                                return;
                            }
                            m_ArgumentRuntimeObjectArray[(uint)iri.index].SetSValueByRuntimeObjct(ref m_ValueStack[slot]);
#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "LoadArgument: index=" + iri.index);
#endif
                        }
                    }
                    break;
                case EIROpCode.LoadLocal:
                    {
                        if (TryPushStackSlot(out int slot))
                        {
                            if ((uint)iri.index > m_LocalVariableRuntimeObjectArray.Length)
                            {
                                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "LoadLocal", iri.index );
                                return;
                            }
                            m_LocalVariableRuntimeObjectArray[(uint)iri.index].SetSValueByRuntimeObjct(ref m_ValueStack[slot] );
#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "LoadLocal: index=" + iri.index);
#endif
                        }
                    }
                    break;
                case EIROpCode.LoadGlobal:
                    {
                        if (TryPushStackSlot(out int slot))
                        {
                            CLRVM.LoadGlobalVariable((uint)iri.index, ref m_ValueStack[slot]);
#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "LoadGlobal: index=" + iri.index);
#endif
                        }
                    }
                    break;
                case EIROpCode.StoreLocal:
                    {
                        if (m_ValueIndex > 0)
                        {
                            if ((uint)iri.index > m_LocalVariableRuntimeObjectArray.Length)
                            {
                                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "MethodId:" + id.ToString() + "SetLocalVariableSValue", (uint)iri.index );
                                return;
                            }
                            m_LocalVariableRuntimeObjectArray[(uint)iri.index].SetSObjectBySValue(ref m_ValueStack[--m_ValueIndex]);
#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreLocal: index=" + iri.index);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "MethodId:" + id.ToString() + $"StoreLocal stack underflow at index {iri.index}");
                        }
                    }
                    break;
                case EIROpCode.StoreGlobal:
                    {
                        if (m_ValueIndex > 0)
                        {
                            CLRVM.StoreGlobalVariable((uint)iri.index, ref m_ValueStack[--m_ValueIndex]);
#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreGlobal: index=" + iri.index);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;
                case EIROpCode.StoreReturn:
                    {
                        if (m_ValueIndex > 0)
                        {
                            if ((uint)iri.index > m_ReturnRuntimeObjectArray.Length)
                            {
                                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "MethodId:" + id.ToString() + "StoreReturn", (uint)iri.index);
                                return;
                            }
                            m_ReturnRuntimeObjectArray[(uint)iri.index].SetSObjectBySValue(ref m_ValueStack[--m_ValueIndex]);
#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreReturn: index=" + iri.index);
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "MethodId:" + id.ToString() + $"StoreReturn stack underflow at index {iri.index}");
                        }
                        m_ExecuteIndex = m_ExecuteCount;
                    }
                    break;
                case EIROpCode.LoadArrayIndex:
                    {
                        ref var v = ref m_ValueStack[m_ValueIndex - 1];
                        if (v.sobject is ArrayObject ao)
                        {
                            ao.LoadValue(iri.index, ref v );
#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "LoadArrayIndex: runtimeclass=" + ao.runtimeClass?.name
                                            + " objectId=" + ao.id + "index=" + iri.index );
#endif
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", v.eType.ToString() );
                        }
                    }
                    break;
                case EIROpCode.StoreArrayIndex:
                    {
                        int int1 = 1, int2 = 2;
                        byte flagByte = (byte)EStoreArrayIndexFlag.StoreTopMinus1_ValueTopMinus2;
                        if (iri.TryGetUInt8(out var bflag))
                        {
                            flagByte = bflag;
                        }
                        else if (iri.TryGetBoolean(out var oldFlag))
                        {
                            // backward-compat with old Front emitter: true means swapped order.
                            flagByte = oldFlag
                                ? (byte)EStoreArrayIndexFlag.StoreTopMinus2_ValueTopMinus1
                                : (byte)EStoreArrayIndexFlag.StoreTopMinus1_ValueTopMinus2;
                        }

                        if (flagByte == (byte)EStoreArrayIndexFlag.StoreTopMinus2_ValueTopMinus1)
                        {
                            int1 = 2;
                            int2 = 1;
                        }

                        if (m_ValueIndex - int1 >= 0 && m_ValueIndex - int2 >= 0 )
                        {
                            ref SValue sStore = ref m_ValueStack[m_ValueIndex - int1];
                            ref SValue sValue = ref m_ValueStack[m_ValueIndex - int2];
                            if (sStore.sobject is ArrayObject ao)
                            {
                                ao.StoreValue(iri.index, sValue);
#if DEBUG
                                Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreArrayIndex: runtimeclass=" + ao.runtimeClass?.name
                                            + " objectId=" + ao.id + "index=" + iri.index );
#endif
                            }
                            else
                            {
                                Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM StoreArrayIndex", sStore.eType.ToString());
                            }
                            m_ValueIndex -= 2;
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;
                case EIROpCode.LoadArrayIndexField:
                    {
                        if (m_ValueIndex > 1 )
                        {
                            ref SValue arrayref = ref m_ValueStack[m_ValueIndex - 2];
                            ref SValue loadindex = ref m_ValueStack[m_ValueIndex - 1];

                            if (arrayref.sobject is ArrayObject ao)
                            {
                                if (SValue.TryGetInt32FromSValue(loadindex, out var idx))
                                {
                                    ao.LoadValue(idx, ref arrayref );
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "LoadArrayIndexField: runtimeclass=" + ao.runtimeClass?.name
                                            + " objectId=" + ao.id + "index=" + idx);
#endif
                                }
                                else
                                {
                                    Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndexField", loadindex.eType.ToString());
                                }
                            }
                            else
                            {
                                Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndexField", arrayref.eType.ToString());
                            }
                            m_ValueIndex -= 1;
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;
                case EIROpCode.StoreArrayIndexField:
                    {
                        if (m_ValueIndex > 2)
                        {
                            ref SValue arrayref = ref m_ValueStack[m_ValueIndex - 3];
                            ref SValue loadindex = ref m_ValueStack[m_ValueIndex - 2];
                            ref SValue storevalue = ref m_ValueStack[m_ValueIndex - 1];

                            if (arrayref.sobject is ArrayObject ao)
                            {
                                if (SValue.TryGetInt32FromSValue(loadindex, out var idx))
                                {
                                    ao.StoreValue(idx, storevalue);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreArrayIndexField: runtimeclass=" + ao.runtimeClass?.name
                                            + " objectId=" + ao.id + "index=" + idx );
#endif
                                }
                                else
                                {
                                    Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM StoreArrayIndexField", loadindex.eType.ToString());
                                }
                            }
                            else
                            {
                                Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM StoreArrayIndexField", arrayref.eType.ToString());
                            }
                            m_ValueIndex -= 3;
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM LoadArrayIndex", "");
                        }
                    }
                    break;
                case EIROpCode.Dup:
                    {
                        int dupCount = 1;
                        if (iri.Payload != null && iri.Payload.Length >= 4)
                        {
                            dupCount = BitConverter.ToInt32(iri.Payload, 0); 
                        }

                        if (m_ValueIndex >= dupCount)
                        {
                            int baseIndex = m_ValueIndex - dupCount;
                            for (int i = 0; i < dupCount; i++)
                            {
                                PushSValueSynced(m_ValueStack[baseIndex + i]);
                            }
                        }
                    }
                    break;
                case EIROpCode.Pop:
                    if (m_ValueIndex > 0) m_ValueIndex--;
                    break;
                case EIROpCode.LoadStaticField:
                    {
                        var mt = TryGetInstructionRuntimeDefType(iri);
                        if (mt != null)
                        {
                            var rt = RuntimeTypeManager.GetRuntimeTypeByDefTypeAndAdd(mt);
                            if (rt != null)
                            {
                                if (TryPushStackSlot(out int slot))
                                    rt.GetStaticMemberVariableSValue(iri.index, ref m_ValueStack[slot]);
                            }
                        }
                    }
                    break;
                case EIROpCode.StoreStaticField:
                    {
                        var mt = TryGetInstructionRuntimeDefType(iri);
                        if (mt != null)
                        {
                            if (m_ValueIndex > 0)
                            {
                                var val = m_ValueStack[--m_ValueIndex];
                                var rt = RuntimeTypeManager.GetRuntimeTypeByDefTypeAndAdd(mt);
                                if (rt == null)
                                {
                                    Log.AddRuntimeLog(LID.ShowMessageAssert, "StoreStaticField failed to get runtime type for metadata type: ");
                                    break;
                                }
                                rt?.SetStaticMemberVariableSValue(iri.index, val);
                            }
                        }
                    }
                    break;
                case EIROpCode.LoadNotStaticField:
                    {
                        // expects instance on stack
                        if (m_ValueIndex > 0)
                        {
                            var inst = m_ValueStack[m_ValueIndex-1];
                            if (inst.eType == EVMType.Array
                                || inst.eType == EVMType.Class
                                || inst.eType == EVMType.Type
                                || inst.eType == EVMType.Object
                                || inst.eType == EVMType.Member )
                            {
                                --m_ValueIndex;
                                if (inst.sobject is ClassObject co)
                                {
                                    if (TryPushStackSlot(out int slot))
                                    {
                                        co.GetMemberVariableSValue(iri.index, ref m_ValueStack[slot]);
#if DEBUG
                                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "LoadNotStaticField: runtimeclass=" + co.runtimeClass?.name 
                                            + " objectId=" + co.id + "index=" + iri.index  );
#endif
                                    }
                                }
                                else
                                {
                                    if (TryPushStackSlot(out int slot))
                                        m_ValueStack[slot].SetNull();
                                }
                            }
                            //else
                            //{
                            //    Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM StoreArrayIndex", inst.eType.ToString());
                            //}
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "MethodId:" + id.ToString() + "RuntimeVM StoreArrayIndex", "" );
                        }
                    }
                    break;
                case EIROpCode.StoreNotStaticField2:
                    {
                        // expect value then instance on stack (value pushed last)
                        if (m_ValueIndex >= 2)
                        {
                            ref var val = ref m_ValueStack[--m_ValueIndex];
                            ref var inst = ref m_ValueStack[--m_ValueIndex];
                            if (inst.sobject is ClassObject co )
                            {
                                co.SetMemberVariableSValue( iri.index, val);
#if DEBUG
                                Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreNotStaticField2: runtimeclass=" + co.runtimeClass?.name
                                            + " objectId=" + co.id + "index=" + iri.index);
#endif
                            }
                            
                        }
                    }
                    break;

                case EIROpCode.StoreNotStaticField1:
                    {
                        // -2鍦ㄥ瓨鍌ㄧ殑鍊?-1琛ㄧず瑕佸瓨鍌ㄧ殑瀵硅薄 瀛樺偍瀹屾垚锛岀洿鎺ュ彉鎴愪綅缃?
                        // expect value then instance on stack (value pushed last)
                        if (m_ValueIndex >= 2)
                        {
                            ref SValue val = ref m_ValueStack[m_ValueIndex - 1];
                            ref SValue inst = ref m_ValueStack[m_ValueIndex - 2];
                            if (inst.sobject is ClassObject co)
                            {
                                co.SetMemberVariableSValue(iri.index, val);
#if DEBUG
                                Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreNotStaticField1: runtimeclass=" + co.runtimeClass?.name
                                            + " objectId=" + co.id + "index=" + iri.index);
#endif
                            }
                            //else
                            //{
                            //    Log.AddRuntimeLog(LID.RuntimeVMNotFoundHandleEVMType, "RuntimeVM StoreArrayIndex", inst.eType.ToString());
                            //}
                            m_ValueIndex -= 1;
                        }
                    }
                    break;
                //case EIROpCode.ClassInit:
                //    {
                //        var mdt = TryGetInstructionRuntimeDefType(iri);
                //        if (mdt != null)
                //        {
                //            var rt = GetRuntimeTypeByDefType(mdt, m_CurrentRuntimeClass != null ? m_CurrentRuntimeClass : mdt.ownerRuntimeClass, m_InputTemplateRuntimeTypeList, true);
                //        }
                //    }
                //    break;
                case EIROpCode.NewObject:
                    {
                        if( iri.TryGetInt32(out int i32) )
                        {
                            var rt = RuntimeTypeManager.GetRuntimeTypeById(i32);
                            // If runtime type not yet created, try to find corresponding RuntimeClass
                            // (possibly registered from package metadata) and dynamically register a RuntimeType for it.
                            if (rt == null)
                            {
                                // first try existing runtime class
                                var rc = RuntimeClassManager.GetRuntimeClassById(i32);
                                // if not found, attempt to resolve/create from loaded package metadata
                                if (rc == null)
                                {
                                    rc = SLRuntimeModuleRegistry.ResolveOrCreateRuntimeClassById(i32);
                                }
                                if (rc != null)
                                {
                                    rt = RuntimeTypeManager.AddRuntimeTypeByClass(rc);
                                }
                            }
                            SObject sobj = ObjectManager.CreateObjectByRuntimeType(rt, true);


#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreNotStaticField1: runtimeclass=" + rt.runtimeClass?.name + " objectId=" + sobj.id );
#endif

                            ObjectManager.RegisterObject(sobj);
                            m_ValueStack[m_ValueIndex++].SetRawSObject(sobj);

                            if( rt.runtimeClass.metaClassKind == 0 )
                            {
                                var irList = rt.runtimeClass.nonStaticMemberVariableSetValueList;
                                if (irList.Count > 0)
                                {
                                    CLRVM.RunIRNewMethod($"__new_object__{rt.runtimeClass.name}", rt.runtimeTemplateList, irList);
                                }
                            }
                            //var sv = default(SValue);
                            //sv.SetSObject(sobj);
                            //PushSValueSynced(sv);
                        }
                    }
                    break;
                case EIROpCode.NewTemplateObject:
                    {
                        var mdt = TryGetInstructionRuntimeDefType(iri);
                        if (mdt != null)
                        {
                            var rt = GetRuntimeTypeByDefType(mdt, m_CurrentRuntimeClass != null ? m_CurrentRuntimeClass : mdt.ownerRuntimeClass, m_InputTemplateRuntimeTypeList, true);
                            SObject sobj = ObjectManager.CreateObjectByRuntimeType(rt, true);
                            ObjectManager.RegisterObject(sobj);
                            m_ValueStack[m_ValueIndex++].SetValueBySObject(sobj);
                            var irc = rt.runtimeClass;

#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreNotStaticField1: runtimeclass=" + rt.runtimeClass?.name + " objectId=" + sobj.id);
#endif

                            var irList = rt.runtimeClass.nonStaticMemberVariableSetValueList;
                            if (irList.Count > 0)
                            {
                                CLRVM.RunIRNewMethod($"__new_object__{rt.runtimeClass.name}", rt.runtimeTemplateList, irList);
                            }
                            //if (TryPushStackSlot(out int slot))
                            //    m_ValueStack[slot].SetSObject(sobj);
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "MethodId:" + id.ToString() + "new array get svalue");
                            break;
                        }
                    }
                    break;
                case EIROpCode.NewArray:
                    {
                        // expects length on stack
                        var rdt = TryGetInstructionRuntimeDefType(iri);
                        if (m_ValueIndex > 0 && rdt != null)
                        {
                            var sval = m_ValueStack[m_ValueIndex - 1];
                            if (!SValue.TryGetInt32FromSValue(sval, out var arrLength))
                            {
                                Log.AddRuntimeLog(LID.ShowMessageAssert, "MethodId:" + id.ToString() + "new array get svalue");
                                break;
                            }

                            if (arrLength < 0)
                            {
                                Log.AddRuntimeLog(LID.ShowMessageAssert, "MethodId:" + id.ToString() +
                                    $"不能将负值写入无符号类型: target= int32, source={sval.eType}");
                                return;
                            }

                            var rt = GetRuntimeTypeByDefType(rdt, m_CurrentRuntimeClass != null ? m_CurrentRuntimeClass : rdt.ownerRuntimeClass, m_InputTemplateRuntimeTypeList, true);
                            ArrayObject arr = new ArrayObject(rt, arrLength);
                            // NewArray opcode path should initialize only the backing storage.
                            // Full CreateObject() may require runtime member types that are not guaranteed ready.
                            arr.CreateObject();
                            ObjectManager.AddClassObject(arr);
                            m_ValueStack[m_ValueIndex - 1].SetArrayObject(arr);

#if DEBUG
                            Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "StoreNotStaticField1: runtimeclass=" + rt.runtimeClass?.name + " objectId=" + arr.id);
#endif


                            //var sv = default(SValue);
                            //sv.SetSObject(arr);
                            //PushSValueSynced(sv);
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "new array get svalue");
                            break;
                        }
                    }
                    break;
                case EIROpCode.Br:
                case EIROpCode.BrLabel:
                case EIROpCode.Break:
                case EIROpCode.Jmp:
                    {
                        m_ExecuteIndex = (ushort)iri.index;
#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "jumpto->" + m_ExecuteIndex);
#endif
                    }
                    break;
                case EIROpCode.Label:
                    {

#if DEBUG
                        Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "label" );
#endif
                    }
                    break;
                case EIROpCode.BrFalse:
                    {
                        if (m_ValueIndex > 0)
                        {
                            var cond = m_ValueStack[--m_ValueIndex];
                            if(cond.eType == EVMType.Boolean )
                            {
                                if (cond.int8Value != 1)
                                {
                                    m_ExecuteIndex = (ushort)(iri.index - 1);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brfalse to->" + m_ExecuteIndex);
#endif
                                }
                                else
                                {
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brfalse nojump ");
#endif
                                }
                            }
                            else if( cond.sobject is BoolObject bl )
                            {
                                if ( !bl.value )
                                {
                                    m_ExecuteIndex = (ushort)(iri.index - 1);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brfalse to->" + m_ExecuteIndex );
#endif
                                }
                                else
                                {
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brfalse nojump ");
#endif
                                }
                            }
                            else
                            {
                                Log.AddRuntimeLog(LID.ShowMessageAssert, "MethodId:" + id.ToString() + "BrFalse");
                                break;
                            }
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "MethodId:" + id.ToString() + "BrFalse");
                            break;
                        }
                    }
                    break;
                case EIROpCode.BrTrue:
                    {
                        if (m_ValueIndex > 0)
                        {
                            var cond = m_ValueStack[--m_ValueIndex];
                            if (cond.eType == EVMType.Boolean)
                            {
                                if (cond.int8Value != 1)
                                {
                                    m_ExecuteIndex = (ushort)(iri.index - 1);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brtrue to->" + m_ExecuteIndex);
#endif
                                }
                                else
                                {
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brtrue nojump ");
#endif
                                }
                            }
                            else if (cond.sobject is BoolObject bl)
                            {
                                if (bl.value)
                                {
                                    m_ExecuteIndex = (ushort)(iri.index - 1);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brtrue to->" + m_ExecuteIndex);
#endif
                                }
                                else
                                {
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() + "brtrue nojump ");
#endif
                                }
                            }
                            else
                            {
                                Log.AddRuntimeLog(LID.ShowMessageAssert, "MethodId:" + id.ToString() + "BrTrue");
                                break;
                            }
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "new array get svalue");
                            break;
                        }
                    }
                    break;
                case EIROpCode.Switch:
                    {
                        if (m_ValueIndex >= 2)
                        {
                            ref var right = ref  m_ValueStack[--m_ValueIndex];
                            ref var left = ref m_ValueStack[m_ValueIndex];
                            //bool methodCall = false;
                            //SValue.CompareEuqalSValue1AndValue2(ref left, ref right, true, out methodCall);
                            //PushSValueSynced(left);
                            if (SValue.TryGetInt32FromSValue(left, out var switchValue) && SValue.TryGetInt32FromSValue(right, out _))
                            {
                                int caseCount = iri.opValue is int[] arr ? arr.Length : 0;
                                bool matched = false;
                                for (int i = 0; i < caseCount; i++)
                                {
                                    if (switchValue == (iri.opValue as int[])[i])
                                    {
                                        m_ExecuteIndex = (ushort)(iri.index + i - 1);
                                        matched = true;
                                        break;
                                    }
                                }
                                if (!matched)
                                {
                                    m_ExecuteIndex = (ushort)(iri.index + caseCount - 1);
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "MethodId:" + id.ToString() +  "  switch to->" + m_ExecuteIndex);
#endif
                                }
                                else
                                {
#if DEBUG
                                    Log.AddVM(LID.ShowMessageInfo, "switch nojump ");
#endif
                                }
                            }
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "new array get svalue");
                            break;
                        }
                    }
                    break;
                case EIROpCode.And:
                    {
                        if (m_ValueIndex >= 2)
                        {
                            ref var right = ref m_ValueStack[--m_ValueIndex];
                            ref var left = ref m_ValueStack[--m_ValueIndex];
                            bool methodCall = false;
                            SValue.LogicalAnd(ref left, ref right, out methodCall);
                            if (methodCall)
                            {
                                if (m_ValueIndex > 0)
                                {
                                    var top = m_ValueStack[m_ValueIndex - 1];
                                    if (top.eType == EVMType.Boolean)
                                    {
                                        PushSValueSynced(top);
                                    }
                                    else
                                    {
                                        bool b = SValue.IsTruthy(ref top);
                                        top.SetBoolValue(b);
                                        PushSValueSynced(top);
                                    }
                                }
                                else
                                {
                                    PushSValueSynced(left);
                                }
                            }
                            else
                            {
                                PushSValueSynced(left);
                            }
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "new array get svalue");
                            break;
                        }
                    }
                    break;
                case EIROpCode.Or:
                    {
                        if (m_ValueIndex >= 2)
                        {
                            ref var right = ref m_ValueStack[--m_ValueIndex];
                            ref var left = ref m_ValueStack[--m_ValueIndex];
                            bool methodCall = false;
                            SValue.LogicalOr(ref left, ref right, out methodCall);
                            if (methodCall)
                            {
                                if (m_ValueIndex > 0)
                                {
                                    var top = m_ValueStack[m_ValueIndex - 1];
                                    if (top.eType == EVMType.Boolean)
                                    {
                                        PushSValueSynced(top);
                                    }
                                    else
                                    {
                                        bool b = SValue.IsTruthy(ref top);
                                        top.SetBoolValue(b);
                                        PushSValueSynced(top);
                                    }
                                }
                                else
                                {
                                    PushSValueSynced(left);
                                }
                            }
                            else
                            {
                                PushSValueSynced(left);
                            }
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "new array get svalue");
                            break;
                        }
                    }
                    break;
                case EIROpCode.Ceq:
                case EIROpCode.Ceq_Un:
                    {
                        ExecuteEqualityOperation(iri, true, false);
                    }
                    break;
                case EIROpCode.Cne:
                case EIROpCode.Cne_Un:
                    {
                        ExecuteEqualityOperation(iri, false, false);
                    }
                    break;
                case EIROpCode.Beq:
                case EIROpCode.Beq_Un:
                    {
                        ExecuteEqualityOperation(iri, true, true);
                    }
                    break;
                case EIROpCode.Bne:
                case EIROpCode.Bne_Un:
                    {
                        ExecuteEqualityOperation(iri, false, true);
                    }
                    break;
                case EIROpCode.Clt:
                case EIROpCode.Clt_Un:
                    {
                        ExecuteRelationalOperation(iri, 2, false);
                    }
                    break;
                case EIROpCode.Cgt:
                case EIROpCode.Cgt_Un:
                    {
                        ExecuteRelationalOperation(iri, 0, false);
                    }
                    break;
                case EIROpCode.Cge:
                case EIROpCode.Cge_Un:
                    {
                        ExecuteRelationalOperation(iri, 1, false);
                    }
                    break;
                case EIROpCode.Cle:
                case EIROpCode.Cle_Un:
                    {
                        ExecuteRelationalOperation(iri, 3, false);
                    }
                    break;
                case EIROpCode.Bge:
                case EIROpCode.Bge_un:
                    {
                        ExecuteRelationalOperation(iri, 1, true);
                    }
                    break;
                case EIROpCode.Bgt:
                case EIROpCode.Bgt_Un:
                    {
                        ExecuteRelationalOperation(iri, 0, true);
                    }
                    break;
                case EIROpCode.Ble:
                case EIROpCode.Ble_Un:
                    {
                        ExecuteRelationalOperation(iri, 3, true);
                    }
                    break;
                case EIROpCode.Neg:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, "EIROpCode.Neg", 1, m_ValueIndex );
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 1].NegSValue(false);
                    }
                    break;
                case EIROpCode.Not:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, "EIROpCode.Not", 1, m_ValueIndex );
                            break;
                        }
                        m_ValueStack[m_ValueIndex - 1].NotSValue();
                    }
                    break;
                case EIROpCode.Add:
                case EIROpCode.Add_Un:
                case EIROpCode.Minus:
                case EIROpCode.Minus_Un:
                case EIROpCode.Multiply:
                case EIROpCode.Multiply_Un:
                case EIROpCode.Divide:
                case EIROpCode.Divide_Un:
                case EIROpCode.Modulo:
                case EIROpCode.Module_Un:
                case EIROpCode.Combine:
                case EIROpCode.Combine_Un:
                case EIROpCode.InclusiveOr:
                case EIROpCode.InclusiveOr_Un:
                case EIROpCode.XOR:
                case EIROpCode.XOR_Un:
                case EIROpCode.Shi:
                case EIROpCode.Shi_Un:
                case EIROpCode.Shr:
                case EIROpCode.Shr_Un:
                    {
                        if (m_ValueIndex >= 2)
                        {
                            ref var right = ref m_ValueStack[--m_ValueIndex];
                            ref var left = ref m_ValueStack[--m_ValueIndex];
                            int sign = 0;
                            bool isUn = iri.opCode == EIROpCode.Add_Un
                                || iri.opCode == EIROpCode.Minus_Un
                                || iri.opCode == EIROpCode.Multiply_Un
                                || iri.opCode == EIROpCode.Divide_Un
                                || iri.opCode == EIROpCode.Module_Un
                                || iri.opCode == EIROpCode.Combine_Un
                                || iri.opCode == EIROpCode.InclusiveOr_Un
                                || iri.opCode == EIROpCode.XOR_Un
                                || iri.opCode == EIROpCode.Shi_Un
                                || iri.opCode == EIROpCode.Shr_Un;
                            switch (iri.opCode)
                            {
                                case EIROpCode.Add: sign = 0; break;
                                case EIROpCode.Add_Un: sign = 0; break;
                                case EIROpCode.Minus: sign = 1; break;
                                case EIROpCode.Minus_Un: sign = 1; break;
                                case EIROpCode.Multiply: sign = 2; break;
                                case EIROpCode.Multiply_Un: sign = 2; break;
                                case EIROpCode.Divide: sign = 3; break;
                                case EIROpCode.Divide_Un: sign = 3; break;
                                case EIROpCode.Modulo: sign = 4; break;
                                case EIROpCode.Module_Un: sign = 4; break;
                                case EIROpCode.Combine: sign = 5; break;
                                case EIROpCode.Combine_Un: sign = 5; break;
                                case EIROpCode.InclusiveOr: sign = 6; break;
                                case EIROpCode.InclusiveOr_Un: sign = 6; break;
                                case EIROpCode.XOR: sign = 7; break;
                                case EIROpCode.XOR_Un: sign = 7; break;
                                case EIROpCode.Shi: sign = 8; break;
                                case EIROpCode.Shi_Un: sign = 8; break;
                                case EIROpCode.Shr: sign = 9; break;
                                case EIROpCode.Shr_Un: sign = 9; break;
                            }
                            SValue.ComputeValueInline(ref left, sign, ref right, isUn);
                            PushSValueSynced(left);
                        }
                    }
                    break;
                case EIROpCode.CallSystemMethod:
                    {
                        if (!iri.TryGetSystemMethodCallPackage(out var sysPkg) || sysPkg == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "CallSystemMethod: expected JSON payload with name/paramCount/systemMethodKind");
                            break;
                        }

                        int kind = sysPkg.systemMethodKind;
                        switch (kind)
                        {
                            case (int)ESystemMethodCall.SystemPrint:
                                ConsoleSystemMethodCall.ExecuteSystemPrint(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemPrintln:
                                ConsoleSystemMethodCall.ExecuteSystemPrintln(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemCallCLRMethod:
                                BridgeSystemMethodCall.ExecuteSystemCallCLRMethod(this, iri);
                                break;
                            case (int)ESystemMethodCall.SystemCallNativeMethod:
                                BridgeSystemMethodCall.ExecuteSystemCallNativeMethod(this, iri);
                                break;
                            case (int)ESystemMethodCall.SystemCallJVMMethod:
                                BridgeSystemMethodCall.ExecuteSystemCallJVMMethod(this, iri);
                                break;
                            case (int)ESystemMethodCall.SystemReadLine:
                                ConsoleSystemMethodCall.ExecuteSystemReadLine(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemReadKey:
                                ConsoleSystemMethodCall.ExecuteSystemReadKey(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemConvertInt8:
                            case (int)ESystemMethodCall.SystemConvertUInt8:
                            case (int)ESystemMethodCall.SystemConvertBool:
                            case (int)ESystemMethodCall.SystemConvertSInt8:
                            case (int)ESystemMethodCall.SystemConvertInt16:
                            case (int)ESystemMethodCall.SystemConvertUInt16:
                            case (int)ESystemMethodCall.SystemConvertInt32:
                            case (int)ESystemMethodCall.SystemConvertUInt32:
                            case (int)ESystemMethodCall.SystemConvertInt64:
                            case (int)ESystemMethodCall.SystemConvertUInt64:
                            case (int)ESystemMethodCall.SystemConvertFloat32:
                            case (int)ESystemMethodCall.SystemConvertFloat64:
                                NumSystemMethodCall.ExecuteNumericConvert(this, sysPkg, (ESystemMethodCall)kind);
                                break;
                            case (int)ESystemMethodCall.SystemInt32Parse:
                                NumSystemMethodCall.ExecuteSystemInt32Parse(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemNumAbs:
                                NumSystemMethodCall.ExecuteSystemNumAbs(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemNumFloor:
                                NumSystemMethodCall.ExecuteSystemNumFloor(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemConvertString:
                                StringSystemMethodCall.ExecuteStringConvert(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemStringFormat:
                                StringSystemMethodCall.ExecuteStringFormat(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemStringFront:
                                StringSystemMethodCall.ExecuteStringFront(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemStringEnd:
                                StringSystemMethodCall.ExecuteStringEnd(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemStringRange:
                                StringSystemMethodCall.ExecuteStringRange(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemStringToByteArray:
                                StringSystemMethodCall.ExecuteStringToByteArray(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemEqualObject:
                                ObjectSystemMethodCall.ExecuteSystemEqualObject(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.DataAllEqual:
                                DataSystemMethodCall.ExecuteDataAllEqual(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.DataTypeEqual:
                                DataSystemMethodCall.ExecuteDataTypeEqual(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.DataNameAndTypeEqual:
                                DataSystemMethodCall.ExecuteDataNameAndTypeEqual(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.DataDataEqual:
                                DataSystemMethodCall.ExecuteDataDataEqual(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemBuildDataString:
                                DataSystemMethodCall.ExecuteBuildDataString(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectGetType:
                                ObjectSystemMethodCall.ExecuteSystemObjectGetType(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectGetHashCode:
                                ObjectSystemMethodCall.ExecuteSystemObjectGetHashCode(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectRef:
                                ObjectSystemMethodCall.ExecuteSystemObjectRef(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectRefWeak:
                                ObjectSystemMethodCall.ExecuteSystemObjectRefWeak(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectRefCount:
                                ObjectSystemMethodCall.ExecuteSystemObjectRefCount(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectFree:
                                ObjectSystemMethodCall.ExecuteSystemObjectFree(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemObjectRelease:
                                ObjectSystemMethodCall.ExecuteSystemObjectRelease(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemArrayGetValueThis:
                                ObjectSystemMethodCall.ExecuteSystemArrayGetValueThis(this, sysPkg);
                                break;
                            case (int)ESystemMethodCall.SystemArraySetValueThis:
                                ObjectSystemMethodCall.ExecuteSystemArraySetValueThis(this, sysPkg);
                                break;
                            default:
                                Log.AddRuntimeLog(LID.ShowMessageAssert, "CallSystemMethod: unknown systemMethodKind " + kind + " name=" + sysPkg.name);
                                break;
                        }
                    }
                    break;
                case EIROpCode.CallStatic:
                    {
                        // try to create runtime call on demand from instruction payload
                        SLRuntimeCallPackage? callPkg = null;
                        if (iri.TryGetRuntimeCallPackage(out var parsedCallPkg)) callPkg = parsedCallPkg;

                        RuntimeCall? runtimeCall = SLRuntimeModuleRegistry.TryCreateRuntimeCallForInstruction(callPkg, iri.index);
                        if (runtimeCall == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "鎵ц闈欐€佸嚱鏁帮紝娌℃湁鍙戠幇鐩稿叧鍑芥暟浣?");
                            return;
                        }

                        List<RuntimeType> classRTList = new List<RuntimeType>();
                        for (int i = 0; i < runtimeCall.runtimeDefType.runtimeDefTypeList.Count; i++)
                        {
                            var crt = GetRuntimeTypeByDefType(runtimeCall.runtimeDefType.runtimeDefTypeList[i], runtimeCall.runtimeDefType.runtimeDefTypeList[i].ownerRuntimeClass, m_InputTemplateRuntimeTypeList, true);
                            classRTList.Add(crt);
                        }
                        var rt = RuntimeTypeManager.AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(runtimeCall.runtimeDefType.runtimeClass, classRTList);
                        if (rt == null)
                        {
                            rt = RuntimeTypeManager.AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(runtimeCall.runtimeDefType.runtimeClass, classRTList);
                        }

                        if (runtimeCall.method.id == "type")
                        {
                            var sobj = RuntimeTypeManager.CreateTypeObject(rt);
                            m_ValueStack[m_ValueIndex++].SetValueBySObject(sobj);
                        }
                        else
                        {
                            for (int i = 0; i < runtimeCall.templateRuntimeDefTypeList.Count; i++)
                            {
                                var crt = RuntimeTypeManager.GetRuntimeTypeByDefType(runtimeCall.templateRuntimeDefTypeList[i]);
                                classRTList.Add(crt);
                            }
                            CLRVM.RunIRMethod(classRTList, runtimeCall.method );
                        }
                    }
                    break;
                case EIROpCode.CallDynamic:
                    {
                        SLRuntimeCallPackage callPkg = null;
                        if (!iri.TryGetRuntimeCallPackage(out callPkg))
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "");
                            return;
                        }
                        RuntimeCall? mfc = SLRuntimeModuleRegistry.TryCreateRuntimeCallForInstruction(callPkg, iri.index);                        
                        if (mfc == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "鎵ц鍔ㄦ€佸嚱鏁帮紝娌℃湁鍙戠幇鐩稿叧鍑芥暟浣?");
                            return;
                        }

                        RuntimeType rt = null;
                        RuntimeClass irc = null;
                        if (iri.index > -1)
                        {
                            int stackIndex = m_ValueIndex - iri.index;
                            if (stackIndex < 0)
                            {
                                Log.AddRuntimeLog(LID.ShowMessageAssert, "StackIndex 鏄礋鏁?");
                                return;
                            }
                            var v = m_ValueStack[stackIndex];
                            if (v.sobject != null )
                            {
                                rt = v.sobject.runtimeType;
                                irc = rt.runtimeClass;
                            }
                            else
                            {
                                irc = RuntimeClassManager.GetRuntimeClassByName(v.eType.ToString());
                                if( irc != null )
                                {
                                    rt = RuntimeTypeManager.GetRuntimeTypeByRuntimeClass(irc);
                                }
                            }
                            if (irc == null)
                            {
                                Log.AddRuntimeLog(LID.RuntimeVMNotFoundRuntimeClass, "");
                                return;
                            }
                            if (mfc.method == null)
                            {
                                Log.AddRuntimeLog(LID.RuntimeVMNotFoundRuntimeMethod, "mfc.method");
                                return;
                            }
                            if (mfc.method.id == "type")
                            {
                                var sobj = RuntimeTypeManager.CreateTypeObject(rt);
                                this.m_ValueStack[m_ValueIndex++].SetValueBySObject(sobj);
                            }
                            else
                            {
                                // attribute hooks are handled in Front/Core; VM does不 reference Front.
                                List<RuntimeType> rtList = new List<RuntimeType>(rt.runtimeTemplateList);
                                for (int i = 0; i < mfc.templateRuntimeDefTypeList.Count; i++)
                                {
                                    var crt = GetRuntimeTypeByDefType(mfc.templateRuntimeDefTypeList[i], irc, rt.runtimeTemplateList, true);
                                    rtList.Add(crt);
                                }
                                if (mfc.method.interfaceMethod)
                                {
                                    var irmethod = irc.GetNonStaticMethodIndexByName(mfc.methodName, out int index);
                                    if (irmethod != null)
                                    {
                                        CLRVM.RunIRMethod(rtList, irmethod);
                                    }
                                    else
                                    {
                                        Log.AddRuntimeLog(LID.RuntimeVMNotFoundRuntimeMethod, $"interfaceMethod:{mfc.methodName}");
                                    }
                                }
                                else
                                {
                                    CLRVM.RunIRMethod(rtList, mfc.method);
                                }
                            }
                        }
                        else
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMNotFoundCurrentValue, "Dynamic function call from stack failed.", iri.index );
                        }
                    }
                    break;
                case EIROpCode.CallVirt:
                    {
                        SLRuntimeCallPackage? callPkg = null;
                        if (iri.TryGetRuntimeCallPackage(out var parsedCallPkg)) callPkg = parsedCallPkg;

                        RuntimeCall? runtimeCall = SLRuntimeModuleRegistry.TryCreateRuntimeCallForInstruction(callPkg, 0 );
                        if (runtimeCall == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "Virtual call failed: runtime call metadata not found.");
                            return;
                        }
                            // attribute hooks are handled in Front/Core; VM does不 reference Front。

                        int stackFrontIndex = (int)runtimeCall.paramCount + 1;
                        int stackIndex = m_ValueIndex - stackFrontIndex;
                        if (stackIndex < 0)
                        {
                            Log.AddProjectLog(LID.RuntimeVMStackIndexNotEnough, "Stack index is negative.", stackFrontIndex, m_ValueIndex );
                            return;
                        }
                        var v = m_ValueStack[stackIndex];

                        if (v.isNull)
                        {
                            Log.AddRuntimeLog(LID.RuntimeVMNotShouldIsNull, iri.debugInfo,  "Current stack value is null." );
                            return;
                        }

                        RuntimeType? rt = null;
                        RuntimeClass? irc = null;
                        if (v.eType == EVMType.Class
                            || v.eType == EVMType.Object
                            || v.eType == EVMType.Array )
                        {
                            irc = v.sobject.runtimeClass;
                            rt = v.sobject.runtimeType;
                        }
                        else
                        {
                            irc = RuntimeClassManager.GetRuntimeClassByName( "Core." + v.eType.ToString());
                            rt = RuntimeTypeManager.GetRuntimeTypeByRuntimeClass(irc);
                            if( rt == null )
                            {
                                rt = RuntimeTypeManager.AddRuntimeTypeByClass(irc);
                            }
                        }
                        if (irc == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageError, "Virtual call failed: runtime class is null.");
                            return;
                        }
                        RuntimeMethod cfc = irc.GetNonStaticMethodByIndex(iri.index);


                        if (cfc == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "Method index not found: " + iri.index);
                            return;
                        }
                        List<RuntimeType> rtList = new List<RuntimeType>(rt.runtimeTemplateList);
                        for (int i = 0; i < runtimeCall.templateRuntimeDefTypeList.Count; i++)
                        {
                            var crt = GetRuntimeTypeByDefType(runtimeCall.templateRuntimeDefTypeList[i], irc, rt.runtimeTemplateList, true);
                            rtList.Add(crt);
                        }
                        CLRVM.RunIRMethod(rtList, cfc);

                        var a = ObjectManager.classObjectDict;
                    }
                    break;
                case EIROpCode.Ret:
                    // stop execution early
                    m_ExecuteIndex = m_ExecuteCount;
                    break;

                case EIROpCode.CastClass:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Log.AddProjectLog(LID.RuntimeVMStackIndexNotEnough, "CastClass", 1, m_ValueIndex );
                            break;
                        }
                        var mt = TryGetInstructionRuntimeDefType(iri);
                        if (mt == null)
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "StoreStaticField failed to get runtime type for metadata type: ");
                            break;
                        }
                        var rt = GetRuntimeTypeByDefType(mt, mt.ownerRuntimeClass, m_InputTemplateRuntimeTypeList, true );
                        if( rt == null )
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, "CastClass failed to get runtime type for metadata type: " );
                            break;
                        }
                        if (rt.eType == EVMType.Object)
                        {
                            break;
                        }
                        ref var v1 = ref m_ValueStack[m_ValueIndex - 1];
                        if (v1.isNull)
                        {
                            break;
                        }

                        // For primitive/builtin types, "as" should behave as numeric/string conversion,
                        // not strict runtime-class identity check.
                        bool targetIsPrimitiveLike =
                            rt.eType == EVMType.Boolean
                            || rt.eType == EVMType.UInt8
                            || rt.eType == EVMType.Int8
                            || rt.eType == EVMType.Int16
                            || rt.eType == EVMType.UInt16
                            || rt.eType == EVMType.Int32
                            || rt.eType == EVMType.UInt32
                            || rt.eType == EVMType.Int64
                            || rt.eType == EVMType.UInt64
                            || rt.eType == EVMType.Float32
                            || rt.eType == EVMType.Float64
                            || rt.eType == EVMType.Num
                            || rt.eType == EVMType.String;
                        if (targetIsPrimitiveLike)
                        {
                            try
                            {
                                v1.ConvertByEType(rt.eType);
                            }
                            catch
                            {
                                v1.SetNull();
                            }                            
                            break;
                        }
                        else
                        {
                            if( v1.sobject == null )
                            {
                                Log.AddRuntimeLog(LID.ShowMessageAssert, "CastClass failed to get runtime type for metadata type: ");
                            }
                            else
                            {
                                if (!v1.sobject.runtimeType.IsExtendsRelation(rt))
                                {
                                    bool interfaceMatched = false;
                                    var targetClass = rt.runtimeClass;
                                    var sourceClass = v1.sobject.runtimeType?.runtimeClass;
                                    if (targetClass != null && targetClass.isInterfaceClass && sourceClass != null)
                                    {
                                        interfaceMatched = sourceClass.ImplementsInterface(targetClass);
                                    }

                                    if (!interfaceMatched)
                                    {
                                        m_ValueStack[m_ValueIndex - 1].SetNull();
                                    }
                                }
                            }
                        }   
                    }
                    break;
                default:
                    // unhandled op
                    Log.AddRuntimeLog(LID.ShowMessageAssert, "Function" + this.id + "IRData" + iri.id + "  " + iri.opCode);
                    break;
            }
        }

        private void ExecuteEqualityOperation(Instruction iri, bool equalCompare, bool isBranch)
        {
            if (!TryPopBranchOperands(out var left, out var right, iri, true))
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "Function" + this.id + "IRData" + iri.id + "  " + iri.opCode);
                return;
            }

            bool methodCall = false;
            SValue.CompareEuqalSValue1AndValue2(ref left, ref right, equalCompare, out methodCall);

            SValue result = left;
            if (methodCall)
            {
                if (m_ValueIndex == 0)
                {
                    Log.AddRuntimeLog(LID.ShowMessageAssert, "Function" + this.id + "IRData" + iri.id + "  " + iri.opCode + " equality operator has no return value");
                    result.SetBoolValue(false);
                }
                else
                {
                    result = m_ValueStack[--m_ValueIndex];
                }
            }

            bool isTrue = SValue.IsTruthy(ref result);
            result.SetBoolValue(isTrue);

            if (isBranch)
            {
                if (isTrue)
                {
                    m_ExecuteIndex = (ushort)(iri.index - 1);
                }
            }
            else
            {
                PushSValueSynced(result);
            }
        }

        private void ExecuteRelationalOperation(Instruction iri, int compareSign, bool isBranch)
        {
            if (!TryPopBranchOperands(out var left, out var right, iri, true))
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "Function" + this.id + "IRData" + iri.id + "  " + iri.opCode);
                return;
            }

            SValue.CompareSValue1AndValue2(ref left, ref right, compareSign);

            bool isTrue = SValue.IsTruthy(ref left);
            left.SetBoolValue(isTrue);

            if (isBranch)
            {
                if (isTrue)
                {
                    m_ExecuteIndex = (ushort)(iri.index - 1);
                }
            }
            else
            {
                PushSValueSynced(left);
            }
        }

        private bool TryPopBranchOperands(out SValue left, out SValue right, Instruction iri, bool logStackNotEnough)
        {
            left = default;
            right = default;
            if (m_ValueIndex < 2)
            {
                if (logStackNotEnough)
                {
                    Log.AddRuntimeLog(LID.RuntimeVMStackIndexNotEnough, iri?.opCode.ToString() ?? "Branch", 2, m_ValueIndex);
                }
                return false;
            }

            right = m_ValueStack[--m_ValueIndex];
            left = m_ValueStack[--m_ValueIndex];
            return true;
        }
        public void SetObjectByValue(int type, uint index, ref SValue svalue)
        {

            RuntimeObject[]? targetArray = type switch
            {
                0 => m_ArgumentRuntimeObjectArray,
                1 => m_LocalVariableRuntimeObjectArray,
                2 => m_ReturnRuntimeObjectArray,
                _ => null,
            };

            if (targetArray == null || index >= targetArray.Length)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, " runtime object is null for type " + type + " index " + index);
                return;
            }
            
            var robj = targetArray[index];
            if (robj == null)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, " runtime object is null for type " + type + " index " + index);
                return;
            }

            if (svalue.isNull)
            {
                robj.SetNull();
                return;
            }

            //var valueToSet = svalue;
            svalue.TryCoerceScalarForAssignment(robj.eType);

            bool targetUnsigned32OrLess = robj.eType == EVMType.UInt8
                || robj.eType == EVMType.UInt16
                || robj.eType == EVMType.UInt32;
            if (targetUnsigned32OrLess)
            {
                bool sourceIsNegativeSigned = (svalue.eType == EVMType.Int8 && svalue.int8Value < 0)
                    || (svalue.eType == EVMType.Int16 && svalue.int16Value < 0)
                    || (svalue.eType == EVMType.Int32 && svalue.int32Value < 0);
                if (sourceIsNegativeSigned)
                {
                    Log.AddRuntimeLog(LID.ShowMessageAssert,
                        $"不能将负值写入无符号类型: target={robj.eType}, source={svalue.eType}");
                    return;
                }
            }

            if( svalue.eType == EVMType.Object )
            {
                svalue.ConvertValueByTargetTypeAndObject(robj.eType);
            }

            robj.SetSObjectBySValue(ref svalue);
        }

    }
}
