//****************************************************************************
//  File:      StringObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

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
        }
        public override void CreateObject()
        {
            base.CreateObject();

            eType = m_MemberObjectArray[0] as Int8Object;

            /*
            IRMetaClass irmc = IRManager.instance.GetIRMetaClassByName("MetaClass");
            RuntimeType rt = new RuntimeType(irmc, new List<RuntimeType>());

            ClassObject mco = new ClassObject(rt);
            mco.CreateObject();
            m_MemberObjectArray[1] = mco;
            metaClassObject = m_MemberObjectArray[1] as ClassObject;

            var sv = new SValue();
            sv.SetStringValue( m_Rt.runtimeClass.name );
            mco.SetMemberVariableSValue(0, sv );


            var sv2 = new SValue();
            sv2.SetStringValue( m_Rt.runtimeClass.name);
            mco.SetMemberVariableSValue(1, sv2);

            typeObjectsArray = new TypeObject[m_Rt.runtimeTemplateList.Count];
            for ( int i = 0; i < m_Rt.runtimeTemplateList.Count; i++ )
            {
                typeObjectsArray[i] = new TypeObject(m_Rt.runtimeTemplateList[i]);
                typeObjectsArray[i].CreateObject();
            }
            IRMetaClass arrayIRClass = IRManager.instance.GetIRMetaClassByName("Array<T>");

            List<RuntimeType> rtList = new List<RuntimeType>();
            rtList.Add(RuntimeTypeManager.typeRuntimeType);
            RuntimeType rtmain = new RuntimeType(arrayIRClass, rtList );

            if(typeObjectsArray.Length > 0 )
            {
                ArrayObject ao = new ArrayObject(rtmain, typeObjectsArray.Length);
                ao.CreateObject();
                for (int i = 0; i < ao.length; i++)
                {
                    ao.array.SetValue(typeObjectsArray[i], i);
                }
                m_MemberObjectArray[2] = ao;
            }
            else
            {
                m_MemberObjectArray[2] = null;
            }
            */
        }
        public override string ToFormatString()
        {
            return "";
        }
    }
}
