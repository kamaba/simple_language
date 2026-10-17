import Std;
import Core;

namespace SLang
{
    # ============================================================================
    # SLang/Server/Router.sl — 路由器（P1）
    # 设计契约：SERVER_DESIGN.md §4.2 / §6.2
    #
    # 匹配规则：pattern 按 '/' 拆段；{name} 段匹配任意非空单段并捕获；
    #   优先级 = 精确段 > 参数段（静态段多者优先，同数先注册者优先）；
    #   HEAD 视同 GET；method "ANY" 接受全部动词；
    #   判定序：路径命中但动词不符 → 405（+Allow）；全无匹配 → 404；
    #   冲突检测：start() 时对 (method, pattern) 去重（ServerError.DuplicateRoute）。
    # WS 路由共用同一套 {param} 语法（§4.6 s.pathParam）。
    # 形态：Router/RouteInfo 均为静态/模块内构造（跨模块多实例化隔离不可靠，
    # [2026-10-17] 复盘 ①）；RouteGroup 由 SLang.Server.group() 模块内构造
    # （HttpResponse 同款安全模式），实例方法只做前缀拼接转发。
    # ============================================================================

    # 路由记录（模块内构造；字段公开可读写——文档元数据 P3 扩展位）
    public class RouteInfo
    {
        # "GET"/"POST"... 或 "ANY"（全动词）/ "WS"（WebSocket 升级路由）
        public string method = ""
        # 原始 pattern（如 "/api/user/{id}"）
        public string pattern = ""
        # 处理器（HTTP 双参 Function(req,res)；WS 单参 Function(session)）
        public Function handler = null
        # ── 文档元数据（P3 OpenApi 消费；字段直写）──
        public string summary = ""
        public string tags = ""
        public string desc = ""

        # ── 匹配预编译（模块内部）──
        List<string> _segs = null
        List<string> _paramNames = null
        # 参数段在 _segs 中的下标（extractParams 对位用：第 k 个参数名取
        # 路径 _paramSegIdx[k] 段，勿按参数序号直取路径段——/a/{x}/{y} 中
        # 参数 0 在路径段 1）
        List<Int32> _paramSegIdx = null
        Int32 _staticCount = 0
        bool _hasParams = false
    }

    public class Router
    {
        # HTTP 路由表（线性扫描；v1 规模假设 < 200 条，前缀树为 P5 优化项）
        static List<RouteInfo> _routes = null
        # WS 升级路由表
        static List<RouteInfo> _wsRoutes = null
        # 引擎构造期标志（未 configure 时表未建）
        static bool _ready = false

        # ── 初始化 / 清理（HttpServerCore.configure/reset 调用）──

        static void _ensure()
        {
            if _routes == null
            {
                _routes = new()
            }
            if _wsRoutes == null
            {
                _wsRoutes = new()
            }
            _ready = true
        }

        # 清空全部路由（HTTP + WS；stop 后重新配置用）
        public static void clear()
        {
            _routes = new()
            _wsRoutes = new()
        }

        # ── 注册 ──

        # HTTP 路由注册（method 大小写不敏感；"ANY" 全动词）；返回 RouteInfo
        #（文档元数据可链式后写：r = Router.add(...); r.summary = "..."）
        public static RouteInfo add( string method, string pattern, Function handler )
        {
            Router._ensure()
            RouteInfo r = Router._build( method, pattern, handler )
            _routes.add( r )
            ret r
        }

        # WS 升级路由注册（pattern 同一套 {param} 语法）
        public static RouteInfo addWs( string pattern, Function handler )
        {
            Router._ensure()
            RouteInfo r = Router._build( "WS", pattern, handler )
            _wsRoutes.add( r )
            ret r
        }

        # 注册项预编译：段拆分 + 参数名提取 + 静态段计数
        static RouteInfo _build( string method, string pattern, Function handler )
        {
            RouteInfo r = RouteInfo()
            r.method = method
            r.pattern = pattern
            r.handler = handler
            r._segs = new()
            r._paramNames = new()
            r._paramSegIdx = new()
            r._staticCount = 0
            r._hasParams = false
            Router._splitSegments( pattern, r._segs )
            Int32 n = r._segs.length
            for Int32 i = 0, i < n, i = i + 1
            {
                string seg = r._segs[i]
                Int32 sl = SystemStringLength( seg )
                if sl > 2 && SystemStringCharCodeAt( seg, 0 ) == 123 && SystemStringCharCodeAt( seg, sl - 1 ) == 125
                {
                    # {name} 参数段
                    r._paramNames.add( SystemStringRange( seg, 1, sl - 1 ) )
                    r._paramSegIdx.add( i )
                    r._hasParams = true
                }
                else
                {
                    r._staticCount = r._staticCount + 1
                }
            }
            ret r
        }

