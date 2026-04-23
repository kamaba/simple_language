//****************************************************************************
//  File:      IRAssignStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/11 12:00:00
//  Description:  handle assign statements syntax to instruction r!
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.IR;
using SimpleLanguage.Logging;

namespace SimpleLanguage.IR
{
    public class IRAssignStatements : IRStatements
    {
        protected IRExpressBase m_IRExpress = null;
        protected IRStoreVariable m_StoreVariable = null;
        public IRAssignStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        public void ParseIRStatements(MetaAssignStatements ms)
        {
            var clist = ms.leftMetaExpress.metaCallLink.visitNodeList;
            if( clist.Count == 0 )
            {
                Log.AddIRLog(LID.ShowExtendMessage, ms.leftMetaExpress.token, "AssignStatement 没有可生成的表达式");
                return;
            }
            if( ms.leftMethodCall != null )
            {
                // Setter assignment form: `a.prop = x` already got x into MetaMethodCall's
                // input parameter list by MetaAssignStatements. So just execute the whole
                // call-link on the left side and return.
                for (int i = 0; i < clist.Count; i++)
                {
                    var list = IRMetaCallLink.ExecOnceCnode(this.irMethod, clist[i]);
                    m_IRStatements.AddRange(list);
                }
                return;
            }


            MetaVisitNode lastCL = null;
            for (int i = 0; i < clist.Count; i++)
            {
                if (i < clist.Count - 1)
                {
                    var list = IRMetaCallLink.ExecOnceCnode(this.irMethod, clist[i]);
                    m_IRStatements.AddRange(list);
                }
                else
                {
                    lastCL = clist[i];
                }
            }
            if (lastCL.visitType == MetaVisitNode.EVisitType.VisitVariable)
            {
                MetaVisitVariable mvv = lastCL.visitVariable;
                IRExpressBase irexpress = IRExpressManager.CreateExpress(irMethod, mvv.visitExpressNode);
                m_IRStatements.Add(irexpress);

                if (ms.autoAddExpressOpSign != ELeftRightOpSign.None)
                {
                    IRDup irdup = new IRDup(this.irMethod, 2);
                    m_IRStatements.Add(irdup);

                    //IRMetaClass owirmc1 = IRManager.instance.GetIRMetaClassById(mvv.GetOwnerClassTemplateClass().GetHashCode());
                    if (mvv.isStatic)
                    {
                        Log.AddIRLog(LID.ShowExtendMessage, ms.token, "visit variable is Static");
                    }
                    IRLoadVariable irVar = new IRLoadVariable(null, this.irMethod, 0, IRMetaVariableFrom.Array);
                    m_IRStatements.Add(irVar);
                }
            }
            else if (lastCL.visitType == MetaVisitNode.EVisitType.MethodCall)
            {
                var mfc = lastCL.methodCall;
                IRCallFunction irCallFun = new IRCallFunction(this.irMethod);
                irCallFun.Parse(mfc);
                m_IRStatements.Add(irCallFun);
            }
            else if (lastCL.visitType == MetaVisitNode.EVisitType.Variable)
            {
                /*
                 * 
                 *
                 MetaVariable mv22 = lastCL.GetRetMetaVariable();

                IRMetaType irmt2 = null;
                IRMetaClass irmc2 = null;
                IRMetaClass owirmc2 = IRManager.instance.GetIRMetaClassById(mv22.GetOwnerClassTemplateClass().GetHashCode());
                if (mv22.isStatic)
                {
                    if (lastCL.callMetaType != null)
                    {
                        irmt = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(cnode.callMetaType, owirmc);
                    }
                    else
                    {
                        irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mv.defineMetaType, owirmc);
                    }
                    irmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());
                }
                else
                {
                    irmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());
                }
                IRLoadVariable irVar = IRLoadVariable.CreateLoadVariable(irmt, irmc, _irMethod, mv);
                irList.Add(irVar);
                 */

                if (ms.autoAddExpressOpSign != ELeftRightOpSign.None)
                {
                    MetaVariable mvtt = lastCL.GetOrgTemplateMetaVariable();
                    // read-modify-write: keep one instance for StoreNotStaticField2 after the load.
                    // template-specialized / copied members are still MetaMemberVariable; require dup even if
                    // variableFrom was not normalized to Member|Global.
                    bool needInstanceDup = mvtt != null && !mvtt.isStatic
                        && (mvtt.variableFrom == MetaVariable.EVariableFrom.Member
                            || mvtt.variableFrom == MetaVariable.EVariableFrom.Global
                            || mvtt is MetaMemberVariable);
                    if (needInstanceDup)
                    {
                        m_IRStatements.Add(new IRDup(this.irMethod));
                    }

                    if (lastCL.variable.isStatic)
                    {
                        Log.AddIRLog(LID.ShowExtendMessage, lastCL.variable.token, "visit variable is Static");
                    }

                    var list = IRMetaCallLink.ExecOnceCnode(this.irMethod, lastCL);
                    if (list == null)
                    {
                        Log.AddIRLog(LID.IRMethodNotFoundVariable, ms.leftMetaExpress?.token, "compound assign: lvalue load list is null");
                    }
                    else
                    {
                        for (int li = 0; li < list.Count; li++)
                        {
                            if (list[li] == null)
                            {
                                Log.AddIRLog(LID.IRMethodNotFoundVariable, ms.leftMetaExpress?.token, "compound assign: lvalue load IR is null (LoadNotStaticField / load not emitted).");
                                break;
                            }
                        }
                        m_IRStatements.AddRange(list);
                    }
                }

            }
            else
            {
                Log.AddIRLog(LID.IRVisitNodeNotHandleType, "visit variable is Static");
            }

            //如果不是 a.setValue(xxx)这种方式，那么就执行右边的表达式
            if (ms.rightMetaExpress != null)
            {
                m_IRExpress = IRExpressManager.CreateExpress(irMethod, ms.rightMetaExpress);
                m_IRStatements.Add(m_IRExpress);
            }

            IRData irsign = IRUtil.CreateLeftAndRightIRData(ms.autoAddExpressOpSign);
            if( irsign != null && irsign.opCode != EIROpCode.Nop )
            {
                IRBase irbase = new IRBase(irsign);
                m_IRStatements.Add(irbase);
            }
            
            var mv = lastCL.GetRetMetaVariable();

            var irmc = IRManager.instance.GetIRMetaClassById(mv.defineMetaType.GetTemplateMetaClass().GetHashCode());
            var owirmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());
            var irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mv.defineMetaType, owirmc);

