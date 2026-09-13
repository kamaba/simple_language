# =========================================================================
# Image/Svg.sl —— SVG 矢量图文档模型
#
# 纯 SL 实现：
#   - viewBox 解析（"min-x min-y width height"，允许逗号分隔）
#   - **path `d` 属性解析**：命令字符 + 参数表（含隐式重复、负号当分隔符）
#     SVG 的 number 语法允许 "1-2"、".5"、"1e-3"，这里都处理了
# 依赖后端：
#   - parse()        ：XML 解析成文档树（复用 XML 解析器）
#   - rasterize()    ：路径光栅化（填充规则、描边、渐变、裁剪）
#
# 说明：所有类型都是 namespace 下的顶级类型（DB/Sqlite.sl 的硬约束）。
# =========================================================================

namespace Image
{
    # ── viewBox ───────────────────────────────────────────
    public class SvgViewBox
    {
        public Float32 x = 0.0f
        public Float32 y = 0.0f
        public Float32 width = 0.0f
        public Float32 height = 0.0f

        _init_()
        {
            this.x = 0.0f
            this.y = 0.0f
            this.width = 0.0f
            this.height = 0.0f
        }

        _init_( Float32 _x, Float32 _y, Float32 _w, Float32 _h )
        {
            this.x = _x
            this.y = _y
            this.width = _w
            this.height = _h
        }

        public get bool isValid()
        {
            ret this.width > 0.0f && this.height > 0.0f
        }

        public get Float32 aspect()
        {
            if this.height <= 0.0f
            {
                ret 1.0f
            }
            ret this.width / this.height
        }

        # "0 0 100 100" / "0,0,100,100" / " 0 0 100 100 "
        public static SvgViewBox parse( string text )
        {
            SvgViewBox vb = SvgViewBox()
            if text == null
            {
                ret vb
            }
            SvgPathCursor c = SvgPathCursor( text )
            vb.x = SvgPath.readNumber( c )
            vb.y = SvgPath.readNumber( c )
            vb.width = SvgPath.readNumber( c )
            vb.height = SvgPath.readNumber( c )
            ret vb
        }

        override string toString()
        {
            ret "SvgViewBox(" + this.x.toString() + " " + this.y.toString() + " " +
                this.width.toString() + " " + this.height.toString() + ")"
        }
    }

    # ── 解析游标（SL 无 ref/out 参数，用对象承载位置）──────
    public class SvgPathCursor
    {
        public string text = ""
        public Int32 pos = 0

        _init_( string s )
        {
            if s == null
            {
                this.text = ""
            }
            else
            {
                this.text = s
            }
            this.pos = 0
        }

        public get bool atEnd()
        {
            ret this.pos >= this.text.length()
        }

        # 当前字符的 Unicode 码位，越界返回 0
        public get Int32 current()
        {
            if this.atEnd()
            {
                ret 0
            }
            ret SystemStringCharCodeAt( this.text, this.pos )
        }

        public void advance( Int32 n )
        {
            this.pos = this.pos + n
        }
    }

    # ── 路径命令 ───────────────────────────────────────────
    public class SvgPathCommand
    {
        # 命令字符的 Unicode 码位：M(77) L(76) H(72) V(86) C(67)
        # S(83) Q(81) T(84) A(65) Z(90)；小写 = 相对坐标
        public Int32 code = 0
        # 参数（按命令语义分组，例如 C 为 6 个）
        public Array<Float32> args = null

        _init_()
        {
            this.code = 0
            this.args = Array<Float32>( 0 )
        }

        _init_( Int32 _code, Array<Float32> _args )
        {
            this.code = _code
            if _args == null
            {
                this.args = Array<Float32>( 0 )
            }
            else
            {
                this.args = _args
            }
        }

        public get Int32 argCount()
        {
            ret this.args.length
        }

        # 小写字母（97..122）= 相对坐标
        public get bool isRelative()
        {
            ret this.code >= 97 && this.code <= 122
        }

