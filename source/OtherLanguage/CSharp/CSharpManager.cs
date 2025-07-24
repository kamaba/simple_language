//****************************************************************************
//  File:      CSharpManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/15 12:00:00
//  Description: Manager CSharp Interface About!
//****************************************************************************

using SimpleLanguage.Core;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SimpleLanguage.Compile.CoreFileMeta;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SimpleLanguage.CSharp
{
    class CSharpManager
    {
        static System.Reflection.Assembly[] allAssembly = AppDomain.CurrentDomain.GetAssemblies();

        static List<Assembly> canSearchAssemblyList = new List<Assembly>();
        static CSharpManager()
        {
            InitCanSearchAssemblyList();
        }
        public static void InitCanSearchAssemblyList()
        {
            foreach( var am in allAssembly )
            {
                if (am.FullName != "SimpleLanguage, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
                        && am.FullName != "System.Console, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
                {
                    continue;
                }
                canSearchAssemblyList.Add(am);
            }

        }
        public static MetaBase GetCSharpFunction( MetaBase mb, string name )
        {
            if (mb is MetaClass)
            {
                MetaClassCSharp mc = mb as MetaClassCSharp;

                Type type = mc.csharpType;

                PropertyInfo pi = type.GetProperty(name);

                if (pi != null)
                {
                    MetaMemberVariableCSharp mv = new MetaMemberVariableCSharp(mc, pi);

                    mc.AddMetaMemberVariable(mv);

                    return mv;
                }
                //MethodInfo mi = type.GetMethod(name);
                //if (mi != null)
                //{
                //    MetaMemberFunction mmf = new MetaMemberFunction(mc, mi);

                //    mc.AddMetaMemberFunction(mmf);

                //    return mmf;
                //}
            }
            return null;
        }
        public static Type FindCSharpType( string name )
        {
            string allName = name;
            for (int k = 0; k < canSearchAssemblyList.Count; k++)
            {
                Assembly am = canSearchAssemblyList[k];
                var modules = am.GetModules();
                var types = am.GetExportedTypes();
                var typeInfos = am.DefinedTypes;
                for (int i = 0; i < types.Length; i++)
                {
                    var type = types[i];
                    if (type.FullName == allName)
                    {
                        return type;
                    }
                }
            }
            return null;
        }
        public static bool IsFindMetaCSharpNamespace( string allName )
        {
            for (int k = 0; k < canSearchAssemblyList.Count; k++)
            {
                Assembly am = canSearchAssemblyList[k];
                var gts = am.GetTypes();
                string[] namespaces = gts
                    .Select(t => t.Namespace)
                    .Where(ns => !string.IsNullOrEmpty(ns))
                    .Distinct()
                    .ToArray();

                for (int l = 0; l < namespaces.Length; l++)
                {
                    string cuns = namespaces[l];
                    if (cuns == allName)
                        return true;
                }
            }
            return false;
        }
        public static MetaNode FindAndCreateMetaNode(MetaNode mb, string name )
        {
            string frontName = "";
            if (mb != null)
            {
                if( mb.isMetaModule )
                {

                }
                else
                {
                    frontName = mb.allName;
                }
            }
            MetaNode getmb = FindCSharpClassOrNameSpace(frontName, name);
            if (getmb == null) return null;

            getmb.AddMetaNode(mb);
            
            return getmb;
        }
        public static MetaNode FindCSharpClassOrNameSpace( string frontName, string name )
        {
            string allName = frontName + "." + name;
            for (int k = 0; k < canSearchAssemblyList.Count; k++)
            {
                Assembly am = canSearchAssemblyList[k];
                Type ttype = null;
                foreach (var x in am.DefinedTypes)
                {
                    if (x.FullName == allName)
                    {
                        ttype = x;
                        break;
                    }
                }
                if (ttype != null)
                {
                    if (ttype.IsClass)
                    {
                        MetaClassCSharp mc = new MetaClassCSharp(ttype.Name, ttype);                        
                        return new MetaNode(mc);
                    }
                    else if (ttype.IsTypeDefinition)
                    {
                        MetaNamespaceCSharp nmn = new MetaNamespaceCSharp(ttype.Name);
                        return new MetaNode(nmn);
                    }
                }
                else
                {
                    string[] namespaces = am.GetTypes()
                        .Select(t => t.Namespace)
                        .Where(ns => !string.IsNullOrEmpty(ns))
                        .Distinct()
                        .ToArray();

                    for (int l = 0; l < namespaces.Length; l++)
                    {
                        string cuns = namespaces[l];
                        if (cuns == allName)
                        {
                            MetaNamespaceCSharp nmn = new MetaNamespaceCSharp(name);
                            return new MetaNode(nmn);
                        }
                    }
                }
            }
            return null;
        }
        public static Object GetObject(FileMetaClassDefine fmcv, MetaNamespace mn)
        {
            //if (mb == null) return null;
            //MetaBase mb2 = mb;
            //var list = stringList;
            //for (int i = 0; i < list.Count; i++)
            //{
            //    mb2 = mb2.GetMetaBaseByName(list[i]);
            //    if (mb2 == null)
            //        break;
            //}
            //return mb2;

            return new Object();
        }
    }
}


namespace Common.CSharp
{
    public class CSharpUtils
    {
        public static void WrapMethod(Type input, string name, Type injectType, string injectMethod)
        {
            // Generate ILasm for delegate.
            BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            Binder binder = null;
            CallingConventions callConvention = CallingConventions.Any;
            Type[] types = Type.EmptyTypes;
            ParameterModifier[] modifiers = null;

            MethodInfo mi1 = input.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
            if (mi1 == null)
                return;
            MethodBody mb = mi1.GetMethodBody();
            if (mb == null) return;
            byte[] il = mb.GetILAsByteArray();

            MethodInfo injMethod = injectType.GetMethod(injectMethod, bindingAttr, binder, callConvention, types, modifiers);

            // Pin the bytes in the garbage collection.
            GCHandle h = GCHandle.Alloc((object)il, GCHandleType.Pinned);
            IntPtr addr = h.AddrOfPinnedObject();
            int size = il.Length;

            // Swap the method.
            //MethodRental.SwapMethodBody( injectType, injMethod.MetadataToken, addr, size, MethodRental.JitImmediate );
        }

        public static bool ReplaceMethod(Type targetType, string targetMethod, Type injectType, string injectMethod, BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, Binder binder = null, CallingConventions callConvention = CallingConventions.Any, Type[] types = null, ParameterModifier[] modifiers = null)
        {
            if (types == null)
            {
                types = Type.EmptyTypes;
            }
            MethodInfo tarMethod = targetType.GetMethod(targetMethod, bindingAttr, binder, callConvention, types, modifiers);
            MethodInfo injMethod = injectType.GetMethod(injectMethod, bindingAttr, binder, callConvention, types, modifiers);
            if (tarMethod == null || injMethod == null)
            {
                return false;
            }
            RuntimeHelpers.PrepareMethod(tarMethod.MethodHandle);
            RuntimeHelpers.PrepareMethod(injMethod.MethodHandle);
            unsafe
            {
                if (IntPtr.Size == 4)
                {
//                    int* tar = (int*)tarMethod.MethodHandle.Value.ToPointer() + 2;
//                    int* inj = (int*)injMethod.MethodHandle.Value.ToPointer() + 2;

//#if DEBUG
//                    //Debug.Log("Version x86 Debug");
//                    byte* injInst = (byte*)*inj;
//                    byte* tarInst = (byte*)*tar;
//                    int* injSrc = (int*)(injInst + 1);
//                    int* tarSrc = (int*)(tarInst + 1);
//                    *tarSrc = (((int)injInst + 5) + *injSrc) - ((int)tarInst + 5);
//#else
//                    //Debug.Log("Version x86 Relaese");
//                    //*tar = *inj;
//#endif
                }
                else
                {
//                    long* tar = (long*)tarMethod.MethodHandle.Value.ToPointer() + 1;
//                    long* inj = (long*)injMethod.MethodHandle.Value.ToPointer() + 1;
//#if DEBUG
//                    //Debug.Log("Version x64 Debug");
//                    byte* injInst = (byte*)*inj;
//                    byte* tarInst = (byte*)*tar;
//                    int* injSrc = (int*)(injInst + 1);
//                    int* tarSrc = (int*)(tarInst + 1);
//                    //*tarSrc = (((int)injInst + 5) + *injSrc) - ((int)tarInst + 5);

//                    *((int*)new IntPtr(((int*)tarMethod.MethodHandle.Value.ToPointer() + 2)).ToPointer()) = injMethod.MethodHandle.GetFunctionPointer().ToInt32();
//#else
//                    //Debug.Log("Version x64 Relaese");
//                   // *tar = *inj;
//#endif
                }
            }
            return true;
        }


        public static void Call(string typeName, string methodName, params object[] args)
        {
            Call<object>(typeName, methodName, args);
        }
        public static T Call<T>(string typeName, string methodName, params object[] args)
        {
            Type type = Type.GetType(typeName);
            T defaultValue = default(T);
            if (null == type)
                return defaultValue;

            Type[] argTypes = new Type[args.Length];
            for (int i = 0, count = args.Length; i < count; ++i)
            {
                argTypes[i] = null != args[i] ? args[i].GetType() : null;
            }
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, argTypes, null);
            if (null == method)
            {
                //Debug.Log(string.Format("method {0} does not exist!", methodName));
                return defaultValue;
            }
            object result = method.Invoke(null, args);
            if (null == result)
                return defaultValue;
            if (!(result is T))
            {
                //Debug.Log(string.Format("method {0} cast failed!", methodName));
                return defaultValue;
            }
            return (T)result;
        }
        public static T GetProperty<T>(string typeName, string propertyName)
        {
            return GetProperty<T>(null, typeName, propertyName);
        }
        public static T GetProperty<T>(object instance, string typeName, string propertyName)
        {
            bool isStatic = null == instance;
            Type type = Type.GetType(typeName);
            T defaultValue = default(T);
            if (null == type)
                return defaultValue;

            BindingFlags flag = (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            PropertyInfo property = type.GetProperty(propertyName, flag | BindingFlags.Public | BindingFlags.NonPublic);
            if (null == property)
            {
                //Debug.Log(string.Format("property {0} does not exist!", propertyName));
                return defaultValue;
            }
            object result = property.GetValue(instance, null);
            if (null == result)
                return defaultValue;
            if (!(result is T))
            {
                //Debug.Log(string.Format("property {0} cast failed!", propertyName));
                return defaultValue;
            }
            return (T)result;
        }
        public static T GetField<T>(string typeName, string fieldName)
        {
            return GetField<T>(null, typeName, fieldName);
        }
        public static T GetField<T>(object instance, string typeName, string fieldName)
        {
            bool isStatic = null == instance;
            Type type = Type.GetType(typeName);
            T defaultValue = default(T);
            if (null == type)
                return defaultValue;

            BindingFlags flag = (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            FieldInfo field = type.GetField(fieldName, flag | BindingFlags.Public | BindingFlags.NonPublic);
            if (null == field)
            {
                //Debug.LogError(string.Format("field {0} does not exist!", fieldName));
                return defaultValue;
            }
            object result = field.GetValue(instance);
            if (null == result)
                return defaultValue;
            if (!(result is T))
            {
                //Debug.LogError(string.Format("field {0} cast failed!", fieldName));
                return defaultValue;
            }
            return (T)result;
        }
    }
}
