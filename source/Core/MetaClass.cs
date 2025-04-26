//****************************************************************************
//  File:      MetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: Meta class's attribute
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Reflection;

namespace SimpleLanguage.Core
{
    public partial class MetaClass : MetaBase
    {
        public MetaNamespace topLevelMetaNamespace
        {
            get
            {
                if (parentNode == null) return null;
                return parentNode as MetaNamespace;
            }
        }
        public MetaClass topLevelMetaClass
        {
            get
            {
                if (parentNode == null) return null;
                return parentNode as MetaClass;
            }

        }
        public EType eType => m_Type;
        public bool isInnerDefineInCompile => m_IsInnerDefineCompile;
        public MetaClass extendClass => m_ExtendClass;
        public List<MetaClass> interfaceClass => m_InterfaceClass;
        public bool isTemplateClass { get { return m_MetaTemplateList.Count > 0; } }        //是否是模版类
        public virtual bool isGenTemplate { get { return false; } }
        public List<MetaTemplate> metaTemplateList => m_MetaTemplateList;   
        public MetaExpressNode defaultExpressNode => m_DefaultExpressNode;
        public Dictionary<string, MetaMemberVariable> allMetaMemberVariableDict
        {
            get
            {
                Dictionary<string, MetaMemberVariable> allMetaMemberVariableDict = new Dictionary<string, MetaMemberVariable>(m_MetaMemberVariableDict);
                allMetaMemberVariableDict = allMetaMemberVariableDict.Concat(m_MetaExtendMemeberVariableDict).ToDictionary(k => k.Key, v => v.Value);
                return allMetaMemberVariableDict;
            }
        }
        public List<MetaMemberVariable> allMetaMemberVariableList
        {
            get
            {
                List<MetaMemberVariable> allMetaMemberVariableList = new List< MetaMemberVariable>(m_MetaMemberVariableDict.Count + m_MetaExtendMemeberVariableDict.Count);

                foreach (var v in allMetaMemberVariableDict)
                {
                    allMetaMemberVariableList.Add(v.Value);
                }
                foreach (var v in m_MetaExtendMemeberVariableDict)
                {
                    allMetaMemberVariableList.Add(v.Value);
                }
                return allMetaMemberVariableList;
            }
        }

        public Dictionary<string, MetaMemberVariable> metaMemberVariableDict => m_MetaMemberVariableDict;
        public Dictionary<string, MetaMemberVariable> metaExtendMemeberVariableDict => m_MetaExtendMemeberVariableDict;
        public Dictionary<Token, FileMetaClass> fileMetaClassDict => m_FileMetaClassDict;
        public Dictionary<string, List<MetaMemberFunction>> metaMemberFunctionListDict => m_MetaMemberFunctionListDict;
        public List<MetaGenTemplateClass> metaGenTemplateClassList => m_MetaGenTemplateClassList;
        public bool isHandleExtendVariableDirty { get; set; } = false;


        protected EType m_Type = EType.None;
        protected Dictionary<Token, FileMetaClass> m_FileMetaClassDict = new Dictionary<Token, FileMetaClass>();
        protected MetaClass m_ExtendClass  = null;
        protected List<MetaClass> m_InterfaceClass = new List<MetaClass>();

        protected List<MetaTemplate> m_MetaTemplateList = new List<MetaTemplate>();
        protected List<MetaGenTemplateClass> m_MetaGenTemplateClassList = new List<MetaGenTemplateClass>();
        protected Dictionary<string, MetaClass> m_ChildrenMetaClassDict = new Dictionary<string, MetaClass>();
        protected Dictionary<string, MetaMemberVariable> m_MetaMemberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected Dictionary<string, MetaMemberVariable> m_MetaExtendMemeberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected Dictionary<string, MetaMemberFunction> m_MetaMemberAllNameFunctionDict = new Dictionary<string, MetaMemberFunction>();
        protected Dictionary<string, List<MetaMemberFunction>> m_MetaMemberFunctionListDict = new Dictionary<string, List<MetaMemberFunction>>();
        protected List<MetaMemberFunction> m_TempInnerFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        protected MetaExpressNode m_DefaultExpressNode = null;
        protected bool m_IsInnerDefineCompile = false;