        public get bool isClose()
        {
            ret this.code == 90 || this.code == 122
        }

        override string toString()
        {
            ret "SvgPathCommand(code=" + this.code.toString() +
                ", relative=" + this.isRelative.toString() +
                ", args=" + this.argCount().toString() + ")"
        }
    }

    # ── 路径（纯 SL 解析器）────────────────────────────────
    public class SvgPath
    {
        public Array<SvgPathCommand> commands = null

        _init_()
        {
            this.commands = Array<SvgPathCommand>( 0 )
        }

        public get Int32 commandCount()
        {
            if this.commands == null
            {
                ret 0
            }
            ret this.commands.length
        }

        # 分隔符：空白 + 逗号
        public static bool isSeparator( Int32 ch )
        {
            ret ch == 32 || ch == 9 || ch == 10 || ch == 13 || ch == 44
        }

        public static bool isCommandChar( Int32 ch )
        {
            ret ( ch >= 65 && ch <= 90 ) || ( ch >= 97 && ch <= 122 )
        }

        # 每个命令需要的参数个数
        public static Int32 argCountOf( Int32 ch )
        {
            if ch == 77 || ch == 109      # M m
            {
                ret 2
            }
            if ch == 76 || ch == 108      # L l
            {
                ret 2
            }
            if ch == 84 || ch == 116      # T t（平滑二次）
            {
                ret 2
            }
            if ch == 72 || ch == 104      # H h
            {
                ret 1
            }
            if ch == 86 || ch == 118      # V v
            {
                ret 1
            }
            if ch == 67 || ch == 99       # C c
            {
                ret 6
            }
            if ch == 83 || ch == 115      # S s（平滑三次）
            {
                ret 4
            }
            if ch == 81 || ch == 113      # Q q
            {
                ret 4
            }
            if ch == 65 || ch == 97       # A a（弧线）
            {
                ret 7
            }
            # Z z 及未知命令
            ret 0
        }

        # 读一个 number（支持 "1-2"、".5"、"1e-3"）
        public static Float32 readNumber( SvgPathCursor c )
        {
            if c == null
            {
                ret 0.0f
            }
            # 跳过分隔符
            while !c.atEnd() && SvgPath.isSeparator( c.current )
            {
                c.advance( 1 )
            }
            if c.atEnd()
            {
                ret 0.0f
            }
            Int32 start = c.pos

            # 前导符号
            Int32 ch = c.current
            if ch == 43 || ch == 45
            {
                c.advance( 1 )
            }

            bool done = false
            while !c.atEnd() && !done
            {
                Int32 k = c.current
                if k >= 48 && k <= 57
                {
                    c.advance( 1 )
                }
                elif k == 46
                {
                    # 小数点
                    c.advance( 1 )
                }
                elif k == 101 || k == 69
                {
                    # 指数 e / E，后可跟符号
                    c.advance( 1 )
                    if !c.atEnd()
                    {
                        Int32 nk = c.current
                        if nk == 43 || nk == 45
                        {
                            c.advance( 1 )
                        }
                    }
                }
                else
                {
                    done = true
                }
            }

            if c.pos <= start
            {
                ret 0.0f
            }
            string token = c.text.range( start, c.pos )
            ret SystemConvertFloat32( token )
        }

