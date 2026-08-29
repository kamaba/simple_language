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
using System;

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
            var clist = ms.leftMetaExpress.visitNodeList;
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

            var mv = lastCL.GetReturnMetaVariable();


            if( lastCL.methodCall != null && lastCL.methodCall.function.name == "_setItem_" )
            {
                // 赋值场景：_setItem_ 已经包含 value 参数，直接执行方法调用
                // 读取场景：_getItem_ 需要执行方法调用并保留返回值
                IRCallFunction irCallFun = new IRCallFunction(this.irMethod);
                irCallFun.Parse(lastCL.methodCall);
                m_IRStatements.Add(irCallFun);

                // 读取场景：返回值已在栈上，不需要额外处理
                // 赋值场景：m_RightMetaExpress 已被消费（在 MetaAssignStatements 中设置），直接返回
                if (ms.rightMetaExpress == null)
                {
                    return;
                }
                return;
            }

            // 如果左侧最后一个访问节点是方法调用且返回 void（返回类型为 void），
            // 则不应该继续处理赋值（没有可存储的返回值）。
            // void 方法的 returnMetaVariable 不为 null，但其类型是 void。
            // setter 场景（a.prop = x）：右值已被 MetaCallLink 的 setterFunction 机制
            // 消费进 setter 的参数列表，这里必须生成 setter 调用指令，
            // 否则赋值完全不生效且会在栈上遗留接收者（栈失衡）。
            if (mv == null || mv.GetFinalMetaType()?.metaClass == CoreMetaClassManager.voidMetaClass)
            {
                // void 方法调用作为赋值左侧：执行方法调用，不生成 store。
                if (lastCL.visitType == MetaVisitNode.EVisitType.MethodCall && lastCL.methodCall != null)
                {
                    IRCallFunction irCallFun = new IRCallFunction(this.irMethod);
                    irCallFun.Parse(lastCL.methodCall);
                    m_IRStatements.Add(irCallFun);
                }
                // 右侧表达式仍然需要执行（可能有副作用），但不需要存储。
                // setter 场景右值为 null（已被消费），不会重复执行。
                if (ms.rightMetaExpress != null)
                {
                    m_IRExpress = IRExpressManager.CreateExpress(irMethod, ms.rightMetaExpress);
                    m_IRStatements.Add(m_IRExpress);
                }
                return;
            }
            if (lastCL.visitType == MetaVisitNode.EVisitType.VisitVariable)
            {
                MetaVisitVariable mvv = lastCL.visitVariable;

                // MethodCall 模式：_getItem_/_setItem_ 下标访问
                if (mvv.visitType == MetaVisitVariable.EVisitType.MethodCall)
                {
                    // 赋值场景：_setItem_ 已经包含 value 参数，直接执行方法调用
                    // 读取场景：_getItem_ 需要执行方法调用并保留返回值
                    IRCallFunction irCallFun = new IRCallFunction(this.irMethod);
                    irCallFun.Parse(mvv.methodCall);
                    m_IRStatements.Add(irCallFun);

                    // 读取场景：返回值已在栈上，不需要额外处理
                    // 赋值场景：m_RightMetaExpress 已被消费（在 MetaAssignStatements 中设置），直接返回
                    if (ms.rightMetaExpress == null)
                    {
                        return;
                    }
                }
                else
                {
                    // 原有数组访问逻辑
                    IRExpressBase irexpress = IRExpressManager.CreateExpress(irMethod, mvv.visitExpressNode);
                    m_IRStatements.Add(irexpress);

                    if (ms.autoAddExpressOpSign != ELeftRightOpSign.None)
                    {
                        IRDup irdup = new IRDup(this.irMethod, 2);
                        m_IRStatements.Add(irdup);

                        //IRMetaClass owirmc1 = IRManager.instance.GetIRMetaClassById(mvv.GetOwnerClassTemplateClass().GetHashCode());
                        if (mvv.isStatic)
                        {
                            Log.AddIRLog(LID.ShowExtendMessage, ms.token, "IRAssignStatement visit variable is Static");
                        }
                        IRLoadVariable irVar = new IRLoadVariable(null, this.irMethod, 0, IRMetaVariableFrom.Array);
                        m_IRStatements.Add(irVar);
                    }
                }
            }
            else if (lastCL.visitType == MetaVisitNode.EVisitType.MethodCall)
            {
                var mfc = lastCL.methodCall;
                IRCallFunction irCallFun = new IRCallFunction(this.irMethod);
                irCallFun.Parse(mfc);
                m_IRStatements.Add(irCallFun);
            }
            else if (lastCL.visitType == MetaVisitNode.EVisitType.Variable || lastCL.visitType == MetaVisitNode.EVisitType.EnumMember )
            {
                if( lastCL.visitType == MetaVisitNode.EVisitType.EnumMember )
                {
                    var fieldOwner = IRManager.GetIRMetaClassByMetaVariable(mv);
                    var index = fieldOwner.GetMetaMemberVariableIndexByHashCode(mv.GetHashCode());
                    var irvar = new IRLoadVariable(new IRMetaType(fieldOwner), irMethod, index, IRMetaVariableFrom.Static);

                    m_IRStatements.Add(irvar);
                }
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
                        && (mvtt.variableFrom == MetaVariable.EVariableFrom.ClassMember
                            || mvtt.variableFrom == MetaVariable.EVariableFrom.Global
                            || mvtt is MetaMemberVariable);
                    if (needInstanceDup)
                    {
                        m_IRStatements.Add(new IRDup(this.irMethod));
                    }

                    //if (lastCL.variable.isStatic)
                    //{
                    //    Log.AddIRLog(LID.ShowExtendMessage, ms.token, "IRAssignStatement visit variable is Static2");
                    //}

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

                // If the RHS expression type differs from the target variable's
                // type (and both are numeric), emit a Convert before the store.
                // e.g.  b8 = someInt32Var;  -> LoadLocal + Convert_I8 + StoreLocal
                var expType = ms.rightMetaExpress.GetReturnMetaType();
                var varType = mv.GetFinalMetaType();
                if (expType != null && varType != null)
                {
                    var expEType = CoreMetaClassManager.GetETypeByMetaClass(expType.metaClass);
                    var varEType = CoreMetaClassManager.GetETypeByMetaClass(varType.metaClass);
                    if (expEType != varEType
                        && NumberManager.IsNumericEType(expEType)
                        && NumberManager.IsNumericEType(varEType))
                    {
                        IRConvert irconv = new IRConvert(irMethod, expEType, varEType);
                        m_IRStatements.Add(irconv);
                    }
                }
            }

            IRData irsign = IRUtil.CreateLeftAndRightIRData(ms.autoAddExpressOpSign, out bool flag );
            if( irsign != null && irsign.opCode != EIROpCode.Nop )
            {
                IRBase irbase = new IRBase(irsign);
                m_IRStatements.Add(irbase);
            }            

            var owirmc = IRManager.GetIRMetaClassByMetaVariable(mv);
            if ( lastCL.callMetaType != null )
            {
                if( lastCL.callMetaType.isEnumMember )
                {
                    MetaType mt = new MetaType(CoreMetaClassManager.memberMetaClass);
                    var irmtsv = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mt, owirmc);
                    IRStoreVariable irsv = new IRStoreVariable(irmtsv, irMethod, 2, IRMetaVariableFrom.Member );
                    m_IRStatements.Add(irsv);
                }
                else
                {
                    // 使用原始模板变量查找 IRMetaClass，与 Load 路径一致
                    var orgMv = lastCL.GetOrgTemplateMetaVariable();
                    var orgOwirmc = orgMv != null ? IRManager.GetIRMetaClassByMetaVariable(orgMv) : owirmc;
                    var irmt = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(lastCL.callMetaType, orgOwirmc);
                    int hashcode = 0;
                    if (mv.sourceMetaVariable != null)
                    {
                        hashcode = mv.sourceMetaVariable.GetHashCode();
                    }
                    else
                    {
                        hashcode = mv.GetHashCode();
                    }
                    int index = irmt.irMetaClass.GetMetaMemberVariableIndexByHashCode(hashcode);
                    IRStoreVariable irsv = new IRStoreVariable(irmt, irMethod, index, IRMetaVariableFrom.Static);
                    m_IRStatements.Add(irsv);
                }
            }
            else
            {
                var irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mv.GetFinalMetaType(), owirmc);

                IRStoreVariable irsv = IRStoreVariable.CreateIRStoreVariable(irmt, owirmc, irMethod, mv);
                m_IRStatements.Add(irsv);
            }
        }
    }
}
