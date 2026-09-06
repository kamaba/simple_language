# =========================================================================
# DB - SQLite 管理类，仿 Python sqlite3 模块风格
#
# 用法（Python 风格）:
#   conn = DB.Sqlite3.connect("test.db")
#   cursor = conn.cursor()
#   cursor.execute("INSERT INTO test1 (uid, name) VALUES (?, ?)", Tuple(1, "alice"))
#   conn.commit()
#   cursor.execute("SELECT * FROM test1")
#   rows = cursor.fetchall()
#   for i = 0, i < rows.length, i++
#   {
#       row = rows._getItem_(i)
#       Console.println(row._getItem_(0).toString() + ", " + row._getItem_(1))
#   }
#   cursor.close()
#   conn.close()
#
# 底层通过 SystemSqlite3* 系列 system method call 调用 C 层 SQLite API。
#
# 注意：所有类型必须是 namespace 下的顶级类型（不能嵌套在类内）。
# 嵌套类型会让导出端（SLModulePackageWriter）把父类全名写进
# namespaceList，加载端随即创建同名命名空间节点遮蔽类节点，
# 导致 DB.Sqlite3.connect 之类的调用链解析失败。
# =========================================================================

namespace DB
{
    # ---------------------------------------------------------------
    # 枚举（保留已有定义）
    # ---------------------------------------------------------------
    public enum SQLITE_OPEN
    {
        READONLY = 1
        READWRITE = 2
        CREATE = 4
        URI = 128
        MEMORY = 256
        NOMUTEX = 2048
        FULLMUTEX = 4096
        SHAREDCACHE = 8192
        PRIVATECACHE = 16384
    }

    public enum SqliteErrorCode extends Error
    {
        OK = {code = 0}
        ERROR = {code = 1}
        INTERNAL = {code = 2}
        PERM = {code = 3}
        ABORT = {code = 4}
        BUSY = {code = 5}
        LOCKED = {code = 6}
        NOMEM = {code = 7}
        READONLY = {code = 8}
        INTERRUPT = {code = 9}
        IOERR = {code = 10}
        CORRUPT = {code = 11}
        NOTFOUND = {code = 12}
    }

    # ===============================================================
    # Row - 行数据，仿 Python sqlite3.Row
    # ===============================================================
    public class Row
    {
        Array<object> _values = null
        int _count = 0

        _init_(int columnCount)
        {
            this._values = Array<object>(columnCount)
            this._count = columnCount
        }

        # 内部：设置列值
        void _set(int index, object val)
        {
            if index >= 0 && index < this._count
            {
                SystemArraySetValueThis(this._values, index, val)
            }
        }

        # 下标读取：row[0], row[1] ...
        public object _getItem_(int index)
        {
            if index < 0 || index >= this._count
            {
                ret null
            }
            ret SystemArrayGetValueThis(this._values, index)
        }

        get int length()
        {
            ret this._count
        }

        override string toString()
        {
            string s = "("
            for i = 0, i < this._count, i++
            {
                var v = SystemArrayGetValueThis(this._values, i)
                if v == null
                {
                    s = s + "null"
                }
                else
                {
                    s = s + v.toString()
                }
                if i < this._count - 1
                {
                    s = s + ", "
                }
            }
            s = s + ")"
            ret s
        }
    }

    # ===============================================================
    # Cursor - 游标，仿 Python sqlite3.Cursor
    # ===============================================================
    public class Cursor
    {
        Int64 _dbHandle = 0
        Int64 _stmtHandle = 0
        int _columnCount = 0
        bool _closed = false

        _init_(Int64 dbHandle)
        {
            this._dbHandle = dbHandle
        }

        # execute(sql) - 执行 SQL（无参数），返回 this 便于链式调用
        public Cursor execute(string sql)
        {
            this._prepareAndBind(sql, null)
            ret this
        }

        # execute(sql, args) - 执行带参数 SQL，args 为 Tuple
        public Cursor execute(string sql, Tuple args)
        {
            this._prepareAndBind(sql, args)
            ret this
        }

        # 内部：准备语句 + 绑定参数 + DML 自动 step
        void _prepareAndBind(string sql, Tuple args)
        {
            # 释放上一次的语句
            if this._stmtHandle != 0
            {
                SystemSqlite3Finalize(this._stmtHandle)
                this._stmtHandle = 0
            }

            # 准备 SQL
            this._stmtHandle = SystemSqlite3PrepareV2(this._dbHandle, sql)
            if this._stmtHandle == 0
            {
                ret
            }

            this._columnCount = SystemSqlite3ColumnCount(this._stmtHandle)

            # 绑定参数（在 step 之前）
            if args != null
            {
                this._bindParams(args)
            }

            # 非查询语句（INSERT/UPDATE/DELETE/DDL）立即执行一次 step
            if this._columnCount == 0
            {
                SystemSqlite3Step(this._stmtHandle)
            }
        }

        # 内部：将 Tuple 中的参数逐个绑定到预编译语句
        # SQLite 类型亲和性会自动将文本 "42" 转为整数 42
        void _bindParams(Tuple args)
        {
            if this._stmtHandle == 0
            {
                ret
            }
            int count = args.length
            for i = 0, i < count, i++
            {
                var val = args._getItem_(i)
                int idx = i + 1  # SQLite 绑定索引从 1 开始
                if val == null
                {
                    SystemSqlite3BindText(this._stmtHandle, idx, "")
                }
                else
                {
                    string s = val.toString()
                    SystemSqlite3BindText(this._stmtHandle, idx, s)
                }
            }
        }

