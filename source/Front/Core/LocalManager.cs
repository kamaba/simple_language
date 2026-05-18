//****************************************************************************
//  File:      LocalManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/2/28 12:00:00
//  Description:  file-level local{} manager
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using SimpleLanguage.Project;
using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleLanguage.Core
{
    public sealed class LocalManager
    {
        public static readonly LocalManager instance = new LocalManager();

        private readonly Dictionary<string, MetaClass> m_FileLocalClassDict = new Dictionary<string, MetaClass>();
        private readonly Dictionary<string, MetaMemberFunction> m_FileInitFunctionDict = new Dictionary<string, MetaMemberFunction>();
        private readonly Dictionary<string, MetaVariable> m_FileLocalInstanceVarDict = new Dictionary<string, MetaVariable>();

        public MetaClass GetFileLocalClass(FileMeta fm)
        {
            if (fm == null) return null;
            m_FileLocalClassDict.TryGetValue(fm.path, out var mc);
            return mc;
        }

        public MetaMemberFunction GetFileLocalInitFunction(FileMeta fm)
        {
            if (fm == null) return null;
            m_FileInitFunctionDict.TryGetValue(fm.path, out var fn);
            return fn;
        }

        public MetaVariable GetFileLocalInstanceVariable(FileMeta fm)
        {
            if (fm == null) return null;
            m_FileLocalInstanceVarDict.TryGetValue(fm.path, out var v);
            return v;
        }

        public void BuildFileLocalClasses(List<FileParse> fileParses)
        {
            if (fileParses == null) return;

            for (int i = 0; i < fileParses.Count; i++)
            {
                var fp = fileParses[i];
                var fm = fp?.file;
                if (fm == null) continue;

                var localSyntax = fm.GetFileMetaLocalSyntax();
                if (localSyntax == null) continue;

                if (m_FileLocalClassDict.ContainsKey(fm.path))
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(fm.path);
                if (string.IsNullOrEmpty(fileName))
                    fileName = "File" + fm.GetHashCode();

                var localClassName = fileName + "_Local";

                // Attach generated local class to module root (no namespace), file-private usage is enforced at call-site.
                var root = ModuleManager.instance.selfModule.metaNode;
                var existNode = root.GetChildrenMetaNodeByName(localClassName);
                if (existNode != null && existNode.IsMetaClass())
                {
                    Log.AddMetaCoreLog(LID.AutoLocalManagerL75, "Error local{} 生成的类名发生冲突: " + localClassName);
                    continue;
                }

                var localMc = new MetaClass(localClassName);
                root.AddMetaClass(localMc);
                localMc.SetClassDefineType(EClassDefineType.CodeDefine);

                var instVar = CreateOrGetFileLocalInstanceVariable(fm, localMc);

                // add local-defined functions as instance member functions
                if (localSyntax.functionList != null)
                {
                    for (int j = 0; j < localSyntax.functionList.Count; j++)
                    {
                        var fmmf = localSyntax.functionList[j];
                        if (fmmf == null) continue;
                        if (fmmf.staticToken != null)
                        {
                            Log.AddMetaCoreLog(LID.AutoLocalManagerL94, "Error local{} 中定义的函数不允许使用 static");
                            continue;
                        }
                        var mmf = new MetaMemberFunction(localMc, fmmf);
                        localMc.AddMetaMemberFunction(mmf);
                    }
                }

                // create init function on local class (statements only)
                var initFun = CreateLocalInitFunction(localMc, localSyntax);
                if (initFun != null)
                {
                    // ensure meta statements are created so we can discover variable definitions
                    initFun.ParseStatements();
                    RegisterLocalInitDefinedMemberVariables(localMc, initFun);
                }

                m_FileLocalClassDict.Add(fm.path, localMc);
                if (initFun != null)
                    m_FileInitFunctionDict.Add(fm.path, initFun);

                if (instVar != null)
                    m_FileLocalInstanceVarDict.Add(fm.path, instVar);

                ClassManager.instance.AddInitHandleMetaClassList(localMc);
            }
        }

        private MetaVariable CreateOrGetFileLocalInstanceVariable(FileMeta fm, MetaClass localMc)
        {
            if (fm == null || localMc == null) return null;

            // Store local instance in globalData as: local_<FileHash>
            var global = ProjectManager.globalData;
            if (global == null) return null;

            var varName = "local_" + fm.path.GetHashCode();
            var exist = global.GetMetaMemberVariableByName(varName);
            if (exist != null)
                return exist;

            var mv = new MetaMemberVariable(global, varName);
            mv.SetMetaDefineType(new MetaType(localMc));
            mv.SetRealMetaType(new MetaType(localMc));
            mv.SetIsStatic(true);
            global.AddMetaMemberVariable(mv);
            return global.GetMetaMemberVariableByName(varName);
        }

        private MetaMemberFunction CreateLocalInitFunction(MetaClass localMc, FileMetaLocalSyntax localSyntax)
        {
            if (localMc == null || localSyntax == null) return null;

            // Build a synthetic member function that holds the local{} statements.
            // We reuse MetaMemberFunction's ability to translate a FileMetaBlockSyntax into MetaStatements.

            var fn = new MetaMemberFunction(localMc, "__local_init__");

            // mark as static: there isn't a setter, so keep it non-static but we will invoke it via instance anyway.
            // local{} semantics in this phase: new LocalClass(); instance.__local_init__();

            var block = localSyntax.blockSyntax;
            fn.metaBlockStatements.SetFileMetaBlockSyntax(block);
            MetaMemberFunction.CreateMetaSyntax(block, fn.metaBlockStatements);

            localMc.AddMetaMemberFunction(fn);
            return fn;
        }

        private void RegisterLocalInitDefinedMemberVariables(MetaClass localMc, MetaMemberFunction initFun)
        {
            if (localMc == null || initFun == null) return;
            var mbs = initFun.metaBlockStatements;
            if (mbs == null) return;

            // local{} init statements run on the local instance; variables defined there should be treated as member variables
            // of the generated <FileName>_Local class so `local.xxx` resolves like normal instance member access.
            for (MetaStatements cur = mbs.nextMetaStatements; cur != null; cur = cur.nextMetaStatements)
            {
                if (cur is not MetaDefineVarStatements mdvs)
                    continue;

                var mv = mdvs.defineVarMetaVariable;
                if (mv == null) continue;
                var name = mv.name;
                if (string.IsNullOrEmpty(name)) continue;

                if (localMc.GetMetaMemberVariableByName(name) != null)
                    continue;

                var mmv = new MetaMemberVariable(localMc, name);
                mmv.SetIsStatic(false);
                mmv.SetMetaDefineType(mv.defineMetaType);
                mmv.SetRealMetaType(mv.realMetaType);
                localMc.AddMetaMemberVariable(mmv, false);
                MetaVariableManager.instance.AddMetaMemberVariable(mmv);
            }
        }

        public void InjectLocalInitCalls(List<FileParse> fileParses)
        {
            if (fileParses == null) return;

            // Ensure local init methods have meta syntax created -> IR.
            for (int i = 0; i < fileParses.Count; i++)
            {
                var fm = fileParses[i]?.file;
                if (fm == null) continue;
                var init = GetFileLocalInitFunction(fm);
                if (init == null) continue;
                init.ParseStatements();
            }

            // Inject init calls into Project._main_() if present.
            var project = ClassManager.instance.TryGetProjectMetaClass();
            if (project == null) return;
            var main = project.GetFirstMetaMemberFunctionByName("_main_");
            if (main == null) return;

            var mbs = main.metaBlockStatements;
            if (mbs == null) return;

            // We insert at the front; to keep compile file order execution, iterate reverse.
            for (int i = fileParses.Count - 1; i >= 0; i--)
            {
                var fm = fileParses[i]?.file;
                if (fm == null) continue;

                var localMc = GetFileLocalClass(fm);
                var instVar = GetFileLocalInstanceVariable(fm);
                if (localMc == null || instVar == null) continue;

                // 1) local_xxx = <FileName>_Local()
                var baseToken = new Token(fm.path, ETokenType.Identifier, instVar.name, 0, 0);
                var callNode = new Node(baseToken) { nodeType = ENodeType.IdentifierLink };
                var link = new FileMetaCallLink(fm, callNode, true);
                var callSyntax = new FileMetaCallSyntax(link);
                var defineVar = new MetaDefineVarStatements(mbs, callSyntax);
                mbs.AddFrontToEndStatements(defineVar);

                // 2) local_xxx.__local_init__()
                // create identifier chain: local_xxx.__local_init__
                var initToken = new Token(fm.path, ETokenType.Identifier, "__local_init__", 0, 0);
                var initNode = new Node(initToken);
                initNode.nodeType = ENodeType.IdentifierLink;
                // attach to as extend link, emulating `local_xxx.__local_init__`
                callNode.AddLinkNode(new Node(new Token(fm.path, ETokenType.Period, ".", 0, 0)) { nodeType = ENodeType.Period });
                callNode.AddLinkNode(initNode);

                var link2 = new FileMetaCallLink(fm, callNode, true);
                var call2 = new FileMetaCallSyntax(link2);
                var callStmt = new MetaCallStatements(mbs, call2);
                mbs.AddFrontStatements(callStmt);
            }
        }
    }
}
