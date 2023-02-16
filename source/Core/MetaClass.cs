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

namespace SimpleLanguage.Core
{
    public partial class MetaClass : MetaBase
    {
        public class MetaClassData
        {
            public List<MetaMemberVariable> metaMemberVariables => m_MetaMemberVariables;

            public int allocSize = 0;
            List<MetaMemberVariable> m_MetaMemberVariables = new List<MetaMemberVariable>();
            public List<EType> m_MetaTypeList = new List<EType>();
            public int byteCount = 0;

            public int GetMemberVariableIndex( MetaMemberVariable mmv )
            {
                for( int i = 0; i < m_MetaMemberVariables.Count; i++ )
                {
                    if (m_MetaMemberVariables[i] == mmv)
                        return i;
                }
                return -1;
            }
            public void SetRealMetaMemberVariable( List<MetaMemberVariable> mmvList )
            {
                m_MetaMemberVariables = mmvList;
            }

            [Conditional("DEBUG")]           
            public void CalcAllocSize()
            {
                m_MetaTypeList.Clear();
                foreach (var v in m_MetaMemberVariables)
                {
                    m_MetaTypeList.Add(v.metaDefineType.metaClass.eType);
                }
                int count = 0;
                int ssize = 0;
                for(int i = 0; i < m_MetaTypeList.Count; i++ )
                {
                    ssize = GetTypeSize( m_MetaTypeList[i] );                    
                    count += ssize;
                    byteCount += ssize;
                }
            }
            public int GetTypeSize( EType etype )
            {
                switch (etype)
                {
                    case EType.Bit:
                        return 1;
                    case EType.Byte:
                    case EType.Boolean:
                        return 1;
                    case EType.Char:
                        return 2;
                    case EType.Int16:
                    case EType.UInt16:
                        return 2;
                    case EType.Int32:
                    case EType.UInt32:
                    case EType.Class:
                    case EType.String:
                    case EType.Float:
                        return 4;
                    case EType.Int64:
                    case EType.UInt64:
                    case EType.Double:
                        return 8;
                    case EType.Int128:
                    case EType.UInt128:
                    case EType.Decimal:
                        return 16;

                }

                return 1;
            }
        }
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
        public int GetSize()
        {
            return 100;
        }

        MetaClassData m_MetaClassData = new MetaClassData();

        public MetaClassData metaClassData => m_MetaClassData;
        public EType eType => m_Type;
        public MetaClass extendClass => m_ExtendClass;
        public List<MetaClass> interfaceClass => m_InterfaceClass;
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
        public bool isHandleExtendVariableDirty { get; set; } = false;


        protected Dictionary<Token, FileMetaClass> m_FileMetaClassDict = new Dictionary<Token, FileMetaClass>();
        protected MetaClass m_ExtendClass  = null;
        protected List<MetaClass> m_InterfaceClass = new List<MetaClass>();

        protected List<MetaTemplate> m_MetaTemplateList = new List<MetaTemplate>();
        protected Dictionary<string, MetaClass> m_ChildrenMetaClassDict = new Dictionary<string, MetaClass>();
        protected Dictionary<string, MetaMemberVariable> m_MetaMemberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected Dictionary<string, MetaMemberVariable> m_MetaExtendMemeberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected Dictionary<string, MetaMemberFunction> m_MetaMemberAllNameFunctionDict = new Dictionary<string, MetaMemberFunction>();
        protected Dictionary<string, List<MetaMemberFunction>> m_MetaMemberFunctionListDict = new Dictionary<string, List<MetaMemberFunction>>();
        protected MetaExpressNode m_DefaultExpressNode = null;
        protected EType m_Type = EType.None;

