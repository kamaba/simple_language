# HTTP — Net.HttpClient 客户端

> 位置：`source/Front/Lib/Std/Net/HttpClient.sl`、`Uri.sl`、`Route.sl`（namespace `Net`，Route 除外）
> 关系：建立在 [stream.md](stream.md) 的 `NetStream` / `TlsStream` 之上（HTTPS 走 TLS）
> 总览：[net.md](net.md) §10；测试：`test/Other/NetTest/HttpTest.sl`（回环）、`test/Other/HttpWebTest/HttpWebTest.sl`（公网）

---

## 1. 快速上手

```sl
using Net

# ---------- (1) 便捷 GET ----------
Net.HttpResponse r = Net.HttpClient.httpGet( "http://127.0.0.1:19361/a/b?q=1&r=2" )
Console.println( r.statusCode )            # 200
Console.println( r.isOk )                  # true（2xx）
Console.println( r.body )                  # 响应体（UTF-8 文本）

# ---------- (2) 便捷 POST ----------
Net.HttpResponse r2 = Net.HttpClient.httpPost( "http://127.0.0.1:19362/echo",
    "hello=http", "text/plain" )           # body、Content-Type

# ---------- (3) 自定义请求 ----------
Net.HttpClient c = Net.HttpClient()
c.readTimeoutMs = 15000                    # 实例配置
Net.HttpRequest req = Net.HttpRequest()
req.method = Net.HttpMethod.Post
req.url    = "https://example.com/api"
req.headers.setHeader( "X-Token", "abc" )
req.body   = "{ 'k': 1 }"
Net.HttpResponse r3 = c.send( req )

# ---------- (4) 协程异步 ----------
Task t1 = Net.HttpClient.httpGetAsync( "https://www.httpbin.org/get" )
Net.HttpResponse r1 = await t1 as Net.HttpResponse    # 协程内 await 取回
```

---

## 2. Net.HttpClient

### 2.1 便捷静态入口

| 成员 | 签名 | 说明 |
|------|------|------|
| httpGet | `static HttpResponse httpGet( string url ) throws` | GET，默认配置（connect 10s / read 无限） |
| httpPost | `static HttpResponse httpPost( string url, string body, string contentType ) throws` | POST，自动设置 `Content-Type` |
| httpGetAsync | `static Task httpGetAsync( string url )` | 异步 GET，返回 `Task`，`await` 取回 `HttpResponse` |
| httpPostAsync | `static Task httpPostAsync( string url, string body, string contentType )` | 异步 POST，同上 |

### 2.2 实例配置与发送

| 成员 | 类型/签名 | 默认 | 说明 |
|------|-----------|------|------|
| connectTimeoutMs | `Int32` 字段 | 10000 | 建连（TCP+TLS 握手）超时毫秒 |
| readTimeoutMs | `Int32` 字段 | 0 | 读超时毫秒；**0 = 无限阻塞** ⚠ 公网请求务必显式设置 |
| caPem | `string` 字段 | 空串 | 自定义 CA 证书 PEM（自签/私有 CA 场景）；空 = 用系统内置信任链 |
| send | `HttpResponse send( HttpRequest req ) throws` | | 发送请求，同步返回响应 |
| sendAsync | `Task sendAsync( HttpRequest req )` | | 异步发送，返回 `Task` |

---

## 3. Net.HttpRequest

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| method | `HttpMethod` | Get | 请求方法（枚举见下） |
| url | `string` | 空串 | 完整 URL，send 时经 `Uri` 解析 |
| headers | `HttpHeaders` | — | 请求头容器（new 时已创建） |
| body | `string` | 空串 | 请求体；**UTF-8 文本**，空串 = 无 body；内嵌 NUL 会被截断 |

`HttpMethod` 枚举（`source/Front/Lib/Std/Net/HttpMethodEnum.sl`）：

| 值 | 名称 |
|----|------|
| 0 | Get |
| 1 | Post |
| 2 | Put |
| 3 | Delete |
| 4 | Head |

---

## 4. Net.HttpResponse

| 成员 | 类型 | 说明 |
|------|------|------|
| statusCode | `Int32` | 状态码（200、404…） |
| reasonPhrase | `string` | 状态短语（OK、Not Found…） |
| protocol | `string` | 协议串（如 `HTTP/1.1`） |
| headers | `HttpHeaders` | 响应头容器 |
| body | `string` | 响应体（UTF-8 文本；HEAD / 204 / 205 / 304 为空串） |
| isOk | `get bool` | 是否 2xx |
| contentLength | `get Int64` | 声明体长；chunked 传输或缺失时为 **-1** |

**响应体读取三态**（由 send 内部自动处理，调用方只读 `body`）：

| 场景 | 行为 |
|------|------|
| `Transfer-Encoding: chunked` | 逐块拼接，`contentLength` = -1 |
| `Content-Length: n` | 精确读 n 字节，不足则抛 `HttpError(Protocol)` |
| 二者皆无 | 读到连接关闭（EOF）为止 |

---

## 5. Net.HttpHeaders

| 成员 | 签名 | 说明 |
|------|------|------|
| count | `get int count()` | 头数量 |
| add | `void add( string name, string value )` | 追加，**同名头共存**（如多个 Set-Cookie） |
| setHeader | `void setHeader( string name, string value )` | 覆盖式设置，同名先清空再写入 |
| getHeader | `string getHeader( string name )` | 取首个匹配，不存在返回 `null` |
| contains | `bool contains( string name )` | 是否存在（大小写不敏感） |
| nameAt | `string nameAt( int index )` | 按下标取名 |
| valueAt | `string valueAt( int index )` | 按下标取值 |

