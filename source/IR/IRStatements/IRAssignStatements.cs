//****************************************************************************
//  File:      IRAssignStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/11 12:00:00
//  Description:  handle assign statements syntax to instruction r!
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.IR;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.Parse;

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

                        var irmc = IRManager.instance.GetIRMetaClassById(mv.metaDefineType.GetTemplateMetaClass().GetHashCode());

                        IRStoreVariable irsv = IRStoreVariable.CreateIRStoreVariable( new IRMetaType(mv.metaDefineType), irmc, irMethod, finalMVN.GetOrgTemplateMetaVariable() );
                        m_IRStatements.Add(irsv);

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

                            var irmc = IRManager.instance.GetIRMetaClassById(mv.metaDefineType.GetTemplateMetaClass().GetHashCode());
                            IRMetaType irmt = null;
                            if (cl.callMetaType == null)
                            {
                                irmc = IRManager.instance.GetIRMetaClassById(mv.ownerMetaClass.GetHashCode());
                                irmt = new IRMetaType(mv.metaDefineType);
                            }
                            else
                            {
                                irmc = IRManager.instance.GetIRMetaClassById(mv.ownerMetaClass.GetHashCode());
                                irmt = new IRMetaType(cl.callMetaType);
                            }
                            //else
                            //    irmt = new IRMetaType(mv.ownerMetaClass, null);
                            IRStoreVariable irsv = IRStoreVariable.CreateIRStoreVariable(irmt, irmc, irMethod, clist[i].GetOrgTemplateMetaVariable() );
                            m_IRStatements.Add(irsv);
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
