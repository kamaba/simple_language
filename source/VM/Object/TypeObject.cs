//****************************************************************************
//  File:      StringObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Core;
using SimpleLanguage.VM.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.VM
{
    class TypeObject : ClassObject
    {
        public RuntimeType currentRT => m_Rt;

        RuntimeType m_Rt = null;

        Int8Object eType = null;
        ClassObject metaClassObject = null;

        TypeObject[] typeObjectsArray = null;
        public TypeObject(RuntimeType rm ) : base(RuntimeTypeManager.typeRuntimeType)
        {
            m_Rt = rm;
            m_Type = EVMType.Type;
            CreateDefine();

            eType = m_MemberObjectArray[0] as Int8Object;
            metaClassObject = m_MemberObjectArray[1] as ClassObject;
            ArrayObject ao = m_MemberObjectArray[2] as ArrayObject;
            if( ao != null )
            {
                typeObjectsArray = new TypeObject[ao.array.Length];
                for( int i = 0; i < ao.array.Length; i++ )
                {
                    typeObjectsArray[i] = ao.array.GetValue(i) as TypeObject;
                }
            }
        }
        public override string ToFormatString()
        {
            return "";
        }
    }
}
