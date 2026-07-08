using SimpleLanguage.Logging;
using SimpleLanguage.VM.Runtime;
using System.Buffers.Binary;
using System.Text;

namespace SimpleLanguage.VM
{
    public class RuntimeObject
    {
        public RuntimeType runtimeType => m_RuntimeType;
        public EVMType eType => m_RuntimeType != null ? m_RuntimeType.eType : EVMType.Null;
        public SObject sobject => GetSObject();
        public DebugInfo debugInfo => m_RuntimeVariable != null ? m_RuntimeVariable.debugInfo : null;
        public RuntimeVariable runtimeVariable => m_RuntimeVariable;
        public bool isNull => m_IsNull;

        public int memberVariableId => m_RuntimeVariable?.id ?? 0;
        public int memberIndex => m_Index;
        public int memberDataStart => m_Start;
        public int memberDataLength => m_Length;
        public bool hasMemberDataSlice => m_MemberDataBuffer != null && m_Length > 0;

        private RuntimeVariable m_RuntimeVariable = null;
        private RuntimeType m_RuntimeType = null;
        private bool m_IsNull = true;
#if DEBUG
        private SObject m_SObject = null;
#endif
        private int m_Index = 0;
        private int m_Start = 0;
        private int m_Length = 0;
        private byte[]? m_MemberDataBuffer = null;
        private EVMType m_ObjectActualType = EVMType.Null;


        public RuntimeObject( RuntimeType rt, RuntimeVariable rv, SObject sobj )
        {
            m_RuntimeVariable = rv;
            m_RuntimeType = rt;
            if( rt == null )
            {
                Log.AddRuntimeLog(LID.RuntimeVMRuntimeTypeIsNull, "", m_RuntimeVariable.name );
                return;
            }

            SetObjectPointer(sobj);
            RefreshIsNull();
        }

        private void RefreshIsNull()
        {
            if (RuntimeTypeManager.IsMemberDataDirectType(eType) && hasMemberDataSlice)
            {
                m_IsNull = false;
                return;
            }

            if (eType == EVMType.Object && RuntimeTypeManager.IsObjectScalarType(m_ObjectActualType))
            {
                m_IsNull = false;
                return;
            }

            m_IsNull = GetSObject() == null;
        }

        private void EnsureStandaloneMemberDataSlice()
        {
            if (m_RuntimeType == null)
                return;
            if (m_MemberDataBuffer != null && m_Length > 0)
                return;

            int slotLen = MemberDataLayout.GetSlotByteLength(m_RuntimeType);
            if (slotLen <= 0)
                slotLen = sizeof(int);

            m_MemberDataBuffer = new byte[slotLen];
            m_Start = 0;
            m_Length = slotLen;
            m_Index = 0;
        }

        internal void AttachMemberDataSlice(byte[]? classMemberData, int start, int length, int memberIndex)
        {
            m_MemberDataBuffer = classMemberData;
            m_Start = start;
            m_Length = length;
            m_Index = memberIndex;
        }
        private SObject GetSObject()
        {
            if (m_MemberDataBuffer == null || m_Start < 0 || m_Start + 4 > m_MemberDataBuffer.Length)
            {
#if DEBUG
                m_SObject = null;
#endif
                return null;
            }
            int pointerId = BinaryPrimitives.ReadInt32LittleEndian(m_MemberDataBuffer.AsSpan(m_Start, 4));
            if (pointerId <= 0)
            {
#if DEBUG
                m_SObject = null;
#endif
                return null;
            }

            var realObject = ObjectManager.GetObjectById(pointerId);
#if DEBUG
            m_SObject = realObject;
#endif
            return realObject;
        }

