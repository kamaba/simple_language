//****************************************************************************
//  File:      TypeManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.MetaObjects;
using SimpleLanguage.Parse;
using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    public class TypeManager
    {
        public static TypeManager instance = new TypeManager();
        //public void UpdateMetaType( MetaType mt, MetaGenTemplateClass mgtc )
        //{
        //    bool isNeedReg = false;
        //    MetaClass findfn = null;
        //    if ( mt.isTemplate )
        //    {
        //        var gmgt = mgtc.GetMetaGenTemplate(mt.metaTemplate.name);
        //        if( gmgt != null )
        //        {
        //            mt.SetMetaClass(gmgt.metaType.metaClass);
        //            mt.SetMetaTemplate(null);
        //            findfn = gmgt.metaType.metaClass;
        //        }
        //    }
        //    List<MetaClass> regMCList = new List<MetaClass>();
        //    if ( mt.templateMetaTypeList.Count > 0 )
        //    {
        //        isNeedReg = true;
        //        for (int i = 0; i < mt.templateMetaTypeList.Count; i++)
        //        {
        //            UpdateMetaType(mt.templateMetaTypeList[i], mgtc);
        //            regMCList.Add(mt.templateMetaTypeList[i].metaClass);
        //            if (mt.templateMetaTypeList[i].isTemplate)
        //            {
        //                isNeedReg = false;
        //            }
        //        }
        //    }
        //    if (findfn != null && isNeedReg)
        //    {
        //        //var newmc = findfn.AddInstanceMetaClass(regMCList);
        //        //mt.SetMetaClass(newmc);
        //    }

        //}
        public bool UpdateMetaTypeByGenClassAndFunction(MetaType mt, MetaGenTemplateClass mgtc, MetaGenTempalteFunction mgtf)
        {
            bool isNeedReg = false;
            MetaClass findfn = null;
            List<MetaClass> regMCList = new List<MetaClass>();
            if (mt.templateMetaTypeList.Count > 0)
            {
                for (int i = 0; i < mt.templateMetaTypeList.Count; i++)
                {
                    if (UpdateMetaTypeByGenClassAndFunction(mt.templateMetaTypeList[i], mgtc, mgtf))
                    {
                        isNeedReg = true;
                    }
                    regMCList.Add(mt.templateMetaTypeList[i].metaClass);
                }
            }
            if (isNeedReg)
            {
                var newmc = mt.metaClass.AddInstanceMetaClass(regMCList);
                if (newmc == null)
                {
                    Log.AddInStructMeta(EError.None, "MetaClass is Null");
                    return false;
                }
                mt.SetMetaClass(newmc);
                return true;
            }
            if (mt.isTemplate)
            {
                MetaGenTemplate gmgt = mgtc.GetMetaGenTemplate(mt.metaTemplate.name);
                if (gmgt != null)
                {

                    if (gmgt.metaType.metaClass == null)
                    {
                        Log.AddInStructMeta(EError.None, "MetaClass is Null");
                        return false;
                    }

                    mt.SetMetaClass(gmgt.metaType.metaClass);
                    mt.SetMetaTemplate(null);
                    findfn = gmgt.metaType.metaClass;
                }
                else
                {
                    gmgt = mgtf?.GetMetaGenTemplate(mt.metaTemplate.name);
                    if (gmgt != null)
                    {
                        if (gmgt.metaType.metaClass == null)
                        {
                            Log.AddInStructMeta(EError.None, "MetaClass is Null");
                            return false;
                        }
                        mt.SetMetaClass(gmgt.metaType.metaClass);
                        mt.SetMetaTemplate(null);
                        findfn = gmgt.metaType.metaClass;
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "没有找到模板中定义的模板内容!" + mt.metaTemplate.name);
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }
        #region 模板类定义处理区
        public MetaType GetMetaTemplateClassAndRegisterExptendTemplateClassInstance(MetaClass curMc, FileMetaClassDefine fmcd)
        {
            if (fmcd == null) return null;

            MetaNode getmc = ClassManager.instance.GetMetaClassByRef(curMc, fmcd);
            if (getmc == null)
            {
                var mt = curMc.GetMetaTemplateByName(fmcd.stringList[0]);
                if (mt == null)
                {
                    Log.AddInStructMeta(EError.None, $"没有找到模板类中，对应的模板，名称为{fmcd.stringList[0]}请仔细检查模板的命名与使用模板命名是否对应", fmcd.classNameToken );
                }
                else
                {
                    var retmt = new MetaType(mt);
                    return retmt;
                }
            }
            else
            {
                return GetMetaTypeByInputTemplateList(curMc, getmc, fmcd.inputTemplateNodeList);
            }
            return null;
        }
        public MetaType GetMetaTypeByInputTemplateList(MetaClass ownerMc, MetaNode getmc, List<FileInputTemplateNode> inputTemplateNodeList, List<MetaType> list = null )
        {
            if (inputTemplateNodeList.Count == 0)
            {
                return new MetaType(getmc.GetMetaClassByTemplateCount(0));
            }
            var findfn = getmc.GetMetaClassByTemplateCount(inputTemplateNodeList.Count);
            if (findfn == null)
            {
                return null;
            }
            if( inputTemplateNodeList.Count == 0 )
            {
                return new MetaType(findfn );
            }
            var mt = new MetaType();
            mt.SetTemplateMetaClass(findfn);
            //这里，要注册实体模板类            
            for (int i = 0; i < inputTemplateNodeList.Count; i++)
            {
                MetaType mt2 = GetAndRegisterTemplateDefineMetaTemplateClass(ownerMc, findfn, inputTemplateNodeList[i]);
                mt.AddTemplateMetaType(mt2);
            }
            
            return ownerMc.AddMetaPreTemplateClass(mt, false, out bool igmc);
        }
        MetaType GetAndRegisterTemplateDefineMetaTemplateClass(MetaClass ownerMc, MetaClass findMc, FileInputTemplateNode fmtd)
        {
            var newmn = ClassManager.instance.GetMetaClassByNameAndFileMeta(ownerMc, fmtd.fileMeta, fmtd.nameList);
            FileMetaCallNode cnode = null;
            if (newmn != null)
            {
                var findfn = newmn.GetMetaClassByTemplateCount(fmtd.inputTemplateCount);
                if(findfn == null )
                {
                    Log.AddInStructMeta(EError.None, $"没有发现{fmtd.nameList}找到的类!");
                    return null;
                }
                if( fmtd.inputTemplateCount == 0 )
                {
                    return new MetaType(findfn);
                }
                else
                {
                    var mt = new MetaType();
                    mt.SetTemplateMetaClass(findfn);
                    List<MetaGenTemplate> mgtList = new List<MetaGenTemplate>();
                    for (int i = 0; i < fmtd.defineClassCallLink.callNodeList.Count; i++)
                    {
                        var dcc = fmtd.defineClassCallLink.callNodeList[i];
                        for (int j = 0; j < dcc.inputTemplateNodeList.Count; j++)
                        {
                            var itn = dcc.inputTemplateNodeList[j];
                            var mt2 = GetAndRegisterTemplateDefineMetaTemplateClass(ownerMc, findfn, itn);
                            mt.AddTemplateMetaType(mt2);
                            //if (mt2.isTemplate)
                            //{
                            //    isNeedReg = false;
                            //    MetaGenTemplate mgt = new MetaGenTemplate(mt2.metaTemplate);
                            //    mgtList.Add(mgt);
                            //}
                            //else if( mt2.metaClass != null )
                            //{
                            //    regMCList.Add(mt2.metaClass);
                            //}
                            //else
                            //{
                            //    isNeedReg = false;
                            //    var template = findfn.GetMetaTemplateByIndex(j);
                            //    MetaGenTemplate mgt = new MetaGenTemplate(template, mt2);
                            //    mgtList.Add(mgt);
                            //}
                        }
                    }

                    return ownerMc.AddMetaPreTemplateClass(mt, false, out bool igmc);
                }
            }
            else
            {
                if (fmtd.nameList.Count == 1)
                {
                    var mt = ownerMc.GetMetaTemplateByName(fmtd.nameList[0]);
                    if (mt == null)
                    {
                        Log.AddInStructMeta(EError.None, "没有找到模板类中，对应的模板，请仔细检查模板的命名与使用模板命名是否对应", cnode?.token );
                    }
                    else
                    {
                        return new MetaType(mt);
                    }
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "使用模板类中使用.连接符号，模板中不允许使用.");
                }
            }
            return null;
        }
        #endregion

        #region 模板函数处理区
        public MetaType GetMetaTypeByTemplateFunction(MetaClass curMc, MetaMemberFunction findFun, FileMetaClassDefine fmcd)
        {
            if (fmcd == null) return null;

            MetaNode getmc = ClassManager.instance.GetMetaClassByRef(curMc, fmcd);
            if (getmc == null)
            {
                var gmtbn = curMc.GetMetaTemplateByName(fmcd.stringList[0]);
                if (gmtbn != null)
                {
                    var mt = new MetaType(gmtbn);
                    return mt;
                }
                else if (findFun != null)
                {
                    var mt = findFun.GetMetaDefineTemplateByName(fmcd.stringList[0]);
                    if( mt == null )
                    {
                        return null;
                    }
                    return new MetaType(mt);
                }
                else
                {
                    Log.AddInStructMeta(EError.None, $"没有找到{fmcd.stringList[0]} 的相关类!");
                }

            }
            else
            {
                return GetMetaTypeByTemplateList(curMc, getmc, findFun, fmcd.inputTemplateNodeList);
            }
            return null;
        }
        public MetaType GetMetaTypeByTemplateList(MetaClass curMc, MetaNode getmc, MetaMemberFunction findFun, List<FileInputTemplateNode> inputTemplateNodeList)
        {            
            var findfn = getmc.GetMetaClassByTemplateCount(inputTemplateNodeList.Count);
            if (findfn != null)
            {
                if( inputTemplateNodeList.Count == 0 )
                {
                    return new MetaType(findfn);
                }

                var newmc = HandleInputTemplateNodeList(curMc, findfn, findFun, inputTemplateNodeList, false);
                if( newmc != null)
                {
                    return newmc;
                }
                else
                {
                    var mt = new MetaType();
                    mt.SetTemplateMetaClass(findfn);
                    return mt;
                }
            }

            return null;
        }
        public MetaType HandleInputTemplateNodeList(MetaClass findfn, MetaClass regMc,  MetaMemberFunction findFun, List<FileInputTemplateNode> inputTemplateNodeList, bool isParse )
        {
            var getmc = findfn;
            MetaType mt = new MetaType();
            if( inputTemplateNodeList.Count == 0 )
            {
                mt.SetMetaClass(regMc);
                return mt;
            }
            mt.SetTemplateMetaClass(regMc);
            //这里，要注册实体模板类
            for (int i = 0; i < inputTemplateNodeList.Count; i++)
            {
                var t = RegisterTemplateDefineMetaTemplateFunction(findfn, regMc, findFun, inputTemplateNodeList[i], isParse );
                mt.AddTemplateMetaType(t);
            }
            mt = regMc.AddMetaPreTemplateClass(mt, isParse, out bool igmc);
            return mt;
        }
        public MetaType RegisterTemplateDefineMetaTemplateFunction(MetaClass findMc, MetaClass regMc, MetaMemberFunction findFun, FileInputTemplateNode fmtd, bool isParse = false )
        {
            var newmc = ClassManager.instance.GetMetaClassByNameAndFileMeta(findMc, fmtd.fileMeta, fmtd.nameList);
            if (newmc != null)
            {
                var findfn = newmc.GetMetaClassByTemplateCount(fmtd.inputTemplateCount);

                if (findfn == null)
                {
                    Log.AddInStructMeta(EError.None, "没有找到相对应的模板类!!");
                    return null;
                }
                if( fmtd.inputTemplateCount > 0 )
                {
                    var dcc = fmtd.defineClassCallLink.callNodeList[fmtd.defineClassCallLink.callNodeList.Count - 1];

                    var retmc = HandleInputTemplateNodeList(findMc, findfn, findFun, dcc.inputTemplateNodeList, isParse);

                    if( retmc != null )
                    {
                        return retmc;
                    }
                }
                else
                {
                    var mt = new MetaType(findfn);
                    return mt;
                }
            }
            else
            {
                if (fmtd.nameList.Count == 1)
                {
                    if (findMc != null)
                    {
                        var mgtc2 = findMc.GetMetaTemplateByName(fmtd.nameList[0]);
                        if (mgtc2 != null)
                        {
                            return new MetaType(mgtc2);
                        }
                    }
                    if (findFun != null)
                    {
                        var mt = findFun.GetMetaDefineTemplateByName(fmtd.nameList[0]);
                        if (mt != null)
                        {
                            return new MetaType(mt);
                        }
                    }
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "----fmtd.nameList.count > 1 ");
                }
            }
            return null;
        }
        #endregion
    }
}
