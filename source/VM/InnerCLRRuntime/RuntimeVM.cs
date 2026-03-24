//****************************************************************************
//  File:      RuntimeVM.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection;
using SimpleLanuageVM.Load;
using SimpleLanguage.Parse;
using System.Globalization;

namespace SimpleLanguage.VM.Runtime
{
    public unsafe class RuntimeVM
    {
        public SObject[] returnObjectArray { get => m_ReturnObjectArray; }


        public SValue[] m_ValueStack;
        public IntPtr m_RawBuffer;
        public RawSValue* m_RawPtr;
        public int m_RawCapacity;
        public ushort m_ValueIndex;


        private List<RuntimeType> m_InputTemplateRuntimeTypeList;

        private SObject[] m_LocalVariableObjectArray;
        private SValue[] m_LocalValueArray;

        private SObject[] m_ArgumentObjectArray;
        private SValue[] m_ArgumentValueArray;

        private SObject[] m_ReturnObjectArray;

        private RuntimeMethod m_Method;
        private Instruction[] m_InstructionList;
        private ushort m_ExecuteIndex;
        private ushort m_ExecuteCount;
        private RuntimeClass m_CurrentRuntimeClass;
        public string id { get; set; }
        public int level { get; set; }
        public bool isPersistent { get; set; }

        public RuntimeVM(List<Instruction> irlist)
        {
            m_InstructionList = irlist?.ToArray();
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            m_RawCapacity = 1024;
            m_LocalValueArray = null;
            m_ArgumentValueArray = null;

            Init();
        }
        public RuntimeVM(List<RuntimeType> rtList, RuntimeMethod rm )
        {
            m_InputTemplateRuntimeTypeList = rtList ?? new List<RuntimeType>();
            m_Method = rm;
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            m_RawCapacity = 1024;
            m_InstructionList = rm.InstructionList.ToArray();
            if (m_Method != null)
            {
                m_ArgumentValueArray = new SValue[m_Method.methodArgumentList.Count];
                m_LocalValueArray = new SValue[m_Method.methodLocalVariableList.Count];
            }
            Init();
        }
        public RuntimeVM(List<RuntimeType> rtList, List<Instruction> irlist)
        {
            m_InputTemplateRuntimeTypeList = rtList ?? new List<RuntimeType>();
            m_InstructionList = irlist?.ToArray();
            m_ValueStack = new SValue[1024];
            m_ValueIndex = 0;
            m_RawCapacity = 1024;
            m_LocalValueArray = null;
            m_ArgumentValueArray = null;

            Init();
        }

