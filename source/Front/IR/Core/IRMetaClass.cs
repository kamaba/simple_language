//****************************************************************************
//  File:      IRMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: Meta class's ir attribute
//****************************************************************************


using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using SimpleLanguage.Core;
using SimpleLanguage.Logging;

namespace SimpleLanguage.IR
{
    public class IRMetaClass
    {
        public int id { get; set; } = 0;
        public string irName => m_IRName;
        public string sourcePath => m_SourcePath;
        public bool needInitMemberVariable => m_NeedInitMemberVariable;
        /// <summary>Whether this IR type is a <c>class</c>, <c>interface</c>, <c>enum</c>, or <c>data</c> (see <see cref="IRMetaClassKind"/>).</summary>
        public IRMetaClassKind metaClassKind => m_MetaClassKind;

        public List<IRMetaVariable> localIRMetaVariableList => m_LocalIRMetaVariableList;
        public List<IRMetaVariable> staticIRMetaVariableList => m_StaticIRMetaVariableList;

        // expose method lists for exporter
        public List<IRMethod> nonStaticMethodList => m_IRNotStaticMethodList;
        public List<IRMethod> operatorMethodList => m_IROperatorMethodList;
        public List<IRMethod> staticMethodList => m_IRStaticMethodList;
        // expose generated/template meta types for exporter
        public List<IRMetaType> templateTypeList => m_IRMetaTypeList;
        // number of generated/template meta types
        public int templateCount => m_TemplateCount;
        /// <summary>Number of template parameters declared in source (e.g. <c>class Foo&lt;T,U&gt;</c> 鈫?2). Not the same as <see cref="templateCount"/>.</summary>
        public int templateParameterCount => OwnerMetaClass?.metaTemplateList?.Count ?? 0;

        /// <summary>语义类型宿主：<see cref="MetaClass"/> / <see cref="MetaData"/> / <see cref="MetaEnum"/> 之一。</summary>
        public MetaBase typeOwner => m_TypeOwner;
        public MetaClass OwnerMetaClass => m_TypeOwner as MetaClass;
        public MetaData OwnerMetaData => m_TypeOwner as MetaData;
        public MetaEnum OwnerMetaEnum => m_TypeOwner as MetaEnum;
        // template relations mapping: key is related class id, value maps template index -> IRMetaType
        public Dictionary<int, Dictionary<int, IRMetaType>> templateRelation => m_IRMetaClassMapTemplateDict;

        Dictionary<int, Dictionary<int, IRMetaType>> m_IRMetaClassMapTemplateDict = new Dictionary<int, Dictionary<int, IRMetaType>>();
        private Dictionary<int, int> m_MetaMemberVariableHashCodeDict = new Dictionary<int, int>();
        private List<IRMetaVariable> m_LocalIRMetaVariableList = new List<IRMetaVariable>();
        private List<IRMetaVariable> m_StaticIRMetaVariableList = new List<IRMetaVariable>();
        private List<IRMethod> m_IRNotStaticMethodList = new List<IRMethod>();
        private List<IRMethod> m_IROperatorMethodList = new List<IRMethod>();
        private List<IRMethod> m_IRStaticMethodList = new List<IRMethod>();
        private List<IRMetaType> m_IRMetaTypeList = new List<IRMetaType>();
        private string m_IRName = "";
        private string m_SourcePath = "";
        private MetaBase m_TypeOwner = null;
        private IRMetaClassKind m_MetaClassKind = IRMetaClassKind.Class;
        private int m_TemplateCount = 0;
        private bool m_NeedInitMemberVariable = false;


        //public int byteCount => m_ByteCount;
        //public int templateCount => m_TemplateCount;
        //private int allocSize = 0;
        //private List<EType> m_MetaTypeList = new List<EType>();
        //private int m_ByteCount = 0;

        //static int s_TypeLength = 1000;
        public IRMetaClass(MetaClass mc)
        {
            if (mc == null)
            {
                throw new System.ArgumentNullException(nameof(mc));
            }
            InitFromMetaOwner(mc, mc.isInterfaceClass ? IRMetaClassKind.Interface : IRMetaClassKind.Class);
        }

