//****************************************************************************
//  File:      IRClosureDefineStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/8/25 12:00:00
//  Description:  闭包定义语句的 IR 生成
//      1. 依 slot 顺序压捕获值 (宿主函数上下文加载宿主变量)
//      2. NewClosure funcIndex, captureCount  -> 弹捕获值, 压闭包对象
//      3. 闭包对象存入闭包变量 (宿主函数局部变量 StoreLocal)
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Logging;

namespace SimpleLanguage.IR
{
    public class IRClosureDefineStatements : IRStatements
    {
        public IRClosureDefineStatements( IRMethod method )
        {
            this.irMethod = method;
        }

        public void ParseIRStatements( MetaClosureDefineStatements ms )
        {
            if( ms == null || ms.closureMetaVariable == null )
            {
                return;
            }

            // 1. 压入宿主函数的共享捕获上下文数组 (__closure_ctx__ 局部变量)
            //    NewClosure 新协议: 弹 1 个 ctx 数组 -> 压闭包对象
            //    闭包与宿主共享同一个数组, 捕获槽读写互通 ("捕获即共享")
            var hostMmf = irMethod?.bindMetaFunction as MetaMemberFunction;
            if( hostMmf == null || !hostMmf.hasClosureContext )
            {
                Log.AddIRLog( LID.MetaCoreAssertShowMessage, ms.token, "closure host function missing __closure_ctx__" );
                return;
            }
            var ctxIrmv = irMethod.GetIRLocalVariableById( hostMmf.closureContextVariable.GetHashCode() );
            if( ctxIrmv == null )
            {
                Log.AddIRLog( LID.MetaCoreAssertShowMessage, ms.token, "closure context local variable not found: __closure_ctx__" );
                return;
            }
            IRData loadCtx = new IRData();
            loadCtx.opCode = EIROpCode.LoadLocal;
            loadCtx.index = ctxIrmv.index;
            loadCtx.SetDebugInfoByToken( ms.token, "LoadLocal __closure_ctx__" );
            m_IRStatements.Add( new IRBase( loadCtx ) );

            // 2. NewClosure: 弹 1 个共享 ctx 数组 -> 压闭包对象
            //    (payload 仅用于 methodId 定位闭包函数, 捕获值不再经栈传递)
            var closureIRM = ResolveClosureIRMethod( ms.closureFunction, ms.token );
            if( closureIRM == null )
            {
                return;
            }
            var imc = new IRMethodCall( null, null, closureIRM, 0 );
            IRData dataNew = new IRData();
            dataNew.opCode = EIROpCode.NewClosure;
            dataNew.SetOpValue( imc );
            dataNew.index = 0;
            dataNew.SetDebugInfoByToken( ms.token, "NewClosure " + ms.closureFunction?.name );
            m_IRStatements.Add( new IRBase( dataNew ) );

            // 3. 闭包对象存入闭包变量 (宿主函数局部变量)
            var irStore = IRStoreVariable.CreateIRStoreVariable( null, null, irMethod, ms.closureMetaVariable );
            if( irStore == null )
            {
                Log.AddIRLog( LID.IRMethodNotFoundVariable, ms.token, "closure variable store failed", irMethod?.id, ms.closureMetaVariable.name );
                return;
            }
            m_IRStatements.Add( irStore );
        }

        /// <summary>查找/创建闭包函数的 IRMethod (闭包函数在动态函数列表, TranslateIR 阶段已注册)</summary>
        internal static IRMethod ResolveClosureIRMethod( MetaMemberFunction closureFunction, Token token )
        {
            if( closureFunction == null )
            {
                Log.AddIRLog( LID.MetaCoreAssertShowMessage, token, "closure function is null" );
                return null;
            }
            var irm = IRManager.instance.GetIRMethod( closureFunction.functionAllName );
            if( irm == null )
            {
                irm = IRManager.instance.TranslateIRByFunction( closureFunction );
                if( irm != null )
                {
                    IRManager.instance.AddIRMethod( irm );
                }
            }
            if( irm == null )
            {
                Log.AddIRLog( LID.MetaCoreAssertShowMessage, token, "closure IRMethod not found: " + closureFunction.functionAllName );
            }
            return irm;
        }
    }
}
