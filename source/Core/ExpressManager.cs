using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

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
    public class ExpressManager
    {
        public static ExpressManager s_Instance = null;
        public static ExpressManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new ExpressManager();
                }
                return s_Instance;
            }
        }

        public static ExpressOptimizeConfig expressOptimizeConfig = new ExpressOptimizeConfig();
        public static bool IsCanExpressCampute( MetaClass mc )
        {
            if (mc == CoreMetaClassManager.int16MetaClass
                || mc == CoreMetaClassManager.int32MetaClass
                || mc == CoreMetaClassManager.int64MetaClass
                || mc == CoreMetaClassManager.floatMetaClass
                || mc == CoreMetaClassManager.doubleMetaClass)
                return true;
            return false;
        }
        public MetaExpressNode CreateOptimizeAfterExpress( MetaExpressNode men, ExpressOptimizeConfig config = null )
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
                case MetaCallExpressNode mcn:
                    {
                        //mcn.GetMetaVariable
                    }
                    break;
                default:
                    {
                        Console.WriteLine("Error Optimaze don't support that ExpressType");
                    }
                    break;
            }
            return men;
        }
        public MetaExpressNode VisitFileMetaExpress(MetaClass mc, MetaBlockStatements mbs, MetaType mdt, FileMetaBaseTerm fme)
        {
            if (fme == null) return null;

            var OpSign = fme;
            if (fme.left == null && fme.right == null)
            {
                return CreateExpressNode(mc, mbs, mdt, fme);
            }
            MetaExpressNode leftNode = VisitFileMetaExpress(mc, mbs, mdt, fme.left);
            MetaExpressNode rightNode = VisitFileMetaExpress(mc, mbs, mdt, fme.right);

            if (leftNode != null && rightNode != null)
            {
                if (fme is FileMetaSymbolTerm)
                {
                    return new MetaOpExpressNode(fme as FileMetaSymbolTerm, mdt, leftNode, rightNode );
                }
                else
                {
                    Console.WriteLine(" Error VisitFileMetaExpress fileMetaNode 不是符号!!");
                }
            }
            else if (leftNode != null && rightNode == null)
            {
                if (fme is FileMetaSymbolTerm)
                {
                    return new MetaUnaryOpExpressNode(fme as FileMetaSymbolTerm, leftNode);
                }
                else
                {
                    return leftNode;
                }
            }
            else if (leftNode == null && rightNode != null)
            {
                if (fme is FileMetaSymbolTerm)
                {
                    return new MetaUnaryOpExpressNode(fme as FileMetaSymbolTerm, rightNode);
                }
                else
                {
                    return rightNode;
                }
            }
            else
            {
                Console.WriteLine(" Error VisitFileMetaExpress left and right都为空!!");
            }
            return null;
        }
        public MetaExpressNode CreateExpressNode( MetaClass mc, MetaBlockStatements mbs, MetaType mdt, FileMetaBaseTerm fme )
        {
            MetaExpressNode men = null;
            switch ( fme )
            {
                case FileMetaSymbolTerm fmst:
                    {
                        Console.WriteLine("Error CreateExpressNode 创建表达项不能为符号");
                    }
                    break;
                case FileMetaConstValueTerm fmcvt:
                    {
                        men = new MetaConstExpressNode(fmcvt);
                    }
                    break;
                case FileMetaCallTerm fmct:
                    {
                        MetaCallExpressNode men2 = new MetaCallExpressNode(fmct.callLink, mc, mbs );
                        men = men2;
                    }
                    break;
                case FileMetaBraceTerm fmbt:
                    {
                        men = new MetaNewObjectExpressNode( fmbt, mdt, mc, mbs );
                    }
                    break;
                case FileMetaParTerm fmpt:
                    {
                        Console.WriteLine("Error CreateExpressNode 已在前边拆解，不应该还有原素, 该位置的()一般只能构建对象时使用");
                        //men = VisitFileMetaExpress(mc, mbs, selfMC, fmpt.express );
                    }
                    break;
                case FileMetaTermExpress fmte:
                    {
                        //Console.WriteLine("Error CreateExpressNode 创建表达项不能为符号");
                        men = CreateExpressNode(mc, mbs, mdt, fmte.root );
                    }
                    break;
                default:
                    Console.WriteLine("Error CreateExpressNode 创建表达项不能为符号");
                    break;
            }
            return men;
        }
        public MetaExpressNode CreateExpressNodeInMetaFunctionCommonStatements(MetaBlockStatements mbs, MetaType mdt, FileMetaBaseTerm fme, bool isStatic = false, bool isConst = false )
        {
            MetaClass ownerClass = mbs?.ownerMetaClass;
            var root = fme.root;
            if (root.left == null && root.right == null)
            {
                MetaExpressNode men = null;
                switch (root)
                {
                    case FileMetaConstValueTerm fmcvt:
                        {
                            if( fmcvt.token.type == ETokenType.NumberArrayLink )
                            {
                                MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(fmcvt, ownerClass, mbs);

                                return mnoen;
                            }
                            else
                            {
                                men = new MetaConstExpressNode(fmcvt);
                            }
                        }
                        break;
                    case FileMetaCallTerm fmct:
                        {
                            var auc = new AllowUseConst();

                            MetaNewObjectExpressNode mnoen = MetaNewObjectExpressNode.CreateNewObjectExpressNodeByCall((root as FileMetaCallTerm), mdt, ownerClass, mbs, auc );
                            if (mnoen != null)
                                return mnoen;

                            MetaCallExpressNode men2 = new MetaCallExpressNode( fmct.callLink, ownerClass, mbs ); 
                            men = men2;
                        }
                        break;
                    case FileMetaBraceTerm fmbt:
                        {
                            men = new MetaNewObjectExpressNode(fmbt, mdt, ownerClass, mbs);
                        }
                        break;
                    case FileMetaParTerm fmpt:
                        {
                            //Console.WriteLine("Error CreateExpressNode 已在前边拆解，不应该还有原素, 该位置的()一般只能构建对象时使用");
                            MetaNewObjectExpressNode mnoen = MetaNewObjectExpressNode.CreateNewObjectExpressNodeByPar((root as FileMetaParTerm), mdt, ownerClass, mbs);
                            if (mnoen != null)
                                return mnoen;

                            //men = CreateMetaClassByFileMetaClass( ownerClass, selfMC, mbs, fmpt.express);
                        }
                        break;
                    case FileMetaTermExpress fmte:
                        {
                            //Console.WriteLine("Error CreateExpressNode 创建表达项不能为符号");
                            men = CreateExpressNode(ownerClass, mbs, mdt, fmte);
                        }
                        break;
                    case FileMetaBracketTerm fmbt:
                        {
                            //Console.WriteLine("Error CreateExpressNode 已在前边拆解，不应该还有原素, 该位置的()一般只能构建对象时使用");
                            
                            MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode((root as FileMetaBracketTerm),  mdt, ownerClass, mbs, null);
                            if (mnoen != null)
                                return mnoen;
                        }
                        break;
                    default:
                        Console.WriteLine("Error CreateExpressNode 创建表达项不能为符号");
                        break;
                }
                if( men != null )
                {
                    AllowUseConst auc = new AllowUseConst();
                    auc.useNotStatic = !isStatic;
                    auc.useNotConst = isConst;
                    men.Parse(auc);
                    return men;
                }
            }

            MetaExpressNode mn = VisitFileMetaExpress(ownerClass, mbs, mdt, root);
            AllowUseConst auc2 = new AllowUseConst();
            auc2.useNotStatic = !isStatic;
            auc2.useNotConst = isConst;
            mn.Parse(auc2);

            return mn;
        }

        public static MetaExpressNode CreateExpressNodeInMetaFunctionNewStatementsWithIfOrSwitch(FileMetaBaseTerm fmte, MetaBlockStatements mbs, MetaType mdt )
        {
            MetaClass mc = mbs.ownerMetaClass;

            if (fmte != null)
            {
                FileMetaIfSyntaxTerm ifExpressTerm = fmte as FileMetaIfSyntaxTerm;
                FileMetaSwitchSyntaxTerm switchExpressTerm = fmte as FileMetaSwitchSyntaxTerm;
                if (ifExpressTerm != null)
                {
                    MetaExecuteStatementsNode mesn = MetaExecuteStatementsNode.CreateMetaExecuteStatementsNodeByIfExpress(mdt, mc, mbs, ifExpressTerm.ifSyntax);
                    if (mesn != null)
                    {
                        return mesn;
                    }
                }
                else if (switchExpressTerm != null)
                {
                    MetaExecuteStatementsNode mesn = MetaExecuteStatementsNode.CreateMetaExecuteStatementsNodeBySwitchExpress(mdt, mc, mbs, switchExpressTerm.switchSyntax);
                    if (mesn != null)
                    {
                        return mesn;
                    }
                }
                else
                {
                    return ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements(mbs, mdt, fmte, false, false );
                }
            }
            else
            {
                Console.WriteLine("Error 没有找到合适的表达式 位置: " + fmte.token?.ToLexemeAllString());
            }
            return null;
        }
        public int CalcParseLevel( int level, MetaExpressNode men )
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
                case MetaCallExpressNode mcn:
                    {
                        level = mcn.CalcParseLevel(level);
                    }
                    break;
            }
            return level;
        }
    }
}
