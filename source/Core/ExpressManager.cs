//****************************************************************************
//  File:      ExpressManager.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/5/17 12:00:00
//  Description:  
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Core
{
    public class ExpressOptimizeConfig
    {
        public bool ifLeftAndRightIsConstThenCompute = true;
        public bool notEqualConvertEqualExpress = false;    // not equal express convert equal express
        public bool greaterOrEqualConvertGeraterAndEqual = false;
        public bool lessOrEqualConvertLessAndEqual = false;
        public bool ifUnaryExpressValueIsConstThenCompute = true;// if unary express's value node is const type, then force compute 
        public bool ifOpExpressLeftAndRightIsConstThenCompute = true;
    }
    public struct CreateExpressParam
    {
        public MetaBlockStatements ownerMBS;
        public MetaClass ownerMetaClass;
        public MetaType metaType;
        public MetaType parentMetaType;
        public MetaVariable equalMetaVariable;
        public FileMetaBaseTerm fme;
        public bool isStatic;
        public bool isConst;
        public bool allowUseIfSyntax;
        public bool allowUseSwitchSyntax;
        public bool allowUseParSyntax;
        public bool allowUseBraceSyntax;
        public EParseFrom parsefrom;

        public CreateExpressParam()
        {
            ownerMBS = null;
            ownerMetaClass = null;
            equalMetaVariable = null;
            metaType = null;
            parentMetaType = null;
            fme = null;
            isStatic = false;
            isConst = false;
            allowUseIfSyntax = false;
            allowUseSwitchSyntax = false;
            allowUseParSyntax = false;
            allowUseBraceSyntax = false;
            parsefrom = EParseFrom.None;
        }
        public CreateExpressParam( CreateExpressParam clone )
        {
            ownerMBS = clone.ownerMBS;
            ownerMetaClass = clone.ownerMetaClass;
            equalMetaVariable = clone.equalMetaVariable;
            metaType = clone.metaType;
            parentMetaType = clone.parentMetaType;
            fme = clone.fme;
            isStatic = clone.isStatic;
            isConst = clone.isConst;
            allowUseIfSyntax = clone.allowUseIfSyntax;
            allowUseSwitchSyntax = clone.allowUseSwitchSyntax;
            allowUseParSyntax = clone.allowUseParSyntax;
            allowUseBraceSyntax = clone.allowUseBraceSyntax;
            parsefrom = clone.parsefrom;
        }
    }
    public class ExpressManager
    {
        public static ExpressOptimizeConfig expressOptimizeConfig = new ExpressOptimizeConfig();
        public static bool IsCanExpressCampute( MetaClass mc )
        {
            if (mc == CoreMetaClassManager.int16MetaClass
                || mc == CoreMetaClassManager.int32MetaClass
                || mc == CoreMetaClassManager.int64MetaClass
                || mc == CoreMetaClassManager.float32MetaClass
                || mc == CoreMetaClassManager.float64MetaClass)
                return true;
            return false;
        }
        public static MetaExpressNode CreateExpressNodeByCEP(CreateExpressParam cep)
        {
            FileMetaBaseTerm fmte = cep.fme;
            MetaBlockStatements mbs = cep.ownerMBS;
            MetaType mdt = cep.metaType;
            MetaVariable equalMetaVariable = cep.equalMetaVariable;
            MetaClass mc = cep.ownerMetaClass;

            if (fmte == null)
            {
                Log.AddInStructMeta(EError.None, "CreateExpressNode FileMetaBaseTerm 为空 !!");
                return null;
            }
            
            FileMetaIfSyntaxTerm ifExpressTerm = fmte as FileMetaIfSyntaxTerm;
            if ( cep.allowUseIfSyntax )
            {
                if (ifExpressTerm != null)
                {
                    MetaExecuteStatementsNode mesn = MetaExecuteStatementsNode.CreateMetaExecuteStatementsNodeByIfExpress(mdt, mc, mbs, ifExpressTerm.ifSyntax);
                    if (mesn != null)
                    {
                        return mesn;
                    }
                }
            }
            else if( ifExpressTerm != null )
            {
                Log.AddInStructMeta(EError.None, "不允许使用If语句!!");
                return null;
            }

            FileMetaMatchSyntaxTerm switchExpressTerm = fmte as FileMetaMatchSyntaxTerm;
            if ( cep.allowUseSwitchSyntax )
            {
                if (switchExpressTerm != null)
                {
                    MetaExecuteStatementsNode mesn = MetaExecuteStatementsNode.CreateMetaExecuteStatementsNodeBySwitchExpress(mdt, mc, mbs, switchExpressTerm.switchSyntax);
                    if (mesn != null)
                    {
                        return mesn;
                    }
                }
            }
            else if (switchExpressTerm != null)
            {
                Log.AddInStructMeta(EError.None, "不允许使用Switch语句!!");
                return null;
            }

            FileMetaParTerm parExpressTerm = fmte as FileMetaParTerm;
            if (cep.allowUseParSyntax)
            {
                if (parExpressTerm != null)
                {
                    MetaNewObjectExpressNode mnoen = MetaNewObjectExpressNode.CreateNewObjectExpressNodeByPar(parExpressTerm, mdt, mc, mbs);
                    if (mnoen != null)
                        return mnoen;
                }
            }
            else if (parExpressTerm != null)
            {
                Log.AddInStructMeta(EError.None, "不允许使用Switch语句!!");
                return null;
            }

            return CreateExpressNode(cep);
        }
        public static MetaExpressNode CreateExpressNode(CreateExpressParam cep)
        {
            if(cep.fme == null )
            {
                return null;
            }
            MetaClass ownerClass = cep.ownerMetaClass;
            var root = cep.fme.root;
            if (root.left == null && root.right == null)
            {
                MetaExpressNode men = null;
                switch (root)
                {

                    case FileMetaSymbolTerm fmst:
                        {
                            Log.AddInStructMeta(EError.None, "Error CreateExpressNode 创建表达项不能为符号");
                        }
                        break;
                    case FileMetaConstValueTerm fmcvt:
                        {
                            if (fmcvt.token.type == ETokenType.NumberArrayLink)
                            {
                                MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(fmcvt, ownerClass, cep.ownerMBS);

                                return mnoen;
                            }
                            else
                            {
                                men = new MetaConstExpressNode(fmcvt);
                                return men;
                            }
                        }
                        break;
                    case FileMetaCallTerm fmct:     //className.functionname().varname;
                        {
                            MetaCallLinkExpressNode men2 = new MetaCallLinkExpressNode(fmct.callLink, cep.ownerMetaClass, cep.ownerMBS, cep.equalMetaVariable );
                            return men2;
                        }
                    case FileMetaBraceTerm fmbt:  // {1,2,3} {a=10,b=20}
                        {
                            men = new MetaNewObjectExpressNode(fmbt, cep.metaType, ownerClass, cep.ownerMBS, cep.equalMetaVariable);
                            return men;
                        }
                    case FileMetaParTerm fmpt:  //  (1,2)
                        {
                            //Debug.Write("Error CreateExpressNode 已在前边拆解，不应该还有原素, 该位置的()一般只能构建对象时使用");
                            MetaNewObjectExpressNode mnoen = MetaNewObjectExpressNode.CreateNewObjectExpressNodeByPar((root as FileMetaParTerm), cep.metaType, ownerClass, cep.ownerMBS);
                            if (mnoen != null)
                                return mnoen;

                            //men = CreateMetaClassByFileMetaClass( ownerClass, selfMC, mbs, fmpt.express);
                        }
                        break;
                    case FileMetaTermExpress fmte:
                        {
                            //Debug.Write("Error CreateExpressNode 创建表达项不能为符号");
                            cep.ownerMetaClass = ownerClass;
                            men = CreateExpressNode(cep);
                            return men;
                        }
                    case FileMetaBracketTerm fmbt:
                        {
                            //Debug.Write("Error CreateExpressNode 已在前边拆解，不应该还有原素, 该位置的()一般只能构建对象时使用");

                            MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode((root as FileMetaBracketTerm), ownerClass, cep.ownerMBS, cep.equalMetaVariable);
                            if (mnoen != null)
                                return mnoen;
                        }
                        break;
                    default:
                        Log.AddInStructMeta(EError.None, "Error CreateExpressNode 创建表达项不能为符号");
                        break;
                }
            }
            else
            {
                CreateExpressParam clonecep = new CreateExpressParam(cep);
                clonecep.fme = root.left;
                MetaExpressNode leftNode = CreateExpressNode(clonecep);
                clonecep.fme = root.right;
                MetaExpressNode rightNode = CreateExpressNode(clonecep);

                if (leftNode != null && rightNode != null)
                {
                    if (root is FileMetaSymbolTerm)
                    {
                        return new MetaOpExpressNode(root as FileMetaSymbolTerm, cep.metaType, leftNode, rightNode);
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, " Error VisitFileMetaExpress fileMetaNode 不是符号!!");
                    }
                }
                else if (leftNode != null && rightNode == null)
                {
                    if (root is FileMetaSymbolTerm)
                    {
                        return new MetaUnaryOpExpressNode(root as FileMetaSymbolTerm, leftNode);
                    }
                    else
                    {
                        return leftNode;
                    }
                }
                else if (leftNode == null && rightNode != null)
                {
                    if (root is FileMetaSymbolTerm)
                    {
                        return new MetaUnaryOpExpressNode(root as FileMetaSymbolTerm, rightNode);
                    }
                    else
                    {
                        return rightNode;
                    }
                }
                else
                {
                    Log.AddInStructMeta(EError.None, " Error VisitFileMetaExpress left and right都为空!!");
                }
                return null;
            }
            return null;
        }
        public static MetaExpressNode CreateOptimizeAfterExpress( MetaExpressNode men, ExpressOptimizeConfig config = null )
        {
            if( config == null )
            {
                config = ExpressManager.expressOptimizeConfig;
            }
            switch (men)
            {
                case MetaUnaryOpExpressNode muoen:
                    {
                        var newValue = CreateOptimizeAfterExpress(muoen.value, config);
                        muoen.SetValue(newValue);
                        if ( config.ifUnaryExpressValueIsConstThenCompute )
                        {
                            return muoen.SimulateCompute();
                        }
                    }
                    break;
                case MetaOpExpressNode moen:
                    {
                        var newLeft = CreateOptimizeAfterExpress(moen.left, config);
                        var newRight = CreateOptimizeAfterExpress(moen.right, config);
                        moen.SetLeft(newLeft);
                        moen.SetRight(newRight);
                        moen.SimulateCompute(config);
                    }
                    break;
                case MetaCallLinkExpressNode mcn:
                    {
                        //mcn.GetMetaVariable
                    }
                    break;
                case MetaConstExpressNode mcen:
                    {
                    }
                    break;
                default:
                    {
                        Log.AddInStructMeta(EError.None, "Error Optimaze don't support that ExpressType");
                    }
                    break;
            }
            return men;
        }
       
        public static void CreateNewOrCalllink( CreateExpressParam cep, out MetaNewObjectExpressNode mnoen, out MetaCallLinkExpressNode men2 )
        {
            mnoen = null;
            men2 = null;
            MetaClass omc = cep.ownerMetaClass;
            MetaBlockStatements mbs = cep.ownerMBS;
            var fmct = cep.fme as FileMetaCallTerm;
            if (fmct == null) return;
            if (fmct.callLink == null) return;
            if (fmct.callLink.callNodeList.Count <= 0) return;

            AllowUseSettings aus = new AllowUseSettings() { parseFrom = cep.parsefrom };

            MetaCallLink mcl = new MetaCallLink(fmct.callLink, cep.ownerMetaClass, cep.ownerMBS, cep.metaType, cep.equalMetaVariable );
            if (!mcl.Parse(aus)) return;
            mcl.CalcReturnType();

            bool isNewClass = false;
            bool isNewData = false;
            bool isNewEnum = false;
            if (mcl.finalCallNode?.visitType == MetaVisitNode.EVisitType.New)
            {
                if( mcl.finalCallNode.callMetaType.isData )
                {
                    isNewData = true;
                }
                else
                {
                    isNewClass = true;
                }
            }
            //else if (mcl.finalMetaCallNode.callNodeType == ECallNodeType.EnumNewValue)
            //{
            //    isNewEnum = true;
            //}
            if (mcl.finalCallNode.methodCall != null)
            {
                if ((mcl.finalCallNode.methodCall.function as MetaMemberFunction).isConstructInitFunction)
                {
                    isNewClass = true;
                }
            }
            MetaType retmt = mcl.GetMetaDeineType();
            if (isNewClass)
            {
                mnoen = new MetaNewObjectExpressNode(fmct, mcl, retmt, omc, mbs, mcl.finalCallNode.methodCall);
            }
            else if (isNewData)
            {
                mnoen = new MetaNewObjectExpressNode(fmct, mcl, retmt, omc, mbs, mcl.finalCallNode.methodCall);
            }
            else if (isNewEnum)
            {
                mnoen = new MetaNewObjectExpressNode(fmct, mcl, retmt, omc, mbs, null);
            }
            else
            {
                men2 = new MetaCallLinkExpressNode(fmct.callLink, omc, mbs, null);
            }
        }
        public static int CalcParseLevel( int level, MetaExpressNode men )
        {
            switch( men )
            {
                case MetaUnaryOpExpressNode muoen:
                    {
                        level = CalcParseLevel( level, muoen.value );
                    }
                    break;
                case MetaOpExpressNode moen:
                    {
                        level = CalcParseLevel(level, moen.right);
                        level = CalcParseLevel(level, moen.left);
                    }
                    break;
                case MetaCallLinkExpressNode mcn:
                    {
                        level = mcn.CalcParseLevel(level);
                    }
                    break;
            }
            return level;
        }
    }
}
