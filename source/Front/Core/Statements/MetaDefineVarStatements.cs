//****************************************************************************
//  File:      MetaNewStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Compile;
using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using System;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaDefineVarStatements : MetaStatements
    {
        public MetaExpressNodeBase expressNode => m_ExpressNode;
        public MetaVariable defineVarMetaVariable => m_DefineVarMetaVariable;

        private FileMetaDefineVariableSyntax m_FileMetaDefineVariableSyntax = null;
        private FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax = null;
        private FileMetaCallSyntax m_FileMetaCallSyntax = null;

        private MetaVariable m_DefineVarMetaVariable = null;
        private MetaExpressNodeBase m_ExpressNode = null;
        public MetaDefineVarStatements( MetaBlockStatements mbs ) : base(mbs)
        {
        }
        public MetaDefineVarStatements(MetaBlockStatements mbs, FileMetaDefineVariableSyntax fmdvs ) : base( mbs )
        {
            m_FileMetaDefineVariableSyntax = fmdvs;
            m_Name = fmdvs.name;
            m_Token = fmdvs.nameToken;
            m_OwnerMetaBlockStatements.AddOnlyNameMetaVariable(m_Name);

            Parse();
        }
        public MetaDefineVarStatements(MetaBlockStatements mbs, FileMetaOpAssignSyntax fmoas ): base( mbs )
        {
            m_FileMetaOpAssignSyntax = fmoas;
            m_Token = fmoas.token;
            m_Name = m_FileMetaOpAssignSyntax.variableRef.name;
            m_OwnerMetaBlockStatements.AddOnlyNameMetaVariable(m_Name);

            Parse();
        }
        public MetaDefineVarStatements( MetaBlockStatements mbs, FileMetaCallSyntax callSyntax ):base( mbs )
        {
            m_FileMetaCallSyntax = callSyntax;
            m_Name = callSyntax.variableRef.name;
            m_Token = callSyntax.variableRef.callNodeList[0].token;
            m_OwnerMetaBlockStatements.AddOnlyNameMetaVariable(m_Name);
            Parse();
        }
        private void Parse()
        {
            string defineName = m_Name;
            MetaType leftMt = null;
            var metaFunction = m_OwnerMetaBlockStatements?.ownerMetaFunction;

            bool isSynamicData = false;
            FileMetaBaseTerm fileExpress = null;
            MetaType expressRetMetaDefineType = null;
            if ( m_FileMetaDefineVariableSyntax != null )
            {
                var fmcd = m_FileMetaDefineVariableSyntax.fileMetaClassDefine;
                leftMt = TypeManager.instance.GetMetaTypeByTemplateFunction(ownerMetaClass, m_OwnerMetaBlockStatements.ownerMetaFunction as MetaMemberFunction, fmcd);
                if(leftMt == null )
                {
                    Log.AddMetaCoreLog(LID.MetaCoreNotFoundMetaTypeByFMClassDefine, fmcd.classNameToken, "DefineStatements Parse", fmcd.name );
                    return;
                }

                if(leftMt.metaClass is MetaGenTemplateClass mgtc )
                {
                    mgtc.ParseGenTemplateClass(mgtc);
                    mgtc.ParseGenMemberVarible();
                }
                Node node = new Node(m_Token);
                FileMetaCallNode fmcn = new FileMetaCallNode(m_FileMetaDefineVariableSyntax.fileMeta, node);
                MetaCallNode mcn = new MetaCallNode(null, fmcn, ownerMetaBase, m_OwnerMetaBlockStatements );
                mcn.SetAllowUseSettings(new AllowUseSettings());
                mcn.SetToken(m_Token);
                mcn.GetFirstNode(m_Name, ownerMetaBase, 0);
                if (mcn.callNodeType != ECallNodeType.None)
                {
                    // local{} init 上下文: `float len = expr` 的 len 已被 LocalManager
                    // 预提升为 _Local 类占位成员，这里仍按局部变量定义解析
                    //（末尾 this.len = len 同步语句写回成员），不算重复定义
                    bool isLocalPlaceholder = mcn.callNodeType == ECallNodeType.MemberVariableName
                        && mcn.metaVariable is MetaMemberVariable pmv
                        && !pmv.isStatic
                        && LocalManager.IsFileLocalClass(pmv.ownerMetaClass);
                    if (!isLocalPlaceholder)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, $"名称{m_Name}与{mcn.callNodeType} 有重复");
                        return;
                    }
                }

                m_DefineVarMetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, m_OwnerMetaBlockStatements.ownerMetaClass, leftMt );
                m_DefineVarMetaVariable.SetIsDefineMetaType(true);
                m_DefineVarMetaVariable.SetIsConst(m_FileMetaDefineVariableSyntax.constToken != null);
                m_DefineVarMetaVariable.AddPingToken(m_FileMetaDefineVariableSyntax.token);
                fileExpress = m_FileMetaDefineVariableSyntax.express;

                // Func<Ret, P1, P2...> / 函数 typealias 声明 + FFI 取函数调用:
                // 调用点只传 name 一个参数时, 从左侧函数签名类型推导 FFI sig
                // 自动补全第二个实参 (详见 TryInjectFFIFunctionSig)
                if ( leftMt != null && leftMt.metaClass is FunctionSignatureMetaClass fsmc )
                {
                    TryInjectFFIFunctionSig( fileExpress, fsmc );
                }

            }
            else if (m_FileMetaOpAssignSyntax != null)
            {
                m_DefineVarMetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, m_OwnerMetaBlockStatements.ownerMetaClass, leftMt);
                m_DefineVarMetaVariable.SetIsConst(m_FileMetaOpAssignSyntax.constToken != null);
                m_Token = m_FileMetaOpAssignSyntax.variableRef.callNodeList[0].token;
                AddPingToken(m_Token);
                m_DefineVarMetaVariable.AddPingToken(m_Token);
                //if ( m_FileMetaOpAssignSyntax.dynamicToken != null )
                //{
                //    isDynamicClass = true;
                //    leftMt = null;// new MetaType(CoreMetaClassManager.dynamicMetaClass);
                //}
                //else
                    if( m_FileMetaOpAssignSyntax.dataToken != null )
                {
                    isSynamicData = true;
                    leftMt = new MetaType(CoreMetaClassManager.dynamicMetaData );
                }
                else if (m_FileMetaOpAssignSyntax.functionToken != null)
                {
                    // function 声明: 不检查函数签名类型, 变量类型固定为 Function 基类
                    // (类似 var 的宽松语义), 后续对该变量的调用 f(a,b) 按闭包/函数调用处理
                    leftMt = new MetaType(CoreMetaClassManager.functionMetaClass);
                    m_DefineVarMetaVariable.SetMetaDefineType(leftMt);
                    m_DefineVarMetaVariable.SetIsDefineMetaType(true);
                    m_DefineVarMetaVariable.SetRealMetaType(new MetaType(leftMt));
                }
                if (m_FileMetaOpAssignSyntax.variableRef != null)
                {
                    if( m_FileMetaOpAssignSyntax.variableRef.callNodeList.Count != 1 )
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "call node list is not count equal 1");
                        return;
                    }
                    MetaCallNode mcn = new MetaCallNode( null, m_FileMetaOpAssignSyntax.variableRef.callNodeList[0],ownerMetaBase, m_OwnerMetaBlockStatements );
                    mcn.SetAllowUseSettings(new AllowUseSettings());
                    mcn.SetToken(m_Token);
                    mcn.GetFirstNode(m_Name, ownerMetaBase, 0);
                    if( mcn.callNodeType != ECallNodeType.None )
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, $"名称{m_Name}与{mcn.callNodeType } 有重复");
                        return;
                    }
                }

                fileExpress = m_FileMetaOpAssignSyntax.express;
            }
            else if (m_FileMetaCallSyntax!= null )
            {
                m_DefineVarMetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaBlockStatements, m_OwnerMetaBlockStatements.ownerMetaClass, leftMt);
                m_DefineVarMetaVariable.AddPingToken(m_FileMetaCallSyntax.token);

                expressRetMetaDefineType = new MetaType(CoreMetaClassManager.objectMetaClass);

                m_DefineVarMetaVariable.SetIsDefineMetaType(true);
                m_DefineVarMetaVariable.SetMetaDefineType(expressRetMetaDefineType);
            }
            if(m_DefineVarMetaVariable == null )
            {
                Log.AddMetaCoreLog( LID.MetaCoreDefineVariableParseIsNull, m_Token, "" +  defineName);
                return;
            }
            m_OwnerMetaBlockStatements.UpdateMetaVariableDict(m_DefineVarMetaVariable);

            if (fileExpress != null)
            {
                // lib.lookupFunction<Ret, P1, ...>( name ): 调用点模板实参提供
                // FFI 签名, 注入后内部仍走 getFunction 现有体系 (详见
                // TryInjectLookupFunctionSig)。须在表达式解析前完成改写。
                TryInjectLookupFunctionSig( fileExpress );

                // Memory.nativeStructToData<DataName>( addr ): 调用点模板实参
                // 提供 data 类型名, 注入为第二个实参 (详见
                // TryInjectNativeStructToDataTypeName)。须在表达式解析前完成改写。
                TryInjectNativeStructToDataTypeName( fileExpress );

                CreateExpressParam cep = new CreateExpressParam();
                cep.fme = fileExpress;
                cep.equalMetaVariable = m_DefineVarMetaVariable;
                cep.metaType = leftMt;
                cep.ownerMBS = m_OwnerMetaBlockStatements;
                cep.ownerMetaBase = m_OwnerMetaBlockStatements.ownerMetaClass;

                m_ExpressNode = ExpressManager.CreateExpressNodeByCEP(cep);
                if (m_ExpressNode == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 解析新建变量语句时，表达式解析为空!!__1");
                    return;
                }
                m_ExpressNode.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                m_ExpressNode.CalcReturnType();

                m_ExpressNode = ExpressManager.ConvertNewExpress(m_ExpressNode, leftMt );

                // If the initializer is a const literal whose type doesn't match the
                // declared variable type (e.g. Byte b8 = 250 where 250 is Int32),
                // fold the constant to the target type at compile time so that IR
                // generation emits the correct LoadConst opcode (e.g. LoadConstUInt8)
                // instead of LoadConstInt32 + a runtime Convert.
                if (m_ExpressNode is MetaConstExpressNode mcen)
                {
                    ExpressManager.TryAdjustConstExpressByDefineMetaType(leftMt, mcen);
                }

                expressRetMetaDefineType = m_ExpressNode.GetReturnMetaType();
            }

            if (expressRetMetaDefineType == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error 解析新建变量语句时，表达式返回类型为空!!__2", defineName);
                return;
            }
            //if(metaFunction.IsEqualMetaTemplateCollectionAndMetaParamCollection )
            if (!m_DefineVarMetaVariable.isDefineMetaType )
            {
                m_DefineVarMetaVariable.SetRealMetaType(expressRetMetaDefineType);
            }
            else
            {
                if (TypeManager.CompareLeftRightMetaType(m_DefineVarMetaVariable.defineMetaType, expressRetMetaDefineType, m_Token,
                            out var convertMetaType))
                {
                    if (convertMetaType != null)
                    {
                        m_DefineVarMetaVariable.SetRealMetaType(convertMetaType);
                    }
                    else
                    {
                        m_DefineVarMetaVariable.SetRealMetaType(expressRetMetaDefineType);
                    }
                }
                else
                {
                    // Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error DefineVarStatement表达式中返回定义类型为空 "
                    //     + $"(left={m_DefineVarMetaVariable.defineMetaType?.metaClass?.allName ?? "null"}, "
                    //     + $"right={expressRetMetaDefineType?.metaClass?.allName ?? "null"}, var={m_Name}) ");
                }
            }
            SetTRMetaVariable(m_DefineVarMetaVariable);
        }

        //==================== FFI 函数签名推导 ====================
        //
        // 声明目标为 Func<Ret, P1, P2...> / 函数 typealias (FunctionSignatureMetaClass)
        // 且初始化表达式是 FFI 取函数的单实参调用时, 从左侧签名类型推导 FFI sig
        // 字符串并注入为第二个实参:
        //     Func<int,int,int> addf = lib.getFunction( "addcalc" )
        //         -> lib.getFunction( "addcalc", "i32,i32->i32" )
        //     Func<int,int,int> addf = lib.getSymbol( "addcalc" )
        //         -> lib.getFunction( "addcalc", "i32,i32->i32" )
        // (getSymbol 只返回裸地址 Int64, 无法赋给函数类型变量, 因此同时把
        //  调用名改写为 getFunction, 由其包装成可调用的 native 闭包)
        // 签名中任一类型无法映射到 FFI 短名(如 Ptr)时不注入, 保持原调用,
        // 用户仍可手写完整 sig 字符串作为第二个实参。
        private static void TryInjectFFIFunctionSig( FileMetaBaseTerm fileExpress, FunctionSignatureMetaClass fsmc )
        {
            if( !(fileExpress is FileMetaCallTerm fmct) )
                return;
            var link = fmct.callLink;
            if( link == null || link.callNodeList.Count == 0 )
                return;
            // 末节点须是函数调用 (lib.getFunction(...) / this.lib.getSymbol(...))
            var lastNode = link.callNodeList[link.callNodeList.Count - 1];
            if( !lastNode.isCallFunction || lastNode.fileMetaParTerm == null )
                return;
            var callName = lastNode.name;
            if( callName != "getFunction" && callName != "getSymbol" )
                return;
            var fmpt = lastNode.fileMetaParTerm;
            if( fmpt.SplitParamList().Count != 1 )
                return;   // 已传 sig / 空实参 / 多段 -> 不注入

            string sig = BuildFFIFunctionSig( fsmc );
            if( string.IsNullOrEmpty( sig ) )
                return;

            // getSymbol(name) -> getFunction(name, sig): 裸地址无法直接赋给
            // 函数类型变量, 改写调用名 (仅 lexeme; IR 使用解析后的方法元数据)
            if( callName == "getSymbol" )
            {
                lastNode.token?.SetLexeme( "getFunction" );
            }

            // 追加第二个实参: Comma 符号 + String 常量
            // (与 FileMetaParTerm 构造器的 Comma/ConstValue 分支一致,
            //  SplitParamList 按 Comma symbol term 拆分实参)
            var fm = lastNode.fileMeta;
            string path = fm?.path;
            int line = (lastNode.token?.sourceBeginLine ?? 1) - 1;
            int pos = lastNode.token?.sourceBeginChar ?? 0;

            var commaToken = new Token( path, ETokenType.Comma, ",", line, pos );
            var commaTerm = new FileMetaSymbolTerm( fm, commaToken );
            commaTerm.priority = SignComputePriority.Level12_Split;
            fmpt.AddFileMetaTerm( commaTerm );

            // String 常量 Token: MetaConstExpressNode.Parse 要求
            // childrenTokensList 为 [[同内容 String Token]]
            var strToken = new Token( path, ETokenType.String, "\"" + sig + "\"", line, pos );
            strToken.AddChildrenToken( new Token( path, ETokenType.String, sig, line, pos ) );
            fmpt.AddFileMetaTerm( new FileMetaConstValueTerm( fm, strToken ) );
        }

        //==================== FFI lookupFunction 模板实参签名注入 ====================
        //
        // lib.lookupFunction<Ret, P1, P2...>( "name" ): 调用点模板实参直接给出
        // 函数签名, 前端把模板实参转成 FFI sig 字符串并注入为第二个实参:
        //     var addf = lib.lookupFunction<int,int,int>( "addcalc" )
        //         -> lib.getFunction( "addcalc", "i32,i32->i32" )
        // (lookupFunction 是纯前端语法糖: 调用名改写为 getFunction, 内部仍走
        //  getFunction(name, sig) 现有体系; 与 Func<Ret,P...> 左侧类型推导路径
        //  互补——模板实参在调用点自带签名, 覆盖 var / 无类型声明场景)
        // 模板实参须为单段标量类型名且能映射到 FFI 短名(见 FFISigNameOfTypeName);
        // 任一不满足时不注入, 保持原调用(用户可手写 getFunction 完整形态)。
        private static void TryInjectLookupFunctionSig( FileMetaBaseTerm fileExpress )
        {
            if( !(fileExpress is FileMetaCallTerm fmct) )
                return;
            var link = fmct.callLink;
            if( link == null || link.callNodeList.Count == 0 )
                return;
            // 末节点须是函数调用 (lib.lookupFunction<...>(...))
            var lastNode = link.callNodeList[link.callNodeList.Count - 1];
            if( !lastNode.isCallFunction || lastNode.fileMetaParTerm == null )
                return;
            if( lastNode.name != "lookupFunction" )
                return;
            var tplList = lastNode.inputTemplateNodeList;
            if( tplList == null || tplList.Count < 1 )
                return;   // 无模板实参 -> 不注入
            var fmpt = lastNode.fileMetaParTerm;
            if( fmpt.SplitParamList().Count != 1 )
                return;   // 已传 sig / 空实参 / 多段 -> 不注入

            // 模板实参 <Ret, P1, P2...> -> sig "p1,p2,...->ret"
            string retTn = FFISigNameOfTemplateNode( tplList[0] );
            if( string.IsNullOrEmpty( retTn ) )
                return;
            var sb = new StringBuilder();
            for( int i = 1; i < tplList.Count; i++ )
            {
                var tn = FFISigNameOfTemplateNode( tplList[i] );
                if( string.IsNullOrEmpty( tn ) )
                    return;
                if( i > 1 )
                    sb.Append( ',' );
                sb.Append( tn );
            }
            sb.Append( "->" );
            sb.Append( retTn );

            // lookupFunction(name) -> getFunction(name, sig): 纯前端语法糖,
            // 改写调用名 (仅 lexeme; IR 使用解析后的方法元数据), 并清空模板
            // 实参列表——getFunction 是非模板方法, 方法查找按模板计数精确
            // 匹配, 残留的 <Ret,P...> 会导致 getFunction 查找失败
            lastNode.token?.SetLexeme( "getFunction" );
            lastNode.ClearInputTemplateNodeList();

            // 追加第二个实参: Comma 符号 + String 常量
            // (与 TryInjectFFIFunctionSig 的注入方式一致)
            var fm = lastNode.fileMeta;
            string path = fm?.path;
            int line = (lastNode.token?.sourceBeginLine ?? 1) - 1;
            int pos = lastNode.token?.sourceBeginChar ?? 0;

            var commaToken = new Token( path, ETokenType.Comma, ",", line, pos );
            var commaTerm = new FileMetaSymbolTerm( fm, commaToken );
            commaTerm.priority = SignComputePriority.Level12_Split;
            fmpt.AddFileMetaTerm( commaTerm );

            var sigToken = new Token( path, ETokenType.String, "\"" + sb.ToString() + "\"", line, pos );
            sigToken.AddChildrenToken( new Token( path, ETokenType.String, sb.ToString(), line, pos ) );
            fmpt.AddFileMetaTerm( new FileMetaConstValueTerm( fm, sigToken ) );
        }

        //==================== nativeStructToData 模板实参类型名注入 ====================
        //
        // Memory.nativeStructToData<DataName>( addr ): 调用点模板实参直接给出
        // data 类型名, 前端把类型名转成字符串并注入为第二个实参, 但保留
        // 模板实参本身:
        //     var dn = Memory.nativeStructToData<FFIStructSample>( ptr )
        //         -> Memory.nativeStructToData<FFIStructSample>( ptr, "FFIStructSample" )
        // Memory 里同名方法按模板实参数分桶: <T>(Int64,string) 模板版在
        // bucket[1], (Int64,string) 非模板版在 bucket[0]; 保留模板实参让
        // 该调用匹配模板版, 实例化后返回强类型 DataName 实例 (T 由前端
        // 替换, C 端按 typeName 查 RuntimeClass 构建 data 骨架后从 native
        // 内存加载; 多段类型名(NS.DataName)按 '.' 拼接, C 端按名匹配支持
        // 全名/短名)
        private static void TryInjectNativeStructToDataTypeName( FileMetaBaseTerm fileExpress )
        {
            if( !(fileExpress is FileMetaCallTerm fmct) )
                return;
            var link = fmct.callLink;
            if( link == null || link.callNodeList.Count == 0 )
                return;
            // 末节点须是函数调用 (Memory.nativeStructToData<...>(...))
            var lastNode = link.callNodeList[link.callNodeList.Count - 1];
            if( !lastNode.isCallFunction || lastNode.fileMetaParTerm == null )
                return;
            if( lastNode.name != "nativeStructToData" )
                return;
            var tplList = lastNode.inputTemplateNodeList;
            if( tplList == null || tplList.Count != 1 )
                return;   // 无模板实参/多个模板实参 -> 不注入
            var fmpt = lastNode.fileMetaParTerm;
            if( fmpt.SplitParamList().Count != 1 )
                return;   // 已传 typeName / 空实参 / 多段 -> 不注入

            // 模板实参 <DataName> -> 类型名 (多段 NS.DataName 以 '.' 拼接)
            var nl = tplList[0]?.nameList;
            if( nl == null || nl.Count == 0 )
                return;
            string typeName = string.Join( ".", nl );

            // 保留模板实参列表: Memory.nativeStructToData<T>(Int64,string)
            // 模板版与 (Int64,string) 非模板版同名共存, 方法查找按模板计数
            // 精确匹配, 保留 <DataName> 使该调用命中模板版并实例化出强类型
            // 返回值

            // 追加第二个实参: Comma 符号 + String 常量
            // (与 TryInjectLookupFunctionSig 的注入方式一致)
            var fm = lastNode.fileMeta;
            string path = fm?.path;
            int line = (lastNode.token?.sourceBeginLine ?? 1) - 1;
            int pos = lastNode.token?.sourceBeginChar ?? 0;

            var commaToken = new Token( path, ETokenType.Comma, ",", line, pos );
            var commaTerm = new FileMetaSymbolTerm( fm, commaToken );
            commaTerm.priority = SignComputePriority.Level12_Split;
            fmpt.AddFileMetaTerm( commaTerm );

            var nameToken = new Token( path, ETokenType.String, "\"" + typeName + "\"", line, pos );
            nameToken.AddChildrenToken( new Token( path, ETokenType.String, typeName, line, pos ) );
            fmpt.AddFileMetaTerm( new FileMetaConstValueTerm( fm, nameToken ) );
        }

        /// <summary>
        /// 模板实参节点(FileInputTemplateNode) -> FFI sig 短名。
        /// 只接受单段标量类型名(int/Int32/string...), 多段名(NS.Class1)不映射。
        /// </summary>
        private static string FFISigNameOfTemplateNode( FileInputTemplateNode fitn )
        {
            var nl = fitn?.nameList;
            if( nl == null || nl.Count != 1 )
                return null;
            return FFISigNameOfTypeName( nl[0] );
        }

        /// <summary>
        /// SL 类型名 -> FFI sig 短名 (与 cvm 侧 vm_ffi_sl_name_to_ffi 映射表
        /// 对齐, 另含 C# 风格别名; 注入的 sig 均为标准短名, cvm 侧无需再认识
        /// SL 名)。未知名返回 null (不注入)。
        /// </summary>
        private static string FFISigNameOfTypeName( string name )
        {
            switch( name )
            {
                case "void": case "Void":               return "void";
                case "bool": case "Bool": case "boolean": return "bool";
                case "int": case "Int32":               return "i32";
                case "Int8": case "sbyte":              return "i8";
                case "UInt8": case "byte":              return "u8";
                case "Int16": case "short":             return "i16";
                case "UInt16":                          return "u16";
                case "UInt32": case "uint":             return "u32";
                case "long": case "Int64":              return "i64";
                case "UInt64": case "ulong":            return "u64";
                case "float": case "Float32":           return "f32";
                case "double": case "Float64":          return "f64";
                case "Float16":                         return "f16";
                case "Float16_Brain":                   return "bf16";
                case "Float8":                          return "f8e4m3";
                case "Float8_E5M2":                     return "f8e5m2";
                case "string": case "String":           return "utf8";
                case "ptr": case "Ptr":                 return "ptr";
                default:                                return null;
            }
        }

        /// <summary>
        /// 从 FunctionSignatureMetaClass 组 FFI sig 短名字符串
        /// ("i32,i32->i32", 与 cvm 侧 sl_ffi_sig_parse 格式一致);
        /// 任一类型无法映射时返回 null (不注入)。
        /// (public: 供 @DllImport 成员变量初始化注入复用)
        /// </summary>
        public static string BuildFFIFunctionSig( FunctionSignatureMetaClass fsmc )
        {
            var sb = new StringBuilder();
            var paramList = fsmc.paramMetaTypeList;
            if( paramList != null )
            {
                for( int i = 0; i < paramList.Count; i++ )
                {
                    var tn = FFISigNameOfMetaType( paramList[i] );
                    if( string.IsNullOrEmpty( tn ) )
                        return null;
                    if( i > 0 )
                        sb.Append( ',' );
                    sb.Append( tn );
                }
            }
            var retTn = FFISigNameOfMetaType( fsmc.returnMetaType );
            if( string.IsNullOrEmpty( retTn ) )
                return null;
            sb.Append( "->" );
            sb.Append( retTn );
            return sb.ToString();
        }

        /// <summary>
        /// SL 类型 -> FFI sig 短名。先按类型单例匹配, 再按类名回退
        /// (与 cvm 侧 vm_ffi_sl_name_to_ffi 映射表对齐; Ptr 保守不映射,
        ///  需要 Ptr 签名时用户可手写完整 sig 字符串)。
        /// </summary>
        private static string FFISigNameOfMetaType( MetaType mt )
        {
            if( mt == null )
                return null;
            var mc = mt.metaClass;
            if( mc == null )
                return null;
            if( mc == CoreMetaClassManager.voidMetaClass )           return "void";
            if( mc == CoreMetaClassManager.booleanMetaClass )        return "bool";
            if( mc == CoreMetaClassManager.int8MetaClass )           return "i8";
            if( mc == CoreMetaClassManager.uint8MetaClass )          return "u8";
            if( mc == CoreMetaClassManager.int16MetaClass )          return "i16";
            if( mc == CoreMetaClassManager.uint16MetaClass )         return "u16";
            if( mc == CoreMetaClassManager.int32MetaClass )          return "i32";
            if( mc == CoreMetaClassManager.uint32MetaClass )         return "u32";
            if( mc == CoreMetaClassManager.int64MetaClass )          return "i64";
            if( mc == CoreMetaClassManager.uint64MetaClass )         return "u64";
            if( mc == CoreMetaClassManager.float32MetaClass )        return "f32";
            if( mc == CoreMetaClassManager.float64MetaClass )        return "f64";
            if( mc == CoreMetaClassManager.float16MetaClass )        return "f16";
            if( mc == CoreMetaClassManager.float16_BrainMetaClass )  return "bf16";
            if( mc == CoreMetaClassManager.float8MetaClass )         return "f8e4m3";
            if( mc == CoreMetaClassManager.float8_E5M2MetaClass )    return "f8e5m2";
            if( mc == CoreMetaClassManager.stringMetaClass )         return "utf8";
            switch( mc.name )
            {
                case "void":  case "Void":    return "void";
                case "bool":  case "Bool":    case "boolean": return "bool";
                case "int":   case "Int32":   return "i32";
                case "Int8":  case "UInt8":   return "i8";
                case "Int16": case "UInt16":  return "i16";
                case "UInt32":                return "u32";
                case "long":  case "Int64":   return "i64";
                case "UInt64":                return "u64";
                case "float": case "Float32": return "f32";
                case "double":case "Float64": return "f64";
                case "Float16":               return "f16";
                case "Float16_Brain":         return "bf16";
                case "Float8":                return "f8e4m3";
                case "Float8_E5M2":           return "f8e5m2";
                case "string": case "String": return "utf8";
                default:                      return null;
            }
        }
        public override void SetTRMetaVariable(MetaVariable mv)
        {
            if(m_ExpressNode != null && m_ExpressNode is MetaExecuteStatementsNode )
            {
                (m_ExpressNode as MetaExecuteStatementsNode).UpdateTrMetaVariable(mv);
            }
            if (nextMetaStatements != null)
            {
                nextMetaStatements.SetTRMetaVariable(mv);
            }
        }
        //public override MetaStatements GenTemplateClassStatement(MetaGenTemplateClass mgt, MetaBlockStatements parentMs)
        //{
        //    MetaDefineVarStatements mns = new MetaDefineVarStatements(parentMs);
        //    mns.m_FileMetaDefineVariableSyntax = m_FileMetaDefineVariableSyntax;
        //    mns.m_FileMetaOpAssignSyntax = m_FileMetaOpAssignSyntax;
        //    mns.m_FileMetaCallSyntax = m_FileMetaCallSyntax;
        //    mns.m_IsNeedCastStatements = m_IsNeedCastStatements;
        //    mns.m_DefineVarMetaVariable = new MetaVariable(m_DefineVarMetaVariable);
        //    mns.m_ExpressNode = m_ExpressNode;
        //    mns.m_DefineVarMetaVariable.GenTemplateMetaVaraible( mgt, parentMs );
        //    if (m_NextMetaStatements != null)
        //    {
        //        m_NextMetaStatements.GenTemplateClassStatement(mgt, parentMs);
        //    }
        //    return mns;
        //}
        public override void SetDeep(int dp)
        {
            base.SetDeep(dp);
            if (m_ExpressNode is MetaExecuteStatementsNode)
            {
                (m_ExpressNode as MetaExecuteStatementsNode).SetDeep(dp);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
            sb.Append(m_DefineVarMetaVariable.ToFormatString());
            sb.Append(" = ");
            if (m_DefineVarMetaVariable.defineMetaType.isData)
            {
                sb.Append(m_ExpressNode.ToFormatString());
            }
            else if (m_DefineVarMetaVariable.defineMetaType.isEnum)
            {
            }
            else
            {
                if (m_IsNeedCastState)
                {
                    sb.Append("(");
                }
                sb.Append(m_ExpressNode?.ToFormatString());
                if (m_IsNeedCastState)
                {
                    sb.Append(").cast<" + m_DefineVarMetaVariable.defineMetaType.metaClass.allName + ">()");
                }
                sb.Append(";");
            }

            if (nextMetaStatements != null)
            {
                sb.Append(Environment.NewLine);
                sb.Append(nextMetaStatements.ToFormatString());
            }

            return sb.ToString();

        }
    }
}
