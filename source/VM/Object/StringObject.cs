//****************************************************************************
//  File:      StringObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description:
//****************************************************************************

using SimpleLanguage.VM.Runtime;

namespace SimpleLanguage.VM
{
    public class StringObject : SObject
    {
        private string m_Value;
        public new string value => m_Value;
        public StringObject(string str) : base(RuntimeTypeManager.stringRuntimeType)
        {
            m_Header.EType = (byte)EVMType.String;
            m_Value = str;

            // Bind m_Value to String.sl's _value member field.
            // The _value field is of type String (reference), so store `this`.
            BindMemberVariables();
        }

        /// <summary>
        /// Binds the .sl-declared member fields to this object's data.
        /// String.sl declares `private String _value` — we store `this`
        /// so that .sl code accessing `this._value` gets the correct object.
        /// </summary>
        private void BindMemberVariables()
        {
            if (m_RuntimeType?.runtimeClass == null) return;

            var rc = m_RuntimeType.runtimeClass;
            var valueIndex = FindMemberIndex(rc, "_value");
            if (valueIndex >= 0)
            {
                var sv = default(RuntimeValue);
                sv.SetValueBySObject(this);
                SetMemberVariableSValue(valueIndex, sv);
            }
        }

        public void SetValue(string _val)
        {
            m_Value = _val;

            // Keep _value member in sync
            if (m_RuntimeType?.runtimeClass != null)
            {
                var valueIndex = FindMemberIndex(m_RuntimeType.runtimeClass, "_value");
                if (valueIndex >= 0)
                {
                    var sv = default(RuntimeValue);
                    sv.SetValueBySObject(this);
                    SetMemberVariableSValue(valueIndex, sv);
                }
            }
        }

        public override string ToFormatString()
        {
            return m_Value;
        }
    }
}
