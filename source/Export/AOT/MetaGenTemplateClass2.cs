//****************************************************************************
//  File:      TemplateMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: Generator Template Class's entity by Template Class
//****************************************************************************

using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.AOT
{
    public sealed class MetaGenTemplateClass2 : MetaClass
    {
        public MetaClass metaTemplateClass => m_MetaTemplateClass;

        public override bool isGenTemplate => true;

        private List<MetaGenTemplate> m_MetaGenTemplateList = new List<MetaGenTemplate>();
        private MetaClass m_MetaTemplateClass = null;

        public MetaGenTemplateClass2( MetaClass mtc, List<MetaGenTemplate> list ) : base(mtc.name)
        {
            m_MetaTemplateClass = mtc;
            m_MetaGenTemplateList = list;
        }
        public static MetaGenTemplateClass GenerateTemplateClass( MetaClass mc, MetaInputTemplateCollection mic)
        {
            if (mc.isTemplateClass == false)
            {
                Log.AddInStructMeta(EError.None, "Error 该类不是模版类,不能生成模版生成类!!");
                return null;
            }
            if (mic == null)
            {
                return null;
            }
            if (mc.metaTemplateList.Count == mic.metaTemplateParamsList.Count)
            {
                MetaGenTemplateClass tmc = new MetaGenTemplateClass(mc, null);
                //mc.AddGenTemplateMetaClass(tmc);

                string extenName = "";
                for (int i = 0; i < mc.metaTemplateList.Count; i++)
                {
                    var classTemplate = mc.metaTemplateList[i];
                    var inputTemplate = mic.metaTemplateParamsList[i];

                    MetaGenTemplate mgt = new MetaGenTemplate(classTemplate, inputTemplate);
                    tmc.AddMetaGenTemplate(mgt);

                    if (string.IsNullOrEmpty(extenName))
                    {
                        extenName = inputTemplate.metaClass.name;
                    }
                    else
                    {
                        extenName = extenName + "," + inputTemplate.metaClass.name;
                    }
                }
                //tmc.SetName(mc.name + "<" + extenName + ">");
                //tmc.SetDeep(mc.deep + 1);

                return tmc;
            }
            else
            {
                Log.AddInStructMeta(EError.None, "Error 传进来的模版参数与类定义的参数长度对不上!!");
                return null;
            }            
        }
        public override void SetDeep(int deep)
        {
            m_Deep = deep;
            base.SetDeep(deep);
            //foreach (var v in m_MetaMemberVariableDict)
            //{
            //    v.Value.SetDeep(m_Deep + 1);
            //}

            //foreach (var v in metaMemberFunctionTemplateNodeDict)
            //{
            //    v.Value.SetDeep(m_Deep + 1);
            //}
        }
        public MetaType GetGenTemplateByIndex( int index )
        {
            if(index < m_MetaGenTemplateList.Count && index >= 0  )
            {
                return m_MetaGenTemplateList[index].metaType;
            }
            return null;
        }
        public bool IsMatchByMetaTemplateClass( List<MetaGenTemplate> mgtList )
        {
            if (mgtList == null || mgtList.Count == 0) return false;
            if (mgtList.Count != m_MetaGenTemplateList.Count) return false;
            bool flag = true;
            for( int i = 0; i < mgtList.Count; i++ )
            {
                var c1 = mgtList[i];
                var c2 = m_MetaGenTemplateList[i];
                if( c1.metaType.metaClass != c2.metaType.metaClass )
                {
                    flag = false;
                    break;
                }
            }
            return flag;
        }
        public void GetMetaTemplateMT( Dictionary<string, MetaType> mtdict )
        {
            foreach( var v in m_MetaGenTemplateList )
            {
                var cmg = v;
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
            m_MetaGenTemplateList.Add(mgt);
        }
        public MetaGenTemplate GetMetaGenTemplate( string name )
        {
            return m_MetaGenTemplateList.Find( a=> a.name == name  );
        }
        public override void Parse()
        {
            ParseMemberVariableDefineMetaType();
            ParseMemberFunctionDefineMetaType();

            HandleExtendMemberVariable();
            HandleExtendMemberFunction();
            ParseDefineComplete();

            foreach (var it in m_MetaTemplateClass.metaMemberVariableDict)
            {
                UpdateTemplateInstanceMetaMemberVariableExpress(it.Value);
            }
            //foreach (var it in this.m_MetaTemplateClass.metaMemberFunctionDict )
            //{
            //    var it2 = it.Value;
            //    //foreach (var it2 in it.Value)
            //    {
            //        if( !it2.isTemplateFunction )
            //        {
            //            UpdateTemplateInstanceStatement(it2);
            //        }
            //        else
            //        {

            //        }
            //    }
            //}
        }
        public override void ParseMemberVariableDefineMetaType()
        {
            foreach (var it in this.m_MetaTemplateClass.metaMemberVariableDict)
            {
                ParseMetaMemberVariableDefineMetaType( it.Value );
            }
        }
        void ParseMetaMemberVariableDefineMetaType( MetaMemberVariable mmv )
        {
            MetaMemberVariable mgmv = new MetaMemberVariable(mmv);
            mgmv.SetOwnerMetaClass(this);
            //TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(mgmv.metaDefineType, this, null );
            m_MetaMemberVariableDict.Add(mgmv.name, mgmv);
        }
        public override void ParseMemberFunctionDefineMetaType()
        {
            //foreach (var it in m_MetaTemplateClass.allMetaMemberFunctionList )
            //{
            //    ParseMetaMemberFunctionDefineMetaType(it);
            //}
        }
        void ParseMetaMemberFunctionDefineMetaType(MetaMemberFunction mmv)
        {
            MetaMemberFunction mgmf = new MetaMemberFunction(mmv);
            mgmf.SetOwnerMetaClass(this);

            if( !mgmf.isTemplateFunction )
            {
                //TypeManager.instance.UpdateMetaType(mgmf.metaDefineType, this);

                var list = mgmf.GetCalcMetaVariableList(true);
                for ( int i = 0; i < list.Count; i++ )
                {
                 //   TypeManager.instance.UpdateMetaType(list[i].metaDefineType, this);
                }
            }
            AddMetaMemberFunction(mgmf);
        }
        public bool Adapter(MetaInputTemplateCollection mitc)
        {
            if( mitc.metaTemplateParamsList.Count == m_MetaGenTemplateList.Count )
            {
                int i = 0;
                foreach( var v in m_MetaGenTemplateList)
                {                    
                    var mtpl = mitc.metaTemplateParamsList[i++];
                    var mgtl = v;
                    if ( v.metaType.metaClass != mtpl.metaClass )
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }
        public void UpdateTemplateInstanceMetaMemberVariableExpress( MetaMemberVariable mmv )
        {
            //TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(mmv.metaDefineType, this, null );
        }
        public void UpdateTemplateInstanceStatement(MetaMemberFunction mmf)
        {
            for( int i = 0; i < mmf.metaMemberParamCollection.metaDefineParamList.Count; i++ )
            {
                var mdp = mmf.metaMemberParamCollection.metaDefineParamList[i];
                //TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(mdp.metaVariable.metaDefineType, this, mmf as MetaGenTempalteFunction );
            }

            var list = mmf.GetCalcMetaVariableList();

            for( int i = 0; i < list.Count; i++ )
            {
                //TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(list[i].metaDefineType, this, mmf as MetaGenTempalteFunction );
            }
        }
        public override string ToString()
        {           
            return this.ToDefineTypeString();
        }
        public override string ToDefineTypeString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(" [Gen] ");
            sb.Append(m_Name);
            if (m_MetaGenTemplateList.Count > 0)
            {
                sb.Append("<");
                for (int i = 0; i < m_MetaGenTemplateList.Count; i++)
                {
                    var v = m_MetaGenTemplateList[i];
                    sb.Append(v.ToDefineTypeString());
                    if (i < m_MetaGenTemplateList.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(">");
            }

            return sb.ToString();
        }
        public override string ToFormatString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Clear();
            //for (int i = 0; i < realDeep; i++)
            //    stringBuilder.Append(Global.tabChar);
            //stringBuilder.Append(permission.ToFormatString());
            //stringBuilder.Append(" ");

            //stringBuilder.Append("class " + name);
            //if (m_MetaGenTemplateList.Count > 0)
            //{
            //    stringBuilder.Append("<");
            //    for( int i = 0; i < m_MetaGenTemplateList.Count; i++ )
            //    {
            //        var v = m_MetaGenTemplateClassList[i];
            //        stringBuilder.Append(v.ToDefineTypeString());
            //        if (i < m_MetaGenTemplateList.Count - 1)
            //        {
            //            stringBuilder.Append(",");
            //        }
            //    }
            //    stringBuilder.Append(">");
            //}
            //if (m_ExtendClass != null)
            //{
            //    stringBuilder.Append(" :: ");
            //    stringBuilder.Append(m_ExtendClass.allName);
            //    var mtl = m_ExtendClass.metaTemplateList;
            //    if (mtl.Count > 0)
            //    {
            //        stringBuilder.Append("<");
            //        for (int i = 0; i < mtl.Count; i++)
            //        {
            //            stringBuilder.Append(mtl[i].ToFormatString());
            //            if (i < mtl.Count - 1)
            //            {
            //                stringBuilder.Append(",");
            //            }
            //        }
            //        stringBuilder.Append(">");
            //    }
            //}
            //if (m_InterfaceClass.Count > 0)
            //{
            //    stringBuilder.Append(" interface ");
            //}
            //for (int i = 0; i < m_InterfaceClass.Count; i++)
            //{
            //    stringBuilder.Append(m_InterfaceClass[i].allName);
            //    if (i != m_InterfaceClass.Count - 1)
            //        stringBuilder.Append(",");
            //}
            //stringBuilder.Append(Environment.NewLine);

            //for (int i = 0; i < realDeep; i++)
            //    stringBuilder.Append(Global.tabChar);
            //stringBuilder.Append("{" + Environment.NewLine);

            foreach (var v in m_MetaMemberVariableDict)
            {
                stringBuilder.Append(v.Value.ToFormatString());
                stringBuilder.Append(Environment.NewLine);
            }

            foreach (var v in m_MetaMemberFunctionTemplateNodeDict )
            {
                //foreach (var v2 in v.Value)
                //{
                //    stringBuilder.Append(v2.ToFormatString());
                //    stringBuilder.Append(Environment.NewLine);
                //}
            }

            stringBuilder.Append(Environment.NewLine);
            //for (int i = 0; i < realDeep; i++)
            //    stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("}" + Environment.NewLine);

            return stringBuilder.ToString();
        }
    }
}
