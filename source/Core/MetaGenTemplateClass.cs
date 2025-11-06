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

namespace SimpleLanguage.Core
{
    public sealed class MetaGenTemplateClass : MetaClass
    {
        public MetaClass metaTemplateClass => m_MetaTemplateClass;
        public List<MetaGenTemplate> metaGenTemplateList => m_MetaGenTemplateList;
        public override bool isGenTemplate => true;

        private List<MetaGenTemplate> m_MetaGenTemplateList = new List<MetaGenTemplate>();
        private MetaClass m_MetaTemplateClass = null;
        protected bool m_GenTemplateFlag = false;

        public MetaGenTemplateClass( MetaClass mtc, List<MetaGenTemplate> list ) : base(mtc.name)
        {
            m_MetaTemplateClass = mtc;
            m_MetaGenTemplateList = list;
            m_MetaNode = mtc.metaNode;
            m_MetaTemplateList = mtc.metaTemplateList;
            m_ExtendClassMetaType = mtc.extendClassMetaType;
            m_FileCollectMetaMemberVariable = mtc.fileCollectMetaMemberVariable;
            m_FileCollectMetaMemberFunctionList = mtc.fileCollectMetaMemberFunctionList;


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
        public override void ParseGenTemplateClass( MetaGenTemplateClass mgtc )
        {
            if(m_GenTemplateFlag )
            {
                return;
            }

            m_MetaMemberVariableDict.Clear();
            m_MetaMemberFunctionTemplateNodeDict.Clear();
            m_MetaExtendMemeberVariableDict.Clear();
            m_ExtendClassMetaType = this.m_MetaTemplateClass.extendClassMetaType;
            TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(m_ExtendClassMetaType, this, null);
            m_ExtendClass = m_ExtendClassMetaType.metaClass;

            m_ExtendClass.ParseGenTemplateClass(m_ExtendClass as MetaGenTemplateClass);

            ParseMemberVariableDefineMetaType();
            ParseMemberFunctionDefineMetaType();

            m_GenTemplateFlag = true;
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
            List<MetaMemberVariable> mmvList = new List<MetaMemberVariable>();
            foreach (var v in m_ExtendClass.metaExtendMemeberVariableDict)
            {
                mmvList.Add(v.Value);
            }
            foreach (var v in m_ExtendClass.metaMemberVariableDict)
            {
                mmvList.Add(v.Value);
            }
            foreach (var it in mmvList)
            {
                MetaMemberVariable mgmv = new MetaMemberVariable(it);
                mgmv.SetOwnerMetaClass(this);
                this.m_MetaExtendMemeberVariableDict.Add(mgmv.name, mgmv);
            }

            foreach (var it in this.m_MetaTemplateClass.metaMemberVariableDict)
            {
                var mmv = ParseMetaMemberVariableDefineMetaType(it.Value);

                m_MetaMemberVariableDict.Add(mmv.name, mmv);
            }
        }
        //public bool UpdateMetaTypeByGenClassAndFunction( MetaType mt )
        //{
        //    List<MetaClass> regMCList = new List<MetaClass>();
        //    if (mt.defineTemplateMetaTypeList.Count > 0)
        //    {
        //        for (int i = 0; i < mt.defineTemplateMetaTypeList.Count; i++)
        //        {
        //            if (UpdateMetaTypeByGenClassAndFunction(mt.defineTemplateMetaTypeList[i]))
        //            {
        //            }
        //        }
        //    }
        //    if (mt.isTemplate)
        //    {
        //        MetaType ggmt = m_ExtendClassMetaType.GetMetaInputTemplateByIndex(mt.metaTemplate.index);
        //        if (ggmt != null)
        //        {
        //            mt.SetMetaType(ggmt);
        //        }
        //        else
        //        {
        //            //ggmt = mgtf?.GetMetaGenTemplate(mt.metaTemplate.name);
        //            //if (ggmt != null)
        //            //{
        //            //    MetaType mt11 = m_ExtendClassMetaType.GetMetaInputTemplateByIndex(ggmt.metaTemplate.index);
        //            //    mt.SetMetaType(mt11);
        //            //}
        //            //else
        //            //{
        //            //    Log.AddInStructMeta(EError.None, "没有找到模板中定义的模板内容!" + mt.metaTemplate.name);
        //            //}
        //        }
        //    }
        //    else
        //    {
        //        return false;
        //    }

        //    return true;
        //}
        MetaMemberVariable ParseMetaMemberVariableDefineMetaType( MetaMemberVariable mmv )
        {
            MetaMemberVariable mgmv = new MetaMemberVariable(mmv);
            mgmv.SetOwnerMetaClass(this);
            TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(mgmv.realMetaType, this, null );
            return mgmv;
        }
        public override void ParseMemberFunctionDefineMetaType()
        {
            List<MetaMemberFunction> mmfList = new();
            foreach (var it in this.m_MetaTemplateClass.fileCollectMetaMemberFunctionList)
            {
                mmfList.Add(ParseMetaMemberFunctionDefineMetaType(it));
            }

            bool canAdd = false;
            foreach (var v in this.m_ExtendClass.nonStaticVirtualMetaMemberFunctionList)
            {
                canAdd = true;
                var efun = v;
                //if (efun.isConstructInitFunction) { continue; }

                foreach (var v2 in mmfList )
                {
                    //if (v2.isConstructInitFunction) continue;
                    if (efun.IsEqualMetaFunction(v2))
                    {
                        canAdd = false;
                        m_NonStaticVirtualMetaMemberFunctionList.Add(v2);
                        continue;
                    }
                }
                if (canAdd)
                {
                    m_NonStaticVirtualMetaMemberFunctionList.Add(efun);
                }
            }

            foreach (var v2 in mmfList )
            {
                if (v2.isStatic)
                {
                    var find = m_StaticMetaMemberFunctionList.Find(a => a == v2);
                    if (find != null) continue;

                    m_StaticMetaMemberFunctionList.Add(v2);
                }
                else
                {
                    var find = m_NonStaticVirtualMetaMemberFunctionList.Find(a => a == v2);
                    if (find != null) continue;

                    m_NonStaticVirtualMetaMemberFunctionList.Add(v2);
                }
            }


            foreach (var v2 in m_NonStaticVirtualMetaMemberFunctionList)
            {
                //var find = m_AllMetaMemberFunctionList.Find(a => a == v2);
                //if (find != null) continue;

                AddMetaMemberFunction(v2);
                //m_AllMetaMemberFunctionList.Add(v2);
            }
            foreach (var v2 in m_StaticMetaMemberFunctionList)
            {
                //var find = m_AllMetaMemberFunctionList.Find(a => a == v2);
                //if (find != null) continue;

                AddMetaMemberFunction(v2);
                //m_AllMetaMemberFunctionList.Add(v2);
            }
        }
        MetaMemberFunction ParseMetaMemberFunctionDefineMetaType(MetaMemberFunction mmf)
        {
            MetaMemberFunction mgmf = new MetaMemberFunction(mmf);
            mgmf.SetSourceMetaMemberFunction(mmf);
            mgmf.SetOwnerMetaClass(this);

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
                    if (!(mdp.metaVariable.metaDefineType.eType == EMetaTypeType.MetaClass
                        && mdp.metaVariable.metaDefineType.metaClass.isTemplateClass == false))
                    {
                        var realMetaType = new MetaType(mdp.metaVariable.metaDefineType);
                        TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(realMetaType, this, null);
                        mdp.metaVariable.SetRealMetaType(realMetaType);
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
            //AddMetaMemberFunction(mgmf);

            return mgmf;
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
