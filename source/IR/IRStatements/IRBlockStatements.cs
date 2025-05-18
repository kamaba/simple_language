//****************************************************************************
//  File:      IRBlockStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR.Statements;
using System.Diagnostics;

namespace SimpleLanguage.IR
{
    public class IRBlockStatements : IRStatements
    {
        public IRBlockStatements( IRMethod irmthod )
        {
            irMethod = irmthod;
        }

        public IRNop blockStart = null;        
        public void ParseAllIRStatements(MetaBlockStatements ms)
        {
            blockStart = new IRNop(this.irMethod);
            m_IRStatements.Add(blockStart);

            MetaStatements nextmbs = ms.nextMetaStatements;
            while (nextmbs != null)
            {
                switch(nextmbs)
                {
                    case MetaBlockStatements mbs:
                        {
                            blockStart = new IRNop(irMethod);
                            m_IRStatements.Add(blockStart);
                        }
                        break;
                    case MetaDefineVarStatements mns:
                        {
                            IRDefineVarStatements mirns = new IRDefineVarStatements(irMethod);
                            mirns.ParseIRStatements(mns);
                        }
                        break;
                    case MetaAssignStatements mas:
                        {
                            IRAssignStatements miras = new IRAssignStatements(irMethod);
                            miras.ParseIRStatements(mas);
                        }
                        break;
                    case MetaBreakStatements mbreaks:
                        {
                            IRBreakStatements mirbs = new IRBreakStatements(irMethod);
                            mirbs.ParseIRStatements(mbreaks);
                        }
                        break;
                    case MetaContinueStatements mcs:
                        {
                            IRContinueStatements mircs = new IRContinueStatements(irMethod);
                            mircs.ParseIRStatements(mcs);
                        }
                        break;
                    case MetaGotoLabelStatements mgls:
                        {
                            IRGotoLabelStatements mirgls = new IRGotoLabelStatements(irMethod);
                            mirgls.ParseIRStatements(mgls);
                        }
                        break;
                    case MetaIfStatements mif:
                        {
                            IRIfStatements mirif = new IRIfStatements(irMethod);
                            mirif.ParseIRStatements(mif);
                        }
                        break;
                    case MetaReturnStatements mirrs:
                        {
                            IRReturnStatements mirrss = new IRReturnStatements(irMethod);
                            mirrss.ParseIRStatements(mirrs);
                        }
                        break;
                    case MetaSwitchStatements mswitchs:
                        {
                            IRSwitchStatements mirss = new IRSwitchStatements(irMethod);
                            mirss.ParseIRStatements(mswitchs);
                        }
                        break;
                    case MetaForStatements mfors:
                        {
                            IRForStatements mirfors = new IRForStatements(irMethod);
                            mirfors.ParseIRStatements(mfors);
                        }
                        break;
                    case MetaWhileDoWhileStatements mwdws:
                        {
                            IRWhileDoWhileStatements mirwdws = new IRWhileDoWhileStatements(irMethod);
                            mirwdws.ParseIRStatements(mwdws);
                        }
                        break;
                    case MetaOtherPlatformStatements mops:
                        {
                            //MetaIRCSharpCallStatements mcsharpcsinst = new MetaCSharpCallStatements(mcsharpcs, )
                        }
                        break;
                    default:
                        {
                            Debug.Write("------------------没有解析IR的语句类型------------");
                        }
                        break;
                }
                m_IRStatements.AddRange(irStatements);
                nextmbs = nextmbs.nextMetaStatements;
            }
        }
    }
}
