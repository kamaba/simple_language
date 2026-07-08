//****************************************************************************
//  File:      BoolObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.VM.Runtime;

namespace SimpleLanguage.VM
{
    public class TemplateObject : SObject
    {
        public SObject instnceObject => m_InstnceObject;

        SObject m_InstnceObject = null;
        public TemplateObject() :base(  )
        {
        }
        public TemplateObject(RuntimeDefType val)
        {
        }
        public void SetClassObject(ClassObject val)
        {
            m_Type = EVMType.Class;
            m_Numeric = default;
            m_Reference = val;
            val.refCount++;
        }
        public void SetValue(EVMType _eType, System.Object val)
        {
            StoreValue(_eType, val);
        }
        public override string ToFormatString()
        {
            return "";
        }
    }
}
