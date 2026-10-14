# SLang Server — HTTP / WebSocket 服务框架设计

> 状态：**设计稿 v2（待评审）** · v1 2026-10-14 · v2 2026-10-14（评审修订）
> 落位：`source/Front/Lib/SLang/Server/`（模块名 `SLangServer`，命名空间 `SLang`）
> 参照系：Python FastAPI（路由注册/自动文档）、Java Spring Boot（控制器/插件生态）、Node Express（Request/Response 双参签名）
> 关联：`md/lib/Std/net/Http.md`（Route 未接线说明）、`csimple_lang/md/design/ATTRIBUTE_DESIGN.md` §6.6（Route 另立项）、`csimple_lang/md/design/NETSTREAM_DESIGN.md`（Tcp 挂起协议）
>
> **v2 修订记录**（评审意见落位）：
> ① 路由注册改 `onGet` 家族——`get` / `set` 是 SL 关键字不能作方法名（`HttpClient.sl` 头注记先例）；
> ② `HttpContext` 拆分为 **`SLang.Request`（读）+ `SLang.Response`（写）**，处理器双参签名 `function( req, res )`，语义更明确；
> ③ 新增**请求体格式处理**（json / form / multipart / bin，按 Content-Type 自动分流，§4.4 / §6.4）；
> ④ 新增**文件上传** `SLang.UploadedFile`（multipart 解析 + saveAs/saveInto，§4.4 / §6.5）；
> ⑤ 明确**依赖边界：只引用 Core / Std 两个模块**，其它库一概不调（§1.1 G8）；
> ⑥ `ApiResult.data` 字段更名 `payload`（`data` 是关键字），输出 JSON 键名仍为 `"data"`（§6.7）；
> ⑦ 中间件放行改为 `SLang.Next` 类 `nx.go()`（`next` 是关键字，不能作参数名/方法名）；
> ⑧ 插件基类钩子去除 `virtual`——**语言无 `virtual` 关键字**，成员方法默认可被子类重写（`override` 标记、`final` 封禁），基类钩子直接 `public void` 空实现；
> ⑨ 补齐 **HTTP 流式传输**设计：响应侧 `res.stream()`（chunked）/ `res.sse()`（SSE）/ `res.sendFile()`（流式下载）落 P2（§4.5 / §6.6）；请求侧流式（边收边落盘上传、Range 断点续传）维持 P5。

---

## §0 摘要（30 秒）

在 `Lib/SLang/Server/` 新建 `SLangServer` 模块，提供 **HTTP + WebSocket 一体的应用服务框架**：

- **一个门面类 `SLang.Server`**：`Server()` 构造 → `app.onGet/onPost/...` 注册路由 → `app.run(8080)` 启动；
- **语法极简**（本文档第一目标）：路由处理器就是普通闭包 `function( SLang.Request req, SLang.Response res ) { ... }`——**Request 管读、Response 管写**，路径参数 / 查询参数 / JSON·form·bin 三种 body / JSON 返回 / 文件上传全是行级 API；
- **统一 JSON 封装** `ApiResult`（`{"code","message","data"}`）+ `res.jsonOk / jsonFail`；
- **请求体按 Content-Type 自动分流**：`application/json` / `application/x-www-form-urlencoded` / `multipart/form-data`（文件上传）/ `text/*` / 二进制；
- **文件上传**：`req.file("avatar")` → `UploadedFile.saveAs(path)`，multipart 全解析（boundary / part 头 / 文件名安全剥离）；
- **流式传输**：`res.stream()`（chunked 分块下发）/ `res.sse()`（Server-Sent Events，AI 流式输出标配）/ `res.sendFile()`（大文件流式下载不整载内存）；写挂起天然背压；请求侧流式上传为 P5；
- **内置 API 文档**：`/docs`（自包含 HTML 页面）+ `/openapi.json`（OpenAPI 3.0.3 规范，**可直接导入 Apifox / Postman / SwaggerHub**）；反向支持从 OpenAPI 文件导入接口定义；
- **插件模型**：中间件管道（`nx.go()` 放行）+ `ServerPlugin` 基类（Logger / CORS / StaticFiles 内置起步）；
- **P0–P3 纯 SL 层实现**，复用 `Net.TcpServer` / `WebSocketStream.accept` / `BaseJson`，**零 C VM 改动、零 Front 改动**；P4 attribute 控制器（`@Route`）才动 C VM；
- **只依赖 Core / Std** 两个模块（硬边界）。

```sl
# ── 全部核心能力一览（语法目标，详见 §4）──
import SLangServer;

var app = SLang.Server();                          # ① 服务
app.use( SLang.Plugins.logger() );                 # ③ 插件
app.use( SLang.Plugins.cors() );
app.use( SLang.Plugins.staticFiles( "./www" ) );

app.onGet( "/api/user/{id}", function( SLang.Request req, SLang.Response res )   # ② 路径注册
{
    var u = User( req.pathInt( "id" ), "tom" );
    res.jsonOk( u );                               # JSON 封装 {"code":0,"data":{...}}
} );

app.onPost( "/api/upload/avatar", function( SLang.Request req, SLang.Response res )   # ④ 文件上传
{
    SLang.UploadedFile f = req.file( "avatar" );
    if f == null
    {
        res.jsonFail( 400, "no file" );
        ret
    }
    f.saveAs( "./uploads/" + f.filename );
    res.jsonOk( f.filename );
} );

app.onGet( "/api/sse/progress", function( SLang.Request req, SLang.Response res )   # ⑤ 流式（SSE）
{
    SLang.SseWriter sse = res.sse();
    for Int32 i = 0, i <= 100, i = i + 10
    {
        sse.sendEvent( "progress", i.toString() );
        Coroutine.delay( 500 );
    }
    sse.close();
} );

app.websocket( "/ws/chat", function( SLang.WsSession s )       # WebSocket 路由
{
    while true
    {
        Net.WsMessage m = s.receive();
        if m == null
        {
            break
        }
        s.sendText( "echo: " + m.text() );
        m.release();
    }
} );

app.run( 8080 );                                   # 启动（/docs 自动挂载）
```

---

## §1 目标与非目标

### 1.1 目标

| # | 目标 | 度量 |
|---|------|------|
| G1 | **语法简单**（第一优先级）：一个类 + 闭包完成 HTTP/WS 服务 | 最小示例 ≤ 5 行；读 API 全挂 `req`、写 API 全挂 `res`，无散落工具类 |
| G2 | 服务启动 / 停止 / 生命周期钩子 | `run / start / stop` + onStart / onStop / onError |
| G3 | 路径注册：方法 + 路径参数 + 查询参数 + 路由分组 | REST 动词 `onGet/onPost/...` + `onAny` + `{param}` 段参数 |
| G4 | 插件加载：中间件管道 + 插件基类 + 内置插件 | `app.use(...)`，插件可注册路由/中间件/钩子 |
| G5 | JSON 封装：统一返回体 + 对象自动序列化 | `ApiResult` + `res.jsonOk/jsonFail` + `req.body<T>()` |
| G6 | 文档：Swagger 风格 `/docs` + OpenAPI 3.0 导出 + Apifox 导入 | Apifox「导入 → OpenAPI/Swagger → URL」零手工 |
| G7 | HTTP 与 WebSocket 同端口（Upgrade 分流）+ WS 独立端口两种形态 | `app.websocket(path, fn)` 与 `SLang.WebSocketServer(port)` |
| G8 | **依赖边界：只引用 Core / Std 两模块** | jsonc `references` 白名单 = `[Core, Std]`；禁用 Math / Render / Tensora / SLMysql / 插件体系 / 任何 native DLL |
| G9 | 请求体格式处理：json / form / multipart / bin 按 Content-Type 自动分流 | `req.bodyFormat` 判定 + 各格式读取 API（§4.4） |
| G10 | 文件上传：multipart 解析 + `UploadedFile` 类 | `req.file("f")` → `saveAs/saveInto`（§4.4） |
| G11 | 流式传输：chunked 流式响应 + SSE + 流式文件下载 | `res.stream() / res.sse() / res.sendFile()`（§4.5）；写挂起天然背压；请求侧流式 P5 |

### 1.2 非目标（v1 明确不做）

- HTTPS / WSS **服务端**（TLS 服务端需证书装载与 SNI，P5 再议；客户端 `Net.TlsStream` 不受影响）；
- HTTP/2、QUIC、反向代理、集群；
- 多进程 / 多 VM 实例负载模型（单 VM 协程调度即全部并发，见 §10）；
- 模板引擎 / ORM / 依赖注入容器（Spring 全家桶不做，只借其**用法形态**）；
- 流式**请求**（边收边落盘上传、Range 断点续传）：v1 请求体全量驻留内存、受上限约束，P5（**响应侧流式 chunked / SSE / 文件流为 P2 落地**，见 §4.5）；
- WASM 目标（socket 能力在 wasm 侧降级，本模块不参与 wasm 构建）。

---

## §2 现状盘点：复用与缺口

### 2.1 直接复用（不改动，全部位于 Core / Std——满足 G8）

