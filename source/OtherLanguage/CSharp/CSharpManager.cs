//****************************************************************************
//  File:      CSharpManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/15 12:00:00
//  Description: Manager CSharp Interface About!
//****************************************************************************

using SimpleLanguage.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SimpleLanguage.Compile.CoreFileMeta;
using System.Data;

namespace SimpleLanguage.CSharp
{
    class CSharpManager
    {
        static CSharpManager()
        {

        }
        public static MetaBase GetCSharpFunction( MetaBase mb, string name )
        {
            if (mb is MetaClass)
            {
                MetaClass mc = mb as MetaClass;

                Type type = mc.csharpType;

                PropertyInfo pi = type.GetProperty(name);

                if (pi != null)
                {
                    MetaMemberVariable mv = new MetaMemberVariable(mc, pi);

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
            System.Reflection.Assembly[] all = AppDomain.CurrentDomain.GetAssemblies();

            string allName = name;
            for (int k = 0; k < all.Length; k++)
            {
                Assembly am = all[k];
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
        public static MetaBase FindAndCreateMetaBase( MetaBase mb, string name )
        {
            System.Reflection.Assembly[] all = AppDomain.CurrentDomain.GetAssemblies();

            string allName = name;
            if (mb != null)
            {
                if( mb is MetaModule )
                {

                }
                else
                {
                    allName = mb.allName + "." + name;
                }
            }
            for( int k = 0; k < all.Length; k++ )
            {
                Assembly am = all[k];

                var ttype = am.GetType(allName);
                if (ttype != null )
                {
                    if (ttype.IsClass)
                    {
                        MetaClass mc = new MetaClass(ttype.Name, ttype);
                        mc.SetRefFromType(RefFromType.CSharp);
                        if (mb is MetaModule)
                        {
                            MetaModule mm = mb as MetaModule;
                            if (mm != null)
                            {
                                mm.AddMetaClass(mc);
                            }
                        }
                        if (mb is MetaNamespace)
                        {
                            MetaNamespace mn = mb as MetaNamespace;
                            if (mn != null)
                            {
                                mn.AddMetaClass(mc);
                            }
                        }
                        else if (mb is MetaClass)
                        {
                            MetaClass gmc = mb as MetaClass;
                            gmc.AddChildrenMetaClass(mc);
                        }
                        ClassManager.instance.AddDictMetaClass(mc);
                        return mc;
                    }
                    else if (ttype.IsTypeDefinition)
                    {
                        MetaNamespace nmn = new MetaNamespace(ttype.Name);
                        nmn.SetRefFromType(RefFromType.CSharp);
                        if (mb is MetaNamespace)
                        {
                            MetaNamespace mn = mb as MetaNamespace;
                            if (mn != null)
                            {
                                mn.AddMetaNamespace(nmn);
                                return nmn;
                            }
                        }
                    }
                }

                /*
                var types = am.GetExportedTypes();
                for( int i = 0; i < types.Length; i++ )
                {
                    var type = types[i];
                    if( type.FullName == allName )
                    {
                        //Console.WriteLine("CSharp FindAndCreateMetaBase Find Type Successed!!");
                        if( type.IsClass )
                        {
                            MetaClass mc = new MetaClass(type.Name, type );
                            mc.SetRefFromType(RefFromType.CSharp);
                            if( mb is MetaModule )
                            {
                                MetaModule mm = mb as MetaModule;
                                if( mm != null )
                                {
                                    mm.AddMetaClass(mc);
                                }
                            }
                            if ( mb is MetaNamespace )
                            {
                                MetaNamespace mn = mb as MetaNamespace;
                                if( mn != null )
                                {
                                    mn.AddMetaClass(mc);
                                }
                            }
                            else if( mb is MetaClass )
                            {
                                MetaClass gmc = mb as MetaClass;
                                gmc.AddChildrenMetaClass(mc);
                            }
                            ClassManager.instance.AddDictMetaClass(mc);
                            return mc;
                        }
                        else if( type.IsTypeDefinition )
                        {
                            MetaNamespace nmn = new MetaNamespace(type.Name);
                            nmn.SetRefFromType(RefFromType.CSharp);
                            if (mb is MetaNamespace)
                            {
                                MetaNamespace mn = mb as MetaNamespace;
                                if (mn != null)
                                {
                                    mn.AddMetaNamespace(nmn);
                                    return nmn;
                                }
                            }
                        }
                    }
                }
                */
            }
            /*
            MetaNamespace gnmn = new MetaNamespace(name);
            gnmn.SetRefFromType(RefFromType.CSharp);
            MetaModule gmm = mb as MetaModule;
            MetaNamespace gmn = mb as MetaNamespace;
            if (gmm != null )
            {
                gmm.AddMetaNamespace(gnmn);
            }
            else if( gmn != null )
            {
                gmn.AddMetaNamespace(gnmn);
            }
            return gnmn;
            */
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
