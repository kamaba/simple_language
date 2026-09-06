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
        /// <summary>占位为 Object 的 local 成员变量（类型待 init 解析后用右值类型回填）</summary>
        private readonly HashSet<MetaMemberVariable> m_PendingTypeMembers = new HashSet<MetaMemberVariable>();

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

                // 立即注册到字典：init 语句解析期间（ParseStatements）若引用 local.xxx，
                // MetaCallNode 的 Local 分支才能查到该类
                m_FileLocalClassDict.Add(fm.path, localMc);

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
                // 预扫描：为 local{} 中定义的变量创建 _Local 类占位成员变量，
                // 并在 init 语句末尾注入 `this.x = x` 同步语句（把局部变量值写回成员，
                // 供本文件其它位置通过 local.x 访问）。
                PrepareLocalInitMembers(fm, localMc, localSyntax);

                var initFun = CreateLocalInitFunction(localMc, localSyntax);
                if (initFun != null)
                {
                    initFun.ParseDefineMetaType();
                    localMc.AddDynamicNonStaticMemberFunction(initFun);
                    initFun.ParseStatements();
                    UpdatePendingMemberTypes(initFun);
                }

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

        /// <summary>
        /// 预扫描 local{} 的 init 语句：
        /// 1. 为每个"变量定义"（`a = expr` / `Type name = expr`）在 _Local 类上创建占位成员变量
        ///    （Object 类型，init 解析后用实际类型回填）；
        /// 2. 在 blockSyntax 末尾注入 `this.x = x` 同步语句——init 内部按普通局部变量
        ///    解析（`a = 1` 定义局部变量、`a.addVector(c)` 等链式访问均走标准路径），
        ///    语句结束时把局部变量值写回成员，供本文件其它位置通过 `local.x` 访问。
        /// 必须在 CreateLocalInitFunction（CreateMetaSyntax）之前调用。
        /// </summary>
        private void PrepareLocalInitMembers(FileMeta fm, MetaClass localMc, FileMetaLocalSyntax localSyntax)
        {
            var block = localSyntax?.blockSyntax;
            var syntaxList = block?.fileMetaSyntax;
            if (syntaxList == null) return;

            var defineNames = new List<(string name, Token nameToken)>();
            for (int i = 0; i < syntaxList.Count; i++)
            {
                var syn = syntaxList[i];
                if (syn == null) continue;

                if (syn is FileMetaDefineVariableSyntax fmdvs)
                {
                    var name = fmdvs.nameToken?.lexeme?.ToString();
                    if (string.IsNullOrEmpty(name)) continue;
                    if (localMc.GetMetaMemberVariableByName(name) != null) continue;
                    defineNames.Add((name, fmdvs.nameToken));
                }
                else if (syn is FileMetaOpAssignSyntax fmoas)
                {
                    // a = expr（单名字、'=' 赋值、无 var/data/dynamic 前缀）
                    if (fmoas.variableRef?.isOnlyName != true) continue;
                    if (fmoas.assignToken?.type != ETokenType.Assign) continue;
                    if (fmoas.hasDefine) continue;
                    var name = fmoas.variableRef.name;
                    if (string.IsNullOrEmpty(name)) continue;
                    if (localMc.GetMetaMemberVariableByName(name) != null) continue;
                    defineNames.Add((name, fmoas.variableRef.callNodeList[0]?.token));
                }
            }

            foreach (var (name, nameToken) in defineNames)
            {
                // 创建占位成员（Object），init 解析后用局部变量的实际类型回填
                var objMt = new MetaType(CoreMetaClassManager.objectMetaClass);
                var mmv = new MetaMemberVariable(localMc, name);
                mmv.SetIsStatic(false);
                mmv.SetMetaDefineType(objMt);
                mmv.SetRealMetaType(objMt);
                localMc.AddMetaMemberVariable(mmv);
                m_PendingTypeMembers.Add(mmv);

                // 注入同步语句: this.x = x
                var syncSyntax = CreateSyncToMemberSyntax(fm, name, nameToken);
                if (syncSyntax != null)
                    syntaxList.Add(syncSyntax);
            }
        }

        /// <summary>构造 `this.<name> = <name>` 同步语句（把 init 局部变量写回 _Local 成员）</summary>
        private FileMetaOpAssignSyntax CreateSyncToMemberSyntax(FileMeta fm, string name, Token nameToken)
        {
            if (fm == null || string.IsNullOrEmpty(name)) return null;

            var line = nameToken?.sourceBeginLine ?? 0;
            var col = nameToken?.sourceBeginChar ?? 0;
            if (nameToken == null)
                nameToken = new Token(fm.path, ETokenType.Identifier, name, line, col);

            // 左值: this.name 链
            var thisToken = new Token(fm.path, ETokenType.This, "this", line, col);
            var thisNode = new Node(thisToken) { nodeType = ENodeType.IdentifierLink };
            // 关键：不设置 identifierNode 时 AddLinkNode 会静默失败
            thisNode.SetIdentifierNode(thisNode);
            thisNode.AddLinkNode(new Node(new Token(fm.path, ETokenType.Period, ".", line, col)) { nodeType = ENodeType.Period });
            thisNode.AddLinkNode(new Node(nameToken) { nodeType = ENodeType.IdentifierLink });
            var varRef = new FileMetaCallLink(fm, thisNode, true);

            // 右值: name 表达式（局部变量）
            var xNode = new Node(nameToken) { nodeType = ENodeType.IdentifierLink };
            var fme = FileMetatUtil.CreateFileMetaExpress(fm, new List<Node> { xNode }, FileMetaTermExpress.EExpressType.Common);
            if (fme == null) return null;

            var assignToken = new Token(fm.path, ETokenType.Assign, "=", line, col);
            return new FileMetaOpAssignSyntax(varRef, assignToken, null, null, null, null, fme, true);
        }

        /// <summary>
        /// 判断某个 MetaBase 是否为 LocalManager 生成的 _Local 类
        /// （local{} init 函数内的裸名字定义需要按局部变量解析，见 MetaMemberFunction 的语句分支）
        /// </summary>
        public static bool IsFileLocalClass(MetaBase mb)
        {
            if (mb is MetaClass mc)
                return instance.m_FileLocalClassDict.ContainsValue(mc);
            return false;
        }

        /// <summary>
        /// `a = expr` 在 local{} init 中直接按成员赋值解析时（隐式 this.a = expr），
        /// 用右值类型回填占位为 Object 的成员变量类型，
        /// 保证后续语句（如 a.addVector(c)）按实际类型解析链式调用。
        /// </summary>
        public static void UpdatePendingMemberType(MetaVariable mv, MetaType mt)
        {
            if (mt == null) return;
            if (mv is MetaMemberVariable mmv && instance.m_PendingTypeMembers.Contains(mmv))
            {
                mmv.SetMetaDefineType(mt);
                mmv.SetRealMetaType(mt);
                instance.m_PendingTypeMembers.Remove(mmv);
            }
        }

        /// <summary>
        /// init 语句解析完成后，把占位为 Object 的成员变量类型回填为
        /// init 中同名局部变量的实际类型，保证后续函数（如 Add 里的 x + local.a）
        /// 与 `this.x = x` 同步语句都按正确类型解析。
        /// </summary>
        private void UpdatePendingMemberTypes(MetaMemberFunction initFun)
        {
            if (m_PendingTypeMembers.Count == 0) return;
            var mbs = initFun?.metaBlockStatements;
            for (var cur = mbs?.nextMetaStatements; cur != null; cur = cur.nextMetaStatements)
            {
                // `a = 1` / `Type a = expr` 解析为局部变量定义，类型已正确推导
                if (cur is not MetaDefineVarStatements mdvs) continue;
                var lmv = mdvs.defineVarMetaVariable;
                if (lmv == null) continue;
                var rt = lmv.realMetaType;
                if (rt == null) continue;

                var target = initFun.ownerMetaClass?.GetMetaMemberVariableByName(lmv.name);
                if (target == null || target.isStatic) continue;
                if (!m_PendingTypeMembers.Contains(target)) continue;

                target.SetMetaDefineType(rt);
                target.SetRealMetaType(rt);
                m_PendingTypeMembers.Remove(target);
            }
        }

        /// <summary>
        /// Injects `__local_init__()` calls into Project._main_(), ordered by compileFiles priority.
        /// The instance is already created via static initialization, so we only need to call init.
        /// </summary>
        public void InjectLocalInitCalls(List<FileParse> fileParses)
        {
            if (fileParses == null) return;

            // 注意：__local_init__ 的语句已在 BuildFileLocalClasses 中解析完成，
            // 这里不再重复调用 ParseStatements（会重复追加语句链）。

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