| 现有能力 | 位置 | 在本框架中的角色 |
|----------|------|------------------|
| TCP 监听/接入（协程挂起） | `Lib/Std/Net/TcpSocket.sl`（`Tcp.listen / TcpServer.accept`） | 连接层：`HttpServerCore` 的 accept 循环 |
| WebSocket 帧层 + 服务端握手 | `Lib/Std/Net/Websocket.sl`（`WebSocketStream.accept`、`receive`、`sendText`） | WS 会话层：`WsSession` 直接包装 |
| HTTP 头集合 / 解析先例 | `Lib/Std/Net/HttpClient.sl`（`HttpHeaders`、`HttpMethod`、三态 body 读取） | 请求模型：`HttpHeaders` 直接复用；解析算法照搬改向 |
| JSON 树 + 对象序列化 | `Lib/Core/Text/BaseJson.sl`（`parse / fromData / toJson / toJsonPretty`）+ `Text.Json` | §6.7 JSON 封装 + §6.10 OpenAPI 生成 |
| 对象序列化规则 attribute | `Lib/Core/Attribute.sl`（`Serializable / SerializeField / NonSerialized`） | DTO 的成员可见性规则 |
| 协程调度 | `Lib/Core/Coroutine.sl`（`spawnClosure0..3`、`Task`） | 每连接一协程、每 WS 会话一协程 |
| 文件读写 / 静态资源 | `Lib/Std/IO/File.sl`、`Directory.sl`、`Path.sl` | StaticFiles 插件 + `UploadedFile.saveAs` |
| 路由属性占位 | `Lib/Std/Net/Route.sl`（`@Route("/x")`，Preload、未接线、**未入 Std.jsonc 编译清单**） | P4 接线对象（见 §7） |

### 2.2 缺口（本设计补齐）

1. **HTTP/1.1 服务端**：请求行/头/body 解析、响应编码、keep-alive、状态码——`Net.md` 明言"不含 HTTP 服务端"；
2. **路由器**：方法×路径匹配、`{param}` 提取、405/404 判定、冲突检测；
3. **Request / Response 模型与返回封装**：读 API / 写 API / `ApiResult`；
4. **请求体格式处理**：Content-Type 分流、form 解码（percent-decode + `+`→空格）、multipart 解析；
5. **文件上传**：`UploadedFile` 类 + multipart 落盘；
6. **流式响应**：chunked 编码（`StreamWriter`）、SSE（`SseWriter`）、流式文件下载（`sendFile`）；
7. **中间件管道与插件框架**；
8. **OpenAPI 生成 + docs 页面**；
9. **`WebSocketStream.accept` 的预读缓冲变体**：HTTP 层已预读后才识别 Upgrade，握手需消费残留缓冲（§6.1，Std 侧唯一一处增量改动）。

### 2.3 语言层约束（已核实，设计必须绕行）

| 约束 | 来源 | 对策 |
|------|------|------|
| **`get` / `set` 是关键字**，不能作方法名 | `HttpClient.sl` 头注记（getHeader/setHeader 改名先例） | 路由注册用 **`onGet/onPost/onPut/onDelete/onPatch/onHead/onOptions/onAny`** 家族；原始形式 `app.on( "GET", path, fn )` |
| **`next` 是关键字**（ETokenType.Next） | `Parse/LexerParseToToken.cs` 2376 | 中间件第三参为 **`SLang.Next` 类型**（参数名 `nx`），放行调 **`nx.go()`** |
| **`data` 是关键字**（ETokenType.Data） | 同上 | `ApiResult` 字段名用 **`payload`**；输出 JSON 键名仍为 `"data"`（框架手工组包，§6.6） |
| `as` / `is` 是关键字 | 词法器 | 不用作任何方法名 |
| **SL 无 `virtual` 关键字**：成员方法默认可被子类重写，仅有 `override`（重写标记）/ `final`（封禁重写） | 语言作者确认；`Attribute.sl`、`Websocket.sl` 等主力库全为直接 `public void` + 子类 `override` 先例 | 基类钩子直接 `public void` 空实现；子类 `override public` 重写；不希望再被重写加 `final` |
| `Function` 闭包**无类型签名**，调用靠约定 arity | `WebSocketClient.onMessage` 先例 | 处理器/中间件签名在文档固化为**调用约定**（§4.2/§4.7），框架按约定转发 |
| **Function 变量多参直调无库内先例**（仅单参 `hMsg(msg)`） | 全 Lib/test 检索 | `(req, res)` 双参与 `(req, res, nx)` 三参依赖 `CallClosure(106)` 多参能力（IROpEnum 注记 `arg0, arg1, ...` + paramCount，机制存在）；**P0 前置探针验证**（§8） |
| 闭包**不能声明 throws** | `Serialize.sl` 文件头注记 | 框架侧全部 catch，处理器永不抛出（§6.11） |
| **跨模块 extends 模板类不支持** | `Serialize.sl` 注记 | 本模块不 extends `Stream<T>` 等模板类；`NetStream`（非模板）可 extends |
| Preload 阶段 SL `_enter_` 不执行（v1） | `ATTRIBUTE_DESIGN.md` D2 | P0–P3 不用 attribute；P4 走 C VM 原生 handler（§7） |
| 运行时**无方法枚举反射**（`Attribute.getMethodAttributes` 需先知方法名，且只返回 Runtime stage） | `Attribute.sl` + `Type.sl` 核查 | P4 用 C VM 装配期收集（§7.1），不做运行时扫描 |
| Front attribute 名称回退表仅含 `DllImport/Condition/Route→1` 等 | `ATTRIBUTE_DESIGN.md` §1 现状表 | P4 新 attribute 名需 Front 注册（§7.3 前置调研），优先复用已知名 `Route` |
| `this.字段` 直接作 if 条件有求值异常 | `Websocket.sl` 注记 | 实现遵循"先拷局部再判断"既有规避 |
| `Uri` 无 percent-decode 工具 | `Uri.sl` API 核查 | 框架内置 `_UrlCodec`（percent-decode + `+`→空格，§6.4） |

---

## §3 总体架构

```
                        ┌────────────────────────────────────────────────┐
                        │  SLang.Server（应用门面 = App）                  │
                        │  路由表 / 插件列表 / ServerConfig / 生命周期钩子   │
                        └───────┬───────────────────────────┬────────────┘
                                │ 注册                       │ 注册
              ┌─────────────────▼──────────┐   ┌────────────▼─────────────┐
              │ Router（HTTP 路由）          │   │ WsRouter（WS 升级路由）    │
              │ Method+Pattern → RouteInfo  │   │ Pattern → WsRouteInfo     │
              └─────────────────▲──────────┘   └────────────▲─────────────┘
                                │ 匹配                        │ Upgrade 分流
┌───────────────────────────────┴────────────────────────────┴──────────────┐
│ HttpServerCore（连接引擎，每连接一协程）                                       │
│  accept 循环 → 请求解析（请求行/头/body：Content-Length|chunked|EOF）          │
│  → 中间件管道（req, res, nx） → handler(req, res) → 响应编码（keep-alive/chunked）│
│  → Upgrade: websocket ? WebSocketStream.acceptBuffered(残留缓冲) ──► WsSession│
└───────────────────────────────┬────────────────────────────────────────────┘
                                │ 复用（不改动）—— 仅 Core / Std
              ┌─────────────────▼──────────────────────────────┐
              │ Net.TcpServer / TcpStream（C VM 协程挂起 IO）    │
              │ Net.WebSocketStream（帧层/握手）  BaseJson（序列化）│
              └────────────────────────────────────────────────┘
        BodyParser（Content-Type 分流：Json/Form/Multipart/Text/Binary）
            ├─ form 解码（_UrlCodec：percent-decode + '+'→空格）
            └─ multipart 解析（boundary/part 头/文件名安全剥离）→ UploadedFile

独立形态：SLang.WebSocketServer( port ) —— 不经 HTTP，裸 TCP + WS 握手直入 WsSession
```

**并发模型**：C VM 协程单线程协作调度 → **框架全部共享状态（路由表/会话表）无锁**，这是设计不变量（§10）。

**分层原则**：`Server`（门面/组装）→ `Router`/`WsRouter`（匹配）→ `HttpServerCore`（协议 IO）→ `BodyParser`（格式）→ `Net.*`（传输）。插件只允许触碰门面层 API。

---

## §4 语法设计（核心章节：使用简单）

> 本章是 API 契约的**权威定义**；所有示例语法均已对照现有库先例（`var` / 闭包字面量 / 方法重载 / 链式返回 this）与词法器关键字表校验。
> 用户侧可用 typealias 缩短书写（`md/syntax/typealias.md`）：
> `typealias Req = SLang.Request;` · `typealias Res = SLang.Response;`

### §4.1 最小可用（HTTP）

```sl
import SLangServer;

var app = SLang.Server();
app.onGet( "/hello", function( SLang.Request req, SLang.Response res )
{
    res.text( "hello world" );
} );
app.run( 8080 );          # 阻塞当前协程直至 stop()；启动横幅打印 http://0.0.0.0:8080
```

- `run(port)` = `start(port)` + `awaitStop()`；`start` 立即返回（accept 循环进新协程），`stop()` 优雅收尾。
- 重复注册同 method+pattern：`start()` 时抛 `ServerError.DuplicateRoute`。
- 未匹配路径 → 404 + `ApiResult` JSON 体；路径命中但方法不命中 → 405。

### §4.2 路径注册（目标②）

