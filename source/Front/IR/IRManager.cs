//****************************************************************************
//  File:      IRManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SimpleLanguage.IR
{
    public class IRManager
    {
        public static IRManager instance = new IRManager();

        public List<IRData> irDataList => m_IRDataList;
        public Dictionary<string, IRMethod> IRMethodDict = new Dictionary<string, IRMethod>();
        public Dictionary<int, string> IRStringDict = new Dictionary<int,string>();
        public List<IRMetaVariable> globalStaticVariableList => m_GlobalStaticVariableList;

        private List<IRMetaVariable> m_GlobalStaticVariableList = new List<IRMetaVariable>();
        #region debug用
        private Dictionary<int,IRMetaVariable> m_AllVariableDict = new Dictionary<int,IRMetaVariable>();
        #endregion

        private List<IRMetaClass> m_IRMetaClassList = new List<IRMetaClass>();

        public List<IRMetaClass> GetIRMetaClassList()
        {
            return m_IRMetaClassList;
        }
        public void AddIRMetaClass(IRMetaClass irmc)
        {
            if (irmc == null) return;
            if (m_IRMetaClassList.Find(a => a.id == irmc.id) == null)
            {
                m_IRMetaClassList.Add(irmc);
            }
        }
        private List<IRData>  m_IRDataList = new List<IRData>();
        public void TranslateIR()
        {
            Log.AddIRLog(LID.ShowExtendMessage, "Start translating IR...");

            ParseClass();

            //代码定义的成员函数
            // NOTE:
            // DebugCode 的 IR 导出需要同时包含“源码定义的成员函数”和“动态解析出来的函数”。
            // 之前仅翻译 metaDynamicFunctionList，导致 NativeBridge / BridgeKind 等类的同级函数缺失。
            var allMethods = new List<MetaMemberFunction>();
            var mmfDict = MethodManager.instance.metaOriginalFunctionList;
            foreach (var v in mmfDict)
            {
                allMethods.Add(v);
            }
            var dynamicMmfDict4 = MethodManager.instance.metaDynamicFunctionList;
            foreach (var v in dynamicMmfDict4)
            {
                allMethods.Add(v);
            }

            var translatedMethods = new ConcurrentBag<IRMethod>();
            Parallel.ForEach(allMethods, mmf =>
            {
                var irm = TranslateIRByFunction(mmf);
                if (irm != null)
                {
                    translatedMethods.Add(irm);
                }
            });

            foreach (var irm in translatedMethods)
            {
                AddIRMethod(irm);
            }

            ParseIRMethod();
            ExportIRDebugData();

            Log.AddIRLog(LID.ShowExtendMessage, "End translating IR...");
        }

        public void ExportIRDebugData()
        {
            try
            {
                if (!Common.ShouldExportDebugText("IR.txt"))
                {
                    return;
                }

                Log.AddIRLog(LID.ShowExtendMessage, "Start exporting IR debug data...");

                var fileClassMap = new Dictionary<string, List<IRMetaClass>>();
                for (int i = 0; i < m_IRMetaClassList.Count; i++)
                {
                    var irClass = m_IRMetaClassList[i];
                    if (irClass == null || string.IsNullOrEmpty(irClass.sourcePath))
                    {
                        continue;
                    }

                    if (!fileClassMap.ContainsKey(irClass.sourcePath))
                    {
                        fileClassMap.Add(irClass.sourcePath, new List<IRMetaClass>());
                    }
                    fileClassMap[irClass.sourcePath].Add(irClass);
                }

                foreach (var kv in fileClassMap)
                {
                    string filePath = kv.Key;
                    var classList = kv.Value;
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine("-------------------IR 文件显示 开始 : Path: " + filePath + "-----------------------");

                    for (int i = 0; i < classList.Count; i++)
                    {
                        var irClass = classList[i];
                        sb.AppendLine("Class: " + irClass.irName);
                        sb.AppendLine("  metaClassKind: " + irClass.metaClassKind);
                        sb.AppendLine("  TemplateCount: " + irClass.templateCount);
                        sb.AppendLine("  TemplateParameterCount: " + irClass.templateParameterCount);
                        if (irClass.templateTypeList == null || irClass.templateTypeList.Count == 0)
                        {
                            sb.AppendLine("  TemplateTypes: []");
                        }
                        else
                        {
                            sb.AppendLine("  TemplateTypes:");
                            for (int ti = 0; ti < irClass.templateTypeList.Count; ti++)
                            {
                                var tt = irClass.templateTypeList[ti];
                                if (tt == null) continue;
                                sb.AppendLine("    - #" + ti + " " + tt.ToString());
                            }
                        }
                        if (irClass.templateRelation == null || irClass.templateRelation.Count == 0)
                        {
                            sb.AppendLine("  TemplateRelations: []");
                        }
                        else
                        {
                            sb.AppendLine("  TemplateRelations:");
                            foreach (var rel in irClass.templateRelation)
                            {
                                sb.AppendLine("    RelatedClassId: " + rel.Key);
                                var map = rel.Value;
                                if (map == null || map.Count == 0)
                                {
                                    sb.AppendLine("      Mapping: []");
                                    continue;
                                }
                                foreach (var kvp in map)
                                {
                                    sb.AppendLine("      - TIndex " + kvp.Key + " => " + (kvp.Value?.ToString() ?? "null"));
                                }
                            }
                        }
                        AppendIRVariableList(sb, "  ClassLocals", irClass.localIRMetaVariableList);
                        AppendIRVariableList(sb, "  ClassStatics", irClass.staticIRMetaVariableList);
                        AppendGlobalBindingList(sb, "  GlobalBindings", irClass, m_GlobalStaticVariableList);
                        sb.AppendLine();

                            // Export methods based on the class's own resolved method lists.
                            // This ensures derived classes show inherited/virtual methods correctly (e.g. BridgeKind should
                            // show Byte's virtuals even when IR ownerMetaClass points to the base).
                            var exported = new HashSet<string>(StringComparer.Ordinal);

                            void ExportOneMethod(IRMethod irMethod)
                            {
                                if (irMethod == null) return;
                                if (string.IsNullOrEmpty(irMethod.id)) return;
                                if (!exported.Add(irMethod.id)) return;

                                sb.AppendLine("  Method: " + irMethod.id);
                                sb.AppendLine("    VirtualName: " + irMethod.virtualFunctionName);
                                sb.AppendLine("    Name: " + irMethod.onlyFunctionName);
                                sb.AppendLine("    InterfaceMethod: " + irMethod.interfaceMethod);
                                AppendIRVariableList(sb, "    Return", irMethod.methodReturnVariableList);
                                AppendIRVariableList(sb, "    Arguments", irMethod.methodArgumentList);
                                AppendIRVariableList(sb, "    Locals", irMethod.methodLocalVariableList);
                                sb.AppendLine("    Instructions:");
                                sb.Append(irMethod.ToIRString());
                                sb.AppendLine();
                            }

                            if (irClass.staticMethodList != null)
                            {
                                for (int m = 0; m < irClass.staticMethodList.Count; m++)
                                    ExportOneMethod(irClass.staticMethodList[m]);
                            }
                            if (irClass.nonStaticMethodList != null)
                            {
                                for (int m = 0; m < irClass.nonStaticMethodList.Count; m++)
                                    ExportOneMethod(irClass.nonStaticMethodList[m]);
                            }
                            if (irClass.operatorMethodList != null)
                            {
                                for (int m = 0; m < irClass.operatorMethodList.Count; m++)
                                    ExportOneMethod(irClass.operatorMethodList[m]);
                            }
                    }

                    sb.AppendLine("-------------------IR 文件显示 结束 : -----------------------");

                    string outPath = Common.GetDebugCodeFilePath(filePath, "IR.txt");
                    File.WriteAllText(outPath, sb.ToString());
                }
                Log.AddIRLog(LID.ShowExtendMessage, "End exporting IR debug data...");
            }
            catch (Exception e)
            {
                Debug.Assert(false, "Export IR debug data failed: " + e.Message);
            }
        }

        private static void AppendIRVariableList(StringBuilder sb, string title, List<IRMetaVariable> list)
        {
            if (list == null || list.Count == 0)
            {
                sb.AppendLine(title + ": []");
                return;
            }

            sb.AppendLine(title + ":");
            for (int i = 0; i < list.Count; i++)
            {
                var v = list[i];
                if (v == null)
                {
                    continue;
                }

                sb.Append("      - #");
                sb.Append(i);
                sb.Append(" id=");
                sb.Append(v.id);
                sb.Append(" idx=");
                sb.Append(v.index);
                sb.Append(" name=");
                sb.Append(v.name);
                sb.Append(" type=");
                sb.AppendLine(FormatIRMetaType(v.irMetaType));
            }
        }

        private static string FormatIRMetaType(IRMetaType irType)
        {
            if (irType == null)
            {
                return "<null>";
            }

            var sb = new StringBuilder();
            if (irType.templateIndex >= 0)
            {
                sb.Append("T[");
                sb.Append(irType.templateIndex);
                sb.Append("]");
            }
            else
            {
                sb.Append(irType.irMetaClass?.irName ?? "<null>");
            }

            var genericList = irType.irMetaTypeList;
            if (genericList != null && genericList.Count > 0)
            {
                sb.Append("<");
                for (int i = 0; i < genericList.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(FormatIRMetaType(genericList[i]));
                }
                sb.Append(">");
            }

            return sb.ToString();
        }

        private static void AppendGlobalBindingList(StringBuilder sb, string title, IRMetaClass irClass, List<IRMetaVariable> globalList)
        {
            if (irClass == null || globalList == null || globalList.Count == 0)
            {
                sb.AppendLine(title + ": []");
                return;
            }

            int hit = 0;
            for (int i = 0; i < globalList.Count; i++)
            {
                var g = globalList[i];
                if (g?.irMetaType?.irOwnerMetaClass == null) continue;
                if (!string.Equals(g.irMetaType.irOwnerMetaClass.irName, irClass.irName, StringComparison.Ordinal)) continue;

                if (hit == 0)
                {
                    sb.AppendLine(title + ":");
                }

                sb.Append("      - #");
                sb.Append(i);
                sb.Append(" id=");
                sb.Append(g.id);
                sb.Append(" owner=");
                sb.Append(irClass.irName);
                sb.Append(" ownerFieldIndex=");
                sb.Append(g.index);
                sb.Append(" name=");
                sb.Append(g.name);
                sb.Append(" type=");
                sb.AppendLine(FormatIRMetaType(g.irMetaType));
                hit++;
            }

            if (hit == 0)
            {
                sb.AppendLine(title + ": []");
            }
        }
        //public void GlobalVariable()
        //{
        //    var staticArray = IRManager.instance.globalStaticVariableList;

        //    List<IRData> execIRList = new List<IRData>();
        //    for (int i = 0; i < staticArray.Count; i++)
        //    {
        //        //m_GlobalVariableId2IndexDict.Add(staticArray[i].id, i);

        //        //var rt = RuntimeTypeManager.GetRuntimeTypeByMIRMetaType(staticArray[i].irMetaType);
        //        //IRMetaClass owirmc = IRManager.instance.GetIRMetaClassById(staticArray[i].irMetaType.irOwnerMetaClass.id);

        //        //var obj = ObjectManager.CreateObjectByRuntimeType(rt, true);
        //        //SValue tmp = default;
        //        //tmp.SetSObject(obj);
        //        //m_GlobalVariableValueList.Add(tmp);

        //        IRExpressBase irexpress = IRExpressManager.CreateExpress(null, staticArray[i].express);

        //        IRStoreVariable irsv = new IRStoreVariable(staticArray[i].irMetaType, null, staticArray[i].id, IRMetaVariableFrom.Global);

        //        execIRList.AddRange(irexpress.IRDataList);
        //        execIRList.AddRange(irsv.IRDataList);

        //    }
        //}
        public IRMetaClass GetIRMetaClassById( int id )
        {
            return m_IRMetaClassList.Find(a => a.id == id );
        }
        public IRMetaClass GetIRMetaClassByName( string allname )
        {
            return m_IRMetaClassList.Find(a => a.irName == allname);
        }

        /// <summary>
        /// ref module 的 IRMetaClass 在 IR 先行构建时用包内 cls.id（导出会话 hash）作为 id，
        /// 与当前会话 MetaBase.GetHashCode() 不一致，按 id 会查不到。此时仅对 ref module 类型
        /// 回退到按 irName 查找：先试 allName（Core 内建类型 irName 含 "Core." 前缀，可命中），
        /// 再去掉模块名前缀试一次（非 Core ref module 类型的 irName 是去前缀的，可命中）。
        /// </summary>
        private static IRMetaClass FindRefModuleIRMetaClassByName( MetaBase owner )
        {
            if (owner == null || instance == null) return null;
            if (owner.refFromType != RefFromType.RefModule) return null;
            var name = owner.allName;
            if (string.IsNullOrEmpty(name)) return null;
            var irmc = instance.GetIRMetaClassByName(name);
            if (irmc != null) return irmc;
            int idx = name.IndexOf('.');
            if (idx > 0 && idx + 1 < name.Length)
            {
                irmc = instance.GetIRMetaClassByName(name.Substring(idx + 1));
            }
            return irmc;
        }

        /// <summary>
        /// 由语义 <see cref="MetaType"/> 解析已导出的 <see cref="IRMetaClass"/>（class / data / enum / 模板实例）。
        /// </summary>
        public static IRMetaClass GetIRMetaClassByMetaType(MetaType type)
        {
            if (type == null || instance == null)
            {
                return null;
            }

            if (type.isData)
            {
                var md = type.metaData;
                if (md != null)
                {
                    var irmc = instance.GetIRMetaClassById(md.GetHashCode());
                    if (irmc == null) irmc = FindRefModuleIRMetaClassByName(md);
                    if (irmc == null)
                    {
                        irmc = new IRMetaClass(md);
                        instance.m_IRMetaClassList.Add(irmc);
                    }
                    return irmc;
                }
            }

            if (type.isEnum)
            {
                var me = type.metaEnum;
                if (me != null)
                {
                    var irmc = instance.GetIRMetaClassById(me.GetHashCode());
                    if (irmc == null) irmc = FindRefModuleIRMetaClassByName(me);
                    if (irmc == null)
                    {
                        irmc = new IRMetaClass(me);
                        instance.m_IRMetaClassList.Add(irmc);
                    }
                    return irmc;
                }
            }

            var tmc = type.GetTemplateMetaClass();
            if (tmc != null)
            {
                var irmc = instance.GetIRMetaClassById(tmc.GetHashCode());
                if (irmc == null) irmc = FindRefModuleIRMetaClassByName(tmc);
                if (irmc == null && tmc is not MetaGenTemplateClass)
                {
                    irmc = new IRMetaClass(tmc);
                    instance.m_IRMetaClassList.Add(irmc);
                }
                return irmc;
            }

            return null;
        }

        /// <summary>
        /// 由宿主 <see cref="MetaBase"/>（class / data / enum）解析 <see cref="IRMetaClass"/>。
        /// </summary>
        public static IRMetaClass GetIRMetaClassByMetaOwner(MetaBase owner)
        {
            if (owner == null || instance == null)
            {
                return null;
            }

            return instance.GetIRMetaClassById(owner.GetHashCode())
                ?? FindRefModuleIRMetaClassByName(owner);
        }

        /// <summary>
        /// 由 <see cref="MetaVariable"/> 的 owner 上下文解析 <see cref="IRMetaClass"/>（含 data / enum 宿主）。
        /// </summary>
        public static IRMetaClass GetIRMetaClassByMetaVariable(MetaVariable mv)
        {
            if (mv == null || instance == null)
            {
                return null;
            }

            var ownerClass = mv.GetOwnerClassTemplateClass();
            if (ownerClass != null)
            {
                return instance.GetIRMetaClassById(ownerClass.GetHashCode())
                    ?? FindRefModuleIRMetaClassByName(ownerClass);
            }

            return GetIRMetaClassByMetaOwner(mv.ownerMetaBase);
        }
        void ParseClass()
        {
            Log.AddIRLog(LID.ShowExtendMessage, "Start translating IRMetaClass...");
            var cm = ClassManager.instance;
            var exportClassList = cm.exportMetaClassList;
            var exportedOwnerNames = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var v in exportClassList)
            {
                IRMetaClass irmc = new IRMetaClass(v);
                m_IRMetaClassList.Add(irmc);
                if (!string.IsNullOrEmpty(v.allName))
                {
                    exportedOwnerNames.Add(v.allName);
                }
            }
            foreach (var v in cm.exportMetaDataList)
            {
                IRMetaClass irmc = new IRMetaClass(v);
                m_IRMetaClassList.Add(irmc);
                if (!string.IsNullOrEmpty(v.allName))
                {
                    exportedOwnerNames.Add(v.allName);
                }
            }
            foreach (var v in cm.exportMetaEnumList)
            {
                IRMetaClass irmc = new IRMetaClass(v);
                m_IRMetaClassList.Add(irmc);
                if (!string.IsNullOrEmpty(v.allName))
                {
                    exportedOwnerNames.Add(v.allName);
                }
            }
            /* Register built-in core types that are not in exportMetaClassList
             * but are referenced by metaTemplateMapDict of user-defined classes. */
            var builtinTypes = new[] {
                CoreMetaClassManager.objectMetaClass,
                CoreMetaClassManager.voidMetaClass,
                CoreMetaClassManager.dataMetaClass,
                CoreMetaClassManager.memberMetaClass,
                CoreMetaClassManager.numMetaClass,
                CoreMetaClassManager.stringMetaClass,
                CoreMetaClassManager.typeMetaClass,
            };
            foreach (var bt in builtinTypes)
            {
                if (bt == null) continue;
                if (m_IRMetaClassList.Find(a => a.id == bt.GetHashCode()) == null)
                {
                    m_IRMetaClassList.Add(new IRMetaClass(bt));
                }
            }
            /* Also register ALL classes from ClassManager that are not yet in m_IRMetaClassList.
             * This includes ref module classes (Core.String, Core.Number, etc.) that are
             * referenced by user code but not in exportMetaClassList. */
            foreach (var kv in cm.allClassDict)
            {
                var mc = kv.Value;
                if (mc == null) continue;
                /* Skip gen template classes (handled separately below) and
                 * template classes (already in exportMetaClassList if source-compiled). */
                if (mc is MetaGenTemplateClass) continue;
                if (m_IRMetaClassList.Find(a => a.id == mc.GetHashCode()) == null)
                {
                    m_IRMetaClassList.Add(new IRMetaClass(mc));
                }
            }
            /* Iterate a snapshot since GetIRMetaClassByMetaType may add new
             * IRMetaClass instances to m_IRMetaClassList during processing. */
            foreach ( var v in m_IRMetaClassList.ToArray() )
            {
                v.CreateMemberData();
                v.CreateMemberMethod();
                v.CreateTemplateRelation();

                if ( v.metaClassKind == IRMetaClassKind.Class
                    || v.metaClassKind == IRMetaClassKind.Interface )
                {
                    v.CreateGenMetaTypeTemplateList();
                }
            }

            /* Register gen template classes AFTER CreateTemplateRelation (so they don't
             * get processed by it) but BEFORE CreateStaticMetaMetaVariableIRList (so
             * static variable initializers can reference them). */
            foreach (var v in cm.genTemplateMetaClassList)
            {
                if (v == null) continue;
                m_IRMetaClassList.Add(new IRMetaClass(v));
            }

            foreach ( var v in m_IRMetaClassList.ToArray())
            {
                foreach( var v2 in v.localIRMetaVariableList )
                {
                    m_AllVariableDict.Add(v2.GetHashCode(), v2);
                }

            }
            foreach ( var v in exportClassList )
            {
                if (v.isTemplateClass)
                {
                    continue;
                }
                var irmc = m_IRMetaClassList.Find(a => a.irName == v.allName);
                if (irmc == null)
                    continue;

                if (v != null && ClassManager.instance.FindMetaDataByName(v.allName) is MetaData md)
                {
                    var mmvd = md.metaMemberDataDict;
                    foreach (var v2 in mmvd)
                    {
                        if (v2.Value.isStatic)
                        {
                            IRMetaVariable irMV = new IRMetaVariable(v2.Value);
                            //irMV.index = m_StaticVariableList.Count;
                            ////irMV.SetExpress(v2.Value.express);
                            //m_StaticVariableList.Add(irMV);
                        }
                    }
                }
                else
                {
                    var mmvd = v.metaMemberVariableDict;
                    foreach (var v2 in mmvd)
                    {
                        if (v2.Value.isStatic)
                        {
                            IRMetaVariable irMV = new IRMetaVariable(v2.Value);
                            if( v2.Value.sourceMetaMemberVariable != null )
                            {
                                //irmc.AddMetaMemberVariableHashCode(v2.Value.sourceMetaMemberVariable.GetHashCode(), v2.Value.GetHashCode());
                            }
                            //irMV.index = m_StaticVariableList.Count;
                            //irMV.SetExpress(v2.Value.express);
                            //m_StaticVariableList.Add(irMV);
                        }
                    }
                }
            }
            foreach (var v in m_IRMetaClassList.ToArray())
            {
                /* Skip gen template classes: they're registered for lookup only,
                 * not for full IR generation (static variable IR list would crash
                 * on unresolved type references). */
                if (v.OwnerMetaClass is MetaGenTemplateClass)
                    continue;
                v.CreateStaticMetaMetaVariableIRList();
            }

            Log.AddIRLog(LID.ShowExtendMessage, "End translating IRMetaClass...");
        }
        //public void TranslateIRAutoAdd( MetaFunction mf )
        //{
        //    if (IRMethodDict.ContainsKey(mf.functionAllName))
        //    {
        //        return;
        //    }
        //    var irm = TranslateIRByFunction(mf);

        //    IRMethodDict.Add(mf.functionAllName, irm);

        //}
        public IRMethod TranslateIRByFunction( MetaFunction mf )
        {
            var hmf = mf;
            if( mf is MetaMemberFunction mmf )
            {
                if(mmf.sourceMetaMemberFunction != null )
                {
                    hmf = mmf.sourceMetaMemberFunction;
                }
                else
                {
                    hmf = mmf;
                }
            }

            if(IRMethodDict.ContainsKey(hmf.functionAllName ) )
            {
                return IRMethodDict[hmf.functionAllName];
            }
            IRMethod irmethod = new IRMethod(this, hmf);
            return irmethod;
        }
        public void ParseIRMethod()
        {
            foreach( var v in IRMethodDict )
            {
                v.Value.Parse();
            }
        }
        public int GetGlobalStaticMetaVariableById(int id)
        {
            for( int i = 0; i < m_GlobalStaticVariableList.Count; i++ )
            {
                if(m_GlobalStaticVariableList[i].id == id )
                {
                    return i;
                }
            }
            return -1;
        }
        public void AddGlobalMetaMemberVariable( IRMetaVariable irmv )
        {
            m_GlobalStaticVariableList.Add(irmv);
        }
        public int AddStringIRStack( string strMsg )
        {
            foreach( var v in IRStringDict )
            {
                if (v.Value.Equals(strMsg))
                    return v.Key;
            }
            int count = IRStringDict.Count + 1;
            IRStringDict.Add(count, strMsg);
            return count;
        }
        public string GetStringIRStack( int index )
        {
            if( IRStringDict.ContainsKey( index ) )
            {
                return IRStringDict[index];
            }
            return null;
        }
        public bool AddIRMethod( IRMethod method )
        {
            if(IRMethodDict.ContainsKey( method.id ) )
            {
                return false;
            }
            else
            {
                IRMethodDict.Add(method.id, method);
                return true;
            }
        }
        public IRMethod GetIRMethod( string name )
        {
            if( IRMethodDict.ContainsKey( name ) )
            {
                return IRMethodDict[name];
            }
            return null;
        }
        public static string GetIRNameByMetaOwner(MetaBase owner)
        {
            if (owner == null)
            {
                return string.Empty;
            }
            if (owner is MetaClass mc)
            {
                return mc.allName;
            }
            if (owner is MetaData md)
            {
                return md.allName;
            }
            if (owner is MetaEnum me)
            {
                return me.allName;
            }
            return owner.name ?? string.Empty;
        }

        public static string GetIRNameByMetaClass(MetaClass mc)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(mc.allName);
            if (mc is MetaGenTemplateClass mgtc)
            {
                sb.Append("<");
                for (int i = 0; i < mgtc.metaGenTemplateList.Count; i++)
                {
                    sb.Append(IRManager.GetIRNameByMetaClass(mgtc.metaGenTemplateList[i].metaType.metaClass));
                    if (i < mgtc.metaGenTemplateList.Count - 1)
                    { sb.Append(","); }
                }
                sb.Append(">");
            }
            else
            {
                if (mc.metaTemplateList.Count > 0)
                {
                    sb.Append("<");
                    for (int i = 0; i < mc.metaTemplateList.Count; i++)
                    {
                        sb.Append(mc.metaTemplateList[i].name);
                        if (i < mc.metaTemplateList.Count - 1)
                        { sb.Append(","); }
                    }
                    sb.Append(">");
                }
            }

            return sb.ToString();
        }
        public static string GetIRNameByMetaType(MetaType mt)
        {
            StringBuilder sb = new StringBuilder();

            if (mt.eMetaTypeType == EMetaTypeType.Template)
            {
                sb.Append("$");
                sb.Append(mt.metaTemplate.name);
                sb.Append("$");
            }
            else if (mt.eMetaTypeType == EMetaTypeType.MetaData)
            {
                sb.Append(GetIRNameByMetaOwner(mt.metaData));
            }
            else if (mt.eMetaTypeType == EMetaTypeType.MetaEnum)
            {
                sb.Append(GetIRNameByMetaOwner(mt.metaEnum));
            }
            else if (mt.eMetaTypeType == EMetaTypeType.MetaClass)
            {
                sb.Append(mt.GetTemplateMetaClass().metaNode.allName);
                if (mt.metaClass is MetaGenTemplateClass mgtc)
                {
                    sb.Append("<");
                    for (int i = 0; i < mgtc.metaGenTemplateList.Count; i++)
                    {
                        sb.Append(GetIRNameByMetaType(mgtc.metaGenTemplateList[i].metaType));
                        if (i < mgtc.metaGenTemplateList.Count - 1)
                        { sb.Append(","); }
                    }
                    sb.Append('>');
                }
                else
                {
                    if (mt.GetTemplateMetaClass().metaTemplateList.Count > 0)
                    {
                        sb.Append("<");
                        for (int i = 0; i < mt.GetTemplateMetaClass().metaTemplateList.Count; i++)
                        {
                            sb.Append("$");
                            sb.Append(mt.GetTemplateMetaClass().metaTemplateList[i].name);
                            sb.Append("$");
                            if (i < mt.GetTemplateMetaClass().metaTemplateList.Count - 1)
                            { sb.Append(","); }
                        }
                        sb.Append('>');
                    }
                }
            }
            else
            {
                if (mt.isData && mt.metaData != null)
                {
                    sb.Append(GetIRNameByMetaOwner(mt.metaData));
                }
                else if (mt.isEnum && mt.metaEnum != null)
                {
                    sb.Append(GetIRNameByMetaOwner(mt.metaEnum));
                }
                else
                {
                    var tmc = mt.GetTemplateMetaClass();
                    if (tmc == null)
                    {
                        return sb.ToString();
                    }
                    sb.Append(tmc.metaNode.allName);
                    sb.Append("<");
                    for (int i = 0; i < mt.defineTemplateMetaTypeList.Count; i++)
                    {
                        sb.Append(GetIRNameByMetaType(mt.defineTemplateMetaTypeList[i]));
                        if (i < mt.defineTemplateMetaTypeList.Count - 1)
                        { sb.Append(","); }
                    }
                    sb.Append('>');
                }
            }

            return sb.ToString();
        }
        public string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            foreach( var v in IRMethodDict )
            {
                sb.Append(v.Value.ToIRString());
            }

            return sb.ToString();
        }
    }
}
