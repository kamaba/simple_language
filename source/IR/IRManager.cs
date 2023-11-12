//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRData
    {
        public int id = 0;
        public EIROpCode opCode;                 //指令类型
        public object    opValue;                //指令值
        public int       index;                  //索引
        public string    path;                   //文件路径
        public int       line;                   //运行行

        public IRData()
        {

        }
        public override string ToString()
        {
            StringBuilder m_StringBuilder = new StringBuilder();
            m_StringBuilder.Append( path + " " + line.ToString() + " [" + opCode.ToString() + "] index:[" + index.ToString() + "]");
            if (opValue != null)
                m_StringBuilder.Append( " val:[" +opValue.ToString() + "] ");
            return m_StringBuilder.ToString();
        }
    }
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
        public List<IRMethodStackData> staticVariableList => m_StaticVariableList;


        private List<IRMethodStackData> m_StaticVariableList = new List<IRMethodStackData>();
        private List<IRData> m_IRDataList = new List<IRData>();


        public static EIROpCode GetConstIROpCode( EType etype )
        {
            switch( etype )
            {
                case EType.Byte: return EIROpCode.LoadConstByte;
                case EType.SByte: return EIROpCode.LoadConstSByte;
                case EType.Boolean:return EIROpCode.LoadConstBoolean;
                case EType.Char: return EIROpCode.LoadConstChar;
                case EType.Int16: return EIROpCode.LoadConstInt16;
                case EType.UInt16:return EIROpCode.LoadConstUInt16;
                case EType.Int32: return EIROpCode.LoadConstInt32;
                case EType.UInt32: return EIROpCode.LoadConstUInt32;
                case EType.Int64: return EIROpCode.LoadConstInt64;
                case EType.UInt64: return EIROpCode.LoadConstUInt64;
                case EType.String: return EIROpCode.LoadConstString;
                case EType.Null:return EIROpCode.LoadConstNull;
                default:
                    {
                        Console.WriteLine("Error GetConstIROpCode!!");
                    }
                    break;
            }
            return EIROpCode.Nop;
        }
        public void TranslateIR()
        {
            ParseClass();

            //动态解析出来的函数
            var dynamicMmfDict = MethodManager.instance.dynamicMetaMemberFunctionDict;
            foreach (var v in dynamicMmfDict)
            {
                IRMethod irm = TranslateIRByFunction(v.Value);
                AddIRMethod(irm);
            }

            //代码定义的成员函数
            var mmfDict = MethodManager.instance.metaMemberFunctionDict;
            foreach (var v in mmfDict)
            {
                IRMethod irm = TranslateIRByFunction(v.Value);
                AddIRMethod(irm);
            }
        }
        void ParseClass()
        {
            //解析成员中的string类型
            //解析成员中的const类型
            var classDict = ClassManager.instance.allClassDict;
            foreach( var v in classDict)
            {
                var mmvd = v.Value.metaMemberVariableDict;
                foreach( var v2 in mmvd )
                {
                    if( v2.Value.isStatic )
                    {
                        IRMethodStackData irData = new IRMethodStackData(v2.Value);
                        irData.index = m_StaticVariableList.Count;
                        m_StaticVariableList.Add(irData);
                    }
                }
            }
            foreach (var v in classDict)
            {
                var mmvd = v.Value.metaMemberVariableDict;
                foreach (var v2 in mmvd)
                {
                    if (v2.Value.isStatic)
                    {
                        IRExpress irexp = new IRExpress( IRManager.instance, v2.Value.express);
                        m_IRDataList.AddRange(irexp.IRDataList);

                        IRData insNode = new IRData();
                        insNode.opCode = EIROpCode.StoreStaticField;
                        insNode.index = GetStaticVariableIndex(v2.Value);
                        m_IRDataList.Add(insNode);
                    }
                }
            }
        }
        public IRMethod TranslateIRByFunction( MetaFunction mf )
        {
            if(IRMethodDict.ContainsKey( mf.irMethodName ) )
            {
                return IRMethodDict[mf.irMethodName];
            }
            IRMethod irmethod = new IRMethod(mf);
            irmethod.Parse();
            return irmethod;
        }
        public int GetStaticVariableIndex(MetaVariable mv)
        {
            foreach (var v in m_StaticVariableList)
            {
                if (v.metaVariable == mv)
                    return v.index;
            }
            return -1;
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
        public string ToStringFormat()
        {
            StringBuilder sb = new StringBuilder();

            foreach( var v in IRMethodDict )
            {
                sb.Append(v.Value.ToStringFormat());
            }

            return sb.ToString();
        }
    }
}