```sl
# ── 动词注册（get/set 是关键字 → 统一 on 前缀）──
# onGet / onPost / onPut / onDelete / onPatch / onHead / onOptions / onAny
app.onPost( "/api/users", function( SLang.Request req, SLang.Response res )
{
    # body JSON → DTO（Serializable 类；解析失败返回 null）
    CreateUserReq body = req.body<CreateUserReq>();
    if body == null
    {
        res.jsonFail( 400, "bad json body" );
        ret
    }
    var u = User( 1, body.name );
    res.status( 201 ).jsonOk( u );
} );

# ── 路径参数 {name}（单段匹配）──
app.onGet( "/api/user/{id}", function( SLang.Request req, SLang.Response res )
{
    Int32 id = req.pathInt( "id", 0 );        # 带默认值；坏格式回落默认
    ...
} );

# ── 查询参数 ?page=1&size=20 ──
app.onGet( "/api/users", function( SLang.Request req, SLang.Response res )
{
    Int32 page = req.queryInt( "page", 1 );
    Int32 size = req.queryInt( "size", 20 );
    ...
} );

# ── 通配后缀（P2）：/static/{path*} 匹配剩余全部段 ──

# ── 路由分组（前缀提取，Spring @RequestMapping 形态）──
var api = app.group( "/api/v1" );
api.onGet( "/users",  listUsers );
api.onPost( "/users", createUser );

# ── 原始形式（动态注册 / 方法变量）──
app.on( "GET", "/ping", pingHandler );
```

**处理器签名（调用约定）**：`function( SLang.Request req, SLang.Response res )`——**req 只读请求、res 只写响应**；无返回值语义（响应一律经 `res.*` 写出）。

### §4.3 JSON 返回封装（用户点名功能一）

统一返回体 `ApiResult`（注意：类字段名 `payload`，**输出 JSON 键名仍为 `"data"`**，见 §6.6）：

```json
{ "code": 0, "message": "ok", "data": { "id": 1, "name": "tom" } }
```

```sl
public class ApiResult
{
    public Int32 code = 0                # 0 = 成功；非 0 业务错误码（用户自定义段 ≥ 1000）
    public string message = "ok"
    public object payload = null         # 任意 Serializable 对象 / null（data 是关键字，字段名用 payload）
}
```

`res` 写出 API：

| API | 语义 |
|-----|------|
| `res.json( obj )` | 任意对象 → `BaseJson.fromData` → `application/json; charset=utf-8`，200 |
| `res.jsonOk( payload )` | = `res.json( ApiResult.ok( payload ) )`（统一封装） |
| `res.jsonFail( code, msg )` | = `res.json( ApiResult.fail( code, msg ) )`，HTTP 仍 200（业务码与 HTTP 码分离，Apifox 侧按 code 断言） |
| `res.jsonFail( code, msg, httpStatus )` | 三参重载，同时改 HTTP 状态码 |
| `res.rawJson( text )` | 预构建 JSON 串直写（OpenApiBuilder 等内部使用） |
| `res.text( s )` / `res.html( s )` | `text/plain` / `text/html`，200 |
| `res.bytes( buf )` | 二进制（`ByteBuffer`），默认 `application/octet-stream` |
| `res.file( path )` / `res.file( path, downloadName )` | 文件下载：按扩展名置 MIME + `Content-Disposition`（P2） |
| `res.redirect( url )` | 302 + `Location` |
| `res.status( code )` | 链式置状态码，返回 res |
| `res.header( k, v )` | 链式追加响应头，返回 res |
| `res.contentType( mime )` | 链式覆盖 Content-Type，返回 res |

链式组合示例：`res.status( 201 ).header( "X-Trace-Id", tid ).jsonOk( u )`。
终端方法（json 族 / text / html / bytes / file / redirect）**第二次调用抛 `ServerError.AlreadyResponded`**（开发期防重复写出）。

### §4.4 请求体格式与文件上传（v2 新增，目标 G9/G10）

**按 Content-Type 自动分流**（`req.bodyFormat` 只读属性，`EBodyFormat` 枚举）：

| Content-Type | `req.bodyFormat` | 读取 API |
|--------------|------------------|----------|
| `application/json`（含 `+json` 后缀） | `EBodyFormat.Json` | `req.bodyJson()` → BaseJson · `req.body<T>()` → DTO |
| `application/x-www-form-urlencoded` | `EBodyFormat.Form` | `req.form( name )` / `req.formInt( name, def )` / `req.formBool( name, def )` · `req.hasForm( name )` · `req.formKeys()` |
| `multipart/form-data` | `EBodyFormat.Multipart` | 普通字段同 form API；文件走 `req.file / req.files` |
| `text/*` | `EBodyFormat.Text` | `req.bodyText()` |
| 其余（`application/octet-stream` 等） | `EBodyFormat.Binary` | `req.bodyBytes()` → ByteBuffer |
| 无 body（GET/HEAD/长度 0） | `EBodyFormat.None` | 各 API 返回空值 |

```sl
# ── form 表单（application/x-www-form-urlencoded）──
app.onPost( "/api/login", function( SLang.Request req, SLang.Response res )
{
    string user = req.form( "username" );          # 已 percent-decode + '+'→空格
    string pass = req.form( "password" );
    if SystemStringLength( user ) == 0
    {
        res.jsonFail( 400, "username required" );
        ret
    }
    ...
} );

# ── JSON body ──
app.onPost( "/api/users", function( SLang.Request req, SLang.Response res )
{
    CreateUserReq body = req.body<CreateUserReq>();
    ...
} );

# ── bin（原始二进制，如 protobuf / 图片直传）──
app.onPost( "/api/raw", function( SLang.Request req, SLang.Response res )
{
    ByteBuffer b = req.bodyBytes();
    ...
    b.release();
} );

# ── 文件上传（multipart/form-data）──
app.onPost( "/api/upload/avatar", function( SLang.Request req, SLang.Response res )
{
    SLang.UploadedFile f = req.file( "avatar" );       # 单文件（字段名；null = 无）
    if f == null
    {
        res.jsonFail( 400, "no file" );
        ret
    }
    if f.size > 5242880
    {
        res.jsonFail( 400, "file too large" );
        ret
    }
    f.saveAs( "./uploads/avatar_" + f.filename );      # 落盘
    res.jsonOk( f.filename );
} );

# ── 多文件同字段（如批量照片）──
app.onPost( "/api/upload/photos", function( SLang.Request req, SLang.Response res )
{
    List<SLang.UploadedFile> list = req.files( "photos" );
    for Int32 i = 0, i < list.length, i = i + 1
    {
        list[i].saveInto( "./uploads" );               # 目录 + 原名（重名自动改名 file(1).txt）
    }
    res.jsonOk( req.fileCount );
} );
```

**`SLang.UploadedFile`（文件上传类，目标 G10）**：

| 成员 | 语义 |
|------|------|
| `f.field` → string | 表单字段名 |
| `f.filename` → string | 原始文件名（**已剥离路径**，防 `../` 穿越，只留 basename） |
| `f.contentType` → string | part 声明的 Content-Type |
| `f.size` → Int64 | 字节数 |
| `f.ext()` → string | 扩展名（小写、无点；无扩展空串） |
| `f.bytes()` → ByteBuffer | 内容视图（归属文件对象，请求结束统一回收；勿跨请求持有） |
| `f.saveAs( path )` → bool | 落盘到指定完整路径（`Std.IO.File`） |
| `f.saveInto( dir )` → bool | 落盘到目录 + 原文件名（重名自动改名 `name(1).ext`） |

配套配置（§4.9）：`maxUploadBytes`（multipart 请求总量上限，默认 100MB）、`maxFileBytes`（单文件上限，默认 50MB）；超限 413 断连。上传目录不存在时由用户保证（`saveAs` 失败返回 false，不自动建目录——显式优于隐式）。

### §4.5 流式传输（chunked / SSE / 文件流，目标 G11）

**1. chunked 流式响应**（`Transfer-Encoding: chunked`，边生成边下发）：

```sl
app.onGet( "/api/export", function( SLang.Request req, SLang.Response res )
{
    res.status( 200 ).contentType( "text/plain" );    # 头部设置须在 stream() 之前完成
    SLang.StreamWriter w = res.stream();              # 即刻发出状态行+头，进入流式态
    for Int32 i = 0, i < 10, i = i + 1
    {
        w.write( "part " + i.toString() + "\n" );     # 每次 write 即编码一个 chunk 下发
        Coroutine.delay( 100 );
    }
    w.close();                                        # 0 长终止块，响应完成，连接可继续复用
} );
```

**2. SSE**（`text/event-stream`，浏览器 `EventSource` / Apifox 可直连——AI 流式输出标配）：

```sl
app.onGet( "/api/sse/progress", function( SLang.Request req, SLang.Response res )
{
    SLang.SseWriter sse = res.sse();                  # 自动置 Content-Type + Cache-Control: no-cache
    for Int32 i = 0, i <= 100, i = i + 10
    {
        sse.sendEvent( "progress", i.toString() );    # → "event: progress\ndata: 10\n\n"
        Coroutine.delay( 500 );
        sse.comment( "keepalive" );                   # 心跳注释行，防中间层掐空闲连接
    }
    sse.send( "done" );                               # 默认事件 → "data: done\n\n"
    sse.close();
} );
```

**3. 流式文件下载**（大文件 64KB 分块读发，不整载内存）：

```sl
app.onGet( "/download/{name}", function( SLang.Request req, SLang.Response res )
{
    res.sendFile( "./files/" + req.pathParam( "name" ) );   # Content-Length=文件大小 → 非 chunked
} );
```

要点：

