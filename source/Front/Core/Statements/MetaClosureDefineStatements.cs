//****************************************************************************
//  File:      MetaClosureDefineStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/8/25 12:00:00
//  Description:  闭包机制的 MetaCore 层实现 (Dart-like 框架版)
//      语法:  function name( int a, int b ) { ret a + b; }
//             var name = ( int a, int b ) { ret a + b; }
//      降级策略:
//          闭包体   -> 合成的 static MetaMemberFunction (注册到 MethodManager 动态函数列表)
//          捕获变量 -> context 数组(闭包函数隐藏的第一个参数 Argument 0)
//          调用     -> IR: NewClosure / CallClosure
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    /// <summary>
    /// 闭包变量: 保存一个闭包函数对象的引用 (函数入口 + context)
    /// 定义在宿主函数体内, 作为宿主函数的局部变量存在。
    /// </summary>
    public class MetaClosureVariable : MetaVariable
    {
        public MetaClosureDefineStatements closureDefineStatements => m_ClosureDefineStatements;

        private MetaClosureDefineStatements m_ClosureDefineStatements = null;

        public MetaClosureVariable( string name, MetaBlockStatements mbs, MetaBase ownerBase,
            MetaClosureDefineStatements cds )
            : base( name, EVariableFrom.ClosureVariable, mbs, ownerBase, new MetaType( CoreMetaClassManager.functionMetaClass ) )
        {
            m_ClosureDefineStatements = cds;
            SetIsDefineMetaType( true );
            SetRealMetaType( new MetaType( CoreMetaClassManager.functionMetaClass ) );
        }

        /// <summary>
        /// 解析闭包变量: mv 本身是闭包变量, 或是闭包变量的捕获代理(在另一个闭包体内被捕获)。
        /// </summary>
        public static MetaClosureVariable ResolveClosureVariable( MetaVariable mv )
        {
            if ( mv is MetaClosureVariable cv )
                return cv;
            if ( mv is MetaClosureContextVariable ccv )
                return ccv.hostMetaVariable as MetaClosureVariable;
            return null;
        }
    }

    /// <summary>
    /// 闭包捕获变量的代理变量: 闭包体内对宿主变量的读写都通过它进行。
    /// 实际运行时存放在 context 数组的 slotIndex 槽位中。
    /// IR 生成: 读 = LoadArgument 0 + LoadArrayIndex slot; 写 = LoadArgument 0 + 值 + StoreArrayIndex slot
    /// </summary>
    public class MetaClosureContextVariable : MetaVariable
    {
        public int slotIndex => m_SlotIndex;
        public MetaVariable hostMetaVariable => m_HostMetaVariable;

        private int m_SlotIndex = -1;
        private MetaVariable m_HostMetaVariable = null;

        public MetaClosureContextVariable( MetaVariable hostMv, int slotIndex )
            : base( hostMv.name, EVariableFrom.ClosureContext, null, hostMv.ownerMetaBase, hostMv.GetFinalMetaType() )
        {
            m_HostMetaVariable = hostMv;
            m_SlotIndex = slotIndex;
            if ( hostMv.isDefineMetaType )
            {
                SetMetaDefineType( hostMv.defineMetaType );
            }
            SetIsDefineMetaType( hostMv.isDefineMetaType );
            if ( hostMv.realMetaType != null )
            {
                SetRealMetaType( hostMv.realMetaType );
            }
            SetIsConst( hostMv.isConst );
        }
    }

    /// <summary>
    /// 闭包体语句块: 重写变量解析顺序 (Dart 词法作用域语义)
    ///      1. 闭包体局部变量
    ///      2. 已捕获变量 (context 代理)
    ///      3. 闭包函数自身参数
    ///      4. 宿主函数作用域变量 (捕获 -> 加入 context)
    /// </summary>
    public class MetaClosureBlockStatements : MetaBlockStatements
    {
        public MetaClosureDefineStatements ownerClosureDefineStatements => m_OwnerClosureDefineStatements;

        private MetaClosureDefineStatements m_OwnerClosureDefineStatements = null;
        private MetaBlockStatements m_HostMetaBlockStatements = null;
        // 已捕获变量表: name -> 代理变量 (与 m_MetaVariableDict 分离, 避免被当作闭包函数局部变量收集)
        private Dictionary<string, MetaClosureContextVariable> m_CaptureDict = new Dictionary<string, MetaClosureContextVariable>();

        public MetaClosureBlockStatements( MetaClosureDefineStatements cds, MetaFunction closureFunction,
            FileMetaBlockSyntax fmbs, MetaBlockStatements hostBlock )
            : base( closureFunction.metaBlockStatements, fmbs )
        {
            m_OwnerClosureDefineStatements = cds;
            m_HostMetaBlockStatements = hostBlock;
        }

        public override MetaVariable GetMetaVariableByName( string name, bool isFromParent = true )
        {
            // 1. 闭包体局部变量
            if ( m_MetaVariableDict.ContainsKey( name ) )
                return m_MetaVariableDict[name];

            // 2. 已捕获变量
            if ( m_CaptureDict.TryGetValue( name, out var captured ) )
                return captured;

            // 3. 闭包函数自身参数 (在闭包函数顶块中, 不再向其父级查找)
            var closureFunctionBlock = m_OwnerClosureDefineStatements?.closureFunction?.metaBlockStatements;
            if ( closureFunctionBlock != null )
            {
                var paramMv = closureFunctionBlock.GetMetaVariableByName( name, false );
                if ( paramMv != null )
                    return paramMv;
            }

            // 4. 宿主函数作用域捕获
            return CaptureFromHost( name );
        }

        /// <summary>
        /// 沿宿主块链向上查找并捕获变量。框架版不支持闭包嵌套(遇到 MetaClosureBlockStatements 报错)。
        /// </summary>
        private MetaVariable CaptureFromHost( string name )
        {
            var host = m_HostMetaBlockStatements;
            while ( host != null )
            {
                if ( host is MetaClosureBlockStatements )
                {
                    Log.AddMetaCoreLog( LID.ShowExtendMessage, "Error 闭包嵌套定义暂不支持!! (变量:" + name + ")" );
                    return null;
                }
                var mv = host.GetMetaVariableByName( name, false );
                if ( mv != null )
                {
                    return CreateCaptureProxy( mv );
                }
                host = host.parentBlockStatements;
            }
            // 未在宿主作用域找到 -> 返回 null, 让 MetaCallNode.GetFirstNode 走类成员路径处理
            // (静态成员生成 LoadStaticField/StoreStaticField, 裸实例成员本语言要求 this. 前缀)
            // 注意: 不能在此直接返回类成员变量, 否则 GetFirstNode 会把它当作
            // FunctionInnerVariableName 处理, 生成错误的 IR 指令
            return null;
        }

        private MetaVariable CreateCaptureProxy( MetaVariable hostMv )
        {
            // 查宿主函数注册表: 同一变量在多个闭包中捕获时复用同一代理/槽位 (共享语义的关键)
            // 注册表的槽位同时是宿主 prologue 分配的共享数组下标
            var ownerCds = m_OwnerClosureDefineStatements;
            var hostFunc = ownerCds?.hostMemberFunction;
            var proxy = hostFunc?.GetOrAddClosureCapture( hostMv );
            if( proxy == null )
            {
                // 兜底: 无宿主函数注册表时退化为闭包私有槽位
                int slotIndex = ownerCds != null ? ownerCds.captureList.Count : m_CaptureDict.Count;
                proxy = new MetaClosureContextVariable( hostMv, slotIndex );
            }
            m_CaptureDict.Add( hostMv.name, proxy );
            ownerCds?.AddCaptureInfo( proxy );
            return proxy;
        }
    }

    /// <summary>
    /// 闭包定义语句: 出现在宿主函数体内的 function name(){...} / var name = (...){...}
    /// 构造时完成: 合成闭包函数 -> 解析闭包体(捕获变量) -> 注册动态函数
    /// </summary>
    public class MetaClosureDefineStatements : MetaStatements
    {
        public MetaClosureVariable closureMetaVariable => m_ClosureMetaVariable;
        public MetaMemberFunction closureFunction => m_ClosureFunction;
        public MetaClosureBlockStatements closureBlockStatements => m_ClosureBlockStatements;
        public List<MetaClosureContextVariable> captureList => m_CaptureList;
        public bool isAnonymous => m_IsAnonymous;
        /// <summary>定义该闭包的宿主函数 (捕获注册表持有者)</summary>
        public MetaMemberFunction hostMemberFunction => m_HostMetaMemberFunction;

        private static int s_ClosureCount = 0;

        private FileMetaDefineClosureSyntax m_FileMetaDefineClosureSyntax = null;
        private MetaClosureVariable m_ClosureMetaVariable = null;
        private MetaMemberFunction m_ClosureFunction = null;
        private MetaClosureBlockStatements m_ClosureBlockStatements = null;
        private List<MetaClosureContextVariable> m_CaptureList = new List<MetaClosureContextVariable>();
        private bool m_IsAnonymous = false;
        private MetaMemberFunction m_HostMetaMemberFunction = null;

        public MetaClosureDefineStatements( MetaBlockStatements mbs, FileMetaDefineClosureSyntax fmdcs ) : base( mbs )
        {
            m_FileMetaDefineClosureSyntax = fmdcs;
            m_Token = fmdcs.nameToken;
            m_Name = fmdcs.nameToken?.lexeme?.ToString();
            m_IsAnonymous = fmdcs.isAnonymous;

            if ( string.IsNullOrEmpty( m_Name ) )
            {
                Log.AddMetaCoreLog( LID.ShowExtendMessage, m_Token, "Error 闭包定义缺少名称!!" );
                return;
            }

            var ownerClass = m_OwnerMetaBlockStatements.ownerMetaClass;
            if ( ownerClass == null )
            {
                Log.AddMetaCoreLog( LID.ShowExtendMessage, m_Token, "Error 闭包只能在类的方法体内定义!!" );
                return;
            }

            try
            {
            // 1. 注册闭包变量名(防前向引用) 并创建闭包变量
            m_OwnerMetaBlockStatements.AddOnlyNameMetaVariable( m_Name );
            m_ClosureMetaVariable = new MetaClosureVariable( m_Name, m_OwnerMetaBlockStatements, ownerClass, this );
            m_ClosureMetaVariable.AddPingToken( m_Token );
            m_OwnerMetaBlockStatements.UpdateMetaVariableDict( m_ClosureMetaVariable );

            // 2. 创建合成的静态闭包函数 (挂在宿主类上)
            s_ClosureCount++;
            string funcName = "__closure_" + m_Name + "_" + s_ClosureCount.ToString();
            m_ClosureFunction = new MetaMemberFunction( ownerClass, funcName, true );

            // 3. 隐藏的 context 参数 (Argument 0): 存放捕获变量的数组
            var contextParam = new MetaDefineParam( "__closure_context__", m_ClosureFunction );
            contextParam.metaVariable.SetMetaDefineType( new MetaType( CoreMetaClassManager.objectMetaClass ) );
            contextParam.metaVariable.SetIsDefineMetaType( true );
            m_ClosureFunction.AddMetaDefineParam( contextParam );

            // 4. 用户声明的闭包参数
            var fmpList = fmdcs.paramList;
            if ( fmpList != null )
            {
                for ( int i = 0; i < fmpList.Count; i++ )
                {
                    var mdp = new MetaDefineParam( m_ClosureFunction, fmpList[i] );
                    mdp.ParseMetaDefineType();
                    m_ClosureFunction.AddMetaDefineParam( mdp );
                }
            }

            // 5. 闭包函数返回类型默认为 Void, 解析闭包体后根据 ret 语句推断实际返回类型
            var retMt = new MetaType( CoreMetaClassManager.voidMetaClass );
            m_ClosureFunction.returnMetaVariable.SetMetaDefineType( retMt );
            m_ClosureFunction.returnMetaVariable.SetIsDefineMetaType( true );
            m_ClosureFunction.returnMetaVariable.SetRealMetaType( new MetaType( CoreMetaClassManager.voidMetaClass ) );

            // 6. 参数加入闭包函数顶块 (供闭包体解析第3级查找)
            m_ClosureFunction.metaBlockStatements.SetMetaMemberParamCollection( m_ClosureFunction.metaMemberParamCollection );

            // 6.2 解析宿主函数 (捕获注册表持有者); 闭包必须在 MetaMemberFunction 体内定义
            m_HostMetaMemberFunction = m_OwnerMetaBlockStatements.ownerMetaFunction as MetaMemberFunction;
            if( m_HostMetaMemberFunction == null )
            {
                Log.AddMetaCoreLog( LID.ShowExtendMessage, m_Token, "Error 闭包只能在类的方法体内定义!! (宿主函数缺失)" );
                return;
            }

            // 6.5 如果宿主方法是实例方法, 捕获 this 到 context 数组
            // (查宿主函数注册表: 同一宿主的多个闭包复用同一 this 槽位, 实现共享)
            if( !m_HostMetaMemberFunction.isStatic && m_HostMetaMemberFunction.thisMetaVariable != null )
            {
                var thisProxy = m_HostMetaMemberFunction.GetOrAddClosureCapture( m_HostMetaMemberFunction.thisMetaVariable );
                if( thisProxy != null )
                {
                    m_CaptureList.Add( thisProxy );
                    m_ClosureFunction.SetCapturedThis( thisProxy );
                }
            }

            // 7. 创建闭包体块: 父块 = 闭包函数顶块, 宿主块 = 定义闭包时所在的块
            m_ClosureBlockStatements = new MetaClosureBlockStatements( this, m_ClosureFunction,
                fmdcs.blockSyntax, m_OwnerMetaBlockStatements );
            m_ClosureFunction.metaBlockStatements.SetNextStatements( m_ClosureBlockStatements );

            // 8. 解析闭包体 (此过程中触发宿主变量捕获, ret 语句会回填返回类型)
            MetaMemberFunction.CreateMetaSyntax( fmdcs.blockSyntax, m_ClosureBlockStatements );

            // 8.5 闭包返回类型推断: 如果 ret 语句已更新返回类型, 同步到 RealMetaType
            var inferredType = m_ClosureFunction.returnMetaVariable.defineMetaType;
            if ( inferredType != null && inferredType.metaClass != CoreMetaClassManager.voidMetaClass )
            {
                m_ClosureFunction.returnMetaVariable.SetRealMetaType( inferredType );
            }

            // 9. 注册到 MethodManager 动态函数列表 (IR 翻译阶段会合并输出)
            MethodManager.instance.AddDynamicMemeberFunction( m_ClosureFunction );

            // 10. 确保宿主函数持有共享捕获数组 __closure_ctx__ (即使 0 捕获也创建, 统一 NewClosure 协议)
            //     Meta 阶段先于 IR 阶段完成, IR 生成开始时全部捕获集合已知,
            //     宿主 IRMethod prologue 按注册表总数分配数组并统一路由读写
            m_HostMetaMemberFunction.EnsureClosureContext();

            SetTRMetaVariable( m_ClosureMetaVariable );
            }
            catch( System.Exception ex )
            {
                Log.AddMetaCoreLog( LID.ShowExtendMessage, m_Token, "Error 闭包构造异常: " + ex.Message + " / " + ex.StackTrace );
            }
        }

        /// <summary>闭包体解析过程中, 由 MetaClosureBlockStatements 回调登记捕获的变量</summary>
        public void AddCaptureInfo( MetaClosureContextVariable capture )
        {
            if ( capture != null )
            {
                m_CaptureList.Add( capture );
            }
        }

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for ( int i = 0; i < realDeep; i++ )
                sb.Append( Global.tabChar );
            if ( m_IsAnonymous )
            {
                sb.Append( "var " + m_Name + " = ( " );
            }
            else
            {
                sb.Append( "function " + m_Name + "( " );
            }
            sb.Append( " ) " );
            sb.Append( "// capture:" + m_CaptureList.Count );
            if ( nextMetaStatements != null )
            {
                sb.Append( "\n" );
                sb.Append( nextMetaStatements.ToFormatString() );
            }
            return sb.ToString();
        }
    }
}
