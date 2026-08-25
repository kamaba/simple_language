//****************************************************************************
//  File:      CompilePhaseDefine.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/08/25 12:00:00
//  Description: Front 编译过程的阶段/策略/状态枚举定义
//****************************************************************************

namespace SimpleLanguage.Compile.Process
{
    /// <summary>
    /// 编译过程的五个大阶段，顺序即执行顺序：
    /// RefModule(读取外部引用模块) -> File(文件编译) -> MetaCore(逻辑整合) -> IR(IR编译) -> Export(导出)
    /// </summary>
    public enum ECompilePhase
    {
        None = 0,

        /// <summary>读取外部引入的 RefModule。错误只提示，不影响后续编译。</summary>
        RefModule,

        /// <summary>文件编译阶段：读取文件 -> Token -> Node -> FileMeta。单文件错误只影响当前文件，不影响其它文件。</summary>
        File,

        /// <summary>MetaCore 阶段：全工程(含 RefModule)的 class/data/enum 关系与方法逻辑整合。出现错误则中断，不进入 IR 阶段。</summary>
        MetaCore,

        /// <summary>IR 阶段：编译成 IR 逻辑。错误影响本阶段并阻止导出。</summary>
        IR,

        /// <summary>导出阶段：对 IR 逻辑进行 Module 导出。</summary>
        Export,
    }

    /// <summary>大阶段的错误处理策略</summary>
    public enum EPhaseErrorPolicy
    {
        /// <summary>错误仅提示，不阻止后续阶段（RefModule 阶段）。</summary>
        NotifyOnly,

        /// <summary>编译单元(文件)错误只影响当前单元，不影响其它单元；阶段内有错误则不进入下一阶段（File 阶段）。</summary>
        ContinueUnit,

        /// <summary>错误中断本阶段并阻止后续阶段（MetaCore / IR / Export 阶段）。</summary>
        AbortPhase,
    }

    /// <summary>阶段/步骤/文件单元的运行状态</summary>
    public enum EProcessState
    {
        /// <summary>待执行</summary>
        Pending,

        /// <summary>执行中</summary>
        Running,

        /// <summary>成功完成</summary>
        Completed,

        /// <summary>失败</summary>
        Failed,

        /// <summary>被跳过（前置阶段/步骤失败，或单元已失败）</summary>
        Skipped,
    }
}
