//****************************************************************************
//  File:      BindExpandManager.cs
// ------------------------------------------------
//  Description: bind 关键字语义展开器
//               在 File 阶段完成后、MetaCore CreateNamespace 之前执行
//               将 bind 的 data 结构展开为 class 的成员变量、字段访问器、
//               数据访问器、_init_ 重载，以及接口的抽象字段访问器
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    public class BindExpandManager
    {
        public static BindExpandManager instance => m_Instance ??= new BindExpandManager();
        private static BindExpandManager m_Instance;

        private const string kSyntheticClassNamePrefix = "__SLBindExpand_";
        private const int kMaxExtendDepth = 32;

        private int m_SyntheticClassId = 0;
        private Dictionary<string, FileMetaClass> m_DataDefineDict = new Dictionary<string, FileMetaClass>();

        // =========================================================================
        // 入口
        // =========================================================================
        public void ExpandAll( List<FileParse> fileParseList )
        {
            // Pass 1: 收集全工程 isData 的 FileMetaClass 按简单名建 dict
            CollectDataDefines( fileParseList );

            // Pass 2: 对每个文件的 allClassList 快照逐类展开
            foreach ( var fp in fileParseList )
            {
                var fm = fp.file;
                // 快照防止合成类污染迭代
                var snapshot = new List<FileMetaClass>( fm.fileMetaAllClassList );
                foreach ( var fmc in snapshot )
                {
                    if ( fmc.name.StartsWith( kSyntheticClassNamePrefix ) ) continue;
                    if ( fmc.isData || fmc.isEnum ) continue;
                    if ( fmc.bindClassList == null || fmc.bindClassList.Count == 0 ) continue;

                    if ( fmc.preInterfaceToken != null )
                        ExpandInterface( fm, fmc );
                    else
                        ExpandClass( fm, fmc );
                }
            }
        }

        // =========================================================================
        // Pass 1: 收集 data 定义
        // =========================================================================
        private void CollectDataDefines( List<FileParse> fileParseList )
        {
            foreach ( var fp in fileParseList )
            {
                foreach ( var fmc in fp.file.fileMetaAllClassList )
                {
                    if ( fmc.isData && !m_DataDefineDict.ContainsKey( fmc.name ) )
                    {
                        m_DataDefineDict[fmc.name] = fmc;
                    }
                }
            }
        }

        // =========================================================================
        // 数据结构
        // =========================================================================
        private class BindDataInfo
        {
            public string name;          // 简单名 (e.g. "BookData")
            public string allName;       // 全名 (e.g. "A.B.BookData")
            public FileMetaClass dataClass;
            public List<FieldInfo> fields = new List<FieldInfo>();
        }

        private struct FieldInfo
        {
            public string name;
            public string sourceType;  // 源码类型关键字 (e.g. "int", "string")
        }

        // =========================================================================
        // bind 解析
        // =========================================================================
        private BindDataInfo ResolveBind( FileMeta fm, FileMetaClassDefine define )
        {
            string simpleName = define.name;
            // 先在目标文件查找
            var dataClass = fm.GetFileMetaClassByName( simpleName );
            if ( dataClass == null || !dataClass.isData )
            {
                // 退全局 dict
                m_DataDefineDict.TryGetValue( simpleName, out dataClass );
            }
            if ( dataClass == null || !dataClass.isData )
            {
                Log.AddFileMetaLog( LID.ShowExtendMessage, define.classNameToken ?? fm.token,
                    $"Error bind 引用的数据类型 '{simpleName}' 未找到或不是 data 类型!" );
                return null;
            }

            var info = new BindDataInfo
            {
                name = simpleName,
                allName = define.allName,
                dataClass = dataClass
            };

            // 收集 ConstValue 字段
            foreach ( var fmmd in dataClass.memberDataList )
            {
                if ( fmmd.isWithName && fmmd.DataType == FileMetaMemberData.EMemberDataType.ConstValue )
                {
                    string fieldType = GetSourceTypeString( fmmd.fileMetaConstValue?.token );
                    if ( fieldType != null )
                    {
                        info.fields.Add( new FieldInfo { name = fmmd.name, sourceType = fieldType } );
                    }
                }
            }
            return info;
        }

        /// <summary>
        /// 将 Token 的 EType 映射回源码类型关键字
        /// </summary>
        private string GetSourceTypeString( Token valueToken )
        {
            if ( valueToken == null ) return null;
            var etype = valueToken.GetEType();
            switch ( etype )
            {
                case EType.Int32: return "int";
                case EType.Boolean: return "bool";
                case EType.Int64: return "long";
                case EType.Float32: return "float";
                case EType.Float64: return "double";
                case EType.String: return "string";
                case EType.Int16: return "short";
                case EType.UInt16: return "ushort";
                case EType.UInt32: return "uint";
                case EType.UInt64: return "ulong";
                case EType.Float16: return "half";
                case EType.Int8: return "sbyte";
                case EType.UInt8: return "byte";
                default: return null; // 非简单类型，跳过
            }
        }

        // =========================================================================
        // 类展开
        // =========================================================================
        private void ExpandClass( FileMeta fm, FileMetaClass fmc )
        {
            // 解析自身 binds
            var ownBinds = new List<BindDataInfo>();
            foreach ( var define in fmc.bindClassList )
            {
                var info = ResolveBind( fm, define );
                if ( info != null ) ownBinds.Add( info );
            }
            if ( ownBinds.Count == 0 ) return;

            // 收集祖先 binds
            var ancestorBinds = new List<BindDataInfo>();
            CollectAncestorBinds( fm, fmc, ancestorBinds, new HashSet<string>(), 0 );

            // allBinds = 祖先 + 自身（去重）
            var ancestorNames = new HashSet<string>();
            foreach ( var b in ancestorBinds ) ancestorNames.Add( b.name );

            var allBinds = new List<BindDataInfo>( ancestorBinds );
            foreach ( var info in ownBinds )
            {
                if ( !ancestorNames.Contains( info.name ) )
                    allBinds.Add( info );
            }

            // 收集接口要求的字段名（用于决定是否加 override）
            var interfaceFields = new HashSet<string>();
            CollectInterfaceFields( fm, fmc, interfaceFields, new HashSet<string>(), 0 );

            // 接口约束检查
            CheckInterfaceConstraints( fm, fmc, allBinds );

            // 构建字段冲突表
            var fieldToBinds = new Dictionary<string, List<BindDataInfo>>();
            foreach ( var info in ownBinds )
            {
                foreach ( var field in info.fields )
                {
                    if ( !fieldToBinds.ContainsKey( field.name ) )
                        fieldToBinds[field.name] = new List<BindDataInfo>();
                    fieldToBinds[field.name].Add( info );
                }
            }

            // 生成合成源码
            var sb = new StringBuilder();
            string syntheticName = kSyntheticClassNamePrefix + ( m_SyntheticClassId++ ) + "__";
            sb.AppendLine( syntheticName );
            sb.AppendLine( "{" );

            // 1. 成员变量（自身 binds，不在祖先中）
            foreach ( var info in ownBinds )
            {
                if ( ancestorNames.Contains( info.name ) ) continue;
                if ( HasMemberVariable( fmc, info.name ) || HasMemberFunction( fmc, info.name ) )
                {
                    Log.AddFileMetaLog( LID.ShowExtendMessage, fmc.token,
                        $"Warning 类 '{fmc.name}' 已有成员 '{info.name}'，跳过 bind 成员变量注入。" );
                    continue;
                }
                sb.AppendLine( $"    {info.allName} {info.name} = new();" );
            }

            // 2. 字段访问器 get/set
            foreach ( var info in ownBinds )
            {
                foreach ( var field in info.fields )
                {
                    // 类已有同名函数 -> 静默跳过（手动实现）
                    if ( HasMemberFunction( fmc, field.name ) )
                        continue;

                    // 类已有同名成员变量 -> 告警跳过
                    if ( HasMemberVariable( fmc, field.name ) )
                    {
                        Log.AddFileMetaLog( LID.ShowExtendMessage, fmc.token,
                            $"Warning 类 '{fmc.name}' 已有成员变量 '{field.name}'，跳过 bind 字段访问器。" );
                        continue;
                    }

                    // 多 bind 同名字段冲突 -> 告警跳过
                    if ( fieldToBinds[field.name].Count > 1 )
                    {
                        Log.AddFileMetaLog( LID.ShowExtendMessage, fmc.token,
                            $"Warning bind 数据中字段 '{field.name}' 存在冲突，请手动实现 get/set。" );
                        continue;
                    }

                    bool needOverride = interfaceFields.Contains( field.name );
                    string prefix = needOverride ? "override " : "";
                    string paramName = MakeParamName( field.name );

                    sb.AppendLine( $"    {prefix}{field.sourceType} get {field.name}() {{ ret this.{info.name}.{field.name} }}" );
                    sb.AppendLine( $"    {prefix}set {field.name}( {field.sourceType} {paramName} ) {{ this.{info.name}.{field.name} = {paramName} }}" );
                }
            }

            // 3. 数据访问器
            foreach ( var info in ownBinds )
            {
                if ( ancestorNames.Contains( info.name ) ) continue;
                string accessorName = info.name + "Data";
                if ( HasMemberFunction( fmc, accessorName ) )
                    continue;
                string paramName = MakeParamName( info.name );
                sb.AppendLine( $"    {info.allName} get {accessorName}() {{ ret this.{info.name} }}" );
                sb.AppendLine( $"    set {accessorName}( {info.allName} {paramName} ) {{ this.{info.name} = {paramName} }}" );
            }

            // 4. _init_ 前缀重载
            for ( int k = 1; k <= allBinds.Count; k++ )
            {
                if ( HasInitWithParamCount( fmc, k ) )
                    continue;

                sb.Append( "    _init_( " );
                var bodyParts = new List<string>();
                for ( int i = 0; i < k; i++ )
                {
                    var bind = allBinds[i];
                    string paramName = MakeParamName( bind.name );
                    sb.Append( $"{bind.allName} {paramName}" );
                    if ( i < k - 1 ) sb.Append( ", " );
                    bodyParts.Add( $"this.{bind.name} = {paramName}" );
                }
                sb.Append( " ) { " );
                sb.Append( string.Join( "\n    ", bodyParts ) );
                sb.AppendLine( " }" );
            }

            sb.AppendLine( "}" );

            // 解析注入
            InjectSyntheticMembers( fm, fmc, sb.ToString(), syntheticName );
        }

        // =========================================================================
        // 接口展开
        // =========================================================================
        private void ExpandInterface( FileMeta fm, FileMetaClass fmc )
        {
            var ownBinds = new List<BindDataInfo>();
            foreach ( var define in fmc.bindClassList )
            {
                var info = ResolveBind( fm, define );
                if ( info != null ) ownBinds.Add( info );
            }
            if ( ownBinds.Count == 0 ) return;

            // 字段冲突表
            var fieldToBinds = new Dictionary<string, List<BindDataInfo>>();
            foreach ( var info in ownBinds )
            {
                foreach ( var field in info.fields )
                {
                    if ( !fieldToBinds.ContainsKey( field.name ) )
                        fieldToBinds[field.name] = new List<BindDataInfo>();
                    fieldToBinds[field.name].Add( info );
                }
            }

            var sb = new StringBuilder();
            string syntheticName = kSyntheticClassNamePrefix + ( m_SyntheticClassId++ ) + "__";
            sb.AppendLine( syntheticName );
            sb.AppendLine( "{" );

            foreach ( var info in ownBinds )
            {
                foreach ( var field in info.fields )
                {
                    // 接口已有同名函数 -> 跳过
                    if ( HasMemberFunction( fmc, field.name ) )
                        continue;

                    // 冲突 -> 告警跳过
                    if ( fieldToBinds[field.name].Count > 1 )
                    {
                        Log.AddFileMetaLog( LID.ShowExtendMessage, fmc.token,
                            $"Warning 接口 '{fmc.name}' bind 字段 '{field.name}' 冲突，请手动声明。" );
                        continue;
                    }

                    // 抽象 getter（带返回类型，无体）
                    sb.AppendLine( $"    {field.sourceType} get {field.name}();" );
                    // 抽象 setter（无返回类型，无体）
                    sb.AppendLine( $"    set {field.name}( {field.sourceType} _{field.name} );" );
                }
            }

            sb.AppendLine( "}" );

            InjectSyntheticMembers( fm, fmc, sb.ToString(), syntheticName );
        }

        // =========================================================================
        // 祖先 binds 收集
        // =========================================================================
        private void CollectAncestorBinds( FileMeta fm, FileMetaClass fmc,
            List<BindDataInfo> result, HashSet<string> visited, int depth )
        {
            if ( depth > kMaxExtendDepth ) return;
            if ( fmc.fileMetaExtendClass == null ) return;

            string parentName = fmc.fileMetaExtendClass.name;
            if ( visited.Contains( parentName ) ) return;
            visited.Add( parentName );

            var parentClass = fm.GetFileMetaClassByName( parentName );
            if ( parentClass == null ) return;
            if ( parentClass.isData || parentClass.isEnum ) return;

            // 祖先的祖先先收集（递归）
            CollectAncestorBinds( fm, parentClass, result, visited, depth + 1 );

            // 祖先自身的 binds
            foreach ( var define in parentClass.bindClassList )
            {
                var info = ResolveBind( fm, define );
                if ( info != null )
                {
                    // 去重：只保留首次出现的
                    if ( !result.Exists( r => r.name == info.name ) )
                        result.Add( info );
                }
            }
        }

        // =========================================================================
        // 接口闭包字段收集
        // =========================================================================
        private void CollectInterfaceFields( FileMeta fm, FileMetaClass fmc,
            HashSet<string> result, HashSet<string> visited, int depth )
        {
            if ( depth > kMaxExtendDepth ) return;
            if ( fmc.interfaceClassList == null ) return;

            foreach ( var ifaceDefine in fmc.interfaceClassList )
            {
                string ifaceName = ifaceDefine.name;
                if ( visited.Contains( ifaceName ) ) continue;
                visited.Add( ifaceName );

                var ifaceClass = fm.GetFileMetaClassByName( ifaceName );
                if ( ifaceClass == null ) continue;
                if ( ifaceClass.preInterfaceToken == null ) continue; // 不是接口

                // 接口 bind 的每个 data 的字段
                foreach ( var define in ifaceClass.bindClassList )
                {
                    var info = ResolveBind( fm, define );
                    if ( info == null ) continue;
                    foreach ( var field in info.fields )
                        result.Add( field.name );
                }

                // 递归接口的 extends 链
                CollectInterfaceFields( fm, ifaceClass, result, visited, depth + 1 );
            }
        }

        // =========================================================================
        // 接口约束检查
        // =========================================================================
        private void CheckInterfaceConstraints( FileMeta fm, FileMetaClass fmc, List<BindDataInfo> allBinds )
        {
            if ( fmc.interfaceClassList == null ) return;

            var allBindNames = new HashSet<string>();
            foreach ( var b in allBinds ) allBindNames.Add( b.name );

            var visited = new HashSet<string>();
            foreach ( var ifaceDefine in fmc.interfaceClassList )
            {
                string ifaceName = ifaceDefine.name;
                if ( visited.Contains( ifaceName ) ) continue;
                visited.Add( ifaceName );

                var ifaceClass = fm.GetFileMetaClassByName( ifaceName );
                if ( ifaceClass == null ) continue;
                if ( ifaceClass.preInterfaceToken == null ) continue;
                if ( ifaceClass.bindClassList == null || ifaceClass.bindClassList.Count == 0 ) continue;

                // 接口 bind 的每个 data 名必须在类 allBinds 中
                foreach ( var define in ifaceClass.bindClassList )
                {
                    if ( !allBindNames.Contains( define.name ) )
                    {
                        Log.AddFileMetaLog( LID.ShowExtendMessage, fmc.token,
                            $"Error 类 '{fmc.name}' 实现接口 '{ifaceName}' bind 了数据 '{define.name}'，但类未 bind 该数据!" );
                    }
                }
            }
        }

        // =========================================================================
        // 合成源码注入
        // =========================================================================
        private void InjectSyntheticMembers( FileMeta fm, FileMetaClass targetClass,
            string sourceCode, string syntheticName )
        {
            try
            {
                // 用目标文件路径做 LexerParse 的 path（错误定位指向真实文件）
                var lexer = new LexerParse( fm.path, sourceCode.ToCharArray() );
                lexer.ParseToTokenList();

                var tokenParse = new TokenParse( fm, lexer.listTokens );
                tokenParse.BuildStruct();

                var structParse = new StructParse( fm, tokenParse.rootNode );
                structParse.ParseRootNodeToFileMeta();

                // 查找合成类
                var syntheticClass = fm.GetFileMetaClassByName( syntheticName );
                if ( syntheticClass == null )
                {
                    Log.AddFileMetaLog( LID.ShowExtendMessage, fm.token,
                        $"Error bind 展开失败：无法找到合成类 '{syntheticName}'。" );
                    return;
                }

                // 转移成员变量
                var variables = new List<FileMetaMemberVariable>( syntheticClass.memberVariableList );
                foreach ( var fmv in variables )
                {
                    targetClass.AddFileMemberVariable( fmv );
                }

                // 转移成员函数
                var functions = new List<FileMetaMemberFunction>( syntheticClass.memberFunctionList );
                foreach ( var fmmf in functions )
                {
                    targetClass.AddFileMemberFunction( fmmf );
                }

                // 从两个列表中移除合成类
                fm.RemoveFileMetaClass( syntheticClass );
            }
            catch ( Exception e )
            {
                Log.AddFileMetaLog( LID.ShowExtendMessage, fm.token,
                    $"Error bind 展开异常 (类 '{targetClass.name}'): {e.Message}" );
            }
        }

        // =========================================================================
        // 工具方法
        // =========================================================================
        private bool HasMemberVariable( FileMetaClass fmc, string name )
        {
            return fmc.memberVariableList.Find( v => v.name == name ) != null;
        }

        private bool HasMemberFunction( FileMetaClass fmc, string name )
        {
            return fmc.memberFunctionList.Find( f => f.name == name ) != null;
        }

        private bool HasInitWithParamCount( FileMetaClass fmc, int paramCount )
        {
            foreach ( var fmmf in fmc.memberFunctionList )
            {
                if ( fmmf.name == "_init_" && fmmf.metaParamtersList != null
                    && fmmf.metaParamtersList.Count == paramCount )
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 生成参数名：_ + 首字母小写 + 剩余部分
        /// </summary>
        private string MakeParamName( string name )
        {
            if ( string.IsNullOrEmpty( name ) ) return "_";
            return "_" + char.ToLower( name[0] ) + ( name.Length > 1 ? name.Substring( 1 ) : "" );
        }
    }
}