        public void Init()
        {
            //参数列表 argument variable table
            if (m_Method != null)
            {
                m_ReturnObjectArray = new SObject[m_Method.methodReturnVariableList.Count];
                for (int i = 0; i < m_Method.methodReturnVariableList.Count; i++)
                {
                    RuntimeDefType imt = m_Method.methodReturnVariableList[i].runtimeDefType;
                    SObject sobj = imt != null
                        ? CreateObjectByIRMetaType(imt, imt.ownerRuntimeClass, true)
                        : new SObject(EVMType.Object);
                    m_ReturnObjectArray[i] = sobj;
                }

                m_ArgumentObjectArray = new SObject[m_Method.methodArgumentList.Count];
                for (int i = 0; i < m_Method.methodArgumentList.Count; i++)
                {
                    RuntimeDefType imt = m_Method.methodArgumentList[i].runtimeDefType;
                    SObject sobj = imt != null
                        ? CreateObjectByIRMetaType(imt, imt.ownerRuntimeClass, true)
                        : new SObject(EVMType.Object);
                    m_ArgumentObjectArray[i] = sobj;
                }
                for (int i = 0; i < m_ArgumentObjectArray.Length; i++)
                {
                    Log.AddVM(EError.None, "Argu_" + i.ToString() + "_Value: [" + m_ArgumentObjectArray[i].ToString() + "]");
                }

                //局部变量列表 local variable table
                m_LocalVariableObjectArray = new SObject[m_Method.methodLocalVariableList.Count];
                for (int i = 0; i < m_Method.methodLocalVariableList.Count; i++)
                {
                    var mev = m_Method.methodLocalVariableList[i];
                    RuntimeDefType imt = mev.runtimeDefType;
                    SObject sobj = imt != null
                        ? CreateObjectByIRMetaType(imt, m_Method.ownerMetaClass, true)
                        : new SObject(EVMType.Object);
                    m_LocalVariableObjectArray[i] = sobj;
                }
                for (int i = 0; i < m_LocalVariableObjectArray.Length; i++)
                {
                    Log.AddVM(EError.None, "Variable_" + i.ToString() + m_LocalVariableObjectArray[i].ToString());
                }
            }

            else
            {
                m_ReturnObjectArray = new SObject[0];
                m_ArgumentObjectArray = new SObject[0];
                m_LocalVariableObjectArray = new SObject[0];
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
        }
        public bool IsNumericTypeLocal(EVMType t)
        {
            return t == EVMType.Num || t == EVMType.Int32 || t == EVMType.Int64 || t == EVMType.Float32 || t == EVMType.Float64;
        }
        public void SyncRawFromSValue(int index)
        {
            // minimal noop for now
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncRawAtIndex(int index)
        {
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushSValueSynced(in SValue v)
        {
            if (m_ValueStack == null) m_ValueStack = new SValue[1024];
            if (m_ValueIndex >= m_ValueStack.Length) return;
            m_ValueStack[m_ValueIndex++] = v;
        }
        public SObject CreateObjectByIRMetaType(RuntimeDefType irmt, RuntimeClass curIrMc, bool isAdd = false)
        {
            if (irmt == null) return new SObject(EVMType.Object);
            var rt = RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(irmt);
            if (rt == null) return new SObject(EVMType.Object);
            return ObjectManager.CreateObjectByRuntimeType(rt, false);
        }
        public void AddReturnObjectArray(SObject[] sobjs)
        {
            //m_ReturnObjectArray = sobjs;

            for (int i = 0; i < sobjs.Length; i++)
            {
                if (sobjs[i].runtimeType != RuntimeTypeManager.voidRuntimeType)
                {
                    //GetObjectByValue(4, i, sobjs, ref m_ValueStack[m_ValueIndex++] );
                    var obj = sobjs[i];
                    Debug.Assert(obj != null);
                    if (obj.isNull)
                    {
                        m_ValueStack[m_ValueIndex++].SetNull();
                        return;
                    }
                    SetSValue(obj, obj.eType, ref m_ValueStack[m_ValueIndex]);
                    m_ValueIndex++;
                }
            }
        }
        public void GetArgumentValue(int index, ref SValue svalue)
        {
            if (index > m_ArgumentObjectArray.Length)
            {
                Log.AddVM(EError.None, $"SVM Error FunctionName:{this.id} 执行的参数超出范围!!");
                return;
            }
            GetObjectByValue(0, index, ref svalue);
        }
        public void SetArgumentValue(int index, SValue svalue)
        {
            if (index > m_ArgumentObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }
            SetObjectByValue(0, index, ref svalue);
        }
        public void SetLocalVariableSValue(int index, SValue svalue)
        {
            if (index > m_LocalVariableObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的栈超出范围!!");
                return;
            }
            SetObjectByValue(1, index, ref svalue);
        }
        public void GetLocalVariableSValue(int index, ref SValue svalue)
        {

            if (index > m_LocalVariableObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的栈超出范围!!");
                return;
            }
            GetObjectByValue(1, index, ref svalue);
        }
        public void SetReturnVariableSValue(int index, SValue svalue)
        {
            if (index > m_ReturnObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的栈超出范围!!");
                return;
            }
            SetObjectByValue(2, index, ref svalue);
            /*
            if (m_ReturnObjectArray == null)
            {
                if (m_IRMethod != null)
                {
                    m_ReturnObjectArray = new SObject[m_IRMethod.methodReturnVariableList.Count];
                }
            }
            if (m_ReturnObjectArray != null && index >= 0 && index < m_ReturnObjectArray.Length)
            {
                m_ReturnObjectArray[index] = svalue.CreateSObject();
            }
            */
        }
        public SValue GetCurrentIndexValue(int index)
        {
            return m_ValueStack[index];
        }
        public static RuntimeType GetClassRuntimeType(RuntimeDefType irmt, RuntimeClass curIRMc, List<RuntimeType> __rtList, bool isAdd = false)
        {
            if (irmt.templateIndex != -1)
            {
                if (irmt.ownerRuntimeClass == curIRMc || curIRMc.name == "Object")
                {
                    return __rtList[irmt.templateIndex];
                }
                else
                {
                    var mt = curIRMc.GetRuntimeDefTypeByTemplateAndClassRelation(irmt.ownerRuntimeClass, irmt.templateIndex);

                    return GetClassRuntimeType(mt, curIRMc, __rtList, isAdd);
                }
            }
            else
            {
                List<RuntimeType> rtList = new List<RuntimeType>();
                if (irmt.runtimeDefTypeList.Count > 0)
                {
                    for (int i = 0; i < irmt.runtimeDefTypeList.Count; i++)
                    {
                        var crt = GetClassRuntimeType(irmt.runtimeDefTypeList[i], curIRMc, __rtList, isAdd);
                        rtList.Add(crt);
                    }
                }
                RuntimeType rt = RuntimeTypeManager.GetRuntimeTypeByMTAndTemplateMT(irmt.runtimeClass, rtList);
                if (rt == null && isAdd)
                {
                    rt = RuntimeTypeManager.AddRuntimeTypeByClassAndTemplate(irmt.runtimeClass, rtList);
                }
                return rt;
            }
        }
        public RuntimeType GetMethodRuntimeType( RuntimeDefType irmt)
        {
            return RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(irmt);
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
            Log.AddVM(EError.None, pushChar + "[VMRuntime] [Push] Method: [" + funName + "]");
            level++;

            var topClrRuntime = CLRVM.topCLRRuntime;
            for (int i = 0; i < m_ArgumentObjectArray.Length; i++)
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
                SetArgumentValue(m_ArgumentObjectArray.Length - i - 1, sval);
            }

            m_ExecuteIndex = 0;
            m_ExecuteCount = (ushort)m_InstructionList.Length;
            while (m_ExecuteIndex < m_ExecuteCount)
            {
                var iri = m_InstructionList[m_ExecuteIndex];
                RunInstruction(iri);
                m_ExecuteIndex++;
            }


            level--;
            pushChar = "";
            for (int i = 0; i < level; i++)
            {
                pushChar = '\t' + pushChar;
            }
            Log.AddVM(EError.None, pushChar + "[VMRuntime] [Pop] Method: [" + funName + "]");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string MakeIndent(int count) { return new string(' ', count); }
        public static void SetValue(ref SValue sValue, ref SValue sStore, Instruction iri)
        {
            sStore = sValue;
        }

        private static object? ConvertInvokeArg(object? source, Type targetType)
        {
            if (source == null) return null;

            // BridgeObject 参数落地（legacy bridge 路径也需要）
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
            payloadObj = sv.GetValueObject();
            return true;
        }

        private static object? NormalizeLegacyBridgeArg(ref SValue sv)
        {
            var raw = sv.GetValueObject();
            // Use object-target conversion so BridgeObject payload is extracted to CLR-friendly value.
            return ConvertInvokeArg(raw, typeof(object));
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

        private bool TryInvokeRegisteredBridgeByIndex(Instruction iri)
        {
            int bridgeIndex;
            if (iri.TryGetInt32(out var payloadIndex)) bridgeIndex = payloadIndex;
            else bridgeIndex = iri.index;

            if (!CSharpBridgeRegistry.TryResolve(bridgeIndex, out var model)) return false;
            if (!CSharpBridgeRegistry.TryBindMethod(model, out var methodInfo))
            {
                Debug.Assert(false, $"Bridge method bind failed, index={bridgeIndex}");
                return true;
            }

            var pars = methodInfo.GetParameters();
            if (m_ValueIndex < pars.Length)
            {
                Debug.Assert(false, $"Bridge stack underflow, need={pars.Length}, has={m_ValueIndex}");
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
                Debug.Assert(false, "Bridge instance methods are not supported");
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

        private bool TryInvokeLegacyBridgeSignature(Instruction iri, string callName)
        {
            int paramCountLocal = iri.index;
            if (paramCountLocal <= 0) return false;

            var values = new SValue[paramCountLocal];
            for (int i = paramCountLocal - 1; i >= 0; i--)
            {
                if (m_ValueIndex == 0)
                {
                    Debug.Assert(false, $"{callName} stack underflow");
                    return true;
                }
                values[i] = m_ValueStack[--m_ValueIndex];
            }

            string namespaceName = values.Length > 1 ? values[0].GetValueObject()?.ToString() ?? string.Empty : string.Empty;
            string className = values.Length > 2 ? values[1].GetValueObject()?.ToString() ?? string.Empty : string.Empty;
            string methodName = values.Length > 3 ? values[2].GetValueObject()?.ToString() ?? string.Empty : string.Empty;

            object[] argsClr = Array.Empty<object>();
            if (values.Length == 5)
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
                    var single = values[5];
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
                    Debug.Assert(false, $"{callName}: type not found " + typeFull);
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
                        Debug.Assert(false, $"{callName}: instance methods not supported in bridge");
                        return true;
                    }
                    var ret2 = miFound.Invoke(null, bestInvokeArgs);
                    if (miFound.ReturnType != typeof(void))
                    {
                        var sv2 = SValue.FromClrObject(ret2);
                        PushSValueSynced(sv2);
                    }
                    return true;
                }
            }

            if (miFound == null)
            {
                Debug.Assert(false, $"{callName}: method not found " + methodName);
                return true;
            }

            if (!TryBuildInvokeArgsForMethod(miFound, argsClr, out var invokeArgs, out _))
            {
                Debug.Assert(false, $"{callName}: method signature mismatch " + methodName);
                return true;
            }

            if (!miFound.IsStatic)
            {
                Debug.Assert(false, $"{callName}: instance methods not supported in bridge");
                return true;
            }

            var ret = miFound.Invoke(null, invokeArgs);
            if (miFound.ReturnType != typeof(void))
            {
                var sv = SValue.FromClrObject(ret);
                PushSValueSynced(sv);
            }

            return true;
        }

        private bool TryInvokeSystemCallByName(Instruction iri)
        {
            string callName = string.Empty;
            if (!iri.TryGetString(out callName) || string.IsNullOrWhiteSpace(callName))
            {
                callName = iri.opValue?.ToString() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(callName))
            {
                Debug.Assert(false, "CallSystemMethod missing function name");
                return false;
            }

            // Each system function can have unique VM logic.
            // Keep existing bridge handlers as the default implementation.
            switch (callName)
            {
                case "CallCLRMethod":
                    if (TryInvokeRegisteredBridgeByIndex(iri)) return true;
                    return TryInvokeLegacyBridgeSignature(iri, callName);
                case "CallNativeMethod":
                    if (TryInvokeRegisteredBridgeByIndex(iri)) return true;
                    return TryInvokeLegacyBridgeSignature(iri, callName);
                case "CallJVMMethod":
                    if (TryInvokeRegisteredBridgeByIndex(iri)) return true;
                    return TryInvokeLegacyBridgeSignature(iri, callName);
                default:
                    Debug.Assert(false, "Unknown system function: " + callName);
                    return false;
            }
        }

        public void RunInstruction(Instruction iri)
        {
            if (iri == null) return;
            switch (iri.opCode)
            {
                case EIROpCode.Nop: break;
                case EIROpCode.LoadConstNull:
                    {
                        m_ValueStack[m_ValueIndex++].SetNull();
                    }
                    break;
                case EIROpCode.LoadConstBoolean:
                    {
                        if (iri.TryGetBoolean(out bool b)) { var v = default(SValue); v.SetBoolValue(b); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstByte:
                    {
                        if (iri.TryGetByte(out byte cb)) { var v = default(SValue); v.SetInt8Value(cb); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstSByte:
                    {
                        if (iri.TryGetSByte(out sbyte sb)) { var v = default(SValue); v.SetSInt8Value(sb); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstInt16:
                    {
                        if (iri.TryGetInt16(out short sv)) { var v = default(SValue); v.SetInt16Value(sv); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstUInt16:
                    {
                        if (iri.TryGetUInt16(out ushort usv)) { var v = default(SValue); v.SetUInt16Value(usv); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstInt32:
                    {
                        if (iri.TryGetInt32(out int i32)) { var v = default(SValue); v.SetInt32Value(i32); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstUInt32:
                    {
                        if (iri.TryGetUInt32(out uint ui32)) { var v = default(SValue); v.SetUInt32Value(ui32); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstInt64:
                    {
                        if (iri.TryGetInt64(out long l)) { var v = default(SValue); v.SetInt64Value(l); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstUInt64:
                    {
                        if (iri.TryGetUInt64(out ulong ul)) { var v = default(SValue); v.SetUInt64Value(ul); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstFloat32 :
                    {
                        if (iri.TryGetSingle(out float f)) { var v = default(SValue); v.SetFloatValue(f); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstFloat64:
                    {
                        if (iri.TryGetDouble(out double d)) { var v = default(SValue); v.SetDoubleValue(d); PushSValueSynced(v); }
                    }
                    break;
                case EIROpCode.LoadConstString:
                    {
                        if (iri.TryGetString(out string s))
                        {
                            var v = default(SValue);
                            v.SetStringValue(s);
                            PushSValueSynced(v);
                        }
                        else
                        {
                            var resolved = SLAssembly.TryGetConstString(iri.index)
                                ?? SLIRModuleLoader.TryGetConstString(iri.index)
                                ?? string.Empty;
                            var v = default(SValue);
                            v.SetStringValue(resolved);
                            PushSValueSynced(v);
                        }
                    }
                    break;
                case EIROpCode.LoadConstType:
                    {

                    }
                    break;
                case EIROpCode.Convert_I8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Byte);
                    }
                    break;
                case EIROpCode.Convert_SI8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.SByte);
                    }
                    break;
                case EIROpCode.Convert_I16:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Int16);
                    }
                    break;
                case EIROpCode.Convert_UI16:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.UInt16);
                    }
                    break;
                case EIROpCode.Convert_I32:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Int32);
                    }
                    break;
                case EIROpCode.Convert_UI32:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.UInt32);
                    }
                    break;
                case EIROpCode.Convert_I64:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Int64);
                    }
                    break;
                case EIROpCode.Convert_UI64:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.UInt64);
                    }
                    break;
                case EIROpCode.Convert_R4:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Float32);
                    }
                    break;
                case EIROpCode.Convert_R8:
                    {
                        m_ValueStack[m_ValueIndex - 1].ConvertByEType(EVMType.Float64);
                    }
                    break;
                //case EIROpCode.LoadArgument:
                //    {
                //        GetArgumentValue(iri.index, ref m_ValueStack[m_ValueIndex++]);
                //    }
                //    break;
                case EIROpCode.LoadArgument:
                    {
                        var data = iri.index;
                        var v = default(SValue);
                        GetArgumentValue(data, ref v);
                        PushSValueSynced(v);
                    }
                    break;
                case EIROpCode.LoadLocal:
                    {
                        var data = iri.index;
                        var v = default(SValue);
                        GetLocalVariableSValue(data, ref v);
                        PushSValueSynced(v);
                    }
                    break;
                case EIROpCode.LoadGlobal:
                    {
                        var data = iri.index;
                        var v = default(SValue);
                        CLRVM.LoadGlobalVariable(data, ref v);
                        PushSValueSynced(v);
                    }
                    break;
                case EIROpCode.StoreLocal:
                    {
                        var idx = iri.index;
                        if (m_ValueIndex > 0)
                        {
                            var val = m_ValueStack[--m_ValueIndex];
                            SetLocalVariableSValue(idx, val);
                        }
                    }
                    break;
                case EIROpCode.StoreReturn:
                    {
                        SetReturnVariableSValue(iri.index, m_ValueStack[--m_ValueIndex]);
                        m_ExecuteIndex = m_ExecuteCount;
                    }
                    break;
                case EIROpCode.StoreGlobal:
                    {
                        var id = iri.index;
                        if (m_ValueIndex > 0)
                        {
                            var val = m_ValueStack[--m_ValueIndex];
                            CLRVM.StoreGlobalVariable(id, ref val);
                        }
                    }
                    break;

                case EIROpCode.LoadArrayIndex:
                    {
                        var v = m_ValueStack[m_ValueIndex - 1];
                        if (v.eType == EVMType.Array)
                        {
                            (v.sobject as ArrayObject).LoadValue(iri.index, ref m_ValueStack[m_ValueIndex - 1]);
                        }
                        else
                        {
                            Log.AddVM(EError.None, "不是数组类型!!");
                        }
                    }
                    break;
                case EIROpCode.StoreArrayIndex:
                    {
                        int int1 = 1, int2 = 2;
                        if (iri.opValue is Boolean flag)
                        {
                            if (flag)
                            {
                                int1 = 2;
                                int2 = 1;
                            }
                        }
                        SValue sStore = m_ValueStack[m_ValueIndex - int1];
                        SValue sValue = m_ValueStack[m_ValueIndex - int2];

                        if (sStore.eType == EVMType.Array)
                        {
                            (sStore.sobject as ArrayObject).StoreValue(iri.index, sValue);
                        }
                        else
                        {
                            Debug.Assert(false, "不是数组类型!!");
                            Log.AddVM(EError.None, "不是数组类型!!");
                        }
                        m_ValueIndex -= 2;
                    }
                    break;
                case EIROpCode.LoadArrayIndexField:
                    {
                        SValue arrayref = m_ValueStack[m_ValueIndex - 2];
                        SValue loadindex = m_ValueStack[m_ValueIndex - 1];

                        if (arrayref.eType == EVMType.Array)
                        {
                            int index = (int)loadindex.GetValueObject();
                            (arrayref.sobject as ArrayObject).LoadValue(index, ref m_ValueStack[m_ValueIndex - 2]);
                        }
                        else
                        {
                            Debug.Assert(false, "不是数组类型!!");
                            Log.AddVM(EError.None, "不是数组类型!!");
                        }
                        m_ValueIndex -= 1;
                    }
                    break;
                case EIROpCode.StoreArrayIndexField:
                    {
                        SValue arrayref = m_ValueStack[m_ValueIndex - 3];
                        SValue loadindex = m_ValueStack[m_ValueIndex - 2];
                        SValue storevalue = m_ValueStack[m_ValueIndex - 1];

                        if (arrayref.eType == EVMType.Array)
                        {
                            int index = (int)loadindex.GetValueObject();
                            (arrayref.sobject as ArrayObject).StoreValue(index, storevalue);
                        }
                        else
                        {
                            Debug.Assert(false, "不是数组类型!!");
                            Log.AddVM(EError.None, "不是数组类型!!");
                        }
                        m_ValueIndex -= 3;
                    }
                    break;
                case EIROpCode.Dup:
                    if (m_ValueIndex > 0)
                    {
                        var v = m_ValueStack[m_ValueIndex - 1];
                        PushSValueSynced(v);
                    }
                    break;
                case EIROpCode.Pop:
                    if (m_ValueIndex > 0) m_ValueIndex--;
                    break;
                case EIROpCode.LoadNotStaticField:
                    {
                        // expects instance on stack
                        if (m_ValueIndex > 0)
                        {
                            var inst = m_ValueStack[--m_ValueIndex];
                            if (inst.eType == EVMType.Array || inst.eType == EVMType.Class || inst.eType == EVMType.Type || inst.eType == EVMType.Object)
                            {
                                var v = default(SValue);
                                if (inst.sobject is ClassObject co)
                                {
                                    co.GetMemberVariableSValue(iri.index, ref v);
                                    PushSValueSynced(v);
                                }
                                else
                                {
                                    PushSValueSynced(v);
                                }
                            }

                        }
                    }
                    break;
                case EIROpCode.StoreNotStaticField2:
                    {
                        // expect value then instance on stack (value pushed last)
                        if (m_ValueIndex >= 2)
                        {
                            var val = m_ValueStack[--m_ValueIndex];
                            var inst = m_ValueStack[--m_ValueIndex];
                            if (inst.eType == EVMType.Class || inst.eType == EVMType.Object)
                            {
                                if (inst.sobject is ClassObject co)
                                {
                                    co.SetMemberVariableSValue(iri.index, val);
                                }
                            }
                        }
                    }
                    break;
                case EIROpCode.ClassInit:
                    {
                        var mdt = TryGetInstructionRuntimeDefType(iri);
                        if (mdt != null)
                        {
                            var rt = GetClassRuntimeType(mdt, m_CurrentRuntimeClass != null ? m_CurrentRuntimeClass : mdt.ownerRuntimeClass, m_InputTemplateRuntimeTypeList, true);
                        }
                    }
                    break;
                case EIROpCode.NewObject:
                    {
                        if( iri.TryGetInt32(out int i32) )
                        {
                            var rt = RuntimeTypeManager.GetRuntimeTypeByClassId(i32);
                            // If runtime type not yet created, try to find corresponding RuntimeClass
                            // (possibly registered from package metadata) and dynamically register a RuntimeType for it.
                            if (rt == null)
                            {
                                // first try existing runtime class
                                var rc = RuntimeClassManager.instance.GetRuntimeClassById(i32);
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
                            if (sobj is ClassObject co)
                            {
                                ObjectManager.AddClassObject(co);
                            }
                            m_ValueStack[m_ValueIndex++].SetSObject(sobj);

                            var irList = rt.runtimeClass.nonStaticMemberVariableSetValueList;
                            if (irList.Count > 0)
                            {
                                CLRVM.RunIRNewMethod(rt.runtimeTemplateList, irList);
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
                            var rt = GetClassRuntimeType(mdt, m_CurrentRuntimeClass != null ? m_CurrentRuntimeClass : mdt.ownerRuntimeClass, m_InputTemplateRuntimeTypeList, true);
                            SObject sobj = ObjectManager.CreateObjectByRuntimeType(rt, true);
                            if (sobj is ClassObject co)
                            {
                                ObjectManager.AddClassObject(co);
                            }
                            m_ValueStack[m_ValueIndex++].SetSObject(sobj);
                            var irc = rt.runtimeClass;


                            var irList = rt.runtimeClass.nonStaticMemberVariableSetValueList;
                            if (irList.Count > 0)
                            {
                                CLRVM.RunIRNewMethod(rt.runtimeTemplateList, irList);
                            }
                            var sv = default(SValue);
                            sv.SetSObject(sobj);
                            PushSValueSynced(sv);
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
                            if (sval.eType != EVMType.Int32)
                            {
                                Log.AddVM(EError.None, "创建数组长度不是Int32类型!!");
                                break;
                            }

                            var rt = GetClassRuntimeType(rdt, m_CurrentRuntimeClass != null ? m_CurrentRuntimeClass : rdt.ownerRuntimeClass, m_InputTemplateRuntimeTypeList, true);
                            ArrayObject arr = new ArrayObject(rt, sval.int32Value);
                            // NewArray opcode path should initialize only the backing storage.
                            // Full CreateObject() may require runtime member types that are not guaranteed ready.
                            arr.EnsureArrayStorageInitialized();
                            ObjectManager.AddClassObject(arr);
                            m_ValueStack[m_ValueIndex - 1].SetSObject(arr);

                            //var sv = default(SValue);
                            //sv.SetSObject(arr);
                            //PushSValueSynced(sv);
                        }
                    }
                    break;
                case EIROpCode.Br:
                case EIROpCode.BrLabel:
                    {
                        m_ExecuteIndex = (ushort)iri.index;
                    }
                    break;
                case EIROpCode.Label: break;
                case EIROpCode.BrFalse:
                    {
                        if (m_ValueIndex > 0)
                        {
                            var cond = m_ValueStack[--m_ValueIndex];
                            bool isTrue = cond.eType == EVMType.Boolean ? cond.int8Value == 1 : cond.GetValueObject() != null;
                            if (!isTrue)
                            {
                                m_ExecuteIndex = (ushort)(iri.index - 1);
                            }
                        }
                    }
                    break;
                case EIROpCode.BrTrue:
                    {
                        if (m_ValueIndex > 0)
                        {
                            var cond = m_ValueStack[--m_ValueIndex];
                            bool isTrue = cond.eType == EVMType.Boolean ? cond.int8Value == 1 : cond.GetValueObject() != null;
                            if (isTrue)
                            {
                                m_ExecuteIndex = (ushort)(iri.index - 1);
                            }
                        }
                    }
                    break;
                case EIROpCode.Switch:
                    {
                        if (m_ValueIndex >= 2)
                        {
                            var right = m_ValueStack[--m_ValueIndex];
                            var left = m_ValueStack[m_ValueIndex];
                            //bool methodCall = false;
                            //SValue.CompareEuqalSValue1AndValue2(ref left, ref right, true, out methodCall);
                            //PushSValueSynced(left);
                            if (left.eType == EVMType.Int32 && right.eType == EVMType.Int32)
                            {
                                int switchValue = left.int32Value;
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
                                }
                            }
                        }
                    }
                    break;
                case EIROpCode.Ceq:
                    {
                        if (m_ValueIndex >= 2)
                        {
                            var right = m_ValueStack[--m_ValueIndex];
                            var left = m_ValueStack[--m_ValueIndex];
                            bool methodCall = false;
                            SValue.CompareEuqalSValue1AndValue2(ref left, ref right, true, out methodCall);
                            PushSValueSynced(left);
                        }
                    }
                    break;
                case EIROpCode.Cne:
                    {
                        if (m_ValueIndex >= 2)
                        {
                            var right = m_ValueStack[--m_ValueIndex];
                            var left = m_ValueStack[--m_ValueIndex];
                            bool methodCall = false;
                            SValue.CompareEuqalSValue1AndValue2(ref left, ref right, false, out methodCall);
                            PushSValueSynced(left);
                        }
                    }
                    break;
                case EIROpCode.Clt:
                    {
                        if (m_ValueIndex >= 2)
                        {
                            var right = m_ValueStack[--m_ValueIndex];
                            var left = m_ValueStack[--m_ValueIndex];
                            // compareSign 2 -> <
                            SValue.CompareSValue1AndValue2(ref left, ref right, 2);
                            PushSValueSynced(left);
                        }
                    }
                    break;
                case EIROpCode.Cle:
                    {
                        if (m_ValueIndex >= 2)
                        {
                            var right = m_ValueStack[--m_ValueIndex];
                            var left = m_ValueStack[--m_ValueIndex];
                            // compareSign 3 -> <=
                            SValue.CompareSValue1AndValue2(ref left, ref right, 3);
                            PushSValueSynced(left);
                        }
                    }
                    break;
                case EIROpCode.Neg:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Log.AddVM(EError.None, "Error Neg运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex].NegSValue(false);
                    }
                    break;
                case EIROpCode.Not:
                    {
                        if (m_ValueIndex - 1 < 0)
                        {
                            Log.AddVM(EError.None, "Error Not运算!!超出的栈范围");
                            break;
                        }
                        m_ValueStack[m_ValueIndex].NotSValue();
                    }
                    break;
                case EIROpCode.Add:
                case EIROpCode.Minus:
                case EIROpCode.Multiply:
                case EIROpCode.Divide:
                case EIROpCode.Modulo:
                    {
                        if (m_ValueIndex >= 2)
                        {
                            var right = m_ValueStack[--m_ValueIndex];
                            var left = m_ValueStack[--m_ValueIndex];
                            int sign = 0;
                            bool isUn = false;
                            switch (iri.opCode)
                            {
                                case EIROpCode.Add: sign = 0; break;
                                case EIROpCode.Minus: sign = 1; break;
                                case EIROpCode.Multiply: sign = 2; break;
                                case EIROpCode.Divide: sign = 3; break;
                                case EIROpCode.Modulo: sign = 4; break;
                            }
                            SValue.ComputeValueInline(ref left, sign, ref right, isUn);
                            PushSValueSynced(left);
                        }
                    }
                    break;
                case EIROpCode.CallCLRMethod:
                    {
                        if (!TryInvokeRegisteredBridgeByIndex(iri))
                        {
                            TryInvokeLegacyBridgeSignature(iri, "CallCLRMethod");
                        }
                    }
                    break;
                case EIROpCode.CallNativeMethod:
                    {
                        if (iri.opValue is RuntimeNativeCall irnf)
                        {
                            irnf.InvokeNativeMethod(this);
                        }
                        else if (!TryInvokeRegisteredBridgeByIndex(iri))
                        {
                            TryInvokeLegacyBridgeSignature(iri, "CallNativeMethod");
                        }
                    }
                    break;
                case EIROpCode.CallJVMMethod:
                    {
                        if (!TryInvokeRegisteredBridgeByIndex(iri))
                        {
                            if (!TryInvokeLegacyBridgeSignature(iri, "CallJVMMethod"))
                            {
                                Debug.Assert(false, "CallJVMMethod is not configured");
                            }
                        }
                    }
                    break;
                case EIROpCode.CallSystemMethod:
                    {
                        TryInvokeSystemCallByName(iri);
                    }
                    break;
                case EIROpCode.CallStatic:
                    {
                        // try to create runtime call on demand from instruction payload
                        SLRuntimeCallPackage callPkg = null;
                        if (iri.TryGetRuntimeCallPackage(out var parsedCallPkg)) callPkg = parsedCallPkg;

                        object legacyOpValue = iri.opValue;
                        if (legacyOpValue == null && iri.TryGetString(out var methodIdFromPayload) && !string.IsNullOrWhiteSpace(methodIdFromPayload))
                        {
                            legacyOpValue = methodIdFromPayload;
                        }
                        if (legacyOpValue == null && callPkg != null && !string.IsNullOrWhiteSpace(callPkg.methodId))
                        {
                            legacyOpValue = callPkg.methodId;
                        }

                        RuntimeCall runtimeCall = SLRuntimeModuleRegistry.TryCreateRuntimeCallForInstruction(callPkg, iri.index);
                        // attribute hooks are handled in Front/Core; VM does not reference Front.

                        if (runtimeCall == null)
                        {
                            Debug.Assert(false, "执行静态函数，没有发现相关函数体!");
                            return;
                        }

                        List<RuntimeType> classRTList = new List<RuntimeType>();
                        for (int i = 0; i < runtimeCall.runtimeDefType.runtimeDefTypeList.Count; i++)
                        {
                            var crt = GetClassRuntimeType(runtimeCall.runtimeDefType.runtimeDefTypeList[i], runtimeCall.runtimeDefType.runtimeDefTypeList[i].ownerRuntimeClass, m_InputTemplateRuntimeTypeList, true);
                            classRTList.Add(crt);
                        }
                        var rt = RuntimeTypeManager.GetRuntimeTypeByMTAndTemplateMT(runtimeCall.runtimeDefType.runtimeClass, classRTList);
                        if (rt == null)
                        {
                            rt = RuntimeTypeManager.AddRuntimeTypeByClassAndTemplate(runtimeCall.runtimeDefType.runtimeClass, classRTList);
                        }

                        if (runtimeCall.method.id == "type")
                        {
                            var sobj = RuntimeTypeManager.CreateTypeObject(rt);
                            m_ValueStack[m_ValueIndex++].SetSObject(sobj);
                        }
                        else
                        {
                            for (int i = 0; i < runtimeCall.templateRuntimeDefTypeList.Count; i++)
                            {
                                var crt = GetMethodRuntimeType(runtimeCall.templateRuntimeDefTypeList[i]);
                                classRTList.Add(crt);
                            }
                            CLRVM.RunIRMethod(classRTList, runtimeCall.method );
                        }
                    }
                    break;
                case EIROpCode.CallDynamic:
                    {
                        var mfc = iri.opValue as RuntimeCall;
                        if (mfc == null)
                        {
                            SLRuntimeCallPackage callPkg = null;
                            if (iri.TryGetRuntimeCallPackage(out var parsedCallPkg)) callPkg = parsedCallPkg;

                            object legacyOpValue = iri.opValue;
                            if (legacyOpValue == null && iri.TryGetString(out var methodIdFromPayload) && !string.IsNullOrWhiteSpace(methodIdFromPayload))
                            {
                                legacyOpValue = methodIdFromPayload;
                            }
                            if (legacyOpValue == null && callPkg != null && !string.IsNullOrWhiteSpace(callPkg.methodId))
                            {
                                legacyOpValue = callPkg.methodId;
                            }

                            var runtimeCall = SLRuntimeModuleRegistry.TryCreateRuntimeCallForInstruction(callPkg, iri.index);
                            if (runtimeCall != null)
                            {
                                iri.opValue = runtimeCall;
                                mfc = runtimeCall;
                            }
                        }
                        if (mfc == null)
                        {
                            Debug.Assert(false, "执行动态函数，没有发现相关函数体!");
                            return;
                        }

                        RuntimeType rt = null;
                        RuntimeClass irc = null;
                        if (iri.index > -1)
                        {
                            int stackIndex = m_ValueIndex - iri.index;
                            if (stackIndex < 0)
                            {
                                Log.AddVM(EError.None, "StackIndex 是负数!");
                                return;
                            }
                            var v = m_ValueStack[stackIndex];
                            if (v.eType == EVMType.Class
                                || v.eType == EVMType.Array)
                            {
                                var co = (v.sobject as ClassObject);
                                irc = co.runtimeClass;
                                rt = v.sobject.runtimeType;
                            }
                            else
                            {
                                irc = RuntimeClassManager.instance.GetRuntimeClassByName(v.eType.ToString());
                                rt = RuntimeTypeManager.GetRuntimeTypeByMT(irc);
                            }
                            if (irc == null)
                            {
                                Log.AddVM(EError.None, "IRC是调用虚函数为空!!");
                                return;
                            }
                            if (mfc.method == null)
                            {
                                Debug.Assert(false, "没有找到合适的调用方式");
                                return;
                            }
                            if (mfc.method.id == "type")
                            {
                                var sobj = RuntimeTypeManager.CreateTypeObject(rt);
                                m_ValueStack[m_ValueIndex++].SetSObject(sobj);
                            }
                            else
                            {
                                // attribute hooks are handled in Front/Core; VM does not reference Front.
                                List<RuntimeType> rtList = new List<RuntimeType>(rt.runtimeTemplateList);
                                for (int i = 0; i < mfc.templateRuntimeDefTypeList.Count; i++)
                                {
                                    var crt = GetClassRuntimeType(mfc.templateRuntimeDefTypeList[i], irc, rt.runtimeTemplateList, true);
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
                                        Debug.Assert(false, "没有找到合适的调用方式");
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
                            Log.AddVM(EError.None, "调用栈上动态函数");
                        }
                    }
                    break;
                case EIROpCode.CallVirt:
                    {
                        SLRuntimeCallPackage callPkg = null;
                        if (iri.TryGetRuntimeCallPackage(out var parsedCallPkg)) callPkg = parsedCallPkg;

                        RuntimeCall runtimeCall = SLRuntimeModuleRegistry.TryCreateRuntimeCallForInstruction(callPkg, 0 );
                        if (runtimeCall == null)
                        {
                            Debug.Assert(false, "执行虚函数，没有发现相关函数体!");
                            return;
                        }
                        // attribute hooks are handled in Front/Core; VM does not reference Front.

                        int stackFrontIndex = (int)runtimeCall.paramCount + 1;
                        int stackIndex = m_ValueIndex - stackFrontIndex;
                        if (stackIndex < 0)
                        {
                            Log.AddVM(EError.None, "StackIndex 是负数!");
                            return;
                        }
                        var v = m_ValueStack[stackIndex];

                        if (v.isNull)
                        {
                            Debug.Assert(false, "当前值为空!!");
                            return;
                        }

                        RuntimeType rt = null;
                        RuntimeClass irc = null;
                        if (v.eType == EVMType.Class || v.eType == EVMType.Array)
                        {
                            var co = (v.sobject as ClassObject);
                            irc = co.runtimeClass;
                            rt = co.runtimeType;
                        }
                        else if (v.eType == EVMType.Object)
                        {
                            SObject co = (v.sobject) as SObject;
                            m_ValueStack[stackIndex].SetValue(co);
                            var nco = m_ValueStack[stackIndex].GetSObject();
                            Debug.Assert(nco != null);
                            irc = nco.runtimeClass;
                            rt = nco.runtimeType;
                        }
                        //else if( v.eType == EVMType.Array )
                        //{
                        //    irc = IRManager.instance.GetIRMetaClassByName("Array");
                        //    rt = RuntimeTypeManager.GetRuntimeTypeByMTAndIRMetaClass(irc);
                        //}
                        else
                        {
                            irc = RuntimeClassManager.instance.GetRuntimeClassByName( "Core." + v.eType.ToString());
                            rt = RuntimeTypeManager.GetRuntimeTypeByMTAndIRMetaClass(irc);
                        }
                        if (irc == null)
                        {
                            Log.AddVM(EError.None, "IRC是调用虚函数为空!!");
                            return;
                        }
                        RuntimeMethod cfc = irc.GetNonStaticMethodByIndex(iri.index);


                        if (cfc == null)
                        {
                            Log.AddVM(EError.None, "IRC是调用虚函数为空!!");
                            Debug.Assert(false, "没有找到索引是" + iri.index + "的函数!");
                            return;
                        }
                        List<RuntimeType> rtList = new List<RuntimeType>(rt.runtimeTemplateList);
                        for (int i = 0; i < runtimeCall.templateRuntimeDefTypeList.Count; i++)
                        {
                            var crt = GetClassRuntimeType(runtimeCall.templateRuntimeDefTypeList[i], irc, rt.runtimeTemplateList, true);
                            rtList.Add(crt);
                        }
                        CLRVM.RunIRMethod(rtList, cfc);

                        var a = ObjectManager.classObjectDict;
                    }
                    break;
                case EIROpCode.Ldc:
                    {
                        var mdt = TryGetInstructionRuntimeDefType(iri);
                        if (mdt != null)
                        {
                            var rt = GetClassRuntimeType(mdt, m_CurrentRuntimeClass != null ? m_CurrentRuntimeClass : mdt.ownerRuntimeClass, m_InputTemplateRuntimeTypeList, true);
                           
                            var sobj = new TypeObject(rt);
                            sobj.CreateObject();
                            var sv = default(SValue);
                            sv.SetSObject(sobj);
                            PushSValueSynced(sv);
                        }
                    }
                    break;
                case EIROpCode.Ret:
                    // stop execution early
                    m_ExecuteIndex = m_ExecuteCount;
                    break;
                case EIROpCode.LoadStaticField:
                    {
                        var mt = TryGetInstructionRuntimeDefType(iri);
                        if (mt != null)
                        {
                            var rt = RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(mt);
                            var v = default(SValue);
                            if (rt != null)
                            {
                                rt.GetMemberVariableSValue(iri.index, ref v);
                                PushSValueSynced(v);
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
                                var rt = RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(mt);
                                rt?.SetMemberVariableSValue(iri.index, val);
                            }
                        }
                    }
                    break;
                case EIROpCode.Beq:
                    {
                        if (m_ValueIndex >= 2)
                        {
                            var right = m_ValueStack[--m_ValueIndex];
                            var left = m_ValueStack[--m_ValueIndex];
                            bool methodCall = false;
                            SValue.CompareEuqalSValue1AndValue2(ref left, ref right, true, out methodCall);
                            bool isTrue = left.eType == EVMType.Boolean ? left.int8Value == 1 : left.GetValueObject() != null;
                            if (isTrue)
                            {
                                m_ExecuteIndex = (ushort)(iri.index - 1);
                            }
                        }
                    }
                    break;
                case EIROpCode.Beq_Un:
                    {
                        // TODO: implement unsigned compare; for now follow Beq semantics.
                        if (m_ValueIndex >= 2)
                        {
                            var right = m_ValueStack[--m_ValueIndex];
                            var left = m_ValueStack[--m_ValueIndex];
                            bool methodCall = false;
                            SValue.CompareEuqalSValue1AndValue2(ref left, ref right, true, out methodCall);
                            bool isTrue = left.eType == EVMType.Boolean ? left.int8Value == 1 : left.GetValueObject() != null;
                            if (isTrue)
                            {
                                m_ExecuteIndex = (ushort)(iri.index - 1);
                            }
                        }
                    }
                    break;
                default:
                    // unhandled op
                    Debug.Assert(false, "Function" + this.id + "IRData" + iri.id + "  " + iri.opCode);
                    break;
            }
        }
        public void SetObjectByValue(int type, int index, ref SValue svalue)
        {
            SObject obj = null;
            if (type == 0)
            {
                obj = m_ArgumentObjectArray[index];
            }
            else if (type == 1)
            {
                obj = m_LocalVariableObjectArray[index];
            }
            else if (type == 2)
            {
                obj = m_ReturnObjectArray[index];
            }
            Debug.Assert(obj != null);
            if (svalue.isNull)
            {
                obj.SetNull();
                return;
            }
            bool anyObj = obj.eType == EVMType.Object;
            switch (svalue.eType)
            {
                case EVMType.Null:
                    {
                        obj.SetNull();
                    }
                    break;
                //case EVMType.RawBoolean:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Boolean, svalue.int8Value);
                //        }
                //        BoolObject boolObj = obj as BoolObject;
                //        if (boolObj == null)
                //        {
                //            Debug.Write("该类型不是Boolean类型!!");
                //            return;
                //        }
                //        boolObj.SetValue(svalue.int8Value == 1);
                //    }
                //    break;
                case EVMType.Boolean:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Boolean, svalue.int8Value == 1);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Boolean, svalue.int8Value);
                        }
                        BoolObject boolObj = obj as BoolObject;
                        if (boolObj == null)
                        {
                            Debug.Write("该类型不是Boolean类型!!");
                            return;
                        }
                        boolObj.SetValue(svalue.int8Value == 1);
                    }
                    break;
                //case EVMType.RawByte:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Byte, svalue.int8Value);
                //            return;
                //        }
                //        Int8Object byteObj = obj as Int8Object;
                //        if (byteObj == null)
                //        {
                //            Debug.Write("该类型不是Byte类型!!");
                //            return;
                //        }
                //        byteObj.SetValue(svalue.int8Value);
                //    }
                //    break;
                case EVMType.Byte:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Byte, svalue.int8Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Byte, svalue.int8Value);
                            return;
                        }
                        Int8Object byteObj = obj as Int8Object;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是Byte类型!!");
                            return;
                        }
                        byteObj.SetValue(svalue.int8Value);
                    }
                    break;
                //case EVMType.RawSByte:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.SByte, svalue.sint8Value);
                //        }
                //        SInt8Object byteObj = obj as SInt8Object;
                //        if (byteObj == null)
                //        {
                //            Debug.Write("该类型不是SByte类型!!");
                //            return;
                //        }
                //        byteObj.SetValue(svalue.sint8Value);
                //    }
                //    break;
                case EVMType.SByte:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.SByte, svalue.sint8Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.SByte, svalue.sint8Value);
                        }
                        SInt8Object byteObj = obj as SInt8Object;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是SByte类型!!");
                            return;
                        }
                        byteObj.SetValue(svalue.sint8Value);
                    }
                    break;
                //case EVMType.RawInt16:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Int16, svalue.int16Value);
                //        }
                //        Int16Object int16Obj = obj as Int16Object;
                //        if (int16Obj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        int16Obj.SetValue(svalue.int16Value);
                //    }
                //    break;
                case EVMType.Int16:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Int16, svalue.int16Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Int16, svalue.int16Value);
                        }
                        Int16Object int16Obj = obj as Int16Object;
                        if (int16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int16Obj.SetValue(svalue.int16Value);
                    }
                    break;
                //case EVMType.RawUInt16:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.UInt16, svalue.uint16Value);
                //        }
                //        UInt16Object uint16Obj = obj as UInt16Object;
                //        if (uint16Obj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        uint16Obj.SetValue(svalue.uint16Value);
                //    }
                //    break;
                case EVMType.UInt16:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.UInt16, svalue.int16Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.UInt16, svalue.uint16Value);
                        }
                        UInt16Object uint16Obj = obj as UInt16Object;
                        if (uint16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        uint16Obj.SetValue(svalue.uint16Value);
                    }
                    break;
                //case EVMType.RawInt32:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Int32, svalue.int32Value);
                //            return;
                //        }
                //        Int32Object int32Obj = obj as Int32Object;
                //        if (int32Obj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        int32Obj.SetValue(svalue.int32Value);
                //    }
                //    break;
                case EVMType.Int32:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Int32, svalue.int32Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Int32, svalue.int32Value);
                            return;
                        }
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int32Value);
                    }
                    break;
                //case EVMType.RawUInt32:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.UInt32, svalue.uint32Value);
                //        }
                //        UInt32Object uint32Obj = obj as UInt32Object;
                //        if (uint32Obj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        uint32Obj.SetValue(svalue.uint32Value);
                //    }
                //    break;
                case EVMType.UInt32:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.UInt32, svalue.uint32Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.UInt32, svalue.uint32Value);
                        }
                        UInt32Object uint32Obj = obj as UInt32Object;
                        if (uint32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        uint32Obj.SetValue(svalue.uint32Value);
                    }
                    break;
                //case EVMType.RawInt64:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Int64, svalue.int64Value);
                //        }
                //        Int64Object int64Obj = obj as Int64Object;
                //        if (int64Obj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        int64Obj.SetValue(svalue.int64Value);
                //    }
                //    break;
                case EVMType.Int64:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Int64, svalue.int64Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Int64, svalue.int64Value);
                        }
                        Int64Object int64Obj = obj as Int64Object;
                        if (int64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int64Obj.SetValue(svalue.int64Value);
                    }
                    break;
                //case EVMType.RawUInt64:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.UInt64, svalue.uint64Value);
                //        }
                //        UInt64Object uint64Obj = obj as UInt64Object;
                //        if (uint64Obj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        uint64Obj.SetValue(svalue.uint64Value);
                //    }
                //    break;
                case EVMType.UInt64:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.UInt64, svalue.uint64Value);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.UInt64, svalue.uint64Value);
                        }
                        UInt64Object uint64Obj = obj as UInt64Object;
                        if (uint64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        uint64Obj.SetValue(svalue.uint64Value);
                    }
                    break;
                //case EVMType.RawFloat32:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Float32, svalue.floatValue);
                //        }
                //        Float32Object floatObj = obj as Float32Object;
                //        if (floatObj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        floatObj.SetValue(svalue.floatValue);
                //    }
                //    break;
                case EVMType.Float32:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Float32, svalue.floatValue);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Float32, svalue.floatValue);
                        }
                        Float32Object floatObj = obj as Float32Object;
                        if (floatObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        floatObj.SetValue(svalue.floatValue);
                    }
                    break;
                //case EVMType.RawFloat64:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.Float64, svalue.doubleValue);
                //        }
                //        Float64Object doubleObj = obj as Float64Object;
                //        if (doubleObj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        doubleObj.SetValue(svalue.doubleValue);
                //    }
                //    break;
                case EVMType.Float64:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Float64, svalue.doubleValue);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.Float64, svalue.doubleValue);
                        }
                        Float64Object doubleObj = obj as Float64Object;
                        if (doubleObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        doubleObj.SetValue(svalue.doubleValue);
                    }
                    break;
                //case EVMType.RawString:
                //    {
                //        TemplateObject to = obj as TemplateObject;
                //        if (to != null)
                //        {
                //            to.SetValue(EVMType.String, svalue.stringValue);
                //            return;
                //        }
                //        StringObject stringObj = obj as StringObject;
                //        if (stringObj == null)
                //        {
                //            Debug.Write("该类型不是Int32类型!!");
                //            return;
                //        }
                //        stringObj.SetValue(svalue.stringValue);
                //    }
                //    break;
                case EVMType.String:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.String, svalue.stringValue);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetValue(EVMType.String, svalue.stringValue);
                            return;
                        }
                        StringObject stringObj = obj as StringObject;
                        if (stringObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        stringObj.SetValue(svalue.stringValue);
                    }
                    break;
                case EVMType.Array:
                    {
                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Class, svalue.sobject);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetClassObject(svalue.sobject as ClassObject);
                            return;
                        }
                        if (obj is ClassObject co)
                        {
                            var ao = svalue.sobject as ClassObject;
                            Debug.Assert(ao != null);
                            //co.SetClassObject(ao);                            
                            (obj as ClassObject).SetClassObject(ao);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    break;
                case EVMType.Object:
                    {
                        if (svalue.eType == EVMType.Object)
                        {
                            obj.SetValueByType(EVMType.Object, svalue.sobject);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    break;
                case EVMType.Type:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetClassObject(svalue.sobject as ClassObject);
                            return;
                        }
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    if( svalue.sobject is ClassObject co )
                        //    {
                        //        anyObject.SetValue(EVMType.Class, co.value );
                        //    }
                        //    return;
                        //}

                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Type, svalue.sobject);
                            return;
                        }
                        TypeObject classObj = obj as TypeObject;
                        if (classObj == null)
                        {
                            Debug.Assert(false);
                            Debug.Write("该类型不是Class类型!!");
                            return;
                        }
                        classObj.SetClassObject(svalue.sobject as ClassObject);
                    }
                    break;
                case EVMType.Class:
                    {
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            to.SetClassObject(svalue.sobject as ClassObject);
                            return;
                        }
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    if( svalue.sobject is ClassObject co )
                        //    {
                        //        anyObject.SetValue(EVMType.Class, co.value );
                        //    }
                        //    return;
                        //}

                        if (anyObj)
                        {
                            obj.SetValueByType(EVMType.Class, svalue.sobject);
                            return;
                        }
                        ClassObject classObj = obj as ClassObject;
                        if (classObj == null)
                        {
                            Debug.Assert(false);
                            Debug.Write("该类型不是Class类型!!");
                            return;
                        }
                        classObj.SetClassObject(svalue.sobject as ClassObject);
                        /*
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj != null)
                        {
                            int32Obj.SetValue(svalue.int32Value);
                            return;
                        }
                        BoolObject boolObject = obj as BoolObject;
                        if (boolObject != null)
                        {
                            boolObject.SetValue(svalue.int8Value == 1 ? true : false);
                            return;
                        }
                        */
                    }
                    break;
                default:
                    {
                        Debug.Assert(false);
                    }
                    break;
            }

        }

        public void GetObjectByValue(int type, int index, ref SValue svalue)
        {
            SObject obj = null;
            if (type == 0)
            {
                obj = m_ArgumentObjectArray[index];
            }
            else if (type == 1)
            {
                obj = m_LocalVariableObjectArray[index];
            }
            else if (type == 2)
            {
                obj = m_ReturnObjectArray[index];
            }
            Debug.Assert(obj != null);
            if (obj.isNull)
            {
                svalue.SetNull();
                return;
            }
            SetSValue(obj, obj.eType, ref svalue);
        }

        public void SetSValue(SObject obj, EVMType etype, ref SValue svalue)
        {
            bool anyObj = svalue.eType == EVMType.Object;
            switch (etype)
            {
                case EVMType.Null:
                    {
                        svalue.SetNull();
                    }
                    break;
                case EVMType.Boolean:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Boolean)
                            {
                                svalue.SetBoolValue((bool)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetBoolValue((bool)to.value);
                            return;
                        }

                        BoolObject boolObj = obj as BoolObject;
                        if (boolObj == null)
                        {
                            Debug.Write("该类型不是Boolean类型!!");
                            return;
                        }
                        svalue.SetBoolValue(boolObj.value);
                    }
                    break;
                case EVMType.Byte:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Byte)
                            {
                                svalue.SetInt8Value((byte)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetInt8Value((Byte)to.value);
                            to.SetValue(EVMType.Byte, obj.value);
                            return;
                        }

                        Int8Object byteObj = obj as Int8Object;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是Byte类型!!");
                            return;
                        }
                        svalue.SetInt8Value((Byte)byteObj.value);
                    }
                    break;
                case EVMType.SByte:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.SByte)
                            {
                                svalue.SetSInt8Value((sbyte)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetSInt8Value((SByte)to.value);
                            return;
                        }

                        SInt8Object byteObj = obj as SInt8Object;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是SByte类型!!");
                            return;
                        }
                        svalue.SetSInt8Value((SByte)byteObj.value);
                    }
                    break;
                case EVMType.Int16:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Int16)
                            {
                                svalue.SetInt16Value((short)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetInt16Value((Int16)to.value);
                            return;
                        }

                        Int16Object int16Obj = obj as Int16Object;
                        if (int16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetInt16Value((short)int16Obj.value);
                    }
                    break;
                case EVMType.UInt16:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.UInt16)
                            {
                                svalue.SetUInt16Value((ushort)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetUInt16Value((UInt16)to.value);
                            return;
                        }

                        UInt16Object uint16Obj = obj as UInt16Object;
                        if (uint16Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetUInt16Value((ushort)uint16Obj.value);
                    }
                    break;
                case EVMType.Int32:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Int32)
                            {
                                svalue.SetInt32Value((int)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetInt32Value((int)to.value);
                            return;
                        }
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetInt32Value((int)int32Obj.value);
                    }
                    break;
                case EVMType.UInt32:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.UInt32)
                            {
                                svalue.SetUInt32Value((uint)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetUInt32Value((UInt32)to.value);
                            return;
                        }
                        UInt32Object uint32Obj = obj as UInt32Object;
                        if (uint32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetUInt32Value((uint)uint32Obj.value);
                    }
                    break;
                case EVMType.Int64:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Int64)
                            {
                                svalue.SetInt64Value((long)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetInt64Value((Int64)to.value);
                            return;
                        }
                        Int64Object int64Obj = obj as Int64Object;
                        if (int64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetInt64Value((Int64)int64Obj.value);
                    }
                    break;
                case EVMType.UInt64:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.UInt64)
                            {
                                svalue.SetUInt64Value((ulong)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetUInt64Value((UInt64)to.value);
                            return;
                        }

                        UInt64Object uint64Obj = obj as UInt64Object;
                        if (uint64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetUInt64Value((UInt64)uint64Obj.value);
                    }
                    break;
                case EVMType.Float32:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Float32)
                            {
                                svalue.SetFloatValue((float)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetFloatValue((Single)to.value);
                            return;
                        }

                        Float32Object floatObj = obj as Float32Object;
                        if (floatObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetFloatValue((float)floatObj.value);
                    }
                    break;
                case EVMType.Float64:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.Float64)
                            {
                                svalue.SetDoubleValue((double)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetDoubleValue((Double)to.value);
                            return;
                        }

                        Float64Object doubleObj = obj as Float64Object;
                        if (doubleObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetDoubleValue((double)doubleObj.value);
                    }
                    break;
                case EVMType.String:
                    {
                        if (anyObj)
                        {
                            if (obj.eAnyType == EVMType.String)
                            {
                                svalue.SetStringValue((string)obj.value);
                            }
                            else
                            {
                                Debug.Assert(false, "该类型不是Boolean类型!!");
                            }
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetStringValue((String)to.value);
                            return;
                        }

                        StringObject stringObj = obj as StringObject;
                        if (stringObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        svalue.SetStringValue((string)stringObj.value);
                    }
                    break;
                case EVMType.Array:
                    {
                        if (obj is ClassObject co)
                        {
                            svalue.SetSObject(co.value);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    break;
                case EVMType.Object:
                    {
                        switch (obj.eAnyType)
                        {
                            case EVMType.Boolean:
                                {
                                    svalue.SetBoolValue((bool)obj.value);
                                }
                                break;
                            case EVMType.Byte:
                                {
                                    svalue.SetInt8Value((byte)obj.value);
                                }
                                break;
                            case EVMType.SByte:
                                {
                                    svalue.SetSInt8Value((sbyte)obj.value);
                                }
                                break;
                            case EVMType.Int16:
                                {
                                    svalue.SetInt16Value((Int16)obj.value);
                                }
                                break;
                            case EVMType.UInt16:
                                {
                                    svalue.SetUInt16Value((UInt16)obj.value);
                                }
                                break;
                            case EVMType.Int32:
                                {
                                    svalue.SetInt32Value((Int32)obj.value);
                                }
                                break;
                            case EVMType.UInt32:
                                {
                                    svalue.SetUInt32Value((UInt32)obj.value);
                                }
                                break;
                            case EVMType.Int64:
                                {
                                    svalue.SetInt64Value((Int64)obj.value);
                                }
                                break;
                            case EVMType.UInt64:
                                {
                                    svalue.SetUInt64Value((UInt64)obj.value);
                                }
                                break;
                            case EVMType.Float32:
                                {
                                    svalue.SetFloatValue((float)obj.value);
                                }
                                break;
                            case EVMType.Float64:
                                {
                                    svalue.SetDoubleValue((double)obj.value);
                                }
                                break;
                            case EVMType.String:
                                {
                                    svalue.SetStringValue((string)obj.value);
                                }
                                break;
                            default:
                                {
                                    svalue.SetSObject(obj.value as SObject);
                                }
                                break;
                        }
                    }
                    break;
                case EVMType.Type:
                    {
                        if (anyObj)
                        {
                            svalue.SetSObject(obj.value as SObject);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetSObject(to.value as SObject);
                            return;
                        }
                        if (obj is TypeObject co)
                        {
                            svalue.SetSObject(co.value as SObject);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    break;
                case EVMType.Class:
                    {
                        if (anyObj)
                        {
                            svalue.SetSObject(obj.value as SObject);
                            return;
                        }
                        TemplateObject to = obj as TemplateObject;
                        if (to != null)
                        {
                            svalue.SetSObject(to.value as SObject);
                            return;
                        }
                        //AnyObject anyObject = obj as AnyObject;
                        //if (anyObject != null)
                        //{
                        //    if( svalue.sobject is ClassObject co )
                        //    {
                        //        anyObject.SetValue(EVMType.Class, co.value );
                        //    }
                        //    return;
                        //}
                        //4代表的是直接传值 ，所以，原来值啥样就传进去
                        //if (type == 4)
                        //{
                        //    if (obj is ClassObject co)
                        //    {
                        //        if (co.value != null)
                        //        {
                        //            svalue.SetSObject(co.value as SObject);
                        //        }
                        //        else
                        //        {
                        //            svalue.SetSObject(co);
                        //        }
                        //    }
                        //    else
                        //    {
                        //        Debug.Assert(false);
                        //    }
                        //}
                        //else
                        //{
                        if (obj is ClassObject co)
                        {
                            svalue.SetSObject(co.value as SObject);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                        //}
                        /*
                        Int32Object int32Obj = obj as Int32Object;
                        if (int32Obj != null)
                        {
                            int32Obj.SetValue(svalue.int32Value);
                            return;
                        }
                        BoolObject boolObject = obj as BoolObject;
                        if (boolObject != null)
                        {
                            boolObject.SetValue(svalue.int8Value == 1 ? true : false);
                            return;
                        }
                        */
                    }
                    break;
                default:
                    {
                        Debug.Assert(false);
                    }
                    break;
            }
        }
    }
}
