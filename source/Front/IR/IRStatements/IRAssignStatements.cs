//****************************************************************************
//  File:      IRAssignStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/11 12:00:00
//  Description:  handle assign statements syntax to instruction r!
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.IR;
using System.Diagnostics;

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
                Debug.Assert(false, "AssignStatement 没有可生成的表达式");
                return;
            }
            if( ms.leftMethodCall != null )
            {
                //如果是 a.setValue(xxx)这种方式，那么就直接执行左边的表达式
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
            Debug.Assert(lastCL != null, "");

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
                        Debug.Assert(false);
                    }
                    IRLoadVariable irVar = new IRLoadVariable(null, this.irMethod, 0, IRMetaVariableFrom.Array);
                    m_IRStatements.Add(irVar);
                }
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
                    if (mvtt.variableFrom == MetaVariable.EVariableFrom.Member
                        || mvtt.variableFrom == MetaVariable.EVariableFrom.Global)
                    {
                        IRDup irdup = new IRDup(this.irMethod);
                        m_IRStatements.Add(irdup);
                    }

                    if (lastCL.variable.isStatic)
                    {
                        Debug.Assert(false);
                    }

                    var list = IRMetaCallLink.ExecOnceCnode(this.irMethod, lastCL);
                    m_IRStatements.AddRange(list);
                }

            }
            else
            {
                Debug.Assert(false);
            }

            //如果不是 a.setValue(xxx)这种方式，那么就执行右边的表达式
            if (ms.rightMetaExpress != null)
            {
                m_IRExpress = IRExpressManager.CreateExpress(irMethod, ms.rightMetaExpress);
                m_IRStatements.Add(m_IRExpress);
            }
            else
            {
                Debug.Assert(false);
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
            //        Log.AddGenIR(EError.None, "没有最终表达式，错误处理");
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
            //            Log.AddGenIR(EError.None, "------------------------------------------");
            //        }
            //    }
            //    else
            //    {
            //        Log.AddGenIR(EError.None, "这里应该有一个创建new的过程表达式");
            //    }
            //}
            //else
            //{
        }
    }
}
