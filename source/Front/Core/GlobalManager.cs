//****************************************************************************
//  File:      GlobalManager.cs
// ------------------------------------------------
//  Description:  file-level global{} manager
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleLanguage.Core
{
    public sealed class GlobalManager
    {
        public static readonly GlobalManager instance = new GlobalManager();

        private MetaClass m_GlobalClass;
        private MetaMemberFunction m_GlobalInitFunction;
        private MetaMemberVariable m_GlobalInstanceVariable;
        private FileMeta m_GlobalBindFileMeta;

        public MetaMemberVariable GetGlobalInstanceVariable()
        {
            return m_GlobalInstanceVariable;
        }

        public void BuildGlobalClass(List<FileParse> fileParses)
        {
            if (fileParses == null) return;
            if (m_GlobalClass != null) return;

            var globalSyntaxList = new List<FileMetaGlobalOrLocalSyntax>();
            for (int i = 0; i < fileParses.Count; i++)
            {
                var fm = fileParses[i]?.file;
                if (fm == null) continue;
                if (!fm.path.EndsWith(".sp", StringComparison.OrdinalIgnoreCase)) continue;

                var gs = fm.GetFileMetaGlobalSyntax();
                if (gs != null)
                {
                    if (m_GlobalBindFileMeta == null) m_GlobalBindFileMeta = fm;
                    globalSyntaxList.Add(gs);
                }
            }

            if (globalSyntaxList.Count == 0) return;

            var root = ModuleManager.instance.selfModule.metaNode;
            const string globalClassName = "__Global_Runtime";
            var existNode = root.GetChildrenMetaNodeByName(globalClassName);
            if (existNode != null && existNode.IsMetaClass())
            {
                m_GlobalClass = existNode.GetMetaClassByTemplateCount(0);
            }
            else
            {
                m_GlobalClass = new MetaClass(globalClassName);
                root.AddMetaClass(m_GlobalClass);
                m_GlobalClass.SetClassDefineType(EClassDefineType.CodeDefine);
                ClassManager.instance.AddInitHandleMetaClassList(m_GlobalClass);
            }

            m_GlobalInstanceVariable = CreateOrGetGlobalInstanceVariable(m_GlobalClass);

            for (int i = 0; i < globalSyntaxList.Count; i++)
            {
                var gs = globalSyntaxList[i];
                var fnList = gs.functionList;
                for (int j = 0; j < fnList.Count; j++)
                {
                    var fmmf = fnList[j];
                    if (fmmf == null) continue;
                    if (fmmf.staticToken != null) continue;
                    var mmf = new MetaMemberFunction(m_GlobalClass, fmmf);
                    m_GlobalClass.AddMetaMemberFunction(mmf);
                }
            }

            m_GlobalInitFunction = new MetaMemberFunction(m_GlobalClass, "__global_init__");
            m_GlobalClass.AddMetaMemberFunction(m_GlobalInitFunction);

            for (int i = 0; i < globalSyntaxList.Count; i++)
            {
                var block = globalSyntaxList[i].blockSyntax;
                if (block == null) continue;
                MetaMemberFunction.CreateMetaSyntax(block, m_GlobalInitFunction.metaBlockStatements);
            }
        }

        private MetaMemberVariable CreateOrGetGlobalInstanceVariable(MetaClass globalMc)
        {
            if (globalMc == null) return null;

            var globalData = ProjectManager.globalData;
            if (globalData == null) return null;

            const string varName = "global";
            var exist = globalData.GetMetaMemberVariableByName(varName);
            if (exist is MetaMemberVariable emv)
                return emv;

            var mv = new MetaMemberVariable(globalData, varName);
            mv.SetMetaDefineType(new MetaType(globalMc));
            mv.SetRealMetaType(new MetaType(globalMc));
            mv.SetIsStatic(true);
            globalData.AddMetaMemberVariable(mv, false);
            return mv;
        }

        public void InjectGlobalInitCall()
        {
            if (m_GlobalClass == null || m_GlobalInitFunction == null || m_GlobalInstanceVariable == null)
                return;

            m_GlobalInitFunction.ParseStatements();

            var project = ClassManager.instance.GetClassByName("S.Project", 0) ?? ClassManager.instance.GetClassByName("Core.Project", 0);
            if (project == null) return;
            var main = project.GetFirstMetaMemberFunctionByName("Main");
            if (main == null) return;

            var mbs = main.metaBlockStatements;
            if (mbs == null) return;

            // global = __Global_Runtime()
            var fmPath = string.Empty;
            var fm = m_GlobalBindFileMeta;
            if (fm != null) fmPath = fm.path;
            var baseToken = new Token(fmPath, ETokenType.Identifier, m_GlobalInstanceVariable.name, 0, 0);
            var callNode = new Node(baseToken) { nodeType = ENodeType.IdentifierLink };
            var link = new FileMetaCallLink(fm, callNode, true);
            var callSyntax = new FileMetaCallSyntax(link);
            var defineVar = new MetaDefineVarStatements(mbs, callSyntax);
            mbs.AddFrontToEndStatements(defineVar);

            // global.__global_init__()
            var initToken = new Token(fmPath, ETokenType.Identifier, "__global_init__", 0, 0);
            var initNode = new Node(initToken) { nodeType = ENodeType.IdentifierLink };
            callNode.AddLinkNode(new Node(new Token(fmPath, ETokenType.Period, ".", 0, 0)) { nodeType = ENodeType.Period });
            callNode.AddLinkNode(initNode);
            var link2 = new FileMetaCallLink(fm, callNode, true);
            var call2 = new FileMetaCallSyntax(link2);
            var callStmt = new MetaCallStatements(mbs, call2);
            mbs.AddFrontStatements(callStmt);
        }
    }
}