        public IRMetaClass(MetaData md)
        {
            if (md == null)
            {
                throw new System.ArgumentNullException(nameof(md));
            }
            InitFromMetaOwner(md, IRMetaClassKind.Data);
        }

        public IRMetaClass(MetaEnum me)
        {
            if (me == null)
            {
                throw new System.ArgumentNullException(nameof(me));
            }
            InitFromMetaOwner(me, IRMetaClassKind.Enum);
        }

        void InitFromMetaOwner(MetaBase owner, IRMetaClassKind kind)
        {
            m_TypeOwner = owner ?? throw new System.ArgumentNullException(nameof(owner));
            m_MetaClassKind = kind;
            id = owner.GetHashCode();
            m_IRName = IRManager.GetIRNameByMetaOwner(owner);

            try
            {
                if (owner is MetaClass mc)
                {
                    foreach (var kv in mc.fileMetaClassDict)
                    {
                        var fmc = kv.Value;
                        if (fmc != null && !string.IsNullOrEmpty(fmc.fileMeta?.path))
                        {
                            m_SourcePath = fmc.fileMeta.path;
                            break;
                        }
                    }
                }
                else if (owner is MetaData md && md.boundFileMetaClass != null)
                {
                    var fp = md.boundFileMetaClass.fileMeta?.path;
                    if (!string.IsNullOrEmpty(fp))
                        m_SourcePath = fp;
                }
                else if (owner is MetaEnum me && me.boundFileMetaClass != null)
                {
                    var fp = me.boundFileMetaClass.fileMeta?.path;
                    if (!string.IsNullOrEmpty(fp))
                        m_SourcePath = fp;
                }
            }
            catch
            {
                m_SourcePath = "";
            }
        }
        /// <summary>Ids of interface types this class is declared to implement (stable hash = IR class id), including those merged from the base class.</summary>
        public IReadOnlyList<int> GetImplementsInterfaceClassIds()
        {
            var ownerMc = OwnerMetaClass;
            if (ownerMc == null) return System.Array.Empty<int>();
            var icl = ownerMc.interfaceClass;
            if (icl == null || icl.Count == 0) return System.Array.Empty<int>();
            var list = new List<int>(icl.Count);
            for (int i = 0; i < icl.Count; i++)
            {
                var ic = icl[i];
                if (ic == null) continue;
                int iid = ic.GetHashCode();
                if (iid == 0) continue;
                if (!list.Contains(iid)) list.Add(iid);
            }
            return list;
        }

