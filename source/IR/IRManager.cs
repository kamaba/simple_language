//****************************************************************************
//  File:      IRManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Core;
using SimpleLanguage.VM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
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
        public Dictionary<int, SValue> IRConstDict = new Dictionary<int, SValue>();
        public List<IRMetaVariable> staticVariableList => m_StaticVariableList;

        private List<IRMetaVariable> m_StaticVariableList = new List<IRMetaVariable>();
        #region debug用
        private Dictionary<int,IRMetaVariable> m_AllVariableDict = new Dictionary<int,IRMetaVariable>();
        #endregion
        private List<IRData> m_IRDataList = new List<IRData>();

        private List<IRMetaClass> m_IRMetaClassList = new List<IRMetaClass>();
        public static EIROpCode GetConstIROpCode( EType etype )
        {
            switch( etype )
            {
                case EType.Byte: return EIROpCode.LoadConstByte;
                case EType.SByte: return EIROpCode.LoadConstSByte;
                case EType.Boolean:return EIROpCode.LoadConstBoolean;
                //case EType.Char: return EIROpCode.LoadConstChar;
                case EType.Int16: return EIROpCode.LoadConstInt16;
                case EType.UInt16:return EIROpCode.LoadConstUInt16;
                case EType.Int32: return EIROpCode.LoadConstInt32;
                case EType.UInt32: return EIROpCode.LoadConstUInt32;
                case EType.Int64: return EIROpCode.LoadConstInt64;
                case EType.UInt64: return EIROpCode.LoadConstUInt64;
                case EType.Float32: return EIROpCode.LoadConstFloat;
                case EType.Float64: return EIROpCode.LoadConstDouble;
                case EType.String: return EIROpCode.LoadConstString;
                case EType.Null:return EIROpCode.LoadConstNull;
                default:
                    {
                        Debug.Write("Error GetConstIROpCode!!");
                    }
                    break;
            }
            return EIROpCode.Nop;
        }
        public void TranslateIR()
        {
            ParseClass();

            //代码定义的成员函数
            var mmfDict = MethodManager.instance.metaOriginalFunctionList;
            foreach (var v in mmfDict)
            {
                IRMethod irm = this.TranslateIRByFunction(v);
                AddIRMethod(irm);
            }
            //动态解析出来的函数
            var dynamicMmfDict4 = MethodManager.instance.metaDynamicFunctionList;
            foreach (var v in dynamicMmfDict4)
            {
                IRMethod irm = TranslateIRByFunction(v);
                AddIRMethod(irm);
            }

            ParseIRMethod();
        }
        public IRMetaClass GetIRMetaClassById( short id )
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
                IRMetaClass irmc = new IRMetaClass(this);
                irmc.CreateMetaClassData(v);
                m_IRMetaClassList.Add(irmc);
            }
            foreach (var v in classList)
            {
                if( v is MetaGenTemplateClass mgtc )
                {
                    IRMetaClass irmc = GetIRMetaClassByName(GetIRNameByMetaClass(v));
                    IRMetaClass irmcc = GetIRMetaClassByName(GetIRNameByMetaClass(mgtc.metaTemplateClass));
                    if( irmcc != null && irmc != null )
                    {
                        irmc.SetTemplateIRMetaClass(irmcc);
                    }
                }

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
                var irlist = v.CreateStaticMetaMetaVariableIRList();
                //m_AllVariableDict.Add(v.GetHashCode(), v);

                //IRExpress irexp = new IRExpress(IRManager.instance, v.express);
                //m_IRDataList.AddRange(irexp.IRDataList);

                //IRData insNode = new IRData();
                //insNode.opCode = EIROpCode.StoreStaticField;
                //insNode.index = v.index;
                //m_IRDataList.Add(insNode);
                m_IRDataList.AddRange(irlist);
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
            if(IRMethodDict.ContainsKey( mf.functionAllName ) )
            {
                return IRMethodDict[mf.functionAllName];
            }
            IRMethod irmethod = new IRMethod(this, mf );
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
                sb.Append(mt.metaClass.metaNode.allName);
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
                    if(mt.metaClass.metaTemplateList.Count > 0 )
                    {
                        sb.Append("<");
                        for (int i = 0; i < mt.metaClass.metaTemplateList.Count; i++)
                        {
                            sb.Append("$");
                            sb.Append(mt.metaClass.metaTemplateList[i].name);
                            sb.Append("$");
                            if (i < mt.metaClass.metaTemplateList.Count - 1)
                            { sb.Append(","); }
                        }
                        sb.Append('>');
                    }
                }
            }
            else
            {
                sb.Append(mt.templateMetaClass.metaNode.allName);
                sb.Append("<");
                for (int i = 0; i < mt.templateMetaTypeList.Count; i++)
                {
                    sb.Append(GetIRNameByMetaType(mt.templateMetaTypeList[i]));
                    if (i < mt.templateMetaTypeList.Count - 1)
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
        //public IRMetaVariable GetStaticMetaVariableById( IRMetaClass irmc, int id )
        //{
        //    if( irmc != null )
        //    {
        //        var genstaticid = irmc.GetStaticMetaMemberVariableHashCode(id);
        //        if( genstaticid == -1 )
        //        {
        //            return null;
        //        }
        //        if (m_AllVariableDict.ContainsKey(genstaticid))
        //        {
        //            return m_AllVariableDict[genstaticid];
        //        }

        //    }
        //    else
        //    {
        //        if (m_AllVariableDict.ContainsKey(id))
        //        {
        //            return m_AllVariableDict[id];
        //        }
        //    }
        //    return null;
        //}
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
