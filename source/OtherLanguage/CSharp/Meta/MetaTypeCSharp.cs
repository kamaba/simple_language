using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core
{
    public partial class MetaType
    {
        public static string GetClassNameByCSharpType(System.Type type)
        {
            if (type == null)
            {
                return null;
            }
            switch (type.Name)
            {
                case "Boolean":
                    return "Boolean";
                case "Byte":
                    return "Byte";
                case "Single":
                    return "Float";
                case "Double":
                    return "Double";
                default:
                    return type.Name;
            }
            return "Object";
        }
    }
}