        public IRMethod GetIRNonStaticMethodByIndex( int index )
        {
            if( index >= m_IRNotStaticMethodList.Count || index < 0 )
            {
                Log.AddIRLog(LID.ShowExtendMessage, "GetIRMethodByIndex is null");
                return null;
            }
            return m_IRNotStaticMethodList[index];
        }
        public IRMethod GetIRNonStaticMethodIndexByMethod(string name, out int index)
        {
            index = -1;
            for (int i = 0; i < m_IRNotStaticMethodList.Count; i++)
            {
                if (m_IRNotStaticMethodList[i].virtualFunctionName == name)
                {
                    index = i;
                    return m_IRNotStaticMethodList[i];
                }
            }
            return null;
        }
        public IRMethod GetIRNonStaticMethodIndexByName(string name, out int index)
        {
            index = -1;
            for (int i = 0; i < m_IRNotStaticMethodList.Count; i++)
            {
                if (m_IRNotStaticMethodList[i].onlyFunctionName == name)
                {
                    index = i;
                    return m_IRNotStaticMethodList[i];
                }
            }
            return null;
        }
        public IRMethod GetIROperatorMethodIndexByMethod( string name, out int index )
        {
            index = -1;
            for (int i = 0; i < m_IROperatorMethodList.Count; i++)
            {
                if (m_IROperatorMethodList[i].onlyFunctionName == name)
                {
                    index = i;
                    return m_IROperatorMethodList[i];
                }
            }
            return null;
        }
        public IRMetaType GetIRMetaTypeByTemplateAndClassRelation( IRMetaClass irmc, int index )
        {
            if(m_IRMetaClassMapTemplateDict.ContainsKey(irmc.id ) )
            {
                var irmcmap = m_IRMetaClassMapTemplateDict[irmc.id];
                if( irmcmap != null )
                {
                    if( irmcmap.ContainsKey( index ) )
                    {
                        return irmcmap[index];
                    }
                }
            }
            return null;
        }
        public bool IsExtendsRelation( IRMetaClass irmc )
        {
            if( this == irmc )
            {
                return true;
            }
            if (m_IRMetaClassMapTemplateDict.ContainsKey(irmc.id))
            {
                return true;
            }
            return false;
        }
        public int GetIRNonStaticMethodIndexByMethod( string name )
        {
            for ( int i = 0; i < m_IRNotStaticMethodList.Count; i++ )
            {
                if(m_IRNotStaticMethodList[i].virtualFunctionName == name)
                {
                    return i;
                }
            }
            return -1;
        }
        public int GetMetaMemberVariableIndexByHashCode( int id )
        {
            if(m_MetaMemberVariableHashCodeDict.ContainsKey(id ) )
            {
                return m_MetaMemberVariableHashCodeDict[id];
            }
            return -1;
        }
        public void AddMetaMemberVariableIndexBindHashCode( int id, int newid)
        {
            if( !m_MetaMemberVariableHashCodeDict.ContainsKey( id ) )
            {
                m_MetaMemberVariableHashCodeDict.Add(id, newid);
            }
        }
        public void CreateMemberData()
        {
            if (OwnerMetaData != null)
            {
                CreateMemberDataFromMetaData(OwnerMetaData);
                return;
            }
            if (OwnerMetaEnum != null)
            {
                CreateMemberDataFromMetaEnum(OwnerMetaEnum);
                return;
            }
            var mc = OwnerMetaClass;
            if (mc == null)
            {
                return;
            }

            var md = ClassManager.instance.FindMetaDataByName(mc.allName);
            if (md != null)
            {
                CreateMemberDataFromMetaData(md);
                return;
            }

            var localMetaMemberVariables = mc.GetMetaMemberVariableListByFlag(false);
            for (int i = 0; i < localMetaMemberVariables.Count; i++)
            {
                var v = localMetaMemberVariables[i];
                IRMetaVariable irmv = new IRMetaVariable(this, v, i);
                m_LocalIRMetaVariableList.Add(irmv);
                AddMetaMemberVariableIndexBindHashCode(irmv.id, i);
                if (v.isInnerDefine == false)
                {
                    //if (v.metaDefineType.metaClass != null)
                    //    m_MetaTypeList.Add(v.metaDefineType.metaClass.eType);
                }
            }

            var staticMetaMemberVariables = mc.GetMetaMemberVariableListByFlag(true);
            for (int i = 0; i < staticMetaMemberVariables.Count; i++)
            {
                var v = staticMetaMemberVariables[i];

                IRMetaVariable irmv = new IRMetaVariable(this, v, i);
                //bool isProjectLikeClass = OwnerMetaClass != null
                //    && OwnerMetaClass.name == "Project"
                //    && !string.IsNullOrEmpty(this.m_SourcePath)
                //    && this.m_SourcePath.EndsWith(".sp", System.StringComparison.OrdinalIgnoreCase);

                // const 鎴愬憳闇€瑕佽繘鍏?globalStaticVariableList锛岀敱 VM 鍦ㄥ叏灞€闃舵鍒濆鍖栵紙涓庛€岄潪 Project/闈炴ā鏉块潤鎬併€嶈矾寰勪竴鑷达級銆?
                // 鑻ュ悓鏃舵斁杩?staticIRMetaVariableList锛屼細涓庡叏灞€鍒濆鍖栭噸澶嶃€?
                //if (v.isConst || isProjectLikeClass )
                //{
                //    IRManager.instance.AddGlobalMetaMemberVariable(irmv);
                //}
                //else// if ( v.realMetaType.GenTemplateIsIncludeTemplate())
                //{
                //}

                m_StaticIRMetaVariableList.Add(irmv);
                AddMetaMemberVariableIndexBindHashCode(v.GetHashCode(), i);
            }
            //int count = 0;
            //int ssize = 0;
            //for (int i = 0; i < m_MetaTypeList.Count; i++)
            //{
            //    ssize = IR.IRUtil.GetTypeSize(m_MetaTypeList[i]);
            //    count += ssize;
            //    m_ByteCount += ssize;
            //}
        }