        private void SetObjectPointer(SObject sobj, bool isWriteMemberDataSpan = false )
        {
            if (sobj != null)
            {
                ObjectManager.RegisterObject(sobj);
#if DEBUG
                m_SObject = sobj;
#endif
            }
            else
            {
#if DEBUG
                m_SObject = null;
#endif
            }

            if(isWriteMemberDataSpan )
            {
                if (TryGetMemberDataSpan(out Span<byte> span))
                {
                    BinaryPrimitives.WriteInt32LittleEndian(span, sobj?.id ?? 0);
                }
            }
        }
        private bool ValidateGenericReferenceAssignment(SObject incomingRef)
        {
            if (incomingRef == null || m_RuntimeType == null)
                return false;

            var targetType = m_RuntimeType;
            var sourceType = incomingRef.runtimeType;
            if (sourceType == null)
                return false;

            if( targetType.eType == EVMType.Object )
            {
                return true;
            }
            if( targetType == RuntimeTypeManager.objectRuntimeType )
            {
                return true;
            }
            // 允许 TypeObject 赋值给 Data 基类或其子类参数
            // 例如 StudentRecord.toString() 内部 SystemBuildDataString(this) 的 this 为 TypeObject（由 LoadConstType 生成），
            // TypeObject.currentRT 包装了实际 data 类型（如 StudentRecord），需检查其是否继承自 targetType（如 Core.Data）
            if (incomingRef is TypeObject typeObj && typeObj.currentRT?.runtimeClass?.IsExtendsRelation(targetType.runtimeClass) == true)
            {
                return true;
            }
            bool targetIsInterface = targetType.runtimeClass?.isInterfaceClass == true;
            if (targetIsInterface)
            {
                if (!sourceType.runtimeClass.IsExtendsRelation(targetType.runtimeClass))
                    return false;

                return ValidateInterfaceTemplateCovariance(sourceType, targetType);
            }

            if( targetType.runtimeTemplateList.Count > 0 )  //比较带模板的
            {
                // 如果 source 的运行时类继承自 target 的运行时类，允许赋值
                // (类型安全已在编译期由前端检查)
                if (sourceType.runtimeClass != targetType.runtimeClass &&
                    sourceType.runtimeClass.IsExtendsRelation(targetType.runtimeClass))
                {
                    return true;
                }

                if( targetType.runtimeTemplateList.Count != sourceType.runtimeTemplateList.Count )
                {
                    return false;
                }

                bool flag = ValidateSameGenericRuntimeTypeRecursive(sourceType, targetType);
                if (flag == false)
                {
                    return false;
                }
            }
            else
            {
                if (sourceType.runtimeClass.IsExtendsRelation(targetType.runtimeClass))
                    return true;

                if(targetType.runtimeClass.metaClassKind == 1 )
                {
                    if(sourceType == RuntimeTypeManager.memberRuntimeType )
                    {
                        return true;
                    }
                }
                return ReferenceEquals(sourceType.runtimeClass, targetType.runtimeClass);                   
            }
            return true;
        }

        private static bool ValidateSameGenericRuntimeTypeRecursive(RuntimeType sourceType, RuntimeType targetType)
        {
            if (sourceType == null || targetType == null)
                return false;

            if (!ReferenceEquals(sourceType.runtimeClass, targetType.runtimeClass))
                return false;

            var sourceTemplates = sourceType.runtimeTemplateList;
            var targetTemplates = targetType.runtimeTemplateList;
            if (sourceTemplates == null || targetTemplates == null)
                return sourceTemplates == targetTemplates;

            if (sourceTemplates.Count != targetTemplates.Count)
                return false;

            for (int i = 0; i < sourceTemplates.Count; i++)
            {
                var sourceArg = sourceTemplates[i];
                var targetArg = targetTemplates[i];
                if (sourceArg == null || targetArg == null)
                    return false;

                if (!ValidateSameGenericRuntimeTypeRecursive(sourceArg, targetArg))
                    return false;
            }

            return true;
        }

