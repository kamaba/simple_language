//****************************************************************************
//  File:      IRManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRManager
    {
        public static IRManager s_Instance = null;
        public static IRManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new IRManager();
                }
                return s_Instance;
            }
        }
        public List<IRData> irDataList => m_IRDataList;

        public List<IRMetaClass> irMetaClassList => m_IRMetaClassList;
        public Dictionary<string, IRMethod> IRMethodDict = new Dictionary<string, IRMethod>();
        public Dictionary<int, string> IRStringDict = new Dictionary<int,string>();
        public List<IRMetaVariable> globalStaticVariableList => m_GlobalStaticVariableList;

        private List<IRMetaVariable> m_GlobalStaticVariableList = new List<IRMetaVariable>();
        #region debug用
        private Dictionary<int,IRMetaVariable> m_AllVariableDict = new Dictionary<int,IRMetaVariable>();
        #endregion

        private List<IRMetaClass> m_IRMetaClassList = new List<IRMetaClass>();
        private List<IRData>  m_IRDataList = new List<IRData>();
        public void TranslateIR()
        {
            ParseClass();

            //代码定义的成员函数
            //var mmfDict = MethodManager.instance.metaOriginalFunctionList;
            //foreach (var v in mmfDict)
            //{
            //    v.UpdateFunctionName();
            //    IRMethod irm = this.TranslateIRByFunction(v);
            //    AddIRMethod(irm);
            //}
            //动态解析出来的函数
            var dynamicMmfDict4 = MethodManager.instance.metaDynamicFunctionList;
            foreach (var v in dynamicMmfDict4)
            {
                IRMethod irm = TranslateIRByFunction(v);
                AddIRMethod(irm);
            }

            ParseIRMethod();
        }
        public void GlobalVariable()
        {
            var staticArray = IRManager.instance.globalStaticVariableList;
            /*
            m_GlobalVariableValueList = new List<SValue>(staticArray.Count);

            List<Instruction> execIRList = new List<Instruction>();
            for (int i = 0; i < staticArray.Count; i++)
            {
                m_GlobalVariableId2IndexDict.Add(staticArray[i].id, i);

                var rt = RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(staticArray[i].irMetaType);
                IRMetaClass owirmc = IRManager.instance.GetIRMetaClassById(staticArray[i].irMetaType.irOwnerMetaClass.id);

                var obj = ObjectManager.CreateObjectByRuntimeType(rt, true);
                SValue tmp = default;
                tmp.SetSObject(obj);
                m_GlobalVariableValueList.Add(tmp);

                IRExpressBase irexpress = IRExpressManager.CreateExpress(null, staticArray[i].express);

                IRStoreVariable irsv = new IRStoreVariable(staticArray[i].irMetaType, null, staticArray[i].id, IRMetaVariableFrom.Global);

                execIRList.AddRange(irexpress.IRDataList);
                execIRList.AddRange(irsv.IRDataList);

            }
            RuntimeVM clrRuntime = new RuntimeVM(execIRList);
            clrRuntime.isPersistent = true;
            clrRuntime.id = "InnverCLRRuntimeVM.CLRRuntime.EntryMethod()";
            PushCLRRuntime(clrRuntime);
            clrRuntime.Run(true);
            */
        }
        public IRMetaClass GetIRMetaClassById( int id )
        {
            return m_IRMetaClassList.Find(a => a.id == id );
        }
        public IRMetaClass GetIRMetaClassByName( string allname )
        {
            return m_IRMetaClassList.Find(a => a.irName == allname);
        }
        void ParseClass()
        {
            //解析成员中的string类型
            //解析成员中的const类型
            var classList = ClassManager.instance.runtimeClassList;
            foreach (var v in classList)
            {
                IRMetaClass irmc = new IRMetaClass(v);
                m_IRMetaClassList.Add(irmc);
                //if(!v.isGenTemplate && !v.isTemplateClass )
                //    RuntimeTypeManager.AddRuntimeTypeByClass(irmc);
            }
            foreach ( var v in m_IRMetaClassList )
            {
                v.CreateMemberData();
                v.CreateMemberMethod();
                v.CreateTemplateRelation();
                v.CreateGenMetaTypeTemplateList();
            }
            
            foreach ( var v in m_IRMetaClassList)
            {
                foreach( var v2 in v.localIRMetaVariableList )
                {
                    m_AllVariableDict.Add(v2.GetHashCode(), v2);
                }

            }
            foreach ( var v in classList )
            {
                if( v.isTemplateClass )
                {
                    continue;
                }
                var irmc = m_IRMetaClassList.Find(a => a.irName == v.allClassName );
                if (irmc == null)
                    continue;

                if ( v is MetaEnum me )
                {
                    var mmvd = me.metaMemberEnumDict;
                    //foreach (var v2 in mmvd)
                    //{
                    //    if (v2.Value.isStatic)
                    //    {
                    //        IRMetaVariable irMV = new IRMetaVariable(v2.Value);
                    //        irMV.index = m_StaticVariableList.Count;
                    //        irMV.SetExpress(v2.Value.express);
                    //        m_StaticVariableList.Add(irMV);
                    //    }
                    //}
                    //if( me.metaVariable != null )
                    //{
                    //    IRMetaVariable irMV = new IRMetaVariable(me.metaVariable);
                    //    irMV.index = m_StaticVariableList.Count;
                    //    //irMV.SetExpress(v2.Value.express);
                    //    m_StaticVariableList.Add(irMV);
                    //}
                }
                else if( v is MetaData md )
                {
                    var mmvd = md.metaMemberDataDict;
                    foreach (var v2 in mmvd)
                    {
                        if (v2.Value.isStatic)
                        {
                            IRMetaVariable irMV = new IRMetaVariable(v2.Value);
                            //irMV.index = m_StaticVariableList.Count;
                            ////irMV.SetExpress(v2.Value.express);
                            //m_StaticVariableList.Add(irMV);
                        }
                    }
                }
                else
                {
                    var mmvd = v.metaMemberVariableDict;
                    foreach (var v2 in mmvd)
                    {
                        if (v2.Value.isStatic)
                        {
                            IRMetaVariable irMV = new IRMetaVariable(v2.Value);
                            if( v2.Value.sourceMetaMemberVariable != null )
                            {
                                //irmc.AddMetaMemberVariableHashCode(v2.Value.sourceMetaMemberVariable.GetHashCode(), v2.Value.GetHashCode());
                            }
                            //irMV.index = m_StaticVariableList.Count;
                            //irMV.SetExpress(v2.Value.express);
                            //m_StaticVariableList.Add(irMV);
                        }
                    }
                }
            }
            foreach (var v in m_IRMetaClassList)
            {
                v.CreateStaticMetaMetaVariableIRList();
            }
        }
        public void TranslateIRAutoAdd( MetaFunction mf )
        {
            if (IRMethodDict.ContainsKey(mf.functionAllName))
            {
                return;
            }
            var irm = TranslateIRByFunction(mf);

            IRMethodDict.Add(mf.functionAllName, irm);

        }
        public IRMethod TranslateIRByFunction( MetaFunction mf )
        {
            var hmf = mf;
            if( mf is MetaMemberFunction mmf )
            {
                if(mmf.sourceMetaMemberFunction != null )
                {
                    hmf = mmf.sourceMetaMemberFunction;
                }
                else
                {
                    hmf = mmf;
                }
            }

            if(IRMethodDict.ContainsKey(hmf.functionAllName ) )
            {
                return IRMethodDict[hmf.functionAllName];
            }
            IRMethod irmethod = new IRMethod(this, hmf);
            return irmethod;
        }
        public static string GetIRNameByMetaClass(MetaClass mc)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(mc.metaNode.allName);
            if (mc is MetaGenTemplateClass mgtc)
            {
                sb.Append("<");
                for (int i = 0; i < mgtc.metaGenTemplateList.Count; i++)
                {
                    sb.Append(IRManager.GetIRNameByMetaClass(mgtc.metaGenTemplateList[i].metaType.metaClass));
                    if (i < mgtc.metaGenTemplateList.Count - 1)
                    { sb.Append(","); }
                }
                sb.Append(">");
            }
            else
            {
                if (mc.metaTemplateList.Count > 0)
                {
                    sb.Append("<");
                    for (int i = 0; i < mc.metaTemplateList.Count; i++)
                    {
                        sb.Append(mc.metaTemplateList[i].name);
                        if (i < mc.metaTemplateList.Count - 1)
                        { sb.Append(","); }
                    }
                    sb.Append(">");
                }
            }

            return sb.ToString();
        }
        public static string GetIRNameByMetaType(MetaType mt)
        {
            StringBuilder sb = new StringBuilder();

            if (mt.eType == EMetaTypeType.Template )
            {
                sb.Append("$");
                sb.Append(mt.metaTemplate.name);
                sb.Append("$");
            }
            else if (mt.eType == EMetaTypeType.MetaClass)
            {
                sb.Append(mt.GetTemplateMetaClass().metaNode.allName);
                if ( mt.metaClass is MetaGenTemplateClass mgtc )
                {
                    sb.Append("<");
                    for (int i = 0; i < mgtc.metaGenTemplateList.Count; i++)
                    {
                        sb.Append(GetIRNameByMetaType(mgtc.metaGenTemplateList[i].metaType ));
                        if (i < mgtc.metaGenTemplateList.Count - 1)
                        { sb.Append(","); }
                    }
                    sb.Append('>');
                }
                else
                {
                    if(mt.GetTemplateMetaClass().metaTemplateList.Count > 0 )
                    {
                        sb.Append("<");
                        for (int i = 0; i < mt.GetTemplateMetaClass().metaTemplateList.Count; i++)
                        {
                            sb.Append("$");
                            sb.Append(mt.GetTemplateMetaClass().metaTemplateList[i].name);
                            sb.Append("$");
                            if (i < mt.GetTemplateMetaClass().metaTemplateList.Count - 1)
                            { sb.Append(","); }
                        }
                        sb.Append('>');
                    }
                }
            }
            else
            {
                sb.Append(mt.GetTemplateMetaClass().metaNode.allName);
                sb.Append("<");
                for (int i = 0; i < mt.defineTemplateMetaTypeList.Count; i++)
                {
                    sb.Append(GetIRNameByMetaType(mt.defineTemplateMetaTypeList[i]));
                    if (i < mt.defineTemplateMetaTypeList.Count - 1)
                    { sb.Append(","); }
                }
                sb.Append('>');
            }

            return sb.ToString();
        }
        public void ParseIRMethod()
        {
            foreach( var v in IRMethodDict )
            {
                v.Value.Parse();
                Console.WriteLine("Method: " + v.Value.id);
                Console.WriteLine(v.Value.ToIRString());
            }
        }
        public int GetGlobalStaticMetaVariableById(int id)
        {
            for( int i = 0; i < m_GlobalStaticVariableList.Count; i++ )
            {
                if(m_GlobalStaticVariableList[i].id == id )
                {
                    return i;
                }
            }
            return -1;
        }
        public void AddGlobalMetaMemberVariable( IRMetaVariable irmv )
        {
            m_GlobalStaticVariableList.Add(irmv);
        }
        public int AddStringIRStack( string strMsg )
        {
            foreach( var v in IRStringDict )
            {
                if (v.Value.Equals(strMsg))
                    return v.Key;
            }
            int count = IRStringDict.Count + 1;
            IRStringDict.Add(count, strMsg);
            return count;
        }
        public string GetStringIRStack( int index )
        {
            if( IRStringDict.ContainsKey( index ) )
            {
                return IRStringDict[index];
            }
            return null;
        }
        public bool AddIRMethod( IRMethod method )
        {
            if(IRMethodDict.ContainsKey( method.id ) )
            {
                return false;
            }
            else
            {
                IRMethodDict.Add(method.id, method);
                return true;
            }
        }
        public IRMethod GetIRMethod( string name )
        {
            if( IRMethodDict.ContainsKey( name ) )
            {
                return IRMethodDict[name];
            }
            return null;
        }
        public string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            foreach( var v in IRMethodDict )
            {
                sb.Append(v.Value.ToIRString());
            }

            return sb.ToString();
        }
    }
}
