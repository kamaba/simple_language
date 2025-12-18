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
        public void SetClassObject(ClassObject val)
        {
            value = val;
            val.refCount++;
        }
        public void SetValue(EVMType _eType, System.Object val)
        {
            m_Type = _eType;
            value = val;
        }
        public override string ToFormatString()
        {
            return "";
        }
    }
}
