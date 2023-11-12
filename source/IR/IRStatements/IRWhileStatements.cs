//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description:  handle for loop statements or while/dowhile statements create instruction 
//****************************************************************************

using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaForStatements
    {
        public IRNop startIRData = null;       //for开始执行起点
        public IRData forStartIRData = null;    //for 循环返回起点
        public IRData ifData = null;            //判断是否到终点if判断
        public IRData brData = null;            //for结束点返回起点语句
        public IRNop endIrRata = null;           //for语句点

        private IRExpress m_IRConditionExpress = null;
        public override void ParseIRStatements()
        {
            //startIRData = new IRNop(irMethod);
            //endIrRata = new IRNop(irMethod);

            //if (m_IsForIn)
            //{

            //}
            //else
            //{
            //    if (m_NewStatements != null)
            //    {
            //        m_NewStatements.ParseIRStatements();
            //        m_IRDataList.AddRange(m_NewStatements.irDataList);
            //    }
            //    else if( m_AssignStatements != null )
            //    {
            //        m_AssignStatements.ParseIRStatements();
            //        m_IRDataList.AddRange(m_AssignStatements.irDataList);
            //    }
            //    forStartIRData = new IRData();
            //    forStartIRData.opCode = EIROpCode.Nop;
            //    m_IRDataList.Add(forStartIRData);

            //    if (m_ConditionExpress != null)
            //    {
            //        m_IRConditionExpress = new IRExpress(irMethod, m_ConditionExpress);
            //        m_IRDataList.AddRange(m_IRConditionExpress.IRDataList);

            //        ifData = new IRData();
            //        ifData.opCode = EIROpCode.BrFalse;
            //        ifData.opValue = endData;
            //        m_IRDataList.Add(ifData);

            //        irMethod.AddLabelDict(ifData);
            //    }
            //    if (m_StepStatements != null)
            //    {
            //        m_StepStatements.ParseIRStatements();
            //        m_IRDataList.AddRange(m_StepStatements.irDataList);
            //    }

            //    m_ThenMetaStatements.ParseAllIRStatements();
            //    m_IRDataList.AddRange(m_ThenMetaStatements.irDataList);

            //}

            //brData = new IRData();
            //brData.opCode = EIROpCode.Br;
            //brData.opValue = forStartIRData;
            //irMethod.AddLabelDict(brData);
            //m_IRDataList.Add(brData);
            //m_IRDataList.Add(endData);

            //if (m_NextMetaStatements != null)
            //{
            //    m_NextMetaStatements.ParseIRStatements();
            //    m_IRDataList.AddRange(m_NextMetaStatements.irDataList);
            //}
        }
    }

    public partial class MetaWhileDoWhileStatements
    {
        public IRData startIRData = null;       //while 开始执行起点
        public IRData whileStartIRData = null;    //while 循环返回起点
        public IRData ifData = null;            //判断是否到终点if判断
        public IRData brData = null;            //while 结束点返回起点语句
        public IRData endData = null;           //while 语句点

        private IRExpress m_IRConditionExpress = null;
        public override void ParseIRStatements()
        {
            //startIRData = new IRData();
            //startIRData.opCode = EIROpCode.Nop;
            //m_IRDataList.Add(startIRData);

            //endData = new IRData();
            //endData.opCode = EIROpCode.Nop;

            //whileStartIRData = new IRData();
            //whileStartIRData.opCode = EIROpCode.Nop;
            //m_IRDataList.Add(whileStartIRData);

            //if ( m_IsWhile )
            //{
            //    if (m_ConditionExpress != null)
            //    {
            //        m_IRConditionExpress = new IRExpress(irMethod, m_ConditionExpress);
            //        m_IRDataList.AddRange(m_IRConditionExpress.IRDataList);

            //        ifData = new IRData();
            //        ifData.opCode = EIROpCode.BrFalse;
            //        ifData.opValue = endData;
            //        m_IRDataList.Add(ifData);

            //        irMethod.AddLabelDict(ifData);
            //    }
            //    m_ThenMetaStatements.ParseAllIRStatements();
            //    m_IRDataList.AddRange(m_ThenMetaStatements.irDataList);
            //}
            //else
            //{
            //    m_ThenMetaStatements.ParseAllIRStatements();
            //    m_IRDataList.AddRange(m_ThenMetaStatements.irDataList);

            //    if (m_ConditionExpress != null)
            //    {
            //        m_IRConditionExpress = new IRExpress(irMethod, m_ConditionExpress);
            //        m_IRDataList.AddRange(m_IRConditionExpress.IRDataList);

            //        ifData = new IRData();
            //        ifData.opCode = EIROpCode.BrFalse;
            //        ifData.opValue = endData;
            //        m_IRDataList.Add(ifData);

            //        irMethod.AddLabelDict(ifData);
            //    }
            //}
            //brData = new IRData();
            //brData.opCode = EIROpCode.Br;
            //brData.opValue = whileStartIRData;
            //irMethod.AddLabelDict(brData);
            //m_IRDataList.Add(brData);
            //m_IRDataList.Add(endData);            

            //if (m_NextMetaStatements != null)
            //{
            //    m_NextMetaStatements.ParseIRStatements();
            //    m_IRDataList.AddRange(m_NextMetaStatements.irDataList);
            //}
        }
    }
}
