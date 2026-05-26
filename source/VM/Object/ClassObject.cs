//****************************************************************************
//  File:      ClassObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/28 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Logging;
using SimpleLanguage.VM.Runtime;
using System.Text;

namespace SimpleLanguage.VM
{
    public class ClassObject : SObject
    {
        protected RuntimeObject[] m_MemberRuntimeObjectArray = null;
        protected List<RuntimeType> m_IRTemplateList = new List<RuntimeType>();
        protected byte[] m_MemberData = null;

        /// <summary>实例字段紧凑布局（静态字段见 <see cref="RuntimeType.memberData"/>）。</summary>
        public byte[]? memberData => m_MemberData;

        protected ClassObject() { }

        public ClassObject( RuntimeType irmt )
        {
            m_Id = ++idCount;
            m_RuntimeType = irmt;

            typeId = (short)m_RuntimeType.runtimeClass.id;
            m_IRTemplateList = irmt.runtimeTemplateList;

            var metaVariableList = m_RuntimeType.runtimeClass.nonStaticIRMetaVariableList;
            m_MemberRuntimeObjectArray = new RuntimeObject[metaVariableList.Count];
            for (int i = 0; i < m_MemberRuntimeObjectArray.Length; i++)
            {
                var rt = RuntimeVM.GetRuntimeTypeByDefType(metaVariableList[i].runtimeDefType, m_RuntimeType.runtimeClass, m_IRTemplateList, true);
                m_MemberRuntimeObjectArray[i] = new RuntimeObject( rt, metaVariableList[i], null);
            }
            BuildMemberDataLayout();
            //m_Type = new short[m_IRMetaVariableList.Count];
        }
        public virtual void CreateObject() { }

        /// <summary>实例成员与 IR 非静态字段顺序一致，使用与 <see cref="RuntimeClass.nonStaticIRMetaVariableList"/> 相同的下标。</summary>
        public RuntimeObject? GetMemberRuntimeObject(int memberIndex)
        {
            if (memberIndex < 0 || memberIndex >= m_MemberRuntimeObjectArray.Length)
                return null;
            return m_MemberRuntimeObjectArray[memberIndex];
        }
        /// <summary>按成员下标从 <see cref="memberData"/> 解析到 <paramref name="svalue"/>（引用型槽位为对象指针 Id，见 RuntimeObject）。</summary>
        public bool TryReadMemberDataAsSValue(int memberIndex, ref SValue svalue)
        {
            if (memberIndex < 0 || memberIndex >= m_MemberRuntimeObjectArray.Length)
                return false;
            return m_MemberRuntimeObjectArray[memberIndex].TryReadMemberDataToSValue(ref svalue);
        }
        protected void BuildMemberDataLayout()
        {
            if (m_MemberRuntimeObjectArray == null || m_MemberRuntimeObjectArray.Length == 0)
            {
                m_MemberData = null;
                return;
            }

            int n = m_MemberRuntimeObjectArray.Length;
            int totalBytes = 0;
            for (int i = 0; i < n; i++)
            {
                totalBytes += MemberDataLayout.GetSlotByteLength(m_MemberRuntimeObjectArray[i].runtimeType);
            }

            m_MemberData = totalBytes > 0 ? new byte[totalBytes] : null;
            int offset = 0;
            for (int i = 0; i < n; i++)
            {
                var ro = m_MemberRuntimeObjectArray[i];
                int len = MemberDataLayout.GetSlotByteLength(ro.runtimeType);
                ro.AttachMemberDataSlice(m_MemberData, offset, len, i);
                offset += len;
            }
        }
        public virtual void SetSValue(ClassObject val )
        {
            //m_Object = val;
            //m_IsNull = m_Object == null;
            val.refCount++;
        }
        /// <summary>从实例成员读取到 <paramref name="svalue"/>；与 <see cref="m_MemberData"/> 一致（同 <see cref="RuntimeType.GetStaticMemberVariableSValue"/> 静态侧）。</summary>
        public void GetMemberVariableSValue( int index, ref SValue svalue )
        {
            if (index < 0 )
            {
                Log.AddRuntimeLog(LID.AutoClassObjectL95, "执行的参数超出范围!! < 0 ");
                return;
            }
            if (m_MemberRuntimeObjectArray == null || index >= m_MemberRuntimeObjectArray.Length)
            {
                Log.AddRuntimeLog(LID.AutoClassObjectL100, "执行的参数超出范围!!");
                return;
            }
            m_MemberRuntimeObjectArray[index].SetSValueByRuntimeObjct(ref svalue);
        }
        /// <summary>实例成员写统一入口，同步 <see cref="m_MemberData"/>（同 <see cref="RuntimeType.SetStaticMemberVariableSValue"/> 静态侧）。</summary>
        public void SetMemberVariableSValue( int index, SValue svalue)
        {
            if (m_MemberRuntimeObjectArray == null || index < 0 || index >= m_MemberRuntimeObjectArray.Length)
            {
                Log.AddRuntimeLog(LID.AutoClassObjectL109, "执行的参数超出范围!!");
                return;
            }

            int targetIndex = ResolveCompatibleMemberIndex(index, ref svalue);
            m_MemberRuntimeObjectArray[targetIndex].SetSObjectBySValue(ref svalue);

        }

        private int ResolveCompatibleMemberIndex(int preferIndex, ref SValue svalue)
        {
            if (m_RuntimeType?.runtimeClass?.metaClassKind != 2)
                return preferIndex;

            if (preferIndex < 0 || preferIndex >= m_MemberRuntimeObjectArray.Length)
                return preferIndex;

            var preferRuntimeType = m_MemberRuntimeObjectArray[preferIndex]?.runtimeType;
            if (IsValueCompatibleWithRuntimeType(ref svalue, preferRuntimeType))
                return preferIndex;

            for (int i = 0; i < m_MemberRuntimeObjectArray.Length; i++)
            {
                if (i == preferIndex)
                    continue;

                var candidateType = m_MemberRuntimeObjectArray[i]?.runtimeType;
                if (IsValueCompatibleWithRuntimeType(ref svalue, candidateType))
                    return i;
            }

            return preferIndex;
        }

        private static bool IsValueCompatibleWithRuntimeType(ref SValue svalue, RuntimeType? expectedType)
        {
            if (expectedType == null)
                return false;

            if (svalue.isNull)
                return true;

            if (expectedType.runtimeClass?.metaClassKind == 2)
                return svalue.sobject is ClassObject;

            if (expectedType.eType == EVMType.Array)
                return svalue.sobject is ArrayObject || svalue.eType == EVMType.Array;

            if (expectedType.eType == EVMType.String)
                return svalue.eType == EVMType.String || svalue.sobject is StringObject;

            return true;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            //if (m_Object != null )
            //{
            //    sb.Append(m_Object.ToFormatString());
            //}
            sb.Append(m_RuntimeType.runtimeClass.ToString());
            //for( int i = 0; i < m_MemberVariableArray)

            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_RuntimeType.ToString());

            return sb.ToString();
         }
    }
}
