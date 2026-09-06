//****************************************************************************
//  File:      MetaReturnStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Security.Cryptography;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed  class MetaReturnStatements : MetaStatements
    {
        public MetaExpressNodeBase express => m_Express;
        /// <summary>result 关键字: 该 ret 是否被改写为值返回 (ret expr 等价 result.value = expr; ret result)。</summary>
        public bool isResultValueReturn => m_IsResultValueReturn;
        /// <summary>result 关键字: 改写目标 result 变量 (函数返回类型为 Result/Result&lt;T&gt; 时注入的隐藏局部变量)。</summary>
        public MetaVariable resultMetaVariable => m_ResultMetaVariable;

        private FileMetaKeyReturnSyntax m_FileMetaReturnSyntax;
        private MetaType m_ReturnMetaDefineType;
        private MetaExpressNodeBase m_Express = null;
        private bool m_IsResultValueReturn = false;
        private MetaVariable m_ResultMetaVariable = null;
        public MetaReturnStatements( MetaBlockStatements mbs, FileMetaKeyReturnSyntax fmrs ) : base(mbs)
        {
            m_FileMetaReturnSyntax = fmrs;
            m_Token = fmrs.token;

            MetaType mdt = mbs.ownerMetaFunction.GetFinalMetaType();

            if(m_FileMetaReturnSyntax.returnExpress != null )
            {
                CreateExpressParam cep2 = new CreateExpressParam()
                {
                    ownerMetaBase = m_OwnerMetaBlockStatements.ownerMetaClass,
                    ownerMBS = m_OwnerMetaBlockStatements,
                    metaType = mdt,
                    fme = m_FileMetaReturnSyntax.returnExpress,
                    isStatic = false,
                    isConst = false,
                    parsefrom = EParseFrom.StatementRightExpress,
                    equalMetaVariable = mbs.ownerMetaFunction.returnMetaVariable
                };
                m_Express = ExpressManager.CreateExpressNode(cep2);
                if( m_Express != null )
                {
                    m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                    m_Express = ExpressManager.ConvertNewExpress(m_Express, null);
                    m_Express.CalcReturnType();
                    m_ReturnMetaDefineType = m_Express.GetReturnMetaType();
                }
            }
            else
            {
                m_ReturnMetaDefineType = new MetaType(CoreMetaClassManager.voidMetaClass);
            }

            // result 关键字: 返回类型为 Result/Result<T> 的函数, ret 语义改写判定
            // 裸 ret / ret 非 Result 类型值 => 改写为 result.value = expr; ret result
            var ownerMmf = mbs.ownerMetaFunction as MetaMemberFunction;
            if( ownerMmf != null && ownerMmf.hasResultVariable )
            {
                // 当前作用域可见的 result 必须是函数注入的那个 (用户自定义同名变量会遮蔽注入变量, 此时按普通返回处理)
                if( mbs.GetMetaVariableByName("result") == ownerMmf.resultVariable )
                {
                    m_ResultMetaVariable = ownerMmf.resultVariable;
                    if( m_Express == null || !CoreMetaClassManager.IsResultMetaType( m_ReturnMetaDefineType ) )
                    {
                        m_IsResultValueReturn = true;
                    }
                }
            }

            // 闭包函数返回类型推断: 首次遇到带返回值的 ret 语句时, 将返回类型从 Void 更新为实际类型
            var ownerFunc = mbs.ownerMetaFunction as MetaMemberFunction;
            if( ownerFunc != null && ownerFunc.isClosureFunction )
            {
                if( m_ReturnMetaDefineType != null
                    && m_ReturnMetaDefineType.metaClass != CoreMetaClassManager.voidMetaClass )
                {
                    var curType = ownerFunc.returnMetaVariable.defineMetaType;
                    if( curType != null && curType.metaClass == CoreMetaClassManager.voidMetaClass )
                    {
                        ownerFunc.returnMetaVariable.SetMetaDefineType( m_ReturnMetaDefineType );
                    }
                }
            }
            else
            {
                // result 值返回改写: ret expr 等价 result.value = expr (Object/T 字段语义), 跳过函数返回类型比对
                if( !m_IsResultValueReturn )
                {
                    if( !TypeManager.CompareLeftRightMetaType(mdt, m_ReturnMetaDefineType, m_Token, out MetaType convertMt  ) )
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "left compare right " + m_ReturnMetaDefineType?.ToString(), mdt?.ToString() ?? "null");
                    }
                }
            }
        }        
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
            sb.Append("return ");
            sb.Append(m_Express?.ToFormatString());
            sb.Append(";");
            return sb.ToString();
        }
    }
    public sealed class MetaTRStatements : MetaStatements
    {
        public MetaType m_ReturnMetaType;
        public MetaExpressNodeBase m_Express = null;

        private FileMetaKeyReturnSyntax m_FileMetaReturnSyntax;
        public MetaTRStatements(MetaBlockStatements mbs, FileMetaKeyReturnSyntax fmrs) : base(mbs)
        {
            m_FileMetaReturnSyntax = fmrs;
            m_Token = fmrs.token;

            MetaType mdt = null;
            if (m_FileMetaReturnSyntax?.returnExpress != null)
            {
                CreateExpressParam cep2 = new CreateExpressParam()
                {
                    ownerMBS = m_OwnerMetaBlockStatements,
                    metaType = mdt,
                    fme = m_FileMetaReturnSyntax.returnExpress,
                    isStatic = false,
                    isConst = false,
                    parsefrom = EParseFrom.StatementRightExpress
                };
                m_Express = ExpressManager.CreateExpressNode(cep2);
                m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                m_Express = ExpressManager.ConvertNewExpress(m_Express, null);
                m_Express.CalcReturnType();
            }
            if (m_Express != null)
            {
                m_ReturnMetaType = m_Express.GetReturnMetaType();
            }
            else
            {
                m_ReturnMetaType = new MetaType(CoreMetaClassManager.voidMetaClass);
            }
            if (!TypeManager.CompareLeftRightMetaType(m_ReturnMetaType, mdt, m_Token, out MetaType convertMt))
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "left compare right " + m_ReturnMetaType.ToString(), mdt.ToString());
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
            if( this.trMetaVariable != null )
            {
                sb.Append(this.trMetaVariable.name);
                sb.Append(" = ");
            }
            sb.Append(m_Express?.ToFormatString());
            sb.Append(";");
            return sb.ToString();
        }
    }
}
