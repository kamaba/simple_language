#
# Path 路径处理工具类（纯静态）
# 参考 C# System.IO.Path
#
# 说明：
#   - 底层路径运算来自 C VM 系统调用（SystemPath*，注册见 Std.jsonc systemCalls）
#   - 命名对照 C#：IsPathRooted -> isPathRooted、GetDirectoryName -> getDirectoryName 等
#   - 平台探测复用 Core 的 SystemPlatformEnvGetInt("os")（1=window 2=linux 3=mac）
#

public class IO.Path
{
    # ── 平台分隔符 ─────────────────────────────────────

    # 目录分隔符：Windows 为 "\"，Unix 为 "/"
    # 类似 C# Path.DirectorySeparatorChar
    public static get string directorySeparatorChar()
    {
        if SystemPlatformEnvGetInt( "os" ) == 1
        {
            ret "\\"
        }
        ret "/"
    }

    # 备用目录分隔符：两端均为 "/"
    # 类似 C# Path.AltDirectorySeparatorChar
    public static get string altDirectorySeparatorChar()
    {
        ret "/"
    }

    # 环境变量列表分隔符：Windows 为 ";"，Unix 为 ":"
    # 类似 C# Path.PathSeparator
    public static get string pathSeparator()
    {
        if SystemPlatformEnvGetInt( "os" ) == 1
        {
            ret ";"
        }
        ret ":"
    }

    # 卷分隔符：Windows 为 ":"，Unix 为 "/"
    # 类似 C# Path.VolumeSeparatorChar
    public static get string volumeSeparatorChar()
    {
        if SystemPlatformEnvGetInt( "os" ) == 1
        {
            ret ":"
        }
        ret "/"
    }

    # ── 路径拼接 ─────────────────────────────────────

    # 将多段路径拼为一个路径
    # 空段被跳过；后段为绝对路径时覆盖已拼接的前段（与 C# 一致）
    # 类似 C# Path.Combine(params string[] paths)
    static string combine( params string[] paths )
    {
        if paths == null || paths.length == 0
        {
            ret ""
        }

        string result = ""
        for i = 0, i < paths.length, i++
        {
            string p = paths[i]
            if p == null || p.length == 0
            {
                continue
            }

            if result.length == 0
            {
                result = p
            }
            elif SystemPathIsAbsolute( p )
            {
                result = p
            }
            else
            {
                result = SystemPathCombine( result, p )
            }
        }
        ret result
    }

    # ── 路径分解 ─────────────────────────────────────

    # 取目录部分（不含末尾分隔符；无分隔符时返回 ""）
    # 类似 C# Path.GetDirectoryName(string path)
    static string getDirectoryName( string path )
    {
        ret SystemPathGetDirectory( path )
    }

    # 取文件名部分（含扩展名；无分隔符时返回原串）
    # 类似 C# Path.GetFileName(string path)
    static string getFileName( string path )
    {
        ret SystemPathGetFilename( path )
    }

    # 取文件名（不含扩展名）部分
    # 类似 C# Path.GetFileNameWithoutExtension(string path)
    static string getFileNameWithoutExtension( string path )
    {
        string name = SystemPathGetFilename( path )
        string ext = getExtensionOfName( name )
        if ext.length > 0
        {
            ret name.slice( 0, name.length - ext.length )
        }
        ret name
    }

    # 取扩展名（含前导点 "."，如 ".txt"；无扩展名返回 ""）
    # 只在文件名部分查找点，与 C# 一致（"dir.d/file" 无扩展名）
    # 类似 C# Path.GetExtension(string path)
    static string getExtension( string path )
    {
        ret getExtensionOfName( SystemPathGetFilename( path ) )
    }

    # 在文件名中找扩展名（含点）
    # ".gitignore" 这类以点开头的隐藏文件名不算扩展名，返回 ""
    static string getExtensionOfName( string name )
    {
        if name == null
        {
            ret ""
        }
        string ext = SystemPathGetExtension( name )
        # 点串即整个文件名 -> 隐藏文件名
        if ext.length == name.length
        {
            ret ""
        }
        ret ext
    }