        private static bool ValidateInterfaceTemplateCovariance(RuntimeType sourceType, RuntimeType targetType)
        {
            var targetTemplateList = targetType.runtimeTemplateList;
            if (targetTemplateList == null || targetTemplateList.Count == 0)
                return true;

            var sourceAsTargetTemplateList = ResolveSourceInterfaceTemplateList(sourceType, targetType.runtimeClass, targetTemplateList.Count);
            if (sourceAsTargetTemplateList == null || sourceAsTargetTemplateList.Count != targetTemplateList.Count)
                return false;

            for (int i = 0; i < targetTemplateList.Count; i++)
            {
                var targetArg = targetTemplateList[i];
                var sourceArg = sourceAsTargetTemplateList[i];
                if (targetArg == null || sourceArg == null)
                    return false;

                if (!sourceArg.IsExtendsRelation(targetArg))
                    return false;
            }

            return true;
        }

        private static List<RuntimeType>? ResolveSourceInterfaceTemplateList(RuntimeType sourceType, RuntimeClass targetInterfaceClass, int expectedCount)
        {
            if (sourceType == null || targetInterfaceClass == null)
                return null;

            if (sourceType.runtimeClass == targetInterfaceClass)
            {
                var direct = sourceType.runtimeTemplateList;
                if (direct != null && direct.Count == expectedCount)
                    return direct;
                return null;
            }

            var resolved = new List<RuntimeType>(expectedCount);
            for (int i = 0; i < expectedCount; i++)
            {
                var relationDef = sourceType.runtimeClass.GetRuntimeDefTypeByTemplateAndClassRelation(targetInterfaceClass, i);
                if (relationDef == null)
                    return null;

                var relationType = RuntimeVM.GetRuntimeTypeByDefType(relationDef, sourceType.runtimeClass, sourceType.runtimeTemplateList, false);
                if (relationType == null)
                    return null;

                resolved.Add(relationType);
            }

            return resolved;
        }
        /*
        private void EnsureObjectScalarMemberDataSlice(int length)
        {
            if (length <= 0)
                length = sizeof(int);

            if (m_MemberDataBuffer != null
                && m_Start >= 0
                && m_Length >= length
                && m_Start + m_Length <= m_MemberDataBuffer.Length)
            {
                return;
            }

            m_MemberDataBuffer = new byte[length];
            m_Start = 0;
            m_Length = length;
            m_Index = 0;
        }

        private bool TryGetObjectScalarDataSpan(out Span<byte> span)
        {
            span = default;
            if (!RuntimeTypeManager.IsObjectScalarType(m_ObjectActualType))
                return false;


            EnsureObjectScalarMemberDataSlice(m_Length);
            if (m_MemberDataBuffer == null || m_Start < 0 || m_Start + m_Length > m_MemberDataBuffer.Length)
                return false;

            span = m_MemberDataBuffer.AsSpan(m_Start, m_Length);
            return true;
        }
        */
        public bool TryReadMemberDataToSValue(ref RuntimeValue RuntimeValue)
        {
            if (m_MemberDataBuffer == null || m_Length <= 0 || m_RuntimeType == null)
                return false;
            if (m_Start + m_Length > m_MemberDataBuffer.Length)
                return false;

            if( RuntimeTypeManager.IsPureNumericTypeLocal( m_RuntimeType.eType ) )
            {
                ReadSpanToSValue(m_MemberDataBuffer.AsSpan(m_Start, m_Length), this.m_RuntimeType.eType, ref RuntimeValue);
            }
            else if(m_RuntimeType.eType == EVMType.Boolean )
            {
                ReadSpanToSValue(m_MemberDataBuffer.AsSpan(m_Start, m_Length), this.m_RuntimeType.eType, ref RuntimeValue);
            }
            else if( m_RuntimeType.eType == EVMType.Num )
            {
                //if( RuntimeTypeManager.IsPureNumericTypeLocal(RuntimeValue.eType ) )
                //{
                    ReadSpanToSValue(m_MemberDataBuffer.AsSpan(m_Start, m_Length), EVMType.Float64, ref RuntimeValue);
                //}
                //else if( RuntimeValue.eType == EVMType.Null )
                //{
                //    m_IsNull = true;
                //}
            }
            else
            {
                ReadSpanToSValue(m_MemberDataBuffer.AsSpan(m_Start, m_Length), EVMType.Object, ref RuntimeValue);
            }

            return true;
        }