        # ── 匹配 ──

        # HTTP 匹配（HEAD 视同 GET；ANY 全动词）；命中者中按静态段多者优先
        # （§4.2 精确段 > 参数段；同数先注册者优先）；无命中返回 null
        # （404/405 区分由 findBest：路径有注册但动词不符 → findBest 非空 → 405）
        public static RouteInfo match( string method, string path )
        {
            string lookup = method
            if Net.Uri.eqFold( method, "HEAD" )
            {
                lookup = "GET"
            }
            RouteInfo best = null
            Int32 bestStatic = 0 - 1
            Int32 n = _routes.length
            for Int32 i = 0, i < n, i = i + 1
            {
                RouteInfo r = _routes[i]
                if Net.Uri.eqFold( r.method, lookup ) || Net.Uri.eqFold( r.method, "ANY" )
                {
                    if Router._matchPattern( r, path )
                    {
                        if r._staticCount > bestStatic
                        {
                            best = r
                            bestStatic = r._staticCount
                        }
                    }
                }
            }
            ret best
        }

        # 路径最优命中（忽略动词；405 判定与 Allow 头生成用）：
        # 静态段多者优先、同数先注册者优先
        public static RouteInfo findBest( string path )
        {
            RouteInfo best = null
            Int32 bestStatic = 0 - 1
            Int32 n = _routes.length
            for Int32 i = 0, i < n, i = i + 1
            {
                RouteInfo r = _routes[i]
                if Router._matchPattern( r, path )
                {
                    if r._staticCount > bestStatic
                    {
                        best = r
                        bestStatic = r._staticCount
                    }
                }
            }
            ret best
        }

        # 该路径已注册的全部动词（405 Allow 头用；逗号分隔；ANY 展开全表）
        public static string allowMethods( string path )
        {
            string allow = ""
            Int32 n = _routes.length
            for Int32 i = 0, i < n, i = i + 1
            {
                RouteInfo r = _routes[i]
                if Router._matchPattern( r, path )
                {
                    if Net.Uri.eqFold( r.method, "ANY" )
                    {
                        ret "GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS"
                    }
                    if SystemStringLength( allow ) > 0
                    {
                        allow = allow + ", "
                    }
                    allow = allow + r.method
                }
            }
            ret allow
        }

        # WS 路由匹配（仅路径；无命中返回 null → 引擎 400）
        public static RouteInfo findWs( string path )
        {
            Int32 n = _wsRoutes.length
            for Int32 i = 0, i < n, i = i + 1
            {
                RouteInfo r = _wsRoutes[i]
                if Router._matchPattern( r, path )
                {
                    ret r
                }
            }
            ret null
        }

        # 提取路径参数（route 命中 path 后调用；无参数路由返回空 Map）
        public static Map<string,string> extractParams( RouteInfo route, string path )
        {
            Map<string,string> m = new()
            if route == null || route._hasParams == false
            {
                ret m
            }
            List<string> segs = new()
            Router._splitSegments( path, segs )
            Int32 pn = route._paramNames.length
            Int32 sn = segs.length
            for Int32 i = 0, i < pn, i = i + 1
            {
                string name = route._paramNames[i]
                Int32 segIdx = route._paramSegIdx[i]
                if segIdx < sn
                {
                    m[name] = segs[segIdx]
                }
            }
            ret m
        }

        # 冲突检测（start() 调用）：同 method+pattern 重复注册返回首个重复
        # 的 pattern 描述（调用方抛 ServerError.DuplicateRoute）；无重复返回 null
        public static string checkDuplicate()
        {
            Int32 n = _routes.length
            for Int32 i = 0, i < n, i = i + 1
            {
                RouteInfo a = _routes[i]
                for Int32 k = i + 1, k < n, k = k + 1
                {
                    RouteInfo b = _routes[k]
                    if Net.Uri.eqFold( a.method, b.method ) && a.pattern == b.pattern
                    {
                        ret a.method + " " + a.pattern
                    }
                }
            }
            Int32 wn = _wsRoutes.length
            for Int32 i = 0, i < wn, i = i + 1
            {
                RouteInfo a = _wsRoutes[i]
                for Int32 k = i + 1, k < wn, k = k + 1
                {
                    RouteInfo b = _wsRoutes[k]
                    if a.pattern == b.pattern
                    {
                        ret "WS " + a.pattern
                    }
                }
            }
            ret null
        }