- 头名匹配全部**大小写不敏感**（RFC 语义）。
- 方法名是 `setHeader`/`getHeader` 而非 set/get：`set`/`get` 是 SL 关键字。
- 遍历：`for int i = 0; i < h.count; i++ { h.nameAt(i) / h.valueAt(i) }`。

---

## 6. Net.Uri

构造即解析：`Net.Uri u = Net.Uri( "https://host:8443/p/q?a=1" )`。

| 成员 | 类型 | 说明 |
|------|------|------|
| scheme | `get string` | 小写化（http/https/ws/wss…） |
| host | `get string` | 主机名 |
| port | `get Int32` | 显式端口；URL 未写为 **-1** |
| path | `get string` | 路径，无则空串 |
| query | `get string` | 查询串（不含 `?`），无则空串 |
| isHttp / isHttps | `get bool` | scheme 判定 |
| isWs / isWss | `get bool` | WebSocket scheme 判定 |
| effectivePort | `get Int32` | 实际端口：显式优先，否则 http/ws=80、https/wss=443，其他 scheme=-1 |
| hostHeader | `get string` | Host 头值：显式端口时 `host:port`，否则仅 host |
| pathAndQuery | `get string` | 请求行资源：`/p/q?a=1`（path+query） |

静态工具：`static bool eqFold( string a, string b )`（ASCII 大小写不敏感比较）。

**不支持**（解析约束，见 net.md §16）：userinfo（`user:pass@`）、IPv6 字面量、percent-encoding 解码、fragment（`#`）。

---

## 7. Route（全局类，不在 namespace Net）

```sl
@Route( "/action/getfin" )     # 标注在类或方法上，类似 FastAPI 路由装饰器
```

- 定义于 `source/Front/Lib/Std/Net/Route.sl`，`public class Route extends Attribute`。
- 属性阶段 `Preload`、目标 `All`：**装配期注册路由表**。
- ⚠ 当前仅完成注册，**未接线**——服务端路由分发随 Net 库另立设计，暂无运行时效果。

---

## 8. Net.HttpError 错误码

`HttpError extends Error`，`code` 字段取值：

| code | 名称 | 触发场景 |
|------|------|----------|
| 1 | UriFormat | URL 语法不合法（缺 scheme/host、端口越界…），**建连前**抛出 |
| 2 | UnsupportedScheme | scheme 不是 http/https，**建连前**抛出 |
| 3 | Protocol | 响应格式违约（坏状态行/坏 chunk/Content-Length 不足…） |
| 4 | IoError | 预留 |

底层 I/O 失败以 **NetError**（TCP 层，见 [tcp.md](tcp.md) §6）或 **TlsError**（TLS 层，见 [stream.md](stream.md) §6）**原样透传**，不会包装成 HttpError。异常捕获写法：

```sl
connect: {
    Net.HttpResponse r = Net.HttpClient.httpGet( "ftp://127.0.0.1/x" )
} catch e {
    ( e as Net.HttpError ).code == Net.HttpError.UnsupportedScheme   # true
}
```

---

## 9. 语义要点

1. **每请求一连接**：无连接池 / keep-alive；每个请求自动带 `Connection: close`，响应读完即关连接。串行多次请求 = 多次建连。
2. **send 自动管理的头**：`Host`（来自 `Uri.hostHeader`）、请求行（method + `pathAndQuery`）、`Content-Length`（body 非空时）、`Connection: close`、POST 便捷入口的 `Content-Type`。手动 `setHeader` 同名头会被覆盖。
3. **HEAD / 204 / 205 / 304 不读 body**：即使有 Content-Length 也不读，`body` 为空串。
4. **readTimeoutMs=0 默认无限阻塞** ⚠：便捷入口 `httpGet`/`httpPost` 即默认配置——服务器不回包会永久挂起。公网环境请用实例 + 显式 `readTimeoutMs`（HttpWebTest 全部显式配置）。
5. **同步入口受 Option A 挂起协议保护**：`root fun()` 入口可直接调用 `httpGet`；协程内网络未就绪时挂起当前协程，fd 就绪后指令重执行（详见 [net.md](net.md)）。
6. **异步入口返回原生 Task**：`httpGetAsync`/`httpPostAsync`/`sendAsync` 返回协程 `Task`，`await t as Net.HttpResponse` 取回；多请求可 `Coroutine.waitAll2` 聚合（HttpWebTest G17 范式）。
7. **caPem 自签信任**：自建 CA / 自签证书场景设置 `caPem = <PEM 文本>`；系统信任链校验失败会抛 `TlsError`。
8. **每请求一连接 + 无重定向跟随**：3xx 响应原样返回，是否跟随由调用方自行处理。

---

## 10. 测试与运行

| 用例 | 位置 | 覆盖 |
|------|------|------|
| 回环单测（L 组，27 断言） | `test/Other/NetTest/HttpTest.sl` | GET/POST 路径与查询、头部解析、chunked、Content-Length、EOF 体、HEAD/204、错误码 1/2、Uri 各属性 |
| 公网集成（G1–G17） | `test/Other/HttpWebTest/HttpWebTest.sl` | example.com / httpbin.org / jsonplaceholder / quotes.toscrape.com；TLS、重定向语义、G17 协程 await + waitAll2 |
| 爬虫实战 | `test/Other/CrawlerTest/` | HttpClient + Json 组合的真实抓取流程 |

```powershell
cd d:\project\lang\simple_language
# 回环（需先有 HttpTest 组的本地回环服务器，随用例自建，端口 19361~19363）
dotnet run --project project\CSimpleVMStdTest -- test\Other\NetTest\ProjectTest.sp
# 公网（需外网可达；全组对 readTimeoutMs 显式配置，不会卡死）
dotnet run --project project\CSimpleVMStdTest -- test\Other\HttpWebTest\HttpWebTest.sp
```
