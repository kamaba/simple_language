//****************************************************************************
//  File:      DllImportFunctionDispatch.cs
// ------------------------------------------------
//  DateTime: 2026/9/29
//  Description: @DllImport 函数式声明的 MetaCore 层分派注入
//  （统一 @ 处理第一步: 声明位置的 @ 走 attribute 管线,
//   在 MetaCore 层消费, 替代源码文本预改写器 DllImportSourceRewriter）
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using SimpleLanguage.Project;

using System;
using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    /// <summary>
    /// @DllImport 函数式声明的 MetaCore 层分派注入:
    ///
    ///     @DllImport( "lib", "sym"[, "sig"] )
    ///     static Ret name( T1 a1, T2 a2, ... ) { ...fallback 本体... }
    ///
    /// 注入产物（等价形态, 成员变量式见 MetaMemberVariable.TryCreateDllImportExpress）:
    ///     static Func&lt;Ret,T1,...&gt; __dll_name = FFI.StaticLibrary.bindFunction( "lib", "sym", "sig" )   // 隐藏字段
    ///     static Ret name( T1 a1, ... )
    ///     {
    ///         if ( __dll_name != null ) { ret __dll_name( a1, ... ) }     // 链头分派
    ///         ...fallback 本体（原体原位保留, 绑定落空时执行）...
    ///     }
    ///
    /// 与旧 DllImportSourceRewriter 的差异: 原体不再包进 else 分支而是原位保留在
    /// 链头 if 之后, 语义等价; void 函数 then 块在裸调用后必须补裸 ret;（防 then
    /// 落空后继续穿透执行原体）, 非 void 用 ret expr 天然终止。
    /// 注入时机在 ParseStatements（CreateMetaSyntax 之后）: 静态初始化 IR 由 IR 相位
    /// IRMetaClass.CreateStaticMetaMetaVariableIRList 统一收集, 时序安全。
    /// </summary>
    public static class DllImportFunctionDispatch
    {
        /// <summary>
        /// 挂有 @DllImport attribute 的 static 函数注入隐藏 Func 字段 + 链头 if 分派。
        /// 非 DllImport 函数 / 校验失败时静默返回（错误已在内部记录日志）。
        /// </summary>
        public static void TryInject( MetaMemberFunction mmf )
        {
            if( mmf == null )
                return;

            // ── 1. 查找 DllImport attribute ──
            MetaAttribute dllAttr = null;
            var attrList = mmf.attributeList;
            for( int i = 0; i < attrList.Count; i++ )
            {
                if( attrList[i].name == "DllImport" )
                {
                    dllAttr = attrList[i];
                    break;
                }
            }
            if( dllAttr == null )
                return;

            // ── 2. 校验: 必须 static + 必须带函数体（fallback 本体） ──
            if( !mmf.isStatic )
            {
                Log.AddMetaCoreLog( LID.MetaCoreMemberFunctionDllImportStatic, mmf.token,
                    $"DllImport: '{mmf.ownerMetaClass?.allName}.{mmf.name}' 必须为 static 函数!!" );
                return;
            }
            var fmfm = mmf.fileMetaMemberFunction;
            if( fmfm?.fileMetaBlockSyntax == null )
            {
                Log.AddMetaCoreLog( LID.MetaCoreMemberFunctionDllImportBody, mmf.token,
                    $"DllImport: 函数 '{mmf.ownerMetaClass?.allName}.{mmf.name}' 必须带函数体（fallback 本体）!!" );
                return;
            }

            // ── 3. 实参: ( 库路径, 符号名[, sig] ) ──
            var args = dllAttr.GetSplitStringArgs();
            if( args.Count < 2 )
            {
                Log.AddMetaCoreLog( LID.MetaCoreMemberFunctionDllImportArgs, mmf.token,
                    "DllImport: 需要 (库路径, 符号名) 两个字符串实参!!" );
                return;
            }
            string libPath = args[0];
            string symbol = args[1];

            // 首实参可为 project.jsonc dllImports 段配置的别名, 命中时替换为完整路径
            var resolvedPath = ProjectManager.config?.ResolveDllImportPath( libPath );
            if( !string.IsNullOrEmpty( resolvedPath ) && resolvedPath != libPath )
            {
                libPath = resolvedPath;
            }

            string sig = args.Count >= 3 ? args[2] : null;
            if( string.IsNullOrEmpty( sig ) )
            {
                sig = MetaDefineVarStatements.BuildFFIFunctionSigFromMetaFunction( mmf );
                if( string.IsNullOrEmpty( sig ) )
                {
                    Log.AddMetaCoreLog( LID.MetaCoreMemberFunctionDllImportSig, mmf.token,
                        $"DllImport: '{mmf.name}' 的签名无法映射为 FFI sig（含 Ptr 等类型时可用第 3 个实参手写 sig）!!" );
                    return;
                }
            }

            // ── 4. 注入隐藏字段 + 链头分派 ──
            var mc = mmf.ownerMetaClass;
            if( mc == null )
                return;
            string hiddenName = "__dll_" + mmf.name;

            if( !InjectHiddenFuncField( mmf, mc, hiddenName, libPath, symbol, sig ) )
                return;

            InjectDispatchIf( mmf, fmfm, hiddenName );

            Log.AddMetaCoreLog( LID.MetaCoreMemberFunctionDllImportInject, mmf.token,
                $"DllImport: inject '{mc.allName}.{mmf.name}' -> 隐藏字段 {hiddenName} + if 分派转发 (\"{libPath}\", \"{symbol}\", \"{sig}\")" );
        }

        /// <summary>
        /// 注入隐藏 static Func 字段: __dll_name = FFI.StaticLibrary.bindFunction( lib, sym, sig )。
        /// 成员构造三段式同 PorjectClass.InjectDllImportFunctionMembers 先例。
        /// </summary>
        private static bool InjectHiddenFuncField( MetaMemberFunction mmf, MetaClass mc, string hiddenName,
            string libPath, string symbol, string sig )
        {
            // 同名隐藏字段已存在（多函数/重复注入）-> 视为成功, 只做分派注入
            if( mc.GetMetaMemberVariableByName( hiddenName ) != null )
                return true;

            var retType = mmf.GetFinalMetaType();
            if( retType == null )
                return false;
            var paramTypes = new List<MetaType>();
            var plist = mmf.metaMemberParamCollection?.metaDefineParamList;
            if( plist != null )
            {
                for( int i = 0; i < plist.Count; i++ )
                {
                    var pt = plist[i]?.metaVariable?.defineMetaType;
                    if( pt == null )
                        return false;
                    paramTypes.Add( pt );
                }
            }

            var funcMt = new MetaType( new FunctionSignatureMetaClass( hiddenName, retType, paramTypes ) );

            var mmv = new MetaMemberVariable( mc, hiddenName );
            mmv.SetIsStatic( true );
            mmv.SetIsConst( false );
            mmv.SetIsDefineMetaType( true );
            mmv.SetMetaDefineType( funcMt );
            mmv.SetRealMetaType( funcMt );

            // static Func<Ret,P...> __dll_name = FFI.StaticLibrary.bindFunction( path, symbol, sig )
            var fm = mmf.fileMetaMemberFunction?.fileMeta;
            var bindTerm = BuildBindFunctionCallTerm( fm, libPath, symbol, sig );
            CreateExpressParam cep = new CreateExpressParam();
            cep.ownerMetaBase = mc;
            cep.metaType = funcMt;
            cep.equalMetaVariable = mmv;
            cep.parsefrom = EParseFrom.MemberVariableExpress;
            cep.isConst = false;
            cep.isStatic = true;
            cep.fme = bindTerm;
            var express = ExpressManager.CreateExpressNode( cep );
            if( express == null )
            {
                Log.AddMetaCoreLog( LID.MetaCoreMemberFunctionDllImportInject, mmf.token,
                    $"DllImport: 隐藏字段 {hiddenName} 的 bind 表达式构建失败, 跳过注入!!" );
                return false;
            }
            mmv.SetExpress( express );

            mc.AddMetaMemberVariable( mmv, false );
            mmv.ParseMetaExpress();
            mmv.ParseRealMetaType();
            return true;
        }

        /// <summary>
        /// 函数体链头注入 if ( __dll_name != null ) { ret __dll_name( a1, ... ) }。
        /// 原体原位保留在 if 之后（等价旧改写器的 else fallback 分支）。
        /// </summary>
        private static void InjectDispatchIf( MetaMemberFunction mmf, FileMetaMemberFunction fmfm, string hiddenName )
        {
            var fm = fmfm.fileMeta;
            var blockSyntax = fmfm.fileMetaBlockSyntax;
            string path = fm?.path ?? "";
            var nameToken = mmf.token;
            int line = ( nameToken?.sourceBeginLine ?? 1 ) - 1;
            int pos = nameToken?.sourceBeginChar ?? 0;

            // ── 条件: __dll_name != null ──
            var condNodeList = new List<Node>();
            condNodeList.Add( MakeIdentLinkNode( path, line, pos, hiddenName ) );    // 单节点链无需 SetIdentifierNode
            var neqNode = new Node( new Token( path, ETokenType.NotEqual, "!=", line, pos ) ) { nodeType = ENodeType.Symbol };
            neqNode.priority = SignComputePriority.Level7_EqualAb;
            condNodeList.Add( neqNode );
            condNodeList.Add( new Node( new Token( path, ETokenType.Null, "null", line, pos ) ) { nodeType = ENodeType.ConstValue } );
            var condTerm = FileMetatUtil.CreateFileMetaExpress( fm, condNodeList, FileMetaTermExpress.EExpressType.Common );
            if( condTerm == null )
            {
                Log.AddMetaCoreLog( LID.MetaCoreMemberFunctionDllImportInject, mmf.token,
                    $"DllImport: 分派条件表达式构建失败, 跳过 {hiddenName} 转发注入!!" );
                return;
            }

            // ── then 块: ret __dll_name( a1, ... ) / 裸调用 + 裸 ret ──
            var thenBlock = new FileMetaBlockSyntax( fm, blockSyntax.beginBlock, blockSyntax.endBlock );
            var callNode = MakeIdentLinkNode( path, line, pos, hiddenName );
            callNode.SetParNode( MakeArgsParNode( path, line, pos, mmf ) );
            var retToken = new Token( path, ETokenType.Return, "ret", line, pos );
            bool isVoid = mmf.GetFinalMetaType()?.metaClass == CoreMetaClassManager.voidMetaClass;
            if( isVoid )
            {
                // void: 裸调用后必须补裸 ret; —— 防 then 落空后继续穿透执行原体
                thenBlock.AddFileMetaSyntax( new FileMetaCallSyntax( new FileMetaCallLink( fm, callNode, true ) ) );
                thenBlock.AddFileMetaSyntax( new FileMetaKeyReturnSyntax( fm, retToken, null, new List<Node>() ) );
            }
            else
            {
                var callTerm = new FileMetaCallTerm( fm, callNode );
                thenBlock.AddFileMetaSyntax( new FileMetaKeyReturnSyntax( fm, retToken, callTerm, new List<Node>() ) );
            }

            // ── if 语句: MetaIfStatements 构造内自动解析条件 + 展开 then 块 ──
            var ifToken = new Token( path, ETokenType.If, "if", line, pos );
            var ifSyntax = new FileMetaKeyIfSyntax( fm );
            ifSyntax.SetFileMetaConditionExpressSyntax( new FileMetaConditionExpressSyntax( fm, ifToken, condTerm, thenBlock ) );
            ifSyntax.SetToken( ifToken );

            var ifStmt = new MetaIfStatements( mmf.metaBlockStatements, ifSyntax );
            mmf.metaBlockStatements.AddFrontStatements( ifStmt );
        }

        /// <summary>
        /// 合成 FFI.StaticLibrary.bindFunction( libPath, symbol, sig ) 的
        /// FileMetaCallTerm（节点构造同 MetaMemberVariable.BuildLibraryGetFunctionCallTerm
        /// 程序化先例; 链头 SetIdentifierNode 必须先设置, 否则 AddLinkNode 静默失效）。
        /// </summary>
        private static FileMetaCallTerm BuildBindFunctionCallTerm( FileMeta fm, string libPath, string symbol, string sig )
        {
            string path = fm?.path ?? "";
            int line = 0;
            int pos = 0;

            // FFI.StaticLibrary.bindFunction( libPath, symbol, sig )
            var bindNode = MakeIdentLinkNode( path, line, pos, "bindFunction" );
            bindNode.SetParNode( MakeStringArgsParNode( path, line, pos, libPath, symbol, sig ) );

            // FFI -> . -> StaticLibrary -> . -> bindFunction(...)
            var ffiNode = MakeIdentLinkNode( path, line, pos, "FFI" );
            ffiNode.SetIdentifierNode( ffiNode );
            ffiNode.AddLinkNode( MakePeriodNode( path, line, pos ) );
            ffiNode.AddLinkNode( MakeIdentLinkNode( path, line, pos, "StaticLibrary" ) );
            ffiNode.AddLinkNode( MakePeriodNode( path, line, pos ) );
            ffiNode.AddLinkNode( bindNode );

            return new FileMetaCallTerm( fm, ffiNode );
        }

        /// <summary>
        /// 合成 ( a1, a2, ... ) 实参 Par 节点（实参名取函数形参名; 零参为空 Par）。
        /// </summary>
        private static Node MakeArgsParNode( string path, int line, int pos, MetaMemberFunction mmf )
        {
            var parNode = new Node( new Token( path, ETokenType.LeftPar, "(", line, pos ) ) { nodeType = ENodeType.Par };
            parNode.endToken = new Token( path, ETokenType.RightPar, ")", line, pos );
            var plist = mmf.metaMemberParamCollection?.metaDefineParamList;
            if( plist != null )
            {
                for( int i = 0; i < plist.Count; i++ )
                {
                    if( i > 0 )
                    {
                        parNode.AddChild( new Node( new Token( path, ETokenType.Comma, ",", line, pos ) ) { nodeType = ENodeType.Comma } );
                    }
                    parNode.AddChild( MakeIdentLinkNode( path, line, pos, plist[i].name ) );
                }
            }
            return parNode;
        }

        private static Node MakeIdentLinkNode( string path, int line, int pos, string name )
        {
            return new Node( new Token( path, ETokenType.Identifier, name, line, pos ) ) { nodeType = ENodeType.IdentifierLink };
        }

        private static Node MakePeriodNode( string path, int line, int pos )
        {
            return new Node( new Token( path, ETokenType.Period, ".", line, pos ) ) { nodeType = ENodeType.Period };
        }

        /// <summary>
        /// 合成 ( "s1", "s2", ... ) 字符串实参 Par 节点: childList 为
        /// [ConstValue, Comma, ConstValue, ...]（FileMetaParTerm 按 Comma 拆分）。
        /// String token 形态与 MetaMemberVariable.MakeStringArgsParNode 一致
        /// （lexeme 带引号 + 单子 token 存内容, MetaConstExpressNode 取子 token）。
        /// </summary>
        private static Node MakeStringArgsParNode( string path, int line, int pos, params string[] stringArgs )
        {
            var parNode = new Node( new Token( path, ETokenType.LeftPar, "(", line, pos ) ) { nodeType = ENodeType.Par };
            parNode.endToken = new Token( path, ETokenType.RightPar, ")", line, pos );
            for( int i = 0; i < stringArgs.Length; i++ )
            {
                if( i > 0 )
                {
                    parNode.AddChild( new Node( new Token( path, ETokenType.Comma, ",", line, pos ) ) { nodeType = ENodeType.Comma } );
                }
                var strToken = new Token( path, ETokenType.String, "\"" + stringArgs[i] + "\"", line, pos );
                strToken.AddChildrenToken( new Token( path, ETokenType.String, stringArgs[i], line, pos ) );
                parNode.AddChild( new Node( strToken ) { nodeType = ENodeType.ConstValue } );
            }
            return parNode;
        }
    }
}