        public MetaClass(string _name, EType _type  = EType.Class )
        {
            m_Name = _name;
            m_Type = _type;
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
        public void Parse()
        {
            ParseCSharp();

            ParseInnerVariable();
            ParseInnerFunction();

            foreach ( var it in m_MetaMemberVariableDict)
            {
                it.Value.Parse();
            }

            foreach ( var it in m_MetaMemberAllNameFunctionDict)
            {
                it.Value.Parse();
            }
            foreach( var it in m_MetaTemplateList)
            {
                it.ParseInConstraint();
            }
        }
#if EditorMode
        public virtual void BindFileMetaClass(FileMetaClass fmc)
        {
            if (m_FileMetaClassDict.ContainsKey(fmc.token))
            {
                return;
            }
            fmc.SetMetaClass( this );
            m_FileMetaClassDict.Add(fmc.token, fmc);

            for( int i = 0; i < fmc.templateParamList.Count; i++ )
            {
                string tTemplateName = fmc.templateParamList[i].name;
                if(m_MetaTemplateList.Find( a=> a.name == tTemplateName ) != null )
                {
                    Console.WriteLine("Error 定义模式名称重复!!");
                }
                else
                {
                    m_MetaTemplateList.Add( new MetaTemplate( this, fmc.templateParamList[i]) );
                }
            }

            bool isHave = false;
            foreach (var v in fmc.memberVariableList)
            {
                MetaBase mb = GetChildrenMetaBaseByName(v.name);
                if (mb != null)
                {
                    Console.WriteLine("Error 已有定义类: " + allName + "中 已有: " + v.token?.ToLexemeAllString() + "的元素!!");
                    isHave = true;
                }
                else
                    isHave = false;
                MetaMemberVariable mmv = new MetaMemberVariable( this, v );
                if( isHave )
                {
                    mmv.SetName( mmv.name + "__repeat__" );
                }
                AddMetaMemberVariable(mmv);
            }
            foreach (var v in fmc.memberFunctionList)
            {
                MetaMemberFunction mmf = new MetaMemberFunction( this, v );
                AddMetaMemberFunction(mmf);
            }
        }
        public bool CompareInputTemplateList( MetaInputTemplateCollection mitc )
        {
            if( mitc == null || mitc?.metaTemplateParamsList?.Count == 0 )
            {
                if (this.metaTemplateList.Count == 0)
                    return true;
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
        public virtual void ParseDefineComplete()
        {
            AddDefineConstructFunction();

            if(m_DefaultExpressNode == null )
            {
                MetaType mdt = new MetaType(this);
                var cdf = new MetaFunctionCall( this, GetMetaMemberConstructDefaultFunction());
                m_DefaultExpressNode = new MetaNewObjectExpressNode(mdt, this, null, cdf );
            }

            CreateMetaClassData();
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
        public void AddMetaMemberFunction(MetaMemberFunction mmf, bool isAddMethod = true )
        {
            if (!m_MetaMemberAllNameFunctionDict.ContainsKey(mmf.functionAllName))
            {
                m_MetaMemberAllNameFunctionDict.Add(mmf.functionAllName, mmf);
            }
            if(m_MetaMemberFunctionListDict.ContainsKey(mmf.name ) )
            {
                var list = m_MetaMemberFunctionListDict[mmf.name];

                MetaMemberFunction finded = list.Find(a => a.functionAllName == mmf.functionAllName);
                if( finded == null )
                {
                    list.Add(mmf);
                }
            }
            else
            {
                var list = new List<MetaMemberFunction>();
                list.Add(mmf);
                m_MetaMemberFunctionListDict.Add(mmf.name, list);
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
        public void AddDefineConstructFunction()
        {
            MetaMemberFunction mmf = GetMetaMemberConstructDefaultFunction();
            if( mmf == null )
            {
                //mmf = new MetaMemberFunction(this, "Init");
                //mmf.SetDefineMetaClass(this);
                //AddMetaMemberFunction(mmf);
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
        public void CreateMetaClassData()
        {
            var resList = GetMetaMemberVariableListByFlag(false, false);
            m_MetaClassData.SetRealMetaMemberVariable(resList);
            //m_MetaClassData.CalcAllocSize();
        }
        public MetaVariable GetMetaMemberByAllName( string name )
        {
            if(m_MetaMemberVariableDict.ContainsKey(name ) )
            {
                return m_MetaMemberVariableDict[name];
            }
            if(m_MetaExtendMemeberVariableDict.ContainsKey( name ))
            {
                return m_MetaExtendMemeberVariableDict[name];
            }
            foreach( var v in m_MetaMemberAllNameFunctionDict)
            {
                if (v.Value.name == name)
                    return v.Value;
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
        public MetaMemberVariable GetMetaMemberVariableByName( string name )
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
        public MetaMemberFunction GetMetaDefineGetSetMemberFunctionByName( string name, bool isGet , bool isSet )
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
        public MetaMemberFunction GetMetaMemberFunctionByNameAndInputParamCollect(string name, MetaInputParamCollection mmpc)
        {
            if (!m_MetaMemberFunctionListDict.ContainsKey(name))
            {
                if ( m_ExtendClass != null)
                {
                    var func = m_ExtendClass.GetMetaMemberFunctionByNameAndInputParamCollect(name, mmpc);
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
                if (fun.IsEqualMetaParamCollection(mmpc))
                    return fun;
            }
            return null;
        }
        public MetaMemberFunction GetMetaMemberFunctionByNameAndTemplateCollectInputParamCollect(string name, MetaInputTemplateCollection mitc, MetaInputParamCollection mmpc)
        {
            if (!m_MetaMemberFunctionListDict.ContainsKey(name))
            {
                if ( m_ExtendClass != null)
                {
                    var func = m_ExtendClass.GetMetaMemberFunctionByNameAndTemplateCollectInputParamCollect(name, mitc, mmpc);
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
                if( fun.isConstructInitFunction )
                {            
                }
                if (fun.IsEqualMetaTemplateCollectionAndMetaParamCollection(mitc, mmpc))
                    return fun;
            }
            return null;
        }
        public MetaMemberFunction GetMetaMemberFunctionByAllName( string name )
        {
            if(m_MetaMemberAllNameFunctionDict.ContainsKey( name ) )
            {
                return (m_MetaMemberAllNameFunctionDict[name] as MetaMemberFunction);
            }
            return null;
        }
        public MetaMemberFunction GetMetaMemberConstructDefaultFunction()
        {
            return GetMetaMemberConstructFunction(null);
        }
        public MetaMemberFunction GetMetaMemberConstructFunction( MetaInputParamCollection mmpc )
        {
            return GetMetaMemberFunctionByNameAndInputParamCollect("__Init__", mmpc);
        }
        public MetaMemberFunction GetMetaMemberConstructFunctionByTemplateAndParam(MetaInputTemplateCollection mitc, MetaInputParamCollection mmpc )
        {
            if(mitc== null )
            {
                return GetMetaMemberFunctionByNameAndInputParamCollect("__Init__", mmpc );
            }
            else
            {
                return GetMetaMemberFunctionByNameAndTemplateCollectInputParamCollect("__Init__", mitc, mmpc );
            }
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
                stringBuilder.Append(" :: ");
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

            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("}" + Environment.NewLine);

            return stringBuilder.ToString();
        }
    }
}
