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
        private readonly Dictionary<string, MetaMemberVariable> m_FileLocalInstanceVarDict = new Dictionary<string, MetaMemberVariable>();

        /// <summary>
        /// Returns the generated class name for a file's local{} block, e.g. "LocalTest1_Local".
        /// Used by MetaCallNode to resolve `local.xxx` -> `LocalTest1_Local.instance.xxx`.
        /// </summary>
        public static string GetFileLocalClassName(FileMeta fm)
        {
            if (fm == null) return null;
            var fileName = Path.GetFileNameWithoutExtension(fm.path);
            if (string.IsNullOrEmpty(fileName))
                fileName = "File" + fm.GetHashCode();
            return  "__" + fileName + "_Local__";
        }

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

        public MetaMemberVariable GetFileLocalInstanceVariable(FileMeta fm)
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

                var localClassName = GetFileLocalClassName(fm);

                // Attach generated local class to module root (no namespace)
                var root = ModuleManager.instance.selfModule.metaNode;
                var existNode = root.GetChildrenMetaNodeByName(localClassName);
                if (existNode != null && existNode.IsMetaClass())
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error local{} generated class name conflict: " + localClassName);
                    continue;
                }

                var localMc = new MetaClass(localClassName);
                root.AddMetaClass(localMc);
                // 动态创建的 local 类必须加入导出列表，
                // 否则 IR 翻译阶段（ParseClass 遍历 exportMetaClassList）不会为其创建 IRMetaClass，
                // 导致 __local_init__ 等虚调用在 IR 阶段找不到方法。
                ClassManager.instance.AddExportMetaClass(localMc);

                // Create static `instance` member on the _Local class itself.
                // Initialized via MetaNewObjectExpressNode so the instance is created
                // during static initialization (before _main_ runs).
                var instVar = CreateOrGetFileLocalInstanceVariable(fm, localMc);

                // Add local-defined functions as instance member functions
                for (int j = 0; j < localSyntax.functionList.Count; j++)
                {
                    var fmmf = localSyntax.functionList[j];
                    if (fmmf == null) continue;
                    if (fmmf.staticToken != null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, fmmf.token, "Error local{} functions cannot use static keyword");
                        continue;
                    }
                    var mmf = new MetaMemberFunction(localMc, fmmf);
                    localMc.AddMetaMemberFunction(mmf);
                    // BuildLocalClass 在 ParseMetaClassLink 之后运行，动态创建的 local 函数
                    // 不会经过该阶段，需补解析参数/返回类型，否则 IR 阶段参数类型为空
                    mmf.ParseDefineMetaType();
                    // 注册到 MethodManager，让主 ParseStatements 解析其函数体
                    MethodManager.instance.AddOriginalMemeberFunction(mmf);
                    // 动态添加的函数需进入虚函数列表，否则 IR 翻译阶段不生成 IRMethod
                    localMc.AddDynamicNonStaticMemberFunction(mmf);
                }

                // Create __local_init__ instance function (holds the local{} statements)
                var initFun = CreateLocalInitFunction(localMc, localSyntax);
                if (initFun != null)
                {
                    initFun.ParseDefineMetaType();
                    localMc.AddDynamicNonStaticMemberFunction(initFun);
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

        /// <summary>
        /// Creates a static `instance` member variable on the _Local class,
        /// typed as the _Local class itself, with a MetaNewObjectExpressNode initializer.
        /// </summary>
        private MetaMemberVariable CreateOrGetFileLocalInstanceVariable(FileMeta fm, MetaClass localMc)
        {
            if (fm == null || localMc == null) return null;

            var existing = localMc.GetMetaMemberVariableByName("instance");
            if (existing != null)
                return existing;

            var mmv = new MetaMemberVariable(localMc, "instance");
            mmv.SetIsStatic(true);
            var localType = new MetaType(localMc);
            mmv.SetMetaDefineType(localType);
            mmv.SetRealMetaType(localType);

            // Set initialization expression: new <FileName>_Local()
            // This creates the instance during static initialization phase,
            // which runs before _main_, satisfying the requirement that
            // local init runs after static/const initialization.
            var newExpr = new MetaNewObjectExpressNode(localType, localMc, null);
            mmv.SetExpress(newExpr);

            localMc.AddMetaMemberVariable(mmv);
            return localMc.GetMetaMemberVariableByName("instance") as MetaMemberVariable;
        }

        private MetaMemberFunction CreateLocalInitFunction(MetaClass localMc, FileMetaLocalSyntax localSyntax)
        {
            if (localMc == null || localSyntax == null) return null;

            var fn = new MetaMemberFunction(localMc, "__local_init__");

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

            // local{} init statements run on the local instance; variables defined there
            // should be treated as member variables of the generated _Local class
            // so `local.xxx` resolves like normal instance member access.
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

        /// <summary>
        /// Injects `__local_init__()` calls into Project._main_(), ordered by compileFiles priority.
        /// The instance is already created via static initialization, so we only need to call init.
        /// </summary>
        public void InjectLocalInitCalls(List<FileParse> fileParses)
        {
            if (fileParses == null) return;

            // Ensure local init methods have meta syntax created
            for (int i = 0; i < fileParses.Count; i++)
            {
                var fm = fileParses[i]?.file;
                if (fm == null) continue;
                var init = GetFileLocalInitFunction(fm);
                if (init == null) continue;
                init.ParseStatements();
            }

            var project = ClassManager.instance.TryGetProjectMetaClass();
            if (project == null) return;
            var main = project.GetFirstMetaMemberFunctionByName("_main_");
            if (main == null) return;

            var mbs = main.metaBlockStatements;
            if (mbs == null) return;

            // Inject in reverse order so that AddFrontStatements produces priority-ordered execution
            for (int i = fileParses.Count - 1; i >= 0; i--)
            {
                var fm = fileParses[i]?.file;
                if (fm == null) continue;

                var localMc = GetFileLocalClass(fm);
                if (localMc == null) continue;

                // Build: <FileName>_Local.instance.__local_init__()
                var className = GetFileLocalClassName(fm);
                var classToken = new Token(fm.path, ETokenType.Identifier, className, 0, 0);
                var baseNode = new Node(classToken) { nodeType = ENodeType.IdentifierLink };
                // CRITICAL: SetIdentifierNode so AddLinkNode doesn't silently fail
                baseNode.SetIdentifierNode(baseNode);

                // .instance
                baseNode.AddLinkNode(new Node(new Token(fm.path, ETokenType.Period, ".", 0, 0)) { nodeType = ENodeType.Period });
                baseNode.AddLinkNode(new Node(new Token(fm.path, ETokenType.Identifier, "instance", 0, 0)) { nodeType = ENodeType.IdentifierLink });

                // .__local_init__()
                baseNode.AddLinkNode(new Node(new Token(fm.path, ETokenType.Period, ".", 0, 0)) { nodeType = ENodeType.Period });

                var initToken = new Token(fm.path, ETokenType.Identifier, "__local_init__", 0, 0);
                var initNode = new Node(initToken) { nodeType = ENodeType.IdentifierLink };
                // Add empty () to make it a function call
                var parNode = new Node(new Token(fm.path, ETokenType.LeftPar, "(", 0, 0)) { nodeType = ENodeType.Par };
                parNode.endToken = new Token(fm.path, ETokenType.RightPar, ")", 0, 0);
                initNode.SetParNode(parNode);
                baseNode.AddLinkNode(initNode);

                var link = new FileMetaCallLink(fm, baseNode, true);
                var callSyntax = new FileMetaCallSyntax(link);
                var callStmt = new MetaCallStatements(mbs, callSyntax);
                mbs.AddFrontStatements(callStmt);
            }
        }
    }
}
