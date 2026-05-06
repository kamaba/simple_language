using SimpleLanguage.Logging;
using SimpleLanguage.VM.Runtime;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
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

        /// <summary>IR / Meta 渚ф垚鍛樺彉閲?id锛堜笌 <see cref="RuntimeVariable.id"/> 涓€鑷达級锛涙棤鍏宠仈鍙橀噺鏃朵负 0銆?/summary>
        public int memberVariableId => m_RuntimeVariable?.id ?? 0;

        /// <summary>鍦ㄦ墍灞?<see cref="ClassObject"/> 鎴愬憳琛ㄤ腑鐨勪笅鏍囥€?/summary>
        public int memberIndex => m_Index;
        /// <summary>鍦ㄧ揣鍑戞垚鍛樼紦鍐插尯涓殑璧峰鍋忕Щ锛堝疄渚嬶細<see cref="ClassObject.memberData"/>锛涢潤鎬侊細<see cref="RuntimeType.memberData"/>锛夈€?/summary>
        public int memberDataStart => m_Start;
        /// <summary>绱у噾鎴愬憳缂撳啿鍖轰腑鏈Ы浣嶅瓧鑺傞暱搴︺€?/summary>
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

        private static EVMType DetectObjectActualType(SObject? sobj)
        {
            if (sobj == null)
                return EVMType.Null;

            if (sobj.eType != EVMType.Object)
                return sobj.eType;

            // Core.Object 装箱壳：优先从 payload 反推实体类型。
            var payload = sobj.value;
            if (payload is SObject inner)
                return inner.eType;
            if (payload is bool)
                return EVMType.Boolean;
            if (payload is byte)
                return EVMType.UInt8;
            if (payload is sbyte)
                return EVMType.Int8;
            if (payload is short)
                return EVMType.Int16;
            if (payload is ushort)
                return EVMType.UInt16;
            if (payload is int)
                return EVMType.Int32;
            if (payload is uint)
                return EVMType.UInt32;
            if (payload is long)
                return EVMType.Int64;
            if (payload is ulong)
                return EVMType.UInt64;
            if (payload is float)
                return EVMType.Float32;
            if (payload is double)
                return EVMType.Float64;
            if (payload is string)
                return EVMType.String;

            return EVMType.Object;
        }

        private static bool IsExactRuntimeType(RuntimeType? left, RuntimeType? right)
        {
            if (left == null || right == null)
                return false;

            if (!ReferenceEquals(left.runtimeClass, right.runtimeClass))
                return false;

            var leftTemplates = left.runtimeTemplateList;
            var rightTemplates = right.runtimeTemplateList;
            if (leftTemplates == null || rightTemplates == null)
                return leftTemplates == rightTemplates;
            if (leftTemplates.Count != rightTemplates.Count)
                return false;

            for (int i = 0; i < leftTemplates.Count; i++)
            {
                if (!IsExactRuntimeType(leftTemplates[i], rightTemplates[i]))
                    return false;
            }

            return true;
        }

        private bool ValidateGenericReferenceAssignment(SObject incomingRef)
        {
            if (incomingRef == null || m_RuntimeType == null)
                return false;

            var targetType = m_RuntimeType;
            var sourceType = incomingRef.runtimeType;
            if (sourceType == null)
                return false;

            bool targetIsGeneric = targetType.runtimeTemplateList != null && targetType.runtimeTemplateList.Count > 0;
            if (!targetIsGeneric)
                return true;

            bool targetIsInterface = targetType.runtimeClass?.isInterfaceClass == true;
            if (targetIsInterface)
            {
                if (!sourceType.runtimeClass.IsExtendsRelation(targetType.runtimeClass))
                    return false;

                return ValidateInterfaceTemplateCovariance(sourceType, targetType);
            }

            if (!ReferenceEquals(sourceType.runtimeClass, targetType.runtimeClass))
                return false;

            return ValidateSameGenericRuntimeTypeRecursive(sourceType, targetType);
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

        private void ClearObjectScalarValue()
        {
            m_ObjectActualType = EVMType.Null;
        }

        private static int GetObjectScalarTypeByteLength(EVMType t)
        {
            return t switch
            {
                EVMType.Boolean => 4,
                EVMType.UInt8 or EVMType.Int8 => 1,
                EVMType.Int16 or EVMType.UInt16 => 2,
                EVMType.Int32 or EVMType.UInt32 or EVMType.Float32 => 4,
                EVMType.Int64 or EVMType.UInt64 or EVMType.Float64 or EVMType.Num => 8,
                _ => 0,
            };
        }

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

        public bool TryReadMemberDataToSValue(ref SValue svalue)
        {
            if (m_MemberDataBuffer == null || m_Length <= 0 || m_RuntimeType == null)
                return false;
            if (m_Start + m_Length > m_MemberDataBuffer.Length)
                return false;

            if( RuntimeTypeManager.IsPureNumericTypeLocal( m_RuntimeType.eType ) )
            {
                ReadSpanToSValue(m_MemberDataBuffer.AsSpan(m_Start, m_Length), this.m_RuntimeType.eType, ref svalue);
            }
            else if(m_RuntimeType.eType == EVMType.Boolean )
            {
                ReadSpanToSValue(m_MemberDataBuffer.AsSpan(m_Start, m_Length), this.m_RuntimeType.eType, ref svalue);
            }
            else if( m_RuntimeType.eType == EVMType.Num )
            {
                //if( RuntimeTypeManager.IsPureNumericTypeLocal(svalue.eType ) )
                //{
                    ReadSpanToSValue(m_MemberDataBuffer.AsSpan(m_Start, m_Length), EVMType.Float64, ref svalue);
                //}
                //else if( svalue.eType == EVMType.Null )
                //{
                //    m_IsNull = true;
                //}
            }
            else
            {
                ReadSpanToSValue(m_MemberDataBuffer.AsSpan(m_Start, m_Length), EVMType.Object, ref svalue);
            }

            return true;
        }

        private static void ReadSpanToSValue(ReadOnlySpan<byte> span, EVMType evmType, ref SValue svalue)
        {
            switch (evmType)
            {
                case EVMType.Boolean:
                    byte bv = span.Length > 0 ? unchecked((byte)span[0]) : (byte)0;
                    svalue.SetBoolValue(bv ==1);
                    break;
                case EVMType.UInt8:
                    svalue.SetUInt8Value(span.Length > 0 ? span[0] : (byte)0);
                    break;
                case EVMType.Int8:
                    svalue.SetInt8Value(span.Length > 0 ? unchecked((sbyte)span[0]) : (sbyte)0);
                    break;
                case EVMType.Int16:
                    svalue.SetInt16Value(span.Length >= 2 ? BinaryPrimitives.ReadInt16LittleEndian(span) : (short)0);
                    break;
                case EVMType.UInt16:
                    svalue.SetUInt16Value(span.Length >= 2 ? BinaryPrimitives.ReadUInt16LittleEndian(span) : (ushort)0);
                    break;
                case EVMType.Int32:
                    svalue.SetInt32Value(span.Length >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(span) : 0);
                    break;
                case EVMType.UInt32:
                    svalue.SetUInt32Value(span.Length >= 4 ? BinaryPrimitives.ReadUInt32LittleEndian(span) : 0u);
                    break;
                case EVMType.Int64:
                    svalue.SetInt64Value(span.Length >= 8 ? BinaryPrimitives.ReadInt64LittleEndian(span) : 0L);
                    break;
                case EVMType.UInt64:
                    svalue.SetUInt64Value(span.Length >= 8 ? BinaryPrimitives.ReadUInt64LittleEndian(span) : 0uL);
                    break;
                case EVMType.Float32:
                    svalue.SetFloatValue(span.Length >= 4 ? BinaryPrimitives.ReadSingleLittleEndian(span) : 0f);
                    break;
                case EVMType.Float64:
                    svalue.SetDoubleValue(span.Length >= 8 ? BinaryPrimitives.ReadDoubleLittleEndian(span) : 0d);
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
                            svalue.SetNull();
                        }
                        else if (evmType == EVMType.String && sobj is StringObject strObj)
                        {
                            svalue.SetStringValue(strObj.value);
                        }
                        else
                        {
                            svalue.SetValueBySObject(sobj);
                        }
                    }
                    break;
                default:
                    Log.AddRuntimeLog(LID.ShowMessageAssert, "error");
                    //svalue.SetInt32Value(span.Length >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(span) : 0);
                    break;
            }
        }
        private bool TryGetMemberDataSpan(out Span<byte> span)
        {
            span = default;

            // 延迟分配：优先复用外部 Attach 进来的共享 memberData；仅在未附着时才创建独立槽位。
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

        private static void WriteSValueToMemberDataSpan(Span<byte> span, EVMType evmType, ref SValue sval)
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
            ClearObjectScalarValue();
            SetObjectPointer(null);
            ClearMemberDataSlice();
            m_IsNull = true;
        }
        public void SetSObjectBySValue( ref SValue sval )
        {
            if (m_RuntimeType == null) return;
            if (sval.isNull)
            {
                ClearObjectScalarValue();
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
                        Log.AddRuntimeLog(LID.ShowMessageAssert, m_RuntimeVariable.debugInfo, "create svalue object failed");
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
                                    Log.AddRuntimeLog(LID.ShowMessageAssert, m_RuntimeVariable.debugInfo, "create svalue object failed");
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
        public void SetSValueByRuntimeObjct(ref SValue svalue)
        {
            var etype = m_RuntimeType.eType;
            if ( RuntimeTypeManager.IsMemberDataDirectType(etype) )
            {
                TryReadMemberDataToSValue(ref svalue);
                return;
            }
            else
            {
                var sobj = this.GetSObject();
                if (sobj == null)
                {
                    svalue.SetNull();
                    return;
                }
                else
                {
                    switch (sobj)
                    {
                        case StringObject so:svalue.SetStringValueByStrinbObject(so);
                                break;
                        default:
                            svalue.SetRawSObject(sobj);
                            svalue.eType = etype;
                            break;
                    }
                }
            }
        }
        public SObject CreateObjectByRuntimeType()
        {
            var sobj = GetSObject();
            if (sobj == null)
            {
                sobj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType, true);
                SetObjectPointer(sobj);
            }
            return sobj;
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
