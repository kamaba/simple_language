//****************************************************************************
//  File:      IRAssignStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/11 12:00:00
//  Description:  handle assign statements syntax to instruction r!
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.IR;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.Parse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using static SimpleLanguage.Core.MetaVariable;

namespace SimpleLanguage.IR
{
    public class IRAssignStatements : IRStatements
    {
        public IRAssignStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        protected IRExpress m_IRExpress = null;
        protected IRStoreVariable m_StoreVariable = null;
        public void ParseIRStatements( MetaAssignStatements ms )
        {
            var clist = ms.leftMetaExpress.metaCallLink.callNodeList;
            if ( ms.isNewStatements )
            {

            }
            else
            {
                if (ms.finalMetaExpress != null)
                {
                    m_IRExpress = new IRExpress(irMethod, ms.finalMetaExpress);
                    m_IRStatements.Add(m_IRExpress);
                }
                for (int i = 0; i < clist.Count; i++)
                {
                    var cl = clist[i];
                    if (i < clist.Count - 1)
                    {
                        var list = IRMetaCallLink.ExecOnceCnode(this.irMethod, cl);
                        m_IRStatements.AddRange(list);
                    }
                    else
                    {
                        if (cl.visitType == MetaVisitNode.EVisitType.Variable)
                        {
                            var mv = cl.GetRetMetaVariable();
                            List<IRBase> irList = new List<IRBase>();
                            IRMetaClass irmc = irMethod.irManager.GetIRMetaClassByName(mv.ownerMetaClass.allClassName);
                            if (cl.genTemplateMetaClass != null)
                            {
                                IRData sc2 = new IRData();
                                sc2.opCode = EIROpCode.SetCallClass;
                                irmc = irMethod.irManager.GetIRMetaClassByName(cl.genTemplateMetaClass.allClassName);
                                sc2.opValue = irmc;
                                IRBase irbase22 = new IRBase(sc2);
                                irList.Add(irbase22);
                            }
                            else if (cl.callerMetaClass?.isTemplateClass == true)
                            {
                                IRData sc2 = new IRData();
                                sc2.opCode = EIROpCode.SetCurrentClassCallClass;
                                IRBase irbase22 = new IRBase(sc2);
                                irList.Add(irbase22);
                            }


                            m_IRStatements.AddRange(irList);

                            IRStoreVariable irsv = IRStoreVariable.CreateIRStoreVariable(irMethod, irmc, clist[i].variable);
                            m_IRStatements.Add(irsv);

                            if (irList.Count > 0)
                            {
                                List<IRBase> irList22 = new List<IRBase>();
                                IRData sc2end = new IRData();
                                sc2end.opCode = EIROpCode.UnSetCallClass;
                                IRBase irbase = new IRBase(sc2end);
                                irList22.Add(irbase);
                                m_IRStatements.AddRange(irList22);
                            }

                        }
                        else
                        {
                            Log.AddGenIR(EError.None, "------------------------------------------");
                        }
                    }
                }
            }


        }
    }
}
