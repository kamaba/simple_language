//****************************************************************************
//  File:      MetaDynamicClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/5/30 12:00:00
//  Description: meta dynamic class  
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaDynamicClass : MetaClass
    {
        public MetaDynamicClass(string _name ) : base( _name )
        {
            m_Type = EType.Class;
        }
        public override void ParseDefineComplete()
        {
            base.ParseDefineComplete();
        }
        public override string ToFormatString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Clear();
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            
            //if (topLevelMetaNamespace != null)
            //{
            //    stringBuilder.Append(topLevelMetaNamespace.allName + ".");
            //}
            stringBuilder.Append(name + " ");

            stringBuilder.Append(Environment.NewLine);
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("{" + Environment.NewLine);

            //foreach (var v in m_ChildrenNameNodeDict )
            //{
            //    MetaBase mb = v.Value;
            //    if (mb is MetaMemberVariable )
            //    {
            //        stringBuilder.Append((mb as MetaMemberVariable ).ToFormatString());
            //        stringBuilder.Append(Environment.NewLine);
            //    }
            //}

            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("}" + Environment.NewLine);

            return stringBuilder.ToString();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
