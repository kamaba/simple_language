//****************************************************************************
//  File:      IRWhileStatements.cs
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

        public Dictionary<string, IRMethod> IRMethodDict = new Dictionary<string, IRMethod>();
        public Dictionary<int, string> IRStringDict = new Dictionary<int,string>();
        public Dictionary<int, SValue> IRConstDict = new Dictionary<int, SValue>();
        public List<IRMetaVariable> staticVariableList => m_StaticVariableList;

        private List<IRMetaVariable> m_StaticVariableList = new List<IRMetaVariable>();
        private Dictionary<int,IRMetaVariable> m_AllVariableDict = new Dictionary<int,IRMetaVariable>();
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
                case EType.Float: return EIROpCode.LoadConstFloat;
                case EType.Double: return EIROpCode.LoadConstDouble;
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
            var mmfDict = MethodManager.instance.metaMemberFunctionDict;
            foreach (var v in mmfDict)
            {
                IRMethod irm = TranslateIRByFunction(v.Value);
                AddIRMethod(irm);
            }

            //动态解析出来的函数
            var dynamicMmfDict = MethodManager.instance.dynamicMetaMemberFunctionDict;
            foreach (var v in dynamicMmfDict)
            {
                IRMethod irm = TranslateIRByFunction(v.Value);
                AddIRMethod(irm);
            }

        }
        public IRMetaClass GetIRMetaClassById( short id )
        {
            return m_IRMetaClassList.Find(a => a.id == id );
        }
        public IRMetaClass GetIRMetaClassByName( string allname )
        {
            return m_IRMetaClassList.Find(a => a.allName == allname);
        }
        void ParseClass()
        {
            //解析成员中的string类型
            //解析成员中的const类型
            var classDict = ClassManager.instance.allClassDict;
            foreach (var v in classDict)
            {
                IRMetaClass irmc = new IRMetaClass(this);
                irmc.CreateMetaClassData(v.Value);
                m_IRMetaClassList.Add(irmc);
            }
            foreach( var v in m_IRMetaClassList)
            {
                v.CreateIRMetaMemberVariable();

                foreach( var v2 in v.localIRMetaVariableList )
                {
                    m_AllVariableDict.Add(v2.GetHashCode(), v2);
                }

            }
            foreach ( var v in classDict )
            {
                if( v.Value is MetaEnum me )
                {
                    var mmvd = me.metaMemberEnumDict;
                    foreach (var v2 in mmvd)
                    {
                        if (v2.Value.isStatic)
                        {
                            IRMetaVariable irMV = new IRMetaVariable(v2.Value);
                            irMV.index = m_StaticVariableList.Count;
                            m_StaticVariableList.Add(irMV);
                        }
                    }
                    if( me.metaVariable != null )
                    {
                        IRMetaVariable irMV = new IRMetaVariable(me.metaVariable);
                        irMV.index = m_StaticVariableList.Count;
                        m_StaticVariableList.Add(irMV);
                    }
                }
                else if( v.Value is MetaData md )
                {
                    var mmvd = md.metaMemberDataDict;
                    foreach (var v2 in mmvd)
                    {
                        if (v2.Value.isStatic)
                        {
                            IRMetaVariable irData = new IRMetaVariable(v2.Value);
                            irData.index = m_StaticVariableList.Count;
                            m_StaticVariableList.Add(irData);
                        }
                    }
                }
                else
                {
                    var mmvd = v.Value.metaMemberVariableDict;
                    foreach (var v2 in mmvd)
                    {
                        if (v2.Value.isStatic)
                        {
                            IRMetaVariable irData = new IRMetaVariable(v2.Value);
                            irData.index = m_StaticVariableList.Count;
                            m_StaticVariableList.Add(irData);
                        }
                    }
                }
            }
            foreach ( var v in m_StaticVariableList )
            {
                m_AllVariableDict.Add(v.GetHashCode(), v);

                IRExpress irexp = new IRExpress(IRManager.instance, v.express );
                m_IRDataList.AddRange(irexp.IRDataList);

                IRData insNode = new IRData();
                insNode.opCode = EIROpCode.StoreStaticField;
                insNode.index = v.index;
                m_IRDataList.Add(insNode);
            }
        }
        public IRMethod TranslateIRByFunction( MetaFunction mf )
        {
            if(IRMethodDict.ContainsKey( mf.functionAllName ) )
            {
                return IRMethodDict[mf.functionAllName];
            }
            IRMethod irmethod = new IRMethod(this);
            irmethod.Parse(mf);
            return irmethod;
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
