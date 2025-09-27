//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRMethod
    {
        public string id { get; set; } = "";
        public string virtualFunctionName { get; set; } = "";
        public IRManager irManager { get; private set; } = null;
        //private List<IRMetaVariable> methodInputTemplateObject => m_MethodInputTemplateObject;
        public List<IRMetaVariable> methodArgumentList => m_MethodArgumentList;
        public List<IRMetaVariable> methodLocalVariableList => m_MethodLocalVariableList;
        public List<IRMetaVariable> methodReturnVariableList => m_MethodReturnList;
        public List<IRData> IRDataList => m_IRDataList;

        //private List<IRMetaVariable> m_MethodInputTemplateObject = new List<IRMetaVariable>();
        private List<IRMetaVariable> m_MethodArgumentList = new List<IRMetaVariable>();
        private List<IRMetaVariable> m_MethodLocalVariableList = new List<IRMetaVariable>();
        private List<IRMetaVariable> m_MethodReturnList = new List<IRMetaVariable>();
        private List<IRData> m_LabelList = new List<IRData>();
        private List<IRData> m_IRDataList = new List<IRData>();
        private MetaFunction m_BindMetaFunction = null;
        public IRMethod(IRManager irma, MetaFunction func )
        {
            irManager = irma;
            m_BindMetaFunction = func;
            this.id = func.functionAllName;
            this.virtualFunctionName = func.virtualFunctionName;
        }
        public void Parse()
        {
            var mf = m_BindMetaFunction;
            var id2 = this.id;
            var vfn = mf.virtualFunctionName;

            if (mf.thisMetaVariable != null)
            {
                IRMetaVariable imp = new IRMetaVariable(mf.thisMetaVariable, 0);
                m_MethodArgumentList.Add(imp);
            }
            if (mf.returnMetaVariable!=null)
            {
                IRMetaVariable imp = new IRMetaVariable(mf.returnMetaVariable, 0);
                m_MethodReturnList.Add(imp);
            }
            var list2 = mf.metaMemberParamCollection.metaDefineParamList;
            for( int i = 0; i < list2.Count; i++ )
            {
                MetaDefineParam mdp = list2[i];
                if (mdp == null) continue;
                var tmv = mdp.metaVariable;
                IRMetaVariable imp = new IRMetaVariable(tmv, m_MethodArgumentList.Count);
                m_MethodArgumentList.Add(imp);
            }

            var list = mf.GetCalcMetaVariableList();
            for( int i = 0; i < list.Count; i++ )
            {
                var irsd = new IRMetaVariable(list[i], m_MethodLocalVariableList.Count);
                m_MethodLocalVariableList.Add(irsd);
            }

            var mmf = mf;
            MetaBlockStatements mbs = mmf.metaBlockStatements;
            if (mbs == null)
            {
                Debug.Write("----------------  Info 空函数!! --------------------");
                return;
            }
            IRBlockStatements irbs = new IRBlockStatements(this);
            irbs.ParseAllIRStatements(mbs);
            for (int i = 0; i < irbs.irStatements.Count; i++)
            {
                for (int j = 0; j < irbs.irStatements[i].IRDataList.Count; j++)
                {
                    var addIR = irbs.irStatements[i].IRDataList[j];
                    addIR.id = m_IRDataList.Count;
                    AddLabelDict(addIR);
                    m_IRDataList.Add(addIR);
                }
            }

            for (int i = 0; i < m_LabelList.Count; i++)
            {
                var defLabel = m_LabelList[i];
                switch (defLabel.opCode)
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
                    case EIROpCode.BrTrue:
                        {
                            var findex = IRDataList.FindIndex(a => a == defLabel.opValue);
                            defLabel.index = findex;
                        }
                        break;
                }
            }
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
            else if( irdata.opCode == EIROpCode.Br
                || irdata.opCode == EIROpCode.BrFalse
                || irdata.opCode == EIROpCode.BrTrue )
            {
                m_LabelList.Add(irdata);
            }
        }
        public IRMetaVariable GetIRLocalVariableById( int id )
        {
            return m_MethodLocalVariableList.Find(a => a.id == id);
        }
        public IRMetaVariable GetIRArgumentById( int id )
        {
            return m_MethodArgumentList.Find(a => a.id == id);
        }
        public IRMetaVariable GetReturnVariableById( int id )
        {
            return m_MethodReturnList.Find(a => a.id == id);
        }
        public string ToIRString()
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

        public override string ToString()
        {
            return this.id;
        }
    }
}