            IRStoreVariable irsv = IRStoreVariable.CreateIRStoreVariable(irmt, owirmc, irMethod, mv);
            m_IRStatements.Add(irsv);
            //if ( ms.isNewStatements )
            //{
            //    MetaVisitNode finalMVN = null;
            //    for (int i = 0; i < clist.Count; i++)
            //    {
            //        finalMVN = clist[i];
            //        var list = IRMetaCallLink.ExecOnceCnode(this.irMethod, finalMVN);
            //        m_IRStatements.AddRange(list);
            //    }

            //    if (finalMVN == null)
            //    {
            //        Log.AddIRLog(LID.Unknown, "没有最终表达式，错误处理");
            //        return;
            //    }

            //    if (ms.rightMetaExpress != null)
            //    {
            //        m_IRExpress = new IRExpress(irMethod, ms.rightMetaExpress );
            //        m_IRStatements.Add(m_IRExpress);

            //        if (finalMVN.visitType == MetaVisitNode.EVisitType.Variable)
            //        {
            //            //这种是  Obja.Objb = new()的方式
            //            var mv = finalMVN.GetRetMetaVariable();

            //            var irmc = IRManager.instance.GetIRMetaClassById(mv.defineMetaType.GetTemplateMetaClass().GetHashCode());
            //            var owirmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());

            //            IRStoreVariable irsv = IRStoreVariable.CreateIRStoreVariable( IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mv.defineMetaType, owirmc), irmc, irMethod, finalMVN.GetOrgTemplateMetaVariable() );
            //            m_IRStatements.Add(irsv);

            //        }
            //        //else if (finalMVN.visitType == MetaVisitNode.EVisitType.MethodCall)
            //        //{
            //        //    //这种是  Obja.Objb_set( new() )的方式
            //        //}
            //        else
            //        {
            //            Debug.Assert(false, "这里应该只有变量和方法调用两种方式");
            //            Log.AddIRLog(LID.Unknown, "------------------------------------------");
            //        }
            //    }
            //    else
            //    {
            //        Log.AddIRLog(LID.Unknown, "这里应该有一个创建new的过程表达式");
            //    }
            //}
            //else
            //{
        }
    }
}