- `res.stream()` / `res.sse()` 是**流式终端**：调用即发出响应头并锁定响应（此后 `res.json` 等终端方法抛 `ServerError.AlreadyResponded`）；`sendFile` 是普通终端（内部流式实现，对用户透明）；
- `SLang.StreamWriter`：`write( string )` / `writeBytes( ByteBuffer )` / `flush()`（空操作——写路径即时下发、无用户态缓冲，`NetStream.flush` 同款语义，保留 API 习惯）/ `close()`；handler 未调 `close()` 即退出时框架**兜底补终止块**（防连接悬挂）；
- `SLang.SseWriter`：`send( data )` / `sendEvent( event, data )` / `comment( text )`（心跳注释行）/ `close()`；
- **背压**：底层 `TcpStream.write` 写不出去时协程挂起（C VM 挂起语义）→ 慢客户端天然限速、发送缓冲不膨胀；
- 超时与保活：`cfg.writeTimeoutMs`（§4.9，默认 30s）防慢客户端拖死协程；流式期间每次写出刷新 keep-alive 空闲计时；
- OpenAPI 文档：SSE 接口的响应 content 记 `text/event-stream`（§6.10）；
- **请求侧流式（P5 展望）**：注册期 `r.streamBody = true` → 解析器不预读 body → `req.bodyStream()` 返回 `HttpBodyReader`（`read( ByteBuffer )` 循环、上限累计）→ 边收边落盘上传，超大文件不驻留内存（详见 §6.6）。

### §4.6 WebSocket（两种形态）

**形态 A：HTTP 同端口升级路由**（推荐，FastAPI `@app.websocket` 对应物）

```sl
# 每连接一协程；receive() 返回 null = 连接关闭（WebSocketClient 事件循环同款语义）
app.websocket( "/ws/chat", function( SLang.WsSession s )
{
    while true
    {
        Net.WsMessage m = s.receive();
        if m == null
        {
            break
        }
        s.sendText( "echo: " + m.text() );
        m.release();
    }
} );
```

**事件式糖（P2）**——内部仍是一协程，适合不想写循环的场景：

```sl
app.wsEvents( "/ws/chat" )
    .onOpen(    function( SLang.WsSession s ) { ... } )
    .onMessage( function( SLang.WsSession s, Net.WsMessage m ) { ...; m.release() } )
    .onClose(   function( SLang.WsSession s, Net.WsCloseInfo info ) { } );
```

**形态 B：独立端口 WS 服务**（无 HTTP 层，裸 TCP 直握手）

```sl
var wss = SLang.WebSocketServer( 9000 );
wss.onMessage = function( SLang.WsSession s, Net.WsMessage m )
{
    s.sendText( "echo: " + m.text() );
    m.release();
};
wss.run();        # 或 start() / stop()
```

**会话与广播**：

```sl
s.id                    # string，GUID（会话注册表键）
s.remoteAddress         # "ip:port"
s.pathParam( "room" )   # 升级 URL 的路径参数（与 HTTP 路由同一套 {param} 语法）
s.query( "token" )      # 升级 URL 查询参数
s.sendText / s.sendBinary / s.sendPing / s.close / s.closeInfo / s.isOpen

app.sessions( "/ws/chat" )            # → List<WsSession>（该路由在线会话）
app.broadcast( "/ws/chat", "hi" )     # 组播文本；binary(ByteBuffer) 重载
s.tag( "role", "admin" ) / s.tagged( "role", "admin" )   # P2：会话标签 → 定向广播
```

### §4.7 中间件与插件（目标③）

**中间件**（Koa 洋葱模型，全局注册；三参签名，`next` 是关键字 → `SLang.Next nx` + `nx.go()` 放行）：

```sl
# 签名约定：function( SLang.Request req, SLang.Response res, SLang.Next nx )
# 调 nx.go() 放行进下一环；不调 = 短路（CORS 预检 / 鉴权拒绝等）
app.use( function( SLang.Request req, SLang.Response res, SLang.Next nx )
{
    Int64 t0 = Timer.millis();                      # Std/OS/Timer，依赖边界内
    string token = req.header( "Authorization" );
    if SystemStringLength( token ) == 0
    {
        res.jsonFail( 401, "unauthorized" );        # 短路：管道终止，不再进 handler
        ret
    }
    req.userData = "uid:1";                         # 中间件 → 处理器传值（object 槽）
    nx.go();                                        # 放行
    Console.println( "cost " + ( Timer.millis() - t0 ) + "ms" );   # 后置逻辑
} );
```

**插件**（`ServerPlugin` 基类，钩子方法默认空实现——SL 无 `virtual` 关键字，方法默认可被子类 `override` 重写，避免 interface 可选方法问题）：

```sl
public class ServerPlugin
{
    public void onInstall( SLang.Server app ) { }   # 注册中间件/路由/钩子的唯一入口（子类 override 重写）
    public void onStart() { }                        # start 后、accept 前
    public void onStop() { }                         # stop 收尾
    public void onWsOpen( SLang.WsSession s ) { }    # WS 会话级钩子（可做在线统计）
    public void onWsClose( SLang.WsSession s ) { }   # 不希望子类再重写时加 final
}

# 使用：一行装一个插件
app.use( SLang.Plugins.logger() );                 # 请求日志（方法/路径/状态/耗时）
app.use( SLang.Plugins.cors() );                   # CORS（自动应答 OPTIONS 预检）
app.use( SLang.Plugins.staticFiles( "./www" ) );   # 静态目录（内置 MIME 表，见附录 B）

# 自定义插件
public class AuthPlugin extends SLang.ServerPlugin
{
    override public void onInstall( SLang.Server app )
    {
        app.use( function( SLang.Request req, SLang.Response res, SLang.Next nx ) { ... } );
    }
}
app.use( AuthPlugin() );
```

内置插件路线：`logger / cors / staticFiles`（P2）→ `bodyLimit / requestId`（P3）→ `rateLimit / auth`（P5）。用户自定义插件即普通类，**无注册中心、无配置文件**，`app.use` 是唯一挂载点。

### §4.8 API 文档：/docs + OpenAPI + Apifox（用户点名功能二）

**使用**：`app.run()` 时自动挂载 `GET /docs` 与 `GET /openapi.json`（`app.docs( false )` 关闭，生产建议关）。

**文档元数据在注册时顺手填**（返回 `RouteInfo`，字段公开 + 链式便捷方法二选一）：

```sl
var r = app.onGet( "/api/user/{id}", getUserHandler );
r.summary = "获取单个用户";
r.tags = "用户";                          # 逗号分隔多标签
r.desc = "按 id 查询用户详情";

# 参数与 schema：实例探针法（传样例实例 → BaseJson.fromData → JSON 树 → 推导 schema）
# 不需要任何运行时反射（语言层无字段枚举，这是唯一零依赖方案）
r.param( "id", "integer", "用户ID", true );            # path/query 参数
r.requestBody( CreateUserReq() );                      # JSON body schema + example
r.formField( "username", "string", "用户名" );          # form 字段 → urlencoded requestBody schema
r.uploadField( "avatar", "头像文件", "image/*" );       # multipart 文件字段
r.response( 200, User() );                              # 响应 schema + example
r.response( 404, SLang.ApiResult.fail( 1001, "no such user" ) );

# 链式等价形式：
app.onGet( "/api/users", listHandler ).summary( "用户列表" ).tag( "用户" ).response( 200, User() );
```

**`/docs` 页面**：自包含单文件 HTML（内嵌字符串常量，零外链、零 CDN——`wasm/playground.html` 同款离线策略），渲染接口分组/参数/示例，GET 接口支持页内 try-it-out（同源 fetch）。

**`/openapi.json`**：OpenAPI 3.0.3 规范（`info / servers / paths / parameters / requestBody / responses / components.schemas`）。**Apifox 导入路径**：项目设置 → 数据导入 → OpenAPI/Swagger → 填 `http://<host>:<port>/openapi.json`（或下载文件导入）→ 接口/分组/示例全量进入 Apifox。Postman / SwaggerHub / Insomnia 同理。

**反向导入（从 Apifox 导出 → 生成路由骨架）**：

```sl
# Apifox 导出 OpenAPI json 后：
var imported = app.importOpenApi( "api-spec.json" );   # → Int32（导入的路径数）
# 语义：未注册的路径以"文档路由"登记（/docs 可见、命中返回 501 ApiResult）
# 后续可用 app.onGet(...) 覆盖同名文档路由 → 设计先行/接口契约对齐工作流
```

### §4.9 生命周期与配置（目标①）

```sl
var app = SLang.Server();

# ── 配置（字段直改，无 builder 链）──
var cfg = app.config;
cfg.host = "0.0.0.0";
cfg.port = 8080;                 # run(port) 为便捷覆盖
cfg.keepAliveTimeoutMs = 15000;  # 空闲连接超时（超时静默断开）
cfg.writeTimeoutMs = 30000;        # 流式写挂起超时（防慢客户端拖死；<= 0 无限等待）
cfg.maxHeaderBytes = 16384;      # 请求头上限，超出 431
cfg.maxBodyBytes = 10485760;     # 普通请求 body 上限 10MB，超出 413
cfg.maxUploadBytes = 104857600;  # multipart 请求总量上限 100MB，超出 413
cfg.maxFileBytes = 52428800;     # 单文件上限 50MB，超出 413
cfg.maxConnections = 1024;       # 并发连接上限，超出 503（P3 生效）
cfg.showBanner = true;           # 启动横幅（含 /docs 地址提示）

# ── 事件钩子（Function 字段，WebSocketClient 先例）──
app.onStart  = function() { Console.println( "up" ); };
app.onStop   = function() { Console.println( "down" ); };
app.onError  = function( SLang.Request req, SLang.Response res, Error e ) { ... };   # 处理器异常兜底

# ── 启停 ──
app.start( 8080 );    # 立即返回；accept 循环独立协程
app.awaitStop();      # 挂起直至 stop()（run = start + awaitStop）
app.stop();           # 关监听 → 逐个关会话/连接 → onStop 钩子
app.isRunning         # get bool
```

