//****************************************************************************
//  File:     IRDefineVarStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/14 12:00:00
//  Description:
//****************************************************************************

using SimpleLanguage.Core;

using System.Text;

namespace SimpleLanguage.IR
{
    public class IRDefineVarStatements : IRStatements
    {
        IRExpressBase m_IRExpress = null;
        public IRDefineVarStatements( IRMethod _method ) 
        {
            this.irMethod = _method;
        }     
        public void ParseIRStatements(MetaDefineVarStatements ms)
        {
            MetaNewObjectExpressNode mnoen = ms.expressNode as MetaNewObjectExpressNode;
            IRMetaClass irmc = null;
            IRMetaType irmt = null;
            if (ms.expressNode != null)
            {
                m_IRExpress = IRExpressManager.CreateExpress(irMethod, ms.expressNode);
                m_IRStatements.Add(m_IRExpress);

                // If the expression's return type differs from the variable's
                // declared type (and both are numeric), emit a Convert instruction
                // before the store.  This handles cases like:
                //   Byte b8 = someInt32Var;   -> LoadLocal + Convert_I8 + StoreLocal
                // Const literals are already folded at parse time (see
                // MetaDefineVarStatements), so this path is mainly for non-const
                // right-hand expressions.
                var expType = ms.expressNode.GetReturnMetaType();
                var varType = ms.defineVarMetaVariable.GetFinalMetaType();
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
            // ── ARC v1: 认领非逃逸 new 局部 (ARC_MEMORY_DESIGN §5.3 / §16-C) ──
            // 求值结束后栈顶正是 NewObject 的结果(无论有无 ctor, new 表达式收尾时
            // 栈顶即新对象本体)。ArcRetain 栈协议: pop 值 -> refcount==1 时置 is_owned
            // (认领, 不加计数) -> 原样压回。
            // v1 保守规则(§16-C): 仅"无 ctor 或 ctor 方法体可证明为空"的 new 才认领
            // (无用户代码接触 this, 编译器可证明绝不逃逸); 有非空 ctor 的 new
            // (ctor 可能把 this 存入静态字段/全局而逃逸)一律交 GC。
            // 被闭包捕获的局部(其 StoreLocal 会被改写为共享数组槽, 引用逃逸到堆上
            // 的闭包上下文数组)不认领。
            if (mnoen != null
                && (mnoen.metaMemberFunction == null || IsEmptyCtorBody(mnoen.metaMemberFunction))
                && ms.defineVarMetaVariable.variableFrom == MetaVariable.EVariableFrom.LocalStatement
                && !IsClosureCapturedLocal(ms.defineVarMetaVariable))
            {
                IRData arcClaimData = new IRData();
                arcClaimData.opCode = EIROpCode.ArcRetain;
                arcClaimData.SetDebugInfoByToken(ms.defineVarMetaVariable.token,
                    "ARC claim owned new local: " + ms.defineVarMetaVariable.name);
                m_IRStatements.Add(new IRBase(arcClaimData));
                irMethod.RegisterArcOwnedLocal(ms.defineVarMetaVariable);
            }

            IRStoreVariable irStoreVar = IRStoreVariable.CreateIRStoreVariable(irmt, irmc, irMethod, ms.defineVarMetaVariable);
            //if(m_FileMetaOpAssignSyntax != null )
            //{
            //    irStoreVar.data.SetDebugInfoByToken(m_FileMetaOpAssignSyntax.assignToken);
            //}
            m_IRStatements.Add(irStoreVar);
        }

        /// <summary>
        /// ARC v1 判据: ctor 方法体是否可证明为空(无任何用户代码接触 this)。
        /// SL 的 new 只调用解析出的单个 ctor(无隐式基类 ctor 链)，空体 ctor
        /// 不可能让 this 逃逸，认领安全 (ARC_MEMORY_DESIGN §16-C)。
        /// - 跨模块方法: Front 侧无方法体，空体证据 = BuildMetaMemberFunctionFromIR
        ///   依据导出包 instructionList(为空或仅 Nop/Label)回填的
        ///   isEmptyBodyFromRefModule；非空体一律 false 交 GC。
        ///   (跨模块方法不回填 isConstructInitFunction，按名字识别 _init_)
        /// - 本地方法: metaBlockStatements 非空且语句链为空(形如 _init_() {});
        ///   metaBlockStatements == null(无体/未解析)保守不认领。
        /// </summary>
        private bool IsEmptyCtorBody(MetaMemberFunction mmf)
        {
            if (mmf.refFromType == RefFromType.RefModule)
            {
                return mmf.name == "_init_" && mmf.isEmptyBodyFromRefModule;
            }
            if (!mmf.isConstructInitFunction)
            {
                return false;
            }
            var mbs = mmf.metaBlockStatements;
            return mbs != null && mbs.nextMetaStatements == null;
        }

        /// <summary>
        /// ARC v1 判据: 局部是否被闭包共享捕获(引用会逃逸进堆上的上下文数组,
        /// StoreLocal/LoadLocal 均被改写为数组槽访问)。与 IRStoreVariable /
        /// IRLoadVariable 的拦截范围保持一致。
        /// </summary>
        private bool IsClosureCapturedLocal(MetaVariable mv)
        {
            if (irMethod == null)
            {
                return false;
            }
            var hostMmf = irMethod.bindMetaFunction as MetaMemberFunction;
            if (hostMmf == null || !hostMmf.hasClosureContext || mv is MetaClosureContextVariable)
            {
                return false;
            }
            return hostMmf.GetClosureCapture(mv) != null;
        }
        public string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(" #new var ");
            //sb.Append(m_MetaVariable);
            if (m_IRExpress != null)
            {
                sb.Append(" = " + m_IRExpress.ToIRString());
            }
            sb.AppendLine(" #");

            sb.AppendLine("{");
            for (int i = 0; i < m_IRStatements.Count; i++)
            {
                sb.AppendLine(m_IRStatements[i].ToIRString());
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
