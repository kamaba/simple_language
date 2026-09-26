# MySQL Connector/C（官方发行包，winx64）

本目录存放 **Oracle 官方 MySQL Connector/C 6.1.11（winx64）** 的头文件与客户端库，
供 `sl_mysql_lib.c` 编译链接使用。这样 SLMysql 模块无需使用者额外安装 MySQL SDK。

## 内容

| 路径 | 说明 |
|---|---|
| `include/` | 官方头文件全集（`mysql.h` / `mysql_com.h` / `mysql_time.h` / `errmsg.h` / `my_*.h` / `mysql/psi/*.h` …），已剔除 `.h.pp` 预编译头 |
| `lib/vs14/mysqlclient.lib` | **静态**客户端库（7.1 MB）。本工程的 VS 工程链接它，使 `sl_mysql_lib.dll` 自包含 |
| `lib/libmysql.lib` | 动态版 `libmysql.dll` 的导入库（备用；改用动态链接时需要同时部署 `libmysql.dll`） |
| `COPYING` | 官方许可文件（GPL v2） |

> 原发行包还包含 `lib/libmysql.dll`（动态库，4.9 MB）与 `bin/` 工具，因体积且当前
> 采用静态链接未一并纳入。需要时从同一发行包取回即可。

## 获取与版本

- 来源：`https://cdn.mysql.com/Downloads/Connector-C/mysql-connector-c-6.1.11-winx64.zip`
- 版本：Connector/C 6.1.11（`INFO_SRC`：branch `release/6.1.11`，2017-07-13）
- 工具集：`mysqlclient.lib` 为 VS2015（v140）构建、`/MD`；本工程用 VS2022（v145）
  链接，Debug 配置下会出现 CRT 兄弟版本冲突提示，已在 VS 工程里用
  `IgnoreSpecificDefaultLibraries=MSVCRT` 处理（见 `cvm_sl_mysql_lib.vcxproj` 注释）。

## 许可

MySQL Connector/C 采用 GPL v2（详见 `COPYING`）。使用静态链接前请确认你的项目
符合 GPL 或已取得 Oracle 商业许可（FOSS Exception 见官方说明）。
