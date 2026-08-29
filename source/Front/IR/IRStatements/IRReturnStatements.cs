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
                m_ReturnValueExpress = IRExpressManager.CreateExpress(this.irMethod, ms.express);
                m_IRStatements.Add(m_ReturnValueExpress);

                IRStoreVariable irsv = IRStoreVariable.CreateStaticReturnIRSV(this.irMethod, ms?.token);
                m_IRStatements.Add(irsv);
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
                    Log.AddIRLog(LID.MetaCoreAssertShowMessage, ms.token, "result value return: not found value field!");
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
    }

    public class MetaIRTRStatements
    {
        public void ParseIRStatements()
        {
        }
    }
}