        private static void ReadSpanToSValue(ReadOnlySpan<byte> span, EVMType evmType, ref RuntimeValue RuntimeValue)
        {
            switch (evmType)
            {
                case EVMType.Boolean:
                    byte bv = span.Length > 0 ? unchecked((byte)span[0]) : (byte)0;
                    RuntimeValue.SetBoolValue(bv ==1);
                    break;
                case EVMType.UInt8:
                    RuntimeValue.SetUInt8Value(span.Length > 0 ? span[0] : (byte)0);
                    break;
                case EVMType.Int8:
                    RuntimeValue.SetInt8Value(span.Length > 0 ? unchecked((sbyte)span[0]) : (sbyte)0);
                    break;
                case EVMType.Int16:
                    RuntimeValue.SetInt16Value(span.Length >= 2 ? BinaryPrimitives.ReadInt16LittleEndian(span) : (short)0);
                    break;
                case EVMType.UInt16:
                    RuntimeValue.SetUInt16Value(span.Length >= 2 ? BinaryPrimitives.ReadUInt16LittleEndian(span) : (ushort)0);
                    break;
                case EVMType.Int32:
                    RuntimeValue.SetInt32Value(span.Length >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(span) : 0);
                    break;
                case EVMType.UInt32:
                    RuntimeValue.SetUInt32Value(span.Length >= 4 ? BinaryPrimitives.ReadUInt32LittleEndian(span) : 0u);
                    break;
                case EVMType.Int64:
                    RuntimeValue.SetInt64Value(span.Length >= 8 ? BinaryPrimitives.ReadInt64LittleEndian(span) : 0L);
                    break;
                case EVMType.UInt64:
                    RuntimeValue.SetUInt64Value(span.Length >= 8 ? BinaryPrimitives.ReadUInt64LittleEndian(span) : 0uL);
                    break;
                case EVMType.Float32:
                    RuntimeValue.SetFloatValue(span.Length >= 4 ? BinaryPrimitives.ReadSingleLittleEndian(span) : 0f);
                    break;
                case EVMType.Float64:
                    RuntimeValue.SetDoubleValue(span.Length >= 8 ? BinaryPrimitives.ReadDoubleLittleEndian(span) : 0d);
                    break;
                case EVMType.String:
                case EVMType.Class:
                case EVMType.Array:
                case EVMType.Object:
                case EVMType.Type:
                case EVMType.Member:
                    {
                        int pointerId = span.Length >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(span) : 0;
                        var sobj = ObjectManager.GetObjectById(pointerId);
                        if (sobj == null)
                        {
                            RuntimeValue.SetNull();
                        }
                        else if (evmType == EVMType.String && sobj is StringObject strObj)
                        {
                            RuntimeValue.SetStringValue(strObj.value);
                        }
                        else
                        {
                            RuntimeValue.SetValueBySObject(sobj);
                        }
                    }
                    break;
                default:
                    Log.AddRuntimeLog(LID.ShowMessageAssert, "error");
                    //RuntimeValue.SetInt32Value(span.Length >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(span) : 0);
                    break;
            }
        }
        private bool TryGetMemberDataSpan(out Span<byte> span)
        {
            span = default;

            // 延迟分配：优先复用外�?Attach 进来的共�?memberData；仅在未附着时才创建独立槽位�?
            if ((m_MemberDataBuffer == null || m_Length <= 0) && m_RuntimeType != null)
            {
                EnsureStandaloneMemberDataSlice();
            }

            if (m_MemberDataBuffer == null || m_Length <= 0)
                return false;
            if (m_Start < 0 || m_Start + m_Length > m_MemberDataBuffer.Length)
                return false;

            span = m_MemberDataBuffer.AsSpan(m_Start, m_Length);
            return true;
        }

