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
        }
        public static void RegisterCallMethod(CallMethod cm)
        {
            if (callMethodList.Find(a => a.EqualCallMethod(cm)) == null)
            {
                callMethodList.Add(cm);
                RegisterMetaNode(cm);
            }
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
