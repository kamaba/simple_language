//****************************************************************************
//  File:      AttributeManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/3/1 12:00:00
//  Description: attribute execution/check hooks
//****************************************************************************

using SimpleLanguage.Logging;
using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    public enum EAttributeHook
    {
        BeforeRun,
        BeforeNew,
        BeforeCall,
        BeforeGet,
        BeforeSet,
    }

    public static class AttributeManager
    {
        public static bool ExecuteByName(EAttributeHook hook, string owner, List<MetaAttribute> list)
        {
            return Execute(hook, list, owner);
        }

        public static bool Execute(EAttributeHook hook, MetaClass mc)
        {
            if (mc == null) return true;
            return Execute(hook, mc.attributeList, mc.allClassName);
        }

        public static bool Execute(EAttributeHook hook, MetaMemberFunction mmf)
        {
            if (mmf == null) return true;
            return Execute(hook, mmf.attributeList, mmf.functionAllName);
        }

        public static bool Execute(EAttributeHook hook, MetaMemberVariable mmv)
        {
            if (mmv == null) return true;
            return Execute(hook, mmv.attributeList, mmv.ToString());
        }

        private static bool Execute(EAttributeHook hook, List<MetaAttribute> list, string owner)
        {
            if (list == null || list.Count == 0) return true;

            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (a == null || string.IsNullOrEmpty(a.name)) continue;

                // Placeholder: for now, attributes only participate in tracing.
                // Future: map attribute name -> handler function/class and execute with params.
                Log.AddMetaCoreLog(LID.AutoAttributeManagerL59, $"AttributeHook {hook} owner:{owner} attr:{a.name}");
            }
            return true;
        }
    }
}