        private static void WriteSValueToMemberDataSpan(Span<byte> span, EVMType evmType, ref RuntimeValue sval)
        {
            if (span.Length <= 0)
                return;

            switch (evmType)
            {
                case EVMType.Boolean:
                    span[0] = (byte)(sval.uint8Value == 1 ? 1 : 0);
                    break;
                case EVMType.UInt8:
                    span[0] = sval.uint8Value;
                    break;
                case EVMType.Int8:
                    span[0] = unchecked((byte)sval.int8Value);
                    break;
                case EVMType.Int16:
                    if (span.Length >= 2)
                        BinaryPrimitives.WriteInt16LittleEndian(span, sval.int16Value);
                    break;
                case EVMType.UInt16:
                    if (span.Length >= 2)
                        BinaryPrimitives.WriteUInt16LittleEndian(span, sval.uint16Value);
                    break;
                case EVMType.Int32:
                    if (span.Length >= 4)
                        BinaryPrimitives.WriteInt32LittleEndian(span, sval.int32Value);
                    break;
                case EVMType.UInt32:
                    if (span.Length >= 4)
                        BinaryPrimitives.WriteUInt32LittleEndian(span, sval.uint32Value);
                    break;
                case EVMType.Int64:
                    if (span.Length >= 8)
                        BinaryPrimitives.WriteInt64LittleEndian(span, sval.int64Value);
                    break;
                case EVMType.UInt64:
                    if (span.Length >= 8)
                        BinaryPrimitives.WriteUInt64LittleEndian(span, sval.uint64Value);
                    break;
                case EVMType.Float32:
                    if (span.Length >= 4)
                        BinaryPrimitives.WriteSingleLittleEndian(span, sval.float32Value);
                    break;
                case EVMType.Float64:
                    if (span.Length >= 8)
                        BinaryPrimitives.WriteDoubleLittleEndian(span, sval.float64Value);
                    break;
            }
        }
        private static void WriteSValueToMemberDataSpanByObject(Span<byte> span, EVMType evmType, object obj )
        {
            if (span.Length <= 0)
                return;

            switch (evmType)
            {
                case EVMType.Boolean:
                    span[0] = (byte)((Convert.ToByte(obj) == 1) ? 1 : 0);
                    break;
                case EVMType.UInt8:
                    span[0] = Convert.ToByte(obj);
                    break;
                case EVMType.Int8:
                    span[0] = unchecked((byte)Convert.ToByte(obj));
                    break;
                case EVMType.Int16:
                    if (span.Length >= 2)
                        BinaryPrimitives.WriteInt16LittleEndian(span, Convert.ToInt16(obj));
                    break;
                case EVMType.UInt16:
                    if (span.Length >= 2)
                        BinaryPrimitives.WriteUInt16LittleEndian(span, Convert.ToUInt16(obj) );
                    break;
                case EVMType.Int32:
                    if (span.Length >= 4)
                        BinaryPrimitives.WriteInt32LittleEndian(span, Convert.ToInt32(obj) );
                    break;
                case EVMType.UInt32:
                    if (span.Length >= 4)
                        BinaryPrimitives.WriteUInt32LittleEndian(span, Convert.ToUInt32(obj) );
                    break;
                case EVMType.Int64:
                    if (span.Length >= 8)
                        BinaryPrimitives.WriteInt64LittleEndian(span, Convert.ToInt64(obj) );
                    break;
                case EVMType.UInt64:
                    if (span.Length >= 8)
                        BinaryPrimitives.WriteUInt64LittleEndian(span, Convert.ToUInt64(obj) );
                    break;
                case EVMType.Float32:
                    if (span.Length >= 4)
                        BinaryPrimitives.WriteSingleLittleEndian(span, Convert.ToSingle(obj) );
                    break;
                case EVMType.Float64:
                    if (span.Length >= 8)
                        BinaryPrimitives.WriteDoubleLittleEndian(span, Convert.ToDouble(obj) );
                    break;
            }
        }

        private static bool TryUnwrapScalarAssignmentValue(SObject? sobj, out object? value)
        {
            value = null;
            if (sobj == null)
                return false;

            var current = sobj;
            while (current != null)
            {
                if (current.eType == EVMType.Object && current.value is SObject inner)
                {
                    current = inner;
                    continue;
                }

                value = current.value;
                return value != null;
            }

            return false;
        }

