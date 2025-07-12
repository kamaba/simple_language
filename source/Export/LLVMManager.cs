//****************************************************************************
//  File:      LLVMManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description:  manager Export other lanuage or il etc.
//****************************************************************************

using SimpleLanguage.Project;
using System.IO;
using Swigged.LLVM;
using System.Reflection;
using System;

namespace SimpleLanguage.ExportLanguage
{
    public class LLVMManager
    {

        public void CreateModule()
        {
            LLVM.InitializeAllTargets();
            LLVM.InitializeAllTargetMCs();
            LLVM.InitializeAllTargetInfos();
            LLVM.InitializeAllAsmPrinters();

        }
    }
}