//****************************************************************************
//  File:      IRBlockStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.IRStatements;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR.Statements
{
    public class IRBlockStatements : MetaIRStatements
    {
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
                            blockStart.data.SetDebugInfoByToken(ms.GetToken());
                            m_IRStatements.Add(blockStart);
                        }
                        break;
                    case MetaDefineVarStatements mns:
                        {
                            MetaIRDefineVarStatements mirns = new MetaIRDefineVarStatements(irMethod);
                            mirns.ParseIRStatements(mns);
                        }
                        break;
                    case MetaAssignStatements mas:
                        {
                            MetaIRAssignStatements miras = new MetaIRAssignStatements();
                            miras.ParseIRStatements(mas);
                        }
                        break;
                    case MetaBreakStatements mbreaks:
                        {
                            MetaIRBreakStatements mirbs = new MetaIRBreakStatements();
                            mirbs.ParseIRStatements(mbreaks);
                        }
                        break;
                    case MetaContinueStatements mcs:
                        {
                            MetaIRContinueStatements mircs = new MetaIRContinueStatements();
                            mircs.ParseIRStatements(mcs);
                        }
                        break;
                    case MetaGotoLabelStatements mgls:
                        {
                            MetaIRGotoLabelStatements mirgls = new MetaIRGotoLabelStatements();
                            mirgls.ParseIRStatements(mgls);
                        }
                        break;
                    case MetaIfStatements mif:
                        {
                            MetaIRIfStatements mirif = new MetaIRIfStatements();
                            mirif.ParseIRStatements(mif);
                        }
                        break;
                    case MetaReturnStatements mirrs:
                        {
                            MetaIRReturnStatements mirrss = new MetaIRReturnStatements();
                            mirrss.ParseIRStatements(mirrs);
                        }
                        break;
                    case MetaSwitchStatements mswitchs:
                        {
                            MetaIRSwitchStatements mirss = new MetaIRSwitchStatements();
                            mirss.ParseIRStatements(mswitchs);
                        }
                        break;
                    case MetaForStatements mfors:
                        {
                            MetaIRForStatements mirfors = new MetaIRForStatements();
                            mirfors.ParseIRStatements(mfors);
                        }
                        break;
                    case MetaWhileDoWhileStatements mwdws:
                        {
                            MetaIRWhileDoWhileStatements mirwdws = new MetaIRWhileDoWhileStatements();
                            mirwdws.ParseIRStatements(mwdws);
                        }
                        break;
                    default:
                        {
                            Console.WriteLine("------------------没有解析IR的语句类型------------");
                        }
                        break;
                }
                m_IRStatements.AddRange(irStatements);
                nextmbs = nextmbs.nextMetaStatements;
            }
        }
    }
}
