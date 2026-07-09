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
    class MemberObject : SObject
    {

        public MemberObject(string str) : base(EVMType.Member)
        {
            m_RuntimeType = RuntimeTypeManager.memberRuntimeType;
        }
        public override string ToFormatString()
        {
            return "member";
        }
    }
}