        # ── 内部：段拆分与模式匹配 ──

        # pattern/path 按 '/' 拆段（空串/仅 '/' → 空表；不产生空段）
        static void _splitSegments( string path, List<string> outSegs )
        {
            Int32 len = SystemStringLength( path )
            Int32 start = -1
            for Int32 i = 0, i <= len, i = i + 1
            {
                if i == len || SystemStringCharCodeAt( path, i ) == 47
                {
                    if start >= 0 && i > start
                    {
                        outSegs.add( SystemStringRange( path, start, i ) )
                    }
                    start = -1
                }
                elif start < 0
                {
                    start = i
                }
            }
        }

        # 段数一致 + 逐段匹配（{param} 匹配任意非空单段；字面段精确相等）
        static bool _matchPattern( RouteInfo route, string path )
        {
            if route == null
            {
                ret false
            }
            List<string> segs = new()
            Router._splitSegments( path, segs )
            Int32 rn = route._segs.length
            Int32 pn = segs.length
            if rn != pn
            {
                ret false
            }
            for Int32 i = 0, i < rn, i = i + 1
            {
                string want = route._segs[i]
                string got = segs[i]
                Int32 wl = SystemStringLength( want )
                if wl > 2 && SystemStringCharCodeAt( want, 0 ) == 123 && SystemStringCharCodeAt( want, wl - 1 ) == 125
                {
                    # 参数段：非空即命中
                    if SystemStringLength( got ) == 0
                    {
                        ret false
                    }
                }
                else
                {
                    if want != got
                    {
                        ret false
                    }
                }
            }
            ret true
        }
    }

    # ============================================================================
    # RouteGroup — 前缀分组（Spring @RequestMapping 形态；§4.2）
    # 由 SLang.Server.group() 构造（模块内 new，HttpResponse 安全模式）；
    # 实例方法只做前缀拼接后转发 Router.add。
    # ============================================================================
    public class RouteGroup
    {
        string _prefix = ""

        _init_( string prefix )
        {
            # 规范化：确保前缀以 '/' 起始、不以 '/' 结尾（拼接免二次处理）
            string p = prefix
            if p == null
            {
                p = ""
            }
            if SystemStringLength( p ) == 0
            {
                p = "/"
            }
            if SystemStringCharCodeAt( p, 0 ) != 47
            {
                p = "/" + p
            }
            Int32 plen = SystemStringLength( p )
            if plen > 1 && SystemStringCharCodeAt( p, plen - 1 ) == 47
            {
                p = SystemStringRange( p, 0, plen - 1 )
            }
            this._prefix = p
        }

        # 前缀 + 子路径（子路径不以 '/' 起始时自动补；"/" 子路径 = 裸前缀）
        string _join( string path )
        {
            if path == null || SystemStringLength( path ) == 0 || path == "/"
            {
                ret this._prefix
            }
            if SystemStringCharCodeAt( path, 0 ) == 47
            {
                ret this._prefix + path
            }
            ret this._prefix + "/" + path
        }

        public RouteInfo onGet( string path, Function handler )
        {
            ret Router.add( "GET", this._join( path ), handler )
        }

        public RouteInfo onPost( string path, Function handler )
        {
            ret Router.add( "POST", this._join( path ), handler )
        }

        public RouteInfo onPut( string path, Function handler )
        {
            ret Router.add( "PUT", this._join( path ), handler )
        }

        public RouteInfo onDelete( string path, Function handler )
        {
            ret Router.add( "DELETE", this._join( path ), handler )
        }

        public RouteInfo onPatch( string path, Function handler )
        {
            ret Router.add( "PATCH", this._join( path ), handler )
        }

        public RouteInfo onAny( string path, Function handler )
        {
            ret Router.add( "ANY", this._join( path ), handler )
        }

        # 原始形式（动态注册 / 方法变量）
        public RouteInfo on( string method, string path, Function handler )
        {
            ret Router.add( method, this._join( path ), handler )
        }

        # WS 升级路由（组内前缀）
        public RouteInfo websocket( string path, Function handler )
        {
            ret Router.addWs( this._join( path ), handler )
        }
    }
}
