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
    public class TypeObject : ClassObject
    {
        public RuntimeType currentRT => m_Rt;

        RuntimeType m_Rt = null;

        ClassObject metaClassObject = null;

        public TypeObject(RuntimeType rm ) : base(RuntimeTypeManager.typeRuntimeType)
        {
            m_Rt = rm;
            m_Type = EVMType.Type;
        }
        public override void CreateObject()
        {
            if (m_Rt == null || m_RuntimeType?.runtimeClass == null) return;

            var typeRc = m_RuntimeType.runtimeClass;

            var eTypeIndex = FindMemberIndex(typeRc, "_eType");
            if (eTypeIndex >= 0)
            {
                var sv = default(RuntimeValue);
                sv.SetUInt8Value((byte)m_Rt.eType);
                SetMemberVariableSValue(eTypeIndex, sv);
            }

            metaClassObject = CreateMetaClassObject();
            var metaClassIndex = FindMemberIndex(typeRc, "_metaClass");
            if (metaClassIndex >= 0)
            {
                var sv = default(RuntimeValue);
                if (metaClassObject != null) sv.SetValueBySObject(metaClassObject);
                else sv.SetNull();
                SetMemberVariableSValue(metaClassIndex, sv);
            }

            var typeListIndex = FindMemberIndex(typeRc, "typelist");
            if (typeListIndex >= 0)
            {
                var typeListObj = CreateTemplateTypeListObject();
                var sv = default(RuntimeValue);
                if (typeListObj != null) sv.SetValueBySObject(typeListObj);
                else sv.SetNull();
                SetMemberVariableSValue(typeListIndex, sv);
            }
        }

        private ClassObject? CreateMetaClassObject()
        {
            var metaRc = TryGetRuntimeClassByNames("Core.MetaClass", "MetaClass");
            if (metaRc == null) return null;

            var metaRt = RuntimeTypeManager.GetRuntimeTypeByRuntimeClassAndRuntimeTypeList(metaRc, new List<RuntimeType>())
                         ?? RuntimeTypeManager.AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(metaRc, new List<RuntimeType>());
            if (metaRt == null) return null;

            var metaObj = ObjectManager.CreateObjectByRuntimeType(metaRt, true) as ClassObject;
            if (metaObj == null) return null;

            string fullName = m_Rt.runtimeClass?.name ?? string.Empty;
            string ns = string.Empty;
            string cls = fullName;
            int dot = fullName.LastIndexOf('.');
            if (dot >= 0)
            {
                ns = fullName.Substring(0, dot);
                cls = fullName.Substring(dot + 1);
            }

            int nsIndex = FindMemberIndex(metaRc, "_namespaceName");
            if (nsIndex >= 0)
            {
                var svNs = default(RuntimeValue);
                svNs.SetStringValue(ns);
                metaObj.SetMemberVariableSValue(nsIndex, svNs);
            }
            int clsIndex = FindMemberIndex(metaRc, "_className");
            if (clsIndex >= 0)
            {
                var svCls = default(RuntimeValue);
                svCls.SetStringValue(cls);
                metaObj.SetMemberVariableSValue(clsIndex, svCls);
            }

            return metaObj;
        }

        private ArrayObject? CreateTemplateTypeListObject()
        {
            var templates = m_Rt.runtimeTemplateList;
            if (templates == null || templates.Count == 0) return null;

            var typeRt = RuntimeTypeManager.typeRuntimeType;
            if (typeRt == null) return null;

            var arrayRc = TryGetRuntimeClassByNames("Core.Array<T>", "Core.Array", "Array");
            if (arrayRc == null) return null;

            var arrayRt = RuntimeTypeManager.GetRuntimeTypeByRuntimeClassAndRuntimeTypeList(arrayRc, new List<RuntimeType> { typeRt })
                         ?? RuntimeTypeManager.AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(arrayRc, new List<RuntimeType> { typeRt });
            if (arrayRt == null) return null;

            var arr = new ArrayObject(arrayRt, templates.Count);
            arr.CreateObject();
            for (int i = 0; i < templates.Count; i++)
            {
                var child = RuntimeTypeManager.CreateTypeObject(templates[i]);
                var sv = default(RuntimeValue);
                if (child != null) sv.SetValueBySObject(child);
                else sv.SetNull();
                arr.StoreValue(i, sv);
            }
            return arr;
        }

        private static RuntimeClass? TryGetRuntimeClassByNames(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                if (string.IsNullOrWhiteSpace(name)) continue;
                var rc = RuntimeClassManager.GetRuntimeClassByName(name);
                if (rc != null) return rc;
            }
            return null;
        }

        private static int FindMemberIndex(RuntimeClass? rc, string target)
        {
            if (rc == null || string.IsNullOrEmpty(target)) return -1;
            var list = rc.nonStaticIRMetaVariableList;
            for (int i = 0; i < list.Count; i++)
            {
                var rv = list[i];
                if (rv == null) continue;
                var n = rv.name ?? string.Empty;
                if (string.Equals(n, target, StringComparison.Ordinal)
                    || n.EndsWith(target, StringComparison.Ordinal)
                    || n.Contains(target, StringComparison.Ordinal))
                    return rv.index >= 0 ? rv.index : i;
            }
            return -1;
        }
        public override string ToFormatString()
        {
            return ToString();
        }

        /// <summary>
        /// Describe the <i>subject</i> type (<see cref="m_Rt"/>), not the <c>Type</c> class itself (base would use <see cref="RuntimeTypeManager.typeRuntimeType"/>).
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Type:");
            if (m_Rt != null)
                return m_Rt.ToString();
            return base.ToString();
        }
    }
}