### §4.10 attribute 控制器（P4 展望语法，暂不实现）

```sl
public class UserController
{
    @Route( "/api/users/{id}" )                 # 方法注解；verb 用第二参（默认 GET）
    public void getUser( SLang.Request req, SLang.Response res ) { ... }

    @Route( "/api/users", "POST" )
    public void createUser( SLang.Request req, SLang.Response res ) { ... }
}

app.use( UserController );      # 装配期（C VM 原生收集，见 §7）批量注册
```

> P0–P3 期间等价能力 = §4.2 的闭包注册；attribute 只是糖，**不提供任何闭包注册做不到的能力**。

---

## §5 模块工程与类型清单

### 5.1 文件布局（`source/Front/Lib/SLang/Server/`）

| 文件 | 内容 |
|------|------|
| `Server.sl` | 命名空间 `SLang`：`Server`（门面/App）、`ServerConfig`、`ServerError`、内部组装逻辑 |
| `HttpServerCore.sl` | `_HttpParser`（内部）、连接循环、响应编码、keep-alive、Upgrade 分流 |
| `Router.sl` | `Router`、`RouteInfo`、`RouteGroup`、路径段匹配 |
| `Request.sl` | `Request`（读侧 API 全量：路径/查询/头/body 分流/form/文件） |
| `Response.sl` | `Response`（写侧 API 全量）、`Next`（中间件放行令牌） |
| `BodyParser.sl` | `EBodyFormat` 判定、form 解码（`_UrlCodec`：percent-decode + `+`→空格）、multipart 解析器（boundary/part 头/文件名安全剥离） |
| `Upload.sl` | `UploadedFile`（上传文件句柄：元数据/bytes/saveAs/saveInto） |
| `StreamIo.sl` | `StreamWriter`（chunked 流式写）、`SseWriter`（text/event-stream 写）；P5 增 `HttpBodyReader`（请求侧流式读） |
| `WebSocketServer.sl` | `WebSocketServer`（独立端口形态）、`WsSession`、`WsSessionRegistry` |
| `ApiResult.sl` | `ApiResult` 统一返回体（字段 payload → JSON 键 "data"） |
| `Plugin.sl` | `ServerPlugin` 基类、中间件管道执行器 |
| `Plugins.sl` | 内置插件门面 `SLang.Plugins`（logger/cors/staticFiles 工厂） |
| `OpenApi.sl` | `OpenApiBuilder`（实例探针 → schema）、`/docs` 内嵌 HTML 常量、`importOpenApi` 解析 |
| `Server.jsonc` / `Server.sp` | 工程配置与入口（见 5.2） |
| `SERVER_DESIGN.md` | 本文档 |
| `RouteAttr.sl`（P4） | `@Route` attribute 子类（届时收编 `Lib/Std/Net/Route.sl`，见 §7.4） |

### 5.2 工程配置要点（`Server.jsonc`）

```jsonc
{
  "project": { "name": "SLangServer", "desc": "HTTP + WebSocket application server framework (depends on Core/Std only)" },
  "compileFiles": { "files": [
      { "path": "Server.sl",          "group": "server", "tag": "core", "ignore": false, "priority": 0 },
      { "path": "HttpServerCore.sl",  "group": "server", "tag": "net",  "ignore": false, "priority": 0 },
      { "path": "Router.sl",          "group": "server", "tag": "core", "ignore": false, "priority": 0 },
      { "path": "Request.sl",         "group": "server", "tag": "core", "ignore": false, "priority": 0 },
      { "path": "Response.sl",        "group": "server", "tag": "core", "ignore": false, "priority": 0 },
      { "path": "BodyParser.sl",      "group": "server", "tag": "core", "ignore": false, "priority": 0 },
      { "path": "Upload.sl",          "group": "server", "tag": "io",   "ignore": false, "priority": 0 },
      { "path": "StreamIo.sl",        "group": "server", "tag": "io",   "ignore": false, "priority": 0 },
      { "path": "WebSocketServer.sl", "group": "server", "tag": "net",  "ignore": false, "priority": 0 },
      { "path": "ApiResult.sl",       "group": "server", "tag": "core", "ignore": false, "priority": 0 },
      { "path": "Plugin.sl",          "group": "server", "tag": "core", "ignore": false, "priority": 0 },
      { "path": "Plugins.sl",         "group": "server", "tag": "core", "ignore": false, "priority": 0 },
      { "path": "OpenApi.sl",         "group": "server", "tag": "doc",  "ignore": false, "priority": 0 }
  ]},
  "systemCalls": [],                     # P0–P3 零新增系统方法（纯 SL 层）
  "references": [
      # ★ 依赖边界（G8）：白名单仅此两项——BaseJson/Coroutine/Attribute/容器（Core）
      #   与 Net.*/File/Timer（Std）；禁引 Math/Render/Tensora/SLMysql/插件/native DLL
      { "path": "../Core", "name": "Core" },
      { "path": "../Std",  "name": "Std" }
  ],
  "struct": [ { "namespace": "SLang", "children": [
      { "class": "Server" }, { "class": "ServerConfig" },
      { "class": "Request" }, { "class": "Response" }, { "class": "Next" },
      { "class": "RouteInfo" }, { "class": "RouteGroup" }, { "class": "ApiResult" },
      { "class": "WsSession" }, { "class": "WebSocketServer" }, { "class": "ServerPlugin" },
      { "class": "Plugins" }, { "class": "UploadedFile" }, { "class": "StreamWriter" }, { "class": "SseWriter" }, { "class": "OpenApiBuilder" }
  ]}],
  "export": { "moduleName": "SLangServer", "outputDir": "..\\..\\..\\..\\out\\export", "...": "..." }
}
```

用法：用户工程 `import SLangServer;` → `SLang.Server` 全路径（与 `SLang.Mysql` 同款命名空间惯例）。

### 5.3 公开类型总表

| 类型 | 职责 | 关键成员 |
|------|------|----------|
| `SLang.Server` | 应用门面（App 本体） | `onGet/onPost/onPut/onDelete/onPatch/onHead/onOptions/onAny/on` · `group/use/websocket/wsEvents` · `run/start/stop/awaitStop/isRunning` · `config/onStart/onStop/onError` · `docs/importOpenApi/sessions/broadcast` |
| `SLang.ServerConfig` | 配置 | §4.8 字段表 |
| `SLang.ServerError extends Error` | 框架错误码 | 附录 D |
| `SLang.Request` | **读侧**：请求上下文 | §6.3 全表（路径/查询/头/body 四格式/form/文件） |
| `SLang.Response` | **写侧**：响应输出 | §4.3 表 + `isResponded` |
| `SLang.Next` | 中间件放行令牌 | `go()`（`next` 是关键字，不能直接命名） |
| `SLang.RouteInfo` | 路由记录 + 文档元数据 | `path/method/handler/summary/tags/desc` + `param/requestBody/formField/uploadField/response` |
| `SLang.RouteGroup` | 前缀分组 | `onGet/onPost/.../websocket`（自动拼前缀） |
| `SLang.ApiResult` | 统一 JSON 返回体 | `ok(payload)/fail(code,msg)` + `code/message/payload`（JSON 键 "data"） |
| `SLang.UploadedFile` | 上传文件句柄 | §4.4 表 |
| `SLang.StreamWriter` | chunked 流式写出器 | `write/writeBytes/flush/close`（§4.5） |
| `SLang.SseWriter` | SSE 写出器 | `send/sendEvent/comment/close`（§4.5） |
| `SLang.WsSession` | WebSocket 会话 | §4.6 |
| `SLang.WebSocketServer` | 独立端口 WS | `onOpen/onMessage/onClose/onError` + `run/start/stop/sessions/broadcast` |
| `SLang.ServerPlugin` | 插件基类 | `onInstall/onStart/onStop/onWsOpen/onWsClose` |
| `SLang.Plugins` | 内置插件工厂 | `logger()/cors()/staticFiles(root)` |
| `SLang.OpenApiBuilder` | 文档生成（内部亦可直用） | `build() → string`（openapi.json 全文） |
| `EBodyFormat`（enum extends Int32） | 请求体格式 | `None/Json/Form/Multipart/Text/Binary` |

---

## §6 核心机制设计

### §6.1 HttpServerCore（连接引擎）

**连接循环**（每连接一协程，`TcpServer.onConnection` 模式）：

```
Tcp.listen( host, port, backlog )
loop: client = accept()
    spawn 协程 handleConnection( client ):
        client.setReadTimeout( keepAliveTimeoutMs )
        loop:                                  # keep-alive 循环
            req = _HttpParser.parse( client )  # 挂起语义：无数据时协程挂起
            ├ 超时/EOF → 关连接，break
            ├ 头超限 → 431 + close
            ├ Upgrade: websocket 且命中 ws 路由 → WebSocketStream.acceptBuffered → 会话协程，break
            ├ Upgrade: websocket 未命中 → 400
            ├ 解析失败 → 400 + close
            └ 正常 → 构造 Request+Response → 管道执行 → 响应编码写出
                    Connection: close 或 HTTP/1.0 → close，break；否则继续
```

**请求解析**（算法与 `HttpClient` 响应解析对称，`Websocket.sl _readLine/_fill` 同款预读缓冲技术）：

- 请求行 `METHOD SP path SP HTTP/1.1`；路径拆 `path?query`；
- 头循环至空行，头总字节 ≤ `maxHeaderBytes`，行数上限 100；
- body 三态（与客户端一致）：`Content-Length` 精确读 / `Transfer-Encoding: chunked` 聚合 / 两者皆无读到 EOF（仅对写侧关闭的请求）；
- body 上限：普通请求 `maxBodyBytes`、multipart 请求 `maxUploadBytes`，超出 413 并断连（防内存攻击）。

