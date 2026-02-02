//****************************************************************************
//  File:      SObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.Runtime;

namespace SimpleLanguage.VM
{
    public class SObject
    {
        public bool isNull => m_IsNull;
        public EVMType eType => m_Type;
        public EVMType eAnyType => m_AnyType;

        public virtual object value 
        {
            get
            {
                return m_Value;
            } 
        }
        public RuntimeClass runtimeClass => m_RuntimeType?.runtimeClass;
        public RuntimeType runtimeType => m_RuntimeType;
        public short typeId { get; set; } = 0;
        public int refCount { get; set; } = 0;


        protected EVMType m_Type = EVMType.Class;
        protected EVMType m_AnyType = EVMType.Class;

        protected bool m_IsNull = false;
        protected RuntimeType m_RuntimeType = null;
        protected int m_Length = 0;
        protected object m_Value = null;
        public int id { get; set; } = 0;

        static int idCount = 0;
        protected SObject()
        {
            id = idCount++;
            m_Value = this;
        }
        public SObject( EVMType etype )
        {
            this.m_Type = etype;
        }
        public void SetValue(System.Object val)
        {
            m_IsNull = false;
            m_Value = val;
        }
        public void SetValueByType(EVMType vmType, System.Object val)
        {
            m_AnyType = vmType;
            m_IsNull = false;
            m_Value = val;
            refCount++;
        }
        public virtual void SetNull()
        {
            m_IsNull = true;
        }
        public virtual string ToFormatString()
        {
            return "";
            //return value.ToString();
        }
    }
}
