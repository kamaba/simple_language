//****************************************************************************
//  File:      FileMetaTypeAliasDecl.cs
//****************************************************************************

namespace SimpleLanguage.Compile
{
    /// <summary>
    /// 编译期 typealias 声明（解析后、MetaType 解析前）。
    /// </summary>
    public sealed class FileMetaTypeAliasDecl
    {
        public FileMetaTypeAliasDecl(string aliasName, FileMetaClassDefine targetDefine, bool isProjectScope)
        {
            AliasName = aliasName;
            TargetDefine = targetDefine;
            IsProjectScope = isProjectScope;
        }

        public string AliasName { get; }
        public FileMetaClassDefine TargetDefine { get; }
        public bool IsProjectScope { get; }
    }
}
