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
#if DEBUG
        protected RuntimeObject[] m_MemberRuntimeObjectArray = null;
#endif
        protected byte[] m_MemberData = null;

        /// <summary>ʵ���ֶν��ղ��֣���̬�ֶμ� <see cref="RuntimeType.memberData"/>����</summary>
        public byte[]? memberData => m_MemberData;

        protected ClassObject() { }

        public ClassObject( RuntimeType irmt )
        {
            m_Header = VMObjectHeader.Make((byte)EVMType.Class, VMObjectHeader.MetaKindRegular, 0);
            m_Header.Hash = (int)++idCount;
            m_RuntimeType = irmt;

            var metaVariableList = m_RuntimeType.runtimeClass.nonStaticIRMetaVariableList;
            m_MemberRuntimeObjectArray = new RuntimeObject[metaVariableList.Count];
            for (int i = 0; i < m_MemberRuntimeObjectArray.Length; i++)
            {
                var rt = RuntimeVM.GetRuntimeTypeByDefType(metaVariableList[i].runtimeDefType, m_RuntimeType.runtimeClass, irmt.runtimeTemplateList, true);
                m_MemberRuntimeObjectArray[i] = new RuntimeObject( rt, metaVariableList[i], null);
            }
            BuildMemberDataLayout();
            //m_Type = new short[m_IRMetaVariableList.Count];
        }
        public virtual void CreateObject() { }

        /// <summary>ʵ����Ա�� IR �Ǿ�̬�ֶ�˳��һ�£�ʹ���� <see cref="RuntimeClass.nonStaticIRMetaVariableList"/> ��ͬ���±ꡣ</summary>
        public RuntimeObject? GetMemberRuntimeObject(int memberIndex)
        {
            if (memberIndex < 0 || memberIndex >= m_MemberRuntimeObjectArray.Length)
                return null;
            return m_MemberRuntimeObjectArray[memberIndex];
        }
        /// <summary>����Ա�±�� <see cref="memberData"/> ������ <paramref name="RuntimeValue"/>�������Ͳ�λΪ����ָ�� Id���� RuntimeObject����</summary>
        public bool TryReadMemberDataAsSValue(int memberIndex, ref RuntimeValue RuntimeValue)
        {
            if (memberIndex < 0 || memberIndex >= m_MemberRuntimeObjectArray.Length)
                return false;
            return m_MemberRuntimeObjectArray[memberIndex].TryReadMemberDataToSValue(ref RuntimeValue);
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
        /// <summary>��ʵ����Ա��ȡ�� <paramref name="RuntimeValue"/>���� <see cref="m_MemberData"/> һ�£�ͬ <see cref="RuntimeType.GetStaticMemberVariableSValue"/> ��̬�ࣩ��</summary>
        public void GetMemberVariableSValue( int index, ref RuntimeValue RuntimeValue )
        {
            if (index < 0 )
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "ִ�еĲ���������Χ!! < 0 ");
                return;
            }
            if (m_MemberRuntimeObjectArray == null || index >= m_MemberRuntimeObjectArray.Length)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "ִ�еĲ���������Χ!!");
                return;
            }
            m_MemberRuntimeObjectArray[index].SetSValueByRuntimeObjct(ref RuntimeValue);
        }
        /// <summary>ʵ����Աдͳһ��ڣ�ͬ�� <see cref="m_MemberData"/>��ͬ <see cref="RuntimeType.SetStaticMemberVariableSValue"/> ��̬�ࣩ��</summary>
        public void SetMemberVariableSValue( int index, RuntimeValue RuntimeValue)
        {
            if (m_MemberRuntimeObjectArray == null || index < 0 || index >= m_MemberRuntimeObjectArray.Length)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "ִ�еĲ���������Χ!!");
                return;
            }

            int targetIndex = ResolveCompatibleMemberIndex(index, ref RuntimeValue);
            m_MemberRuntimeObjectArray[targetIndex].SetSObjectBySValue(ref RuntimeValue);

        }

        private int ResolveCompatibleMemberIndex(int preferIndex, ref RuntimeValue RuntimeValue)
        {
            if (m_RuntimeType?.runtimeClass?.metaClassKind != 2)
                return preferIndex;

            if (preferIndex < 0 || preferIndex >= m_MemberRuntimeObjectArray.Length)
                return preferIndex;

            var preferRuntimeType = m_MemberRuntimeObjectArray[preferIndex]?.runtimeType;
            if (IsValueCompatibleWithRuntimeType(ref RuntimeValue, preferRuntimeType))
                return preferIndex;

            for (int i = 0; i < m_MemberRuntimeObjectArray.Length; i++)
            {
                if (i == preferIndex)
                    continue;

                var candidateType = m_MemberRuntimeObjectArray[i]?.runtimeType;
                if (IsValueCompatibleWithRuntimeType(ref RuntimeValue, candidateType))
                    return i;
            }

            return preferIndex;
        }

        private static bool IsValueCompatibleWithRuntimeType(ref RuntimeValue RuntimeValue, RuntimeType? expectedType)
        {
            if (expectedType == null)
                return false;

            if (RuntimeValue.isNull)
                return true;

            if (expectedType.runtimeClass?.metaClassKind == 2)
                return RuntimeValue.sobject is ClassObject;

            if (expectedType.eType == EVMType.Array)
                return RuntimeValue.sobject is ArrayObject || RuntimeValue.eType == EVMType.Array;

            if (expectedType.eType == EVMType.String)
                return RuntimeValue.eType == EVMType.String || RuntimeValue.sobject is StringObject;

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
