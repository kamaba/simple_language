//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRMethodStackData
    {
        public int index { get; set; } = 0;
        public MetaVariable metaVariable => m_MetaVariable;
        private MetaVariable m_MetaVariable = null;
        public IRMethodStackData( MetaVariable mv )
        {
            m_MetaVariable = mv;
        }
    }
    public class IRMethod
    {
        public string id { get; set; } = "";
        public List<IRMethodStackData> methodArgumentList => m_MethodArgumentList;
        public List<IRMethodStackData> methodLocalVariableList => m_MethodLocalVariableList;
        public List<IRMethodStackData> methodReturnVariableList => m_MethodReturnList;
        public List<IRData> IRDataList => m_IRDataList;


        private MetaFunction m_MetaFunction = null;
        private List<IRMethodStackData> m_MethodArgumentList = new List<IRMethodStackData>();
        private List<IRMethodStackData> m_MethodLocalVariableList = new List<IRMethodStackData>();
        private List<IRMethodStackData> m_MethodReturnList = new List<IRMethodStackData>();
        private List<IRData> m_LabelList = new List<IRData>();
        private List<IRData> m_IRDataList = new List<IRData>();
        public IRMethod( MetaFunction mf )
        {
            m_MetaFunction = mf;
            mf.SetIRMethod(this);
            id = m_MetaFunction.irMethodName;

            if (m_MetaFunction.thisMetaVariable != null)
            {
                IRMethodStackData imp = new IRMethodStackData(m_MetaFunction.thisMetaVariable);
                imp.index = 0;
                m_MethodArgumentList.Add(imp);
            }
            if (m_MetaFunction.returnMetaVariable!=null)
            {
                IRMethodStackData imp = new IRMethodStackData(m_MetaFunction.returnMetaVariable);
                imp.index = 1;
                m_MethodReturnList.Add(imp);
            }
            var list2 = m_MetaFunction.metaMemberParamCollection.metaParamList;
            for( int i = 0; i < list2.Count; i++ )
            {
                MetaDefineParam mdp = list2[i] as MetaDefineParam;
                if (mdp == null) continue;
                IRMethodStackData imp = new IRMethodStackData(mdp.metaVariable);
                imp.index = m_MethodArgumentList.Count;
                m_MethodArgumentList.Add(imp);
            }

            var list = m_MetaFunction.GetCalcMetaVariableList();
            for( int i = 0; i < list.Count; i++ )
            {
                var irsd = new IRMethodStackData(list[i]);
                irsd.index = m_MethodLocalVariableList.Count;
                m_MethodLocalVariableList.Add(irsd);
            }
        }
        public void MethodInitParse()
        {

        }
        public void AddLabelDict( IRData irdata )
        {
            if( irdata.opCode == EIROpCode.Label )
            {
                var findlabel = m_LabelList.Find(a => a.opValue == irdata.opValue);
                if (findlabel == null )
                {
                    m_LabelList.Add(irdata);
                }
            }
            else
            {
                m_LabelList.Add(irdata);
            }
        }
        public int GetReturnVariableIndex( MetaVariable mv )
        {
            foreach (var v in m_MethodReturnList )
            {
                if (v.metaVariable == mv)
                    return v.index;
            }
            return -1;
        }
        public int GetLocalVariableIndex( MetaVariable mv )
        {
            foreach( var v in m_MethodLocalVariableList)
            {
                if (v.metaVariable == mv)
                    return v.index;
            }
            return -1;
        }
        public int GetArgumentIndex( MetaVariable mv )
        {
            foreach (var v in m_MethodArgumentList )
            {
                if (v.metaVariable == mv)
                    return v.index;
            }
            return -1;
        }
        public void Parse()
        {
            var mmf = m_MetaFunction;
            MetaBlockStatements mbs = mmf.metaBlockStatements;
            if (mbs == null )
            {
                Console.WriteLine("Info 空函数!!");
                return;
            }
            mbs.ParseAllIRStatements();
            for( int i = 0; i < mbs.irStatements.Count; i++ )
            {
                IRDataList.AddRange(mbs.irStatements[i].IRDataList);
            }

            for( int i = 0; i < m_LabelList.Count; i++ )
            {
                var defLabel = m_LabelList[i];
                switch( defLabel.opCode )
                {
                    case EIROpCode.BrLabel:
                        {
                            var findLabel = m_LabelList.Find(a => a.opValue == defLabel.opValue);
                            defLabel.opValue = findLabel;
                        }
                        break;
                    case EIROpCode.Br:
                        {
                            var findex = IRDataList.FindIndex(a => a == defLabel.opValue);
                            defLabel.index = findex;
                        }
                        break;
                    case EIROpCode.BrFalse:
                        {
                            var findex = IRDataList.FindIndex(a => a == defLabel.opValue);
                            defLabel.index = findex;
                        }
                        break;
                }
            }



            string str = mbs.ToIRString();//ToStringFormat();
            Console.WriteLine(str);
        }
        public string ToStringFormat()
        {
            StringBuilder sb = new StringBuilder();

            for( int i = 0; i < IRDataList.Count; i++ )
            {
                sb.Append(i.ToString() + " ");
                sb.Append(IRDataList[i].ToString());
                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }
    }
}
