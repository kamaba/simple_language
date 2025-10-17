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

namespace SimpleLanguage.Core
{
    public sealed class MetaGenTemplateClass : MetaClass
    {
        public MetaClass metaTemplateClass => m_MetaTemplateClass;
        public List<MetaGenTemplate> metaGenTemplateList => m_MetaGenTemplateList;
        public override bool isGenTemplate => true;

        private List<MetaGenTemplate> m_MetaGenTemplateList = new List<MetaGenTemplate>();
        private MetaClass m_MetaTemplateClass = null;

        public MetaGenTemplateClass( MetaClass mtc, List<MetaGenTemplate> list ) : base(mtc.name)
        {
            m_MetaTemplateClass = mtc;
            m_MetaGenTemplateList = list;
            m_MetaNode = mtc.metaNode;
            m_ExtendClassMetaType = mtc.extendClassMetaType;


            StringBuilder sb = new StringBuilder();
            sb.Append(m_MetaTemplateClass.pathName);
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
            this.m_AllName = sb.ToString(); ;
        }
        public void UpdateRegsterGenMetaClass()
        {
            //这个过程是 绑定 原来注册过来的T的已有的类
            for (int i = 0; i < this.m_MetaTemplateClass.bindStructTemplateMetaClassList.Count; i++)
            {
                m_MetaTemplateClass.bindStructTemplateMetaClassList[i].UpdateMetaGenTemplate(m_MetaGenTemplateList);
            }
        }
        public override void SetDeep(int deep)
        {
            m_Deep = deep;
            foreach (var v in m_MetaMemberVariableDict)
            {
                v.Value.SetDeep(m_Deep + 1);
            }
            foreach (var v in metaMemberFunctionTemplateNodeDict)
            {
                v.Value.SetDeep(m_Deep + 1);
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
            m_MetaMemberVariableDict.Clear();
            m_MetaMemberFunctionTemplateNodeDict.Clear();
            m_MetaExtendMemeberVariableDict.Clear();

            ParseMemberVariableDefineMetaType();
            ParseMemberFunctionDefineMetaType();

            m_ExtendClassMetaType = this.m_MetaTemplateClass.extendClassMetaType;

            TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(m_ExtendClassMetaType, this, null);
            m_ExtendClass = m_ExtendClassMetaType.metaClass;

            HandleExtendMemberVariable();
            HandleExtendMemberFunction();

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

        public override void HandleExtendMemberVariable()
        {
            base.HandleExtendMemberVariable();
        }
        public override void HandleExtendMemberFunction()
        {
            this.m_NonStaticVirtualMetaMemberFunctionList = m_ExtendClass.nonStaticVirtualMetaMemberFunctionList;
            this.m_StaticMetaMemberFunctionList = m_ExtendClass.staticMetaMemberFunctionList;
        }
        public override void ParseMemberVariableDefineMetaType()
        {
            foreach (var it in this.m_MetaTemplateClass.metaExtendMemeberVariableDict )
            {
                var mmv = ParseMetaMemberVariableDefineMetaType( it.Value );

                m_MetaExtendMemeberVariableDict.Add(mmv.name, mmv);
            }
            foreach (var it in this.m_MetaTemplateClass.metaMemberVariableDict)
            {
                var mmv = ParseMetaMemberVariableDefineMetaType(it.Value);

                m_MetaMemberVariableDict.Add(mmv.name, mmv);
            }
        }
        MetaMemberVariable ParseMetaMemberVariableDefineMetaType( MetaMemberVariable mmv )
        {
            MetaMemberVariable mgmv = new MetaMemberVariable(mmv);
            mgmv.SetOwnerMetaClass(this);
            TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(mgmv.realMetaType, this, null );
            return mgmv;
        }
        public override void ParseMemberFunctionDefineMetaType()
        {
            foreach (var it in this.m_MetaTemplateClass.nonStaticVirtualMetaMemberFunctionList )
            {
                ParseMetaMemberFunctionDefineMetaType(it);
            }
            foreach (var it in this.m_MetaTemplateClass.staticMetaMemberFunctionList)
            {
                ParseMetaMemberFunctionDefineMetaType(it);
            }
        }
        void ParseMetaMemberFunctionDefineMetaType(MetaMemberFunction mmf)
        {
            MetaMemberFunction mgmf = new MetaMemberFunction(mmf);
            mgmf.SetSourceMetaMemberFunction(mmf);

            if (mmf.isTemplateFunction == false)
            {
                if (mgmf.returnMetaVariable?.metaDefineType != null)
                {
                    if (!(mgmf.returnMetaVariable.metaDefineType.eType == EMetaTypeType.MetaClass
                        && mgmf.returnMetaVariable.metaDefineType.metaClass.isTemplateClass == false))
                    {
                        TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(mgmf.returnMetaVariable.realMetaType, this, null);
                    }
                }
                for (int i = 0; i < mgmf.metaMemberParamCollection.metaDefineParamList.Count; i++)
                {
                    var mdp = mgmf.metaMemberParamCollection.metaDefineParamList[i];
                    if (!(mgmf.returnMetaVariable.metaDefineType.eType == EMetaTypeType.MetaClass
                        && mgmf.returnMetaVariable.metaDefineType.metaClass.isTemplateClass == false))
                    {
                        TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(mdp.metaVariable.realMetaType, this, null);
                    }
                }
            }
            else
            {
                for( int i = 0; i < mmf.genTempalteFunctionList.Count; i++ )
                {
                    var v = mmf.genTempalteFunctionList[i];
                    v.UpdateRegsterGenMetaFunctionAndClass(m_MetaGenTemplateList);
                }
            }
            mgmf.UpdateFunctionName();
            AddMetaMemberFunction(mgmf);
        }
        public void UpdateRegisterTemplateFunction()
        {
            foreach (var it in this.m_MetaTemplateClass.nonStaticVirtualMetaMemberFunctionList)
            {
                if( it.isTemplateFunction )
                {
                    UpdateRegisterTemplateFunctionSingle( it );
                }
            }
            foreach (var it in this.m_MetaTemplateClass.staticMetaMemberFunctionList)
            {
                if( it.isTemplateFunction )
                {
                    UpdateRegisterTemplateFunctionSingle(it);
                }
            }
        }
        void UpdateRegisterTemplateFunctionSingle( MetaMemberFunction mmf )
        {
            for (int i = 0; i < mmf.genTempalteFunctionList.Count; i++)
            {
                var v = mmf.genTempalteFunctionList[i];
                v.UpdateRegsterGenMetaFunctionAndClass(m_MetaGenTemplateList);
            }
        }
        public override List<MetaMemberVariable> GetMetaMemberVariableListByFlag(bool isStatic )
        {
            List<MetaMemberVariable> mmvList = new List<MetaMemberVariable>();
            MetaMemberVariable tempMmv = null;
            foreach (var v in m_MetaMemberVariableDict)
            {
                tempMmv = v.Value;
                if (isStatic)
                {
                    if (tempMmv.isStatic == isStatic || tempMmv.isConst == isStatic)
                    {
                        mmvList.Add(tempMmv);
                    }
                }
                else
                {
                    if (tempMmv.isStatic == false && tempMmv.isConst == false)
                    {
                        mmvList.Add(tempMmv);
                    }
                }
            }
            foreach (var v in m_MetaExtendMemeberVariableDict)
            {
                tempMmv = v.Value;
                if (isStatic)
                {
                    if (tempMmv.isStatic == isStatic || tempMmv.isConst == isStatic)
                    {
                        mmvList.Add(tempMmv);
                    }
                }
                else
                {
                    if (tempMmv.isStatic == false && tempMmv.isConst == false)
                    {
                        mmvList.Add(tempMmv);
                    }
                }
            }
            return mmvList;
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