        # fetchone() - 取一行，无数据返回 null
        public Row fetchone()
        {
            if this._stmtHandle == 0 || this._columnCount == 0
            {
                ret null
            }
            int rc = SystemSqlite3Step(this._stmtHandle)
            if rc != 100  # SQLITE_ROW = 100
            {
                ret null
            }
            ret this._readRow()
        }

        # fetchall() - 取所有行，返回 Array<Row>
        public Array<Row> fetchall()
        {
            int capacity = 16
            Array<Row> rows = Array<Row>(capacity)
            int count = 0

            Row row = this.fetchone()
            while row != null
            {
                if count >= capacity
                {
                    capacity = capacity * 2
                    rows = SystemArrayResize(rows, capacity)
                }
                SystemArraySetValueThis(rows, count, row)
                count++
                row = this.fetchone()
            }

            # 裁剪到实际数量
            rows = SystemArrayResize(rows, count)
            ret rows
        }

        # 内部：从当前行读取各列数据，构造 Row
        Row _readRow()
        {
            Row row = Row(this._columnCount)
            for i = 0, i < this._columnCount, i++
            {
                int colType = SystemSqlite3ColumnType(this._stmtHandle, i)
                if colType == 1  # SQLITE_INTEGER
                {
                    int val = SystemSqlite3ColumnInt(this._stmtHandle, i)
                    row._set(i, val)
                }
                elif colType == 3  # SQLITE_TEXT
                {
                    string val = SystemSqlite3ColumnText(this._stmtHandle, i)
                    row._set(i, val)
                }
                elif colType == 5  # SQLITE_NULL
                {
                    row._set(i, null)
                }
                else
                {
                    # SQLITE_FLOAT(2) / BLOB(4) 等：以文本形式读取
                    string val = SystemSqlite3ColumnText(this._stmtHandle, i)
                    row._set(i, val)
                }
            }
            ret row
        }

        # 获取最近一次 DML 影响的行数
        public int changes()
        {
            ret SystemSqlite3Changes(this._dbHandle)
        }

        # 获取最近插入的 rowid
        public Int64 lastInsertRowid()
        {
            ret SystemSqlite3LastInsertRowid(this._dbHandle)
        }

        # 关闭游标，释放语句
        public void close()
        {
            if this._closed
            {
                ret
            }
            this._closed = true
            if this._stmtHandle != 0
            {
                SystemSqlite3Finalize(this._stmtHandle)
                this._stmtHandle = 0
            }
        }
    }

    # ===============================================================
    # Command - 命令对象（ADO.NET 风格辅助类，保留已有定义）
    # ===============================================================
    public class Command
    {
        Int64 _dbHandle = 0
        string _sql = ""

        _init_(Int64 dbHandle, string sql)
        {
            this._dbHandle = dbHandle
            this._sql = sql
        }

        # 执行非查询 SQL（INSERT/UPDATE/DELETE/DDL），返回 SQLite 结果码
        public int executeNonQuery()
        {
            ret SystemSqlite3Exec(this._dbHandle, this._sql)
        }
    }

    # ===============================================================
    # Connection - 数据库连接，仿 Python sqlite3.Connection
    # ===============================================================
    public class Connection
    {
        Int64 _dbHandle = 0
        bool _isClosed = false

        _init_(Int64 dbHandle)
        {
            this._dbHandle = dbHandle
        }

        # 创建游标
        public Cursor cursor()
        {
            ret Cursor(this._dbHandle)
        }

        # 便捷方法：创建游标并执行 SQL
        public Cursor execute(string sql)
        {
            Cursor c = this.cursor()
            c.execute(sql)
            ret c
        }

        # 便捷方法：创建游标并执行带参数 SQL
        public Cursor execute(string sql, Tuple args)
        {
            Cursor c = this.cursor()
            c.execute(sql, args)
            ret c
        }

        # 创建 Command 对象
        public Command createCommand(string sql)
        {
            ret Command(this._dbHandle, sql)
        }

        # 提交事务（SQLite 默认自动提交，此处为兼容 Python 接口）
        public void commit()
        {
        }

        public void lock()
        {
        }

        public void unlock()
        {
        }

        # 获取最近一次 DML 影响的行数
        public int changes()
        {
            ret SystemSqlite3Changes(this._dbHandle)
        }

        # 获取最近插入的 rowid
        public Int64 lastInsertRowid()
        {
            ret SystemSqlite3LastInsertRowid(this._dbHandle)
        }

        # 获取最近一条错误信息
        public string errmsg()
        {
            ret SystemSqlite3Errmsg(this._dbHandle)
        }

        # 关闭连接
        public void close()
        {
            if this._isClosed
            {
                ret
            }
            this._isClosed = true
            if this._dbHandle != 0
            {
                SystemSqlite3Close(this._dbHandle)
                this._dbHandle = 0
            }
        }
    }

    # ===============================================================
    # Sqlite3 - 入口类，connect() 连接数据库（仿 Python sqlite3.connect）
    # ===============================================================
    public class Sqlite3
    {
        static Connection connect(string databasepath)
        {
            Int64 handle = SystemSqlite3Open(databasepath)
            if handle == 0
            {
                ret null
            }
            ret Connection(handle)
        }
    }
}
