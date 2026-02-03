using SimpleLanguage.Core;
using System;
using System.Collections.Generic;


namespace SimpleLanguage.Lib
{
    public class RegisterCallMethodManager
    {
        public enum RegisterCallMethodLanuage
        {
            None,
            CSharpLang,
            JavaLang,
            CLang,
            CPlusPlusLang,
        }
        public class CallType
        {
            public EType eType = EType.None;
            public string typeName;

            public bool EqualCallType(CallType other)
            {
                if (eType != other.eType)
                {
                    return false;
                }
                if (typeName != other.typeName)
                {
                    return false;
                }
                return true;
            }
        }
        public class CallMethod
        {
            public RegisterCallMethodLanuage callMethodLanuage = RegisterCallMethodLanuage.None;
            public List<string> namespaceNameList = new List<string>();
            public List<string> topClassNameList = new List<string>();
            public string className;
            public string methodName;
            public CallType returnType = null;
            public List<CallType> argumentListType = new List<CallType>();

            public string GetFullMethodName()
            {
                string fullName = "";
                if (namespaceNameList.Count > 0)
                {
                    fullName += string.Join(".", namespaceNameList) + ".";
                }
                if (topClassNameList.Count > 0)
                {
                    fullName += string.Join(".", topClassNameList) + ".";
                }
                fullName += className + "." + methodName;
                return fullName;
            }

            public bool EqualCallMethod(CallMethod other)
            {
                if (GetFullMethodName() != other.GetFullMethodName())
                {
                    return false;
                }
                if ((returnType == null) != (other.returnType == null))
                {
                    return false;
                }

                for (int i = 0; i < argumentListType.Count; i++)
                {
                    if (i >= other.argumentListType.Count)
                    {
                        return false;
                    }
                    if (!argumentListType[i].EqualCallType(other.argumentListType[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public static List<CallMethod> callMethodList = new List<CallMethod>();
        public static void RegisterAllCallMethod()
        {
            CallMethod cm = new CallMethod()
            {
                callMethodLanuage = RegisterCallMethodLanuage.CSharpLang,
                namespaceNameList = new List<string>() { "", "" },
                topClassNameList = new List<string>() { },
                className = "",
                methodName = "",
                returnType = new CallType() { eType = EType.Void, typeName = "void" },
                argumentListType = new List<CallType>()
                  {
                      new CallType() { eType = EType.Int32, typeName = "int" },
                      new CallType() { eType = EType.String, typeName = "string" }
                  }
            };
            RegisterCallMethod(cm);
            CallMethod cm2add = new CallMethod()
            {
                callMethodLanuage = RegisterCallMethodLanuage.CLang,
                namespaceNameList = new List<string>() { "", "" },
                topClassNameList = new List<string>() { },
                className = "",
                methodName = "simplelanguage_addtest",
                returnType = new CallType() { eType = EType.Int32, typeName = "int" },
                argumentListType = new List<CallType>()
                  {
                      new CallType() { eType = EType.Int32, typeName = "int" },
                      new CallType() { eType = EType.Int32, typeName = "int" }
                  }
            };
            RegisterCallMethod(cm2add);

            ImportFromJson("ImportCSharpLang.json");
        }
        public static void RegisterCallMethod(CallMethod cm)
        {
            if (callMethodList.Find(a => a.EqualCallMethod(cm)) == null)
            {
                callMethodList.Add(cm);
                RegisterMetaNode(cm);
            }
        }
        // Import call methods from JSON file (produced by VM exporter)
        public static bool ImportFromJson(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return false;
            try
            {
                var json = System.IO.File.ReadAllText(path);
                var opts = new System.Text.Json.JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
                var list = System.Text.Json.JsonSerializer.Deserialize<List<CallMethodModel>>(json, opts);
                if (list == null) return false;
                foreach (var m in list)
                {
                    CallMethod cm = new CallMethod();
                    // map language
                    cm.callMethodLanuage = MapLanguage(m.callMethodLanguage);
                    if (m.namespaceNameList != null) cm.namespaceNameList = new List<string>(m.namespaceNameList);
                    if (m.topClassNameList != null) cm.topClassNameList = new List<string>(m.topClassNameList);
                    cm.className = m.className;
                    cm.methodName = m.methodName;
                    if (m.returnType != null)
                    {
                        cm.returnType = new CallType();
                        cm.returnType.typeName = m.returnType.typeName;
                        cm.returnType.eType = MapEType(m.returnType.eType);
                    }
                    if (m.argumentListType != null)
                    {
                        foreach (var at in m.argumentListType)
                        {
                            var atc = new CallType();
                            atc.typeName = at.typeName;
                            atc.eType = MapEType(at.eType);
                            cm.argumentListType.Add(atc);
                        }
                    }
                    RegisterCallMethod(cm);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        static RegisterCallMethodLanuage MapLanguage(string s)
        {
            if (string.IsNullOrEmpty(s)) return RegisterCallMethodLanuage.None;
            switch (s.Trim())
            {
                case "CSharpLang": return RegisterCallMethodLanuage.CSharpLang;
                case "JavaLang": return RegisterCallMethodLanuage.JavaLang;
                case "CLang": return RegisterCallMethodLanuage.CLang;
                case "CPlusPlusLang": return RegisterCallMethodLanuage.CPlusPlusLang;
                default: return RegisterCallMethodLanuage.None;
            }
        }

        static EType MapEType(string s)
        {
            if (string.IsNullOrEmpty(s)) return EType.None;
            var key = s.Trim().ToLowerInvariant();
            switch (key)
            {
                case "int": case "int32": return EType.Int32;
                case "string": return EType.String;
                case "void": return EType.Void;
                case "float": case "float32": return EType.Float32;
                case "double": case "float64": return EType.Float64;
                case "byte": return EType.Byte;
                case "sbyte": return EType.SByte;
                case "int16": case "short": return EType.Int16;
                case "uint16": return EType.UInt16;
                case "int64": case "long": return EType.Int64;
                case "uint32": return EType.UInt32;
                case "uint64": return EType.UInt64;
                case "boolean": case "bool": return EType.Boolean;
                default: return EType.None;
            }
        }

        // local DTO classes for JSON import
        public class CallMethodModel
        {
            public string callMethodLanguage { get; set; }
            public string[] namespaceNameList { get; set; }
            public string[] topClassNameList { get; set; }
            public string className { get; set; }
            public string methodName { get; set; }
            public CallTypeModel returnType { get; set; }
            public CallTypeModel[] argumentListType { get; set; }
        }
        public class CallTypeModel
        {
            public string eType { get; set; }
            public string typeName { get; set; }
        }
        public static void RegisterMetaNode(CallMethod node)
        {
            switch (node.callMethodLanuage)
            {
                case RegisterCallMethodLanuage.CSharpLang:
                    {
                        var module = ModuleManager.instance.csharpLangRegisterModule;
                    }
                    break;
                case RegisterCallMethodLanuage.JavaLang:
                    {
                        //LibMetaNodeManager.RegisterJavaCallMethod(node);
                    }
                    break;
                case RegisterCallMethodLanuage.CLang:
                    {
                        //LibMetaNodeManager.RegisterCCallMethod(node);
                    }
                    break;
                case RegisterCallMethodLanuage.CPlusPlusLang:
                    {
                        //LibMetaNodeManager.RegisterCPlusPlusCallMethod(node);
                    }
                    break;
            }
        }
    }
}
