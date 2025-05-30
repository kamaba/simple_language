//****************************************************************************
//  File:      TemplateMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: Generator Template Class's entity by Template Class
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile;

namespace SimpleLanguage.Core
{
    public class MetaGenTemplateClass : MetaClass
    {
        public override bool isGenTemplate => true;

        protected Dictionary<string,MetaGenTemplate> m_MetaGenTemplateDict = new Dictionary<string,MetaGenTemplate>();
        public MetaGenTemplateClass(MetaClass mc) : base(mc)
        {
        }
        public override void SetDeep(int deep)
        {
            m_Deep = deep;
            foreach (var v in m_MetaMemberVariableDict)
            {
                v.Value.SetDeep(m_Deep + 1);
            }

            foreach (var v in m_MetaMemberFunctionListDict)
            {
                foreach (var v2 in v.Value)
                {
                    v2.SetDeep(m_Deep + 1);
                }
            }
        }
        public MetaType GetGenTemplateByIndex( int index )
        {
            int i = 0;
            foreach( var v in m_MetaGenTemplateDict )
            {
                if( i == index )
                {
                    return v.Value.metaType;
                }
                i++;
            }
            return null;
        }

        public void GetMetaTemplateMT( Dictionary<string, MetaType> mtdict )
        {
            foreach( var v in m_MetaGenTemplateDict )
            {
                var cmg = v.Value;
                if(mtdict.ContainsKey(cmg.name ))
                {
                    continue;
                }
                mtdict.Add(cmg.name, cmg.metaType);
            }
        }
        public override MetaMemberVariable GetMetaMemberVariableByName(string name)
        {
            if (m_MetaMemberVariableDict.ContainsKey(name))
            {
                return m_MetaMemberVariableDict[name];
            }
            if (m_MetaMemberVariableDict.ContainsKey(name))
            {
                return m_MetaMemberVariableDict[name];
            }
            if (m_MetaExtendMemeberVariableDict.ContainsKey(name))
            {
                return m_MetaExtendMemeberVariableDict[name];
            }
            return null;
        }
        public void AddMetaGenTemplate( MetaGenTemplate mgt )
        {
            m_MetaGenTemplateDict.Add(mgt.name, mgt);
        }
        public MetaGenTemplate GetMetaGenTemplate( string name )
        {
            if( m_MetaGenTemplateDict.ContainsKey(name) )
            {
                return m_MetaGenTemplateDict[name];
            }
            return null;
        }
        public void UpdateGenMember()
        {
            Dictionary<string, MetaMemberVariable> addList = new Dictionary<string, MetaMemberVariable>();
            foreach ( var v in m_MetaMemberVariableDict.Values )
            {
                MetaMemberVariable mgmv = new MetaMemberVariable( this, v, m_MetaGenTemplateDict );
                addList.Add(mgmv.name, mgmv);
                mgmv.UpdateGenMemberVariable();
            }
            m_MetaMemberVariableDict = addList;

            Dictionary<string, List<MetaMemberFunction>> addFunctionList = new Dictionary<string, List<MetaMemberFunction>>();
            foreach (var v in m_MetaMemberFunctionListDict)
            {
                if (v.Value.Count > 0)
                {
                    var list = new List<MetaMemberFunction>();
                    addFunctionList.Add(v.Key, list);

                    for (int j = 0; j < v.Value.Count; j++)
                    {
                        var curFun = v.Value[j];

                        MetaMemberFunction mgmf = new MetaMemberFunction(this);
                        mgmf.UpdateGenMemberFunctionByTemplateClass( curFun );
                        list.Add(mgmf);
                        if( mgmf.isTemplateClassFunction )
                        {
                            MethodManager.instance.AddClassTemplateMemeberFunction(mgmf);
                        }
                    }
                }
            }
            m_MetaMemberFunctionListDict = addFunctionList;
        }
        public bool Adapter(MetaInputTemplateCollection mitc)
        {
            if( mitc.metaTemplateParamsList.Count == m_MetaGenTemplateDict.Count )
            {
                int i = 0;
                foreach( var v in m_MetaGenTemplateDict )
                {                    
                    var mtpl = mitc.metaTemplateParamsList[i++];
                    var mgtl = v.Value;
                    if (!mgtl.EqualWithMetaType(mtpl))
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }

        public override string ToDefineTypeString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_Name);
            sb.Append("<");
            foreach( var v in m_MetaGenTemplateDict )
            {
                sb.Append(v.Value.ToDefineTypeString());

                //if (i < v.e)
                //    sb.Append(",");
            }
            sb.Append(">");

            return sb.ToString();
        }
        public override string ToFormatString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Clear();
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append(permission.ToFormatString());
            stringBuilder.Append(" ");

            stringBuilder.Append("class " + name);
            if (m_MetaGenTemplateDict.Count > 0)
            {
                stringBuilder.Append("<");
                foreach( var v in m_MetaGenTemplateDict )
                {
                    stringBuilder.Append(v.Value.ToDefineTypeString());
                    //if (i < m_MetaGenTemplateList.Count - 1)
                    //{
                    //    stringBuilder.Append(",");
                    //}
                }
                stringBuilder.Append(">");
            }
            if (m_ExtendClass != null)
            {
                stringBuilder.Append(" :: ");
                stringBuilder.Append(m_ExtendClass.allName);
                var mtl = m_ExtendClass.metaTemplateList;
                if (mtl.Count > 0)
                {
                    stringBuilder.Append("<");
                    for (int i = 0; i < mtl.Count; i++)
                    {
                        stringBuilder.Append(mtl[i].ToFormatString());
                        if (i < mtl.Count - 1)
                        {
                            stringBuilder.Append(",");
                        }
                    }
                    stringBuilder.Append(">");
                }
            }
            if (m_InterfaceClass.Count > 0)
            {
                stringBuilder.Append(" interface ");
            }
            for (int i = 0; i < m_InterfaceClass.Count; i++)
            {
                stringBuilder.Append(m_InterfaceClass[i].allName);
                if (i != m_InterfaceClass.Count - 1)
                    stringBuilder.Append(",");
            }
            stringBuilder.Append(Environment.NewLine);

            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("{" + Environment.NewLine);

            foreach (var v in m_MetaMemberVariableDict)
            {
                stringBuilder.Append(v.Value.ToFormatString());
                stringBuilder.Append(Environment.NewLine);
            }

            foreach (var v in m_MetaMemberFunctionListDict)
            {
                foreach (var v2 in v.Value)
                {
                    stringBuilder.Append(v2.ToFormatString());
                    stringBuilder.Append(Environment.NewLine);
                }
            }

            stringBuilder.Append(Environment.NewLine);
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("}" + Environment.NewLine);

            return stringBuilder.ToString();
        }
    }
}