        private void ClearMemberDataSlice()
        {
            if (m_MemberDataBuffer == null || m_Length <= 0)
                return;
            if (m_Start + m_Length <= m_MemberDataBuffer.Length)
                m_MemberDataBuffer.AsSpan(m_Start, m_Length).Clear();
        }
        public void SetNull()
        {
            m_ObjectActualType = EVMType.Null;
            SetObjectPointer(null);
            ClearMemberDataSlice();
            m_IsNull = true;
        }
        public void SetSObjectBySValue( ref RuntimeValue sval )
        {
            if (m_RuntimeType == null) return;
            if (sval.isNull)
            {
                m_ObjectActualType = EVMType.Null;
                SetObjectPointer(null);
                ClearMemberDataSlice();
                m_IsNull = true;
                return;
            }

            var evmType = this.m_RuntimeType.eType;
            if (sval.eType == EVMType.Object
                && (evmType == EVMType.Boolean
                    || evmType == EVMType.UInt8
                    || evmType == EVMType.Int8
                    || evmType == EVMType.Int16
                    || evmType == EVMType.UInt16
                    || evmType == EVMType.Int32
                    || evmType == EVMType.UInt32
                    || evmType == EVMType.Int64
                    || evmType == EVMType.UInt64
                    || evmType == EVMType.Float32
                    || evmType == EVMType.Float64
                    || evmType == EVMType.Num))
            {
                sval.ConvertValueByTargetTypeAndObject(evmType);
            }

            sval.TryCoerceScalarForAssignment(m_RuntimeType.eType);

            if (evmType == EVMType.Boolean
                || evmType == EVMType.UInt8
                || evmType == EVMType.Int8
                || evmType == EVMType.Int16
                || evmType == EVMType.UInt16
                || evmType == EVMType.Int32
                || evmType == EVMType.UInt32
                || evmType == EVMType.Int64
                || evmType == EVMType.UInt64
                || evmType == EVMType.Float32
                || evmType == EVMType.Float64 )
            {
                if( sval.eType == EVMType.Object )
                {
                    if (TryGetMemberDataSpan(out var directSpan)
                        && TryUnwrapScalarAssignmentValue(sval.sobject, out var scalarValue))
                    {
                        SetObjectPointer(null);
                        WriteSValueToMemberDataSpanByObject(directSpan, m_RuntimeType.eType, scalarValue);
                        m_IsNull = false;
                        return;

                    }
                }
                else
                {
                    if (TryGetMemberDataSpan(out var directSpan))
                    {
                        SetObjectPointer(null);
                        WriteSValueToMemberDataSpan(directSpan, m_RuntimeType.eType, ref sval);
                        m_IsNull = false;
                        return;

                    }
                }

            }
            else if(evmType == EVMType.Num )
            {
                if (sval.eType == EVMType.Object)
                {
                    if (TryGetMemberDataSpan(out var directSpan)
                        && TryUnwrapScalarAssignmentValue(sval.sobject, out var scalarValue))
                    {
                        SetObjectPointer(null);
                        WriteSValueToMemberDataSpanByObject(directSpan, m_RuntimeType.eType, scalarValue);
                        m_IsNull = false;
                        return;

                    }
                }
                else if ( RuntimeTypeManager.IsNumericTypeLocal( sval.eType ) )
                {
                    if (TryGetMemberDataSpan(out var directSpan))
                    {
                        SetObjectPointer(null);
                        WriteSValueToMemberDataSpan(directSpan, EVMType.Float64, ref sval);
                        m_IsNull = false;
                        return;

                    }
                }
                else
                {
                    Log.AddRuntimeLog(LID.ShowMessageAssert, this.m_RuntimeVariable.debugInfo,
                        $"Generic assignment is only supported for interface generic targets. target={m_RuntimeType}, source=)" );
                }
            }
            else
            {
                var curObj = GetSObject();
                if (curObj == null)
                {
                    var incomingRef = sval.GetReferenceSObject(createStringRef: true);
                    if (incomingRef != null)
                    {
                        if (!ValidateGenericReferenceAssignment(incomingRef))
                        {
                            Log.AddRuntimeLog(LID.ShowMessageAssert, this.m_RuntimeVariable.debugInfo,
                                $"Generic assignment is only supported for interface generic targets. target={m_RuntimeType}, source={incomingRef.runtimeType}");
                            m_IsNull = true;
                            return;
                        }

                        m_ObjectActualType = sval.eType;                        
                        SetObjectPointer(incomingRef, true);
                        m_IsNull = false;
                        return;
                    }
                    else
                    {
                        Log.AddRuntimeLog(LID.ShowMessageAssert, m_RuntimeVariable.debugInfo, "create RuntimeValue object failed");
                    }
                }
                else
                {
                    switch (m_ObjectActualType)
                    {
                        case EVMType.Boolean: curObj.SetValueByType(EVMType.Boolean, sval.uint8Value == 1); return;
                        case EVMType.Int8: curObj.SetValueByType(EVMType.Int8, sval.int8Value); return;
                        case EVMType.UInt8: curObj.SetValueByType(EVMType.UInt8, sval.uint8Value); return;
                        case EVMType.Int16: curObj.SetValueByType(EVMType.Int16, sval.int16Value); return;
                        case EVMType.UInt16: curObj.SetValueByType(EVMType.UInt16, sval.uint16Value); return;
                        case EVMType.Int32: curObj.SetValueByType(EVMType.Int32, sval.int32Value); return;
                        case EVMType.UInt32: curObj.SetValueByType(EVMType.UInt32, sval.uint32Value); return;
                        case EVMType.Int64: curObj.SetValueByType(EVMType.Int64, sval.int64Value); return;
                        case EVMType.UInt64: curObj.SetValueByType(EVMType.UInt64, sval.uint64Value); return;
                        case EVMType.Float32: curObj.SetValueByType(EVMType.Float32, sval.float32Value); return;
                        case EVMType.Float64: curObj.SetValueByType(EVMType.Float64, sval.float64Value); return;
                        default:
                            {
                                var incomingRef = sval.GetReferenceSObject(createStringRef: true);
                                if (incomingRef == null)
                                {
                                    Log.AddRuntimeLog(LID.ShowMessageAssert, m_RuntimeVariable.debugInfo, "create RuntimeValue object failed");
                                    return;
                                }
                                if (!ValidateGenericReferenceAssignment(incomingRef))
                                {
                                    Log.AddRuntimeLog(LID.ShowMessageAssert, m_RuntimeVariable.debugInfo,
                                        $"Generic assignment is only supported for interface generic targets. target={m_RuntimeType}, source={incomingRef.runtimeType}");
                                    return;
                                }

                                SetObjectPointer(incomingRef, true);
                                break;
                            }
                    }
                }
                return;
            }
        }
        public void SetSValueByRuntimeObjct(ref RuntimeValue RuntimeValue)
        {
            var etype = m_RuntimeType.eType;
            if ( RuntimeTypeManager.IsMemberDataDirectType(etype) )
            {
                TryReadMemberDataToSValue(ref RuntimeValue);
                return;
            }
            else
            {
                var sobj = this.GetSObject();
                if (sobj == null)
                {
                    RuntimeValue.SetNull();
                    return;
                }
                else
                {
                    switch (sobj)
                    {
                        case StringObject so:RuntimeValue.SetStringValueByStrinbObject(so);
                                break;
                        default:
                            RuntimeValue.SetRawSObject(sobj);
                            RuntimeValue.eType = etype;
                            break;
                    }
                }
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if( m_RuntimeType != null )
            {
                sb.Append(m_RuntimeType.ToString());
            }
            var sobj = GetSObject();
            if( sobj != null )
            {
                sb.Append(sobj.ToString());
            }

            return sb.ToString();
        }
    }
}