        public MetaClass(string _name, EType _type  = EType.Class )
        {
            m_Name = _name;
            m_Type = _type;
        }
        public MetaClass( MetaClass mc )
        {
            m_Name = mc.m_Name;
            m_Type = mc.m_Type;
            m_FileMetaClassDict = mc.m_FileMetaClassDict;
            m_ExtendClass = mc.m_ExtendClass;
            m_InterfaceClass = mc.m_InterfaceClass;
            m_ChildrenMetaClassDict = mc.m_ChildrenMetaClassDict;

            m_MetaMemberVariableDict = mc.m_MetaMemberVariableDict;
            m_MetaExtendMemeberVariableDict = mc.m_MetaExtendMemeberVariableDict;
            m_MetaMemberFunctionListDict = mc.m_MetaMemberFunctionListDict;
            m_MetaMemberAllNameFunctionDict = mc.m_MetaMemberAllNameFunctionDict;
            m_DefaultExpressNode = mc.m_DefaultExpressNode;
        }
        public override void SetDeep( int deep )
        {
            m_Deep = deep;

            foreach( var v in m_ChildrenMetaClassDict )
            {
                v.Value.SetDeep(deep + 1);
            }

            foreach( var v in m_MetaMemberVariableDict )
            {
                v.Value.SetDeep(deep + 1);
            }
            foreach( var v in m_MetaMemberAllNameFunctionDict)
            {
                v.Value.SetDeep(deep + 1);
            }
        }
        public void AllFunctionTranslateIR()
        {
            foreach( var v in m_MetaMemberAllNameFunctionDict )
            {
                v.Value.TranslateIR();
            }
        }       
        public void SetDefaultExpressNode( MetaExpressNode defaultExpressNode )
        {
            m_DefaultExpressNode = defaultExpressNode;
        }
        public virtual void ParseInnerVariable()
        {
        }
        public virtual void ParseInnerFunction()
        {
        }
        public void ParseInner()
        {
            ParseCSharp();

            ParseInnerVariable();
            ParseInnerFunction();

        }
        public void ParseExtendsRelation()
        {
            if( this.isInnerDefineInCompile )
            {
                return;
            }
            if (this.extendClass != null)
            {
                Console.WriteLine("已绑定过了继承类 : " + extendClass.name);
                return;
            }
            foreach( var v in m_FileMetaClassDict )
            {
                var mc = v.Value;
                MetaClass getmc = mc.GetExtendMetaClass();
                if (getmc != null)
                {
                    mc.metaClass.SetExtendClass(getmc);
                }
                else
                {
                    mc.metaClass.SetExtendClass(CoreMetaClassManager.objectMetaClass);
                }
            }
        }
        public void HandleExtendData()
        {
        }
        public void ParseTemplateRelation()
        {
        }
        public void ParseMetaMemberVariableName()
        {
            foreach (var it in m_MetaMemberVariableDict)
            {
                it.Value.ParseName();
            }
        }
        public void ParseMetaMemberFunctionName()
        {
            foreach (var it in m_MetaMemberFunctionListDict )
            {
                foreach( var it2 in it.Value )
                {
                    it2.ParseName();
                }
            }
        }
        public void ParseMetaMemberVariableDefineType()
        {
            foreach (var it in m_MetaMemberVariableDict)
            {
                it.Value.ParseReturnMetaType();
            }
        }
        public void ParseMetaMemberFunctionDefineType()
        {
            foreach (var it in m_MetaMemberFunctionListDict)
            {
                foreach( var it2 in it.Value )
                {
                    it2.ParseReturnMetaType();
                }
            }
        }
        public void CheckInterface()
        {

        }
        public void ParseMetaInConstraint()
        {
            foreach (var it in m_MetaTemplateList)
            {
                it.ParseInConstraint();
            }
        }
#if EditorMode
        public void BindFileMetaClass(FileMetaClass fmc)
        {
            if (m_FileMetaClassDict.ContainsKey(fmc.token))
            {
                return;
            }
            fmc.SetMetaClass( this );
            m_FileMetaClassDict.Add(fmc.token, fmc);
        }
        public void ParseFileMetaClassTemplate( FileMetaClass fmc )
        {
            for (int i = 0; i < fmc.templateParamList.Count; i++)
            {
                string tTemplateName = fmc.templateParamList[i].name;
                if ( m_MetaTemplateList.Find(a => a.name == tTemplateName) != null)
                {
                    Console.WriteLine("Error 定义模式名称重复!!");
                }
                else
                {
                    m_MetaTemplateList.Add(new MetaTemplate( this, fmc.templateParamList[i]));
                }
            }
        }
        public void ParseFileMetaClassMemeberVarAndFunc( FileMetaClass fmc )
        {
            bool isHave = false;
            foreach (var v2 in fmc.memberVariableList)
            {
                MetaBase mb = GetChildrenMetaBaseByName(v2.name);
                if (mb != null)
                {
                    MetaMemberVariable cmmv = mb as MetaMemberVariable;
                    if(cmmv != null && cmmv.isInnerDefine )
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Error 已有定义类: " + allName + "中 已有: " + v2.token?.ToLexemeAllString() + "的元素!!");
                    }
                    isHave = true;
                }
                else
                    isHave = false;
                MetaMemberVariable mmv = new MetaMemberVariable(this, v2);
                if (isHave)
                {
                    mmv.SetName(mmv.name + "__repeat__");
                }
                AddMetaMemberVariable(mmv);
            }
            foreach (var v2 in fmc.memberFunctionList)
            {
                MetaMemberFunction mmf = new MetaMemberFunction(this, v2 );
                AddMetaMemberFunction(mmf);
            }            
        }
        public bool CompareInputTemplateList( MetaInputTemplateCollection mitc )
        {
            if( mitc == null || mitc?.metaTemplateParamsList?.Count == 0 )
            {
                if (this.metaTemplateList.Count == 0)
                    return true;
                return false;
            }
            if( mitc.metaTemplateParamsList.Count == this.metaTemplateList.Count )
            {
                for( int i = 0; i < mitc.metaTemplateParamsList.Count; i++ )
                {
                    var mtpl = mitc.metaTemplateParamsList[i];
                    var ctpl = this.metaTemplateList[i];
                    return true;
                }
            }
            return false;
        }
        //解析 自动构建函数  
        public virtual void ParseDefineComplete()
        {
            AddDefineConstructFunction();

            if(m_DefaultExpressNode == null && isTemplateClass == false )
            {
                MetaType mdt = new MetaType(this);
                var defaultFunction = GetMetaMemberConstructDefaultFunction();
                MetaMethodCall mfc = null;
                if (defaultFunction != null)
                {
                    mfc = new MetaMethodCall(this, defaultFunction );
                }
                m_DefaultExpressNode = new MetaNewObjectExpressNode(mdt, this, null, mfc);
            }

            List<MetaMemberFunction> addList = new List<MetaMemberFunction>();
            for( int i = 0; i < m_TempInnerFunctionList.Count; i++ ) 
            {
                MetaMemberFunction mmf = m_TempInnerFunctionList[i];

                bool isAdd = true;
                if (m_MetaMemberFunctionListDict.ContainsKey(mmf.name))
                {
                    var list = m_MetaMemberFunctionListDict[mmf.name];
                    MetaMemberFunction curFun = IsSameMetaMemeberFunction(list, mmf);
                    if (curFun != null)
                    {
                        isAdd = false;
                        if (mmf.isCanRewrite)
                        {
                            int index = list.IndexOf(curFun);
                            list[index] = mmf;
                        }
                        else
                        {
                            RemoveMetaMemberFunction(curFun);
                            isAdd = true;
                            break;
                        }
                    }
                }                
                if (isAdd)
                {
                    addList.Add(mmf);
                }
            }
            for( int i = 0; i < addList.Count; i++ )
            {
                AddMetaMemberFunction(addList[i]);
            }
            m_TempInnerFunctionList.Clear();
        }
#endif
        public MetaTemplate GetTemplateMetaClassByName(string _name)
        {
            return m_MetaTemplateList.Find(a => a.name == _name);
        }
        public bool IsTemplateMetaClassByName(string _name)
        {
            return m_MetaTemplateList.Exists(a => a.name == _name);
        }
        public void AddChildrenMetaClass(MetaClass mc)
        {
            if ( m_ChildrenMetaClassDict.ContainsKey(mc.name))
            {
                return;
            }
            m_ChildrenMetaClassDict.Add(mc.name, mc);
            AddMetaBase(mc.name, mc);
        }
        public MetaClass GetChildrenMetaClass( string name )
        {
            if( m_ChildrenMetaClassDict.ContainsKey( name ) )
            {
                return m_ChildrenMetaClassDict[name];
            }
            return null;
        }
        public void AddGenTemplateMetaClass(MetaGenTemplateClass mtc )
        {
            m_MetaGenTemplateClassList.Add(mtc);
        }
        public MetaGenTemplateClass GenerateTemplateClass( MetaInputTemplateCollection mic )
        {
            if (isTemplateClass == false)
            {
                Console.WriteLine("Error 该类不是模版类,不能生成模版生成类!!");
                return null;
            }
            if(mic == null )
            {
                return null;
            }
            if (metaTemplateList.Count == mic.metaTemplateParamsList.Count)
            {
                MetaGenTemplateClass tmc = new MetaGenTemplateClass(this);
                AddGenTemplateMetaClass(tmc);

                for (int i = 0; i < metaTemplateList.Count; i++)
                {
                    var classTemplate = metaTemplateList[i];
                    var inputTemplate = mic.metaTemplateParamsList[i];

                    MetaGenTemplate mgt = new MetaGenTemplate(classTemplate, inputTemplate);
                    tmc.AddMetaGenTemplate(mgt);
                }
                tmc.UpdateGenMember();
                tmc.SetDeep(m_Deep + 1);

                return tmc;
            }
            else
            {
                Console.WriteLine("Error 传进来的模版参数与类定义的参数长度对不上!!");
                return null;
            }
        }
        public MetaGenTemplateClass GetGenTemplateMetaClassIfNotThenGenTemplateClass(MetaInputTemplateCollection mtic )
        {
            MetaGenTemplateClass mtc = GetGenTemplateMetaClass(mtic);
            if( mtc == null )
            {
                mtc = GenerateTemplateClass(mtic);
            }
            if( mtc == null )
            {
                Console.WriteLine("Error 没有找到合适的Template");
            }
            return mtc;
        }
        public MetaGenTemplateClass GetGenTemplateMetaClass( MetaInputTemplateCollection mitc )
        {
            foreach (var item in m_MetaGenTemplateClassList)
            {
                if (item.Adapter(mitc))
                {
                    return item;
                }
            }
            return null;
        }
        public MetaClass GetChildrenMetaClassByName( string name )
        {
            if( m_ChildrenMetaClassDict.ContainsKey(name ) )
            {
                return m_ChildrenMetaClassDict[name];
            }
            return null;
        }
        public void SetExtendClass(MetaClass sec)
        {
            m_ExtendClass = sec;
        }
        public bool IsParseMetaClass(MetaClass parentClass, bool isIncludeSelf = true )
        {
            MetaClass mc = isIncludeSelf ? this : this.m_ExtendClass;
            while( mc != null )
            {
                if (mc == CoreMetaClassManager.objectMetaClass)
                    break;
                
                if( mc == parentClass)
                {
                    return true;
                }
                mc = mc.m_ExtendClass;
            }
            return false;
        }
        public void AddInterfaceClass(MetaClass aic)
        {
            if (!m_InterfaceClass.Contains(aic))
            {
                m_InterfaceClass.Add(aic);
            }
        }
        public void AddMetaMemberVariable( MetaMemberVariable mmv, bool isAddManager = true )
        {
            if( m_MetaMemberVariableDict.ContainsKey( mmv.name ) )
            {
                return;
            }
            m_MetaMemberVariableDict.Add(mmv.name, mmv);
            AddMetaBase(mmv.name, mmv);
            if( isAddManager )
            {
                MetaVariableManager.instance.AddMetaMemberVariable(mmv);
            }
        }
        public MetaMemberFunction IsSameMetaMemeberFunction(List<MetaMemberFunction> list, MetaMemberFunction mmf)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var curFun = list[i];
                if( curFun.metaMemberParamCollection.IsEqualMetaDefineParamCollection( mmf.metaMemberParamCollection ) )
                {
                    return curFun;
                }
            }
            return null;
        }
        public void AddInnerMetaMemberFunction( MetaMemberFunction mmf )
        {
            m_TempInnerFunctionList.Add(mmf);
        }
        public void AddMetaMemberFunction(MetaMemberFunction mmf, bool isAddMethod = true )
        {
            if(m_MetaMemberFunctionListDict.ContainsKey(mmf.name ) )
            {
                var list = m_MetaMemberFunctionListDict[mmf.name];

                if( IsSameMetaMemeberFunction(list, mmf) == null )
                {
                    list.Add(mmf);
                    m_MetaMemberAllNameFunctionDict.Add(mmf.functionAllName, mmf);
                }
            }
            else
            {
                var list = new List<MetaMemberFunction>();
                list.Add(mmf);
                m_MetaMemberFunctionListDict.Add(mmf.name, list);
                m_MetaMemberAllNameFunctionDict.Add(mmf.functionAllName, mmf);
            }
            AddMetaBase(mmf.functionAllName, mmf);

            if( isAddMethod )
            {
                MethodManager.instance.AddMemeberFunction(mmf);
            }
            else
            {
                MethodManager.instance.AddDynamicMemberFunction(mmf);
            }
        }
        public void RemoveMetaMemberFunction( MetaMemberFunction mmf )
        {
            if (m_MetaMemberFunctionListDict.ContainsKey(mmf.name))
            {
                var list = m_MetaMemberFunctionListDict[mmf.name];

                list.Remove(mmf);
            }
            RemoveMetaBase(mmf);

            //MethodManager.instance.AddMemeberFunction(mmf);
        }
        public void AddDefineConstructFunction()
        {
            MetaMemberFunction mmf = GetMetaMemberConstructDefaultFunction();
            if( mmf == null )
            {
                mmf = new MetaMemberFunction(this, "__Init__");
                mmf.SetDefineMetaClass(this);
                AddMetaMemberFunction(mmf);
            }
        }
        public void HandleExtendClassVariable()
        {
            isHandleExtendVariableDirty = true;
            if ( m_ExtendClass != null)
            {
                foreach (var v in m_ExtendClass.m_MetaMemberVariableDict)
                {
                    m_MetaExtendMemeberVariableDict.Add(v.Key,v.Value);
                }
            }
        }
        public virtual MetaMemberVariable GetMetaMemberVariableByName(string name)
        {
            if (m_MetaMemberVariableDict.ContainsKey(name))
            {
                return m_MetaMemberVariableDict[name];
            }
            if (m_MetaExtendMemeberVariableDict.ContainsKey(name))
            {
                return m_MetaExtendMemeberVariableDict[name];
            }
            return null;
        }
        public List<MetaMemberVariable> GetMetaMemberVariableListByFlag( bool isStatic, bool isConst )
        {
            List<MetaMemberVariable> mmvList = new List<MetaMemberVariable>();
            MetaMemberVariable tempMmv = null;
            foreach (var v in m_MetaMemberVariableDict)
            {
                tempMmv = v.Value;
                if( (tempMmv.isStatic == isStatic) && (tempMmv.isConst==isConst) )
                {
                    mmvList.Add(tempMmv);
                }
            }
            foreach (var v in m_MetaExtendMemeberVariableDict)
            {
                tempMmv = v.Value;
                if ((tempMmv.isStatic == isStatic) && (tempMmv.isConst == isConst))
                {
                    mmvList.Add(tempMmv);
                }
            }
            return mmvList;
        }
        public virtual MetaMemberFunction GetMetaDefineGetSetMemberFunctionByName( string name, bool isGet , bool isSet )
        {
            if (!m_MetaMemberFunctionListDict.ContainsKey(name))
            {
                if (m_ExtendClass != null)
                {
                    var func = m_ExtendClass.GetMetaDefineGetSetMemberFunctionByName(name, isGet, isSet);
                    if (func != null)
                    {
                        return func;
                    }
                }
                return null;
            }
            var mmf = m_MetaMemberFunctionListDict[name];

            for (int i = 0; i < mmf.Count; i++)
            {
                var fun = mmf[i];
                if ( (fun.isGet == isGet) || (fun.isSet == isSet ) )
                    return fun;
            }
            return null;
        }
        public virtual MetaMemberFunction GetMetaMemberFunctionByNameAndInputParamCollect(string name, MetaInputParamCollection mmpc, bool isIncludeExtendClass = true )
        {
            if (!m_MetaMemberFunctionListDict.ContainsKey(name))
            {
                if (isIncludeExtendClass  && m_ExtendClass != null )
                {
                    var func = m_ExtendClass.GetMetaMemberFunctionByNameAndInputParamCollect(name, mmpc, isIncludeExtendClass );
                    if (func != null)
                    {
                        return func;
                    }
                }
                return null;
            }
            var mmf = m_MetaMemberFunctionListDict[name];

            for (int i = 0; i < mmf.Count; i++)
            {
                var fun = mmf[i];
                if (fun.IsEqualMetaInputParamCollection(mmpc))
                    return fun;
            }
            return null;
        }
        //该方法，只能查找Cast<T1>() 模版函数使用 不能用Class<T>{ Fun() } 这种的
        //暂不支持使用模版方法的查找
        //public MetaMemberFunction GetMetaMemberFunctionByNameAndTemplateCollectInputParamCollect(string name, MetaInputTemplateCollection mitc, MetaInputParamCollection mmpc)
        //{
        //    if (!m_MetaMemberFunctionListDict.ContainsKey(name))
        //    {
        //        if ( m_ExtendClass != null)
        //        {
        //            var func = m_ExtendClass.GetMetaMemberFunctionByNameAndTemplateCollectInputParamCollect(name, mitc, mmpc);
        //            if (func != null)
        //            {
        //                return func;
        //            }
        //        }
        //        return null;
        //    }
           

        //    var mmf = m_MetaMemberFunctionListDict[name];

        //    for (int i = 0; i < mmf.Count; i++)
        //    {
        //        var fun = mmf[i];
        //        if (fun.IsEqualMetaTemplateCollectionAndMetaParamCollection(mitc, mmpc))
        //            return fun;
        //    }
        //    return null;
        //}
        public MetaMemberFunction GetMetaMemberConstructDefaultFunction()
        {
            return GetMetaMemberConstructFunction(null);
        }
        public virtual MetaMemberFunction GetMetaMemberConstructFunction( MetaInputParamCollection mmpc )
        {
            return GetMetaMemberFunctionByNameAndInputParamCollect("__Init__", mmpc, false );
        }
        public MetaMemberFunction GetFirstMetaMemberFunctionByName( string name )
        {
            return GetMetaMemberFunctionByNameAndInputParamCollect( name, null );
        }
        public MetaBase GetMetaBaseByTopLevel( string _name )
        {
            if ( m_ChildrenMetaClassDict.ContainsKey(_name))
                return m_ChildrenMetaClassDict[_name];

            MetaBase parentMB = parentNode;
            while( true )
            {
                if ( parentMB != null )
                {
                    var rmb = parentMB.GetChildrenMetaBaseByName(_name);
                    if (rmb != null) return rmb;

                    parentMB = parentMB.parentNode;
                }
                else
                    break;
            }

            return null;
        }
        public List<MetaMemberFunction> GetMemberFunctionList()
        {
            List<MetaMemberFunction> mmf = new List<MetaMemberFunction>();

            foreach( var v in m_MetaMemberAllNameFunctionDict )
            {
                mmf.Add(v.Value);
            }
            return mmf;
        }
        public List<MetaMemberFunction> GetMemberInterfaceFunction()
        {
            List<MetaMemberFunction> mmf = new List<MetaMemberFunction>();

            foreach( var v in m_MetaMemberAllNameFunctionDict )
            {
                var fun = v.Value;
                if (fun.isWithInterface)
                {
                    mmf.Add(fun);
                }
            }
            return mmf;
        }
        public bool GetMemberInterfaceFunctionByFunc( MetaMemberFunction func )
        {
            foreach( var v in m_MetaMemberAllNameFunctionDict)
            {
                if (v.Value.Equals(func))
                {
                    return true;
                }
            }
            return true;
        }
        public override string ToFormatString()
        {
            return GetFormatString(false);
        }
        public virtual string ToDefineTypeString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(name);

            return stringBuilder.ToString();

        }
        public string GetFormatString( bool isShowNamespace )
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Clear();
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append(permission.ToFormatString());
            stringBuilder.Append(" ");
            if(isShowNamespace)
            {
                stringBuilder.Append("class ");
                if ( topLevelMetaNamespace != null )
                {
                    stringBuilder.Append(topLevelMetaNamespace.allName + ".");
                }
                stringBuilder.Append( name);
            }
            else
            {
                stringBuilder.Append("class " + name);
            }
            if (m_MetaTemplateList.Count > 0)
            {
                stringBuilder.Append("<");
                for (int i = 0; i < m_MetaTemplateList.Count; i++)
                {
                    stringBuilder.Append(m_MetaTemplateList[i].ToFormatString());
                    if (i < m_MetaTemplateList.Count - 1)
                    {
                        stringBuilder.Append(",");
                    }
                }
                stringBuilder.Append(">");
            }
            if ( m_ExtendClass != null )
            {
                stringBuilder.Append(" extends ");
                stringBuilder.Append(m_ExtendClass.allName);
                var mtl = m_ExtendClass.metaTemplateList;
                if( mtl.Count > 0 )
                {
                    stringBuilder.Append("<");
                    for (int i = 0; i < mtl.Count; i++)
                    {
                        stringBuilder.Append(mtl[i].ToFormatString());
                        if (i < mtl.Count - 1)
                        {
                            stringBuilder.Append(",");
                        }
                    }
                    stringBuilder.Append(">");
                }
            }
            if( m_InterfaceClass.Count > 0 )
            {
                stringBuilder.Append(" interface ");
            }
            for( int i = 0; i < m_InterfaceClass.Count; i++ )
            {
                stringBuilder.Append(m_InterfaceClass[i].allName);
                if( i != m_InterfaceClass.Count - 1 )
                    stringBuilder.Append(",");
            }
            stringBuilder.Append(Environment.NewLine);

            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("{" + Environment.NewLine);

            foreach (var v in childrenNameNodeDict)
            {
                MetaBase mb = v.Value;
                if (mb is MetaClass)
                {
                    if (mb is MetaEnum)
                    {
                        stringBuilder.Append((mb as MetaEnum).ToFormatString());
                        stringBuilder.Append(Environment.NewLine);
                    }
                    else
                    {
                        stringBuilder.Append((mb as MetaClass).ToFormatString());
                        stringBuilder.Append(Environment.NewLine);
                    }
                }
                else if (mb is MetaMemberVariable)
                {
                    MetaMemberVariable mmv = mb as MetaMemberVariable;
                    if( mmv.fromType == EFromType.Code )
                    {
                        stringBuilder.Append(mmv.ToFormatString());
                        stringBuilder.Append(Environment.NewLine);
                    }
                }
                else if (mb is MetaMemberFunction)
                {
                    MetaMemberFunction mmfc = mb as MetaMemberFunction;
                    if( mmfc.methodCallType == EMethodCallType.Local )
                    {
                        stringBuilder.Append(mmfc.ToFormatString());
                        stringBuilder.Append(Environment.NewLine);
                    }
                }
                else
                {
                    stringBuilder.Append("Errrrrroooorrr ---" + mb.ToFormatString());
                    stringBuilder.Append(Environment.NewLine);
                }
            }
            stringBuilder.Append(Environment.NewLine);
            if( m_MetaGenTemplateClassList.Count > 0 )
            {
                for (int i = 0; i <= realDeep; i++)
                    stringBuilder.Append(Global.tabChar);
                stringBuilder.AppendLine("------------Generator Template List-------------");
                for (int i = 0; i < m_MetaGenTemplateClassList.Count; i++)
                {
                    stringBuilder.Append(m_MetaGenTemplateClassList[i].ToFormatString());
                    stringBuilder.Append(Environment.NewLine);
                }
            }

            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("}" + Environment.NewLine);

            return stringBuilder.ToString();
        }
    }
}
