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
                MetaVisitNode finalMVN = null;
                for (int i = 0; i < clist.Count; i++)
                {
                    finalMVN = clist[i];
                    var list = IRMetaCallLink.ExecOnceCnode(this.irMethod, finalMVN);
                    m_IRStatements.AddRange(list);
                }

                if (finalMVN == null)
                {
                    Log.AddGenIR(EError.None, "没有最终表达式，错误处理");
                    return;
                }
                
                if (ms.finalMetaExpress != null)
                {
                    m_IRExpress = new IRExpress(irMethod, ms.finalMetaExpress);
                    m_IRStatements.Add(m_IRExpress);

                    if (finalMVN.visitType == MetaVisitNode.EVisitType.Variable)
                    {
                        //这种是  Obja.Objb = new()的方式
                        var mv = finalMVN.GetRetMetaVariable();
                        List<IRBase> irList = new List<IRBase>();
                        IRMetaClass irmc = irMethod.irManager.GetIRMetaClassByName(mv.ownerMetaClass.allClassName);
                        if (finalMVN.callerMetaClass != null)
                        {
                            if( finalMVN.callerMetaClass is MetaGenTemplateClass mgtc )
                            {
                                IRData sc23 = new IRData();
                                sc23.opCode = EIROpCode.SetCallClass;
                                irmc = irMethod.irManager.GetIRMetaClassByName(mgtc.allClassName);
                                sc23.opValue = irmc;
                                IRBase irbase23 = new IRBase(sc23);
                                irList.Add(irbase23);
                            }
                            else
                            {
                                if( finalMVN.callerMetaClass?.isTemplateClass == true )
                                {
                                    IRData sc2 = new IRData();
                                    sc2.opCode = EIROpCode.SetCurrentClassCallClass;
                                    irmc = irMethod.irManager.GetIRMetaClassByName(finalMVN.callerMetaClass.allClassName);
                                    IRBase irbase22 = new IRBase(sc2);
                                    irList.Add(irbase22);
                                }
                            }
                        }

                        m_IRStatements.AddRange(irList);

                        IRStoreVariable irsv = IRStoreVariable.CreateIRStoreVariable(irMethod, irmc, finalMVN.variable);
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
                    else if (finalMVN.visitType == MetaVisitNode.EVisitType.MethodCall)
                    {
                        //这种是  Obja.Objb_set( new() )的方式
                    }
                    else
                    {
                        Log.AddGenIR(EError.None, "------------------------------------------");
                    }
                }
                else
                {
                    Log.AddGenIR(EError.None, "这里应该有一个创建new的过程表达式");
                }
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
                            if (cl.callerMetaClass != null)
                            {
                                if (cl.callerMetaClass is MetaGenTemplateClass mgtc)
                                {
                                    IRData sc23 = new IRData();
                                    sc23.opCode = EIROpCode.SetCallClass;
                                    irmc = irMethod.irManager.GetIRMetaClassByName(mgtc.allClassName);
                                    sc23.opValue = irmc;
                                    IRBase irbase23 = new IRBase(sc23);
                                    irList.Add(irbase23);
                                }
                                else
                                {
                                    if (cl.callerMetaClass?.isTemplateClass == true)
                                    {
                                        IRData sc2 = new IRData();
                                        sc2.opCode = EIROpCode.SetCurrentClassCallClass;
                                        irmc = irMethod.irManager.GetIRMetaClassByName(cl.callerMetaClass.allClassName);
                                        IRBase irbase22 = new IRBase(sc2);
                                        irList.Add(irbase22);
                                    }
                                }
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
                        else if( cl.visitType == MetaVisitNode.EVisitType.MethodCall )
                        {

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