        # 解析 path 的 d 属性
        public static SvgPath parse( string d )
        {
            SvgPath path = SvgPath()
            if d == null || d.length() == 0
            {
                ret path
            }
            SvgPathCursor c = SvgPathCursor( d )
            List<SvgPathCommand> list = List<SvgPathCommand>( 16 )
            Int32 lastCmd = 0

            while !c.atEnd()
            {
                Int32 ch = c.current
                bool gotCmd = false

                if SvgPath.isCommandChar( ch )
                {
                    lastCmd = ch
                    c.advance( 1 )
                    gotCmd = true
                }
                elif SvgPath.isSeparator( ch )
                {
                    # 分隔符：直接吃掉，等下一个命令或隐式重复
                    c.advance( 1 )
                    gotCmd = false
                }
                elif lastCmd != 0
                {
                    # 隐式重复上一个命令；M/m 之后的连续坐标按 L/l 处理
                    if lastCmd == 77
                    {
                        lastCmd = 76
                    }
                    elif lastCmd == 109
                    {
                        lastCmd = 108
                    }
                    gotCmd = true
                }
                else
                {
                    # 还没有命令字符，丢弃无法识别的内容
                    c.advance( 1 )
                    gotCmd = false
                }

                if gotCmd
                {
                    Int32 need = SvgPath.argCountOf( lastCmd )
                    Array<Float32> args = Array<Float32>( need )
                    Int32 i = 0
                    while i < need
                    {
                        args[ i ] = SvgPath.readNumber( c )
                        i = i + 1
                    }
                    list.add( SvgPathCommand( lastCmd, args ) )
                }
            }

            Int32 n = list.length()
            path.commands = Array<SvgPathCommand>( n )
            Int32 k = 0
            while k < n
            {
                path.commands[ k ] = list.at( k )
                k = k + 1
            }
            ret path
        }

        override string toString()
        {
            ret "SvgPath(cmds=" + this.commandCount().toString() + ")"
        }
    }

    # ── 元素基类 ───────────────────────────────────────────
    public class SvgElement
    {
        public string id = ""
        public string tag = ""
        public Color fill = null
        public Color stroke = null
        public Float32 strokeWidth = 1.0f
        public Float32 opacity = 1.0f
        public bool visible = true
        # 原始 transform 属性文本（矩阵运算交给后端或上层）
        public string transform = ""

        _init_()
        {
            this.id = ""
            this.tag = ""
            this.fill = null
            this.stroke = null
            this.strokeWidth = 1.0f
            this.opacity = 1.0f
            this.visible = true
            this.transform = ""
        }

        override string toString()
        {
            ret "SvgElement(<" + this.tag + "> id=" + this.id + ")"
        }
    }

    # 矩形 / 圆 / 椭圆 / 线 / 多边形 / 折线 / 文本 只保留几何字段
    public class SvgRect extends SvgElement
    {
        public Float32 x = 0.0f
        public Float32 y = 0.0f
        public Float32 width = 0.0f
        public Float32 height = 0.0f
        public Float32 rx = 0.0f
        public Float32 ry = 0.0f

        _init_()
        {
            this.tag = "rect"
            this.x = 0.0f
            this.y = 0.0f
            this.width = 0.0f
            this.height = 0.0f
            this.rx = 0.0f
            this.ry = 0.0f
        }
    }

    public class SvgCircle extends SvgElement
    {
        public Float32 cx = 0.0f
        public Float32 cy = 0.0f
        public Float32 r = 0.0f

        _init_()
        {
            this.tag = "circle"
            this.cx = 0.0f
            this.cy = 0.0f
            this.r = 0.0f
        }
    }

    public class SvgEllipse extends SvgElement
    {
        public Float32 cx = 0.0f
        public Float32 cy = 0.0f
        public Float32 rx = 0.0f
        public Float32 ry = 0.0f

        _init_()
        {
            this.tag = "ellipse"
            this.cx = 0.0f
            this.cy = 0.0f
            this.rx = 0.0f
            this.ry = 0.0f
        }
    }

    public class SvgLine extends SvgElement
    {
        public Float32 x1 = 0.0f
        public Float32 y1 = 0.0f
        public Float32 x2 = 0.0f
        public Float32 y2 = 0.0f

        _init_()
        {
            this.tag = "line"
            this.x1 = 0.0f
            this.y1 = 0.0f
            this.x2 = 0.0f
            this.y2 = 0.0f
        }
    }

    public class SvgPolygon extends SvgElement
    {
        # 扁平点表：[x0,y0,x1,y1,...]
        public Array<Float32> points = null

        _init_()
        {
            this.tag = "polygon"
            this.points = Array<Float32>( 0 )
        }

