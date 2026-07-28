//****************************************************************************
//  File:      IRBlockStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Core;
using SimpleLanguage.IR.Statements;
using System.Diagnostics;

namespace SimpleLanguage.IR
{
    public class IRBlockStatements : IRStatements
    {
        public IRNop blockStart => m_BlockStart;

        private IRNop m_BlockStart = null;
        public IRBlockStatements( IRMethod irmthod )
        {
            irMethod = irmthod;
            m_BlockStart = new IRNop(this.irMethod);
            m_IRStatements.Add(blockStart);
        }
        public void ParseIRStatements(MetaBlockStatements ms)
        {
            MetaStatements nextmbs = ms.nextMetaStatements;
            ParseAnyIRStatements(nextmbs);
        }
        public void ParseAllIRStatements(MetaBlockStatements ms)
        {
            MetaStatements nextmbs = ms.nextMetaStatements;
            ParseAnyIRStatements(nextmbs);
        }
        public void ParseAnyIRStatements( MetaStatements nextmbs )
        {
            while (nextmbs != null)
            {
                switch (nextmbs)
                {
                    case MetaBlockStatements mbs:
                        {
                            IRBlockStatements ibs = new IRBlockStatements(irMethod);
                            ibs.ParseIRStatements(mbs);
                            m_IRStatements.AddRange(ibs.m_IRStatements);
                        }
                        break;
                    case MetaDefineVarStatements mns:
                        {
                            IRDefineVarStatements mirns = new IRDefineVarStatements(irMethod);
                            mirns.ParseIRStatements(mns);
                            m_IRStatements.AddRange(mirns.irStatements);
                        }
                        break;
                    case MetaAssignStatements mas:
                        {
                            IRAssignStatements miras = new IRAssignStatements(irMethod);
                            miras.ParseIRStatements(mas);
                            m_IRStatements.AddRange(miras.irStatements);
                        }
                        break;
                    case MetaBreakStatements mbreaks:
                        {
                            IRBreakStatements mirbs = new IRBreakStatements(irMethod);
                            mirbs.ParseIRStatements(mbreaks);
                            m_IRStatements.AddRange(mirbs.irStatements);
                        }
                        break;
                    case MetaContinueStatements mcs:
                        {
                            IRContinueStatements mircs = new IRContinueStatements(irMethod);
                            mircs.ParseIRStatements(mcs);
                            m_IRStatements.AddRange(mircs.irStatements);
                        }
                        break;
                    case MetaGotoLabelStatements mgls:
                        {
                            IRGotoLabelStatements mirgls = new IRGotoLabelStatements(irMethod);
                            mirgls.ParseIRStatements(mgls);
                            m_IRStatements.AddRange(mirgls.irStatements);
                        }
                        break;
                    case MetaIfStatements mif:
                        {
                            IRIfStatements mirif = new IRIfStatements(irMethod);
                            mirif.ParseIRStatements(mif);
                            m_IRStatements.AddRange(mirif.irStatements);
                        }
                        break;
                    case MetaReturnStatements mirrs:
                        {
                            IRReturnStatements mirrss = new IRReturnStatements(irMethod);
                            mirrss.ParseIRStatements(mirrs);
                            m_IRStatements.AddRange(mirrss.irStatements);
                        }
                        break;
                    case MetaSwitchStatements mswitchs:
                        {
                            IRSwitchStatements mirss = new IRSwitchStatements(irMethod);
                            mirss.ParseIRStatements(mswitchs);
                            m_IRStatements.AddRange(mirss.irStatements);
                        }
                        break;
                    case MetaForStatements mfors:
                        {
                            IRForStatements mirfors = new IRForStatements(irMethod);
                            mirfors.ParseIRStatements(mfors);
                            m_IRStatements.AddRange(mirfors.irStatements);
                        }
                        break;
                    case MetaWhileDoWhileStatements mwdws:
                        {
                            IRWhileDoWhileStatements mirwdws = new IRWhileDoWhileStatements(irMethod);
                            mirwdws.ParseIRStatements(mwdws);
                            m_IRStatements.AddRange(mirwdws.irStatements);
                        }
                        break;
                    case MetaCallStatements mcs:
                        {
                            IRCallStatements ircs = new IRCallStatements(irMethod);
                            ircs.ParseIRStatements(mcs);
                            m_IRStatements.AddRange(ircs.irStatements);
                        }
                        break;
                    case MetaTryStatements mts:
                        {
                            IRTryStatements irts = new IRTryStatements(irMethod);
                            irts.ParseIRStatements(mts);
                            m_IRStatements.AddRange(irts.irStatements);
                        }
                        break;
                    case MetaThrowStatements mthrows:
                        {
                            IRThrowStatements irthrows = new IRThrowStatements(irMethod);
                            irthrows.ParseIRStatements(mthrows);
                            m_IRStatements.AddRange(irthrows.irStatements);
                        }
                        break;
                    case MetaDeferStatements _:
                    case MetaErrDeferStatements _:
                        // defer/errdefer blocks are emitted at function level by IRMethod,
                        // not at their in-line position.
                        break;
                    case MetaCheckedStatements mcs:
                        {
                            // Emit BeginChecked, block body, EndChecked
                            IRData beginChecked = new IRData();
                            beginChecked.opCode = EIROpCode.BeginChecked;
                            m_IRStatements.Add(new IRRawData(irMethod, beginChecked));

                            if (mcs.checkedBlockStatements != null)
                            {
                                IRBlockStatements irChecked = new IRBlockStatements(irMethod);
                                irChecked.ParseIRStatements(mcs.checkedBlockStatements);
                                m_IRStatements.AddRange(irChecked.irStatements);
                            }

                            IRData endChecked = new IRData();
                            endChecked.opCode = EIROpCode.EndChecked;
                            m_IRStatements.Add(new IRRawData(irMethod, endChecked));
                        }
                        break;
                    case MetaUncheckedStatements mus:
                        {
                            // Emit BeginUnchecked, block body, EndUnchecked
                            IRData beginUnchecked = new IRData();
                            beginUnchecked.opCode = EIROpCode.BeginUnchecked;
                            m_IRStatements.Add(new IRRawData(irMethod, beginUnchecked));

                            if (mus.uncheckedBlockStatements != null)
                            {
                                IRBlockStatements irUnchecked = new IRBlockStatements(irMethod);
                                irUnchecked.ParseIRStatements(mus.uncheckedBlockStatements);
                                m_IRStatements.AddRange(irUnchecked.irStatements);
                            }

                            IRData endUnchecked = new IRData();
                            endUnchecked.opCode = EIROpCode.EndUnchecked;
                            m_IRStatements.Add(new IRRawData(irMethod, endUnchecked));
                        }
                        break;
                    case MetaOtherPlatformStatements mops:
                        {
                            Debug.Write("------------------没有解析IR的语句类型------------");
                            //MetaIRCSharpCallStatements mcsharpcsinst = new MetaCSharpCallStatements(mcsharpcs, )
                        }
                        break;
                    default:
                        {
                            Debug.Write("------------------没有解析IR的语句类型------------");
                        }
                        break;
                }
                nextmbs = nextmbs.nextMetaStatements;
            }
        }
    }
}