    # 判断路径是否含扩展名
    # 类似 C# Path.HasExtension(string path)
    static bool hasExtension( string path )
    {
        ret getExtension( path ).length > 0
    }

    # 替换扩展名（extension 含不含前导点均可；null/空串表示移除扩展名）
    # 类似 C# Path.ChangeExtension(string path, string extension)
    static string changeExtension( string path, string extension )
    {
        if path == null
        {
            ret ""
        }

        string name = SystemPathGetFilename( path )
        string ext = getExtensionOfName( name )

        # 前缀（含分隔符）保持原样，不改写用户使用的分隔符
        Int32 prefixLen = path.length - name.length
        string prefix = path.slice( 0, prefixLen )

        # 去掉旧扩展名
        if ext.length > 0
        {
            name = name.slice( 0, name.length - ext.length )
        }

        # 拼接新扩展名（未以点开头则补点，与 C# 一致）
        if extension != null && extension.length > 0
        {
            if SystemStringCharCodeAt( extension, 0 ) != 46   # '.'
            {
                extension = "." + extension
            }
            name = name + extension
        }

        ret prefix + name
    }

    # ── 根与绝对路径 ─────────────────────────────────

    # 取路径的根部分（"/" 或 "C:\"）；相对路径返回 ""
    # 类似 C# Path.GetPathRoot(string path)
    static string getPathRoot( string path )
    {
        if path == null || path.length == 0
        {
            ret ""
        }
        if SystemStringCharCodeAt( path, 0 ) == 47   # '/'
        {
            ret "/"
        }
        if path.length >= 3 && SystemStringCharCodeAt( path, 1 ) == 58   # ':'
        {
            Int32 c = SystemStringCharCodeAt( path, 2 )
            if c == 47 || c == 92   # '/' or '\'
            {
                ret path.slice( 0, 3 )
            }
        }
        ret ""
    }

    # 判断是否绝对路径（"/..." 或 "X:/..."、"X:\..."）
    # 类似 C# Path.IsPathRooted(string path)
    static bool isPathRooted( string path )
    {
        ret SystemPathIsAbsolute( path )
    }

    # 取绝对路径（相对路径按当前工作目录展开）
    # 类似 C# Path.GetFullPath(string path)
    static string getFullPath( string path )
    {
        ret SystemPathGetFull( path )
    }

    # ── 临时路径 / 随机名 ────────────────────────────

    # 取临时目录（保证以目录分隔符结尾）
    # 依次尝试环境变量 TEMP、TMP，最后回退 "/tmp"
    # 类似 C# Path.GetTempPath()
    static string getTempPath()
    {
        string dir = SystemEnvironmentGetVariable( "TEMP" )
        if dir == null || dir.length == 0
        {
            dir = SystemEnvironmentGetVariable( "TMP" )
        }
        if dir == null || dir.length == 0
        {
            dir = "/tmp"
        }

        Int32 last = SystemStringCharCodeAt( dir, dir.length - 1 )
        if last != 47 && last != 92   # '/' '\'
        {
            if SystemPlatformEnvGetInt( "os" ) == 1
            {
                dir = dir + "\\"
            }
            else
            {
                dir = dir + "/"
            }
        }
        ret dir
    }

    # 生成随机文件名（8 位随机串 + "." + 3 位随机串，基于 GUID）
    # 类似 C# Path.GetRandomFileName()
    static string getRandomFileName()
    {
        # GUID 形如 "xxxxxxxx-xxxx-..."：取前 8 位 + "." + 随后 3 位
        string g = SystemGuidNewGuid()
        if g == null || g.length < 12
        {
            ret "tmp.tmp"
        }
        ret g.slice( 0, 8 ) + "." + g.slice( 9, 12 )
    }
}
