# Sqlite（SQLite 数据库）

`DB.Sqlite3` 是仿 Python `sqlite3` 模块风格的 SQLite 数据库封装，位于 `Std` 标准库的 `DB` 命名空间下。

核心类：

| 类 | 说明 |
|------|------|
| `DB.Sqlite3` | 入口类，提供 `connect()` 静态方法连接数据库 |
| `DB.Connection` | 数据库连接对象，仿 Python `sqlite3.Connection` |
| `DB.Cursor` | 游标对象，仿 Python `sqlite3.Cursor`，执行 SQL 和读取结果 |
| `DB.Row` | 行数据容器，仿 Python `sqlite3.Row` |
| `DB.Command` | ADO.NET 风格辅助类，执行非查询 SQL |

枚举：

| 枚举 | 说明 |
|------|------|
| `DB.SQLITE_OPEN` | 打开模式标志（READONLY=1, READWRITE=2, CREATE=4, URI=128, MEMORY=256 等） |
| `DB.SqliteErrorCode` | 错误码枚举（OK=0, ERROR=1, BUSY=5, CORRUPT=11 等） |

---

## 1. 连接数据库

```sl
# 连接（路径不存在会自动创建）
var conn = DB.Sqlite3.connect("test.db")

# 连接失败返回 null
if conn == null
{
    Console.println("连接数据库失败")
    return
}
```

---

## 2. 建表

```sl
# 使用 Command 执行 DDL
var cmd = conn.createCommand("CREATE TABLE IF NOT EXISTS test1 (uid INTEGER, name TEXT)")
cmd.executeNonQuery()

# 或使用游标执行
var cursor = conn.cursor()
cursor.execute("CREATE TABLE IF NOT EXISTS test2 (id INTEGER, value TEXT)")
```

---

## 3. 插入数据

使用 Tuple 绑定参数（`?` 占位符）：

```sl
var cursor = conn.cursor()

# 插入单条
cursor.execute("INSERT INTO test1 (uid, name) VALUES (?, ?)", Tuple(1, "alice"))
cursor.execute("INSERT INTO test1 (uid, name) VALUES (?, ?)", Tuple(2, "bob"))
cursor.execute("INSERT INTO test1 (uid, name) VALUES (?, ?)", Tuple(3, "charlie"))

# 不带参数的 SQL
cursor.execute("INSERT INTO test1 (uid, name) VALUES (4, 'diana')")
```

获取影响行数和 rowid：

```sl
Console.println(cursor.changes())          # 1（最近 DML 影响行数）
Console.println(cursor.lastInsertRowid())   # 最近插入的 rowid
```

---

## 4. 查询数据

### 4.1 fetchall

获取所有行，返回 `Array<Row>`：

```sl
cursor.execute("SELECT * FROM test1")
var rows = cursor.fetchall()

for i = 0, i < rows.length, i++
{
    DB.Row row = rows._getItem_(i) as DB.Row
    Console.println(row._getItem_(0).toString() + ", " + row._getItem_(1))
}
```

### 4.2 fetchone

逐行获取，无数据返回 null：

```sl
cursor.execute("SELECT * FROM test1 ORDER BY uid")
var row = cursor.fetchone()
while row != null
{
    Console.println(row._getItem_(0).toString() + ", " + row._getItem_(1))
    row = cursor.fetchone()
}
```

### 4.3 条件查询（参数绑定）

```sl
cursor.execute("SELECT * FROM test1 WHERE uid > ?", Tuple(1))
var rows = cursor.fetchall()
for i = 0, i < rows.length, i++
{
    DB.Row row = rows._getItem_(i) as DB.Row
    Console.println("uid=" + row._getItem_(0).toString() + ", name=" + row._getItem_(1))
}
```

---

## 5. 更新与删除

```sl
# 更新
cursor.execute("UPDATE test1 SET name = ? WHERE name = ?", Tuple("Alice", "alice"))
Console.println(cursor.changes())    # 1（影响行数）

# 删除
cursor.execute("DELETE FROM test1 WHERE name = ?", Tuple("bob"))
Console.println(cursor.changes())    # 1
```

---

## 6. Connection 便捷方法

`Connection.execute()` 直接执行 SQL 并返回 Cursor，无需手动创建游标：

```sl
# 不带参数
var c1 = conn.execute("SELECT * FROM test1")
var row = c1.fetchone()

# 带参数
var c2 = conn.execute("SELECT * FROM test1 WHERE uid = ?", Tuple(1))
var r = c2.fetchone()
if r != null
{
    Console.println(r._getItem_(1))
}
```

---

## 7. Command（ADO.NET 风格）

`Command` 用于执行非查询 SQL（DDL、DML），返回 SQLite 结果码：

```sl
var cmd = conn.createCommand("DROP TABLE IF EXISTS test2")
cmd.executeNonQuery()

cmd = conn.createCommand("CREATE TABLE IF NOT EXISTS test2 (id INTEGER, value TEXT)")
cmd.executeNonQuery()
```

---

## 8. 错误信息

```sl
Console.println(conn.errmsg())    # 最近错误信息字符串
```

---

## 9. 资源清理