        public get Int32 pointCount()
        {
            if this.points == null
            {
                ret 0
            }
            ret this.points.length / 2
        }
    }

    public class SvgText extends SvgElement
    {
        public string content = ""
        public Float32 x = 0.0f
        public Float32 y = 0.0f
        public Float32 fontSize = 12.0f
        public string fontFamily = ""
        public string anchor = "start"

        _init_()
        {
            this.tag = "text"
            this.content = ""
            this.x = 0.0f
            this.y = 0.0f
            this.fontSize = 12.0f
            this.fontFamily = ""
            this.anchor = "start"
        }
    }

    public class SvgPathElement extends SvgElement
    {
        # 原始 d 文本（保留，便于序列化回去）
        public string d = ""
        public SvgPath parsedPath = null
        # 填充规则：nonzero / evenodd
        public string fillRule = "nonzero"

        _init_()
        {
            this.tag = "path"
            this.d = ""
            this.parsedPath = null
            this.fillRule = "nonzero"
        }

        public void parseD()
        {
            this.parsedPath = SvgPath.parse( this.d )
        }
    }

    public class SvgGroup extends SvgElement
    {
        public List<SvgElement> children = null

        _init_()
        {
            this.tag = "g"
            this.children = List<SvgElement>( 8 )
        }

        public void add( SvgElement e )
        {
            if e != null
            {
                this.children.add( e )
            }
        }

        public get Int32 childCount()
        {
            if this.children == null
            {
                ret 0
            }
            ret this.children.length()
        }
    }

    # ── 文档 ───────────────────────────────────────────────
    public class SvgDocument
    {
        public Float32 width = 0.0f
        public Float32 height = 0.0f
        public SvgViewBox viewBox = null
        public SvgGroup root = null
        public string title = ""
        public string description = ""

        _init_()
        {
            this.width = 0.0f
            this.height = 0.0f
            this.viewBox = SvgViewBox()
            this.root = SvgGroup()
            this.title = ""
            this.description = ""
        }

        public get bool isValid()
        {
            ret this.root != null
        }

        # 没有显式 width/height 时退回 viewBox 尺寸
        public Float32 effectiveWidth()
        {
            if this.width > 0.0f
            {
                ret this.width
            }
            if this.viewBox != null
            {
                ret this.viewBox.width
            }
            ret 0.0f
        }

        public Float32 effectiveHeight()
        {
            if this.height > 0.0f
            {
                ret this.height
            }
            if this.viewBox != null
            {
                ret this.viewBox.height
            }
            ret 0.0f
        }

        override string toString()
        {
            ret "SvgDocument(" + this.effectiveWidth().toString() + "x" +
                this.effectiveHeight().toString() + ")"
        }
    }

    # ── SVG 门面 ───────────────────────────────────────────
    public class Svg
    {
        # 是否疑似 SVG 文本
        public static bool isSvgText( string text )
        {
            if text == null || text.length() < 5
            {
                ret false
            }
            ret text.range( 0, 5 ) == "<?xml" || text.range( 0, 4 ) == "<svg"
        }

        # XML -> 文档树（后端）
        public static SvgDocument parse( string xml )
        {
            if xml == null
            {
                ret null
            }
            object result = SystemCallExternalFunction( "Image.svgParse", xml )
            ret result as SvgDocument
        }

        # 光栅化成位图（后端）
        public static ImageBuffer rasterize( SvgDocument doc, Int32 width, Int32 height )
        {
            if doc == null
            {
                ret null
            }
            object result = SystemCallExternalFunction( "Image.svgRasterize",
                doc, width, height )
            ret result as ImageBuffer
        }

        # 便捷：XML 文本直接光栅化
        public static ImageBuffer rasterizeText( string xml, Int32 width, Int32 height )
        {
            SvgDocument doc = Svg.parse( xml )
            ret Svg.rasterize( doc, width, height )
        }

        override string toString()
        {
            ret "Svg"
        }
    }
}
