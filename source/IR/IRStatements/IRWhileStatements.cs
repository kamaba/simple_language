//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description:  handle for loop statements or while/dowhile statements create instruction 
//****************************************************************************



using SimpleLanguage.Core;
using SimpleLanguage.IR.Statements;
using SimpleLanguage.Parse;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRForStatements : IRStatements
    {
        public IRForStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        public IRNop startIRData = null;       //for开始执行起点
        public IRNop forStartIRData = null;    //for 循环返回起点
        public IRNop endIRData = null;           //for语句点
        public IRBranch ifIRData = null;            //判断是否到终点if判断
        public IRBranch brIRData = null;            //for结束点返回起点语句
        IRCallFunction hasNextCallFunction = null;
        IRCallFunction nextCallFunction = null;

        private IRExpress m_IRConditionExpress = null;
        public void ParseIRStatements(MetaForStatements ms)
        {
            startIRData = new IRNop(irMethod);
            endIRData = new IRNop(irMethod);
            m_IRStatements.Add(startIRData);

            if( ms.isForIn )
            {
                var fun = ms.hasNextFunction;

                //--------------------var1._hasNext_()
                var irmc = IRManager.instance.GetIRMetaClassById( ms.forInContent.metaDefineType.GetTemplateMetaClass().GetHashCode());
                var irmt = new IRMetaType(irmc);
                var runmethod = irmc.GetIRNonStaticMethodIndexByName("hasNext", out int index );

                var irmethodcall = new IRMethodCall(irmt, new List<IRMetaType>(), runmethod, 0 );
                IRData datacall = new IRData();
                datacall.opCode = EIROpCode.CallDynamic;
                datacall.opValue = irmethodcall;
                datacall.index = 1;
                //datacall.SetDebugInfoByToken(mf.pingToken);
                IRBase irbase = new IRBase(datacall);
                m_IRStatements.Add(irbase);
                // -----------end----------------------

                //----------------------if current return
                ifIRData = new IRBranch(irMethod, EIROpCode.BrFalse, null);
                m_IRStatements.Add(ifIRData);

                //----------------------var = a1._next_
                            // var load
                IRLoadVariable irlv = IRLoadVariable.CreateLoadVariable(null, null, irMethod, ms.forIterateVariable );
                m_IRStatements.Add(irlv);

                // a1._next_
                //--------------------var1._hasNext_()
                var _next_irmc = IRManager.instance.GetIRMetaClassById(ms.forInContent.metaDefineType.GetTemplateMetaClass().GetHashCode());
                var _next_irmt = new IRMetaType(_next_irmc);
                var _next_runmethod = irmc.GetIRNonStaticMethodIndexByName("current", out int _next_index );

                var _next_irmethodcall = new IRMethodCall(irmt, new List<IRMetaType>(), _next_runmethod, 0);
                IRData _next_datacall = new IRData();
                _next_datacall.opCode = EIROpCode.CallDynamic;
                _next_datacall.opValue = irmethodcall;
                _next_datacall.index = 1;
                //datacall.SetDebugInfoByToken(mf.pingToken);
                IRBase _next_irbase = new IRBase(_next_datacall);
                m_IRStatements.Add(_next_irbase);

                IRStoreVariable _next_store_ = IRStoreVariable.CreateIRStoreVariable(null, null, irMethod, ms.forIterateVariable);
                m_IRStatements.Add(_next_store_);

                //---------------------if then 
                IRBlockStatements irbs = new IRBlockStatements(irMethod);
                irbs.ParseAllIRStatements(ms.thenMetaStatements);
                m_IRStatements.AddRange(irbs.irStatements);

                //------------------------goto start label
                IRBranch mirbs = new IRBranch(irMethod, EIROpCode.BrLabel, startIRData.data );
                m_IRStatements.Add(mirbs);

                //-------------------------else label
                m_IRStatements.Add(endIRData);


                //----------------------------var1 = null   ir: load null ir: store current stack
                IRLoadVariable irlv22 = IRLoadVariable.CreateLoadVariable(null, null, irMethod, ms.forIterateVariable);
                m_IRStatements.Add(irlv22);

                IRData _load_null_ = new IRData();
                _load_null_.opCode = EIROpCode.LoadConstNull;
                IRBase _load_null_base_ = new IRBase(_load_null_);
                m_IRStatements.Add(_load_null_base_);

                IRData _save_null_store_ = new IRData();
                _save_null_store_.opCode = EIROpCode.StoreNotStaticField1;
                _save_null_store_.opValue = irmethodcall;
                _save_null_store_.index = 1;

                IRBase _save_null_sotre_null_ = new IRBase(_save_null_store_);
                m_IRStatements.Add(_save_null_sotre_null_);
            }
            else
            {
                /*
                if (m_NewStatements != null)
                {
                    m_NewStatements.ParseIRStatements();
                    m_IRStatements.AddRange(m_NewStatements.irStatements);
                }
                else if (m_AssignStatements != null)
                {
                    m_AssignStatements.ParseIRStatements();
                    m_IRStatements.AddRange(m_AssignStatements.irStatements);
                }
                forStartIRData = new IRNop(irMethod);
                m_IRStatements.Add(forStartIRData);

                if (m_StepStatements != null)
                {
                    m_StepStatements.ParseIRStatements();
                    m_IRStatements.AddRange(m_StepStatements.irStatements);
                }

                if (m_ConditionExpress != null)
                {
                    m_IRConditionExpress = new IRExpress(irMethod, m_ConditionExpress);
                    m_IRStatements.Add(m_IRConditionExpress);

                    ifIRData = new IRBranch(irMethod, EIROpCode.BrFalse, endIRData.data);
                    m_IRStatements.Add(ifIRData);
                }
                m_ThenMetaStatements.ParseAllIRStatements();
                m_IRStatements.AddRange(m_ThenMetaStatements.irStatements);
                
                brIRData = new IRBranch(irMethod, EIROpCode.Br, forStartIRData.data);
                m_IRStatements.Add(brIRData);
                */
            }
            m_IRStatements.Add(endIRData);

            if ( ms.nextMetaStatements != null)
            {
                IRBlockStatements irbs = new IRBlockStatements(irMethod);
                irbs.ParseAllIRStatements(ms.nextMetaStatements as MetaBlockStatements );
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("#for ");
            /*
            if (m_IsForIn)
            {
                sb.Append(m_ForMetaVariable.name);
                sb.Append(" in ");
                sb.Append(m_ForInContent.name);
            }
            sb.AppendLine("#");
            sb.Append("{");
            sb.Append(Environment.NewLine);

            if (!m_IsForIn)
            {
                if (m_NewStatements != null)
                {
                    sb.Append(m_NewStatements.ToIRString());
                }
                if (m_AssignStatements != null)
                {
                    sb.Append(m_AssignStatements.ToIRString());
                }

                if (m_ConditionExpress != null)
                {
                    sb.Append(Environment.NewLine);
                    for (int i = 0; i < deep + 1; i++)
                    {
                        sb.Append(Global.tabChar);
                    }
                    sb.Append("if ");
                    sb.Append(m_ConditionExpress.ToFormatString());
                    sb.Append("{break;}");
                    sb.Append(Environment.NewLine);
                }
                if (m_StepStatements != null)
                {
                    sb.Append(m_StepStatements.ToIRString());
                }
                sb.Append(m_ThenMetaStatements?.ToIRString());
            }
            else
            {
            }

            sb.Append("}");

            sb.Append(Environment.NewLine);
            */

            return sb.ToString();
        }
    }

    public class IRWhileDoWhileStatements : IRStatements
    {
        public IRWhileDoWhileStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        public IRNop startIRData = null;       //while 开始执行起点
        public IRNop whileStartIRData = null;    //while 循环返回起点
        public IRBranch ifIRData = null;            //判断是否到终点if判断
        public IRBranch brIRData = null;            //while结束点返回起点语句
        public IRNop endIRData = null;           //while语句点

        private IRExpress m_IRConditionExpress = null;
        public void ParseIRStatements(MetaWhileDoWhileStatements ms)
        {
            startIRData = new IRNop(irMethod);
            endIRData = new IRNop(irMethod);
            m_IRStatements.Add(startIRData);

            whileStartIRData = new IRNop(irMethod);
            m_IRStatements.Add(whileStartIRData);


            //if (m_IsWhile)
            //{
            //    if (m_ConditionExpress != null)
            //    {
            //        m_IRConditionExpress = new IRExpress(irMethod, m_ConditionExpress);
            //        m_IRStatements.Add(m_IRConditionExpress);

            //        ifIRData = new IRBranch(irMethod, EIROpCode.BrFalse, endIRData.data);
            //        m_IRStatements.Add(ifIRData);
            //    }
            //    m_ThenMetaStatements.ParseAllIRStatements();
            //    m_IRStatements.AddRange(m_ThenMetaStatements.irStatements);
            //}
            //else
            //{
            //    m_ThenMetaStatements.ParseAllIRStatements();
            //    m_IRStatements.AddRange(m_ThenMetaStatements.irStatements);

            //    if (m_ConditionExpress != null)
            //    {
            //        m_IRConditionExpress = new IRExpress(irMethod, m_ConditionExpress);
            //        m_IRStatements.Add(m_IRConditionExpress);

            //        ifIRData = new IRBranch(irMethod, EIROpCode.BrFalse, endIRData.data);
            //        m_IRStatements.Add(ifIRData);
            //    }
            //}

            //brIRData = new IRBranch(irMethod, EIROpCode.Br, whileStartIRData.data);
            //m_IRStatements.Add(brIRData);
            //m_IRStatements.Add(endIRData);

            //if (m_ConditionExpress != null)
            //{
            //    ifIRData.data.SetDebugInfoByToken(m_ConditionExpress.GetToken());
            //}
            //if (m_FileMetaKeyWhileSyntax != null)
            //{
            //    brIRData.data.SetDebugInfoByToken(m_FileMetaKeyWhileSyntax.executeBlockSyntax.endBlock);
            //    startIRData.data.SetDebugInfoByToken(m_FileMetaKeyWhileSyntax.token);
            //    whileStartIRData.data.SetDebugInfoByToken(m_FileMetaKeyWhileSyntax.executeBlockSyntax.beginBlock);
            //    endIRData.data.SetDebugInfoByToken(m_FileMetaKeyWhileSyntax.executeBlockSyntax.endBlock);
            //}

            //if (m_NextMetaStatements != null)
            //{
            //    m_NextMetaStatements.ParseIRStatements();
            //}
        }

        //public override string ToIRString()
        //{
        //    StringBuilder sb = new StringBuilder();

        //    sb.AppendLine("#" + (m_IsWhile ? "while" : "dowhile#   {") );
        //    sb.AppendLine( base.ToIRString() );
        //    sb.AppendLine("}");
        //    if (m_ConditionExpress != null)
        //    {
        //        sb.AppendLine("#condition" + m_ConditionExpress.ToFormatString() + "#");
        //        sb.AppendLine("{");
        //        sb.Append(m_IRConditionExpress.ToIRString());
        //        sb.AppendLine("}");
        //    }

        //    sb.AppendLine(m_ThenMetaStatements.ToIRString());

        //    sb.AppendLine("}");

        //    return sb.ToString();
        //}
    }
}
