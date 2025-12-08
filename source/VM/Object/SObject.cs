//****************************************************************************
//  File:      SObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;

namespace SimpleLanguage.VM
{
    public abstract class SObject
    {
        public bool isNull => m_IsNull;
        public EType eType => m_Etype;
        public IRMetaClass irMetaClass => m_RuntimeType?.irClass;
        public RuntimeType runtimeType => m_RuntimeType;
        public short typeId { get; set; } = 0;
        public int refCount { get; set; } = 0;



        protected EType m_Etype = EType.Class;
        protected bool m_IsNull = false;
        protected RuntimeType m_RuntimeType = null;
        protected int m_Length = 0; 
        public int id { get; set; } = 0;

        static int idCount = 0;
        protected SObject()
        {
            id = idCount++;
        }
        public SObject( EType etype )
        {
            this.m_Etype = etype;
        }
        public virtual void SetNull()
        {
            m_IsNull = true;
        }
        public virtual string ToFormatString()
        {
            return "";
        }
    }
}
