//****************************************************************************
//  File:      BoolObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
namespace SimpleLanguage.VM
{
    public class TemplateObject : SObject
    {
        private ClassObject m_ExtendObject = null;

        public object value;
        public TemplateObject() :base(  )
        {
        }
        public void SetClassObject(ClassObject val)
        {
            value = val;
            val.refCount++;
        }
        public void SetValue(EType _eType, System.Object val)
        {
            m_Etype = _eType;
            value = val;
            m_IsVoid = false;
        }
        public override string ToFormatString()
        {
            return "";
        }
    }
}