**响应编码**：

```
HTTP/1.1 <code> <reason>\r\n
Content-Type: <mime>\r\n
Content-Length: <n>\r\n        # 普通响应恒精确长度；流式响应改 Transfer-Encoding: chunked（§6.6）
Connection: keep-alive|close\r\n
<用户附加头>\r\n
\r\n
<body>
```

**状态码 ↔ reason 表**：内置 2xx/3xx/4xx/5xx 常用 30 项（附录 A），未知码用空 reason。

### §6.2 Router（匹配规则）

- 路由表：`List<RouteInfo>` 线性扫描（v1 规模假设 < 200 条；前缀树为 P5 优化项，非语义变更）；
- 匹配键：`( method, pattern )`；pattern 按 `/` 拆段，`{name}` 段匹配任意非空单段并捕获；
- **优先级**：精确段 > 参数段（先注册静态段更多的 pattern；`/api/user/{id}` 让位 `/api/user/me`）；
- 未注册 method `any` 的路由对该 pattern 接受全部动词；
- 判定序：精确/参数匹配成功但 verb 不符 → **405**（`Allow` 头列出可用动词）；全无匹配 → **404**；
- 冲突检测：`start()` 时对 `(method, pattern)` 去重，重复抛 `ServerError.DuplicateRoute`（fail-fast，防静默覆盖）；
- 路径参数提取进 `req._pathParams`（`Map<string,string>`），查询参数惰性解析进 `req._query`（经 `_UrlCodec` 解码）。

### §6.3 Request（读侧 API 全表）

| API | 语义 |
|-----|------|
| `req.method` → string | "GET" / "POST"…（原始大写） |
| `req.path` → string | 不含 query 的路径 |
| `req.rawQuery` → string | 原始 query 串（可为空） |
| `req.pathParam( name )` → string | 路径段参数；不存在返回 null |
| `req.pathInt( name )` / `req.pathInt( name, def )` | Int32 化；坏格式用 def（单参版 def=0） |
| `req.pathInt64( name, def )` / `req.pathFloat( name, def )` | 同上 Int64 / Float64 |
| `req.query( name )` → string | 查询参数（已解码）；不存在 null |
| `req.queryInt / req.queryBool( name, def )` | 数值/布尔化（`1/true/on` 为真） |
| `req.hasQuery( name )` → bool | — |
| `req.queryKeys()` → List\<string\> | — |
| `req.header( name )` → string | 请求头（大小写不敏感，`HttpHeaders.getHeader` 直通） |
| `req.headers` → Net.HttpHeaders | 原始头集合 |
| `req.contentType` → string | 原始 Content-Type（含参数，如 boundary） |
| `req.bodyFormat` → EBodyFormat | 格式判定（§4.4 分流表） |
| `req.contentLength` → Int64 | 请求声明长度 |
| `req.bodyText()` → string | body 文本（UTF-8）；无 body 空串 |
| `req.bodyJson()` → BaseJson | body 作 JSON 树解析；失败 null |
| `req.body<T>()` → T | `BaseJson.toData<T>` 直通；失败 null（**不抛**，闭包无 throws） |
| `req.bodyBytes()` → ByteBuffer | body 二进制视图（归属 req，请求结束统一回收） |
| `req.form( name )` → string | form 字段值（urlencoded 或 multipart 普通字段；已解码） |
| `req.formInt / req.formBool( name, def )` | 数值/布尔化 |
| `req.hasForm( name )` → bool · `req.formKeys()` → List\<string\> | — |
| `req.file( field )` → UploadedFile | 该字段首个文件；无返回 null |
| `req.files( field )` → List\<UploadedFile\> | 该字段全部文件（多文件） |
| `req.fileCount` → Int32 | 本请求文件总数 |
| `req.isUpgrade` → bool | Upgrade 头探测 |
| `req.ip` / `req.port` | 对端地址（`TcpStream.remoteAddress/remotePort` 直通） |
| `req.userData` → object | 中间件传值槽（自由读写） |

**Response（写侧）**：见 §4.3 表；附加控制：

| API | 语义 |
|-----|------|
| `res.isResponded` → bool | 是否已有终端写出 |
| `res.setCookie( name, value )` | P3 |

### §6.4 请求体格式处理（BodyParser）

**Content-Type → EBodyFormat 判定**（忽略大小写；参数段剥离后比较）：

```
application/json | */+json               → Json
application/x-www-form-urlencoded        → Form
multipart/form-data                      → Multipart
text/*                                   → Text
其余（含 application/octet-stream）        → Binary
无 Content-Type 且无 body                 → None
```

**form 解码（urlencoded）**：

```
"a=1&b=hello+world&c=%E4%BD%A0%E5%A5%BD"
→ 按 '&' 分段 → 首个 '=' 切键值 → _UrlCodec 解码（%HH 逐字节 + '+'→空格 → UTF-8 组装）
→ 键重复：后者覆盖前者（与查询参数一致；同名多值 formAll(name) 为 P3）
```

`_UrlCodec`（内部静态工具，`Uri` 无现成解码）：`decode( string ) → string`（%HH 非法序列原样保留）；查询参数解码同用。

**multipart/form-data 解析（RFC 7578）**：

```
1. Content-Type 参数提取 boundary（忽略大小写、可带引号；缺失 → 400）
2. 分隔符 = "--" + boundary；结束符 = 分隔符 + "--"；段间以 CRLF 相接
3. 逐 part：分隔符行 → part 头（Content-Disposition / Content-Type，头规则同请求头）→ 空行
   → 内容直到下一分隔符（内容不含结尾 CRLF）
4. Content-Disposition: form-data; name="field"[; filename="f.txt"]
   有 filename → 文件 part → UploadedFile（内容进独立 ByteBuffer）
   无 filename → 普通字段 → form 表（值解码同 _UrlCodec 规则中的直传语义，multipart 字段值不解码）
5. 尺寸约束：总量 ≤ maxUploadBytes、单文件 ≤ maxFileBytes（超限 413）；
   part 头行数/大小同请求头限制
6. filename 安全：取 basename（按 '/' 与 '\\' 剥离路径段，防 ../ 穿越）；空名生成 "upload_<序号>"
7. v1 全量驻留内存；流式（边收边落盘）为 P5
```

### §6.5 文件上传（UploadedFile）

- 生命周期：`Request` 构造期由 BodyParser 解析 multipart 产出，`List<UploadedFile>` + 字段名索引（`Map<string, List<UploadedFile>>`）挂在 req 上；请求结束（响应写出/连接关闭）统一 `release()` 内容缓冲；
- `bytes()` 每次返回 `slice()` 视图（不推进源，`Websocket.sl` 同款零拷贝手法）；**勿跨请求持有**——需要持久化就 `saveAs`；
- `saveAs( path )` / `saveInto( dir )`：`Std.IO.File` 写出；`saveInto` 重名自动改名 `name(1).ext`；目录不存在返回 false（不自动创建，显式优于隐式）；
- 扩展名信任：`f.ext()` 仅供业务白名单判断（如仅允许 png/jpg），**不**作为 MIME 判定依据（MIME 用 part 声明的 contentType）。

### §6.6 流式传输机制（响应侧落地 + 请求侧展望）

**chunked 编码**（RFC 9112 §7.1）：

```
HTTP/1.1 200 OK\r\n
Transfer-Encoding: chunked\r\n        # 流式响应不带 Content-Length
<用户头>\r\n
\r\n
5\r\npart1\r\n                         # 每次 write 一个 chunk：<hex 长度>\r\n<数据>\r\n
0\r\n\r\n                              # close() 终止块
```

**StreamWriter 状态机**：`Header（stream() 前可改状态/头）→ Streaming（只接受 write/flush）→ Closed（再写抛 AlreadyResponded）`

- 头部先行：`stream()` 调用点即编码状态行 + 头并写出——`status / contentType / header` 链式必须在之前完成；
- HTTP/1.0 客户端不支持 chunked → 降级 close-delimited（写完直接断连）；
- 兜底收尾：handler 协程退出（含异常路径）未调 `close()` → 框架补 `0\r\n\r\n`，连接按 keep-alive 规则继续或关闭。

**SSE 线格式**（`SseWriter` 内部组装，用户只调语义 API）：

```
event: progress\r\ndata: 10\r\n\r\n     # sendEvent( "progress", "10" )
data: done\r\n\r\n                      # send( "done" )
: keepalive\r\n\r\n                     # comment( "keepalive" ) 心跳注释行
```

**背压与超时**：写路径全走 `TcpStream.write`（C VM 协程挂起语义）→ 发送缓冲满则挂起，天然背压；`cfg.writeTimeoutMs`（默认 30s，<= 0 无限）超时抛 `NetError.Timeout` → 走管道异常协议收尾（§6.11）；对端断开写失败同理。

**sendFile（流式下载）**：`Std.IO.File` 64KB 分块读 + write 循环；`Content-Length` = 文件大小（非 chunked）；MIME 按扩展名（附录 B）；Range / 断点续传为 P5。

**请求侧流式（P5 设计要点）**：

- 注册期声明：`RouteInfo.streamBody = true` → `_HttpParser` 解析到头区空行即停、不预读 body，连接与预读残留移交 handler；
- `req.bodyStream()` → `HttpBodyReader`：`read( ByteBuffer dst ) → Int32`（0 = EOF；Content-Length / chunked 感知；累计字节超上限走 413 语义）；
- 边收边落盘上传：handler 循环 read → `File` 追加写；multipart 流式解析（boundary 跨块匹配）为可选进阶，v1 的 `UploadedFile` 全内存形态并行保留；
- Upgrade（WS）路由不受影响（Upgrade 请求无 body 语义）。

