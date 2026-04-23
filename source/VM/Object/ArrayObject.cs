//****************************************************************************
//  File:      ArrayObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime:  2022/11/22 12:00:00
//  Description: 元素数据由 <see cref="ArrayObjectElementStore"/>（byte[] 紧凑块）管理；
//                 DEBUG 下额外保留 <see cref="m_DebugArray"/> 便于对照，与存储同步写入；读取走 byte 路径。
//****************************************************************************
using SimpleLanguage.Logging;
using SimpleLanguage.VM.Runtime;
namespace SimpleLanguage.VM
{
    public class ArrayObject : ClassObject
    {
        public int length => m_Length;

#if DEBUG
        /// <summary>仅 DEBUG：与旧 <c>Array</c> 相同形状的镜像，供调试对照；生产环境为 null。</summary>
        public Array? array => m_DebugArray;
        private Array? m_DebugArray;
#endif
        private ArrayObjectElementStore? m_Store;
        private RuntimeType eArrayType = null;
        private int m_Length = 0;
        public ArrayObject(RuntimeType rt, int length )
        {
            m_Type = EVMType.Array;
            m_RuntimeType = rt;
            eArrayType = rt.runtimeTemplateList[0];
            m_Length = length;

            m_IRTemplateList = rt.runtimeTemplateList;
            var metaVariableList = m_RuntimeType.runtimeClass.nonStaticIRMetaVariableList;
            m_MemberRuntimeObjectArray = new RuntimeObject[metaVariableList.Count];
            for (int i = 0; i < m_MemberRuntimeObjectArray.Length; i++)
            {
                var rt2 = RuntimeVM.GetRuntimeTypeByDefType(metaVariableList[i].runtimeDefType, m_RuntimeType.runtimeClass, m_IRTemplateList, true);

                SObject sobj = null;
                if( RuntimeTypeManager.IsCoreRuntimeType(rt2) )
                {
                    sobj = ObjectManager.CreateObjectByRuntimeType(rt2, false);
                }

                m_MemberRuntimeObjectArray[i] = new RuntimeObject(rt2, metaVariableList[i], sobj );
            }

            BuildMemberDataLayout();
        }
        public override void CreateObject()
        {
            base.CreateObject();

            var lengthSv = default(SValue);
            lengthSv.SetInt32Value(m_Length);
            SetMemberVariableSValue(0, lengthSv);

            CreateArray();
        }
        void CreateArray()
        {
            if(m_Length < 0 )
            {
                return;
            }
            m_Store = new ArrayObjectElementStore(eArrayType, m_Length);
#if DEBUG
            m_DebugArray = AllocateDebugArray();
            if (m_DebugArray != null && m_Store != null)
            {
                for (int i = 0; i < m_Length; i++)
                    DebugSyncIndex(i);
            }
#endif
        }

#if DEBUG
        private Array? AllocateDebugArray()
        {
            int length = m_Length;
            if (m_Length < 0) return null;
            return eArrayType.eType switch
            {
                EVMType.Boolean => new bool?[length],
                EVMType.UInt8 => new byte?[length],
                EVMType.Int8 => new sbyte?[length],
                EVMType.Int16 => new short?[length],
                EVMType.UInt16 => new ushort?[length],
                EVMType.Int32 => new int?[length],
                EVMType.UInt32 => new uint?[length],
                EVMType.Int64 => new long?[length],
                EVMType.UInt64 => new ulong?[length],
                EVMType.Float32 => new float?[length],
                EVMType.Float64 => new double?[length],
                EVMType.String => new String?[length],
                EVMType.Array => new ArrayObject[length],
                EVMType.Object => new SObject[length],
                EVMType.Type => new TypeObject[length],
                EVMType.Class => new ClassObject[length],
                _ => null,
            };
        }

        private void DebugSyncIndex(int index)
        {
            if (m_DebugArray == null || m_Store == null) return;
            m_DebugArray.SetValue(m_Store.GetBoxedValue(index), index);
        }
#endif

