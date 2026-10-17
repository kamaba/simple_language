# SLang Server（`SLang.HttpServerCore` + `SLang.Server`）

HTTP/WS 服务框架。设计文档与实现实录见 `source/Front/Lib/SLang/Server/SERVER_DESIGN.md`；验收用例 `test/ExpendTest/Server/ServerP1Test.sl`。

## 快速开始

```csharp
import Std;
import Core;
import SLangServer;

SLang.Server.onGet( "/", function( SLang.HttpRequest req, SLang.Response res )
{
    res.text( "hello" )
} )

SLang.Server.run( 8080 )    # 阻塞运行直至 stop()；start() = 后台启动
```

工程 `.jsonc` 需加依赖（产物目录按实际位置调整）：

```jsonc
"references": [ { "path": "../../out/export/SLangServer", "name": "SLangServer.module.json" } ]
```

## 路由

| 注册 | 说明 |
|------|------|
| `onGet/onPost/onPut/onDelete/onPatch/onHead/onOptions/onAny(path, handler)` | 按动词注册；handler 签名 `(HttpRequest, Response)` |
| `on(method, path, handler)` | 自定义动词 |
| `websocket(path, handler)` | WS 路由（进程内升级形态，见下） |
| `group(prefix)` | `RouteGroup` 前缀分组：`g.onGet(...)` = prefix + path |
| `checkDuplicate()` / `start()` | 路径重复 fail-fast（重复抛 DuplicateRoute，start 前调用） |

匹配规则：**静态段优先**（`/user/new` 不会被 `/user/{id}` 抢走）；`{name}` 提取路径参数；重复路径注册抛错。

```csharp
SLang.Server.onGet( "/user/{id}", function( SLang.HttpRequest req, SLang.Response res )
{
    Int32 id = req.pathInt( "id", 0 )
    res.text( "id=" + id.toString() )
} )
```

## Request（请求读取）

| API | 说明 |
|-----|------|
| `method` / `path` / `rawQuery` / `headers` | 请求行与头 |
| `pathParam(name)` / `pathInt(name, def)` / `pathInt64(name, def)` | 路径参数（坏格式/缺失用默认值） |
| `query(name)` / `queryInt` / `queryBool` / `hasQuery` | 查询参数（percent-decode、`+`→空格） |
| `header(name)` | 单头（大小写不敏感） |
| `bodyText()` / `bodyBytes()` | 原文 body |
| `body<T>()` | JSON 反序列化为 DTO（**类必须 `@Serializable()`**，否则解析结果不可用） |
| `form(name)` / `formInt` / `formBool` / `hasForm` | `application/x-www-form-urlencoded` 表单字段 |

## Response（响应写出，链式）

```csharp
res.status( 201 ).header( "X-Trace", "t1" ).text( "created" )
res.json( user )                       # 序列化对象（DTO 同样须 @Serializable）
res.jsonOk( payload )                  # {"ok":true,"data":...}
res.jsonFail( 1001, "bad input", 400 ) # 业务码 + 消息（httpStatus 可省，默认 400）
res.html( "<b>hi</b>" )
res.redirect( "/login" )
res.bytes( buffer )                    # 二进制（application/octet-stream）
```

## WebSocket

形态 A——HTTP 进程内升级（同端口，`app.websocket(path, handler)`）：

```csharp
SLang.Server.websocket( "/ws/{room}", function( SLang.WsSession s )
{
    SLang.WsMessage m = s.receive()          # 循环收；null = 对端关闭
    if m != null && m.isText
    {
        s.sendText( "echo: " + m.text )      # s.pathParam("room") / s.query("name") 可用
    }
} )
```

形态 B——独立端口（`SLang.WebSocketServer`）：

```csharp
SLang.WebSocketServer.onMessage( function( SLang.WsSession s, SLang.WsMessage m ) { ... } )
SLang.WebSocketServer.onClose( function( SLang.WsSession s ) { ... } )
SLang.WebSocketServer.start( 9090 )
# SLang.WebSocketServer.sessions() / broadcast(text) / stop() / isRunning / port
```

全局辅助：`SLang.Server.sessions(path)` 列在线会话、`SLang.Server.broadcast(path, text)` 群发。

## 生命周期与配置

```csharp
SLang.Server.configure( cfg )   # ServerConfig：maxBodyBytes/maxHeaderBytes/backlog
SLang.Server.start( 8080 )      # 后台启动（协程接受连接）
SLang.Server.awaitStop()        # 等待 stop
SLang.Server.stop()             # 关监听 + 强制断开在途连接（触发 onClose）
SLang.Server.reset()            # 清全部路由（下一轮测试复用）
SLang.Server.isRunning
```

handler 内抛异常：已暂存响应照常写出，未响应则 500；全局兜底 `SLang.Server.onError(function( req, res, ex ))` 可覆盖。

## 编译与运行

```powershell
# 库改动后先编库，再编消费工程（顺序不可颠倒）
dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p source\Front\Lib\SLang\Server\Server.sp -e ir --format binary --export-json
dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p test\ExpendTest\ProjectTest.sp -e ir --format binary --export-json
csimple_lang.exe run out\export\ProjectTest2\ProjectTest.module.json
```
