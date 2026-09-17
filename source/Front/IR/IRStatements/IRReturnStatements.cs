//****************************************************************************
//  File:      IRReturnStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/14 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Core;
using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRReturnStatements : IRStatements
    {
        public IRReturnStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        private IRExpressBase m_ReturnValueExpress = null;
        public void ParseIRStatements(MetaReturnStatements ms)
        {
            // ── inline 展开段内的 ret 拦截改写 (M1b §5.5(b)) ──
            // 展开期不写宿主返回槽、不跳宿主函数末尾, 改写为写展开结果槽 + 跳段尾锚
            var inlineCtx = this.irMethod.PeekInlineExpansion();
            if (inlineCtx != null)
            {
                ParseInlineRetStatements(ms, inlineCtx);
                return;
            }
            // ── result 关键字: 值返回改写 ──
            // ret expr => result.value = expr; ret result
            // 裸 ret   => ret result
            if (ms.isResultValueReturn && ms.resultMetaVariable != null)
            {
                if (ParseResultValueReturnIRStatements(ms))
                {
                    return;
                }
                // 生成失败时退回普通返回路径
            }
            if (ms.express != null)
            {
                // ── O3 常数融合：ret <常量> 时把 [LoadConst*][StoreReturn] 融合为
                //    单条 StoreReturnConstValue（常量与类型嵌入 payload）。
                //    融合不成功则完整回退经典 LoadConst + StoreReturn 路径。 ──
                if (ms.express is MetaConstExpressNode retConstNode
                    && IRStoreVariable.TryCreateReturnConstValueStore(this.irMethod, retConstNode, out IRStoreVariable fusedRetStore))
                {
                    m_IRStatements.Add(fusedRetStore);
                }
                else
                {
                    m_ReturnValueExpress = IRExpressManager.CreateExpress(this.irMethod, ms.express);
                    m_IRStatements.Add(m_ReturnValueExpress);

                    IRStoreVariable irsv = IRStoreVariable.CreateStaticReturnIRSV(this.irMethod, ms?.token);
                    m_IRStatements.Add(irsv);
                }
            }
            // 裸 ret（void 函数）也必须生成跳转到函数结束，
            // 否则块内的提前返回会退化为顺序执行后续语句。
            IRBranch irbranch = new IRBranch(this.irMethod, EIROpCode.BrLabel, irMethod.funEndLabelData );
            m_IRStatements.Add(irbranch);
        }

        /// <summary>
        /// result 关键字: 值返回改写的 IR 发射。
        ///   ret expr => [Load result][expr][StoreNotStaticField2 value][Load result][StoreReturn][BrLabel end]
        ///   裸 ret   => [Load result][StoreReturn][BrLabel end]
        /// 栈序: StoreNotStaticField2 弹出 value(顶) 与 instance(次顶), 故先压 result 再压 expr。
        /// </summary>
        private bool ParseResultValueReturnIRStatements(MetaReturnStatements ms)
        {
            var rmv = ms.resultMetaVariable;
            var irmv = this.irMethod.GetIRLocalVariableById(rmv.GetHashCode());
            if (irmv == null)
            {
                return false;
            }
            var mt = rmv.GetFinalMetaType();
            var irmc = IRManager.GetIRMetaClassByMetaType(mt);
            if (irmc == null)
            {
                return false;
            }
            var loadIrmt = new IRMetaType(irmc);

            if (ms.express != null)
            {
                int fieldIndex = irmc.GetMetaMemberVariableIndexByName("value");
                if (fieldIndex < 0)
                {
                    Log.AddIRLog(LID.IRReturnStatementNotFoundResultValue, ms.token, "result value return: not found value field!");
                    return false;
                }
                // result.value = expr : [Load result][expr][StoreNotStaticField2 value]
                IRLoadVariable loadResult = IRLoadVariable.CreateLoadVariable(loadIrmt, irmc, this.irMethod, rmv);
                m_IRStatements.Add(loadResult);

                m_ReturnValueExpress = IRExpressManager.CreateExpress(this.irMethod, ms.express);
                m_IRStatements.Add(m_ReturnValueExpress);

                IRStoreVariable storeField = new IRStoreVariable(loadIrmt, this.irMethod, fieldIndex, IRMetaVariableFrom.Member);
                m_IRStatements.Add(storeField);
            }

            // ret result : [Load result][StoreReturn]
            IRLoadVariable loadResult2 = IRLoadVariable.CreateLoadVariable(loadIrmt, irmc, this.irMethod, rmv);
            m_IRStatements.Add(loadResult2);

            IRStoreVariable irsv = IRStoreVariable.CreateStaticReturnIRSV(this.irMethod, ms?.token);
            m_IRStatements.Add(irsv);

            IRBranch irbranch = new IRBranch(this.irMethod, EIROpCode.BrLabel, irMethod.funEndLabelData);
            m_IRStatements.Add(irbranch);
            return true;
        }

        /// <summary>
        /// inline 展开段内的 ret 改写 (M1b §5.5(b)):
        ///   ret expr => [expr IR][StoreLocal 结果槽][BrLabel 展开段尾锚]
        ///   裸 ret   => [BrLabel 展开段尾锚]
        /// 替代普通路径的 StoreReturn(写宿主返回槽) + BrLabel funEnd(跳宿主函数末尾):
        /// 展开段无独立返回槽/函数末尾, 值落结果槽、控制流跳段尾锚由调用点统一收尾。
        /// 注意: O3 常数融合(StoreReturnConstValue 写宿主返回槽)在展开段不可用, 完整回退经典路径。
        /// </summary>
        private void ParseInlineRetStatements(MetaReturnStatements ms, IRMethod.InlineExpansionContext ctx)
        {
            // result 值返回 (函数返回类型为 Result/Result<T> 时的自动改写) 同步适配到展开段
            if (ms.isResultValueReturn && ms.resultMetaVariable != null)
            {
                if (ParseInlineResultValueRet(ms, ctx))
                {
                    return;
                }
                // 生成失败时退回下面的普通 inline ret 路径
            }
            if (ms.express != null)
            {
                m_ReturnValueExpress = IRExpressManager.CreateExpress(this.irMethod, ms.express);
                m_IRStatements.Add(m_ReturnValueExpress);

                // StoreLocal 结果槽 (void 方法 resultSlot 为 null, 语义层已拦截 void ret expr)
                if (ctx.resultSlot != null)
                {
                    IRStoreVariable storeResult = new IRStoreVariable(null, this.irMethod,
                        ctx.resultSlot.index, IRMetaVariableFrom.LocalStatement);
                    m_IRStatements.Add(storeResult);
                }
            }
            // 裸 ret (void 函数) 也必须跳段尾锚, 否则块内提前返回退化为顺序执行后续语句
            IRBranch irbranch = new IRBranch(this.irMethod, EIROpCode.BrLabel, ctx.endAnchorData);
            m_IRStatements.Add(irbranch);
        }

        /// <summary>
        /// inline 展开段内的 result 值返回改写:
        ///   ret expr => [Load result][expr][StoreNotStaticField2 value][Load result][StoreLocal 结果槽][BrLabel 段尾锚]
        ///   裸 ret   => [Load result][StoreLocal 结果槽][BrLabel 段尾锚]
        /// result 变量经展开段体预注册进入宿主局部表, Load 走工厂方法正常路由。
        /// </summary>
        private bool ParseInlineResultValueRet(MetaReturnStatements ms, IRMethod.InlineExpansionContext ctx)
        {
            var rmv = ms.resultMetaVariable;
            var irmv = this.irMethod.GetIRLocalVariableById(rmv.GetHashCode());
            if (irmv == null)
            {
                return false;
            }
            var mt = rmv.GetFinalMetaType();
            var irmc = IRManager.GetIRMetaClassByMetaType(mt);
            if (irmc == null)
            {
                return false;
            }
            var loadIrmt = new IRMetaType(irmc);

            if (ms.express != null)
            {
                int fieldIndex = irmc.GetMetaMemberVariableIndexByName("value");
                if (fieldIndex < 0)
                {
                    Log.AddIRLog(LID.IRReturnStatementNotFoundResultValue, ms.token,
                        "inline result value return: not found value field!");
                    return false;
                }
                // result.value = expr : [Load result][expr][StoreNotStaticField2 value]
                IRLoadVariable loadResult = IRLoadVariable.CreateLoadVariable(loadIrmt, irmc, this.irMethod, rmv);
                m_IRStatements.Add(loadResult);

                m_ReturnValueExpress = IRExpressManager.CreateExpress(this.irMethod, ms.express);
                m_IRStatements.Add(m_ReturnValueExpress);

                IRStoreVariable storeField = new IRStoreVariable(loadIrmt, this.irMethod, fieldIndex, IRMetaVariableFrom.Member);
                m_IRStatements.Add(storeField);
            }

            // ret result : [Load result][StoreLocal 结果槽]
            IRLoadVariable loadResult2 = IRLoadVariable.CreateLoadVariable(loadIrmt, irmc, this.irMethod, rmv);
            m_IRStatements.Add(loadResult2);

            if (ctx.resultSlot != null)
            {
                IRStoreVariable storeResult = new IRStoreVariable(null, this.irMethod,
                    ctx.resultSlot.index, IRMetaVariableFrom.LocalStatement);
                m_IRStatements.Add(storeResult);
            }

            IRBranch irbranch = new IRBranch(this.irMethod, EIROpCode.BrLabel, ctx.endAnchorData);
            m_IRStatements.Add(irbranch);
            return true;
        }
    }

    public class MetaIRTRStatements
    {
        public void ParseIRStatements()
        {
        }
    }
}