        public void LoadValue( int index, ref SValue sval )
        {
            if (index < 0)
            {
                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "loadvalue index < 0 ", index );
                return;
            }
            if (index >= m_Length )
            {
                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "loadvalue index >= length ", index );
                return;
            }
            m_Store?.LoadSValue(index, ref sval, eArrayType.eType);
        }

        public object? GetValue( int index )
        {
            if (index < 0)
            {
                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "getvalue index < 0 ", index);
                return null;
            }
            if (index >= m_Length)
            {
                Log.AddRuntimeLog(LID.RuntimeArrayIndexOutOfRange, "getvalue index >= length ", index);
                return null;
            }
            return m_Store?.GetBoxedValue(index);
        }
        public void StoreValue(int index, SValue svalue)
        {
            if (m_Store == null) return;
            SObject? anyobj = m_Store.GetSObjectAt(index) is SObject sobj && sobj.eType == EVMType.Object
                ? sobj
                : null;
            if( anyobj != null )
            {
                if( svalue.isNull )
                {
                    m_Store.SetObjectSlotToNull(index);
#if DEBUG
                    DebugSyncIndex(index);
#endif
                    return;
                }
            }
            if (m_Store.TryStoreCoercedNumber(index, svalue, eArrayType.eType))
            {
#if DEBUG
                DebugSyncIndex(index);
#endif
                return;
            }
            if (svalue.eType == EVMType.Null)
            {
                if (anyobj != null)
                {
                    m_Store.SetObjectSlotToNull(index);
                }
                else
                {
                    var nv = default(SValue);
                    nv.isNull = true;
                    m_Store.StoreFromSValue(index, nv, eArrayType.eType);
                }
#if DEBUG
                DebugSyncIndex(index);
#endif
                return;
            }
            switch (svalue.eType)
            {
                case EVMType.Boolean:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Boolean, svalue.int8Value == 1);
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
                case EVMType.UInt8:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.UInt8, svalue.int8Value );
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
                case EVMType.Int8:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Int8, svalue.int8Value );
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
                case EVMType.Int16:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Int16, svalue.int16Value);
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
                case EVMType.UInt16:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.UInt16, svalue.uint16Value );
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
                case EVMType.Int32:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Int32, svalue.int32Value);
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
                case EVMType.UInt32:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.UInt32, svalue.int32Value );
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
                case EVMType.Int64:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Int64, svalue.int64Value );
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
                case EVMType.UInt64:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.UInt64, svalue.uint64Value );
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
                case EVMType.Float32:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Float32, svalue.float32Value );
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
                case EVMType.Float64:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Float64, svalue.float64Value );
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
                case EVMType.String:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.String, svalue.stringValue );
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
                case EVMType.Array:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Array, svalue.sobject);
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
                case EVMType.Type:
                case EVMType.Class:
                    {
                        if (anyobj != null)
                        {
                            m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
#if DEBUG
                            DebugSyncIndex(index);
#endif
                            return;
                        }
                        m_Store.StoreFromSValue(index, svalue, eArrayType.eType);
                    }
                    break;
            }
#if DEBUG
            DebugSyncIndex(index);
#endif
        }
        internal static bool TryGetNumericAsDouble(SValue svalue, out double value)
        {
            value = 0;
            switch (svalue.eType)
            {
                case EVMType.UInt8:
                    value = svalue.uint8Value;
                    return true;
                case EVMType.Int8:
                    value = svalue.int8Value;
                    return true;
                case EVMType.Int16:
                    value = svalue.int16Value;
                    return true;
                case EVMType.UInt16:
                    value = svalue.uint16Value;
                    return true;
                case EVMType.Int32:
                    value = svalue.int32Value;
                    return true;
                case EVMType.UInt32:
                    value = svalue.uint32Value;
                    return true;
                case EVMType.Int64:
                    value = svalue.int64Value;
                    return true;
                case EVMType.UInt64:
                    value = svalue.uint64Value;
                    return true;
                case EVMType.Float32:
                    value = svalue.float32Value;
                    return true;
                case EVMType.Float64:
                    value = svalue.float64Value;
                    return true;
                default:
                    return false;
            }
        }
        public override string ToFormatString()
        {
            return $"Array ID: { m_Id } ";
        }
    }
}
