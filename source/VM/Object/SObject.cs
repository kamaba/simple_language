//****************************************************************************
//  File:      SObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.VM
{
    public class SObject
    {
        public bool isNull => m_IsNull;
        public EType eType => m_Etype;
        protected EType m_Etype = EType.Class;
        public bool m_IsNull = false;
        public RuntimeType runtimeType => m_RuntimeType;
        public short typeId { get; set; } = 0;
        public int refCount { get; set; } = 0;
        protected RuntimeType m_RuntimeType = null;
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
