//****************************************************************************
//  File:      ExpressManager.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/5/17 12:00:00
//  Description:  
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

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
        /// <summary>与 <see cref="MetaVariable"/> 一致：可为 Class / Data / Enum。</summary>
        public MetaBase ownerMetaBase;
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
            ownerMetaBase = null;
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
            ownerMetaBase = clone.ownerMetaBase;
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
        public static MetaExpressNodeBase CreateExpressNodeByCEP(CreateExpressParam cep)
        {
            FileMetaBaseTerm fmte = cep.fme;
            MetaBlockStatements mbs = cep.ownerMBS;
            MetaType mdt = cep.metaType;
            MetaVariable equalMetaVariable = cep.equalMetaVariable;
            MetaBase ownerBase = cep.ownerMetaBase;

            if (fmte == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, equalMetaVariable?.token, "" );
                return null;
            }


            FileMetaAsOrIsTerm asOrIsTerm = fmte as FileMetaAsOrIsTerm;
            if (asOrIsTerm != null)
            {
                MetaAsIsExpressNode mesn = MetaAsIsExpressNode.CreateMetaExecuteStatementsNode(mdt, ownerBase, mbs, asOrIsTerm, equalMetaVariable );
                if (mesn != null)
                {
                    return mesn;
                }
            }

            FileMetaIfSyntaxTerm ifExpressTerm = fmte as FileMetaIfSyntaxTerm;
            if ( cep.allowUseIfSyntax )
            {
                if (ifExpressTerm != null)
                {
                    MetaExecuteStatementsNode mesn = MetaExecuteStatementsNode.CreateMetaExecuteStatementsNodeByIfExpress(mdt, ownerBase, mbs, ifExpressTerm.ifSyntax);
                    if (mesn != null)
                    {
                        return mesn;
                    }
                }
            }
            else if( ifExpressTerm != null )
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "不允许使用If语句!!");
                return null;
            }

            FileMetaMatchSyntaxTerm switchExpressTerm = fmte as FileMetaMatchSyntaxTerm;
            if ( cep.allowUseSwitchSyntax )
            {
                if (switchExpressTerm != null)
                {
                    MetaExecuteStatementsNode mesn = MetaExecuteStatementsNode.CreateMetaExecuteStatementsNodeBySwitchExpress(mdt, ownerBase, mbs, switchExpressTerm.switchSyntax);
                    if (mesn != null)
                    {
                        return mesn;
                    }
                }
            }
            else if (switchExpressTerm != null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "不允许使用Switch语句!!");
                return null;
            }

            //这个注释掉的原因是 原来想着直接 使用 Class a = () 这样创建，后来改模式为Class a = new() 这种创建 所以没有直接解析的处理
            //FileMetaParTerm parExpressTerm = fmte as FileMetaParTerm;
            //if (cep.allowUseParSyntax)
            //{
            //    if (parExpressTerm != null)
            //    {
            //        MetaNewObjectExpressNode mnoen = MetaNewObjectExpressNode.CreateNewObjectExpressNodeByPar(parExpressTerm, mdt, mc, mbs);
            //        if (mnoen != null)
            //            return mnoen;
            //    }
            //}
            //else if (parExpressTerm != null)
            //{
            //    Log.AddMetaCoreLog(LID.ShowExtendMessage, "不允许使用Switch语句!!");
            //    return null;
            //}

            return CreateExpressNode(cep);
        }
        public static MetaExpressNodeBase CreateExpressNode(CreateExpressParam cep)
        {
            if(cep.fme == null )
            {
                return null;
            }
            MetaBase ownerBase = cep.ownerMetaBase;
            var root = cep.fme.root;
            if( root == null )
            {
                Log.AddMetaCoreLog(LID.MetaCoreParseFileExpressFailed, cep.fme.token, "root is Null", cep.fme.token );
                return null;
            }
            if (root.left == null && root.right == null)
            {
                MetaExpressNodeBase men = null;
                switch (root)
                {

                    case FileMetaSymbolTerm fmst:
                        {
                            //Log.AddMetaCoreLog(LID.ShowExtendMessage, root.token, "Error CreateExpressNode 创建表达项不能为符号");
                        }
                        break;
                    case FileMetaAsOrIsTerm fmaoit:
                        {
                            MetaAsIsExpressNode mesn = MetaAsIsExpressNode.CreateMetaExecuteStatementsNode( null, ownerBase, cep.ownerMBS,
                                fmaoit, cep.equalMetaVariable );
                            if (mesn != null)
                            {
                                return mesn;
                            }
                        }
                        break;
                    case FileMetaConstValueTerm fmcvt:
                        {
                            if (fmcvt.token.type == ETokenType.NumberArrayLink)
                            {
                                MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(fmcvt, ownerBase, cep.ownerMBS);

                                return mnoen;
                            }
                            else
                            {
                                men = new MetaConstExpressNode(ownerBase, cep.ownerMBS, fmcvt);
                                return men;
                            }
                        }
                    case FileMetaThreeItemSyntaxTerm fmtist:
                        {
                            MetaThreeItemExpressNode mis = new MetaThreeItemExpressNode(ownerBase, cep.ownerMBS, fmtist );
                            return mis;
                        }
                    case FileMetaCallTerm fmct:     //className.functionname().varname;
                        {
                            MetaCallLinkExpressNode men2 = new MetaCallLinkExpressNode(fmct.callLink, ownerBase, cep.ownerMBS, cep.equalMetaVariable );
                            return men2;
                        }
                    case FileMetaBraceTerm fmbt:  // {1,2,3} {a=10,b=20}
                        {
                            if(cep.metaType == null || cep.metaType?.isDynamicData == true )
                            {
                                men = new MetaAnonDataExpressNode(fmbt, ownerBase, cep.ownerMBS, cep.equalMetaVariable);
                            }
                            else
                            {
                                men = new MetaNewObjectExpressNode(fmbt, cep.metaType, ownerBase, cep.ownerMBS );
                            }
                            return men;
                        }
                    //case FileMetaParTerm fmpt:  //  (1,2) 不允许 这种方式的处理 可能后边会变成tulpe
                    //    {
                    //        //Debug.Write("Error CreateExpressNode 已在前边拆解，不应该还有原素, 该位置的()一般只能构建对象时使用");
                    //        MetaNewObjectExpressNode mnoen = MetaNewObjectExpressNode.CreateNewObjectExpressNodeByPar((root as FileMetaParTerm), cep.metaType, ownerClass, cep.ownerMBS);
                    //        if (mnoen != null)
                    //            return mnoen;

                    //        //men = CreateMetaClassByFileMetaClass( ownerClass, selfMC, mbs, fmpt.express);
                    //    }
                    //    break;
                    case FileMetaTermExpress fmte:
                        {
                            //Debug.Write("Error CreateExpressNode 创建表达项不能为符号");
                            cep.ownerMetaBase = ownerBase;
                            men = CreateExpressNode(cep);
                            return men;
                        }
                    case FileMetaBracketTerm fmbt:
                        {
                            //解析成这样是因为 在[] 中允许多个值的像 [1,2,3] 这种的
                            var maen = new MetaArrayExpressNode(fmbt, ownerBase, cep.ownerMBS, cep.metaType, cep.equalMetaVariable);                           
                            return maen;
                        }
                    default:
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error CreateExpressNode 创建表达项不能为符号");
                        break;
                }
            }
            else
            {
                CreateExpressParam clonecep = new CreateExpressParam(cep);
                clonecep.fme = root.left;
                MetaExpressNodeBase leftNode = CreateExpressNode(clonecep);
                clonecep.fme = root.right;
                MetaExpressNodeBase rightNode = CreateExpressNode(clonecep);

                if (leftNode != null && rightNode != null)
                {
                    if (root is FileMetaSymbolTerm)
                    {
                        return new MetaOpExpressNode(root as FileMetaSymbolTerm, cep.metaType, leftNode, rightNode);
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, " Error VisitFileMetaExpress fileMetaNode 不是符号!!");
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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, " Error VisitFileMetaExpress left and right都为空!!");
                }
                return null;
            }
            return null;
        }
        public static MetaExpressNodeBase CreateOptimizeAfterExpress( MetaExpressNodeBase men, ExpressOptimizeConfig config = null )
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
                case MetaAsIsExpressNode asisExpress:
                    {

                    }
                    break;
                default:
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Optimaze don't support that ExpressType");
                    }
                    break;
            }
            return men;
        }
       
        public static MetaExpressNodeBase ConvertNewExpress( MetaExpressNodeBase oldmen, MetaType mdt, MetaVariable mv )  
        {

            MetaExpressNodeBase menNew = oldmen;
            if (oldmen.convertNewExpressNode == true)
            {
                var mcen = oldmen as MetaCallLinkExpressNode;
                if( mcen == null )
                {
                    Debug.Assert(false, "老类型不是CallLinkExpressNode");
                    return null;
                }
                var menNew1 = new MetaNewObjectExpressNode(mdt, mcen);
                menNew1.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });

                menNew1.CalcReturnType();
                menNew1.CheckDefineVariableMetaTypeAndContentMetaType();
                menNew = menNew1;
            }
            else if (oldmen is MetaArrayExpressNode maen)
            {
                var menNew1 = new MetaNewObjectExpressNode(mdt, maen, oldmen.ownerMetaBase, oldmen.ownerMetaBlockStatements );
                menNew1.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                menNew1.CalcReturnType();
                menNew1.CheckDefineVariableMetaTypeAndContentMetaType();
                menNew = menNew1;
            }
            else if (oldmen is MetaAnonDataExpressNode maden)
            {         
                var menNew1 = MetaNewObjectExpressNode.CreateFromAnonymousMetaData(
                    maden.metaData,
                    oldmen.ownerMetaBase,
                    oldmen.ownerMetaBlockStatements,
                    mv);
                menNew1.SetToken(maden.token);
                menNew1.CheckDefineVariableMetaTypeAndContentMetaType();
                menNew = menNew1;
            }
            else if( oldmen.convertCallExpressNode )
            {
                // if this constant node contains parsed string parts (interpolations),
                // fold them into a chain of Add operations: left + right + ...
                var mce = oldmen as MetaConstExpressNode;
                if (mce != null && mce.stringParseExpressList != null && mce.stringParseExpressList.Count > 0)
                {
                    MetaExpressNodeBase acc = mce.stringParseExpressList[0];
                    for (int i = 1; i < mce.stringParseExpressList.Count; i++)
                    {
                        var right = mce.stringParseExpressList[i];
                        acc = new MetaOpExpressNode(acc, right, ELeftRightOpSign.Add);
                    }
                    menNew = acc;
                    // make sure the newly created tree is parsed and typed
                    menNew.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                    menNew.CalcReturnType();
                }
            }
            

                return menNew;
        }
        /*
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
            MetaType retmt = mcl.GetMetaDefineType();
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
        */
        public static int CalcParseLevel( int level, MetaExpressNodeBase men )
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

        // Remove placeholders with id <= threshold, and for placeholders with id > threshold
        // subtract threshold from their id.
        // Example: input "{1} Name{1000} {2} Score:{1001} {3}", threshold=1000
        // output: " Name{2} Score:{1} "
        public static string NormalizeFormatPlaceholders(string str, int threshold = 1000)
        {
            if (string.IsNullOrEmpty(str)) return str;

            var regex = new Regex(@"\{(\d+)\}");
            var sb = new StringBuilder();
            int last = 0;
            foreach (Match m in regex.Matches(str))
            {
                sb.Append(str, last, m.Index - last);
                if (int.TryParse(m.Groups[1].Value, out int id))
                {
                    if (id >= threshold)
                    {
                        sb.Append('{');
                        sb.Append(id - threshold);
                        sb.Append('}');
                    }
                    // else skip
                }
                else
                {
                    sb.Append(m.Value);
                }
                last = m.Index + m.Length;
            }
            if (last < str.Length)
                sb.Append(str, last, str.Length - last);
            return sb.ToString();
        }


        public static bool TryAdjustConstExpressByDefineMetaType(MetaType defineMetaType, MetaConstExpressNode mcen)
        {
            if (mcen == null || defineMetaType == null)
            {
                return false;
            }

            var curEType = CoreMetaClassManager.GetETypeByMetaClass(defineMetaType.metaClass);

            if (curEType == EType.Object)
            {
                curEType = mcen.eType;
            }

            if (mcen.eType == curEType)
            {
                return true;
            }

            return TryAdjustConstExpressByDefineEType(mcen, curEType);
        }
        public static bool TryAdjustConstExpressByDefineEType(MetaConstExpressNode mcen, EType defineEType)
        {
            if (mcen == null)
            {
                return false;
            }

            if (defineEType == EType.Object)
            {
                return true;
            }

            var curEType = defineEType;
            var expEType = mcen.eType;
            Token token = mcen.token;

            if (expEType == EType.Null)
            {
                return true;
            }

            if (NumberManager.IsNumericEType(curEType) && NumberManager.IsNumericEType(expEType))
            {
                return NumberManager.TryAdjustConstExpressToNumericTarget(mcen, curEType, expEType, token);
            }

            if (expEType != curEType)
            {
                if (NumberManager.TryConvertConstValueByEType(curEType, mcen.value, out var convertedValue))
                {
                    mcen.SetConstValue(curEType, convertedValue);
                    return true;
                }

                if (NumberManager.IsRadixNumberLiteral(mcen)
                    && NumberManager.TryConvertRadixUnsignedToSignedByEType(curEType, mcen.value, out var radixConvertedValue))
                {
                    mcen.SetConstValue(curEType, radixConvertedValue);
                    return true;
                }

                Log.AddMetaCoreLog(LID.MetaCoreExpressTypeGEDefineType, token, (mcen.value?.ToString() ?? "null"), curEType.ToString(), expEType.ToString());
                return false;
            }

            return true;
        }

    }
}