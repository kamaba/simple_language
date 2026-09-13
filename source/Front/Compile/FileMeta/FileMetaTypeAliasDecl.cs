//****************************************************************************
//  File:      FileMetaTypeAliasDecl.cs
//****************************************************************************

using System.Collections.Generic;

namespace SimpleLanguage.Compile
{
    /// <summary>
    /// 编译期 typealias 声明（解析后、MetaType 解析前）。
    /// 支持两种形式:
    ///   1. 普通类型别名: typealias Name = TargetType;
    ///   2. 函数类型别名: typealias Name = ReturnType Function( ParamType, ... );
    /// </summary>
    public sealed class FileMetaTypeAliasDecl
    {
        public FileMetaTypeAliasDecl(string aliasName, FileMetaClassDefine targetDefine, bool isProjectScope)
        {
            AliasName = aliasName;
            TargetDefine = targetDefine;
            IsProjectScope = isProjectScope;
            IsFunctionType = false;
        }

        public FileMetaTypeAliasDecl(string aliasName, bool isProjectScope,
            FileMetaClassDefine returnTypeDefine, List<FileMetaClassDefine> paramTypeDefineList)
        {
            AliasName = aliasName;
            IsProjectScope = isProjectScope;
            IsFunctionType = true;
            FunctionReturnTypeDefine = returnTypeDefine;
            FunctionParamTypeDefineList = paramTypeDefineList ?? new List<FileMetaClassDefine>();
        }

        public string AliasName { get; }
        public FileMetaClassDefine TargetDefine { get; }
        public bool IsProjectScope { get; }

        public bool IsFunctionType { get; }
        public FileMetaClassDefine FunctionReturnTypeDefine { get; }
        public List<FileMetaClassDefine> FunctionParamTypeDefineList { get; }
    }
}