### §6.7 JSON 封装与序列化契约

- 对象 → JSON：一律 `BaseJson.fromData( obj )`；成员规则 = `Serializable` 体系（public 默认参与 / `@NonSerialized` 排除 / `@SerializeField` 强制，见 `Lib/Core/Attribute.sl`）；
- JSON → 对象：`BaseJson.toData<T>()`；
- **`ApiResult` 封装的组包方式**：因 `data` 是关键字、字段只能叫 `payload`，`res.jsonOk( payload )` 的输出由框架**手工组 JSON 树**（`{"code":..,"message":..,"data":<fromData(payload)>}`），键名稳定为 `"data"` 且键序固定（code → message → data）——不依赖 fromData 对 ApiResult 自身的字段名序列化；嵌套 payload 仍走 fromData 递归（**P0 验收含嵌套 DTO 用例**，若 fromData 嵌套深度有坑即整体回落手工 JsonValue 组装并记入 §10）；
- 数值/布尔/字符串/容器（List/Map/Array）映射表跟随 fromData 现状，不自造。

### §6.8 WebSocket 会话

- `WsSession` 包装 `Net.WebSocketStream` + 注册表键（GUID）+ 路由信息 + 标签表（P2）；
- **`acceptBuffered` 增量**：`Websocket.sl` 新增重载 `accept( TcpStream raw, ByteBuffer preRead, WsOptions opt )`——`_readLine/_readHeaders` 优先消费 `preRead` 残留字节再读流（Std 侧唯一改动，纯增量、不影响现有两参形式；实现时同步 `md/lib/Std/net/Websocket.md`）；
- 会话协程：升级成功后 `spawnClosure1( handler, session )`；协程退出（handler ret / receive null）→ 注销注册表 → `onWsClose` 钩子；
- 广播 = 遍历该路由会话表逐个 `sendText`；单会话已断（`try?` 吞错）跳过；
- 心跳：透传 `WebSocketStream` 的 autoPong；服务端主动 Ping 策略（闲置 N 秒探测）P3 再议。

### §6.9 插件模型与中间件管道

**管道执行**（`Plugin.sl` 内执行器）：

```
runPipeline( req, res, mws, handler ):
    环序：mws[0] → mws[1] → … → handler
    Next 令牌：每环框架构造 SLang.Next 实例（内持下一环索引），
              nx.go() = 调用下一环（SL 无方法引用 → 框架持索引逐环构造）
    规则：任一环未调 nx.go() 即短路；中间件必须经 res.* 写响应或短路
    异常：任一环抛错 → 捕获 → app.onError(req, res, e) → 未消费则 500 ApiResult 兜底
```

- **中间件 vs 插件**：中间件 = 单个三参 `Function`；插件 = 类（可携带配置与多条注册）；`app.use( Function )` 与 `app.use( ServerPlugin )` 重载区分；
- 插件 `onInstall` 在 `use()` 时**立即**执行（非 start 时），保证"先 use 中间件、后注册路由"与"路由文档由插件追加"都成立；
- 执行序：全局中间件按注册序 → 路由 handler；**路由级中间件**（`app.onGet( path, fn, mw1 )` variadic 尾参）P3；
- WS 会话钩子（`onWsOpen/onWsClose`）对所有插件广播调用。

### §6.10 OpenAPI 生成与 docs 页面

**实例探针法**（零反射约束下的 schema 推导）：

```
样例实例 → BaseJson.fromData → JSON 树 → 逐节点映射 schema：
    string → {"type":"string"}     number(整数) → {"type":"integer"}
    bool → {"type":"boolean"}      array → {"type":"array","items":<元素探针>}
    object → {"type":"object","properties":{...}}   null → {"nullable":true}
生成的 schema 进 components.schemas（类型名做键，复用去重），operation 引用 $ref
```

**openapi.json 骨架**（OpenAPI 3.0.3）：

```json
{
  "openapi": "3.0.3",
  "info": { "title": "<app 名>", "version": "1.0.0" },
  "servers": [ { "url": "http://0.0.0.0:8080" } ],
  "paths": {
    "/api/user/{id}": {
      "get": {
        "summary": "获取单个用户", "tags": ["用户"],
        "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "integer" } } ],
        "responses": { "200": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/User" } } } } }
      }
    }
  },
  "components": { "schemas": { "User": { ... }, "CreateUserReq": { ... } } }
}
```

**form / multipart 的文档形态**：`r.formField(...)` 收集 → requestBody `application/x-www-form-urlencoded` 的 object schema；`r.uploadField(...)` → requestBody `multipart/form-data`（该字段 `type: string, format: binary`）。

**SSE 的文档形态**：响应 content 记 `text/event-stream`（探针/示例不受影响；Apifox 侧按事件流调试）。

**文档路由**：`GET /docs` → 内嵌 HTML（读同源 `/openapi.json` 渲染）；`GET /openapi.json` → 生成全文。两路由与用户路由冲突时**用户优先**（冲突检测豁免内置路由，`app.docs(false)` 整体关闭）。

**importOpenApi**：解析 paths → 对未注册 pattern 建"文档路由"（RouteInfo.handler = 框架内置 501 应答）；已被真实路由覆盖的路径仅合并文档元数据。

### §6.11 错误处理协议

| 层 | 行为 |
|----|------|
| 协议层（解析失败/超限/boundary 缺失） | 400/413/431 + `ApiResult.fail` 体 + 按需断连；不进管道 |
| 匹配层 | 404 / 405 + `ApiResult.fail` 体 |
| 处理器/中间件异常 | 全捕获（闭包无 throws 的补偿）→ `app.onError(req, res, e)` → 未消费则 500 `ApiResult.fail(500,"internal error")` |
| WS 会话异常 | 会话协程内捕获 → 关连接 → `onWsClose`；不影响其他会话 |
| 服务器级（listen 失败等） | 原样抛 `NetError.*` / `ServerError.*` 给调用方（启动期错误不吞） |

---

## §7 attribute 路由接线（P4，唯一涉及 C VM/Front 的阶段）

### 7.1 主方案：装配期原生收集 + 系统方法导出

与 `ATTRIBUTE_DESIGN.md` §4.3 注册表演进方向一致（"按名 + stage==Preload 匹配"）：

1. **C VM**（`vm/runtime/attribute/vm_runtime_attribute.c`）：注册 `Route` 原生 handler——装配期扫描全模块 `attributeList` 中 `Route` 实例（含 path/verb/宿主类/宿主方法），收集进原生路由表（AOT 注册表同款模式）；
2. **C VM**：新增系统方法 `SystemServerGetPreloadRoutes()` → 返回路由描述（List/序列化串）；
3. **SL 层**：`app.usePreloadRoutes()` 在 start 时拉表 → 生成 `RouteInfo`（handler = 按宿主类/方法构造的 `(req, res)` 转发闭包）→ 进 Router；
4. **R7 四处同步**：`@Route` 声明（`RouteAttr.sl`，收编自 `Lib/Std/Net/Route.sl`）+ `Core.jsonc systemCalls[]`（若挂 Core）/ `Server.jsonc` + `src/vm/system_method_call/xxx_system_method.c` + `Front/Define.cs ESystemMethodCall`；新 .c 文件三处登记（R6）。

### 7.2 备选轻量方案（若 C VM 排期紧张）

`Route` stage 改 Runtime + 新增 `SystemAttributeListAttributedMethods( className )`（枚举带 attribute 的方法名）→ SL 层 `app.use( Controller )` 逐方法 `getMethodAttributes` 查询。改动同为 C VM 侧，但收集逻辑留在 SL，原生面更小。

### 7.3 前置调研项（P4 开工条件）

- Front `MetaAttribute.ResolveHandleType()` 名称回退表对**新 attribute 名**（如 `@Doc`）的支持方式——未知名是否默认按带参构造解析；不可靠则文档元数据 attribute 全部走 `@Route` 单名 + 参数表；
- `@Route( path, verb )` 双参构造在 Front attribute 解析层的合法性（`@DllStaticImport` 双参先例存在，风险低）。

### 7.4 存量处置

`Lib/Std/Net/Route.sl`（未入编译清单的占位）在 P4 落地时**迁入本模块改写为 `RouteAttr.sl`**，原文件删除——避免同名 attribute 双定义；`ATTRIBUTE_DESIGN.md` §6.6 与 `md/lib/Std/net/Http.md` L156 的"未接线"说明同步更新（R14）。

---

## §8 分阶段实施计划