        void CreateMemberDataFromMetaData(MetaData md)
        {
            var dataMembers = md.GetMetaMemberDataList()
                .OrderBy(m => m.index)
                .ThenBy(m => m.name, System.StringComparer.Ordinal)
                .ToList();
            int localIndex = 0;
            int staticIndex = 0;
            for (int i = 0; i < dataMembers.Count; i++)
            {
                var mmd = dataMembers[i];
                //if (mmd.isStatic)
                {
                    var irmv = new IRMetaVariable(this, mmd, staticIndex);
                    irmv.SetIsStatic(true);
                    m_StaticIRMetaVariableList.Add(irmv);
                    AddMetaMemberVariableIndexBindHashCode(irmv.id, staticIndex);
                    staticIndex++;
                }
                //else
                {
                    var irmv = new IRMetaVariable(this, mmd, localIndex);
                    irmv.SetIsStatic(false);
                    m_LocalIRMetaVariableList.Add(irmv);
                    AddMetaMemberVariableIndexBindHashCode(irmv.id, localIndex);
                    localIndex++;
                }
            }
        }

        void CreateMemberDataFromMetaEnum(MetaEnum me)
        {
            var ordered = me.metaMemberVariableDict.OrderBy(kv => kv.Key, System.StringComparer.Ordinal).ToList();
            int staticIndex = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                var mv = ordered[i].Value;
                if (mv is MetaMemberEnum mme)
                {
                    var irmv = new IRMetaVariable(this, mme, staticIndex);
                    m_StaticIRMetaVariableList.Add(irmv);
                    AddMetaMemberVariableIndexBindHashCode(irmv.id, staticIndex);
                    staticIndex++;
                }
                else if (mv is MetaMemberVariable mmv)
                {
                    var irmv = new IRMetaVariable(this, mmv, staticIndex);
                    m_StaticIRMetaVariableList.Add(irmv);
                    AddMetaMemberVariableIndexBindHashCode(irmv.id, staticIndex);
                    staticIndex++;
                }
            }
        }
        public void CreateMemberMethod()
        {
            var mc = OwnerMetaClass;
            if (mc == null)
            {
                return;
            }
            var smflist = mc.staticMetaMemberFunctionList;
            //int index = 0;
            for (int i = 0; i < smflist.Count; i++)
            {
                var mf = smflist[i];
                mf.UpdateFunctionName();
                var gmf = IRManager.instance.TranslateIRByFunction(mf);
                m_IRStaticMethodList.Add(gmf);
                IRManager.instance.AddIRMethod(gmf);
            }

            var nonsmflist = mc.nonStaticVirtualMetaMemberFunctionList;
            // Inheritance handling can skip instance methods that are declared in the file but are not
            // considered "virtual overrides" by MetaClass's override-matching rules. For DebugCode/IR
            // we still want those same-level methods to show up.
            // So: translate both:
            // 1) nonStaticVirtualMetaMemberFunctionList (virtual/inherited)
            // 2) fileCollectMetaMemberFunctionList (child-declared instance functions)
            List<MetaMemberFunction> merged = new List<MetaMemberFunction>();
            if (nonsmflist != null)
            {
                merged.AddRange(nonsmflist);
            }
            var fileFuncs = mc.fileCollectMetaMemberFunctionList;
            if (fileFuncs != null)
            {
                for (int i = 0; i < fileFuncs.Count; i++)
                {
                    var mf = fileFuncs[i];
                    if (mf == null) continue;
                    if (mf.isStatic) continue;
                    merged.Add(mf);
                }
            }

            //int index = 0;
            for (int i = 0; i < merged.Count; i++)
            {
                var mf = merged[i];
                if (mf == null) continue;
                // Ensure functionAllName/id is recomputed with the latest parsed param types.
                // Otherwise id may be cached early as Core.Object and overloads may still collide.
                mf.UpdateFunctionName();
                mf.UpdateVritualFunctionName();
                var gmf = IRManager.instance.TranslateIRByFunction(mf);
                if (mf.name == "_add_"
                    || mf.name == "_sub_"
                    || mf.name == "_mul_"
                    || mf.name == "_truediv_"
                    || mf.name == "_mod_"
                    || mf.name == "_iadd_"
                    || mf.name == "_imul_"
                    || mf.name == "_itruediv_"
                    || mf.name == "_lt_"
                    || mf.name == "_le_"
                    || mf.name == "_gt_"
                    || mf.name == "_ge_"
                    || mf.name == "_eq_"
                    || mf.name == "_ne_" )
                {
                    m_IROperatorMethodList.Add(gmf);
                }
                else
                {
                    m_IRNotStaticMethodList.Add(gmf);
                }
                IRManager.instance.AddIRMethod(gmf);
            }
        }
        public void CreateGenMetaTypeTemplateList()
        {
            var mc = OwnerMetaClass;
            if (mc == null)
            {
                return;
            }
            foreach (var v in mc.genMetaTypeTemplateList )
            {
                var nmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(v, this);
                this.m_IRMetaTypeList.Add(nmt);
            }
            m_TemplateCount = this.m_IRMetaTypeList.Count;
            
            if( m_TemplateCount == 0 )
            {
                m_TemplateCount = mc.metaTemplateList.Count;
            }
            else
            {
                if( mc.metaTemplateList.Count > 0 )
                {
                    Debug.Assert(false, "");
                }
            }
        }
        public void CreateTemplateRelation()
        {
            var mc = OwnerMetaClass;
            if (mc == null)
            {
                return;
            }

            foreach ( var v in mc.metaTemplateMapDict )
            {
                IRMetaClass cv = IRManager.instance.GetIRMetaClassById(v.Key.GetHashCode() );

                Debug.Assert(cv != null, "");

                Dictionary<int, IRMetaType> templateMap = new Dictionary<int, IRMetaType >();

                for( int i = 0; i < v.Value.metaTemplateBindDataList.Count; i++ )
                {
                    var mtbd = v.Value.metaTemplateBindDataList[i];
                    templateMap.Add(mtbd.sourceTemplate.index, IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList( mtbd.targetMetaType, this ) );
                }
                this.m_IRMetaClassMapTemplateDict.Add(cv.id, templateMap);
            }
        }
        public List<IRData> CreateStaticMetaMetaVariableIRList()
        {
            IRMetaType irmt = new IRMetaType(this, this.m_IRMetaTypeList);

            List<IRData> list = new List<IRData>();

            //foreach( var v in m_LocalIRMetaVariableList )
            //{
            //    if(v.express == null )
            //    {
            //        continue;
            //    }
            //    if (v.express is MetaNewObjectExpressNode mnoe)
            //    {
            //        IRNewExpress irexp = new IRNewExpress(null, mnoe);
            //        list.AddRange(irexp.IRDataList);
            //    }
            //    else
            //    {
            //        var irexp = IRExpressManager.CreateExpress(null, v.express);
            //        list.AddRange(irexp.IRDataList);

            //    }

            //    IRData irdata = new IRData();
            //    irdata.id = list.Count;
            //    irdata.opValue = irmt;
            //    // for instance member default init we use StoreNotStaticField1
            //    irdata.opCode = EIROpCode.StoreNotStaticField1;
            //    irdata.index = v.index;

            //    list.Add(irdata);
            //}

            // Also process static member variables so their initialization expressions are
            // converted to IR (this ensures string constants and other consts are collected
            // by AddStringIRStack when IRExpress is created). Static variables that belong
            // to Project-like classes are kept in m_StaticIRMetaVariableList and need
            // to be handled here as well.
            foreach (var v in m_StaticIRMetaVariableList)
            {
                if (v.express == null)
                    continue;

                if (v.express is MetaNewObjectExpressNode mnoe2)
                {
                    IRNewExpress irexp2 = new IRNewExpress(null, mnoe2);
                    list.AddRange(irexp2.IRDataList);
                }
                else
                {
                    var irexp2 = IRExpressManager.CreateExpress(null, v.express);
                    list.AddRange(irexp2.IRDataList);
                }

                IRData irdata2 = new IRData();
                irdata2.id = list.Count;
                // store static field: opValue carries the field type
                irdata2.opValue = irmt;
                irdata2.opCode = EIROpCode.StoreStaticField;
                irdata2.index = v.index;
                irdata2.debugStaticOwnerIrName = this.irName;
                list.Add(irdata2);
            }

            return list;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(this.m_IRName);

            return sb.ToString();
        }
    }
}
