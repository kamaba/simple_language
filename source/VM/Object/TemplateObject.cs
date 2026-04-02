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
        public TemplateObject() :base(  )
        {
        }
        public TemplateObject(RuntimeDefType val)
        {
        }
        public void SetClassObject(ClassObject val)
        {
            m_Value = val;
            val.refCount++;
        }
        public void SetValue(EVMType _eType, System.Object val)
        {
            m_Type = _eType;
            m_Value = val;
        }
        public override string ToFormatString()
        {
            return "";
        }
    }
}
