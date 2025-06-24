//****************************************************************************
//  File:      TemplateMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: Generator Template Class's entity by Template Class
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Core
{
    public class MetaGenTemplateClass : MetaClass
    {
        public bool initTemplateMemberVariable => m_InitTemplateMemberVariable;
        public bool initTemplateMemberFunction => m_InitTemplateMemberFunction;
        public MetaClass metaTemplateClass => m_MetaTemplateClass;

        public override bool isGenTemplate => true;

        private bool m_InitTemplateMemberVariable = false;
        private bool m_InitTemplateMemberFunction = false;
        protected List<MetaGenTemplate> m_MetaGenTemplateList = new List<MetaGenTemplate>();
        protected List<MetaMemberFunction> m_GenMetaMemberFunctions = new List<MetaMemberFunction>();
        protected string m_GenTemplateClassName = "";
        protected MetaClass m_MetaTemplateClass = null;

        public MetaGenTemplateClass( MetaClass mtc, List<MetaGenTemplate> list ) : base(mtc.name)
        {
            m_MetaTemplateClass = mtc;
            m_MetaGenTemplateList = list;
        }
        public static MetaGenTemplateClass GenerateTemplateClass( MetaClass mc, MetaInputTemplateCollection mic)
        {
            if (mc.isTemplateClass == false)
            {
                Debug.Write("Error 该类不是模版类,不能生成模版生成类!!");
                return null;
            }
            if (mic == null)
            {
                return null;
            }
            if (mc.metaTemplateList.Count == mic.metaTemplateParamsList.Count)
            {
                MetaGenTemplateClass tmc = new MetaGenTemplateClass(mc, null);
                mc.AddGenTemplateMetaClass(tmc);

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
                tmc.SetName(mc.name + "<" + extenName + ">");
                tmc.SetDeep(mc.deep + 1);

                return tmc;
            }
            else
            {
                Debug.WriteLine("Error 传进来的模版参数与类定义的参数长度对不上!!");
                return null;
            }            
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

            HandleExtendData();
            ParseDefineComplete();

            foreach (var it in m_MetaTemplateClass.metaMemberVariableDict)
            {
                it.Value.ParseMetaExpress();
            }
            foreach (var it in m_MetaTemplateClass.metaMemberFunctionListDict)
            {
                foreach (var it2 in it.Value)
                {
                    it2.ParseMetaExpress();
                    it2.ParseStatements();
                }
            }
        }
        public override void ParseMemberVariableDefineMetaType()
        {
            foreach (var it in m_MetaTemplateClass.metaMemberVariableDict)
            {
                ParseMetaMemberVariableDefineMetaType( it.Value );
            }
        }
        void ParseMetaMemberVariableDefineMetaType( MetaMemberVariable mmv )
        {
            MetaMemberVariable mgmv = new MetaMemberVariable( this, mmv.name );

            MetaClass retMc = ClassManager.instance.GetMetaClassAndRegisterExptendTemplateClassInstance(this, mmv.fileMetaMemeberVariable.classDefineRef );

            mgmv.SetMetaDefineType(new MetaType(retMc));

            m_MetaMemberVariableDict.Add(mgmv.name, mgmv);
        }
        public override void ParseMemberFunctionDefineMetaType()
        {
            foreach (var it in m_MetaTemplateClass.metaMemberFunctionListDict)
            {
                foreach (var it2 in it.Value)
                {
                    if(it2.isTemplateClassFunction  == false )
                    {
                        ParseMetaMemberFunctionDefineMetaType(it2);
                    }
                }
            }
        }
        void ParseMetaMemberFunctionDefineMetaType(MetaMemberFunction mmv)
        {
            MetaMemberFunction mgmf = new MetaMemberFunction(this, mmv.name);

            MetaClass retMc = ClassManager.instance.GetMetaClassAndRegisterExptendTemplateClassInstance(this, mmv.fileMetaMemberFunction.defineMetaClass );

            mgmf.SetMetaDefineType(new MetaType(retMc));

            for( int i = 0; i < mmv.fileMetaMemberFunction.metaParamtersList.Count; i++ )
            {
                var param = mmv.fileMetaMemberFunction.metaParamtersList[i];
                MetaDefineParam mmp = new MetaDefineParam(param.name, mgmf );
                mmp.ParseMetaDefineType();
                mgmf.metaMemberParamCollection.AddMetaDefineParam(mmp);
            }

            if (!mgmf.isTemplateFunction)
            {
                MethodManager.instance.AddClassTemplateMemeberFunction(mgmf);
            }

            List<MetaMemberFunction> list = null;
            if (m_MetaMemberFunctionListDict.ContainsKey(mgmf.name))
            {
                list = m_MetaMemberFunctionListDict[mgmf.name];
            }
            else
            {
                list = new List<MetaMemberFunction>();
            }
            list.Add(mgmf);

            m_MetaMemberAllNameFunctionDict.Add(mgmf.name, mgmf );
        }
        public void UpdateGenMemberDefineMetaType()
        {           

            if( ProjectManager.compileUseTemplateClassGenClassFunction )
            {
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
                            mgmf.UpdateGenMemberFunctionByTemplateClass(curFun);
                            list.Add(mgmf);
                        }
                    }
                }
                m_MetaMemberFunctionListDict = addFunctionList;
            }
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

        public override string ToString()
        {           
            return this.ToDefineTypeString();
        }
        public override string ToDefineTypeString()
        {
            StringBuilder sb = new StringBuilder();

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
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append(permission.ToFormatString());
            stringBuilder.Append(" ");

            stringBuilder.Append("class " + name);
            if (m_MetaGenTemplateList.Count > 0)
            {
                stringBuilder.Append("<");
                for( int i = 0; i < m_MetaGenTemplateList.Count; i++ )
                {
                    var v = m_MetaGenTemplateClassList[i];
                    stringBuilder.Append(v.ToDefineTypeString());
                    if (i < m_MetaGenTemplateList.Count - 1)
                    {
                        stringBuilder.Append(",");
                    }
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
