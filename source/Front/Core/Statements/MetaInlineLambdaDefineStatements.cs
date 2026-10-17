//****************************************************************************
//  File:      MetaInlineLambdaDefineStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/9/16 12:00:00
//  Description:  内联 lambda 机制的 MetaCore 层实现 (M1 基础内联)
//      语法:  var name = ( a, b ) => a + b;
//      语义:  定义点仅记录参数名 + 表达式体原始节点 (不建函数/不建闭包对象);
//             调用点 name(实参) 由 MetaCallNode 就地展开为
//             形参名->实参表达式替换后的表达式 (零调用帧, IR 层零新 opcode)
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    /// <summary>
    /// 内联 lambda 变量: 只记录参数名列表与表达式体的原始 Node 列表。
    /// 类型为伪类型 InlineLambda (与 Function 不兼容, 拦截跨边界误用)。
    /// 每个调用点用 bodyNodeList 重建全新的 FileMetaBaseTerm 树后做形参替换。
    /// </summary>
    public class MetaInlineLambdaVariable : MetaVariable
    {
        public List<string> paramNameList => m_ParamNameList;
        public List<Node> bodyNodeList => m_BodyNodeList;
        /// <summary>M3: 参数可选类型标注的原始类定义 (File 层), null 项 = 裸名未标注 (与 paramNameList 平行)</summary>
        public List<FileMetaClassDefine> paramClassDefineList => m_ParamClassDefineList;
        /// <summary>M3: 解析后的参数类型, null 项 = 未标注或解析失败 (宽容: 体内使用处连锁报错)</summary>
        public List<MetaType> paramMetaTypeList => m_ParamMetaTypeList;
        public override string name => m_Name;

        private List<string> m_ParamNameList = new List<string>();
        private List<Node> m_BodyNodeList = new List<Node>();
        private List<FileMetaClassDefine> m_ParamClassDefineList = new List<FileMetaClassDefine>();
        private List<MetaType> m_ParamMetaTypeList = new List<MetaType>();

        /// <summary>
        /// 展开栈 (互递归拦截): TryParseInlineLambdaCall 展开期间 push,
        /// 展开体内嵌套调用其他内联 lambda 时会再次进入展开流程;
        /// 若待展开变量已在栈中 (f 体引用 g、g 体又引用 f) 即互递归 -> 无限展开, 编译错误。
        /// </summary>
        private static readonly List<MetaInlineLambdaVariable> s_ExpandingList = new List<MetaInlineLambdaVariable>();
        public static bool IsExpanding( MetaInlineLambdaVariable ilv )
        {
            return s_ExpandingList.Contains( ilv );
        }
        public static void PushExpanding( MetaInlineLambdaVariable ilv )
        {
            s_ExpandingList.Add( ilv );
        }
        public static void PopExpanding( MetaInlineLambdaVariable ilv )
        {
            s_ExpandingList.Remove( ilv );
        }

        /// <summary>
        /// 展开期「实参来源调用点」身份集合 (互递归拦截精化):
        /// inc(inc(41)) 外层展开的 Parse 会递归解析内层同类调用, 内层 term 来自外层实参 (原始对象),
        /// 此时外层 ilv 仍在栈上 —— 只看栈会误报互递归。体每次展开经 CreateFileMetaExpress 重建
        /// 全新 term (身份绝不在集合), 实参子树是调用点原始 term (会被收集), 以此区分两种栈命中:
        /// 栈命中 + 在集合 = 实参位置嵌套 (有限展开, 合法); 栈命中 + 不在集合 = 体位置互递归 (非法)。
        /// </summary>
        private static readonly HashSet<FileMetaCallNode> s_ArgSourceCallNodes = new HashSet<FileMetaCallNode>();
        public static bool IsAnyExpanding()
        {
            return s_ExpandingList.Count > 0;
        }
        public static void ClearArgSourceCallNodes()
        {
            s_ArgSourceCallNodes.Clear();
        }
        public static void AddArgSourceCallNode( FileMetaCallNode fcn )
        {
            if( fcn != null )
            {
                s_ArgSourceCallNodes.Add( fcn );
            }
        }
        public static bool IsArgSourceCallNode( FileMetaCallNode fcn )
        {
            return fcn != null && s_ArgSourceCallNodes.Contains( fcn );
        }

        public MetaInlineLambdaVariable( string name, MetaBlockStatements mbs, MetaBase ownerBase,
            List<string> paramNameList, List<Node> bodyNodeList,
            List<FileMetaClassDefine> paramClassDefineList, List<MetaType> paramMetaTypeList )
            : base( name, EVariableFrom.InlineLambda, mbs, ownerBase, new MetaType( CoreMetaClassManager.inlineLambdaMetaClass ) )
        {
            if ( paramNameList != null )
            {
                m_ParamNameList = paramNameList;
            }
            if ( bodyNodeList != null )
            {
                m_BodyNodeList = bodyNodeList;
            }
            if ( paramClassDefineList != null )
            {
                m_ParamClassDefineList = paramClassDefineList;
            }
            if ( paramMetaTypeList != null )
            {
                m_ParamMetaTypeList = paramMetaTypeList;
            }
        }

        /// <summary>解析内联 lambda 变量 (与闭包变量不同, 无捕获代理形态, 直接类型判断)</summary>
        public static MetaInlineLambdaVariable ResolveInlineLambdaVariable( MetaVariable mv )
        {
            return mv as MetaInlineLambdaVariable;
        }
    }

    /// <summary>
    /// 内联 lambda 定义语句: 出现在函数体内的 var name = (参数) => 表达式
    /// 构造时只注册变量 (防前向引用), 不解析体 —— 体在调用点展开时才重建。
    /// IR 层无对应语句输出 (IRBlockStatements 分派未知语句类型时自动跳过)。
    /// </summary>
    public class MetaInlineLambdaDefineStatements : MetaStatements
    {
        public MetaInlineLambdaVariable inlineLambdaMetaVariable => m_InlineLambdaMetaVariable;

        private FileMetaInlineLambdaSyntax m_FileMetaInlineLambdaSyntax = null;
        private MetaInlineLambdaVariable m_InlineLambdaMetaVariable = null;

        public MetaInlineLambdaDefineStatements( MetaBlockStatements mbs, FileMetaInlineLambdaSyntax fmils ) : base( mbs )
        {
            m_FileMetaInlineLambdaSyntax = fmils;
            m_Token = fmils.nameToken;
            m_Name = fmils.nameToken?.lexeme?.ToString();

            if ( string.IsNullOrEmpty( m_Name ) )
            {
                Log.AddMetaCoreLog( LID.MetaCoreInlineLambdaDefineStatementDefine, m_Token, "Error 内联lambda定义缺少名称!!" );
                return;
            }

            try
            {
                // 1. 收集形参名 + 类型标注 (M3: 可选类型标注, 未标注项类型槽为 null)
                var paramNameList = new List<string>();
                var paramClassDefineList = new List<FileMetaClassDefine>();
                var paramMetaTypeList = new List<MetaType>();
                var fmpList = fmils.paramList;
                if ( fmpList != null )
                {
                    // 宿主函数 (模板函数体内定义时供类型解析查找函数模板形参, 如 (T x) => ...)
                    var hostMmf = m_OwnerMetaBlockStatements.ownerMetaFunction as MetaMemberFunction;
                    for ( int i = 0; i < fmpList.Count; i++ )
                    {
                        paramNameList.Add( fmpList[i].name );
                        var fcd = fmpList[i].classDefineRef;
                        paramClassDefineList.Add( fcd );
                        if ( fcd != null )
                        {
                            // 与闭包参数同一解析路径 (MetaDefineParam.ParseMetaDefineType);
                            // 解析失败时内部已记 MetaCoreTypeNotFound3, 此处宽容置 null,
                            // 后续体内使用处会连锁报错, 调用点校验对 null 类型槽跳过
                            paramMetaTypeList.Add( TypeManager.instance.GetMetaTypeByTemplateFunction(
                                m_OwnerMetaBlockStatements.ownerMetaClass, hostMmf, fcd ) );
                        }
                        else
                        {
                            paramMetaTypeList.Add( null );
                        }
                    }
                }

                // 2. M2 限制拦截: 扫描体违禁构造 (§4.1 禁止总表), 命中即报错并放弃注册
                //    (不注册 -> 后续调用点按未定义变量连锁报错, 符合"上游未过不改下游")
                var forbiddenKind = ScanInlineLambdaBodyForbidden( fmils.bodyNodeList, m_Name, paramNameList, out string hitDesc );
                if ( forbiddenKind != EInlineLambdaForbiddenKind.None )
                {
                    LogInlineLambdaBodyForbidden( forbiddenKind, m_Token, m_Name, hitDesc );
                    return;
                }

                // 3. 注册变量名(防前向引用) 并创建内联 lambda 变量 (体保持原始 Node 列表)
                m_OwnerMetaBlockStatements.AddOnlyNameMetaVariable( m_Name );
                m_InlineLambdaMetaVariable = new MetaInlineLambdaVariable( m_Name, m_OwnerMetaBlockStatements,
                    m_OwnerMetaBlockStatements.ownerMetaClass, paramNameList, fmils.bodyNodeList,
                    paramClassDefineList, paramMetaTypeList );
                m_InlineLambdaMetaVariable.AddPingToken( m_Token );
                m_OwnerMetaBlockStatements.UpdateMetaVariableDict( m_InlineLambdaMetaVariable );

                SetTRMetaVariable( m_InlineLambdaMetaVariable );
            }
            catch ( System.Exception ex )
            {
                Log.AddMetaCoreLog( LID.MetaCoreInlineLambdaDefineStatementIssue, m_Token, "Error 内联lambda构造异常: " + ex.Message + " / " + ex.StackTrace );
            }
        }

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for ( int i = 0; i < realDeep; i++ )
                sb.Append( Global.tabChar );
            sb.Append( "var " + m_Name + " = ( " );
            if ( m_InlineLambdaMetaVariable != null )
            {
                for ( int i = 0; i < m_InlineLambdaMetaVariable.paramNameList.Count; i++ )
                {
                    sb.Append( m_InlineLambdaMetaVariable.paramNameList[i] );
                    if ( i < m_InlineLambdaMetaVariable.paramNameList.Count - 1 )
                        sb.Append( ", " );
                }
            }
            sb.Append( " ) => // inline-lambda" );
            if ( nextMetaStatements != null )
            {
                sb.Append( "\n" );
                sb.Append( nextMetaStatements.ToFormatString() );
            }
            return sb.ToString();
        }

        // ==================== M2 限制拦截: 体扫描 (§4.1 禁止总表 + §5.7 扫描要点) ====================
        // 双形态识别 (脱糖顺序: CrateFileMetaSyntaxNoKey 入口的 TransformCoroutineKeywordNodes /
        //   TransformIsolateCallNodes 先于内联 lambda 识别执行):
        //   (1) 原始关键字 Key 节点 —— spawn/await/yield 嵌套在括号/实参内未被脱糖时保持原样;
        //       try/catch/finally/break/continue/goto/return/out 不参与脱糖, 恒为原始 token
        //   (2) 脱糖产物 —— Coroutine.spawnClosureN / Coroutine.awaitTask / Coroutine.yieldNow 合成调用,
        //       SystemIsolateRun / SystemIsolateSpawn 系统调用, Isolate.run/spawn/spawnInstance 原链
        // 嵌套闭包 (Key(Function) 子树) 整体跳过: 闭包自有帧, 其内部 return/spawn 属于闭包语义

        /// <summary>违禁构造类别 (对应 LID 21449-21452)</summary>
        private enum EInlineLambdaForbiddenKind
        {
            None = 0,
            Coroutine,          // spawn / isolate / await / yield (帧依赖)
            ControlFlow,        // break / continue / goto / return / out (控制流逃逸 / 参数)
            ExceptionFrame,     // try / catch / finally (异常帧)
            SelfReference,      // 自引用 (内联无限展开)
        }

        /// <summary>扫描内联体 Node 列表, 命中违禁构造时返回类别并输出命中描述</summary>
        private static EInlineLambdaForbiddenKind ScanInlineLambdaBodyForbidden( List<Node> nodeList,
            string selfName, List<string> paramNameList, out string hitDesc )
        {
            hitDesc = null;
            if ( nodeList == null )
                return EInlineLambdaForbiddenKind.None;
            foreach ( var node in nodeList )
            {
                var kind = ScanInlineLambdaNodeForbidden( node, selfName, paramNameList, ref hitDesc );
                if ( kind != EInlineLambdaForbiddenKind.None )
                    return kind;
            }
            return EInlineLambdaForbiddenKind.None;
        }

        /// <summary>递归扫描单个 Node 子树 (返回首个命中的违禁类别)</summary>
        private static EInlineLambdaForbiddenKind ScanInlineLambdaNodeForbidden( Node node,
            string selfName, List<string> paramNameList, ref string hitDesc )
        {
            if ( node == null )
                return EInlineLambdaForbiddenKind.None;

            // 嵌套闭包整体跳过 (含其参数 Par 与体 Brace: 闭包自有帧, 内部构造不属于内联体)
            if ( node.nodeType == ENodeType.Key && node.token?.type == ETokenType.Function )
                return EInlineLambdaForbiddenKind.None;

            // (1) 原始关键字: Key 节点按 token 类型分派
            if ( node.nodeType == ENodeType.Key )
            {
                var ttype = node.token?.type;
                if ( ttype == ETokenType.Spawn || ttype == ETokenType.Await || ttype == ETokenType.Yield )
                {
                    hitDesc = "关键字 " + node.token.lexeme?.ToString();
                    return EInlineLambdaForbiddenKind.Coroutine;
                }
                if ( ttype == ETokenType.Break || ttype == ETokenType.Continue || ttype == ETokenType.Goto
                    || ttype == ETokenType.Return || ttype == ETokenType.Out )
                {
                    hitDesc = "关键字 " + node.token.lexeme?.ToString();
                    return EInlineLambdaForbiddenKind.ControlFlow;
                }
                if ( ttype == ETokenType.Try || ttype == ETokenType.Catch || ttype == ETokenType.Finally )
                {
                    hitDesc = "关键字 " + node.token.lexeme?.ToString();
                    return EInlineLambdaForbiddenKind.ExceptionFrame;
                }
            }

            // try?/try! 是表达式前缀运算符 (TokenParseToNode 走 AddSymbol, 同 !/~),
            // 节点类型为 Symbol 而非 Key, 不走上面的 Key 分支
            if ( node.nodeType == ENodeType.Symbol
                && ( node.token?.type == ETokenType.TryQuestion || node.token?.type == ETokenType.TryExclamation ) )
            {
                hitDesc = "表达式前缀 " + node.token.lexeme?.ToString();
                return EInlineLambdaForbiddenKind.ExceptionFrame;
            }

            // (2) 标识符链头检查: Isolate 原始形态 / Coroutine 与 SystemIsolate 脱糖产物 / 自引用
            if ( node.nodeType == ENodeType.IdentifierLink )
            {
                var head = node.token?.lexeme?.ToString();
                var methodName = GetLinkExtendMethodName( node );
                if ( head == "Isolate"
                    && ( methodName == "run" || methodName == "spawn" || methodName == "spawnInstance" ) )
                {
                    hitDesc = "Isolate." + methodName;
                    return EInlineLambdaForbiddenKind.Coroutine;
                }
                if ( head == "Coroutine"
                    && ( IsCoroutineDesugarMethod( methodName ) ) )
                {
                    hitDesc = "Coroutine." + methodName + " (spawn/await/yield 脱糖产物)";
                    return EInlineLambdaForbiddenKind.Coroutine;
                }
                if ( head == "SystemIsolateRun" || head == "SystemIsolateSpawn" )
                {
                    hitDesc = head + " (Isolate 脱糖产物)";
                    return EInlineLambdaForbiddenKind.Coroutine;
                }
                // 自引用: 链头裸名即内联名 (形参名可合法遮蔽)
                if ( !string.IsNullOrEmpty( selfName ) && head == selfName
                    && ( paramNameList == null || !paramNameList.Contains( selfName ) ) )
                {
                    hitDesc = "自引用 '" + selfName + "'";
                    return EInlineLambdaForbiddenKind.SelfReference;
                }
            }

            // 递归子结构 (blockNode 刻意不递归: 闭包体已由上面 Key(Function) 拦截, 其余 Brace 为体形态)
            foreach ( var child in node.childList )
            {
                var kind = ScanInlineLambdaNodeForbidden( child, selfName, paramNameList, ref hitDesc );
                if ( kind != EInlineLambdaForbiddenKind.None )
                    return kind;
            }
            foreach ( var link in node.extendLinkNodeList )
            {
                var kind = ScanInlineLambdaNodeForbidden( link, selfName, paramNameList, ref hitDesc );
                if ( kind != EInlineLambdaForbiddenKind.None )
                    return kind;
            }
            foreach ( var bracket in node.bracketNodeList )
            {
                var kind = ScanInlineLambdaNodeForbidden( bracket, selfName, paramNameList, ref hitDesc );
                if ( kind != EInlineLambdaForbiddenKind.None )
                    return kind;
            }
            var subKind = ScanInlineLambdaNodeForbidden( node.parNode, selfName, paramNameList, ref hitDesc );
            if ( subKind != EInlineLambdaForbiddenKind.None )
                return subKind;
            subKind = ScanInlineLambdaNodeForbidden( node.angleNode, selfName, paramNameList, ref hitDesc );
            if ( subKind != EInlineLambdaForbiddenKind.None )
                return subKind;
            return EInlineLambdaForbiddenKind.None;
        }

        /// <summary>取标识符链头的一级扩展方法名 (extendLinkNodeList 形如 [Period, IdentifierLink(方法名)])</summary>
        private static string GetLinkExtendMethodName( Node linkHead )
        {
            if ( linkHead == null )
                return null;
            var linkList = linkHead.extendLinkNodeList;
            if ( linkList == null )
                return null;
            for ( int i = 0; i < linkList.Count; i++ )
            {
                var n = linkList[i];
                if ( n != null && n.nodeType == ENodeType.IdentifierLink )
                    return n.token?.lexeme?.ToString();
            }
            return null;
        }

        /// <summary>Coroutine 脱糖产物方法名: spawnClosureN / awaitTask / yieldNow (StructParseToSyntax 合成)</summary>
        internal static bool IsCoroutineDesugarMethod( string methodName )
        {
            if ( string.IsNullOrEmpty( methodName ) )
                return false;
            return methodName.StartsWith( "spawnClosure" ) || methodName == "awaitTask" || methodName == "yieldNow";
        }

        /// <summary>按违禁类别分派错误日志 (LID 21449-21452, 文案模板见设计文档 §4.1 末)</summary>
        private static void LogInlineLambdaBodyForbidden( EInlineLambdaForbiddenKind kind, Token token, string name, string hitDesc )
        {
            switch ( kind )
            {
                case EInlineLambdaForbiddenKind.Coroutine:
                    Log.AddMetaCoreLog( LID.MetaCoreInlineLambdaBodyForbiddenCoroutine, token,
                        "Error 内联lambda '" + name + "' 体不允许 spawn/isolate/await/yield (帧依赖), 命中: "
                        + hitDesc + " : 请改用 function 闭包" );
                    break;
                case EInlineLambdaForbiddenKind.ControlFlow:
                    Log.AddMetaCoreLog( LID.MetaCoreInlineLambdaBodyForbiddenControlFlow, token,
                        "Error 内联lambda '" + name + "' 体不允许控制流逃逸 (break/continue/goto/return/out), 命中: "
                        + hitDesc + " : 内联后无方法边界" );
                    break;
                case EInlineLambdaForbiddenKind.ExceptionFrame:
                    Log.AddMetaCoreLog( LID.MetaCoreInlineLambdaBodyForbiddenExceptionFrame, token,
                        "Error 内联lambda '" + name + "' 体不允许异常帧构造 (try/try?/try!/catch/finally), 命中: "
                        + hitDesc + " : 请改用 function 闭包" );
                    break;
                case EInlineLambdaForbiddenKind.SelfReference:
                    Log.AddMetaCoreLog( LID.MetaCoreInlineLambdaBodyForbiddenSelfReference, token,
                        "Error 内联lambda '" + name + "' 体不允许自引用或递归 (无限展开), 命中: "
                        + hitDesc + " : 请改用 function 闭包" );
                    break;
            }
        }
    }
}