| 阶段 | 范围 | 产出 | 验收 |
|------|------|------|------|
| **P0** 引擎 | `HttpServerCore` + `_HttpParser` + 响应编码 + keep-alive；`WebSocketStream.acceptBuffered` 重载（Std 增量）；**前置探针：CallClosure 多参直调（双参/三参闭包变量调用）** | 无路由的裸 HTTP 服务 + `test/Other/NetTest/` 旁路探针 | curl 往返 200/404/413/431；HttpClient 同进程回环；多参闭包探针通过；`md/lib/Std/net/Websocket.md` 同步 |
| **P1** 路由与封装 | `Router/RouteInfo/RouteGroup` + `Request/Response/Next` 全表 + `ApiResult` + **BodyParser（json/form/bin）** + `app.run/start/stop` + WS 路由升级 + `WsSession` + `WebSocketServer` 独立形态 | §4.1–§4.4 与 §4.6 全部示例可运行 | ExpendTest 新增 `Server/` 子目录用例（各动词/路径参数/查询/json·form·bin body/keep-alive/WS echo/广播）全绿 |
| **P1.5** 上传 | multipart 解析器 + `UploadedFile`（saveAs/saveInto/安全剥离）+ `maxUploadBytes/maxFileBytes` | §4.4 文件上传全部示例可运行 | multipart 混合（文件+普通字段）/多文件同字段/路径穿越攻击用例（`../../evil.txt` → 剥离为 `evil.txt`）/超限 413 |
| **P2** 管道插件与流式 | 中间件管道（Next.go）+ `ServerPlugin` + `logger/cors/staticFiles` + **流式三件套（`StreamWriter` chunked / `SseWriter` SSE / `sendFile` 流式下载）** + `wsEvents` 糖 + 通配路由 | §4.5 与 §4.7 全部示例可运行 | 中间件短路/后置/传值用例；静态目录含 MIME 判定；curl 分块接收 chunked 往返 / SSE 浏览器 EventSource / 大文件流式下载内存峰值用例 |
| **P3** 文档 | `OpenApiBuilder` + `/docs` + `/openapi.json` + `importOpenApi` + `bodyLimit/requestId` 插件 + `maxConnections` | §4.8 全部能力 | **openapi.json 经 Apifox 实际导入验证**（接口/分组/schema/example 齐全；含 form/multipart 文档）；嵌套 DTO schema 正确 |
| **P4** attribute | §7 接线（C VM 原生收集 + 系统方法 + R7/R6 登记）| §4.10 语法可用 | `@Route` 控制器用例；`Route.sl` 迁移收编；相关 md 同步 |
| **P5** 打磨 | 前缀树路由 / **请求侧流式（`r.streamBody` + `HttpBodyReader` 边收边落盘上传）** / Range 断点续传 / 主动心跳 / `rateLimit/auth` 插件 / TLS 服务端评估 | 性能与运维项 | 基准（BenchMark 用例集）+ 压测 |

每阶段完成即触发 §9 文档同步清单（R14：代码改了文档必须跟上）。

---

## §9 工程落位与文档同步

| 事项 | 落点 |
|------|------|
| 模块登记 | `Server.jsonc` + `Server.sp`（本目录）；导出 `out/export/SLangServer/` |
| 用例集 | P1 起进 `simple_language/test/ExpendTest/Server/`（宿主 `CSimpleVMStdTest`，`-test` 入口约定见 `md/project/test-guide.md`；量大后独立 `ServerTest` 用例集+宿主，按 test-guide 三步） |
| 文档（用户侧） | `md/lib/` 新增 `Server.md`（用法文档，本文档 §4 收敛而成）+ `md/INDEX.md` §4 挂链（R13） |
| 文档（Std 侧） | `md/lib/Std/net/Websocket.md` 补 `acceptBuffered` 重载；`md/lib/Std/net/Net.md` L34"不含 HTTP 服务端"措辞更新为指向 SLangServer |
| AGENTS.md | §1 表格 `simple_language/` 行与 §3 路由表补 `SLang/Server` 条目 |
| 故障排查 | C VM 侧改动（仅 P4）按 R12 先插桩后修复；复盘按 R10 进 `md/ai/故障排查流程.md` §6 |
| 构建脚本 | 无需改 `scripts/compile-stdlib.ps1`（其编译三件套 Core/Math/Std）；SLangServer 由用例集 jsonc references 拉入编译链，必要时评估加入批量脚本 `-Tests` 矩阵 |
| 依赖边界守护 | 评审/提交时核对 jsonc `references` 仅 Core/Std（G8）；SL 源文件 import 只允许 `SLangServer` 自身 + `Std` + `Core` |

---

## §10 风险与已知限制

| # | 风险/限制 | 影响 | 对策/声明 |
|---|-----------|------|-----------|
| 1 | **Function 变量多参直调无先例**（`(req,res)` / `(req,res,nx)` 依赖 CallClosure 多参） | P1 全部签名 | P0 前置探针；若不支持，回落单参签名 `function( SLang.Request req )` + `req.res` 取 Response（类型不变，仅取用方式变），文档已备该降级路径 |
| 2 | 闭包无类型签名 → 处理器 arity/类型错误运行期才炸 | G1 体验 | 文档固化签名约定；P1 用例覆盖错误签名场景（框架按约定 catch，返回 500 而非崩 VM） |
| 3 | `BaseJson.fromData` 嵌套对象序列化深度/容器支持未验证 | G5 | P0 验收专项；不达标则 ApiResult 整体手工组树（JsonValue），DTO 仍走 toData |
| 4 | multipart v1 全量驻留内存（受 maxUploadBytes 约束） | 大文件内存峰值 | 设计声明（§1.2 非目标）；流式上传 P5；默认 100MB 上限可配 |
| 5 | 单 VM 协程 = 单核事件循环，CPU 密集 handler 阻塞全部连接 | 吞吐 | 设计声明：v1 面向 IO 密集；CPU 密集建议 isolate（`Lib/Std/Isolate`）外置，P5 评估 handler-isolate 桥 |
| 6 | 无 TLS 服务端 | 生产部署 | v1 明确非目标（§1.2）；前置反代（nginx）承接 TLS 是标准解 |
| 7 | 预读缓冲与 WS 握手衔接（acceptBuffered） | 升级失败率 | P0 探针用例：客户端握手紧跟额外字节（如提前发的首条消息）不丢失 |
| 8 | `docs` 页面自研（无 Swagger UI 完整功能） | 文档体验 | 定位为"够用的只读文档 + GET 试调"；完整 Swagger UI 静态包为 P5 可选（体积 ~1MB） |
| 9 | `HttpHeaders`/`File`/`Timer` 依赖 Std 模块引用链 | 模块耦合 | G8 白名单内；与 Mysql→Core 同款机制，风险低 |
| 10 | `SLang` 命名空间跨模块共享（Mysql/Server 并存） | 名称冲突 | 类名互不重叠（Server/Request/Response/... vs Mysql/...）；新增类名前先查 `Lib/SLang/**` |
| 11 | keep-alive 空闲占用协程 | 资源 | 每连接一协程成本极低（C VM 协程轻量）；`keepAliveTimeoutMs` 兜底回收 |
| 12 | 修改公共结构体布局的增量构建坑（AGENTS.md 已知坑） | 仅 P4 C VM 改动 | 遵循"改布局必删树全量重建" |
| 13 | 关键字碰撞（get/set/next/data/as/is） | API 命名 | 已全部规避（§2.3 表）；后续新增 API 命名前查词法器 case 表 |
| 14 | chunked/keep-alive 交互与 SSE 长连接：流式期间连接复用状态机复杂；慢客户端长期占用协程 | P2 流式三件套 | 流式期间每次写出刷新空闲计时；`writeTimeoutMs` 超时兜底断连；close 兜底补终止块（handler 异常退出防连接悬挂）；P2 用例覆盖"流式完成后同连接继续请求"与"handler 中途退出"两条路径 |

---

## §11 附录

### A. 状态码 reason 表（HttpServerCore 内置）

200 OK · 201 Created · 204 No Content · 301 Moved Permanently · 302 Found · 304 Not Modified · 400 Bad Request · 401 Unauthorized · 403 Forbidden · 404 Not Found · 405 Method Not Allowed · 409 Conflict · 413 Payload Too Large · 415 Unsupported Media Type · 431 Request Header Fields Too Large · 500 Internal Server Error · 501 Not Implemented · 503 Service Unavailable（其余按需增补，未知码 reason 置空）

### B. 内置 MIME 表（StaticFiles / res.file / contentType 判定）

html → text/html · js → application/javascript · css → text/css · json → application/json · xml → text/xml · txt → text/plain · png/jpg/jpeg/gif/webp/svg/ico → image/* · mp3/wav → audio/* · mp4/webm → video/* · pdf · zip · gz · wasm → application/wasm · woff/woff2 → font/*（未命中 → application/octet-stream）

### C. OpenAPI 类型映射

| SL 类型 | JSON schema |
|---------|-------------|
| string | `{"type":"string"}` |
| Int8/16/32/64（探针整数值） | `{"type":"integer"}` |
| Float32/64 | `{"type":"number"}` |
| bool | `{"type":"boolean"}` |
| List/Array（探针数组） | `{"type":"array","items":…}` |
| 类实例（探针对象） | `{"type":"object","properties":{…}}` 进 components.schemas |
| null 字段 | `{"nullable":true}` |
| 上传字段（`uploadField`） | `{"type":"string","format":"binary"}`（multipart） |

### D. ServerError 码表

| 名称 | code | 语义 |
|------|------|------|
| DuplicateRoute | 1 | 同 method+pattern 重复注册 |
| AlreadyResponded | 2 | 终端响应方法二次调用 |
| NotRunning | 3 | stop/awaitStop 于未启动时调用 |
| AlreadyRunning | 4 | 重复 start |
| InvalidConfig | 5 | 配置非法（端口 0、目录不存在等） |
| BadUpgrade | 6 | Upgrade 握手失败 |
| DocConflict | 7 | 内置文档路由与用户路由命名冲突（用户优先，仅告警） |

---

> **评审通过后的第一步**：按 §8 P0 开工——先建 `Server.jsonc` / `Server.sp` / 空骨架文件与本文档落位，再写 **CallClosure 多参直调探针**（风险 #1 前置排雷），然后实现 `HttpServerCore` 探针用例（`test/Other/NetTest/` 旁路），跑通 curl 往返后进入 P1。