```sl
cursor.close()    # 关闭游标，释放预编译语句
conn.close()       # 关闭数据库连接
```

---

## 10. 完整 CRUD 示例

```sl
import Std

# 1. 连接数据库
var conn = DB.Sqlite3.connect("Resources/mydb")
if conn == null { ret }

# 2. 建表
var cmd = conn.createCommand("CREATE TABLE IF NOT EXISTS test1 (uid INTEGER, name TEXT)")
cmd.executeNonQuery()

# 3. 创建游标
var cursor = conn.cursor()

# 4. 插入数据
cursor.execute("INSERT INTO test1 (uid, name) VALUES (?, ?)", Tuple(1, "alice"))
cursor.execute("INSERT INTO test1 (uid, name) VALUES (?, ?)", Tuple(2, "bob"))
cursor.execute("INSERT INTO test1 (uid, name) VALUES (?, ?)", Tuple(3, "charlie"))

# 5. 查询全部
cursor.execute("SELECT * FROM test1")
var rows = cursor.fetchall()
for i = 0, i < rows.length, i++
{
    DB.Row row = rows._getItem_(i) as DB.Row
    Console.println(row._getItem_(0).toString() + ", " + row._getItem_(1))
}

# 6. 条件查询
cursor.execute("SELECT * FROM test1 WHERE uid > ?", Tuple(1))
var rows2 = cursor.fetchall()

# 7. 更新
cursor.execute("UPDATE test1 SET name = ? WHERE name = ?", Tuple("Alice", "alice"))
Console.println("changes = " + cursor.changes())

# 8. 删除
cursor.execute("DELETE FROM test1 WHERE name = ?", Tuple("bob"))
Console.println("changes = " + cursor.changes())

# 9. 逐行查询
cursor.execute("SELECT * FROM test1 ORDER BY uid")
var row = cursor.fetchone()
while row != null
{
    Console.println(row._getItem_(0).toString() + ", " + row._getItem_(1))
    row = cursor.fetchone()
}

# 10. 清理
cursor.close()
conn.close()
```

---

## 11. Row 行数据

`DB.Row` 是查询结果的行容器：

| 成员 | 说明 |
|------|------|
| `_getItem_(index)` | 按列下标读取值（0-based） |
| `length` | 列数（只读属性） |
| `toString()` | 格式化为 `(val1, val2, ...)` |

```sl
cursor.execute("SELECT uid, name FROM test1")
var row = cursor.fetchone()
Console.println(row.length)            # 2（列数）
Console.println(row._getItem_(0))      # uid 值
Console.println(row._getItem_(1))      # name 值
```

---

## 12. 参数绑定说明

- SQL 语句中使用 `?` 作为参数占位符。
- 参数通过 `Tuple` 传递，如 `Tuple(1, "alice")` 对应两个 `?`。
- 所有参数统一以文本类型绑定（依赖 SQLite 类型亲和性），数值会自动转换。
- 参数索引从 1 开始（SQLite 约定），内部自动处理。

---

## 13. API 速查

### DB.Sqlite3

| 方法 | 说明 |
|------|------|
| `static connect(path)` | 连接数据库，返回 `Connection`，失败返回 null |

### DB.Connection

| 方法 | 说明 |
|------|------|
| `cursor()` | 创建游标，返回 `Cursor` |
| `execute(sql)` | 执行无参数 SQL，返回 `Cursor` |
| `execute(sql, Tuple args)` | 执行带参数 SQL，返回 `Cursor` |
| `createCommand(sql)` | 创建 `Command` 对象 |
| `commit()` | 空实现（SQLite 默认自动提交） |
| `changes()` | 最近 DML 影响行数 |
| `lastInsertRowid()` | 最近插入的 rowid |
| `errmsg()` | 最近错误信息 |
| `close()` | 关闭连接 |

### DB.Cursor

| 方法 | 说明 |
|------|------|
| `execute(sql)` | 执行无参数 SQL，返回 this（支持链式） |
| `execute(sql, Tuple args)` | 执行带参数 SQL，返回 this |
| `fetchone()` | 取一行，无数据返回 null |
| `fetchall()` | 取所有行，返回 `Array<Row>` |
| `changes()` | 最近 DML 影响行数 |
| `lastInsertRowid()` | 最近插入的 rowid |
| `close()` | 关闭游标 |

### DB.Command

| 方法 | 说明 |
|------|------|
| `executeNonQuery()` | 执行非查询 SQL，返回 SQLite 结果码 |

### DB.Row

| 成员 | 说明 |
|------|------|
| `_getItem_(index)` | 按列下标读取值 |
| `length` | 列数（只读属性） |
| `toString()` | 格式 `(val1, val2, ...)` |

---

## 14. 参考文件

- 实现：[source/Front/Lib/Std/DB/Sqlite.sl](../../source/Front/Lib/Std/DB/Sqlite.sl)
- C 层实现：[csimple_lang/src/vm/system_method_call/sqlite_system_method.c](../../csimple_lang/src/vm/system_method_call/sqlite_system_method.c)
- 测试：[test/ExpendTest/Sqlite3Test.sl](../../test/ExpendTest/Sqlite3Test.sl)
