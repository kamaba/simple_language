# AliasProbeLib - import-as / typealias 探针的库侧定义与测试函数
# 命名空间 AliasNS.Sub 提供被 import 的目标；
# 文件级 typealias 覆盖基础/链式/经 import 别名限定/enum/函数 各形态。

import AliasNS.Sub as SUB
import Std
# 模块别名：import Std as S → S.XXX 续解 Std 模块根下的路径；S.IO.XXX 续解模块下命名空间限定声明的成员
import Std as S
# 字符串 import + 别名：import "Std:IO" as IOx → 直接指向 Std 模块的 IO 命名空间
# （IO 命名空间真实成员 = Directory/Path/SearchOption，均为 .sl 显式限定声明；Console/FileMode/FileStream 在模块根）
import "Std:IO" as IOx

typealias LibIntList = List<int>
typealias LibMapSS = Map<string, string>
typealias LibAliasAlpha = SUB.Alpha
typealias LibChain = LibIntList
typealias LibBoxInt = Box<int>
typealias LibColor = Color3
typealias LibFuncTA = int Function( int, int )
# typealias 目标用 import as 模块别名 / 字符串 import 别名作前缀
typealias LibSFileMode = S.FileMode
typealias LibIOxSearch = IOx.SearchOption

enum Color3
{
    Red = 1
    Green = 2
    Blue = 3
}

Box<T>
{
    T value = null
    _init_( T v ) { this.value = v }
    T take() { ret this.value }
}

AliasNS.Sub.Alpha
{
    static int sid = 100
    static int twice( int v ) { ret v * 2 }
    int x = 1
    _init_( int ix = 0 ) { this.x = ix }
}

AliasProbeTests
{
    # ---- import NS as NSA ----
    static importAsNamespaceTest()
    {
        Console.println( "----- import as namespace -----" )
        a = SUB.Alpha( 5 )
        Console.println( "SUB.Alpha(5).x = " + a.x.toString() )
        Console.println( "SUB.Alpha.sid = " + SUB.Alpha.sid.toString() )
        Console.println( "SUB.Alpha.twice(21) = " + SUB.Alpha.twice( 21 ).toString() )
        SUB.Alpha ta = SUB.Alpha( 6 )
        Console.println( "ta.x = " + ta.x.toString() )
        tAlpha = SUB.Alpha.type
        Console.println( "SUB.Alpha.type = " + tAlpha.toString() )
    }

    # ---- import 模块别名 (import Std as S) / 字符串 import 别名 (import "Std:IO" as IOx) ----
    static importAsModuleTest()
    {
        Console.println( "----- import module as alias / string import -----" )
        # 表达式位置：模块别名前缀链（Console/FileMode/FileStream 注册在 Std 模块根）
        S.Console.println( "S.Console.println works" )
        Console.println( "S.FileMode.Append = " + S.FileMode.Append.toString() )
        # 四段链：模块别名 → IO 命名空间（.sl 限定声明成员）→ 类 → 静态方法
        Console.println( "S.IO.Directory.exists('.') = " + S.IO.Directory.exists( "." ).toString() )
        # 类型位置：模块别名前缀
        S.FileStream sfs = null
        Console.println( "sfs == null : " + ( sfs == null ).toString() )
        # 字符串 import 别名：指向 Std 模块 IO 命名空间（真实成员 Directory/Path/SearchOption）
        Console.println( "IOx.Directory.exists('.') = " + IOx.Directory.exists( "." ).toString() )
        Console.println( "IOx.SearchOption.TopDirectoryOnly = " + IOx.SearchOption.TopDirectoryOnly.toString() )
        # typealias 目标用模块别名 / 字符串 import 别名前缀（enum 成员访问验证别名指向）
        Console.println( "LibSFileMode.Create = " + LibSFileMode.Create.toString() )
        Console.println( "LibIOxSearch.TopDirectoryOnly = " + LibIOxSearch.TopDirectoryOnly.toString() )
    }

    # ---- typealias 基础 ----
    static typeAliasBasicTest()
    {
        Console.println( "----- typealias basic -----" )
        LibIntList l = LibIntList()
        l.add( 1 )
        l.add( 2 )
        Console.println( "LibIntList length = " + l.length.toString() )
        Console.println( "LibIntList[1] = " + l[1].toString() )
        LibMapSS m = LibMapSS()
        m.add( "k", "v" )
        Console.println( "LibMapSS[k] = " + m._getItem_( "k" ) )
        tLibList = LibIntList.type
        Console.println( "LibIntList.type = " + tLibList.toString() )
    }

    # ---- typealias 链 / 经 import 别名限定 / enum ----
    static typeAliasChainTest()
    {
        Console.println( "----- typealias chain / qualified / enum -----" )
        LibChain lc = LibChain()
        lc.add( 3 )
        Console.println( "LibChain[0] = " + lc[0].toString() )
        LibAliasAlpha laa = LibAliasAlpha( 8 )
        Console.println( "LibAliasAlpha(8).x = " + laa.x.toString() )
        Console.println( "LibColor.Red = " + LibColor.Red.toString() )
        LibBoxInt bi = LibBoxInt( 42 )
        Console.println( "LibBoxInt.take() = " + bi.take().toString() )
    }

    # ---- 函数类型别名 ----
    static LibFuncTA makeAdd()
    {
        var f = function( int a, int b )
        {
            ret a + b;
        };
        ret f;
    }
    static typeAliasFuncTest()
    {
        Console.println( "----- typealias function type -----" )
        LibFuncTA f = makeAdd()
        Console.println( "LibFuncTA(3,4) = " + f( 3, 4 ).toString() )
    }

    # ---- 工程级(.sp 顶级) typealias 跨文件可见性 ----
    static typeAliasProjectScopeTest()
    {
        Console.println( "----- typealias project scope (cross file) -----" )
        SpIntList pl = SpIntList()
        pl.add( 7 )
        Console.println( "SpIntList[0] = " + pl[0].toString() )
        SpAliasAlpha pa = SpAliasAlpha( 9 )
        Console.println( "SpAliasAlpha(9).x = " + pa.x.toString() )
        # .sp 顶级（Project 声明区）typealias 跨文件可见性
        SpTopKV tkv = SpTopKV()
        tkv.add( "k", "v" )
        Console.println( "SpTopKV[k] = " + tkv._getItem_( "k" ) )
        SpTopAlpha tpa = SpTopAlpha( 10 )
        Console.println( "SpTopAlpha(10).x = " + tpa.x.toString() )
    }
}
