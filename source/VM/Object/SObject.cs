//****************************************************************************
//  File:      SObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.VM.Runtime;

namespace SimpleLanguage.VM
{
    public class SObject
    {
        public bool isNull => m_IsNull;
        public EVMType eType => m_Etype;
        public IRMetaClass irMetaClass => m_RuntimeType?.irClass;
        public RuntimeType runtimeType => m_RuntimeType;
        public short typeId { get; set; } = 0;
        public int refCount { get; set; } = 0;



        protected EVMType m_Etype = EVMType.Class;
        protected bool m_IsNull = false;
        protected RuntimeType m_RuntimeType = null;
        protected int m_Length = 0;
        public object value;
        public int id { get; set; } = 0;

        static int idCount = 0;
        protected SObject()
        {
            id = idCount++;
            value = this;
        }
        public SObject( EVMType etype )
        {
            this.m_Etype = etype;
        }
        public void SetValue(System.Object val)
        {
            m_IsNull = false;
            value = val;
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
